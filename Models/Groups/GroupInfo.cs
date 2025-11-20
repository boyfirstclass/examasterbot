
namespace examasterbot.Models.Groups;

public class GroupInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public long OwnerTelegramId { get; set; }
    public string InviteCode { get; set; } = "";
}