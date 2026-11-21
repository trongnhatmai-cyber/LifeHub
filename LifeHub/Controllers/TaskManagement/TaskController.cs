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
            var model = new TaskOverviewViewModel { Tasks=tasks, Projects=projects };

            return View(model);
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

        private async Task<bool> CanEditProjectAsync(int projectId,int userId)
        {
            var isOwner=await _context.Projects.AnyAsync(p=>p.Id==projectId&&p.CreatedBy==userId);if(isOwner)return true;
            return await (from member in _context.ProjectMembers join role in _context.ProjectRoles on member.ProjectRoleId equals role.Id where member.ProjectId==projectId&&member.UserId==userId select role.Name).AnyAsync(name=>name.ToLower()=="manager"||name.ToLower()=="member");
        }
        private static bool AllowedStatus(string? status)=>status is "todo" or "in_progress" or "review" or "done";
        private static bool AllowedPriority(string? priority)=>priority is "urgent" or "high" or "medium" or "low";
    }
}
