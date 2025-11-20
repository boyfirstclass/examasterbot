
using System.Collections.Concurrent;
using examasterbot.Formatting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;

using examasterbot.Logic;
using examasterbot.Sessions;
using examasterbot.Models.Users;

namespace examasterbot.Tg;

public partial class TelegramBotService
{
    private readonly TelegramBotClient _bot;
    private readonly BotLogic _logic;
    private readonly ReceiverOptions _receiverOptions;
    private readonly ConcurrentDictionary<long, UserSession> _sessions = new();
    private User? _me;

    public TelegramBotService(string token, BotLogic logic)
    {
        _bot = new TelegramBotClient(token);
        _logic = logic;
        _receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message }
        };
    }

    public async Task StartAsync()
    {
        _me = await _bot.GetMe();
        Console.WriteLine($"Запущен бот @{_me.Username}");

        var cts = new CancellationTokenSource();

        _bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            _receiverOptions,
            cancellationToken: cts.Token);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken token)
    {
        if (update.Type != UpdateType.Message) return;

        var message = update.Message;
        if (message == null) return;

        var from = message.From;
        if (from == null) return;

        if (message.Chat.Type != ChatType.Private)
        {
            await _bot.SendMessage(
                message.Chat.Id,
                "Пожалуйста, общайтесь со мной в личных сообщениях.",
                cancellationToken: token);
            return;
        }

        var userId = from.Id;
        var username = from.Username ?? "";

        var session = _sessions.GetOrAdd(userId, id => new UserSession
        {
            TelegramUserId = id
        });

        var user = _logic.GetOrCreateUser(userId, username);

        if (message.Text != null && message.Text.StartsWith("/cancel"))
        {
            session.State = SessionState.None;
            session.TempAssignmentDraft = null;
            session.TempAssignmentId = null;
            session.TempSubmissionId = null;
            session.TempGrade = null;
            session.TempGroupId = null;
            session.TempFirstName = null;

            await _bot.SendMessage(
                userId,
                "Текущая операция отменена.",
                cancellationToken: token);
            return;
        }

        if (!user.IsRegistered && !IsRegistrationCommand(message))
        {
            await HandleRegistrationFlow(session, user, message, token);
            return;
        }

        if (session.State != SessionState.None)
        {
            await HandleStatefulFlow(session, user, message, token);
            return;
        }

        if (message.Text != null && message.Text.StartsWith("/"))
            await HandleCommand(session, user, message, token);
        else
            await _bot.SendMessage(
                userId,
                "Я не понял. Используйте /help для списка команд.",
                cancellationToken: token);
    }
    
    private Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken token)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
    
    private async Task HandleStatefulFlow(UserSession session, UserProfile user, Message message,
        CancellationToken token)
    {
        switch (session.State)
        {
            case SessionState.CreatingGroup_Name:
                await HandleCreatingGroup_Name(session, user, message, token);
                break;

            case SessionState.JoiningGroup_InviteCode:
                await HandleJoiningGroup_InviteCode(session, user, message, token);
                break;

            case SessionState.CreatingAssignment_GroupId:
            case SessionState.CreatingAssignment_Title:
            case SessionState.CreatingAssignment_Description:
            case SessionState.CreatingAssignment_File:         
            case SessionState.CreatingAssignment_VariantCount:
            case SessionState.CreatingAssignment_VariantTask:
            case SessionState.CreatingAssignment_Deadline:
                await HandleCreatingAssignmentFlow(session, user, message, token);
                break;

            case SessionState.SubmittingSolution_WaitingForContent:
                await HandleSubmittingSolutionContent(session, user, message, token);
                break;

            case SessionState.Grading_WaitingForGrade:
            case SessionState.Grading_WaitingForComment:
                await HandleGradingFlow(session, user, message, token);
                break;

            default:
                session.State = SessionState.None;
                await _bot.SendMessage(
                    chatId: user.TelegramId,
                    text: "Что-то пошло не так. Попробуйте ещё раз или /cancel.",
                    cancellationToken: token);
                break;
        }
    }

    private async Task HandleCommand(UserSession session, UserProfile user, Message message,
        CancellationToken token)
    {
        var text = message.Text ?? "";
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0].ToLowerInvariant();

        switch (cmd)
        {
            case "/start":
                if(user.IsRegistered)
                await _bot.SendMessage(
                    chatId: user.TelegramId,
                    text: MessageFormatter.StartRegistered(user),
                    cancellationToken: token);
                if (!user.IsRegistered) await HandleRegistrationFlow(session, user, message, token);

                break;

            case "/help":
                await HandleHelp(user, token);
                break;
            
            case "/newcode":
                await HandleNewInviteCodeCommand(user, parts, token);
                break;

            case "/creategroup":
                session.State = SessionState.CreatingGroup_Name;
                await _bot.SendMessage(
                    user.TelegramId,
                    "Введите название новой группы:",
                    cancellationToken: token);
                break;

            case "/joingroup":
                session.State = SessionState.JoiningGroup_InviteCode;
                await _bot.SendMessage(
                    user.TelegramId,
                    "Введите инвайт-код группы:",
                    cancellationToken: token);
                break;

            case "/mygroups":
                await HandleMyGroups(user, token);
                break;

            case "/addteacher":
                await HandleAddTeacherCommand(parts, user, token);
                break;

            case "/newtask":
                await HandleNewTaskCommand(session, user, token);
                break;

            case "/submit":
                await HandleSubmitCommand(session, user, parts, token);
                break;

            case "/check":
                await HandleStartCheckingCommand(session, user, parts, token);
                break;

            case "/groupinfo":
                await HandleGroupInfoCommand(user, parts, token);
                break;

            case "/extend":
                await HandleExtendDeadlineCommand(user, parts, token);
                break;

            default:
                await _bot.SendMessage(
                    user.TelegramId,
                    "Неизвестная команда. Используйте /help.",
                    cancellationToken: token);
                break;
        }
    }
}