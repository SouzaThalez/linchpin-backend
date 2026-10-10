using System.Globalization;
using LinchpinBackend.Data;
using LinchpinBackend.DTOs;
using LinchpinBackend.Infrastructure;
using LinchpinBackend.Models.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinchpinBackend.Services;

/// <summary>Filtros aceitos em GET /api/cleaning-reports (todos opcionais).</summary>
public class CleaningReportFilter
{
    public string? SimulatorCategory { get; set; }
    public int? SimulatorId { get; set; }
    public string? SimulatorCode { get; set; }
    public string? CleaningCategory { get; set; }
    public bool? HasDescription { get; set; }
    public bool? HasIntervention { get; set; }
    public int? UserId { get; set; }

    [FromQuery(Name = "user.name")]
    public string? UserName { get; set; }
}

public class CleaningReportService(AppDbContext db, ICurrentUser currentUser)
{
    private static readonly string[] DateFormats = ["dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd"];

    public async Task<List<CleaningReportDto>> ListAsync(CleaningReportFilter filter)
    {
        var query = Query().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.SimulatorCategory))
            query = query.Where(r => r.SimulatorCategory == filter.SimulatorCategory);
        if (filter.SimulatorId is not null)
            query = query.Where(r => r.SimulatorId == filter.SimulatorId);
        if (!string.IsNullOrWhiteSpace(filter.SimulatorCode))
            query = query.Where(r => r.SimulatorCode == filter.SimulatorCode);
        if (!string.IsNullOrWhiteSpace(filter.CleaningCategory))
            query = query.Where(r => r.CleaningCategory == filter.CleaningCategory);
        if (filter.HasDescription is not null)
            query = query.Where(r => r.HasDescription == filter.HasDescription);
        if (filter.HasIntervention is not null)
            query = query.Where(r => r.HasIntervention == filter.HasIntervention);
        if (filter.UserId is not null)
            query = query.Where(r => r.UserId == filter.UserId);
        if (!string.IsNullOrWhiteSpace(filter.UserName))
            query = query.Where(r => r.UserName == filter.UserName);

        var reports = await query.OrderBy(r => r.Id).ToListAsync();
        return reports.Select(CleaningReportDto.From).ToList();
    }

    public async Task<CleaningReportDto> GetAsync(int id) => CleaningReportDto.From(await FindAsync(id));

    public async Task<CleaningReportDto> CreateAsync(CleaningReportRequest request)
    {
        var report = new CleaningReport
        {
            UserId = currentUser.Id,
            UserName = currentUser.Name ?? string.Empty
        };
        await ApplyAsync(report, request);
        db.CleaningReports.Add(report);
        await db.SaveChangesAsync();
        return await GetAsync(report.Id);
    }

    /// <summary>Atualiza o relatório (inclusive a intervenção). O autor da limpeza não muda.</summary>
    public async Task<CleaningReportDto> UpdateAsync(int id, CleaningReportRequest request)
    {
        var report = await FindAsync(id);
        await ApplyAsync(report, request);
        await db.SaveChangesAsync();
        return CleaningReportDto.From(report);
    }

    public async Task DeleteAsync(int id)
    {
        var report = await FindAsync(id);
        db.CleaningReports.Remove(report);
        await db.SaveChangesAsync();
    }

    private async Task ApplyAsync(CleaningReport report, CleaningReportRequest request)
    {
        if (!CleaningCategories.IsValid(request.CleaningCategory))
            throw new AppException($"Categoria da limpeza inválida. Use: {string.Join(", ", CleaningCategories.All)}.");

        if (!DateOnly.TryParseExact(request.Date, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            throw new AppException("Data da limpeza inválida. Use o formato DD-MM-AAAA.");

        if (string.IsNullOrWhiteSpace(request.Findings))
            throw new AppException("Informe os achados da limpeza.");

        var code = request.SimulatorCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (report.SimulatorId != request.SimulatorId || report.SimulatorCode != code)
        {
            var simulator = await db.Simulators.Include(s => s.Codes).FirstOrDefaultAsync(s => s.Id == request.SimulatorId)
                ?? throw new AppException("Simulador não encontrado.");
            if (simulator.Codes.All(c => c.Code != code))
                throw new AppException($"O código '{code}' não pertence ao simulador {simulator.Name}.");

            report.SimulatorId = simulator.Id;
            report.Simulator = simulator;
            report.SimulatorName = simulator.Name;
            report.SimulatorCategory = simulator.Category;
            report.SimulatorCode = code;
        }

        report.CleaningCategory = request.CleaningCategory;
        report.CleaningDate = date;
        report.Findings = request.Findings.Trim();
        report.HasDescription = request.HasDescription;
        report.HasIntervention = request.HasIntervention || request.Intervention is not null;
        report.Intervention = request.Intervention?.ToJsonString();
        report.UpdatedAt = DateTime.UtcNow;
    }

    private IQueryable<CleaningReport> Query() => db.CleaningReports.Include(r => r.Simulator).Include(r => r.User);

    private async Task<CleaningReport> FindAsync(int id) =>
        await Query().FirstOrDefaultAsync(r => r.Id == id) ?? throw new NotFoundException("Relatório não encontrado.");
}
