
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using examasterbot.Models.Users;
using examasterbot.Sessions;
using examasterbot.Formatting;

namespace examasterbot.Tg;

public partial class TelegramBotService
{
    private async Task HandleCreatingGroup_Name(UserSession session, UserProfile user, Message message,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(message.Text))
        {
            await _bot.SendMessage(
                user.TelegramId,
                "Название группы не может быть пустым. Введите название:",
                cancellationToken: token);
            return;
        }

        var name = message.Text.Trim();
        var group = _logic.CreateGroup(user.TelegramId, name);

        session.State = SessionState.None;

        await _bot.SendMessage(
            user.TelegramId,
            $"Группа создана.\nId: {group.Id}\nНазвание: {group.Name}\nИнвайт-код: `{group.InviteCode}`\n" +
            "Передайте код студентам, чтобы они могли присоединиться (/joingroup).",
            ParseMode.Markdown,
            cancellationToken: token);
    }
    
    private async Task HandleJoiningGroup_InviteCode(UserSession session, UserProfile user, Message message,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(message.Text))
        {
            await _bot.SendMessage(
                user.TelegramId,
                "Инвайт-код не может быть пустым. Введите код:",
                cancellationToken: token);
            return;
        }

        var code = message.Text.Trim();
        var (success, error, group) = _logic.JoinGroupByInviteCode(user.TelegramId, code);
        session.State = SessionState.None;

        if (!success)
            await _bot.SendMessage(
                user.TelegramId,
                error,
                cancellationToken: token);
        else
            await _bot.SendMessage(
                user.TelegramId,
                $"Вы присоединились к группе *{group!.Name}* (Id: {group.Id}).",
                ParseMode.Markdown,
                cancellationToken: token);
    }

    private async Task HandleGroupInfoCommand(UserProfile user, string[] parts, CancellationToken token)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], out var groupId))
        {
            await _bot.SendMessage(
                user.TelegramId,
                "❗ Использование: /groupinfo <ID_группы>\nНапример: /groupinfo 1000",
                cancellationToken: token);
            return;
        }

        var userGroups = _logic.GetUserGroups(user.TelegramId);
        var inThisGroup = userGroups.Any(g => g.group.Id == groupId);
        if (!inThisGroup)
        {
            await _bot.SendMessage(
                chatId: user.TelegramId,
                text: "🚫 Вы не состоите в этой группе, поэтому не можете просматривать её состав.",
                cancellationToken: token);
            return;
        }
        
        var (success, error, group, teachers, students) = _logic.GetGroupMembers(groupId);
        if (!success)
        {
            await _bot.SendMessage(
                user.TelegramId,
                $"❗ {error}",
                cancellationToken: token);
            return;
        }

        

        await _bot.SendMessage(
            user.TelegramId,
            MessageFormatter.GroupInfo(group, teachers, students),
            cancellationToken: token);
    }

    private async Task HandleMyGroups(UserProfile user, CancellationToken token)
    {
        var groups = _logic.GetUserGroups(user.TelegramId);
        if (!groups.Any())
        {
            await _bot.SendMessage(
                user.TelegramId,
                "Вы пока не состоите ни в одной группе.",
                cancellationToken: token);
            return;
        }

        var lines = groups.Select(g =>
            $"[{g.group.Id}] {g.group.Name} – {g.role}, код: `{g.group.InviteCode}`");
        var text = "Ваши группы:\n" + string.Join("\n", lines);
        await _bot.SendMessage(
            user.TelegramId,
            text,
            ParseMode.Markdown,
            cancellationToken: token);
    }
    
    private async Task HandleAddTeacherCommand(string[] parts, UserProfile user, CancellationToken token)
    {
        if (parts.Length < 3)
        {
            await _bot.SendMessage(
                user.TelegramId,
                "❗ Использование: /addteacher <groupId> <userTelegramId>",
                cancellationToken: token);
            return;
        }

        if (!int.TryParse(parts[1], out var groupId)
            || !long.TryParse(parts[2], out var teacherId))
        {
            await _bot.SendMessage(
                user.TelegramId,
                "Неверные аргументы. groupId и userTelegramId должны быть числами.",
                cancellationToken: token);
            return;
        }

        var (success, error) = _logic.AddTeacherToGroup(user.TelegramId, groupId, teacherId);
        if (!success)
            await _bot.SendMessage(
                user.TelegramId,
                error,
                cancellationToken: token);
        else
            await _bot.SendMessage(
                user.TelegramId,
                "Преподаватель добавлен.",
                cancellationToken: token);
    }
    
    private async Task HandleNewInviteCodeCommand(
        UserProfile user,
        string[] parts,
        CancellationToken token)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], out var groupId))
        {
            await _bot.SendMessage(
                chatId: user.TelegramId,
                text: "❗ Использование: /newcode <ID_группы>",
                cancellationToken: token);
            return;
        }

        var (success, error, group) = _logic.RegenerateGroupInviteCode(user.TelegramId, groupId);
        if (!success || group == null)
        {
            await _bot.SendMessage(
                chatId: user.TelegramId,
                text: $"❗ {error}",
                cancellationToken: token);
            return;
        }

        await _bot.SendMessage(
            chatId: user.TelegramId,
            text:
            $"🔑 Новый код приглашения для группы \"{group.Name}\" (ID: {group.Id}):\n" +
            $"{group.InviteCode}\n\n" +
            $"Старый код теперь не работает.",
            cancellationToken: token);
    }
}