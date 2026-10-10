using LinchpinBackend.Data;
using LinchpinBackend.DTOs;
using LinchpinBackend.Infrastructure;
using LinchpinBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace LinchpinBackend.Services;

/// <summary>Cursos (ex.: BLS, ACLS, ATLS).</summary>
public class CourseService(AppDbContext db)
{
    public async Task<List<CourseDto>> ListAsync()
    {
        var list = await db.Courses.AsNoTracking().OrderBy(c => c.Id).ToListAsync();
        return list.Select(CourseDto.From).ToList();
    }

    public async Task<CourseDto> GetAsync(int id) => CourseDto.From(await FindAsync(id));

    public async Task<CourseDto> CreateAsync(CourseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Label))
            throw new AppException("Nome e sigla do curso são obrigatórios.");

        var course = new Course
        {
            Name = dto.Name.Trim(),
            Label = dto.Label.Trim(),
            LessonType = string.IsNullOrWhiteSpace(dto.LessonType) ? "curso" : dto.LessonType,
            Description = dto.Description,
            UpdateDate = dto.UpdateDate
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync();
        return CourseDto.From(course);
    }

    /// <summary>Atualização parcial: campos não informados mantêm o valor atual.</summary>
    public async Task<CourseDto> UpdateAsync(int id, CourseDto dto)
    {
        var course = await FindAsync(id);
        if (!string.IsNullOrWhiteSpace(dto.Name)) course.Name = dto.Name.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Label)) course.Label = dto.Label.Trim();
        if (!string.IsNullOrWhiteSpace(dto.LessonType)) course.LessonType = dto.LessonType;
        if (dto.Description is not null) course.Description = dto.Description;
        if (dto.UpdateDate is not null) course.UpdateDate = dto.UpdateDate;

        await db.SaveChangesAsync();
        return CourseDto.From(course);
    }

    public async Task DeleteAsync(int id)
    {
        var course = await FindAsync(id);
        db.Courses.Remove(course);
        await db.SaveChangesAsync();
    }

    private async Task<Course> FindAsync(int id) =>
        await db.Courses.FindAsync(id) ?? throw new NotFoundException("Curso não encontrado.");
}
