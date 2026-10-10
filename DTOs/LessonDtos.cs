using LinchpinBackend.Models;

namespace LinchpinBackend.DTOs;

public class LessonDto
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? UpdateDate { get; set; }

    public static LessonDto From(Lesson l) => new()
    {
        Id = l.Id,
        Name = l.Name,
        Description = l.Description,
        UpdateDate = l.UpdateDate
    };
}

/// <summary>Treinamento ou Disciplina com suas aulas.</summary>
public class LessonGroupDto
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public List<LessonDto>? Lessons { get; set; }

    public static LessonGroupDto From(LessonGroup g) => new()
    {
        Id = g.Id,
        Name = g.Name,
        Value = g.Value,
        Lessons = g.Lessons.OrderBy(l => l.Id).Select(LessonDto.From).ToList()
    };
}

public class CourseDto
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? LessonType { get; set; }
    public string? Description { get; set; }
    public string? UpdateDate { get; set; }

    public static CourseDto From(Course c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Label = c.Label,
        LessonType = c.LessonType,
        Description = c.Description,
        UpdateDate = c.UpdateDate
    };
}
