
using examasterbot.Models.Groups;

namespace examasterbot.Storage.Csv;

public partial class CsvStorage
{
    private string GroupsFile => Path.Combine(_basePath, "groups.csv");
    private string TeachersFile => Path.Combine(_basePath, "group_teachers.csv");
    private string StudentsFile => Path.Combine(_basePath, "group_students.csv");
    private void LoadGroups()
        {
            Groups.Clear();
            foreach (var cols in ReadCsv(GroupsFile, 4))
            {
                Groups.Add(new GroupInfo
                {
                    Id = int.Parse(cols[0]),
                    Name = Unescape(cols[1]),
                    OwnerTelegramId = long.Parse(cols[2]),
                    InviteCode = Unescape(cols[3])
                });
            }
        }

        private void SaveGroups()
        {
            var rows = Groups.Select(g => new[]
            {
                g.Id.ToString(),
                Escape(g.Name),
                g.OwnerTelegramId.ToString(),
                Escape(g.InviteCode)
            });
            WriteCsv(GroupsFile, rows);
        }

        private void LoadGroupTeachers()
        {
            GroupTeachers.Clear();
            foreach (var cols in ReadCsv(TeachersFile, 2))
            {
                GroupTeachers.Add(new GroupTeacher
                {
                    GroupId = int.Parse(cols[0]),
                    TeacherTelegramId = long.Parse(cols[1])
                });
            }
        }

        private void SaveGroupTeachers()
        {
            var rows = GroupTeachers.Select(t => new[]
            {
                t.GroupId.ToString(),
                t.TeacherTelegramId.ToString()
            });
            WriteCsv(TeachersFile, rows);
        }

        private void LoadGroupStudents()
        {
            GroupStudents.Clear();
            foreach (var cols in ReadCsv(StudentsFile, 2))
            {
                GroupStudents.Add(new GroupStudent
                {
                    GroupId = int.Parse(cols[0]),
                    StudentTelegramId = long.Parse(cols[1])
                });
            }
        }

        private void SaveGroupStudents()
        {
            var rows = GroupStudents.Select(s => new[]
            {
                s.GroupId.ToString(),
                s.StudentTelegramId.ToString()
            });
            WriteCsv(StudentsFile, rows);
        }
}