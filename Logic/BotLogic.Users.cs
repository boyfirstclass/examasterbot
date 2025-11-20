
using examasterbot.Models.Users;

namespace examasterbot.Logic;

public partial class BotLogic
{
    public UserProfile GetOrCreateUser(long telegramId, string username)
    {
        var user = _storage.Users.FirstOrDefault(u => u.TelegramId == telegramId);
        if (user == null)
        {
            user = new UserProfile
            {
                TelegramId = telegramId,
                Username = username ?? ""
            };
            _storage.Users.Add(user);
            _storage.SaveAll();
        }

        return user;
    }

    public void RegisterUser(long telegramId, string username, string firstName, string lastName)
    {
        var user = GetOrCreateUser(telegramId, username);
        user.FirstName = firstName;
        user.LastName = lastName;
        user.IsRegistered = true;
        _storage.SaveAll();
    }
}