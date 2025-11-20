
using Telegram.Bot.Types.Enums;
using Telegram.Bot;

using examasterbot.Models.Users;
using examasterbot.Formatting;

namespace examasterbot.Tg;

public partial class TelegramBotService
{
    private async Task HandleHelp(UserProfile user, CancellationToken token)
    {
        var text = MessageFormatter.HelpText();

        await _bot.SendMessage(
            chatId: user.TelegramId,
            text: text,
            parseMode: ParseMode.Html,
            cancellationToken: token);
    }
}