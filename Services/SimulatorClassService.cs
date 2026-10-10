using LinchpinBackend.Data;
using LinchpinBackend.DTOs;
using LinchpinBackend.Infrastructure;
using LinchpinBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace LinchpinBackend.Services;

/// <summary>Classes de simuladores de baixa fidelidade, cada uma com seus simuladores e códigos.</summary>
public class SimulatorClassService(AppDbContext db, SimulatorService simulators)
{
    public async Task<List<SimulatorClassDto>> ListAsync()
    {
        var list = await Query().AsNoTracking().OrderBy(c => c.Id).ToListAsync();
        return list.Select(SimulatorClassDto.From).ToList();
    }

    public async Task<SimulatorClassDto> GetAsync(int id) => SimulatorClassDto.From(await FindAsync(id));

    public async Task<SimulatorClassDto> CreateAsync(SimulatorClassDto dto)
    {
        var entity = new SimulatorClass();
        await ApplyAsync(entity, dto);
        db.SimulatorClasses.Add(entity);
        await db.SaveChangesAsync();
        return SimulatorClassDto.From(entity);
    }

    /// <summary>
    /// Substitui a classe inteira (nome, imagem e lista de simuladores), como o front já faz:
    /// simuladores com id existente são atualizados, os sem id são criados e os ausentes removidos.
    /// </summary>
    public async Task<SimulatorClassDto> UpdateAsync(int id, SimulatorClassDto dto)
    {
        var entity = await FindAsync(id);
        await ApplyAsync(entity, dto);
        await db.SaveChangesAsync();
        return SimulatorClassDto.From(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await FindAsync(id);
        db.SimulatorClasses.Remove(entity);
        await db.SaveChangesAsync();
    }

    private async Task ApplyAsync(SimulatorClass entity, SimulatorClassDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SimulatorClass))
            throw new AppException("O nome da classe é obrigatório.");

        entity.Name = dto.SimulatorClass.Trim();
        entity.ClassImage = string.IsNullOrWhiteSpace(dto.ClassImage) ? "assets/images/default-image.jpg" : dto.ClassImage;

        var incoming = dto.Simulators ?? [];
        var keepIds = incoming.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToHashSet();

        var removed = entity.Simulators.Where(s => !keepIds.Contains(s.Id)).ToList();
        foreach (var simulator in removed)
            entity.Simulators.Remove(simulator);
        var removedIds = removed.Select(s => s.Id).ToList();

        foreach (var simDto in incoming)
        {
            var simulator = simDto.Id is int simId ? entity.Simulators.FirstOrDefault(s => s.Id == simId) : null;
            if (simulator is null)
            {
                simulator = new Simulator();
                entity.Simulators.Add(simulator);
            }
            await simulators.ApplyAsync(simulator, simDto, SimulatorCategories.Low, removedIds);
        }
    }

    private IQueryable<SimulatorClass> Query() =>
        db.SimulatorClasses.Include(c => c.Simulators).ThenInclude(s => s.Codes);

    private async Task<SimulatorClass> FindAsync(int id) =>
        await Query().FirstOrDefaultAsync(c => c.Id == id)
        ?? throw new NotFoundException("Classe de simuladores não encontrada.");
}
