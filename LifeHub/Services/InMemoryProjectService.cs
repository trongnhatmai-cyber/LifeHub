using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text;
using LifeHub.Models.Domain;
using LifeHub.Models.MockData;
using LifeHub.Models.ViewModels.ProjectManagement;

namespace LifeHub.Services
{
    public class InMemoryProjectService : IProjectService
    {
        private readonly object _gate = new();
        private readonly List<UserEntity> _users;
        private readonly List<ProjectEntity> _projects;
        private readonly List<ProjectMemberEntity> _members;
        private readonly List<ProjectInvitationEntity> _invitations;
        private readonly List<ProjectMilestoneEntity> _milestones;
        private readonly List<ProjectTaskEntity> _tasks;
        private readonly List<ProjectActivityEntity> _activities;

        public InMemoryProjectService()
        {
            var seed = ProjectMockData.CreateSeed();
            _users = seed.Users;
            _projects = seed.Projects;
            _members = seed.Members;
            _invitations = seed.Invitations;
            _milestones = seed.Milestones;
            _tasks = seed.Tasks;
            _activities = seed.Activities;
        }

        // =================================================================
        // ĐỌC DỮ LIỆU
        // =================================================================
        public ProjectViewModel GetIndex(int userId)
        {
            lock (_gate)
            {
                var mine = _members.Where(m => m.UserId == userId).Select(m => m.ProjectId).ToHashSet();

                var cards = _projects.Where(p => mine.Contains(p.Id)).OrderBy(p => p.Id)
                    .Select(p => ToCard(p, userId)).ToList();

                var milestones = _milestones.Where(ms => mine.Contains(ms.ProjectId))
                    .OrderBy(ms => ms.IsCompleted).ThenBy(ms => ms.DueDate)
                    .Take(10).Select(ms => ToMilestone(ms, userId)).ToList();

                return new ProjectViewModel
                {
                    TotalProjects = cards.Count,
                    ActiveProjects = cards.Count(c => c.StatusKey == "running"),
                    CompletedProjects = cards.Count(c => c.StatusKey == "completed"),
                    TotalTeamMembers = _members.Where(m => mine.Contains(m.ProjectId)).Select(m => m.UserId).Distinct().Count(),
                    Projects = cards,
                    RecentMilestones = milestones,
                    StatusFilters = new List<ProjectFilterOption>
                    {
                        new() { Key = "all", Label = "Tất cả", Count = cards.Count },
                        new() { Key = "running", Label = ProjectStatuses.Running, Count = cards.Count(c => c.StatusKey == "running") },
                        new() { Key = "planning", Label = ProjectStatuses.Planning, Count = cards.Count(c => c.StatusKey == "planning") },
                        new() { Key = "paused", Label = ProjectStatuses.Paused, Count = cards.Count(c => c.StatusKey == "paused") },
                        new() { Key = "completed", Label = ProjectStatuses.Completed, Count = cards.Count(c => c.StatusKey == "completed") }
                    }
                };
            }
        }

        public ProjectDetailViewModel? GetDetail(int projectId, int userId)
        {
            lock (_gate)
            {
                var p = _projects.FirstOrDefault(x => x.Id == projectId);
                var role = RoleOf(projectId, userId);
                if (p == null || role == null) return null;

                var top = _tasks.Where(t => t.ProjectId == projectId && t.ParentTaskId == null).ToList();
                var done = top.Count(t => t.Status == TaskStatuses.Done);

                var members = _members.Where(m => m.ProjectId == projectId)
                    .OrderBy(m => m.Role == ProjectRoles.Manager ? 0 : m.Role == ProjectRoles.Member ? 1 : 2)
                    .Select(m =>
                    {
                        var u = _users.First(x => x.Id == m.UserId);
                        return new ProjectMemberItem
                        {
                            UserId = u.Id,
                            Name = u.Name,
                            Email = u.Email,
                            Role = m.Role,
                            OpenTaskCount = top.Count(t => t.AssigneeId == u.Id && t.Status != TaskStatuses.Done),
                            IsCurrentUser = u.Id == userId
                        };
                    }).ToList();

                var columns = TaskStatuses.All.Select(st => new BoardColumnItem
                {
                    Key = st,
                    Title = ColumnTitle(st),
                    Tasks = top.Where(t => t.Status == st).OrderBy(t => t.SortOrder)
                        .Select(t => ToTaskCard(p, t)).ToList()
                }).ToList();

                return new ProjectDetailViewModel
                {
                    Id = p.Id,
                    Key = p.Key,
                    Name = p.Name,
                    Description = p.Description,
                    Status = p.Status,
                    StartDate = p.StartDate,
                    DueDate = p.DueDate,
                    TotalTasks = top.Count,
                    CompletedTasks = done,
                    ProgressPercent = top.Count == 0 ? 0 : (int)Math.Round(done * 100.0 / top.Count),
                    CurrentRole = role,
                    CanManage = role == ProjectRoles.Manager,
                    CanEdit = role != ProjectRoles.Viewer,
                    Columns = columns,
                    Members = members,
                    Invitations = _invitations
                        .Where(i => i.ProjectId == projectId && i.Status == InvitationStatuses.Pending)
                        .OrderByDescending(i => i.CreatedAt)
                        .Select(i => new ProjectInvitationItem
                        {
                            Id = i.Id, Email = i.Email, Role = i.Role,
                            InvitedByName = UserName(i.InvitedBy), CreatedAt = i.CreatedAt
                        }).ToList(),
                    Milestones = _milestones.Where(ms => ms.ProjectId == projectId)
                        .OrderBy(ms => ms.IsCompleted).ThenBy(ms => ms.DueDate)
                        .Select(ms => ToMilestone(ms, userId)).ToList(),
                    Activities = _activities.Where(a => a.ProjectId == projectId)
                        .OrderByDescending(a => a.CreatedAt).Take(15)
                        .Select(a => new ProjectActivityItem
                        {
                            ActorName = UserName(a.UserId), Text = a.Text, CreatedAt = a.CreatedAt
                        }).ToList()
                };
            }
        }

        // =================================================================
        // DỰ ÁN
        // =================================================================
        public ProjectActionResult CreateProject(int userId, string? name, string? description, string? key,
            DateTime startDate, DateTime dueDate, string? status)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length == 0) return ProjectActionResult.Fail("Vui lòng nhập tên dự án.");
            if (name.Length > 80) return ProjectActionResult.Fail("Tên dự án tối đa 80 ký tự.");
            if (startDate == default || dueDate == default) return ProjectActionResult.Fail("Vui lòng chọn ngày bắt đầu và hạn chót.");
            if (dueDate.Date < startDate.Date) return ProjectActionResult.Fail("Hạn chót phải sau hoặc bằng ngày bắt đầu.");

            lock (_gate)
            {
                var finalKey = UniqueKey(string.IsNullOrWhiteSpace(key) ? MakeKey(name) : SanitizeKey(key));
                var project = new ProjectEntity
                {
                    Id = NextId(_projects, p => p.Id),
                    Key = finalKey,
                    Name = name,
                    Description = (description ?? string.Empty).Trim(),
                    StartDate = startDate.Date,
                    DueDate = dueDate.Date,
                    Status = status == ProjectStatuses.Running ? ProjectStatuses.Running : ProjectStatuses.Planning,
                    CreatedBy = userId
                };
                _projects.Add(project);
                _members.Add(new ProjectMemberEntity
                {
                    ProjectId = project.Id, UserId = userId, Role = ProjectRoles.Manager, JoinedAt = DateTime.Now
                });
                Log(project.Id, userId, "đã tạo dự án");
                return ProjectActionResult.Ok($"Đã tạo dự án \"{name}\" (mã {finalKey}).", project.Id);
            }
        }

        public ProjectActionResult UpdateProjectStatus(int userId, int projectId, string? status)
        {
            lock (_gate)
            {
                var project = _projects.FirstOrDefault(p => p.Id == projectId);
                if (project == null) return ProjectActionResult.Fail("Không tìm thấy dự án.");
                if (!IsManager(projectId, userId)) return ProjectActionResult.Fail("Chỉ Manager mới đổi được trạng thái dự án.");
                if (status == null || !ProjectStatuses.All.Contains(status)) return ProjectActionResult.Fail("Trạng thái không hợp lệ.");
                if (project.Status == status) return ProjectActionResult.Ok("Trạng thái không thay đổi.");

                project.Status = status;
                Log(projectId, userId, $"đã đổi trạng thái dự án sang \"{status}\"");
                return ProjectActionResult.Ok($"Đã đổi trạng thái dự án sang \"{status}\".");
            }
        }

        // =================================================================
        // CỘT MỐC
        // =================================================================
        public ProjectActionResult AddMilestone(int userId, int projectId, string? title, DateTime dueDate)
        {
            title = (title ?? string.Empty).Trim();
            if (title.Length == 0) return ProjectActionResult.Fail("Vui lòng nhập tên cột mốc.");
            if (title.Length > 100) return ProjectActionResult.Fail("Tên cột mốc tối đa 100 ký tự.");
            if (dueDate == default) return ProjectActionResult.Fail("Vui lòng chọn hạn hoàn thành.");

            lock (_gate)
            {
                if (!_projects.Any(p => p.Id == projectId)) return ProjectActionResult.Fail("Không tìm thấy dự án.");
                if (!IsManager(projectId, userId)) return ProjectActionResult.Fail("Chỉ Manager mới thêm được cột mốc.");

                _milestones.Add(new ProjectMilestoneEntity
                {
                    Id = NextId(_milestones, m => m.Id), ProjectId = projectId, Title = title, DueDate = dueDate.Date
                });
                Log(projectId, userId, $"đã thêm cột mốc \"{title}\"");
                return ProjectActionResult.Ok($"Đã thêm cột mốc \"{title}\".");
            }
        }

        public ProjectActionResult ToggleMilestone(int userId, int milestoneId)
        {
            lock (_gate)
            {
                var ms = _milestones.FirstOrDefault(m => m.Id == milestoneId);
                if (ms == null) return ProjectActionResult.Fail("Không tìm thấy cột mốc.");
                if (!CanEdit(ms.ProjectId, userId)) return ProjectActionResult.Fail("Bạn không có quyền cập nhật cột mốc.");

                ms.IsCompleted = !ms.IsCompleted;
                Log(ms.ProjectId, userId, ms.IsCompleted
                    ? $"đã hoàn thành cột mốc \"{ms.Title}\""
                    : $"đã mở lại cột mốc \"{ms.Title}\"");
                return ProjectActionResult.Ok(ms.IsCompleted
                    ? $"Đã đánh dấu hoàn thành \"{ms.Title}\"."
                    : $"Đã mở lại \"{ms.Title}\".");
            }
        }

        // =================================================================
        // NHIỆM VỤ
        // =================================================================
        public ProjectActionResult CreateTask(int userId, int projectId, string? title, string? description,
            string? priority, DateTime? dueDate, int? assigneeId, string? status, int? parentTaskId)
        {
            title = (title ?? string.Empty).Trim();
            if (title.Length == 0) return ProjectActionResult.Fail("Vui lòng nhập tên nhiệm vụ.");
            if (title.Length > 120) return ProjectActionResult.Fail("Tên nhiệm vụ tối đa 120 ký tự.");

            lock (_gate)
            {
                var project = _projects.FirstOrDefault(p => p.Id == projectId);
                if (project == null) return ProjectActionResult.Fail("Không tìm thấy dự án.");
                if (!CanEdit(projectId, userId)) return ProjectActionResult.Fail("Bạn không có quyền tạo nhiệm vụ trong dự án này.");

                ProjectTaskEntity? parent = null;
                if (parentTaskId.HasValue)
                {
                    parent = _tasks.FirstOrDefault(t => t.Id == parentTaskId.Value && t.ProjectId == projectId && t.ParentTaskId == null);
                    if (parent == null) return ProjectActionResult.Fail("Không tìm thấy nhiệm vụ cha.");
                }

                if (assigneeId.HasValue && !IsAssignable(projectId, assigneeId.Value))
                    return ProjectActionResult.Fail("Chỉ giao việc cho Manager hoặc Member của dự án.");

                var task = new ProjectTaskEntity
                {
                    Id = NextId(_tasks, t => t.Id),
                    ProjectId = projectId,
                    Number = ++project.TaskCounter,
                    Title = title,
                    Description = (description ?? string.Empty).Trim(),
                    Status = parent != null || status == null || !TaskStatuses.All.Contains(status) ? TaskStatuses.Todo : status,
                    Priority = priority != null && TaskPriorities.All.Contains(priority) ? priority : TaskPriorities.Medium,
                    AssigneeId = assigneeId,
                    ParentTaskId = parentTaskId,
                    DueDate = dueDate?.Date,
                    SortOrder = NextSort(projectId),
                    CreatedBy = userId,
                    CreatedAt = DateTime.Now
                };
                _tasks.Add(task);

                if (parent == null)
                {
                    Log(projectId, userId, $"đã tạo nhiệm vụ {project.Key}-{task.Number}: {title}");
                    return ProjectActionResult.Ok($"Đã tạo nhiệm vụ {project.Key}-{task.Number}.", task.Id);
                }

                Log(projectId, userId, $"đã thêm việc con \"{title}\" vào {project.Key}-{parent.Number}");
                return ProjectActionResult.Ok("Đã thêm việc con.", task.Id);
            }
        }

        public ProjectActionResult MoveTask(int userId, int taskId, string? status)
        {
            lock (_gate)
            {
                var task = _tasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null) return ProjectActionResult.Fail("Không tìm thấy nhiệm vụ.");
                if (!CanEdit(task.ProjectId, userId)) return ProjectActionResult.Fail("Bạn không có quyền chuyển nhiệm vụ.");
                if (task.ParentTaskId != null) return ProjectActionResult.Fail("Việc con được tick trực tiếp trong chi tiết nhiệm vụ.");
                if (status == null || !TaskStatuses.All.Contains(status)) return ProjectActionResult.Fail("Trạng thái không hợp lệ.");
                if (task.Status == status) return ProjectActionResult.Ok("Nhiệm vụ đã nằm trong cột này.");

                var project = _projects.First(p => p.Id == task.ProjectId);
                var old = task.Status;
                task.Status = status;
                task.SortOrder = NextSort(task.ProjectId);
                Log(task.ProjectId, userId, $"đã chuyển {project.Key}-{task.Number} từ \"{ColumnTitle(old)}\" sang \"{ColumnTitle(status)}\"");
                return ProjectActionResult.Ok($"Đã chuyển {project.Key}-{task.Number} sang \"{ColumnTitle(status)}\".");
            }
        }

        public ProjectActionResult AssignTask(int userId, int taskId, int? assigneeId)
        {
            lock (_gate)
            {
                var task = _tasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null) return ProjectActionResult.Fail("Không tìm thấy nhiệm vụ.");
                if (!IsManager(task.ProjectId, userId)) return ProjectActionResult.Fail("Chỉ Manager mới giao việc được.");
                if (assigneeId.HasValue && !IsAssignable(task.ProjectId, assigneeId.Value))
                    return ProjectActionResult.Fail("Chỉ giao việc cho Manager hoặc Member của dự án.");

                var project = _projects.First(p => p.Id == task.ProjectId);
                task.AssigneeId = assigneeId;

                if (assigneeId.HasValue)
                {
                    Log(task.ProjectId, userId, $"đã giao {project.Key}-{task.Number} cho {UserName(assigneeId.Value)}");
                    return ProjectActionResult.Ok($"Đã giao {project.Key}-{task.Number} cho {UserName(assigneeId.Value)}.");
                }

                Log(task.ProjectId, userId, $"đã bỏ giao {project.Key}-{task.Number}");
                return ProjectActionResult.Ok($"Đã bỏ giao {project.Key}-{task.Number}.");
            }
        }

        public ProjectActionResult ToggleSubtask(int userId, int taskId)
        {
            lock (_gate)
            {
                var task = _tasks.FirstOrDefault(t => t.Id == taskId && t.ParentTaskId != null);
                if (task == null) return ProjectActionResult.Fail("Không tìm thấy việc con.");
                if (!CanEdit(task.ProjectId, userId)) return ProjectActionResult.Fail("Bạn không có quyền cập nhật việc con.");

                task.Status = task.Status == TaskStatuses.Done ? TaskStatuses.Todo : TaskStatuses.Done;
                return ProjectActionResult.Ok(task.Status == TaskStatuses.Done
                    ? $"Đã hoàn thành \"{task.Title}\"."
                    : $"Đã mở lại \"{task.Title}\".");
            }
        }

        public ProjectActionResult DeleteTask(int userId, int taskId)
        {
            lock (_gate)
            {
                var task = _tasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null) return ProjectActionResult.Fail("Không tìm thấy nhiệm vụ.");
                if (!IsManager(task.ProjectId, userId)) return ProjectActionResult.Fail("Chỉ Manager mới xóa được nhiệm vụ.");

                var project = _projects.First(p => p.Id == task.ProjectId);
                _tasks.RemoveAll(t => t.Id == task.Id || t.ParentTaskId == task.Id);
                Log(task.ProjectId, userId, $"đã xóa nhiệm vụ {project.Key}-{task.Number}");
                return ProjectActionResult.Ok($"Đã xóa nhiệm vụ {project.Key}-{task.Number}.");
            }
        }

        // =================================================================
        // THÀNH VIÊN & LỜI MỜI
        // =================================================================
        public ProjectActionResult InviteMember(int userId, int projectId, string? email, string? role)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();
            if (!IsValidEmail(email)) return ProjectActionResult.Fail("Email không hợp lệ.");
            var finalRole = role != null && ProjectRoles.All.Contains(role) ? role : ProjectRoles.Member;

            lock (_gate)
            {
                if (!_projects.Any(p => p.Id == projectId)) return ProjectActionResult.Fail("Không tìm thấy dự án.");
                if (!IsManager(projectId, userId)) return ProjectActionResult.Fail("Chỉ Manager mới mời được thành viên.");

                var alreadyMember = _members.Any(m => m.ProjectId == projectId &&
                    _users.Any(u => u.Id == m.UserId && u.Email == email));
                if (alreadyMember) return ProjectActionResult.Fail("Người này đã là thành viên của dự án.");

                if (_invitations.Any(i => i.ProjectId == projectId && i.Email == email && i.Status == InvitationStatuses.Pending))
                    return ProjectActionResult.Fail("Email này đã có lời mời đang chờ.");

                _invitations.Add(new ProjectInvitationEntity
                {
                    Id = NextId(_invitations, i => i.Id), ProjectId = projectId, Email = email,
                    Role = finalRole, InvitedBy = userId, Status = InvitationStatuses.Pending, CreatedAt = DateTime.Now
                });
                Log(projectId, userId, $"đã mời {email} tham gia với vai trò {finalRole}");
                return ProjectActionResult.Ok($"Đã tạo lời mời cho {email}. Hệ thống chưa gửi email thật vì chưa tích hợp gửi thư.");
            }
        }

        public ProjectActionResult CancelInvitation(int userId, int invitationId)
        {
            lock (_gate)
            {
                var inv = _invitations.FirstOrDefault(i => i.Id == invitationId && i.Status == InvitationStatuses.Pending);
                if (inv == null) return ProjectActionResult.Fail("Không tìm thấy lời mời đang chờ.");
                if (!IsManager(inv.ProjectId, userId)) return ProjectActionResult.Fail("Chỉ Manager mới hủy được lời mời.");

                _invitations.Remove(inv);
                Log(inv.ProjectId, userId, $"đã hủy lời mời {inv.Email}");
                return ProjectActionResult.Ok($"Đã hủy lời mời {inv.Email}.");
            }
        }

        /// <summary>
        /// GIẢ LẬP hành động của người được mời (bấm link trong email).
        /// Khi có đăng nhập, bước này sẽ do chính người được mời thực hiện.
        /// </summary>
        public ProjectActionResult AcceptInvitation(int invitationId)
        {
            lock (_gate)
            {
                var inv = _invitations.FirstOrDefault(i => i.Id == invitationId && i.Status == InvitationStatuses.Pending);
                if (inv == null) return ProjectActionResult.Fail("Không tìm thấy lời mời đang chờ.");

                var user = _users.FirstOrDefault(u => u.Email == inv.Email);
                if (user == null)
                {
                    var local = inv.Email.Split('@')[0];
                    user = new UserEntity
                    {
                        Id = NextId(_users, u => u.Id),
                        Name = char.ToUpperInvariant(local[0]) + local.Substring(1),
                        Email = inv.Email
                    };
                    _users.Add(user);
                }

                if (!_members.Any(m => m.ProjectId == inv.ProjectId && m.UserId == user.Id))
                {
                    _members.Add(new ProjectMemberEntity
                    {
                        ProjectId = inv.ProjectId, UserId = user.Id, Role = inv.Role, JoinedAt = DateTime.Now
                    });
                }

                inv.Status = InvitationStatuses.Accepted;
                Log(inv.ProjectId, user.Id, "đã tham gia dự án");
                return ProjectActionResult.Ok($"{user.Name} đã tham gia dự án (giả lập chấp nhận lời mời).");
            }
        }

        public ProjectActionResult RemoveMember(int userId, int projectId, int memberUserId)
        {
            lock (_gate)
            {
                if (!IsManager(projectId, userId)) return ProjectActionResult.Fail("Chỉ Manager mới xóa được thành viên.");
                if (memberUserId == userId) return ProjectActionResult.Fail("Bạn không thể tự xóa mình khỏi dự án.");

                var member = _members.FirstOrDefault(m => m.ProjectId == projectId && m.UserId == memberUserId);
                if (member == null) return ProjectActionResult.Fail("Không tìm thấy thành viên.");

                foreach (var t in _tasks.Where(t => t.ProjectId == projectId && t.AssigneeId == memberUserId))
                    t.AssigneeId = null;

                _members.Remove(member);
                Log(projectId, userId, $"đã xóa {UserName(memberUserId)} khỏi dự án");
                return ProjectActionResult.Ok($"Đã xóa {UserName(memberUserId)} khỏi dự án. Các nhiệm vụ của họ chuyển về trạng thái chưa giao.");
            }
        }

        // =================================================================
        // HÀM HỖ TRỢ (private)
        // =================================================================
        private string? RoleOf(int projectId, int userId) =>
            _members.FirstOrDefault(m => m.ProjectId == projectId && m.UserId == userId)?.Role;

        private bool IsManager(int projectId, int userId) => RoleOf(projectId, userId) == ProjectRoles.Manager;

        private bool CanEdit(int projectId, int userId)
        {
            var role = RoleOf(projectId, userId);
            return role == ProjectRoles.Manager || role == ProjectRoles.Member;
        }

        private bool IsAssignable(int projectId, int userId) => CanEdit(projectId, userId);

        private string UserName(int id) => _users.FirstOrDefault(u => u.Id == id)?.Name ?? "Ai đó";

        private static int NextId<T>(IEnumerable<T> items, Func<T, int> idSelector) =>
            items.Any() ? items.Max(idSelector) + 1 : 1;

        private int NextSort(int projectId) =>
            _tasks.Where(t => t.ProjectId == projectId).Select(t => t.SortOrder).DefaultIfEmpty(0).Max() + 1;

        private void Log(int projectId, int userId, string text)
        {
            _activities.Add(new ProjectActivityEntity
            {
                Id = NextId(_activities, a => a.Id), ProjectId = projectId, UserId = userId,
                Text = text, CreatedAt = DateTime.Now
            });
        }

        private static string ColumnTitle(string status) => status switch
        {
            TaskStatuses.Todo => "Cần làm",
            TaskStatuses.InProgress => "Đang làm",
            TaskStatuses.Review => "Chờ duyệt",
            TaskStatuses.Done => "Hoàn thành",
            _ => status
        };

        private static string PriorityLabel(string priority) => priority switch
        {
            TaskPriorities.Low => "Thấp",
            TaskPriorities.High => "Cao",
            TaskPriorities.Urgent => "Khẩn cấp",
            _ => "Trung bình"
        };

        private ProjectCardItem ToCard(ProjectEntity p, int userId)
        {
            var top = _tasks.Where(t => t.ProjectId == p.Id && t.ParentTaskId == null).ToList();
            var done = top.Count(t => t.Status == TaskStatuses.Done);
            var names = _members.Where(m => m.ProjectId == p.Id).Select(m => UserName(m.UserId)).ToList();

            return new ProjectCardItem
            {
                Id = p.Id,
                Key = p.Key,
                Name = p.Name,
                Description = p.Description,
                Role = RoleOf(p.Id, userId) ?? ProjectRoles.Viewer,
                MemberNames = names,
                MemberCount = names.Count,
                TotalTasks = top.Count,
                CompletedTasks = done,
                ProgressPercent = top.Count == 0 ? 0 : (int)Math.Round(done * 100.0 / top.Count),
                Status = p.Status,
                DueDate = p.DueDate
            };
        }

        private ProjectMilestoneItem ToMilestone(ProjectMilestoneEntity ms, int userId) => new()
        {
            Id = ms.Id,
            ProjectId = ms.ProjectId,
            ProjectName = _projects.First(p => p.Id == ms.ProjectId).Name,
            Title = ms.Title,
            DueDate = ms.DueDate,
            IsCompleted = ms.IsCompleted,
            CanToggle = CanEdit(ms.ProjectId, userId)
        };

        private TaskCardItem ToTaskCard(ProjectEntity p, ProjectTaskEntity t)
        {
            var assignee = t.AssigneeId.HasValue ? _users.FirstOrDefault(u => u.Id == t.AssigneeId.Value) : null;
            var subs = _tasks.Where(s => s.ParentTaskId == t.Id).OrderBy(s => s.SortOrder)
                .Select(s => new SubTaskItem { Id = s.Id, Title = s.Title, IsDone = s.Status == TaskStatuses.Done })
                .ToList();

            return new TaskCardItem
            {
                Id = t.Id,
                Key = $"{p.Key}-{t.Number}",
                Title = t.Title,
                Description = t.Description,
                StatusKey = t.Status,
                StatusLabel = ColumnTitle(t.Status),
                PriorityKey = t.Priority,
                PriorityLabel = PriorityLabel(t.Priority),
                AssigneeId = t.AssigneeId,
                AssigneeName = assignee?.Name ?? string.Empty,
                AssigneeInitial = assignee == null || assignee.Name.Length == 0 ? string.Empty : assignee.Name.Substring(0, 1).ToUpperInvariant(),
                DueDate = t.DueDate,
                IsOverdue = t.DueDate.HasValue && t.Status != TaskStatuses.Done && t.DueDate.Value.Date < DateTime.Today,
                SubTasks = subs
            };
        }

        private static bool IsValidEmail(string email) =>
            MailAddress.TryCreate(email, out var addr) && addr.Address == email;

        private static string RemoveDiacritics(string text)
        {
            var sb = new StringBuilder();
            foreach (var c in text.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            }
            return sb.ToString().Replace('đ', 'd').Replace('Đ', 'D');
        }

        private static string SanitizeKey(string raw)
        {
            var s = new string(RemoveDiacritics(raw).ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
            return s.Length > 6 ? s.Substring(0, 6) : s;
        }

        private static string MakeKey(string name)
        {
            var words = RemoveDiacritics(name).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var raw = words.Length == 1 ? words[0] : string.Concat(words.Select(w => w[0]));
            return SanitizeKey(raw);
        }

        private string UniqueKey(string key)
        {
            if (key.Length < 2) key = "PRJ";
            var candidate = key;
            var i = 2;
            while (_projects.Any(p => p.Key == candidate)) candidate = key + i++;
            return candidate;
        }
    }
}