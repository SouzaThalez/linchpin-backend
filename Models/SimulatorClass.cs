namespace LinchpinBackend.Models;

/// <summary>Agrupamento dos simuladores de baixa fidelidade (ex.: "Cabeças", "Torsos RCP").</summary>
public class SimulatorClass
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ClassImage { get; set; }
    public List<Simulator> Simulators { get; set; } = [];
}
