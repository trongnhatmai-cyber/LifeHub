using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.ViewModels.TaskManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers
{
    [Authorize]
    public class TaskController : Controller
    {
        private readonly ApplicationDbContext _context;
        public TaskController(ApplicationDbContext context) => _context = context;
        // GET /Task
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Công việc";

            // Giá trị này phải trùng với điều kiện active trong _Layout.cshtml
            ViewData["ActivePage"] = "Task";

            var userId = User.GetUserId();
            var tasks = await _context.Tasks.AsNoTracking()
                .Where(t => t.UserId == userId || t.AssignedTo == userId)
                .OrderBy(t => t.Deadline).ThenByDescending(t => t.CreatedAt)
                .Select(t => new TaskItemViewModel { Id=t.Id, Title=t.Title, Description=t.Description ?? "", Status=t.Status,
                    Priority=t.Priority, Deadline=t.Deadline, ProjectId=t.ProjectId, EstimatedMinutes=t.EstimatedMinutes,
                    CategoryId=t.CategoryId, CategoryName=t.CategoryId.HasValue ? _context.TaskCategories.Where(c => c.Id == t.CategoryId && c.UserId == userId).Select(c => c.Name).FirstOrDefault() ?? "" : "",
                    ProjectName=_context.Projects.Where(p => p.Id == t.ProjectId).Select(p => p.Name).FirstOrDefault() ?? "",
                    CourseCode=_context.Courses.Where(c => c.Id == t.CourseId && c.UserId == userId).Select(c => c.CourseCode).FirstOrDefault() ?? "",
                    AssignedTo=t.AssignedTo.HasValue ? _context.Users.Where(u => u.Id == t.AssignedTo).Select(u => u.FullName).FirstOrDefault() ?? "Thành viên" : "Bạn",
                    AssignedBy=t.AssignedTo == userId && t.UserId != userId ? _context.Users.Where(u => u.Id == t.UserId).Select(u => u.FullName).FirstOrDefault() ?? "Thành viên" : "",
                    SubtaskTotal=_context.Tasks.Count(s => s.ParentTaskId == t.Id), SubtaskDone=_context.Tasks.Count(s => s.ParentTaskId == t.Id && s.Status == "done") }).ToListAsync();
            var taskIds = tasks.Select(t => t.Id).ToArray();
            var tags = await (from tt in _context.TaskTags.AsNoTracking()
                              join tag in _context.Tags.AsNoTracking() on tt.TagId equals tag.Id
                              where taskIds.Contains(tt.TaskId) && tag.UserId == userId
                              select new { tt.TaskId, tag.Name, tag.Color }).ToListAsync();
            var comments = await _context.TaskComments.AsNoTracking().Where(c => taskIds.Contains(c.TaskId))
                .OrderBy(c => c.CreatedAt).Select(c => new { c.TaskId, Item = new TaskCommentItem { Id=c.Id,
                    AuthorName=_context.Users.Where(u => u.Id == c.UserId).Select(u => u.FullName).FirstOrDefault() ?? "Thành viên",
                    Content=c.Content, CreatedAt=c.CreatedAt } }).ToListAsync();
            foreach (var task in tasks)
            {
                task.Tags = tags.Where(t => t.TaskId == task.Id).Select(t => new TaskTagItem { Name=t.Name, Color=t.Color ?? "#ea580c" }).ToList();
                task.Comments = comments.Where(c => c.TaskId == task.Id).Select(c => c.Item).ToList();
            }
            var projectIds = await _context.ProjectMembers.AsNoTracking().Where(pm => pm.UserId == userId).Select(pm => pm.ProjectId).ToListAsync();
            var projects = await _context.Projects.AsNoTracking().Where(p => projectIds.Contains(p.Id)).Select(p => new TaskProjectOption {
                Id=p.Id, Name=p.Name, Members=_context.ProjectMembers.Where(pm => pm.ProjectId == p.Id).Select(pm => new TaskMemberOption {
                    UserId=pm.UserId, Name=_context.Users.Where(u => u.Id == pm.UserId).Select(u => u.FullName).FirstOrDefault() ?? "Thành viên",
                    Role=_context.ProjectRoles.Where(r => r.Id == pm.ProjectRoleId).Select(r => r.Name).FirstOrDefault() ?? "member" }).ToList() }).ToListAsync();
            var categories = await _context.TaskCategories.AsNoTracking().Where(c => c.UserId == userId)
                .OrderBy(c => c.Name).Select(c => new TaskCategoryOption { Id = c.Id, Name = c.Name, Color = c.Color }).ToListAsync();
            var model = new TaskOverviewViewModel { Tasks=tasks, Projects=projects, Categories=categories };

            return View(model);
        }

        // GET /Task/Task?scope=personal|assigned|project
        public async Task<IActionResult> Task(string? scope)
        {
            ViewData["Title"] = "Danh sách công việc";
            ViewData["ActivePage"] = "TaskList";
            var userId = User.GetUserId();
            var scopeKey = scope?.ToLowerInvariant();
            if (scopeKey is not ("personal" or "assigned" or "project")) scopeKey = string.Empty;

            var projectIds = await _context.ProjectMembers.AsNoTracking().Where(m => m.UserId == userId).Select(m => m.ProjectId).ToListAsync();
            projectIds.AddRange(await _context.Projects.AsNoTracking().Where(p => p.CreatedBy == userId).Select(p => p.Id).ToListAsync());
            projectIds = projectIds.Distinct().ToList();
            var allTasks = await _context.Tasks.AsNoTracking()
                .Where(t => t.UserId == userId || t.AssignedTo == userId || (t.ProjectId.HasValue && projectIds.Contains(t.ProjectId.Value)))
                .OrderBy(t => t.Deadline).ThenByDescending(t => t.CreatedAt).ToListAsync();
            var visibleTasks = scopeKey switch
            {
                "personal" => allTasks.Where(t => t.UserId == userId && t.ProjectId == null && t.AssignedTo != userId),
                "assigned" => allTasks.Where(t => t.AssignedTo == userId && t.UserId != userId),
                "project" => allTasks.Where(t => t.ProjectId.HasValue),
                _ => allTasks.AsEnumerable()
            };
            var categories = await _context.TaskCategories.AsNoTracking().Where(c => c.UserId == userId).OrderBy(c => c.Name).ToListAsync();
            var categoryLookup = categories.ToDictionary(c => c.Id);
            var projectLookup = await _context.Projects.AsNoTracking().Where(p => projectIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);
            var taskIds = allTasks.Select(t => t.Id).ToArray();
            var tags = await (from link in _context.TaskTags.AsNoTracking()
                              join tag in _context.Tags.AsNoTracking() on link.TagId equals tag.Id
                              where taskIds.Contains(link.TaskId)
                              select new { link.TaskId, tag.Name, tag.Color }).ToListAsync();
            var comments = await _context.TaskComments.AsNoTracking().Where(c => taskIds.Contains(c.TaskId)).OrderBy(c => c.CreatedAt)
                .Select(c => new { c.TaskId, Item = new TaskCommentItem { Id = c.Id, AuthorName = _context.Users.Where(u => u.Id == c.UserId).Select(u => u.FullName).FirstOrDefault() ?? "Thành viên", Content = c.Content, CreatedAt = c.CreatedAt } })
                .ToListAsync();
            var subtaskCounts = await _context.Tasks.AsNoTracking().Where(t => t.ParentTaskId.HasValue && taskIds.Contains(t.ParentTaskId.Value))
                .GroupBy(t => t.ParentTaskId!.Value).Select(g => new { Id = g.Key, Total = g.Count(), Done = g.Count(t => t.Status == "done") }).ToDictionaryAsync(x => x.Id);

            var taskItems = visibleTasks.Select(t =>
            {
                var item = new TaskItemViewModel
                {
                    Id = t.Id, Title = t.Title, Description = t.Description ?? "", Status = t.Status, Priority = t.Priority,
                    Deadline = t.Deadline, CreatedAt = t.CreatedAt, CompletedAt = t.CompletedAt, ProjectId = t.ProjectId,
                    ProjectName = t.ProjectId.HasValue && projectLookup.TryGetValue(t.ProjectId.Value, out var projectName) ? projectName : "",
                    CategoryId = t.CategoryId,
                    CategoryName = t.CategoryId.HasValue && categoryLookup.TryGetValue(t.CategoryId.Value, out var category) ? category.Name : "",
                    AssignedTo = t.AssignedTo.HasValue ? _context.Users.Where(u => u.Id == t.AssignedTo).Select(u => u.FullName).FirstOrDefault() ?? "Thành viên" : "Bạn",
                    AssignedBy = t.AssignedTo == userId && t.UserId != userId ? _context.Users.Where(u => u.Id == t.UserId).Select(u => u.FullName).FirstOrDefault() ?? "Thành viên" : "",
                    EstimatedMinutes = t.EstimatedMinutes,
                    SubtaskTotal = subtaskCounts.TryGetValue(t.Id, out var counts) ? counts.Total : 0,
                    SubtaskDone = subtaskCounts.TryGetValue(t.Id, out counts) ? counts.Done : 0,
                    Tags = tags.Where(x => x.TaskId == t.Id).Select(x => new TaskTagItem { Name = x.Name, Color = x.Color ?? "#ea580c" }).ToList(),
                    Comments = comments.Where(x => x.TaskId == t.Id).Select(x => x.Item).ToList()
                };
                return item;
            }).ToList();

            var projects = await (from project in _context.Projects.AsNoTracking()
                                  where projectIds.Contains(project.Id)
                                  select new TaskProjectOption
                                  {
                                      Id = project.Id,
                                      Name = project.Name,
                                      Members = _context.ProjectMembers.Where(m => m.ProjectId == project.Id).Select(m => new TaskMemberOption
                                      {
                                          UserId = m.UserId,
                                          Name = _context.Users.Where(u => u.Id == m.UserId).Select(u => u.FullName).FirstOrDefault() ?? "Thành viên",
                                          Role = _context.ProjectRoles.Where(r => r.Id == m.ProjectRoleId).Select(r => r.Name).FirstOrDefault() ?? "member"
                                      }).ToList()
                                  }).ToListAsync();

            return View("~/Views/Task/Task.cshtml", new TaskListViewModel
            {
                Tasks = taskItems,
                Projects = projects,
                Categories = categories.Select(c => new TaskCategoryOption { Id = c.Id, Name = c.Name, Color = c.Color }).ToList()
            });
        }

        // GET /Task/Category
        public async Task<IActionResult> Category()
        {
            ViewData["Title"] = "Danh mục công việc";
            ViewData["ActivePage"] = "TaskCategory";
            var userId = User.GetUserId();
            var categories = await _context.TaskCategories.AsNoTracking().Where(c => c.UserId == userId).OrderBy(c => c.Name).ToListAsync();
            var counts = await _context.Tasks.AsNoTracking().Where(t => t.UserId == userId && t.ProjectId == null && t.CategoryId.HasValue)
                .GroupBy(t => t.CategoryId!.Value).Select(g => new { Id = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count);
            var uncategorized = await _context.Tasks.CountAsync(t => t.UserId == userId && t.ProjectId == null && t.CategoryId == null);
            return View("~/Views/Task/Category.cshtml", new TaskCategoryViewModel
            {
                UncategorizedTasksCount = uncategorized,
                Categories = categories.Select(c => new TaskCategoryItemViewModel
                {
                    Id = c.Id, Name = c.Name, Description = c.Description ?? "", Color = c.Color, Icon = c.Icon,
                    TaskCount = counts.GetValueOrDefault(c.Id)
                }).ToList()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTask(TaskInputModels input)
        {
            var userId = User.GetUserId();
            input.Title = input.Title?.Trim() ?? string.Empty;
            var isProject = input.TaskType == "project";
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(input.Title) || input.Description?.Length > 4000 ||
                input.Tags?.Length > 500 || input.TaskType is not ("personal" or "project") ||
                input.EstimatedMinutes is <= 0 || !AllowedStatus(input.Status) || !AllowedPriority(input.Priority) ||
                (isProject && !input.ProjectId.HasValue) || (!isProject && input.ProjectId.HasValue))
            {
                TempData["ErrorMessage"] = "Thông tin công việc không hợp lệ.";
                return RedirectToAction(nameof(Task));
            }

            if (input.CategoryId.HasValue && (isProject || !await _context.TaskCategories.AnyAsync(c => c.Id == input.CategoryId && c.UserId == userId)))
            {TempData["ErrorMessage"] = "Danh mục công việc không hợp lệ.";return RedirectToAction(nameof(Task));}

            LifeHub.Models.TaskManagement.Task? task = null;
            if (input.Id > 0)
            {
                task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == input.Id &&
                    (t.UserId == userId || (t.ProjectId.HasValue && _context.ProjectMembers.Any(m => m.ProjectId == t.ProjectId && m.UserId == userId))));
                if (task == null || (task.ProjectId.HasValue && !await CanEditProjectAsync(task.ProjectId.Value, userId))) return NotFound();
            }

            int? assignedTo = null;
            if (isProject)
            {
                var projectId = input.ProjectId!.Value;
                if (!await CanEditProjectAsync(projectId, userId)) return NotFound();
                assignedTo = input.AssignedTo ?? userId;
                var isMember = await (from member in _context.ProjectMembers
                                      join role in _context.ProjectRoles on member.ProjectRoleId equals role.Id
                                      where member.ProjectId == projectId && member.UserId == assignedTo
                                      select role.Name).AnyAsync(role => role.ToLower() == "member" || role.ToLower() == "manager");
                var isOwner = await _context.Projects.AnyAsync(p => p.Id == projectId && p.CreatedBy == assignedTo);
                if (!isMember && !isOwner)
                {TempData["ErrorMessage"] = "Người thực hiện phải thuộc dự án.";return RedirectToAction(nameof(Task));}
            }

            var isNew = task == null;
            task ??= new LifeHub.Models.TaskManagement.Task { UserId = userId, CreatedAt = DateTime.Now };
            task.ProjectId = isProject ? input.ProjectId : null;
            task.AssignedTo = isProject ? assignedTo : null;
            task.CategoryId = isProject ? null : input.CategoryId;
            task.Title = input.Title;
            task.Description = input.Description?.Trim();
            task.Status = input.Status;
            task.Priority = input.Priority;
            task.Deadline = input.Deadline;
            task.EstimatedMinutes = input.EstimatedMinutes;
            task.CompletedAt = input.Status == "done" ? task.CompletedAt ?? DateTime.Now : null;
            task.UpdatedAt = DateTime.Now;
            if (isNew) _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            await SaveTaskTagsAsync(task.Id, input.Tags, userId);
            if (task.ProjectId.HasValue)
                _context.ProjectActivities.Add(new LifeHub.Models.ProjectManagement.ProjectActivity { ProjectId = task.ProjectId.Value, UserId = userId, Action = isNew ? "task_created" : "task_updated", Detail = $"Đã {(isNew ? "tạo" : "cập nhật")} nhiệm vụ: {task.Title}"[..Math.Min(255, $"Đã {(isNew ? "tạo" : "cập nhật")} nhiệm vụ: {task.Title}".Length)], TargetType = "task", TargetId = task.Id, CreatedAt = DateTime.Now });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = isNew ? "Đã thêm công việc." : "Đã cập nhật công việc.";
            return RedirectToAction(nameof(Task));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = User.GetUserId();
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id &&
                (t.UserId == userId || (t.ProjectId.HasValue && _context.ProjectMembers.Any(m => m.ProjectId == t.ProjectId && m.UserId == userId))));
            if (task == null || (task.ProjectId.HasValue && !await CanEditProjectAsync(task.ProjectId.Value, userId))) return NotFound();
            if (await _context.Tasks.AnyAsync(t => t.ParentTaskId == id))
            {
                TempData["ErrorMessage"] = "Hãy xóa hoặc chuyển công việc con trước khi xóa công việc này.";
                return RedirectToAction(nameof(Task));
            }
            _context.TaskTags.RemoveRange(_context.TaskTags.Where(t => t.TaskId == id));
            _context.TaskComments.RemoveRange(_context.TaskComments.Where(c => c.TaskId == id));
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa công việc.";
            return RedirectToAction(nameof(Task));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCategory(TaskCategoryInputViewModel input)
        {
            var userId = User.GetUserId();
            input.Name = input.Name?.Trim() ?? string.Empty;
            input.Description = input.Description?.Trim();
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(input.Name) || !ValidCategoryColor(input.Color) ||
                !AllowedCategoryIcon(input.Icon) || await _context.TaskCategories.AnyAsync(c => c.UserId == userId && c.Name == input.Name && c.Id != input.Id))
            {
                TempData["ErrorMessage"] = "Thông tin danh mục không hợp lệ hoặc tên đã được sử dụng.";
                return RedirectToAction(nameof(Category));
            }

            LifeHub.Models.TaskManagement.TaskCategory? category = null;
            if (input.Id.HasValue)
            {
                category = await _context.TaskCategories.FirstOrDefaultAsync(c => c.Id == input.Id && c.UserId == userId);
                if (category == null) return NotFound();
            }
            var isNew = category == null;
            category ??= new LifeHub.Models.TaskManagement.TaskCategory { UserId = userId };
            category.Name = input.Name;
            category.Description = input.Description;
            category.Color = input.Color;
            category.Icon = input.Icon;
            if (isNew) _context.TaskCategories.Add(category);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = isNew ? "Đã thêm danh mục." : "Đã cập nhật danh mục.";
            return RedirectToAction(nameof(Category));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var userId = User.GetUserId();
            var category = await _context.TaskCategories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            if (category == null) return NotFound();
            if (await _context.Tasks.AnyAsync(t => t.UserId == userId && t.CategoryId == id))
            {
                TempData["ErrorMessage"] = "Danh mục đang được công việc sử dụng. Hãy chuyển các công việc sang danh mục khác trước khi xóa.";
                return RedirectToAction(nameof(Category));
            }
            _context.TaskCategories.Remove(category);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa danh mục.";
            return RedirectToAction(nameof(Category));
        }

        // POST /Task/Create  (form "Thêm công việc")
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskInputModels input)
        {
            var uid=User.GetUserId();input.Title=input.Title?.Trim()??"";
            if(!ModelState.IsValid||!AllowedStatus(input.Status)||!AllowedPriority(input.Priority)||input.Description?.Length>4000||input.Tags?.Length>500)
            {TempData["ErrorMessage"]="Thông tin công việc không hợp lệ.";return RedirectToAction(nameof(Index));}
            int? assigned=null;
            if(input.ProjectId.HasValue)
            {
                if(!await CanEditProjectAsync(input.ProjectId.Value,uid))return NotFound();
                var memberIds=await (from member in _context.ProjectMembers join role in _context.ProjectRoles on member.ProjectRoleId equals role.Id where member.ProjectId==input.ProjectId.Value select new{member.UserId,role.Name}).ToListAsync();
                assigned=input.AssignedTo??uid;
                if(!memberIds.Any(m=>m.UserId==assigned&&m.Name.ToLower()=="member"||m.UserId==assigned&&m.Name.ToLower()=="manager"))return NotFound();
            }
            else if(input.AssignedTo.HasValue&&input.AssignedTo.Value!=uid)return BadRequest();
            var task=new LifeHub.Models.TaskManagement.Task{UserId=uid,ProjectId=input.ProjectId,AssignedTo=assigned,Title=input.Title,Description=input.Description?.Trim(),Status=input.Status,Priority=input.Priority,Deadline=input.Deadline,EstimatedMinutes=input.EstimatedMinutes,CreatedAt=DateTime.Now};
            _context.Tasks.Add(task);await _context.SaveChangesAsync();
            if(task.ProjectId.HasValue)_context.ProjectActivities.Add(new LifeHub.Models.ProjectManagement.ProjectActivity{ProjectId=task.ProjectId.Value,UserId=uid,Action="task_created",Detail=$"Đã tạo nhiệm vụ: {task.Title}".Substring(0,Math.Min(255,$"Đã tạo nhiệm vụ: {task.Title}".Length)),TargetType="task",TargetId=task.Id,CreatedAt=DateTime.Now});
            foreach(var name in (input.Tags??"").Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).Take(10))
            {if(name.Length>40)continue;var tag=await _context.Tags.FirstOrDefaultAsync(t=>t.UserId==uid&&t.Name==name);if(tag==null){tag=new LifeHub.Models.TaskManagement.Tag{UserId=uid,Name=name,Color="#ea580c"};_context.Tags.Add(tag);await _context.SaveChangesAsync();}_context.TaskTags.Add(new LifeHub.Models.TaskManagement.TaskTag{TaskId=task.Id,TagId=tag.Id});}
            await _context.SaveChangesAsync();return RedirectToAction(nameof(Index));
        }

        // POST /Task/UpdateStatus  (kéo thả thẻ sang cột khác trên Kanban)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(TaskInputModels input)
        {
            var uid=User.GetUserId();if(!ModelState.IsValid||!AllowedStatus(input.Status))return BadRequest();
            var task=await _context.Tasks.FirstOrDefaultAsync(t=>t.Id==input.Id&&t.ProjectId==null&&t.UserId==uid);
            if(task==null)
            {
                var shared=await _context.Tasks.FirstOrDefaultAsync(t=>t.Id==input.Id&&t.ProjectId.HasValue&&_context.ProjectMembers.Any(m=>m.ProjectId==t.ProjectId&&m.UserId==uid));
                if(shared==null||!await CanEditProjectAsync(shared.ProjectId!.Value,uid))return NotFound();task=shared;
            }
            task.Status=input.Status;task.CompletedAt=input.Status=="done"?DateTime.Now:null;task.UpdatedAt=DateTime.Now;
            if(task.ProjectId.HasValue)_context.ProjectActivities.Add(new LifeHub.Models.ProjectManagement.ProjectActivity{ProjectId=task.ProjectId.Value,UserId=uid,Action="task_status",Detail=$"Đã cập nhật trạng thái nhiệm vụ #{task.Id}: {task.Status}",TargetType="task",TargetId=task.Id,CreatedAt=DateTime.Now});
            await _context.SaveChangesAsync();
            return Request.Headers.XRequestedWith=="XMLHttpRequest"?Json(new{success=true}):RedirectToAction(nameof(Index));
        }

        // POST /Task/AddComment  (khung "Bình luận & trao đổi" trong chi tiết công việc)
        // Ghi bình luận và hoạt động dự án trong phạm vi task mà người dùng được phép truy cập.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(TaskInputModels input)
        {
            var uid=User.GetUserId();input.Content=input.Content?.Trim()??"";if(!ModelState.IsValid||string.IsNullOrWhiteSpace(input.Content))return BadRequest();
            var task=await _context.Tasks.AsNoTracking().FirstOrDefaultAsync(t=>t.Id==input.TaskId);
            if(task==null)return NotFound();
            if(task.ProjectId.HasValue){if(!await CanEditProjectAsync(task.ProjectId.Value,uid))return NotFound();}
            else if(task.UserId!=uid&&task.AssignedTo!=uid)return NotFound();
            _context.TaskComments.Add(new LifeHub.Models.TaskManagement.TaskComment{TaskId=task.Id,UserId=uid,Content=input.Content,CreatedAt=DateTime.Now});
            if(task.ProjectId.HasValue)_context.ProjectActivities.Add(new LifeHub.Models.ProjectManagement.ProjectActivity{ProjectId=task.ProjectId.Value,UserId=uid,Action="task_comment",Detail=$"Đã bình luận trong nhiệm vụ #{task.Id}",TargetType="task",TargetId=task.Id,CreatedAt=DateTime.Now});
            await _context.SaveChangesAsync();return RedirectToAction(nameof(Index));
        }

        private async Task SaveTaskTagsAsync(int taskId, string? rawTags, int userId)
        {
            _context.TaskTags.RemoveRange(_context.TaskTags.Where(link => link.TaskId == taskId));
            var names = (rawTags ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(name => name.Length <= 50).Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToList();
            foreach (var name in names)
            {
                var tag = await _context.Tags.FirstOrDefaultAsync(t => t.UserId == userId && t.Name == name);
                if (tag == null)
                {
                    tag = new LifeHub.Models.TaskManagement.Tag { UserId = userId, Name = name, Color = "#ea580c" };
                    _context.Tags.Add(tag);
                    await _context.SaveChangesAsync();
                }
                _context.TaskTags.Add(new LifeHub.Models.TaskManagement.TaskTag { TaskId = taskId, TagId = tag.Id });
            }
            await _context.SaveChangesAsync();
        }

        private static bool ValidCategoryColor(string? color) => color is { Length: 7 } && color[0] == '#' && color.Skip(1).All(Uri.IsHexDigit);
        private static bool AllowedCategoryIcon(string? icon) => icon is "folder" or "book" or "briefcase" or "target" or "heart" or "star" or "calendar";

        private async Task<bool> CanEditProjectAsync(int projectId,int userId)
        {
            var isOwner=await _context.Projects.AnyAsync(p=>p.Id==projectId&&p.CreatedBy==userId);if(isOwner)return true;
            return await (from member in _context.ProjectMembers join role in _context.ProjectRoles on member.ProjectRoleId equals role.Id where member.ProjectId==projectId&&member.UserId==userId select role.Name).AnyAsync(name=>name.ToLower()=="manager"||name.ToLower()=="member");
        }
        private static bool AllowedStatus(string? status)=>status is "todo" or "in_progress" or "review" or "done";
        private static bool AllowedPriority(string? priority)=>priority is "urgent" or "high" or "medium" or "low";
    }
}
