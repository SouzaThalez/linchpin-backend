using LinchpinBackend.Data;
using LinchpinBackend.DTOs;
using LinchpinBackend.Infrastructure;
using LinchpinBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace LinchpinBackend.Services;

/// <summary>Treinamentos e Disciplinas (grupos de aulas). Ambos seguem as mesmas regras.</summary>
public class LessonGroupService(AppDbContext db)
{
    public async Task<List<LessonGroupDto>> ListAsync(LessonGroupType type)
    {
        var list = await db.LessonGroups.AsNoTracking()
            .Include(g => g.Lessons)
            .Where(g => g.Type == type)
            .OrderBy(g => g.Id)
            .ToListAsync();
        return list.Select(LessonGroupDto.From).ToList();
    }

    public async Task<LessonGroupDto> GetAsync(LessonGroupType type, int id) =>
        LessonGroupDto.From(await FindAsync(type, id));

    public async Task<LessonGroupDto> CreateAsync(LessonGroupType type, LessonGroupDto dto)
    {
        Validate(dto);
        await EnsureValueIsFreeAsync(type, dto.Value.Trim());

        var group = new LessonGroup { Type = type, Name = dto.Name.Trim(), Value = dto.Value.Trim() };
        foreach (var lesson in dto.Lessons ?? [])
            group.Lessons.Add(NewLesson(lesson));

        db.LessonGroups.Add(group);
        await db.SaveChangesAsync();
        return LessonGroupDto.From(group);
    }

    /// <summary>
    /// Atualiza nome/sigla. Se a lista de aulas vier no corpo, ela é sincronizada
    /// (aulas com id existente são atualizadas, as demais criadas e as ausentes removidas).
    /// </summary>
    public async Task<LessonGroupDto> UpdateAsync(LessonGroupType type, int id, LessonGroupDto dto)
    {
        Validate(dto);
        var group = await FindAsync(type, id);
        if (dto.Value.Trim() != group.Value)
            await EnsureValueIsFreeAsync(type, dto.Value.Trim(), id);

        group.Name = dto.Name.Trim();
        group.Value = dto.Value.Trim();

        if (dto.Lessons is not null)
        {
            var keepIds = dto.Lessons.Where(l => l.Id.HasValue).Select(l => l.Id!.Value).ToHashSet();
            foreach (var removed in group.Lessons.Where(l => !keepIds.Contains(l.Id)).ToList())
                group.Lessons.Remove(removed);

            foreach (var lessonDto in dto.Lessons)
            {
                var existing = lessonDto.Id is int lessonId ? group.Lessons.FirstOrDefault(l => l.Id == lessonId) : null;
                if (existing is null)
                    group.Lessons.Add(NewLesson(lessonDto));
                else
                    ApplyLesson(existing, lessonDto);
            }
        }

        await db.SaveChangesAsync();
        return LessonGroupDto.From(group);
    }

    public async Task DeleteAsync(LessonGroupType type, int id)
    {
        var group = await FindAsync(type, id);
        db.LessonGroups.Remove(group);
        await db.SaveChangesAsync();
    }

    public async Task<LessonDto> AddLessonAsync(LessonGroupType type, int groupId, LessonDto dto)
    {
        var group = await FindAsync(type, groupId);
        var lesson = NewLesson(dto);
        group.Lessons.Add(lesson);
        await db.SaveChangesAsync();
        return LessonDto.From(lesson);
    }

    public async Task<LessonDto> UpdateLessonAsync(LessonGroupType type, int groupId, int lessonId, LessonDto dto)
    {
        var group = await FindAsync(type, groupId);
        var lesson = group.Lessons.FirstOrDefault(l => l.Id == lessonId)
                     ?? throw new NotFoundException("Aula não encontrada.");
        ApplyLesson(lesson, dto);
        await db.SaveChangesAsync();
        return LessonDto.From(lesson);
    }

    public async Task DeleteLessonAsync(LessonGroupType type, int groupId, int lessonId)
    {
        var group = await FindAsync(type, groupId);
        var lesson = group.Lessons.FirstOrDefault(l => l.Id == lessonId)
                     ?? throw new NotFoundException("Aula não encontrada.");
        group.Lessons.Remove(lesson);
        await db.SaveChangesAsync();
    }

    private static void Validate(LessonGroupDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Value))
            throw new AppException("Nome e sigla/número são obrigatórios.");
    }

    private async Task EnsureValueIsFreeAsync(LessonGroupType type, string value, int? ignoreId = null)
    {
        if (await db.LessonGroups.AnyAsync(g => g.Type == type && g.Value == value && g.Id != ignoreId))
            throw new ConflictException($"Já existe um registro com a sigla/número '{value}'.");
    }

    private static Lesson NewLesson(LessonDto dto)
    {
        var lesson = new Lesson();
        ApplyLesson(lesson, dto);
        return lesson;
    }

    private static void ApplyLesson(Lesson lesson, LessonDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new AppException("O nome da aula é obrigatório.");

        lesson.Name = dto.Name.Trim();
        lesson.Description = dto.Description;
        lesson.UpdateDate = dto.UpdateDate ?? DateTime.Now.ToString("dd-MM-yyyy");
    }

    private async Task<LessonGroup> FindAsync(LessonGroupType type, int id) =>
        await db.LessonGroups.Include(g => g.Lessons).FirstOrDefaultAsync(g => g.Id == id && g.Type == type)
        ?? throw new NotFoundException(type == LessonGroupType.Training ? "Treinamento não encontrado." : "Disciplina não encontrada.");
}
