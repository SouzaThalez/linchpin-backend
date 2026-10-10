using System.Text.Json;
using System.Text.Json.Nodes;
using LinchpinBackend.Models;
using LinchpinBackend.Models.Reports;
using Microsoft.EntityFrameworkCore;

namespace LinchpinBackend.Data;

/// <summary>
/// Cria o banco (migrations) e, se estiver vazio, importa os dados do antigo db.json do json-server.
/// Senhas são gravadas com hash BCrypt. Os ids antigos não são mantidos: o MySQL gera novos.
/// </summary>
public class DbSeeder(AppDbContext db, IConfiguration config, IWebHostEnvironment env, ILogger<DbSeeder> logger)
{
    public async Task RunAsync()
    {
        await db.Database.MigrateAsync();

        if (await db.Users.AnyAsync())
            return;

        var seedFile = config["Database:SeedFile"];
        if (!string.IsNullOrWhiteSpace(seedFile))
        {
            var path = Path.Combine(env.ContentRootPath, seedFile);
            if (File.Exists(path))
            {
                logger.LogInformation("Importando dados iniciais de {Path}", path);
                await ImportJsonServerAsync(path);
            }
        }

        if (!await db.Users.AnyAsync())
            await CreateDefaultAdminAsync();
    }

    private async Task CreateDefaultAdminAsync()
    {
        var login = config["DefaultAdmin:Login"] ?? "admin";
        var password = config["DefaultAdmin:Password"];
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Banco sem usuários e DefaultAdmin:Password não configurado. Nenhum administrador criado.");
            return;
        }

        db.Users.Add(new User
        {
            Name = "Administrador",
            Login = login,
            Email = config["DefaultAdmin:Email"] ?? "admin@linchpin.local",
            Role = UserRoles.Admin,
            Image = UserRoles.DefaultImage(UserRoles.Admin),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        });
        await db.SaveChangesAsync();
        logger.LogWarning("Administrador padrão '{Login}' criado. Troque a senha após o primeiro acesso.", login);
    }

    private async Task ImportJsonServerAsync(string path)
    {
        var root = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();

        foreach (var u in Items(root, "Users"))
        {
            var role = Str(u, "role") ?? UserRoles.Analyst;
            db.Users.Add(new User
            {
                Name = Str(u, "name") ?? "",
                Login = Str(u, "login") ?? "",
                Email = Str(u, "email") ?? "",
                Image = Str(u, "image") ?? UserRoles.DefaultImage(role),
                Role = role,
                Task = Str(u, "task") ?? "N/A",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Str(u, "password") ?? Guid.NewGuid().ToString())
            });
        }

        foreach (var s in Items(root, "highSimulators").Concat(Items(root, "mediumSimulators")))
            db.Simulators.Add(ToSimulator(s, Str(s, "simulatorCategory") ?? SimulatorCategories.High));

        foreach (var c in Items(root, "lowSimulators"))
        {
            var simulatorClass = new SimulatorClass
            {
                Name = Str(c, "simulatorClass") ?? "",
                ClassImage = Str(c, "classImage")
            };
            foreach (var s in Items(c, "simulators"))
                simulatorClass.Simulators.Add(ToSimulator(s, SimulatorCategories.Low));
            db.SimulatorClasses.Add(simulatorClass);
        }

        foreach (var c in Items(root, "Curses"))
        {
            db.Courses.Add(new Course
            {
                Name = Str(c, "name") ?? "",
                Label = Str(c, "label") ?? "",
                LessonType = Str(c, "lessonType") ?? "curso",
                Description = Str(c, "description"),
                UpdateDate = Str(c, "updateDate")
            });
        }

        AddLessonGroups(root, "Trainings", LessonGroupType.Training);
        AddLessonGroups(root, "Disciplines", LessonGroupType.Discipline);

        AddReports<LessonReport>(root, "LessonReports");
        AddReports<ScenarioReport>(root, "ScenarioReports");
        AddCleaningReports(root);
        AddReports<MaintenanceReport>(root, "manitanceReports");

        await db.SaveChangesAsync();
    }

    private void AddLessonGroups(JsonObject root, string key, LessonGroupType type)
    {
        foreach (var g in Items(root, key))
        {
            var group = new LessonGroup { Type = type, Name = Str(g, "name") ?? "", Value = Str(g, "value") ?? "" };
            foreach (var l in Items(g, "lessons"))
            {
                group.Lessons.Add(new Lesson
                {
                    Name = Str(l, "name") ?? "",
                    Description = Str(l, "description"),
                    UpdateDate = Str(l, "updateDate")
                });
            }
            db.LessonGroups.Add(group);
        }
    }

    private void AddReports<T>(JsonObject root, string key) where T : Report, new()
    {
        foreach (var r in Items(root, key))
        {
            var document = r.DeepClone().AsObject();
            document.Remove("id");
            var user = document["user"] as JsonObject;
            db.Set<T>().Add(new T
            {
                Data = document.ToJsonString(),
                UserName = user is null ? null : Str(user, "name"),
                UserRole = user is null ? null : Str(user, "role"),
                ReportDate = ParseDate(Str(document, "date") ?? Str(document, "osDate"))
            });
        }
    }

    /// <summary>
    /// No db.json antigo as limpezas vinham agrupadas em "OS" (osCode/osTag + lista "simulator"),
    /// às vezes aninhadas. Aqui cada limpeza vira um registro próprio.
    /// </summary>
    private void AddCleaningReports(JsonObject root)
    {
        foreach (var c in Items(root, "CleaningReports").SelectMany(FlattenCleanings))
        {
            var name = Str(c, "simulatorName") ?? "";
            var userName = (c["user"] as JsonObject) is { } u ? Str(u, "name") ?? "" : "";
            var simulator = db.Simulators.Local.FirstOrDefault(s => s.Name == name);
            var date = ParseDate(Str(c, "date"));
            if (date is null) continue;

            db.CleaningReports.Add(new CleaningReport
            {
                Simulator = simulator,
                SimulatorName = name,
                SimulatorCode = Str(c, "simulatorCode") ?? "",
                SimulatorCategory = simulator?.Category ?? Str(c, "simulatorCategory") ?? SimulatorCategories.High,
                CleaningCategory = Str(c, "cleaningCategory") ?? CleaningCategories.Primary,
                CleaningDate = date.Value,
                Findings = Str(c, "findings") ?? "",
                HasDescription = c["hasDescription"] is JsonValue d && d.GetValueKind() == JsonValueKind.True,
                HasIntervention = c["hasIntervention"] is JsonValue i && i.GetValueKind() == JsonValueKind.True,
                Intervention = (c["intervention"] as JsonObject)?.ToJsonString(),
                User = db.Users.Local.FirstOrDefault(x => x.Name == userName),
                UserName = userName
            });
        }
    }

    private static IEnumerable<JsonObject> FlattenCleanings(JsonObject node) =>
        node["simulator"] is JsonArray children
            ? children.OfType<JsonObject>().SelectMany(FlattenCleanings)
            : [node];

    private static Simulator ToSimulator(JsonObject s, string category)
    {
        var simulator = new Simulator
        {
            Name = Str(s, "name") ?? "",
            Image = Str(s, "image"),
            Description = Str(s, "description"),
            TechnicalDescription = Str(s, "technicalDescription"),
            Category = category
        };
        foreach (var c in Items(s, "codes"))
        {
            var code = Str(c, "code");
            if (string.IsNullOrWhiteSpace(code) || simulator.Codes.Any(x => x.Code == code)) continue;
            simulator.Codes.Add(new SimulatorCode
            {
                Code = code,
                Disabled = c["disabled"] is JsonValue v && v.GetValueKind() == JsonValueKind.True
            });
        }
        return simulator;
    }

    private static DateOnly? ParseDate(string? text) =>
        DateOnly.TryParseExact(text, ["dd-MM-yyyy", "dd/MM/yyyy"], out var date) ? date : null;

    private static IEnumerable<JsonObject> Items(JsonObject parent, string key) =>
        parent[key] is JsonArray array ? array.OfType<JsonObject>() : [];

    private static string? Str(JsonObject obj, string key) => obj[key] switch
    {
        JsonValue v when v.GetValueKind() == JsonValueKind.String => v.GetValue<string>(),
        JsonValue v => v.ToJsonString(),
        _ => null
    };
}
