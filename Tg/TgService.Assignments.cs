
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using examasterbot.Logic;
using examasterbot.Models.Users;
using examasterbot.Sessions;
using examasterbot.Formatting;

namespace examasterbot.Tg;

public partial class TelegramBotService
{
    private async Task HandleCreatingAssignmentFlow(
        UserSession session,
        UserProfile user,
        Message message,
        CancellationToken token)
    {
        var draft = session.TempAssignmentDraft;
        if (draft == null)
        {
            session.State = SessionState.None;
            await _bot.SendMessage(
                user.TelegramId,
                MessageFormatter.InternalError(),
                cancellationToken: token);
            return;
        }

        switch (session.State)
        {
            case SessionState.CreatingAssignment_GroupId:
                await CreatingAssignment_GroupId(session, user, message, draft, token);
                break;

            case SessionState.CreatingAssignment_Title:
                await CreatingAssignment_Title(session, user, message, draft, token);
                break;

            case SessionState.CreatingAssignment_Description:
                await HandleCreatingAssignment_Description(session, user, message, token);
                break;

            case SessionState.CreatingAssignment_File:
                await HandleCreatingAssignment_File(session, user, message, token);
                break;

            case SessionState.CreatingAssignment_VariantCount:
                await HandleCreatingAssignment_VariantCount(session, user, message, token);
                break;

            case SessionState.CreatingAssignment_VariantTask:
                await HandleCreatingAssignment_VariantTask(session, user, message, token);
                break;

            case SessionState.CreatingAssignment_Deadline:
                await HandleCreatingAssignment_Duration(session, user, message, token);
                break;
        }
    }

    private async Task HandleNewTaskCommand(UserSession session, UserProfile user, CancellationToken token)
    {
        var groups = _logic.GetUserGroups(user.TelegramId)
            .Where(g => g.role.Contains("преподаватель"))
            .ToList();

        if (!groups.Any())
        {
            await _bot.SendMessage(
                user.TelegramId,
                MessageFormatter.NewTaskNoGroups(),
                cancellationToken: token);
            return;
        }

        session.TempAssignmentDraft = new BotLogic.AssignmentDraft
        {
            CreatedByTeacherId = user.TelegramId
        };
        session.State = SessionState.CreatingAssignment_GroupId;

        await _bot.SendMessage(
            user.TelegramId,
            MessageFormatter.NewTaskChooseGroup(groups),
            cancellationToken: token);
    }

    private async Task HandleCreatingAssignment_VariantCount(
        UserSession session,
        UserProfile user,
        Message message,
        CancellationToken token)
    {
        var draft = session.TempAssignmentDraft!;
        if (string.IsNullOrWhiteSpace(message.Text) ||
            !int.TryParse(message.Text.Trim(), out var variantCount))
        {
            await _bot.SendMessage(
                chatId: user.TelegramId,
                MessageFormatter.NewTaskAskVariantCount(),
                cancellationToken: token);
            return;
        }

        if (variantCount < 1 || variantCount > 100)
        {
            await _bot.SendMessage(
                chatId: user.TelegramId,
                text: MessageFormatter.VariantCount(),
                cancellationToken: token);
            return;
        }

        draft.VariantCount = variantCount;
        draft.VariantTasks.Clear();

        session.TempCurrentVariantNumber = 1;
        session.State = SessionState.CreatingAssignment_VariantTask;

        await _bot.SendMessage(
            chatId: user.TelegramId,
            text: MessageFormatter.NewTaskAskVariantTask(1),
            parseMode: ParseMode.Html,
            cancellationToken: token);
    }

    private async Task HandleCreatingAssignment_VariantTask(
        UserSession session,
        UserProfile user,
        Message message,
        CancellationToken token)
    {
        var draft = session.TempAssignmentDraft!;
        var currentVariant = session.TempCurrentVariantNumber;

        if (currentVariant == null)
        {
            session.State = SessionState.None;
            await _bot.SendMessage(
                chatId: user.TelegramId,
                text: MessageFormatter.InternalError(),
                cancellationToken: token);
            return;
        }

        string text = "";
        string fileId = "";

        if (message.Document != null)
        {
            fileId = message.Document.FileId;
            text = message.Caption?.Trim() ?? "";
        }
        else if (!string.IsNullOrWhiteSpace(message.Text))
        {
            text = message.Text.Trim();
        }
        else
        {
            await _bot.SendMessage(
                chatId: user.TelegramId,
                text:
                "❗ Пришлите текст, документ или документ с подписью для задания этого варианта.",
                cancellationToken: token);
            return;
        }

        draft.VariantTasks.Add(new BotLogic.VariantTaskDraft
        {
            VariantNumber = currentVariant.Value,
            TaskText = text,
            TaskFileId = fileId
        });

        if (currentVariant < draft.VariantCount)
        {
            session.TempCurrentVariantNumber = currentVariant.Value + 1;
            session.State = SessionState.CreatingAssignment_VariantTask;
            await _bot.SendMessage(
                chatId: user.TelegramId,
                text: MessageFormatter.NewTaskAskVariantTask(currentVariant.Value + 1),
                parseMode: ParseMode.Html,
                cancellationToken: token);
            return;
        }

        // Все варианты собраны – спрашиваем длительность
        session.TempCurrentVariantNumber = null;
        session.State = SessionState.CreatingAssignment_Deadline;

        await _bot.SendMessage(
            chatId: user.TelegramId,
            text: MessageFormatter.NewTaskAskDuration(),
            parseMode: ParseMode.Html,
            cancellationToken: token);
    }

    private async Task CreatingAssignment_GroupId(
        UserSession session,
        UserProfile user,
        Message message,
        BotLogic.AssignmentDraft draft,
        CancellationToken token)
    {
        if (!int.TryParse(message.Text, out var groupId))
        {
            await _bot.SendMessage(
                user.TelegramId,
                "Неверный формат. Введите Id группы (число).",
                cancellationToken: token);
            return;
        }

        draft.GroupId = groupId;
        session.State = SessionState.CreatingAssignment_Title;

        await _bot.SendMessage(
            user.TelegramId,
            "Введите *название* задания:",
            ParseMode.Markdown,
            cancellationToken: token);
    }

    private async Task CreatingAssignment_Title(
        UserSession session,
        UserProfile user,
        Message message,
        BotLogic.AssignmentDraft draft,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(message.Text))
        {
            await _bot.SendMessage(
                user.TelegramId,
                "Название не может быть пустым. Введите название:",
                cancellationToken: token);
            return;
        }

        draft.Title = message.Text.Trim();
        session.State = SessionState.CreatingAssignment_Description;

        await _bot.SendMessage(
            user.TelegramId,
            MessageFormatter.NewTaskAskDescription(),
            cancellationToken: token);
    }

    private async Task CreatingAssignment_Description(
        UserSession session,
        UserProfile user,
        Message message,
        BotLogic.AssignmentDraft draft,
        CancellationToken token)
    {
        draft.Description = message.Text?.Trim() ?? "";
        session.State = SessionState.CreatingAssignment_VariantCount;

        await _bot.SendMessage(
            user.TelegramId,
            MessageFormatter.NewTaskAskVariantCount(),
            ParseMode.Markdown,
            cancellationToken: token);
    }

    private async Task HandleCreatingAssignment_Duration(
        UserSession session,
        UserProfile user,
        Message message,
        CancellationToken token)
    {
        var draft = session.TempAssignmentDraft!;
        if (string.IsNullOrWhiteSpace(message.Text))
        {
            await _bot.SendMessage(
                chatId: user.TelegramId,
                text: MessageFormatter.NewTaskAskDuration(),
                parseMode: ParseMode.Html,
                cancellationToken: token);
            return;
        }

        var parts = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3 ||
            !int.TryParse(parts[0], out var days) ||
            !int.TryParse(parts[1], out var hours) ||
            !int.TryParse(parts[2], out var minutes))
        {
            await _bot.SendMessage(
                chatId: user.TelegramId,
                text: MessageFormatter.NewTaskAskDuration(),
                parseMode: ParseMode.Html,
                cancellationToken: token);
            return;
        }

        if (days < 0 || hours < 0 || minutes < 0)
        {
            await _bot.SendMessage(
                chatId: user.TelegramId,
                text: "❗ Значения не могут быть отрицательными.",
                cancellationToken: token);
            return;
        }

        var duration = TimeSpan.FromDays(days) + TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes);

        var min = TimeSpan.FromMinutes(5);
        var max = TimeSpan.FromDays(31);
        if (duration < min || duration > max)
        {
            await _bot.SendMessage(
                chatId: user.TelegramId,
                text: "❗ Длительность должна быть не менее 5 минут и не более 31 дня.",
                cancellationToken: token);
            return;
        }

        draft.Duration = duration;

        var (success, error, assignment, variants) = _logic.CreateAssignment(draft);

        session.State = SessionState.None;
        session.TempAssignmentDraft = null;

        if (!success || assignment == null || variants == null)
        {
            await _bot.SendMessage(
                chatId: user.TelegramId,
                text: $"❗ Ошибка при создании задания: {error}",
                cancellationToken: token);
            return;
        }

        var durText = $"{days} д. {hours} ч. {minutes} мин.";

        await _bot.SendMessage(
            chatId: user.TelegramId,
            text: MessageFormatter.AssignmentCreatedForTeacher(assignment, duration),
            cancellationToken: token);

        foreach (var (student, variant) in variants)
        {
            var vt = draft.VariantTasks.FirstOrDefault(v => v.VariantNumber == variant);
            var variantText = vt?.TaskText ?? "";
            var variantFileId = vt?.TaskFileId ?? "";

            await _bot.SendMessage(
                chatId: student.TelegramId,
                text: MessageFormatter.AssignmentNotificationForStudent(assignment, durText, variant, draft.Description,
                    variantText),
                cancellationToken: token);

            if (!string.IsNullOrEmpty(variantFileId))
            {
                await _bot.SendDocument(
                    chatId: student.TelegramId,
                    document: InputFile.FromFileId(variantFileId),
                    caption: "📎 Файл задания вашего варианта",
                    cancellationToken: token);
            }

            else if (!string.IsNullOrEmpty(assignment.AssignmentFileId))
            {
                await _bot.SendDocument(
                    chatId: student.TelegramId,
                    document: InputFile.FromFileId(assignment.AssignmentFileId),
                    caption: "📎 Общий файл с условиями контрольной",
                    cancellationToken: token);
            }
        }
    }

    private async Task CreatingAssignment_VariantCount(
        UserSession session,
        UserProfile user,
        Message message,
        BotLogic.AssignmentDraft draft,
        CancellationToken token)
    {
        if (!int.TryParse(message.Text, out var variants) || variants < 1 || variants > 100)
        {
            await _bot.SendMessage(
                user.TelegramId,
                "Неверное число вариантов. Введите число от 1 до 100:",
                cancellationToken: token);
            return;
        }

        draft.VariantCount = variants;
        session.State = SessionState.CreatingAssignment_Deadline;
    }

    private async Task HandleExtendDeadlineCommand(
        UserProfile user,
        string[] parts,
        CancellationToken token)
    {
        if (parts.Length < 5 ||
            !int.TryParse(parts[1], out var assignmentId) ||
            !int.TryParse(parts[2], out var days) ||
            !int.TryParse(parts[3], out var hours) ||
            !int.TryParse(parts[4], out var minutes))
        {
            await _bot.SendMessage(
                user.TelegramId,
                MessageFormatter.ExtendUsage(),
                cancellationToken: token);
            return;
        }

        if (days < 0 || hours < 0 || minutes < 0)
        {
            await _bot.SendMessage(
                user.TelegramId,
                "❗ Значения не могут быть отрицательными.",
                cancellationToken: token);
            return;
        }

        var extension = TimeSpan.FromDays(days) + TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes);
        if (extension <= TimeSpan.Zero)
        {
            await _bot.SendMessage(
                user.TelegramId,
                "❗ Продление должно быть больше нуля.",
                cancellationToken: token);
            return;
        }

        var (success, error, assignment) = _logic.ExtendAssignmentDeadline(assignmentId, user.TelegramId, extension);
        if (!success || assignment == null)
        {
            await _bot.SendMessage(
                user.TelegramId,
                $"❗ {error}",
                cancellationToken: token);
            return;
        }

        var extText = $"{days} д. {hours} ч. {minutes} мин.";

        await _bot.SendMessage(
            user.TelegramId,
            MessageFormatter.ExtendTeacherNotification(assignment, extText),
            cancellationToken: token);

        var students = _logic.GetGroupStudents(assignment.GroupId);
        foreach (var s in students)
            await _bot.SendMessage(
                s.TelegramId,
                MessageFormatter.ExtendStudentNotification(assignment),
                cancellationToken: token);
    }

    private async Task HandleCreatingAssignment_Description(
        UserSession session,
        UserProfile user,
        Message message,
        CancellationToken token)
    {
        var draft = session.TempAssignmentDraft!;

        draft.Description = message.Text?.Trim() ?? "";

        await _bot.SendMessage(
            chatId: user.TelegramId,
            text:
            "📎 Если хотите, пришлите <b>общий файл</b> с условиями контрольной (документ).\n\n" +
            "Если общий файл не нужен — отправьте сообщение с символом <code>-</code>.",
            cancellationToken: token);

        session.State = SessionState.CreatingAssignment_File;
    }
}