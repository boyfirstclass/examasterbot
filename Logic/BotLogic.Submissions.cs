
using examasterbot.Formatting;
using examasterbot.Models.Assignments;
using examasterbot.Models.Groups;

namespace examasterbot.Logic;

public partial class BotLogic
{
    public (bool success, string error, Submission? submission) AddSubmission(
            int assignmentId,
            long studentId,
            string text,
            string fileId)
        {
            var assignment = GetAssignment(assignmentId);
            if (assignment == null)
                return (false, "Задание не найдено.", null);

            if (DateTime.UtcNow.AddHours(3) > assignment.Deadline)
                return (false, MessageFormatter.DeadlineIsOver(), null);

            var variant = GetStudentVariantForAssignment(assignmentId, studentId);
            if (variant == null)
                return (false, MessageFormatter.InternalError(), null);

            if (_storage.Submissions.Any(s => s.AssignmentId == assignmentId && s.StudentTelegramId == studentId))
                return (false, MessageFormatter.AlreadySubmitted(), null);

            var newId = _storage.Submissions.Any() ? _storage.Submissions.Max(s => s.Id) + 1 : 1;

            var submission = new Submission
            {
                Id = newId,
                AssignmentId = assignmentId,
                StudentTelegramId = studentId,
                VariantNumber = variant.Value,
                SubmittedAt = DateTime.UtcNow,
                AnswerText = text ?? "",
                AnswerFileId = fileId ?? "",
                Status = SubmissionStatus.Submitted
            };

            _storage.Submissions.Add(submission);
            _storage.SaveAll();

            return (true, "", submission);
        }

        public Submission? GetNextSubmissionForTeacher(int assignmentId, long teacherId)
        {
            var assignment = GetAssignment(assignmentId);
            if (assignment == null) return null;

            var isTeacher = _storage.GroupTeachers.Any(t =>
                t.GroupId == assignment.GroupId && t.TeacherTelegramId == teacherId);
            if (!isTeacher) return null;

            var locked = _storage.Submissions
                .Where(s => s.AssignmentId == assignmentId
                            && s.Status == SubmissionStatus.Submitted
                            && s.LockedByTeacherId == teacherId)
                .OrderBy(s => s.SubmittedAt)
                .FirstOrDefault();

            if (locked != null)
                return locked;

            var next = _storage.Submissions
                .Where(s => s.AssignmentId == assignmentId
                            && s.Status == SubmissionStatus.Submitted
                            && s.LockedByTeacherId == null)
                .OrderBy(s => s.SubmittedAt)
                .FirstOrDefault();

            if (next != null)
            {
                next.LockedByTeacherId = teacherId;
                next.LockedAt = DateTime.UtcNow;
                _storage.SaveAll();
            }

            return next;
        }
        
        public (bool success, string error, Assignment? assignment)
            ExtendAssignmentDeadline(int assignmentId, long teacherId, TimeSpan extension)
        {
            if (extension <= TimeSpan.Zero)
                return (false, "Продление должно быть положительным.", null);

            var assignment = _storage.Assignments.FirstOrDefault(a => a.Id == assignmentId);
            if (assignment == null)
                return (false, "Задание не найдено.", null);

            var isTeacher = _storage.GroupTeachers.Any(t =>
                t.GroupId == assignment.GroupId && t.TeacherTelegramId == teacherId);
            if (!isTeacher)
                return (false, "Вы не преподаватель этой группы.", null);

            assignment.Deadline = assignment.Deadline.Add(extension);
            _storage.SaveAll();

            return (true, "", assignment);
        }

        public (bool success, string error, Submission? submission) SetGradeAndComment(
            int submissionId,
            long teacherId,
            int grade,
            string comment)
        {
            var submission = _storage.Submissions.FirstOrDefault(s => s.Id == submissionId);
            if (submission == null) return (false, "Решение не найдено.", null);

            var assignment = GetAssignment(submission.AssignmentId);
            if (assignment == null) return (false, "Задание не найдено.", null);

            var isTeacher = _storage.GroupTeachers.Any(t =>
                t.GroupId == assignment.GroupId && t.TeacherTelegramId == teacherId);
            if (!isTeacher) return (false, "Вы не преподаватель этой группы.", null);

            if (submission.LockedByTeacherId != teacherId)
                return (false, "Это решение закреплено за другим преподавателем.", null);

            submission.Grade = grade;
            submission.TeacherComment = comment ?? "";
            submission.Status = SubmissionStatus.Checked;
            submission.CheckedByTeacherId = teacherId;
            submission.CheckedAt = DateTime.UtcNow;
            submission.LockedByTeacherId = null;
            submission.LockedAt = null;

            _storage.SaveAll();
            return (true, "", submission);
        }
        
        public (bool success, string error, GroupInfo? group)
            RegenerateGroupInviteCode(long ownerId, int groupId)
        {
            var group = _storage.Groups.FirstOrDefault(g => g.Id == groupId);
            if (group == null)
                return (false, "Группа не найдена.", null);

            if (group.OwnerTelegramId != ownerId)
                return (false, "Только создатель группы может обновлять код приглашения.", null);

            group.InviteCode = Guid.NewGuid().ToString("N")[..8];
            _storage.SaveAll();

            return (true, "", group);
        }
}