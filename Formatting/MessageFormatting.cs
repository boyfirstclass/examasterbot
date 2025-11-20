
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

using examasterbot.Models.Users;
using examasterbot.Models.Groups;
using examasterbot.Models.Assignments;

namespace examasterbot.Formatting
{
    public static class MessageFormatter
    {
        private static string Escape(string s) =>
            System.Net.WebUtility.HtmlEncode(s ?? "");

        public static string FormatDuration(TimeSpan duration) =>
            $"{(int)duration.TotalDays} д. {duration.Hours} ч. {duration.Minutes} мин.";


        public static string StartRegistered(UserProfile user)
        {
            var name = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = user.Username != "" ? $"@{user.Username}" : "без имени";

            return
                $"👋 Привет, <b>{Escape(name)}</b>!\n\n" +
                "Я — <b>ExaMasterBot</b>, бот для проведения контрольных и проверки заданий.\n\n" +
                "Посмотреть список команд:\n<code>/help</code>";
        }

        public static string StartUnregistered()
        {
            return
                "👋 Привет!\n\n" +
                "Я — <b>ExaMasterBot</b>, бот для проведения контрольных и проверки заданий.\n\n" +
                "Для начала нужно пройти небольшую регистрацию.\n\n" +
                "Введите ваше <b>имя</b>:";
        }

        public static string RegistrationAskFirstName()
        {
            return
                "✏️ Введите ваше <b>имя</b>.\n\n" +
                "Например: <code>Иван</code>";
        }

        public static string RegistrationAskLastName(string firstName)
        {
            return
                $"👍 Отлично, <b>{Escape(firstName)}</b>!\n\n" +
                "Теперь введите вашу <b>фамилию</b>.\n\n" +
                "Например: <code>Иванов</code>";
        }

        public static string RegistrationCompleted(UserProfile user)
        {
            var name = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = user.Username != "" ? $"@{user.Username}" : "без имени";

            return
                $"✅ Регистрация завершена!\n\n" +
                $"Теперь вы — <b>{Escape(name)}</b>.\n\n" +
                "Посмотреть, что я умею:\n<code>/help</code>";
        }

        public static string HelpText()
        {
            return @"
<b>🤖 ExaMasterBot</b> — помощник для проведения контрольных и заданий

<b>🔹 Общие команды</b>
<code>/start</code> – приветствие и краткая информация о боте
<code>/help</code> – показать это справочное сообщение
<code>/register</code> – регистрация (имя и фамилия)
<code>/cancel</code> – отменить текущий шаг диалога

<b>🏫 Группы</b>
<code>/creategroup</code> – создать новую учебную группу
<code>/joingroup</code> – вступить в группу по коду приглашения
<code>/mygroups</code> – список ваших групп и ваша роль в каждой
<code>/groupinfo &lt;ID_группы&gt;</code> – состав группы (преподаватели и студенты)
<code>/newcode &lt;ID_группы&gt;</code> – новый код приглашения (только создатель группы)
<code>/addteacher &lt;ID_группы&gt; &lt;TelegramId&gt;</code> – добавить преподавателя в группу

<b>📝 Контрольные и задания (для преподавателей)</b>
<code>/newtask</code> – мастер создания контрольной:
  • выбор группы
  • название и описание
  • (опционально) файл с условиями
  • задания по вариантам
  • длительность (дни, часы, минуты)

<b>📤 Отправка решений (для студентов)</b>
<code>/submit &lt;ID_задания&gt;</code> – отправить решение на контрольную.
После команды бот попросит прислать текст или документ с решением.

<b>🧪 Проверка работ (для преподавателей)</b>
<code>/check &lt;ID_задания&gt;</code> – начать поочерёдную проверку решений.
Бот показывает работы по очереди, спрашивает оценку и комментарий, затем
автоматически подаёт следующую работу.

<b>⏰ Дедлайны</b>
При создании задания указывается длительность контрольной (от 5 минут до 31 дня).
<code>/extend &lt;ID_задания&gt; &lt;дни&gt; &lt;часы&gt; &lt;минуты&gt;</code> – продлить дедлайн задания
Например: <code>/extend 1001 0 1 30</code> – продлить на 1 час 30 минут.

<b>ℹ️ Важно</b>
• Все команды работают только в личных сообщениях с ботом.
• Если что-то пошло не так, всегда можно ввести <code>/cancel</code> и начать заново.
";
        }

        /*public static string CreateGroupAskName()
        {
            return
                "🏫 Создание новой группы\n\n" +
                "✏️ Введите <b>название группы</b>.\n\n" +
                "Например: <code>МТУ 1 курс</code>";
        }

        public static string GroupCreated(GroupInfo group)
        {
            return
                "✅ <b>Группа создана!</b>\n\n" +
                $"ID группы: <code>{group.Id}</code>\n" +
                $"Название: <b>{Escape(group.Name)}</b>\n\n" +
                "🔑 Код приглашения:\n" +
                $"<code>{Escape(group.InviteCode)}</code>\n\n" +
                "Отправьте этот код студентам и другим преподавателям,\n" +
                "чтобы они могли присоединиться командой <code>/joingroup</code>.";
        }

        public static string JoinGroupAskCode()
        {
            return
                "🔑 Вступление в группу\n\n" +
                "Отправьте <b>код приглашения</b>, который вам дал преподаватель.\n\n" +
                "Например: <code>ab12cd34</code>";
        }

        public static string JoinGroupSuccess(GroupInfo group, string role)
        {
            return
                "✅ Вы успешно вступили в группу!\n\n" +
                $"Группа: <b>{Escape(group.Name)}</b>\n" +
                $"ID: <code>{group.Id}</code>\n" +
                $"Ваша роль: <b>{Escape(role)}</b>";
        }

        public static string MyGroupsList(IEnumerable<(GroupInfo group, string role)> groups)
        {
            var list = groups.ToList();
            if (!list.Any())
                return "ℹ️ Вы пока не состоите ни в одной группе.";

            var sb = new StringBuilder();
            sb.AppendLine("🏫 <b>Ваши группы:</b>");
            sb.AppendLine();

            foreach (var (g, role) in list)
            {
                sb.AppendLine($"• <b>{Escape(g.Name)}</b> (ID: <code>{g.Id}</code>) — {Escape(role)}");
            }

            return sb.ToString();
        }*/

        public static string GroupInfo(GroupInfo group, IEnumerable<UserProfile> teachers,
            IEnumerable<UserProfile> students)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<b>🏫 Группа:</b> {Escape(group.Name)} (ID: <code>{group.Id}</code>)");
            sb.AppendLine();

            sb.AppendLine("<b>👨‍🏫 Преподаватели:</b>");
            var tList = teachers.ToList();
            if (!tList.Any())
            {
                sb.AppendLine("• (нет преподавателей)");
            }
            else
            {
                foreach (var t in tList)
                {
                    var name = $"{t.FirstName} {t.LastName}".Trim();
                    if (string.IsNullOrWhiteSpace(name))
                        name = t.Username != "" ? $"@{t.Username}" : "(без имени)";
                    else
                        name = Escape(name);

                    var username = string.IsNullOrWhiteSpace(t.Username) ? "" : $" (@{t.Username})";
                    sb.AppendLine($"• {name}{username} (id: {t.TelegramId})");
                }
            }

            sb.AppendLine();
            sb.AppendLine("<b>👨‍🎓 Студенты:</b>");
            var sList = students.ToList();
            if (!sList.Any())
            {
                sb.AppendLine("• (нет студентов)");
            }
            else
            {
                foreach (var s in sList)
                {
                    var name = $"{s.FirstName} {s.LastName}".Trim();
                    if (string.IsNullOrWhiteSpace(name))
                        name = s.Username != "" ? $"@{s.Username}" : "(без имени)";
                    else
                        name = Escape(name);

                    var username = string.IsNullOrWhiteSpace(s.Username) ? "" : $" (@{s.Username})";
                    sb.AppendLine($"• {name}{username} (id: {s.TelegramId})");
                }
            }

            return sb.ToString();
        }

        /*public static string NewInviteCode(GroupInfo group)
        {
            return
                "🔑 <b>Новый код приглашения</b>\n\n" +
                $"Группа: <b>{Escape(group.Name)}</b> (ID: <code>{group.Id}</code>)\n\n" +
                "Код:\n" +
                $"<code>{Escape(group.InviteCode)}</code>\n\n" +
                "Старый код теперь недействителен.";
        }

        public static string AddTeacherSuccess(GroupInfo group, UserProfile teacher)
        {
            var name = $"{teacher.FirstName} {teacher.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = teacher.Username != "" ? $"@{teacher.Username}" : "(без имени)";
            else
                name = Escape(name);

            return
                "✅ Преподаватель добавлен в группу.\n\n" +
                $"Группа: <b>{Escape(group.Name)}</b> (ID: <code>{group.Id}</code>)\n" +
                $"Преподаватель: {name} (id: {teacher.TelegramId})";
        }

        public static string NotInGroup()
        {
            return "🚫 Вы не состоите в этой группе, поэтому не можете просматривать её состав.";
        }*/

        public static string NewTaskNoGroups()
        {
            return
                "❗ У вас нет групп, в которых вы являетесь преподавателем.\n\n" +
                "Создайте группу командой <code>/creategroup</code> или попросите коллегу добавить вас.";
        }

        public static string NewTaskChooseGroup(IEnumerable<(GroupInfo group, string role)> groups)
        {
            var sb = new StringBuilder();
            sb.AppendLine("📝 <b>Создание контрольной</b>");
            sb.AppendLine();
            sb.AppendLine("Выберите группу по ID из списка и отправьте ID сообщением:");
            sb.AppendLine();

            foreach (var (g, role) in groups)
            {
                sb.AppendLine($"• ID: <code>{g.Id}</code> — <b>{Escape(g.Name)}</b> ({Escape(role)})");
            }

            sb.AppendLine();
            sb.AppendLine("Например: <code>1000</code>");

            return sb.ToString();
        }

        /*public static string NewTaskAskTitle()
        {
            return
                "✏️ Введите <b>тему контрольной</b>.\n\n" +
                "Например: <code>Контрольная по линейной алгебре №1</code>";
        }

        public static string NewTaskAskCommonFile()
        {
            return
                "📎 Пришлите <b>общий файл</b> с условиями контрольной (документ).\n\n" +
                "Если файл не нужен — отправьте сообщение с символом <code>-</code>.";
        }*/
        
        public static string NewTaskAskDescription()
        {
            return
                "📝 Введите <b>общее описание / инструкции</b> к контрольной.\n\n" +
                "Если хотите, можете описать формат ответов, критерии и т.п.";
        }

        public static string NewTaskAskVariantCount()
        {
            return
                "🔢 Сколько <b>вариантов</b> заданий будет?\n\n" +
                "Введите целое число от <code>1</code> до <code>100</code>.";
        }

        public static string VariantCount()
        {
            return
                "❗ Число вариантов должно быть от 1 до 100.";
        }

        public static string NewTaskAskVariantTask(int variantNumber)
        {
            return
                $"✏️ Отправьте задание для <b>варианта {variantNumber}</b>.\n\n" +
                "Можно прислать:\n" +
                "• текст сообщением\n" +
                "• документ\n" +
                "• документ с подписью (текст задания в подписи)";
        }

        public static string NewTaskAskDuration()
        {
            return
                "⏱ Укажите <b>длительность</b> контрольной в формате:\n" +
                "<code>дни часы минуты</code>\n\n" +
                "Например: <code>0 1 30</code> — 1 час 30 минут.\n\n" +
                "От 5 минут до 31 дня.";
        }

        public static string SubmitTimeTask()
        {
            return
                "Длительность должна быть не менее 5 минут и не более 31 дня.";
        }

        public static string AssignmentCreatedForTeacher(Assignment assignment, TimeSpan duration)
        {
            return
                $"✅ <b>Контрольная создана!</b>\n\n" +
                $"ID: <code>{assignment.Id}</code>\n" +
                $"Группа: <code>{assignment.GroupId}</code>\n" +
                $"Тема: <b>{Escape(assignment.Title)}</b>\n" +
                $"Вариантов: <code>{assignment.VariantCount}</code>\n" +
                $"⏱ Длительность: <code>{FormatDuration(duration)}</code>\n" +
                $"🕒 Дедлайн (UTC + 3): <code>{assignment.Deadline:u}</code>";
        }

        public static string AssignmentNotificationForStudent(
            Assignment assignment,
            String duration,
            int variant,
            string? commonDescription,
            string? variantText)
        {
            var sb = new StringBuilder();
            sb.AppendLine("📚 <b>Новая контрольная!</b>");
            sb.AppendLine();
            sb.AppendLine($"Группа: <code>{assignment.GroupId}</code>");
            sb.AppendLine($"Тема: <b>{Escape(assignment.Title)}</b>");
            sb.AppendLine($"Ваш вариант: <code>{variant}</code>");
            sb.AppendLine($"⏱ Длительность: <code>{(duration)}</code>");
            sb.AppendLine($"🕒 Дедлайн (UTC + 3): <code>{assignment.Deadline:u}</code>");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(commonDescription))
            {
                sb.AppendLine("<b>Общие инструкции:</b>");
                sb.AppendLine(Escape(commonDescription!));
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(variantText))
            {
                sb.AppendLine("<b>Задание вашего варианта:</b>");
                sb.AppendLine(Escape(variantText!));
                sb.AppendLine();
            }

            sb.AppendLine($"Отправьте решение командой:\n<code>/submit {assignment.Id}</code>");

            return sb.ToString();
        }

        /*public static string SubmitUsage()
        {
            return
                "❗ Использование команды:\n" +
                "<code>/submit &lt;ID_задания&gt;</code>\n\n" +
                "Например: <code>/submit 3</code>";
        }

        public static string SubmitAssignmentNotFound(int assignmentId)
        {
            return
                $"❗ Задание с ID <code>{assignmentId}</code> не найдено.";
        }

        public static string SubmitAskContent(Assignment assignment, int variant)
        {
            return
                $"📤 Отправка решения по заданию <code>{assignment.Id}</code>, вариант <code>{variant}</code>.\n\n" +
                "Пришлите текст или документ с решением.\n" +
                "Можно также отправить документ с подписью (текст решения в подписи файла).";
        }*/

        public static string SubmitAccepted(Submission submission)
        {
            return
                "✅ Решение отправлено!\n\n" +
                $"ID задания: <code>{submission.AssignmentId}</code>\n" +
                $"Вариант: <code>{submission.VariantNumber}</code>\n" +
                $"Время отправки (UTC): <code>{submission.SubmittedAt:u}</code>";
        }

        public static string StartCheckingUsage()
        {
            return
                "❗ Использование:\n" +
                "<code>/start_checking &lt;ID_задания&gt;</code>\n\n" +
                "Например: <code>/start_checking 5</code>";
        }

        public static string StartCheckingNoSubmissions()
        {
            return "📭 Пока нет решений, ожидающих проверки по этому заданию.";
        }

        public static string CheckingShowSubmission(
            Assignment assignment,
            Submission submission,
            UserProfile student)
        {
            var name = $"{student.FirstName} {student.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = student.Username != "" ? $"@{student.Username}" : "(без имени)";
            else
                name = Escape(name);

            var sb = new StringBuilder();
            sb.AppendLine("🧾 <b>Работа для проверки</b>");
            sb.AppendLine();
            sb.AppendLine($"Задание ID: <code>{submission.AssignmentId}</code>");
            sb.AppendLine($"Тема: <b>{Escape(assignment.Title)}</b>");
            sb.AppendLine($"Студент: {name} (id: {submission.StudentTelegramId})");
            sb.AppendLine($"Вариант: <code>{submission.VariantNumber}</code>");
            sb.AppendLine($"Отправлено (UTC): <code>{submission.SubmittedAt:u}</code>");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(submission.AnswerText))
            {
                sb.AppendLine("<b>Ответ (текст):</b>");
                sb.AppendLine(Escape(submission.AnswerText));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public static string CheckingAskGrade()
        {
            return "✏️ Введите <b>оценку</b> (целое число), или введите <code>/cancel</code> для отмены.";
        }

        public static string CheckingAskComment()
        {
            return
                "💬 Введите комментарий к работе.\n\n" +
                "Если комментарий не нужен — отправьте символ <code>-</code>.";
        }

        public static string CheckingGradeSaved(int grade)
        {
            return $"✅ Оценка <b>{grade}</b> сохранена.";
        }

        public static string CheckingNoMoreSubmissions()
        {
            return
                "📭 Работы для проверки по этому заданию закончились.\n\n";
        }

        public static string CheckingStudentNotification(Submission submission)
        {
            var gradeText = submission.Grade.HasValue ? submission.Grade.Value.ToString() : "?";

            var sb = new StringBuilder();
            sb.AppendLine("📊 <b>Ваша работа проверена.</b>");
            sb.AppendLine();
            sb.AppendLine($"ID задания: <code>{submission.AssignmentId}</code>");
            sb.AppendLine($"Вариант: <code>{submission.VariantNumber}</code>");
            sb.AppendLine($"Оценка: <b>{Escape(gradeText)}</b>");

            if (!string.IsNullOrWhiteSpace(submission.TeacherComment))
            {
                sb.AppendLine();
                sb.AppendLine("<b>Комментарий преподавателя:</b>");
                sb.AppendLine(Escape(submission.TeacherComment));
            }

            return sb.ToString();
        }


        public static string ExtendUsage()
        {
            return
                "❗ Использование:\n" +
                "<code>/extend &lt;ID_задания&gt; &lt;дни&gt; &lt;часы&gt; &lt;минуты&gt;</code>\n\n" +
                "Например: <code>/extend 3 0 1 30</code> — продлить на 1 час 30 минут.";
        }

        public static string ExtendTeacherNotification(Assignment assignment, String extension)
        {
            return
                "✅ Дедлайн продлён.\n\n" +
                $"ID задания: <code>{assignment.Id}</code>\n" +
                $"Продление: <code>{(extension)}</code>\n" +
                $"Новый дедлайн (UTC + 3): <code>{assignment.Deadline:u}</code>";
        }

        public static string ExtendStudentNotification(Assignment assignment)
        {
            return
                "⏰ <b>Дедлайн по заданию продлён.</b>\n\n" +
                $"Тема: <b>{Escape(assignment.Title)}</b>\n" +
                $"ID задания: <code>{assignment.Id}</code>\n" +
                $"Новый дедлайн (UTC + 3): <code>{assignment.Deadline:u}</code>";
        }

        public static string InternalError()
        {
            return
                "⚠ Произошла внутренняя ошибка.\n\n" +
                "Попробуйте ещё раз или введите <code>/cancel</code> и начните заново.";
        }

        public static string DeadlineIsOver()
        {
            return
                "Дедлайн уже истёк, отправить решение нельзя.";
        }

        public static string AlreadySubmitted()
        {
            return
                "Вы уже отправили решение на это задание.";
        }

        public static string NextSubmission(Submission next, string name)
        {
            var textBuilder = new StringBuilder();
            textBuilder.AppendLine("🧾 Следующая работа для проверки:");
            textBuilder.AppendLine($"Задание ID: {next.AssignmentId}");
            textBuilder.AppendLine($"Студент: {name} (id: {next.StudentTelegramId})");
            textBuilder.AppendLine($"Вариант: {next.VariantNumber}");
            textBuilder.AppendLine($"Отправлено (UTC + 3): {next.SubmittedAt.AddHours(3):u}");
            textBuilder.AppendLine();

            if (!string.IsNullOrWhiteSpace(next.AnswerText))
            {
                textBuilder.AppendLine("Ответ (текст):");
                textBuilder.AppendLine(next.AnswerText);
                textBuilder.AppendLine();
            }

            return textBuilder.ToString();
        }
    }
}

namespace examasterbot.Tg
{
    public static class BotClientExtensions
    {
        public static Task SendMessage(this ITelegramBotClient bot,
            long chatId,
            string text,
            CancellationToken cancellationToken = default)
        {
            return bot.SendMessage(
                chatId: chatId,
                text: text,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }
    }
}