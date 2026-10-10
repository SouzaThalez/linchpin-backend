using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LinchpinBackend.Data;
using LinchpinBackend.Infrastructure;
using LinchpinBackend.Models.Reports;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace LinchpinBackend.Services;

/// <summary>
/// Regras comuns a todos os relatórios (aula, cenário e manutenção).
/// O formulário é salvo como documento JSON e devolvido com o "id" do banco.
/// </summary>
public partial class ReportService<T>(AppDbContext db, ICurrentUser currentUser) where T : Report, new()
{
    private static readonly string[] DateFields = ["date", "osDate"];

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$")]
    private static partial Regex FieldPathRegex();

    /// <summary>
    /// Lista os relatórios. Cada filtro é "campo=valor" sobre o documento JSON, inclusive campos
    /// aninhados (ex.: user.name=Thales), no mesmo formato que o json-server aceitava.
    /// </summary>
    public async Task<List<JsonObject>> ListAsync(IDictionary<string, string> filters)
    {
        var table = db.Model.FindEntityType(typeof(T))!.GetTableName();
        var conditions = new List<string>();
        var parameters = new List<object>();

        foreach (var (field, value) in filters)
        {
            if (!FieldPathRegex().IsMatch(field))
                throw new AppException($"Filtro inválido: '{field}'.");

            var index = parameters.Count;
            conditions.Add($"JSON_UNQUOTE(JSON_EXTRACT(`Data`, @p{index})) = @p{index + 1}");
            parameters.Add(new MySqlParameter($"@p{index}", "$." + field));
            parameters.Add(new MySqlParameter($"@p{index + 1}", value));
        }

        var sql = $"SELECT * FROM `{table}`";
        if (conditions.Count > 0)
            sql += " WHERE " + string.Join(" AND ", conditions);

        var reports = await db.Set<T>().FromSqlRaw(sql, parameters.ToArray())
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .ToListAsync();

        return reports.Select(ToJson).ToList();
    }

    public async Task<JsonObject> GetAsync(int id) => ToJson(await FindAsync(id));

    public async Task<JsonObject> CreateAsync(JsonObject document)
    {
        document.Remove("id");

        // Se o front não informar o autor, assume o usuário autenticado.
        if (document["user"] is not JsonObject && currentUser.Name is not null)
            document["user"] = new JsonObject { ["name"] = currentUser.Name, ["role"] = currentUser.Role };

        var report = new T { CreatedByUserId = currentUser.Id };
        Apply(report, document);
        db.Set<T>().Add(report);
        await db.SaveChangesAsync();
        return ToJson(report);
    }

    /// <summary>Substitui o documento inteiro (PUT).</summary>
    public async Task<JsonObject> ReplaceAsync(int id, JsonObject document)
    {
        var report = await FindAsync(id);
        document.Remove("id");
        Apply(report, document);
        await db.SaveChangesAsync();
        return ToJson(report);
    }

    /// <summary>Altera apenas os campos enviados (PATCH).</summary>
    public async Task<JsonObject> PatchAsync(int id, JsonObject changes)
    {
        var report = await FindAsync(id);
        var document = Parse(report.Data);
        foreach (var (key, value) in changes)
        {
            if (key == "id") continue;
            document[key] = value?.DeepClone();
        }
        Apply(report, document);
        await db.SaveChangesAsync();
        return ToJson(report);
    }

    public async Task DeleteAsync(int id)
    {
        var report = await FindAsync(id);
        db.Set<T>().Remove(report);
        await db.SaveChangesAsync();
    }

    private static void Apply(T report, JsonObject document)
    {
        report.Data = document.ToJsonString();
        report.UpdatedAt = DateTime.UtcNow;
        report.ReportDate = ReadDate(document);

        if (document["user"] is JsonObject user)
        {
            report.UserName = ReadString(user, "name");
            report.UserRole = ReadString(user, "role");
        }
    }

    private static string? ReadString(JsonObject obj, string key) =>
        obj[key] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    /// <summary>As datas chegam do front como "DD-MM-AAAA".</summary>
    private static DateOnly? ReadDate(JsonObject document)
    {
        foreach (var field in DateFields)
        {
            if (document[field] is JsonValue value && value.TryGetValue<string>(out var text) &&
                DateOnly.TryParseExact(text, ["dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd"], CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date))
                return date;
        }
        return null;
    }

    private static JsonObject ToJson(T report)
    {
        var document = Parse(report.Data);
        document["id"] = report.Id;
        return document;
    }

    private static JsonObject Parse(string json) => JsonNode.Parse(json) as JsonObject ?? new JsonObject();

    private async Task<T> FindAsync(int id) =>
        await db.Set<T>().FindAsync(id) ?? throw new NotFoundException("Relatório não encontrado.");
}
