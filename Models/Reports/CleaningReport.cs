namespace LinchpinBackend.Models.Reports;

/// <summary>
/// Relatório de limpeza de uma unidade de simulador. Cada registro é uma limpeza:
/// qual simulador/código, categoria, data, achados e quem realizou.
/// </summary>
public class CleaningReport
{
    public int Id { get; set; }

    /// <summary>Simulador limpo. Nome e categoria são guardados no relatório para manter o histórico se o simulador mudar.</summary>
    public int? SimulatorId { get; set; }
    public Simulator? Simulator { get; set; }
    public string SimulatorName { get; set; } = string.Empty;
    public string SimulatorCode { get; set; } = string.Empty;
    public string SimulatorCategory { get; set; } = SimulatorCategories.High;

    /// <summary>"primária" ou "secundária" (ver <see cref="CleaningCategories"/>).</summary>
    public string CleaningCategory { get; set; } = CleaningCategories.Primary;
    public DateOnly CleaningDate { get; set; }

    /// <summary>Achados durante a limpeza.</summary>
    public string Findings { get; set; } = string.Empty;

    /// <summary>False quando a limpeza não teve achados ("Nenhum").</summary>
    public bool HasDescription { get; set; }

    /// <summary>Preenchido depois, quando houver uma intervenção sobre os achados.</summary>
    public bool HasIntervention { get; set; }

    /// <summary>Formulário de intervenção (JSON), ainda em evolução no front.</summary>
    public string? Intervention { get; set; }

    /// <summary>Usuário que realizou a limpeza.</summary>
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string UserName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public static class CleaningCategories
{
    public const string Primary = "primária";
    public const string Secondary = "secundária";

    public static readonly string[] All = [Primary, Secondary];

    public static bool IsValid(string? category) => category is not null && All.Contains(category);
}
