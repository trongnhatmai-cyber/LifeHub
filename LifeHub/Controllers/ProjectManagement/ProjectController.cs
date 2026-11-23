using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.ViewModels.ProjectManagement;
using LifeHub.Models.TaskManagement;
using LifeHub.Models.SystemManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;

namespace LifeHub.Controllers
{
    // Chống giả mạo form cho mọi action POST (form trong View đã tự có token).
    [Authorize]
    [AutoValidateAntiforgeryToken]
    public class ProjectController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ProjectController(ApplicationDbContext context) => _context = context;
        [HttpGet]
        public async Task<IActionResult> All()
        {
            ViewData["Title"] = "Tất cả dự án";
            ViewData["ActivePage"] = "ProjectList";
            return View("All", await BuildProjectViewModelAsync());
        }
        // GET /Project
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Tổng quan dự án";
            ViewData["ActivePage"] = "ProjectOverview";
            return View("Index", await BuildProjectViewModelAsync());
        }

        // Dùng chung cho Index (Tổng quan) và All (Danh sách dự án)
        private async Task<ProjectViewModel> BuildProjectViewModelAsync()
        {

            var uid=User.GetUserId();
            var ids=await _context.ProjectMembers.AsNoTracking().Where(m=>m.UserId==uid).Select(m=>m.ProjectId).ToListAsync();
            var projects=await _context.Projects.AsNoTracking().Where(p=>p.CreatedBy==uid||ids.Contains(p.Id)).OrderByDescending(p=>p.UpdatedAt??p.CreatedAt).ToListAsync();
            var pids=projects.Select(p=>p.Id).ToArray();
            var members=await _context.ProjectMembers.AsNoTracking().Where(m=>pids.Contains(m.ProjectId)).ToListAsync();
            var tasks=await _context.Tasks.AsNoTracking().Where(t=>t.ProjectId.HasValue&&pids.Contains(t.ProjectId.Value)).ToListAsync();
            var milestones=await _context.ProjectMilestones.AsNoTracking().Where(m=>pids.Contains(m.ProjectId)).OrderBy(m=>m.DueDate).ToListAsync();
            var roles=await _context.ProjectRoles.AsNoTracking().ToDictionaryAsync(r=>r.Id,r=>r.Name);
            var names=await _context.Users.AsNoTracking().ToDictionaryAsync(u=>u.Id,u=>u.FullName);
            var recentActivities=(await _context.ProjectActivities.AsNoTracking().Where(a=>pids.Contains(a.ProjectId)).OrderByDescending(a=>a.CreatedAt).Take(6).ToListAsync()).Select(a=>new ProjectActivityItem {ProjectId=a.ProjectId,ProjectName=projects.First(p=>p.Id==a.ProjectId).Name,ActorName=names.GetValueOrDefault(a.UserId,"Thành viên"),Text=a.Detail??a.Action,CreatedAt=a.CreatedAt}).ToList();
            var cards=projects.Select(p=>new ProjectCardItem {Id=p.Id,Key=$"LIFE-{p.Id}",Name=p.Name,Description=p.Description??"",Role=p.CreatedBy==uid?"Manager":roles.TryGetValue(members.FirstOrDefault(m=>m.ProjectId==p.Id&&m.UserId==uid)?.ProjectRoleId??0,out var role)?role:"Viewer",MemberCount=members.Count(m=>m.ProjectId==p.Id),ProgressPercent=p.ProgressPercent,Status=DisplayStatus(p.Status),DueDate=p.DueDate?.ToDateTime(TimeOnly.MinValue)??DateTime.MaxValue,TotalTasks=tasks.Count(t=>t.ProjectId==p.Id),CompletedTasks=tasks.Count(t=>t.ProjectId==p.Id&&t.Status=="done"),MemberNames=members.Where(m=>m.ProjectId==p.Id).Select(m=>names.GetValueOrDefault(m.UserId,"Thành viên")).Take(4).ToList(),LastActivityAt=p.UpdatedAt??p.CreatedAt}).ToList();
            return new ProjectViewModel {TotalProjects=cards.Count,ActiveProjects=cards.Count(c=>c.StatusKey=="running"),CompletedProjects=cards.Count(c=>c.StatusKey=="completed"),TotalTeamMembers=members.Select(m=>m.UserId).Distinct().Count(),Projects=cards,RecentProjects=cards.Take(6).ToList(),StatusFilters=new(){new(){Key="all",Label="Tất cả",Count=cards.Count},new(){Key="running",Label="Đang chạy",Count=cards.Count(c=>c.StatusKey=="running")},new(){Key="planning",Label="Lên kế hoạch",Count=cards.Count(c=>c.StatusKey=="planning")},new(){Key="paused",Label="Tạm dừng",Count=cards.Count(c=>c.StatusKey=="paused")},new(){Key="completed",Label="Hoàn thành",Count=cards.Count(c=>c.StatusKey=="completed")},},RecentMilestones=milestones.Where(m=>!m.IsCompleted).Take(8).Select(m=>new ProjectMilestoneItem {Id=m.Id,ProjectId=m.ProjectId,ProjectName=projects.First(p=>p.Id==m.ProjectId).Name,Title=m.Title,DueDate=m.DueDate?.ToDateTime(TimeOnly.MinValue)??DateTime.MinValue,IsCompleted=m.IsCompleted,CanToggle=cards.Any(c=>c.Id==m.ProjectId&&c.Role=="Manager")}).ToList(),RecentActivities=recentActivities};
        }

        // GET /Project/Details/3
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var uid=User.GetUserId();
            var userEmail=await _context.Users.AsNoTracking().Where(u=>u.Id==uid).Select(u=>u.Email).FirstOrDefaultAsync();
            var invited=await _context.ProjectInvitations.AsNoTracking().AnyAsync(i=>i.ProjectId==id&&i.Status=="pending"&&(!i.ExpiresAt.HasValue||i.ExpiresAt>DateTime.Now)&&(i.InvitedUserId==uid||i.InvitedUserId==null&&i.InvitedEmail==userEmail));
            var project=await _context.Projects.AsNoTracking().FirstOrDefaultAsync(p=>p.Id==id&&(p.CreatedBy==uid||_context.ProjectMembers.Any(m=>m.ProjectId==p.Id&&m.UserId==uid)||invited));
            if(project==null)return NotFound();
            var canManage=IsManager(id,uid);
            var pendingInvitations=await _context.ProjectInvitations.AsNoTracking().Where(i=>i.ProjectId==id&&i.Status=="pending"&&(!i.ExpiresAt.HasValue||i.ExpiresAt>DateTime.Now)&&(canManage||i.InvitedUserId==uid||i.InvitedUserId==null&&i.InvitedEmail==userEmail)).OrderByDescending(i=>i.CreatedAt).ToListAsync();
            var memberRows=await _context.ProjectMembers.AsNoTracking().Where(m=>m.ProjectId==id).ToListAsync();
            var roleNames=await _context.ProjectRoles.AsNoTracking().ToDictionaryAsync(r=>r.Id,r=>r.Name);
            var memberIds=memberRows.Select(m=>m.UserId).Append(project.CreatedBy).Append(uid).Concat(pendingInvitations.Select(i=>i.InvitedBy)).Distinct().ToArray();
            var userNames=await _context.Users.AsNoTracking().Where(u=>memberIds.Contains(u.Id)).ToDictionaryAsync(u=>u.Id,u=>u.FullName);
            var taskRows=await _context.Tasks.AsNoTracking().Where(t=>t.ProjectId==id).OrderBy(t=>t.SortOrder).ThenByDescending(t=>t.CreatedAt).ToListAsync();
            var taskIds=taskRows.Select(t=>t.Id).ToArray();
            var comments=await _context.TaskComments.AsNoTracking().Where(c=>taskIds.Contains(c.TaskId)).OrderBy(c=>c.CreatedAt).ToListAsync();
            var milestones=await _context.ProjectMilestones.AsNoTracking().Where(m=>m.ProjectId==id).OrderBy(m=>m.DueDate).ToListAsync();
            var activity=await _context.ProjectActivities.AsNoTracking().Where(a=>a.ProjectId==id).OrderByDescending(a=>a.CreatedAt).Take(30).ToListAsync();
            var myMembership=memberRows.FirstOrDefault(m=>m.UserId==uid);
            var role=project.CreatedBy==uid?"Manager":myMembership==null?"Viewer":roleNames.GetValueOrDefault(myMembership.ProjectRoleId,"Viewer");
            var columns=new[]{("todo","Cần làm"),("in_progress","Đang làm"),("review","Chờ duyệt"),("done","Hoàn thành")}.Select(col=>new BoardColumnItem {Key=col.Item1,Title=col.Item2,Tasks=taskRows.Where(t=>t.Status==col.Item1).Select(t=>new TaskCardItem {Id=t.Id,ProjectId=id,Key=$"LIFE-{t.Id}",Title=t.Title,Description=t.Description??"",StatusKey=t.Status,StatusLabel=TaskStatusLabel(t.Status),PriorityKey=t.Priority,PriorityLabel=t.Priority,AssigneeId=t.AssignedTo,AssigneeName=t.AssignedTo.HasValue?userNames.GetValueOrDefault(t.AssignedTo.Value,"Thành viên"):"Chưa giao",AssigneeInitial=t.AssignedTo.HasValue?userNames.GetValueOrDefault(t.AssignedTo.Value,"?").FirstOrDefault().ToString():"",ReporterName=userNames.GetValueOrDefault(t.UserId,"Thành viên"),DueDate=t.Deadline,IsOverdue=t.Deadline<DateTime.Now&&t.Status!="done",AssignedAt=t.CreatedAt,CreatedAt=t.CreatedAt,UpdatedAt=t.UpdatedAt??t.CreatedAt,CompletedAt=t.CompletedAt,SubTasks=taskRows.Where(s=>s.ParentTaskId==t.Id).Select(s=>new SubTaskItem {Id=s.Id,Title=s.Title,IsDone=s.Status=="done"}).ToList(),Comments=comments.Where(c=>c.TaskId==t.Id).Select(c=>new LifeHub.Models.ViewModels.ProjectManagement.TaskCommentItem {Id=c.Id,AuthorName=userNames.GetValueOrDefault(c.UserId,"Thành viên"),Text=c.Content,CreatedAt=c.CreatedAt}).ToList()}).ToList()}).ToList();
            return View(new ProjectDetailViewModel {Id=id,Key=$"LIFE-{id}",Name=project.Name,Description=project.Description??"",Status=DisplayStatus(project.Status),StartDate=project.StartDate.ToDateTime(TimeOnly.MinValue),DueDate=project.DueDate?.ToDateTime(TimeOnly.MinValue)??DateTime.MaxValue,ProgressPercent=project.ProgressPercent,TotalTasks=taskRows.Count,CompletedTasks=taskRows.Count(t=>t.Status=="done"),CurrentUserId=uid,CurrentUserName=userNames.GetValueOrDefault(uid,""),CurrentRole=role,CanManage=canManage,CanEdit=role.Equals("Manager",StringComparison.OrdinalIgnoreCase)||role.Equals("Member",StringComparison.OrdinalIgnoreCase),Columns=columns,Members=memberRows.Select(m=>new ProjectMemberItem {UserId=m.UserId,Name=userNames.GetValueOrDefault(m.UserId,"Thành viên"),Email=_context.Users.Where(u=>u.Id==m.UserId).Select(u=>u.Email).FirstOrDefault()??"",Role=roleNames.GetValueOrDefault(m.ProjectRoleId,"Member"),IsCurrentUser=m.UserId==uid,OpenTaskCount=taskRows.Count(t=>t.AssignedTo==m.UserId&&t.Status!="done")}).ToList(),Invitations=pendingInvitations.Select(i=>new ProjectInvitationItem{Id=i.Id,Email=i.InvitedEmail,Role=roleNames.GetValueOrDefault(i.ProjectRoleId,"Member"),InvitedByName=userNames.GetValueOrDefault(i.InvitedBy,"Thành viên"),CreatedAt=i.CreatedAt,CanAccept=i.InvitedUserId==uid||i.InvitedUserId==null&&i.InvitedEmail.Equals(userEmail,StringComparison.OrdinalIgnoreCase)}).ToList(),Milestones=milestones.Select(m=>new ProjectMilestoneItem {Id=m.Id,ProjectId=id,ProjectName=project.Name,Title=m.Title,DueDate=m.DueDate?.ToDateTime(TimeOnly.MinValue)??DateTime.MinValue,IsCompleted=m.IsCompleted,CanToggle=canManage}).ToList(),Activities=activity.Select(a=>new ProjectActivityItem {ActorName=userNames.GetValueOrDefault(a.UserId,"Thành viên"),Text=a.Detail??a.Action,CreatedAt=a.CreatedAt}).ToList()});
        }

        private static string DisplayStatus(string status)=>status.ToLowerInvariant() switch {"completed"=>ProjectStatuses.Completed,"paused"=>ProjectStatuses.Paused,"planning"=>ProjectStatuses.Planning,_=>ProjectStatuses.Running};
        private static string TaskStatusLabel(string status)=>status switch {"in_progress"=>"Đang làm","review"=>"Chờ duyệt","done"=>"Hoàn thành",_=>"Cần làm"};

        // =====================================================================
        // Các action POST kiểm tra vai trò và phạm vi project trước khi ghi dữ liệu.
        // =====================================================================

        [HttpPost]
        public IActionResult Create(string name, string? key, string? description,
            DateTime? startDate, DateTime? dueDate, string? status)
        {
            var uid=User.GetUserId(); name=name?.Trim()??""; description=description?.Trim();
            if(!ModelState.IsValid||name.Length is <1 or >100||description?.Length>1000||!startDate.HasValue||!dueDate.HasValue||startDate.Value.Date>dueDate.Value.Date||status is not ("Lên kế hoạch" or "Đang chạy"))
            {TempData["ErrorMessage"]="Hãy kiểm tra tên, ngày bắt đầu, hạn chót và trạng thái dự án.";return RedirectToAction(nameof(All));}
            var p=new LifeHub.Models.ProjectManagement.Project{CreatedBy=uid,Name=name,Description=description,StartDate=DateOnly.FromDateTime(startDate.Value),DueDate=DateOnly.FromDateTime(dueDate.Value),Status=status=="Lên kế hoạch"?"planning":"running",CreatedAt=DateTime.Now};
            _context.Projects.Add(p);_context.SaveChanges();AddActivity(p.Id,uid,"project_created","Đã tạo dự án.");_context.SaveChanges();return RedirectToAction(nameof(Details),new{id=p.Id});
        }

        [HttpPost]
        public IActionResult UpdateStatus(int projectId, string status)
        {var uid=User.GetUserId();if(!IsManager(projectId,uid))return NotFound();var p=_context.Projects.FirstOrDefault(p=>p.Id==projectId);var mapped=NormalizeStatus(status);if(p==null)return NotFound();if(mapped==null)return BadRequest();p.Status=mapped;p.UpdatedAt=DateTime.Now;AddActivity(projectId,uid,"project_status",$"Đã đổi trạng thái dự án thành {DisplayStatus(mapped)}.");_context.SaveChanges();return RedirectToAction(nameof(Details),new{id=projectId});}

        [HttpPost]
        public async Task<IActionResult> InviteMember(int projectId, string email, string role)
        {
            var uid=User.GetUserId();if(!IsManager(projectId,uid))return NotFound();email=email?.Trim().ToLowerInvariant()??"";role=role?.Trim()??"";
            if(email.Length>254||!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email)||role is not ("Member" or "Viewer" or "Manager"))
            {TempData["ProjectMessage"]="Email hoặc vai trò không hợp lệ.";TempData["ProjectMessageType"]="error";return RedirectToAction(nameof(Details),new{id=projectId});}
            var target=await _context.Users.AsNoTracking().FirstOrDefaultAsync(u=>u.Email==email);
            var roleRow=await _context.ProjectRoles.FirstOrDefaultAsync(r=>r.Name.ToLower()==role.ToLower());
            if(target?.Id==uid||target!=null&&_context.Projects.Any(p=>p.Id==projectId&&p.CreatedBy==target.Id))
            {TempData["ProjectMessage"]="Bạn không thể mời chính mình hoặc chủ dự án.";TempData["ProjectMessageType"]="error";return RedirectToAction(nameof(Details),new{id=projectId});}
            if(roleRow==null)
            {
                roleRow=new LifeHub.Models.ProjectManagement.ProjectRole
                {
                    Name=role,
                    Description=role switch { "Manager"=>"Quản lý toàn bộ dự án.", "Viewer"=>"Chỉ xem nội dung dự án.", _=>"Tạo và cập nhật nhiệm vụ." },
                    IsDefault=role=="Member",
                    CreatedAt=DateTime.Now
                };
                _context.ProjectRoles.Add(roleRow);
                await _context.SaveChangesAsync();
            }
            if(target!=null&&_context.ProjectMembers.Any(m=>m.ProjectId==projectId&&m.UserId==target.Id))
            {TempData["ProjectMessage"]="Người này đã là thành viên của dự án.";TempData["ProjectMessageType"]="error";return RedirectToAction(nameof(Details),new{id=projectId});}
            if(_context.ProjectInvitations.Any(i=>i.ProjectId==projectId&&i.InvitedEmail==email&&i.Status=="pending"&&(!i.ExpiresAt.HasValue||i.ExpiresAt>DateTime.Now)))
            {TempData["ProjectMessage"]="Đã có lời mời đang chờ cho email này.";TempData["ProjectMessageType"]="error";return RedirectToAction(nameof(Details),new{id=projectId});}
            var project=_context.Projects.AsNoTracking().First(p=>p.Id==projectId);
            var invitation=new LifeHub.Models.ProjectManagement.ProjectInvitation{ProjectId=projectId,InvitedBy=uid,InvitedUserId=target?.Id,InvitedEmail=email,ProjectRoleId=roleRow.Id,Token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),Status="pending",CreatedAt=DateTime.Now,ExpiresAt=DateTime.Now.AddDays(7)};
            _context.ProjectInvitations.Add(invitation);
            AddActivity(projectId,uid,"member_invited",$"Đã mời {email}.");
            await _context.SaveChangesAsync();
            if(target!=null)
            {
                var inviter=await _context.Users.AsNoTracking().Where(u=>u.Id==uid).Select(u=>u.FullName).FirstOrDefaultAsync()??"Một thành viên";
                _context.Notifications.Add(new Notification{UserId=target.Id,Title="Lời mời tham gia dự án",Message=$"{inviter} đã mời bạn tham gia dự án “{project.Name}” với vai trò {role}.",LinkUrl=$"/Project/Details/{projectId}",NotificationType="project_invitation",RefType="project_invitation",RefId=invitation.Id,CreatedAt=DateTime.Now});
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details),new{id=projectId});
        }

        [HttpPost]
        public async Task<IActionResult> AcceptInvitation(int projectId, int invitationId)
        {var uid=User.GetUserId();var user=await _context.Users.AsNoTracking().FirstOrDefaultAsync(u=>u.Id==uid);if(user==null)return NotFound();var invite=await _context.ProjectInvitations.FirstOrDefaultAsync(i=>i.Id==invitationId&&i.ProjectId==projectId&&i.Status=="pending"&&(!i.ExpiresAt.HasValue||i.ExpiresAt>DateTime.Now)&&(i.InvitedUserId==uid||i.InvitedUserId==null&&i.InvitedEmail==user.Email));if(invite==null)return NotFound();if(!await _context.ProjectMembers.AnyAsync(m=>m.ProjectId==projectId&&m.UserId==uid))_context.ProjectMembers.Add(new LifeHub.Models.ProjectManagement.ProjectMember{ProjectId=projectId,UserId=uid,ProjectRoleId=invite.ProjectRoleId,JoinedAt=DateTime.Now});invite.Status="accepted";invite.RespondedAt=DateTime.Now;var notification=await _context.Notifications.FirstOrDefaultAsync(n=>n.UserId==uid&&n.RefType=="project_invitation"&&n.RefId==invite.Id);if(notification!=null)notification.IsRead=true;AddActivity(projectId,uid,"invitation_accepted","Đã chấp nhận lời mời tham gia dự án.");_context.Notifications.Add(new Notification{UserId=invite.InvitedBy,Title="Lời mời đã được chấp nhận",Message=$"{user.FullName} đã tham gia dự án.",LinkUrl=$"/Project/Details/{projectId}",NotificationType="project_invitation_response",RefType="project_invitation",RefId=invite.Id,CreatedAt=DateTime.Now});await _context.SaveChangesAsync();return RedirectToAction(nameof(Details),new{id=projectId});}

        [HttpPost]
        public async Task<IActionResult> DeclineInvitation(int projectId, int invitationId)
        {var uid=User.GetUserId();var user=await _context.Users.AsNoTracking().FirstOrDefaultAsync(u=>u.Id==uid);if(user==null)return NotFound();var invite=await _context.ProjectInvitations.FirstOrDefaultAsync(i=>i.Id==invitationId&&i.ProjectId==projectId&&i.Status=="pending"&&(!i.ExpiresAt.HasValue||i.ExpiresAt>DateTime.Now)&&(i.InvitedUserId==uid||i.InvitedUserId==null&&i.InvitedEmail==user.Email));if(invite==null)return NotFound();invite.Status="declined";invite.RespondedAt=DateTime.Now;var notification=await _context.Notifications.FirstOrDefaultAsync(n=>n.UserId==uid&&n.RefType=="project_invitation"&&n.RefId==invite.Id);if(notification!=null)notification.IsRead=true;AddActivity(projectId,uid,"invitation_declined","Đã từ chối lời mời tham gia dự án.");_context.Notifications.Add(new Notification{UserId=invite.InvitedBy,Title="Lời mời đã bị từ chối",Message=$"{user.FullName} đã từ chối lời mời tham gia dự án.",LinkUrl=$"/Project/Details/{projectId}",NotificationType="project_invitation_response",RefType="project_invitation",RefId=invite.Id,CreatedAt=DateTime.Now});await _context.SaveChangesAsync();return RedirectToAction("Index","Notification");}

        [HttpPost]
        public IActionResult CancelInvitation(int projectId, int invitationId)
        {var uid=User.GetUserId();if(!IsManager(projectId,uid))return NotFound();var invite=_context.ProjectInvitations.FirstOrDefault(i=>i.Id==invitationId&&i.ProjectId==projectId&&i.Status=="pending");if(invite==null)return NotFound();invite.Status="cancelled";invite.RespondedAt=DateTime.Now;AddActivity(projectId,uid,"invitation_cancelled","Đã hủy lời mời.");_context.SaveChanges();return RedirectToAction(nameof(Details),new{id=projectId});}

        [HttpPost]
        public IActionResult RemoveMember(int projectId, int userId)
        {var uid=User.GetUserId();if(!IsManager(projectId,uid))return NotFound();var p=_context.Projects.FirstOrDefault(p=>p.Id==projectId);if(p==null)return NotFound();if(userId==p.CreatedBy)return BadRequest();var member=_context.ProjectMembers.FirstOrDefault(m=>m.ProjectId==projectId&&m.UserId==userId);if(member==null)return NotFound();foreach(var t in _context.Tasks.Where(t=>t.ProjectId==projectId&&t.AssignedTo==userId))t.AssignedTo=null;_context.ProjectMembers.Remove(member);AddActivity(projectId,uid,"member_removed","Đã xóa thành viên khỏi dự án.");_context.SaveChanges();return RedirectToAction(nameof(Details),new{id=projectId});}

        [HttpPost]
        public IActionResult CreateTask(int projectId, string title, string? description, string? status,
            string? priority, DateTime? dueDate, int? assigneeId, int? parentTaskId)
        {
            var uid=User.GetUserId();if(!CanEdit(projectId,uid))return NotFound();title=title?.Trim()??"";status=status?.Trim()??"todo";priority=priority?.Trim()??"medium";
            if(!ModelState.IsValid||title.Length is <1 or >200||description?.Length>4000||status is not ("todo" or "in_progress" or "review" or "done")||priority is not ("urgent" or "high" or "medium" or "low"))
            {TempData["ProjectMessage"]="Hãy kiểm tra tiêu đề, mô tả, trạng thái, mức ưu tiên và hạn chót.";TempData["ProjectMessageType"]="error";return RedirectToAction(nameof(Details),new{id=projectId});}
            if(assigneeId.HasValue&&!CanAssign(projectId,uid,assigneeId.Value))
            {TempData["ProjectMessage"]="Người được giao phải là thành viên có quyền thực hiện nhiệm vụ trong dự án.";TempData["ProjectMessageType"]="error";return RedirectToAction(nameof(Details),new{id=projectId});}
            if(parentTaskId.HasValue&&!_context.Tasks.Any(t=>t.Id==parentTaskId&&t.ProjectId==projectId))return NotFound();
            var task=new LifeHub.Models.TaskManagement.Task{UserId=uid,ProjectId=projectId,ParentTaskId=parentTaskId,AssignedTo=assigneeId,Title=title,Description=description?.Trim(),Status=status,Priority=priority,Deadline=dueDate,CreatedAt=DateTime.Now,CompletedAt=status=="done"?DateTime.Now:null};
            _context.Tasks.Add(task);_context.SaveChanges();AddActivity(projectId,uid,"task_created",$"Đã tạo nhiệm vụ: {title}".Substring(0,Math.Min(255,$"Đã tạo nhiệm vụ: {title}".Length)),"task",task.Id);_context.SaveChanges();return RedirectToAction(nameof(Details),"Project",new{id=projectId},parentTaskId.HasValue?$"task-{parentTaskId}":null);
        }

        [HttpPost]
        public IActionResult MoveTask(int projectId, int taskId, string status, int? openTask, string? reason)
        {var uid=User.GetUserId();if(!CanEdit(projectId,uid))return NotFound();var task=_context.Tasks.FirstOrDefault(t=>t.Id==taskId&&t.ProjectId==projectId);if(task==null)return NotFound();var manager=IsManager(projectId,uid);if(!manager&&(task.AssignedTo.HasValue&&task.AssignedTo!=uid||task.Status=="done"||status=="done"))return Forbid();if(status is not ("todo" or "in_progress" or "review" or "done")||reason?.Length>500)return BadRequest();if(!string.IsNullOrWhiteSpace(reason))_context.TaskComments.Add(new LifeHub.Models.TaskManagement.TaskComment{TaskId=taskId,UserId=uid,Content=reason.Trim(),CreatedAt=DateTime.Now});task.Status=status;task.CompletedAt=status=="done"?DateTime.Now:null;task.UpdatedAt=DateTime.Now;AddActivity(projectId,uid,string.IsNullOrWhiteSpace(reason)?"task_status":"task_rework",$"Đã cập nhật trạng thái nhiệm vụ #{taskId}: {status}","task",taskId);_context.SaveChanges();return RedirectToAction(nameof(Details),"Project",new{id=projectId},openTask.HasValue?$"task-{openTask}":null);}

        [HttpPost]
        public IActionResult AddTaskComment(int projectId,int taskId,string content)
        {var uid=User.GetUserId();if(!CanEdit(projectId,uid))return NotFound();content=content?.Trim()??"";if(content.Length is <1 or >1000)return BadRequest();var task=_context.Tasks.FirstOrDefault(t=>t.Id==taskId&&t.ProjectId==projectId);if(task==null)return NotFound();_context.TaskComments.Add(new LifeHub.Models.TaskManagement.TaskComment{TaskId=taskId,UserId=uid,Content=content,CreatedAt=DateTime.Now});AddActivity(projectId,uid,"task_comment",$"Đã bình luận trong nhiệm vụ #{taskId}.","task",taskId);_context.SaveChanges();return RedirectToAction(nameof(Details),new{id=projectId});}

        [HttpPost]
        public IActionResult AssignTask(int projectId, int taskId, int? assigneeId, int? openTask)
        {var uid=User.GetUserId();if(!CanEdit(projectId,uid))return NotFound();var task=_context.Tasks.FirstOrDefault(t=>t.Id==taskId&&t.ProjectId==projectId);if(task==null)return NotFound();if(assigneeId.HasValue&&!CanAssign(projectId,uid,assigneeId.Value))return BadRequest();task.AssignedTo=assigneeId;task.UpdatedAt=DateTime.Now;AddActivity(projectId,uid,"task_assigned",$"Đã cập nhật người thực hiện nhiệm vụ #{taskId}.","task",taskId);_context.SaveChanges();return RedirectToAction(nameof(Details),"Project",new{id=projectId},openTask.HasValue?$"task-{openTask}":null);}

        [HttpPost]
        public IActionResult ToggleSubtask(int projectId, int taskId, int? openTask)
        {var uid=User.GetUserId();if(!CanEdit(projectId,uid))return NotFound();var task=_context.Tasks.FirstOrDefault(t=>t.Id==taskId&&t.ProjectId==projectId&&t.ParentTaskId.HasValue);if(task==null)return NotFound();task.Status=task.Status=="done"?"todo":"done";task.CompletedAt=task.Status=="done"?DateTime.Now:null;task.UpdatedAt=DateTime.Now;AddActivity(projectId,uid,"subtask_status",$"Đã cập nhật việc con #{taskId}.","task",taskId);_context.SaveChanges();return RedirectToAction(nameof(Details),"Project",new{id=projectId},openTask.HasValue?$"task-{openTask}":null);}

        [HttpPost]
        public IActionResult DeleteTask(int projectId, int taskId)
        {var uid=User.GetUserId();if(!IsManager(projectId,uid))return NotFound();var task=_context.Tasks.FirstOrDefault(t=>t.Id==taskId&&t.ProjectId==projectId);if(task==null)return NotFound();var children=_context.Tasks.Where(t=>t.ProjectId==projectId&&t.ParentTaskId==taskId).ToList();if(children.Any(c=>_context.Tasks.Any(x=>x.ParentTaskId==c.Id)))return BadRequest();foreach(var c in children){_context.TaskComments.RemoveRange(_context.TaskComments.Where(x=>x.TaskId==c.Id));_context.Tasks.Remove(c);}_context.TaskComments.RemoveRange(_context.TaskComments.Where(x=>x.TaskId==taskId));_context.Tasks.Remove(task);AddActivity(projectId,uid,"task_deleted",$"Đã xóa nhiệm vụ #{taskId}.");_context.SaveChanges();return RedirectToAction(nameof(Details),new{id=projectId});}

        [HttpPost]
        public IActionResult AddMilestone(int projectId, string title, DateTime dueDate, string? returnTo)
        {var uid=User.GetUserId();if(!IsManager(projectId,uid))return NotFound();title=title?.Trim()??"";if(title.Length is <1 or >150||dueDate==default)return BadRequest();_context.ProjectMilestones.Add(new LifeHub.Models.ProjectManagement.ProjectMilestone{ProjectId=projectId,Title=title,DueDate=DateOnly.FromDateTime(dueDate)});var activity=$"Đã thêm cột mốc: {title}";AddActivity(projectId,uid,"milestone_added",activity.Substring(0,Math.Min(activity.Length,255)));_context.SaveChanges();return returnTo=="index"?RedirectToAction(nameof(Index)):RedirectToAction(nameof(Details),new{id=projectId});}

        [HttpPost]
        public IActionResult ToggleMilestone(int milestoneId, int projectId, string? returnTo)
        {var uid=User.GetUserId();if(!IsManager(projectId,uid))return NotFound();var m=_context.ProjectMilestones.FirstOrDefault(x=>x.Id==milestoneId&&x.ProjectId==projectId);if(m==null)return NotFound();m.IsCompleted=!m.IsCompleted;var activity=$"Đã {(m.IsCompleted?"hoàn thành":"mở lại")} cột mốc: {m.Title}";AddActivity(projectId,uid,"milestone_status",activity.Substring(0,Math.Min(activity.Length,255)));_context.SaveChanges();return returnTo=="index"?RedirectToAction(nameof(Index)):RedirectToAction(nameof(Details),new{id=projectId});}

        private LifeHub.Models.ProjectManagement.Project? VisibleProject(int projectId,int uid)=>_context.Projects.FirstOrDefault(p=>p.Id==projectId&&(p.CreatedBy==uid||_context.ProjectMembers.Any(m=>m.ProjectId==p.Id&&m.UserId==uid)));
        private bool IsManager(int projectId,int uid)=>_context.Projects.Any(p=>p.Id==projectId&&p.CreatedBy==uid)||
            (from m in _context.ProjectMembers join r in _context.ProjectRoles on m.ProjectRoleId equals r.Id where m.ProjectId==projectId&&m.UserId==uid select r.Name).Any(n=>n.ToLower()=="manager");
        private bool CanEdit(int projectId,int uid)=>IsManager(projectId,uid)||
            (from m in _context.ProjectMembers join r in _context.ProjectRoles on m.ProjectRoleId equals r.Id where m.ProjectId==projectId&&m.UserId==uid select r.Name).Any(n=>n.ToLower()=="member");
        private bool CanAssign(int projectId,int actorId,int assigneeId)
        {
            if(!CanEdit(projectId,actorId))return false;
            if(!IsManager(projectId,actorId)&&assigneeId!=actorId)return false;
            var p=_context.Projects.AsNoTracking().FirstOrDefault(x=>x.Id==projectId);
            if(p?.CreatedBy==assigneeId)return true;
            return (from m in _context.ProjectMembers join r in _context.ProjectRoles on m.ProjectRoleId equals r.Id where m.ProjectId==projectId&&m.UserId==assigneeId select r.Name).Any(n=>n.ToLower()=="manager"||n.ToLower()=="member");
        }
        private void AddActivity(int projectId,int uid,string action,string detail,string? targetType=null,int? targetId=null)
        {_context.ProjectActivities.Add(new LifeHub.Models.ProjectManagement.ProjectActivity{ProjectId=projectId,UserId=uid,Action=action,Detail=detail.Length>500?detail[..500]:detail,TargetType=targetType,TargetId=targetId,CreatedAt=DateTime.Now});}
        private static string? NormalizeStatus(string status)=>status switch{"Đang chạy" or "running"=>"running","Lên kế hoạch" or "planning"=>"planning","Tạm dừng" or "paused"=>"paused","Hoàn thành" or "completed"=>"completed",_=>null};
    }
}
