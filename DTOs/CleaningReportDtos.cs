using System.Text.Json.Nodes;
using LinchpinBackend.Models.Reports;

namespace LinchpinBackend.DTOs;

/// <summary>
/// Formulário de limpeza enviado pelo front. Nome, categoria e imagem do simulador
/// vêm do cadastro do simulador; o usuário vem do token.
/// </summary>
public class CleaningReportRequest
{
    public int SimulatorId { get; set; }
    public string SimulatorCode { get; set; } = string.Empty;
    public string CleaningCategory { get; set; } = string.Empty;

    /// <summary>Data da limpeza no formato "DD-MM-AAAA".</summary>
    public string Date { get; set; } = string.Empty;
    public string Findings { get; set; } = string.Empty;
    public bool HasDescription { get; set; }
    public bool HasIntervention { get; set; }
    public JsonObject? Intervention { get; set; }
}

public record CleaningReportUserDto(int? Id, string Name, string? Role);

public class CleaningReportDto
{
    public int Id { get; set; }
    public int? SimulatorId { get; set; }
    public string SimulatorName { get; set; } = string.Empty;
    public string SimulatorCode { get; set; } = string.Empty;
    public string SimulatorCategory { get; set; } = string.Empty;
    public string? SimulatorImage { get; set; }
    public string CleaningCategory { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Findings { get; set; } = string.Empty;
    public bool HasDescription { get; set; }
    public bool HasIntervention { get; set; }
    public JsonObject? Intervention { get; set; }
    public CleaningReportUserDto User { get; set; } = new(null, string.Empty, null);

    public static CleaningReportDto From(CleaningReport r) => new()
    {
        Id = r.Id,
        SimulatorId = r.SimulatorId,
        SimulatorName = r.SimulatorName,
        SimulatorCode = r.SimulatorCode,
        SimulatorCategory = r.SimulatorCategory,
        SimulatorImage = r.Simulator?.Image,
        CleaningCategory = r.CleaningCategory,
        Date = r.CleaningDate.ToString("dd-MM-yyyy"),
        Findings = r.Findings,
        HasDescription = r.HasDescription,
        HasIntervention = r.HasIntervention,
        Intervention = r.Intervention is null ? null : JsonNode.Parse(r.Intervention) as JsonObject,
        User = new CleaningReportUserDto(r.UserId, r.UserName, r.User?.Role)
    };
}
