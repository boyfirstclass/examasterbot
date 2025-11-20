
using examasterbot.Logic;

namespace examasterbot.Sessions;

public class UserSession
{
    public long TelegramUserId { get; set; }
    public SessionState State { get; set; } = SessionState.None;

    public int? TempGroupId { get; set; }
    public BotLogic.AssignmentDraft? TempAssignmentDraft { get; set; }
    public int? TempAssignmentId { get; set; }
    public int? TempSubmissionId { get; set; }
    public int? TempGrade { get; set; }

    public string? TempFirstName { get; set; }
        
    public int? TempCurrentVariantNumber { get; set; }
}