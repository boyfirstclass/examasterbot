
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using examasterbot.Models.Users;
using examasterbot.Sessions;

namespace examasterbot.Tg;

public partial class TelegramBotService
{
    private async Task HandleRegistrationFlow(UserSession session, UserProfile user, Message message,
        CancellationToken token)
    {
        var userId = session.TelegramUserId;

        if (session.State == SessionState.None)
        {
            session.State = SessionState.AwaitingFirstName;
            await _bot.SendMessage(
                userId,
                "Привет! Давай зарегистрируемся.\nВведите ваше *имя*:",
                ParseMode.Markdown,
                cancellationToken: token);
            return;
        }

        if (session.State == SessionState.AwaitingFirstName)
        {
            if (string.IsNullOrWhiteSpace(message.Text))
            {
                await _bot.SendMessage(
                    userId,
                    "Имя не должно быть пустым. Введите имя:",
                    cancellationToken: token);
                return;
            }

            session.TempFirstName = message.Text.Trim();
            session.State = SessionState.AwaitingLastName;
            await _bot.SendMessage(
                userId,
                "Отлично. Теперь введите *фамилию*:",
                ParseMode.Markdown,
                cancellationToken: token);
            return;
        }

        if (session.State == SessionState.AwaitingLastName)
        {
            if (string.IsNullOrWhiteSpace(message.Text))
            {
                await _bot.SendMessage(
                    userId,
                    "Фамилия не должна быть пустой. Введите фамилию:",
                    cancellationToken: token);
                return;
            }

            var lastName = message.Text.Trim();
            var firstName = session.TempFirstName ?? "";

            _logic.RegisterUser(user.TelegramId, user.Username, firstName, lastName);
            session.State = SessionState.None;
            session.TempFirstName = null;

            await _bot.SendMessage(
                userId,
                $"Готово! Вы зарегистрированы как *{firstName} {lastName}*.\n" +
                "Посмотрите /help для списка команд.",
                ParseMode.Markdown,
                cancellationToken: token);
        }
    }
    
    private bool IsRegistrationCommand(Message message)
    {
        return message.Text != null &&
               (message.Text.StartsWith("/start") || message.Text.StartsWith("/register"));
    }
}