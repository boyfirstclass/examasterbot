
namespace examasterbot.Models.Assignments;

public class Submission
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public long StudentTelegramId { get; set; }
    public int VariantNumber { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string AnswerText { get; set; } = "";
    public string AnswerFileId { get; set; } = "";
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;
    public int? Grade { get; set; }
    public string TeacherComment { get; set; } = "";
    public long? CheckedByTeacherId { get; set; }
    public DateTime? CheckedAt { get; set; }
    public long? LockedByTeacherId { get; set; }
    public DateTime? LockedAt { get; set; }
}