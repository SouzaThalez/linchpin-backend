namespace LinchpinBackend.Models.Reports;

/// <summary>
/// Base dos relatórios. O conteúdo do formulário é guardado como documento JSON (coluna "data"),
/// pois os formulários ainda estão evoluindo. Os campos usados em filtros e listagens
/// são copiados para colunas próprias, indexadas.
/// </summary>
public abstract class Report
{
    public int Id { get; set; }
    public string Data { get; set; } = "{}";
    public DateOnly? ReportDate { get; set; }
    public string? UserName { get; set; }
    public string? UserRole { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class LessonReport : Report;
public class ScenarioReport : Report;
public class MaintenanceReport : Report;
