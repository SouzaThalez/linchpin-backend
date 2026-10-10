namespace LinchpinBackend.Models;

public class Simulator
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string? Description { get; set; }
    public string? TechnicalDescription { get; set; }
    public string Category { get; set; } = SimulatorCategories.High;

    /// <summary>Somente simuladores de baixa fidelidade pertencem a uma classe.</summary>
    public int? SimulatorClassId { get; set; }
    public SimulatorClass? SimulatorClass { get; set; }

    public List<SimulatorCode> Codes { get; set; } = [];
}
