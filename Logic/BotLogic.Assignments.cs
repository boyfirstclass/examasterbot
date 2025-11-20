
using examasterbot.Formatting;
using examasterbot.Models.Users;
using examasterbot.Models.Assignments;

namespace examasterbot.Logic;

public partial class BotLogic
{
    public (bool success, string error, Assignment? assignment, List<(UserProfile student, int variant)>? variants)
            CreateAssignment(AssignmentDraft draft)
        {
            var minDuration = TimeSpan.FromMinutes(5);
            var maxDuration = TimeSpan.FromDays(31);

            if (draft.Duration < minDuration || draft.Duration > maxDuration)
                return (false,
                    MessageFormatter.SubmitTimeTask(),
                    null, null);

            var group = _storage.Groups.FirstOrDefault(g => g.Id == draft.GroupId);
            if (group == null) return (false, "Группа не найдена.", null, null);

            var students = GetGroupStudents(group.Id);
            if (!students.Any())
                return (false, "В группе нет студентов, которым можно выдать задание.", null, null);

            var newId = _storage.Assignments.Any() ? _storage.Assignments.Max(a => a.Id) + 1 : 1;

            var now = DateTime.UtcNow.AddHours(3);
            var deadlineUtc = now.Add(draft.Duration);

            var assignment = new Assignment
            {
                Id = newId,
                GroupId = draft.GroupId,
                Title = draft.Title,
                Description = draft.Description,
                Deadline = deadlineUtc,
                VariantCount = draft.VariantCount,
                AssignmentFileId = draft.AssignmentFileId,
                CreatedByTeacherId = draft.CreatedByTeacherId,
                IsClosed = false
            };

            _storage.Assignments.Add(assignment);

            var sortedStudents = students.OrderBy(s => s.TelegramId).ToList();
            var variantsList = new List<(UserProfile student, int variant)>();

            for (int i = 0; i < sortedStudents.Count; i++)
            {
                int variant = (i % draft.VariantCount) + 1;
                var student = sortedStudents[i];

                _storage.AssignmentVariants.Add(new AssignmentVariant
                {
                    AssignmentId = assignment.Id,
                    StudentTelegramId = student.TelegramId,
                    VariantNumber = variant
                });

                variantsList.Add((student, variant));
            }

            _storage.SaveAll();
            return (true, "", assignment, variantsList);
        }

        public Assignment? GetAssignment(int assignmentId)
        {
            return _storage.Assignments.FirstOrDefault(a => a.Id == assignmentId);
        }

        public int? GetStudentVariantForAssignment(int assignmentId, long studentId)
        {
            return _storage.AssignmentVariants
                .FirstOrDefault(v => v.AssignmentId == assignmentId && v.StudentTelegramId == studentId)
                ?.VariantNumber;
        }
}