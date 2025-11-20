
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using examasterbot.Models.Users;
using examasterbot.Sessions;
using examasterbot.Formatting;

namespace examasterbot.Tg;

public partial class TelegramBotService
{
    private async Task HandleStartCheckingCommand(
        UserSession session,
        UserProfile user,
        string[] parts,
        CancellationToken token)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], out var assignmentId))
        {
            await _bot.SendMessage(
                user.TelegramId,
                MessageFormatter.StartCheckingUsage(),
                cancellationToken: token);
            return;
        }

        var assignment = _logic.GetAssignment(assignmentId);
        if (assignment == null)
        {
            await _bot.SendMessage(
                user.TelegramId,
                "❗ Задание не найдено.",
                cancellationToken: token);
            return;
        }

        var next = _logic.GetNextSubmissionForTeacher(assignmentId, user.TelegramId);
        if (next == null)
        {
            await _bot.SendMessage(
                user.TelegramId,
                MessageFormatter.StartCheckingNoSubmissions(),
                cancellationToken: token);
            return;
        }

        session.TempAssignmentId = assignmentId;
        session.TempSubmissionId = next.Id;
        session.State = SessionState.Grading_WaitingForGrade;
        
        var student = _logic.GetOrCreateUser(next.StudentTelegramId, ""); 
        var name = $"{student.FirstName} {student.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(name)) name = student.Username != "" ? $"@{student.Username}" : "(без имени)";

        await _bot.SendMessage(
            user.TelegramId,
            MessageFormatter.CheckingShowSubmission(assignment, next, student),
            cancellationToken: token);

        if (!string.IsNullOrEmpty(next.AnswerFileId))
            await _bot.SendDocument(
                user.TelegramId,
                InputFile.FromFileId(next.AnswerFileId),
                "📎 Файл с решением студента",
                cancellationToken: token);

        await _bot.SendMessage(
            user.TelegramId,
            "✏️ Введите оценку (целое число), либо /cancel для отмены.",
            cancellationToken: token);
    }
    
    private async Task HandleGradingFlow(
        UserSession session,
        UserProfile user,
        Message message,
        CancellationToken token)
    {
        switch (session.State)
        {
            case SessionState.Grading_WaitingForGrade:
                await HandleGrading_WaitingForGrade(session, user, message, token);
                break;

            case SessionState.Grading_WaitingForComment:
                await HandleGrading_WaitingForComment(session, user, message, token);
                break;
        }
    }
    
    private async Task Grading_WaitingForGrade(
        UserSession session,
        UserProfile user,
        Message message,
        CancellationToken token)
    {
        if (!int.TryParse(message.Text, out var grade))
        {
            await _bot.SendMessage(
                user.TelegramId,
                "Оценка должна быть целым числом. Введите ещё раз:",
                cancellationToken: token);
        }
        else
        {
            session.TempGrade = grade;
            session.State = SessionState.Grading_WaitingForComment;

            await _bot.SendMessage(
                user.TelegramId,
                MessageFormatter.CheckingAskComment(),
                ParseMode.Markdown,
                cancellationToken: token);
        }
    }

    private async Task Grading_WaitingForComment(
        UserSession session,
        UserProfile user,
        Message message,
        CancellationToken token)
    {
        var submissionId = session.TempSubmissionId;
        var grade = session.TempGrade;

        if (submissionId == null || grade == null)
        {
            session.State = SessionState.None;
            await _bot.SendMessage(
                user.TelegramId,
                "Что-то пошло не так. Попробуйте ещё раз /check <assignmentId>.",
                cancellationToken: token);
            return;
        }

        var comment = message.Text == "-" ? "" : message.Text ?? "";

        var (success, error, submission) =
            _logic.SetGradeAndComment(submissionId.Value, user.TelegramId, grade.Value, comment);

        session.State = SessionState.None;
        session.TempSubmissionId = null;
        session.TempGrade = null;

        if (!success || submission == null)
        {
            await _bot.SendMessage(
                user.TelegramId,
                error,
                cancellationToken: token);
            return;
        }

        await _bot.SendMessage(
            user.TelegramId,
            $"Оценка выставлена: {grade.Value}.",
            cancellationToken: token);

        try
        {
            var student =
                _logic.GetOrCreateUser(submission.StudentTelegramId, "");

            var msg =
                $"Ваше решение по заданию {submission.AssignmentId} проверено.\n" +
                $"Оценка: *{submission.Grade}*\n";
            if (!string.IsNullOrWhiteSpace(submission.TeacherComment))
                msg += $"Комментарий преподавателя:\n{submission.TeacherComment}";

            await _bot.SendMessage(
                student.TelegramId,
                msg,
                ParseMode.Markdown,
                cancellationToken: token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при отправке результата студенту: {ex.Message}");
        }

        var assignmentId = submission.AssignmentId;
        var next = _logic.GetNextSubmissionForTeacher(assignmentId, user.TelegramId);
        if (next == null)
        {
            await _bot.SendMessage(
                user.TelegramId,
                "Больше решений для проверки нет.",
                cancellationToken: token);
            return;
        }

        session.State = SessionState.Grading_WaitingForGrade;
        session.TempSubmissionId = next.Id;

        var userProfile = _logic.GetOrCreateUser(next.StudentTelegramId, "");
        var text2 =
            $"Следующее решение #{next.Id} по заданию {next.AssignmentId}.\n" +
            $"Студент: {userProfile.FirstName} {userProfile.LastName} (id {userProfile.TelegramId})\n" +
            $"Вариант: {next.VariantNumber}\n" +
            $"Отправлено: {next.SubmittedAt:u}\n\n";

        if (!string.IsNullOrEmpty(next.AnswerText))
            text2 += $"Текст решения:\n{next.AnswerText}\n\n";

        await _bot.SendMessage(
            user.TelegramId,
            text2 + "Если есть прикреплённый файл, сейчас пришлю отдельно.",
            cancellationToken: token);

        if (!string.IsNullOrEmpty(next.AnswerFileId))
            await _bot.SendDocument(
                user.TelegramId,
                InputFile.FromFileId(next.AnswerFileId),
                "Файл решения",
                cancellationToken: token);

        await _bot.SendMessage(
            user.TelegramId,
            MessageFormatter.CheckingAskGrade(),
            ParseMode.Markdown,
            cancellationToken: token);
    }

    
    private async Task HandleGrading_WaitingForGrade(
        UserSession session,
        UserProfile user,
        Message message,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(message.Text) || !int.TryParse(message.Text.Trim(), out var grade))
        {
            await _bot.SendMessage(
                user.TelegramId,
                "❗ Введите оценку числом, например: 5",
                cancellationToken: token);
            return;
        }

        session.TempGrade = grade;
        session.State = SessionState.Grading_WaitingForComment;

        await _bot.SendMessage(
            user.TelegramId,
            "💬 Введите комментарий к работе (или '-' если без комментария).",
            cancellationToken: token);
    }

    private async Task HandleGrading_WaitingForComment(
    UserSession session,
    UserProfile user,
    Message message,
    CancellationToken token)
{
    var submissionId = session.TempSubmissionId;
    var grade = session.TempGrade;
    var assignmentId = session.TempAssignmentId;

    if (submissionId == null || grade == null || assignmentId == null)
    {
        session.State = SessionState.None;
        session.TempSubmissionId = null;
        session.TempGrade = null;
        session.TempAssignmentId = null;

        await _bot.SendMessage(
            chatId: user.TelegramId,
            text: "⚠ Внутренняя ошибка состояния. Начните заново: /start_checking <ID_задания>.",
            cancellationToken: token);
        return;
    }

    var commentText = message.Text?.Trim();
    if (commentText == "-") commentText = "";

    var (success, error, submission) =
        _logic.SetGradeAndComment(submissionId.Value, user.TelegramId, grade.Value, commentText ?? "");
    if (!success || submission == null)
    {
        session.State = SessionState.None;
        session.TempSubmissionId = null;
        session.TempGrade = null;
        session.TempAssignmentId = null;

        await _bot.SendMessage(
            chatId: user.TelegramId,
            text: $"❗ Не удалось сохранить оценку: {error}",
            cancellationToken: token);
        return;
    }

    await _bot.SendMessage(
        chatId: user.TelegramId,
        text: MessageFormatter.CheckingGradeSaved(grade.Value),
        cancellationToken: token);

    try
    {
        await _bot.SendMessage(
            chatId: submission.StudentTelegramId,
            text: MessageFormatter.CheckingStudentNotification(submission),
            cancellationToken: token);
    }
    catch {} 

    session.TempSubmissionId = null;
    session.TempGrade = null;

    var next = _logic.GetNextSubmissionForTeacher(assignmentId.Value, user.TelegramId);
    if (next == null)
    {
        session.State = SessionState.None;
        session.TempAssignmentId = null;

        await _bot.SendMessage(
            chatId: user.TelegramId,
            text: MessageFormatter.CheckingNoMoreSubmissions(),
            cancellationToken: token);
        return;
    }

    session.TempSubmissionId = next.Id;
    session.State = SessionState.Grading_WaitingForGrade;

    var student = _logic.GetOrCreateUser(next.StudentTelegramId, "");
    var name = $"{student.FirstName} {student.LastName}".Trim();
    if (string.IsNullOrWhiteSpace(name)) name = student.Username != "" ? $"@{student.Username}" : "(без имени)";

    await _bot.SendMessage(
        chatId: user.TelegramId,
        text: MessageFormatter.NextSubmission(next, name),
        cancellationToken: token);

    if (!string.IsNullOrEmpty(next.AnswerFileId))
        await _bot.SendDocument(
            chatId: user.TelegramId,
            document: InputFile.FromFileId(next.AnswerFileId),
            caption: "📎 Файл с решением студента",
            cancellationToken: token);

    await _bot.SendMessage(
        chatId: user.TelegramId,
        text: "✏️ Введите оценку (целое число), либо /cancel для отмены.",
        cancellationToken: token);
}
    
    private async Task HandleCheckCommand(UserSession session, UserProfile user, string[] parts,
        CancellationToken token)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], out var assignmentId))
        {
            await _bot.SendMessage(
                user.TelegramId,
                "❗ Использование: /check <assignmentId>",
                cancellationToken: token);
            return;
        }

        var next = _logic.GetNextSubmissionForTeacher(assignmentId, user.TelegramId);
        if (next == null)
        {
            await _bot.SendMessage(
                user.TelegramId,
                "Нет решений, ожидающих проверки, либо вы не являетесь преподавателем для группы этого задания.",
                cancellationToken: token);
            return;
        }

        session.State = SessionState.Grading_WaitingForGrade;
        session.TempSubmissionId = next.Id;

        var studentProfile = _logic.GetOrCreateUser(next.StudentTelegramId, "");

        var text =
            $"Решение #{next.Id} по заданию {next.AssignmentId}.\n" +
            $"Студент: {studentProfile.FirstName} {studentProfile.LastName} (id {studentProfile.TelegramId})\n" +
            $"Вариант: {next.VariantNumber}\n" +
            $"Отправлено: {next.SubmittedAt:u}\n\n";

        if (!string.IsNullOrEmpty(next.AnswerText))
            text += $"Текст решения:\n{next.AnswerText}\n\n";

        await _bot.SendMessage(
            user.TelegramId,
            text + "Если есть прикреплённый файл, сейчас пришлю отдельно.",
            cancellationToken: token);

        if (!string.IsNullOrEmpty(next.AnswerFileId))
            await _bot.SendDocument(
                user.TelegramId,
                InputFile.FromFileId(next.AnswerFileId),
                "Файл решения",
                cancellationToken: token);

        await _bot.SendMessage(
            user.TelegramId,
            "Введите *оценку* (целое число):",
            ParseMode.Markdown,
            cancellationToken: token);
    }
}