
using examasterbot.Storage.Csv;

namespace examasterbot.Logic;

public partial class BotLogic
{
    private readonly CsvStorage _storage;

    public BotLogic(CsvStorage storage)
    {
        _storage = storage;
    }

    public class AssignmentDraft
    {
        public int GroupId { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public TimeSpan Duration { get; set; }
        public int VariantCount { get; set; }
        public string AssignmentFileId { get; set; } = "";
        public long CreatedByTeacherId { get; set; }

        public List<VariantTaskDraft> VariantTasks { get; } = new();
    }

    public class VariantTaskDraft
    {
        public int VariantNumber { get; set; }
        public string TaskText { get; set; } = "";
        public string TaskFileId { get; set; } = "";
    }
}