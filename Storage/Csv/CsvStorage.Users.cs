
using examasterbot.Models.Users;

namespace examasterbot.Storage.Csv;

public partial class CsvStorage
{
    private string UsersFile => Path.Combine(_basePath, "users.csv");
    
    private void LoadUsers()
    {
        Users.Clear();
        foreach (var cols in ReadCsv(UsersFile, 5))
        {
            Users.Add(new UserProfile
            {
                TelegramId = long.Parse(cols[0]),
                Username = Unescape(cols[1]),
                FirstName = Unescape(cols[2]),
                LastName = Unescape(cols[3]),
                IsRegistered = bool.Parse(cols[4])
            });
        }
    }

    private void SaveUsers()
    {
        var rows = Users.Select(u => new[]
        {
            u.TelegramId.ToString(),
            Escape(u.Username),
            Escape(u.FirstName),
            Escape(u.LastName),
            u.IsRegistered.ToString()
        });
        WriteCsv(UsersFile, rows);
    }
}