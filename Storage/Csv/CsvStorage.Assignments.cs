
using System.Globalization;
using examasterbot.Models.Assignments;

namespace examasterbot.Storage.Csv;

public partial class CsvStorage
{
    private string AssignmentsFile => Path.Combine(_basePath, "assignments.csv");
    private string VariantsFile => Path.Combine(_basePath, "assignment_variants.csv");
    private void LoadAssignments()
        {
            Assignments.Clear();
            foreach (var cols in ReadCsv(AssignmentsFile, 8))
            {
                Assignments.Add(new Assignment
                {
                    Id = int.Parse(cols[0]),
                    GroupId = int.Parse(cols[1]),
                    Title = Unescape(cols[2]),
                    Description = Unescape(cols[3]),
                    Deadline = DateTime.Parse(cols[4], null, DateTimeStyles.RoundtripKind),
                    VariantCount = int.Parse(cols[5]),
                    AssignmentFileId = Unescape(cols[6]),
                    CreatedByTeacherId = long.Parse(cols[7]),
                    IsClosed = cols.Length > 8 && bool.Parse(cols[8])
                });
            }
        }

        private void SaveAssignments()
        {
            var rows = Assignments.Select(a => new[]
            {
                a.Id.ToString(),
                a.GroupId.ToString(),
                Escape(a.Title),
                Escape(a.Description),
                a.Deadline.ToString("o"),
                a.VariantCount.ToString(),
                Escape(a.AssignmentFileId),
                a.CreatedByTeacherId.ToString(),
                a.IsClosed.ToString()
            });
            WriteCsv(AssignmentsFile, rows);
        }

        private void LoadAssignmentVariants()
        {
            AssignmentVariants.Clear();
            foreach (var cols in ReadCsv(VariantsFile, 3))
            {
                AssignmentVariants.Add(new AssignmentVariant
                {
                    AssignmentId = int.Parse(cols[0]),
                    StudentTelegramId = long.Parse(cols[1]),
                    VariantNumber = int.Parse(cols[2])
                });
            }
        }

        private void SaveAssignmentVariants()
        {
            var rows = AssignmentVariants.Select(v => new[]
            {
                v.AssignmentId.ToString(),
                v.StudentTelegramId.ToString(),
                v.VariantNumber.ToString()
            });
            WriteCsv(VariantsFile, rows);
        }
}