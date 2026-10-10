namespace LinchpinBackend.Models;

public class Lesson
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? UpdateDate { get; set; }

    public int LessonGroupId { get; set; }
    public LessonGroup? LessonGroup { get; set; }
}
