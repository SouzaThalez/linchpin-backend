namespace LinchpinBackend.Models;

public class Course
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string LessonType { get; set; } = "curso";
    public string? Description { get; set; }
    public string? UpdateDate { get; set; }
}
