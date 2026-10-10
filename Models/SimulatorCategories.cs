namespace LinchpinBackend.Models;

/// <summary>Categorias de fidelidade dos simuladores.</summary>
public static class SimulatorCategories
{
    public const string Low = "baixa";
    public const string Medium = "media";
    public const string High = "alta";

    public static readonly string[] All = [Low, Medium, High];

    public static bool IsValid(string? category) => category is not null && All.Contains(category);
}
