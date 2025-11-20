
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using examasterbot.Models.Users;
using examasterbot.Sessions;

namespace examasterbot.Tg;

public partial class TelegramBotService
{
    private async Task HandleSubmitCommand(UserSession session, UserProfile user, string[] parts,
        CancellationToken token)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], out var assignmentId))
        {
            await _bot.SendMessage(
                user.TelegramId,
                "❗ Использование: /submit <assignmentId>",
                cancellationToken: token);
            return;
        }

        var assignment = _logic.GetAssignment(assignmentId);
        if (assignment == null)
        {
            await _bot.SendMessage(
                user.TelegramId,
                "Задание не найдено.",
                cancellationToken: token);
            return;
        }

        var variant = _logic.GetStudentVariantForAssignment(assignmentId, user.TelegramId);
        if (variant == null)
        {
            await _bot.SendMessage(
                user.TelegramId,
                "Вам не выдавали вариант по этому заданию (возможно, вы не студент этой группы).",
                cancellationToken: token);
            return;
        }

        session.State = SessionState.SubmittingSolution_WaitingForContent;
        session.TempAssignmentId = assignmentId;

        await _bot.SendMessage(
            user.TelegramId,
            $"Вы отправляете решение по заданию *{assignment.Title}* (ID: {assignment.Id}), ваш вариант: *{variant}*.\n" +
            "Пришлите текст решения или документ (файл). " +
            "Если есть и то и другое, отправьте либо текст, либо файл с комментарием / пояснением в подписи.",
            ParseMode.Markdown,
            cancellationToken: token);
    }
    
    private async Task HandleCreatingAssignment_File(
        UserSession session,
        UserProfile user,
        Message message,
        CancellationToken token)
    {
        var draft = session.TempAssignmentDraft!;
        string? fileId = null;

        if (message.Document != null)
        {
            fileId = message.Document.FileId;
        }
        else if (!string.IsNullOrWhiteSpace(message.Text) &&
                 message.Text.Trim() == "-")
        {
            fileId = null; 
        }
        else
        {
            await _bot.SendMessage(
                chatId: user.TelegramId,
                text:
                "❗ Пришлите либо документ с условиями, либо символ '-' если хотите пропустить файл.",
                cancellationToken: token);
            return;
        }

        draft.AssignmentFileId = fileId ?? "";

        await _bot.SendMessage(
            chatId: user.TelegramId,
            text:
            "🔢 Сколько вариантов заданий будет?\n" +
            "Введите целое число от 1 до 100.",
            cancellationToken: token);

        session.State = SessionState.CreatingAssignment_VariantCount;
    }
    
    private async Task HandleSubmittingSolutionContent(
        UserSession session,
        UserProfile user,
        Message message,
        CancellationToken token)
    {
        var assignmentId = session.TempAssignmentId;
        if (assignmentId == null)
        {
            session.State = SessionState.None;
            await _bot.SendMessage(
                user.TelegramId,
                "Неизвестное задание. Начните заново: /submit <assignmentId>.",
                cancellationToken: token);
            return;
        }

        var text = "";
        var fileId = "";

        if (message.Document != null)
        {
            fileId = message.Document.FileId;
            text = message.Caption ?? "";
        }
        else if (!string.IsNullOrWhiteSpace(message.Text))
        {
            text = message.Text;
        }
        else
        {
            await _bot.SendMessage(
                user.TelegramId,
                "Пришлите либо текст, либо документ (файл) с решением.",
                cancellationToken: token);
            return;
        }

        var (success, error, submission) = _logic.AddSubmission(assignmentId.Value, user.TelegramId, text, fileId);
        session.State = SessionState.None;
        session.TempAssignmentId = null;

        if (!success)
            await _bot.SendMessage(
                user.TelegramId,
                error,
                cancellationToken: token);
        else
            await _bot.SendMessage(
                user.TelegramId,
                $"Решение отправлено. Номер посылки: {submission!.Id}. " +
                "Ожидайте проверки преподавателем.",
                cancellationToken: token);
    }

}