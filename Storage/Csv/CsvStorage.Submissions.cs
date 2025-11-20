
using System.Globalization;
using examasterbot.Models.Assignments;

namespace examasterbot.Storage.Csv;

public partial class CsvStorage
{
    private string SubmissionsFile => Path.Combine(_basePath, "submissions.csv");
    private void LoadSubmissions()
        {
            Submissions.Clear();
            foreach (var cols in ReadCsv(SubmissionsFile, 14))
            {
                Submissions.Add(new Submission
                {
                    Id = int.Parse(cols[0]),
                    AssignmentId = int.Parse(cols[1]),
                    StudentTelegramId = long.Parse(cols[2]),
                    VariantNumber = int.Parse(cols[3]),
                    SubmittedAt = DateTime.Parse(cols[4], null, DateTimeStyles.RoundtripKind),
                    AnswerText = Unescape(cols[5]),
                    AnswerFileId = Unescape(cols[6]),
                    Status = Enum.Parse<SubmissionStatus>(cols[7]),
                    Grade = string.IsNullOrEmpty(cols[8]) ? null : int.Parse(cols[8]),
                    TeacherComment = Unescape(cols[9]),
                    CheckedByTeacherId = string.IsNullOrEmpty(cols[10]) ? null : long.Parse(cols[10]),
                    CheckedAt = string.IsNullOrEmpty(cols[11])
                        ? null
                        : DateTime.Parse(cols[11], null, DateTimeStyles.RoundtripKind),
                    LockedByTeacherId = string.IsNullOrEmpty(cols[12]) ? null : long.Parse(cols[12]),
                    LockedAt = string.IsNullOrEmpty(cols[13])
                        ? null
                        : DateTime.Parse(cols[13], null, DateTimeStyles.RoundtripKind)
                });
            }
        }

        private void SaveSubmissions()
        {
            var rows = Submissions.Select(s => new[]
            {
                s.Id.ToString(),
                s.AssignmentId.ToString(),
                s.StudentTelegramId.ToString(),
                s.VariantNumber.ToString(),
                s.SubmittedAt.ToString("o"),
                Escape(s.AnswerText),
                Escape(s.AnswerFileId),
                s.Status.ToString(),
                s.Grade?.ToString() ?? "",
                Escape(s.TeacherComment),
                s.CheckedByTeacherId?.ToString() ?? "",
                s.CheckedAt?.ToString("o") ?? "",
                s.LockedByTeacherId?.ToString() ?? "",
                s.LockedAt?.ToString("o") ?? ""
            });
            WriteCsv(SubmissionsFile, rows);
        }
}