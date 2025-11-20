
using examasterbot.Models.Users;
using examasterbot.Models.Groups;
using examasterbot.Models.Assignments;

namespace examasterbot.Storage.Csv;

public partial class CsvStorage
{
    private readonly string _basePath;
    private readonly object _lockObj = new();

    public List<UserProfile> Users { get; } = new();
    public List<GroupInfo> Groups { get; } = new();
    public List<GroupTeacher> GroupTeachers { get; } = new();
    public List<GroupStudent> GroupStudents { get; } = new();
    public List<Assignment> Assignments { get; } = new();
    public List<AssignmentVariant> AssignmentVariants { get; } = new();
    public List<Submission> Submissions { get; } = new();
    
    public CsvStorage(string basePath)
    {
        _basePath = basePath;
        Directory.CreateDirectory(_basePath);
    }

    public void LoadAll()
    {
        lock (_lockObj)
        {
            LoadUsers();
            LoadGroups();
            LoadGroupTeachers();
            LoadGroupStudents();
            LoadAssignments();
            LoadAssignmentVariants();
            LoadSubmissions();
        }
    }

    public void SaveAll()
    {
        lock (_lockObj)
        {
            SaveUsers();
            SaveGroups();
            SaveGroupTeachers();
            SaveGroupStudents();
            SaveAssignments();
            SaveAssignmentVariants();
            SaveSubmissions();
        }
    }
    
    private static string Escape(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("|", "/");
    }

    private static string Unescape(string? s)
    {
        return s ?? "";
    }

    private static IEnumerable<string[]> ReadCsv(string path, int expectedColumns)
    {
        if (!File.Exists(path))
            yield break;

        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split('|');
            if (parts.Length < expectedColumns) continue;
            yield return parts;
        }
    }

    private static void WriteCsv(string path, IEnumerable<string[]> rows)
    {
        var lines = rows.Select(r => string.Join("|", r));
        File.WriteAllLines(path, lines);
    }
}