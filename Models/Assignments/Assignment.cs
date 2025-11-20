
namespace examasterbot.Models.Assignments;

public class Assignment
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime Deadline { get; set; } 
    public int VariantCount { get; set; }
    public string AssignmentFileId { get; set; } = "";
    public long CreatedByTeacherId { get; set; }
    public bool IsClosed { get; set; }
}