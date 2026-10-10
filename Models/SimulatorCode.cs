namespace LinchpinBackend.Models;

/// <summary>Código (etiqueta/patrimônio) de uma unidade física do simulador.</summary>
public class SimulatorCode
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool Disabled { get; set; }

    public int SimulatorId { get; set; }
    public Simulator? Simulator { get; set; }
}
