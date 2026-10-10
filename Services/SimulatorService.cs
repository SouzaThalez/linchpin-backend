using LinchpinBackend.Data;
using LinchpinBackend.DTOs;
using LinchpinBackend.Infrastructure;
using LinchpinBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace LinchpinBackend.Services;

/// <summary>Simuladores de média e alta fidelidade e regras comuns de simuladores/códigos.</summary>
public class SimulatorService(AppDbContext db)
{
    public async Task<List<SimulatorDto>> ListAsync(string? category)
    {
        var query = db.Simulators.AsNoTracking().Include(s => s.Codes).Where(s => s.SimulatorClassId == null);
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(s => s.Category == category);

        var list = await query.OrderBy(s => s.Id).ToListAsync();
        return list.Select(SimulatorDto.From).ToList();
    }

    public async Task<SimulatorDto> GetAsync(int id) => SimulatorDto.From(await FindAsync(id));

    public async Task<SimulatorDto> CreateAsync(SimulatorDto dto)
    {
        if (dto.SimulatorCategory == SimulatorCategories.Low)
            throw new AppException("Simuladores de baixa fidelidade devem ser criados dentro de uma classe.");

        var simulator = new Simulator();
        await ApplyAsync(simulator, dto);
        db.Simulators.Add(simulator);
        await db.SaveChangesAsync();
        return SimulatorDto.From(simulator);
    }

    public async Task<SimulatorDto> UpdateAsync(int id, SimulatorDto dto)
    {
        var simulator = await FindAsync(id);
        await ApplyAsync(simulator, dto, simulator.SimulatorClassId is null ? null : SimulatorCategories.Low);
        await db.SaveChangesAsync();
        return SimulatorDto.From(simulator);
    }

    public async Task DeleteAsync(int id)
    {
        var simulator = await FindAsync(id);
        db.Simulators.Remove(simulator);
        await db.SaveChangesAsync();
    }

    /// <summary>Copia os dados do DTO para a entidade, sincronizando a lista de códigos.</summary>
    /// <param name="removedSimulatorIds">Simuladores que serão removidos na mesma operação (seus códigos ficam livres).</param>
    internal async Task ApplyAsync(Simulator simulator, SimulatorDto dto, string? forcedCategory = null,
        IReadOnlyCollection<int>? removedSimulatorIds = null)
    {
        var ignoredIds = removedSimulatorIds?.ToList() ?? [];
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new AppException("O nome do simulador é obrigatório.");

        var category = forcedCategory ?? dto.SimulatorCategory;
        if (!SimulatorCategories.IsValid(category))
            throw new AppException($"Categoria inválida. Use: {string.Join(", ", SimulatorCategories.All)}.");

        var codes = (dto.Codes ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c.Code))
            .Select(c => new SimulatorCodeDto { Code = c.Code.Trim().ToUpperInvariant(), Disabled = c.Disabled })
            .ToList();

        var repeated = codes.GroupBy(c => c.Code).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (repeated.Count > 0)
            throw new ConflictException($"Código repetido no simulador: {string.Join(", ", repeated)}.");

        // Só valida os códigos novos, para não bloquear dados antigos que já tinham duplicidade.
        var currentCodes = simulator.Codes.Select(c => c.Code).ToHashSet();
        var newCodes = codes.Select(c => c.Code).Where(c => !currentCodes.Contains(c)).ToList();
        if (newCodes.Count > 0)
        {
            var used = await db.SimulatorCodes
                .Where(c => newCodes.Contains(c.Code) && c.SimulatorId != simulator.Id && !ignoredIds.Contains(c.SimulatorId))
                .Select(c => c.Code)
                .ToListAsync();
            if (used.Count > 0)
                throw new ConflictException($"Código já cadastrado em outro simulador: {string.Join(", ", used)}.");
        }

        simulator.Name = dto.Name.Trim();
        simulator.Image = string.IsNullOrWhiteSpace(dto.Image) ? "assets/images/default-image.jpg" : dto.Image;
        simulator.Description = dto.Description;
        simulator.TechnicalDescription = dto.TechnicalDescription;
        simulator.Category = category!;

        // Atualiza os códigos existentes, adiciona os novos e remove os que saíram da lista.
        var byCode = codes.ToDictionary(c => c.Code);
        foreach (var existing in simulator.Codes.ToList())
        {
            if (byCode.Remove(existing.Code, out var incoming))
                existing.Disabled = incoming.Disabled;
            else
                simulator.Codes.Remove(existing);
        }
        foreach (var added in byCode.Values)
            simulator.Codes.Add(new SimulatorCode { Code = added.Code, Disabled = added.Disabled });
    }

    private async Task<Simulator> FindAsync(int id) =>
        await db.Simulators.Include(s => s.Codes).FirstOrDefaultAsync(s => s.Id == id)
        ?? throw new NotFoundException("Simulador não encontrado.");
}
