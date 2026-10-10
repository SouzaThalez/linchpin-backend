using LinchpinBackend.Models;

namespace LinchpinBackend.DTOs;

public class SimulatorCodeDto
{
    public string Code { get; set; } = string.Empty;
    public bool Disabled { get; set; }
}

public class SimulatorDto
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string? Description { get; set; }
    public string? TechnicalDescription { get; set; }
    public string SimulatorCategory { get; set; } = string.Empty;
    public List<SimulatorCodeDto> Codes { get; set; } = [];

    public static SimulatorDto From(Simulator s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Image = s.Image,
        Description = s.Description,
        TechnicalDescription = s.TechnicalDescription,
        SimulatorCategory = s.Category,
        Codes = s.Codes.OrderBy(c => c.Id).Select(c => new SimulatorCodeDto { Code = c.Code, Disabled = c.Disabled }).ToList()
    };
}

/// <summary>Classe de simuladores de baixa fidelidade (mesmo formato usado pelo front).</summary>
public class SimulatorClassDto
{
    public int? Id { get; set; }
    public string SimulatorClass { get; set; } = string.Empty;
    public string? ClassImage { get; set; }
    public List<SimulatorDto> Simulators { get; set; } = [];

    public static SimulatorClassDto From(SimulatorClass c) => new()
    {
        Id = c.Id,
        SimulatorClass = c.Name,
        ClassImage = c.ClassImage,
        Simulators = c.Simulators.OrderBy(s => s.Id).Select(SimulatorDto.From).ToList()
    };
}
