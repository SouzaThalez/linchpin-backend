namespace LinchpinBackend.Models;

public enum LessonGroupType
{
    Training = 1,
    Discipline = 2
}

/// <summary>Treinamento ou Disciplina: um agrupamento de aulas.</summary>
public class LessonGroup
{
    public int Id { get; set; }
    public LessonGroupType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public List<Lesson> Lessons { get; set; } = [];
}
