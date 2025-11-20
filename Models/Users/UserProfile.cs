
namespace examasterbot.Models.Users;

public class UserProfile
{
    public long TelegramId { get; set; }
    public string Username { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsRegistered { get; set; }
}