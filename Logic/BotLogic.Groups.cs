
using examasterbot.Models.Groups;
using examasterbot.Models.Users;

namespace examasterbot.Logic;

public partial class BotLogic
{
    public GroupInfo CreateGroup(long ownerTelegramId, string groupName)
    {
        var baseId = 1000;
        int newId;
        if (!_storage.Groups.Any())
        {
            newId = baseId;
        }
        else
        {
            var maxExisting = _storage.Groups.Max(g => g.Id);
            newId = Math.Max(baseId, maxExisting + 1);
        }
            
        var inviteCode = Guid.NewGuid().ToString("N")[..8];

        var group = new GroupInfo
        {
            Id = newId,
            Name = groupName,
            OwnerTelegramId = ownerTelegramId,
            InviteCode = inviteCode
        };

        _storage.Groups.Add(group);

        _storage.GroupTeachers.Add(new GroupTeacher
        {
            GroupId = group.Id,
            TeacherTelegramId = ownerTelegramId
        });

        _storage.SaveAll();
        return group;
    }

    public List<(GroupInfo group, string role)> GetUserGroups(long userId)
    {
        var result = new List<(GroupInfo, string)>();

        foreach (var g in _storage.Groups)
        {
            if (g.OwnerTelegramId == userId)
            {
                result.Add((g, "владелец/преподаватель"));
            }
            else if (_storage.GroupTeachers.Any(t => t.GroupId == g.Id && t.TeacherTelegramId == userId))
            {
                result.Add((g, "преподаватель"));
            }
            else if (_storage.GroupStudents.Any(s => s.GroupId == g.Id && s.StudentTelegramId == userId))
            {
                result.Add((g, "студент"));
            }
        }

        return result;
    }
    
    public (bool success, string error, GroupInfo? group) JoinGroupByInviteCode(long userId, string inviteCode)
        {
            var group = _storage.Groups.FirstOrDefault(g =>
                string.Equals(g.InviteCode, inviteCode, StringComparison.OrdinalIgnoreCase));
            if (group == null)
                return (false, "Группа с таким кодом не найдена.", null);

            if (_storage.GroupStudents.Any(s => s.GroupId == group.Id && s.StudentTelegramId == userId))
                return (false, "Вы уже состоите в этой группе как студент.", group);

            if (_storage.GroupTeachers.Any(t => t.GroupId == group.Id && t.TeacherTelegramId == userId))
                return (false, "Вы уже преподаватель в этой группе.", group);

            _storage.GroupStudents.Add(new GroupStudent
            {
                GroupId = group.Id,
                StudentTelegramId = userId
            });

            _storage.SaveAll();
            return (true, "", group);
        }

        public (bool success, string error) AddTeacherToGroup(long ownerId, int groupId, long teacherId)
        {
            var group = _storage.Groups.FirstOrDefault(g => g.Id == groupId);
            if (group == null) return (false, "Группа не найдена.");

            if (group.OwnerTelegramId != ownerId)
                return (false, "Только создатель группы может назначать преподавателей.");

            if (_storage.GroupTeachers.Any(t => t.GroupId == groupId && t.TeacherTelegramId == teacherId))
                return (false, "Этот пользователь уже преподаватель в этой группе.");

            _storage.GroupTeachers.Add(new GroupTeacher
            {
                GroupId = groupId,
                TeacherTelegramId = teacherId
            });

            _storage.SaveAll();
            return (true, "");
        }

        public List<UserProfile> GetGroupStudents(int groupId)
        {
            var ids = _storage.GroupStudents
                .Where(s => s.GroupId == groupId)
                .Select(s => s.StudentTelegramId)
                .Distinct()
                .ToList();
            return _storage.Users.Where(u => ids.Contains(u.TelegramId)).ToList();
        }

        public List<UserProfile> GetGroupTeachers(int groupId)
        {
            var ids = _storage.GroupTeachers
                .Where(t => t.GroupId == groupId)
                .Select(t => t.TeacherTelegramId)
                .Distinct()
                .ToList();
            return _storage.Users.Where(u => ids.Contains(u.TelegramId)).ToList();
        }
        
        public (bool success, string error, GroupInfo? group, List<UserProfile>? teachers, List<UserProfile>? students)
            GetGroupMembers(int groupId)
        {
            var group = _storage.Groups.FirstOrDefault(g => g.Id == groupId);
            if (group == null)
                return (false, "Группа не найдена.", null, null, null);

            var teachers = GetGroupTeachers(groupId);
            var students = GetGroupStudents(groupId);

            return (true, "", group, teachers, students);
        }
}