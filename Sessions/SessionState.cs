
namespace examasterbot.Sessions;

public enum SessionState
{
    None,
    AwaitingFirstName,
    AwaitingLastName,

    CreatingGroup_Name,

    JoiningGroup_InviteCode,

    CreatingAssignment_GroupId,
    CreatingAssignment_Title,
    CreatingAssignment_Description,
    CreatingAssignment_File,      
    CreatingAssignment_VariantCount,
    CreatingAssignment_VariantTask,
    CreatingAssignment_Deadline,

    SubmittingSolution_WaitingForContent,

    Grading_WaitingForGrade,
    Grading_WaitingForComment
}