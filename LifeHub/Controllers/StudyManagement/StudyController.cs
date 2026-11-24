using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using LifeHub.DataAccessLayer;
using LifeHub.Extensions;
using LifeHub.Models.ViewModels.StudyManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers
{
    [Authorize]
    public class StudyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        public StudyController(ApplicationDbContext context, IWebHostEnvironment env) { _context = context; _env = env; }
        private void Page(string title, string activePage)
        {
            ViewData["Title"] = title;
            // Giá trị phải trùng với khóa mục con trong _Layout.cshtml để mục đó được tô sáng.
            ViewData["ActivePage"] = activePage;
        }

        // GET /Study
        public async Task<IActionResult> Index()
        {
            Page("Quản lý học tập", "StudyOverview");
            var userId=User.GetUserId();
            var courses=await _context.Courses.AsNoTracking().Where(c=>c.UserId==userId).ToListAsync();
            var ids=courses.Select(c=>c.Id).ToArray();
            var assignments=await _context.Assignments.AsNoTracking().Where(a=>ids.Contains(a.CourseId)).OrderBy(a=>a.DueDate).ToListAsync();
            var schedules=await _context.CourseSchedules.AsNoTracking().Where(s=>ids.Contains(s.CourseId)).ToListAsync();
            var model=new StudyViewModel { SemesterName=await _context.Semesters.AsNoTracking().Where(s=>s.UserId==userId&&s.IsCurrent).Select(s=>s.Name).FirstOrDefaultAsync() ?? "",
                Courses=courses.Select(c=>CourseItem(c,assignments)).ToList(), PendingAssignmentsCount=assignments.Count(a=>!a.IsSubmitted&&a.Score==null),
                Assignments=assignments.OrderBy(a=>a.DueDate).Take(5).Select(a=>AssignmentItem(a,courses)).ToList(),
                ActiveCoursesCount=courses.Count(c=>c.Status=="active"), TotalCredits=courses.Where(c=>c.Status=="active").Sum(c=>c.Credits??0) };
            var next=assignments.Where(a=>a.AssignmentType is "midterm" or "final" && a.DueDate>=DateTime.Today).OrderBy(a=>a.DueDate).FirstOrDefault();
            if(next!=null){model.NextExamName=next.Title;model.NextExamDays=(next.DueDate.Date-DateTime.Today).Days;}
            model.Schedule=Enum.GetValues<DayOfWeek>().Select(day=>new StudyScheduleDay { DayName=DayLabel(day), IsToday=day==DateTime.Today.DayOfWeek,
                Events=schedules.Where(s=>s.DayOfWeek==(int)day).OrderBy(s=>s.StartTime).Select(s=>new StudyScheduleEvent { Subject=courses.First(c=>c.Id==s.CourseId).CourseName,
                    Time=$"{s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm}", Room=s.RoomOrLink??"", Color=courses.First(c=>c.Id==s.CourseId).Color??"#2563eb" }).ToList() }).ToList();
            await FillAssignmentExtrasAsync(model.Assignments,userId);
            return View(model);
        }

        // GET /Study/Courses?status=current|completed|all
        public async Task<IActionResult> Courses(string? status) { Page("Môn học và khóa học", "Course"); var userId=User.GetUserId(); var cs=await _context.Courses.AsNoTracking().Where(c=>c.UserId==userId).ToListAsync(); var ids=cs.Select(c=>c.Id).ToArray(); var ass=await _context.Assignments.AsNoTracking().Where(a=>ids.Contains(a.CourseId)).ToListAsync(); var sem=await _context.Semesters.AsNoTracking().Where(s=>s.UserId==userId).ToDictionaryAsync(s=>s.Id,s=>s.Name); var sch=await _context.CourseSchedules.AsNoTracking().Where(s=>ids.Contains(s.CourseId)).ToListAsync(); var key=(status??"current").ToLowerInvariant(); if(key is not ("current" or "completed" or "all"))key="current"; var filtered=cs.Where(c=>key=="all"||(key=="completed"?c.Status=="completed":c.Status!="completed")).ToList(); return View(new StudyCoursesPageViewModel { Status=key, CurrentCount=cs.Count(c=>c.Status!="completed"), CompletedCount=cs.Count(c=>c.Status=="completed"), CurrentCredits=cs.Where(c=>c.Status!="completed").Sum(c=>c.Credits??0), Rows=filtered.Select(c=>new StudyCourseRow { Course=CourseItem(c,ass), SemesterName=c.SemesterId.HasValue&&sem.TryGetValue(c.SemesterId.Value,out var sn)?sn:"", AssignmentCount=ass.Count(a=>a.CourseId==c.Id), SubmittedCount=ass.Count(a=>a.CourseId==c.Id&&a.IsSubmitted), ScheduleSummary=string.Join(", ",sch.Where(s=>s.CourseId==c.Id).Select(s=>$"{DayLabel((DayOfWeek)s.DayOfWeek)} {s.StartTime:hh\\:mm}")) }).ToList() }); }

        // GET /Study/Schedule
        public async Task<IActionResult> Schedule() { Page("Thời khóa biểu", "Schedule"); var userId=User.GetUserId(); var courses=await _context.Courses.AsNoTracking().Where(c=>c.UserId==userId).ToListAsync(); var ids=courses.Select(c=>c.Id).ToArray(); var slots=await _context.CourseSchedules.AsNoTracking().Where(s=>ids.Contains(s.CourseId)).ToListAsync(); var mapped=slots.Select(s=>new StudyScheduleSlot { Id=s.Id,CourseId=s.CourseId,Day=(DayOfWeek)s.DayOfWeek,Start=s.StartTime.ToString(@"hh\:mm"),End=s.EndTime.ToString(@"hh\:mm"),Room=s.RoomOrLink??"",CourseCode=courses.First(c=>c.Id==s.CourseId).CourseCode,CourseName=courses.First(c=>c.Id==s.CourseId).CourseName,Color=courses.First(c=>c.Id==s.CourseId).Color??"#2563eb" }).ToList(); var days=Enum.GetValues<DayOfWeek>().Select(d=>{var daySlots=mapped.Where(s=>s.Day==d).OrderBy(s=>s.StartMinutes).ToList(); for(int i=0;i<daySlots.Count;i++) for(int j=i+1;j<daySlots.Count;j++) if(daySlots[i].StartMinutes<daySlots[j].EndMinutes&&daySlots[j].StartMinutes<daySlots[i].EndMinutes){daySlots[i].HasConflict=true;daySlots[j].HasConflict=true;} return new StudyTimetableDay {DayName=DayLabel(d),IsToday=d==DateTime.Today.DayOfWeek,Blocks=daySlots.Select(s=>new StudyTimetableBlock { Slot=s,TopPercent=Math.Max(0,(s.StartMinutes-420)/6.6),HeightPercent=Math.Max(1,(s.EndMinutes-s.StartMinutes)/6.6)}).ToList()};}).ToList(); return View(new StudySchedulePageViewModel { Slots=mapped,Days=days,Courses=courses.Select(c=>CourseItem(c,new())).ToList(),TodayCount=mapped.Count(s=>s.Day==DateTime.Today.DayOfWeek),WeeklyHours=mapped.Sum(s=>s.EndMinutes-s.StartMinutes)/60d }); }

        // GET /Study/Assignments?status=upcoming|overdue|done&kind=quiz&courseId=1&q=...
        public async Task<IActionResult> Assignments(string? status, string? kind, int? courseId, string? q)
        {
            Page("Bài tập và kỳ thi", "Assignment");
            var uid=User.GetUserId(); var courses=await _context.Courses.AsNoTracking().Where(c=>c.UserId==uid).ToListAsync(); var ids=courses.Select(c=>c.Id).ToArray();
            var all=await _context.Assignments.AsNoTracking().Where(a=>ids.Contains(a.CourseId)).ToListAsync(); var key=(status??"all").ToLowerInvariant(); var kindKey=(kind??"all").ToLowerInvariant();
            var filtered=all.AsEnumerable(); if(courseId.HasValue&&ids.Contains(courseId.Value))filtered=filtered.Where(a=>a.CourseId==courseId); else if(courseId.HasValue)filtered=Enumerable.Empty<LifeHub.Models.StudyManagement.Assignment>();
            if(kindKey!="all")filtered=filtered.Where(a=>a.AssignmentType.ToLower()==kindKey); if(!string.IsNullOrWhiteSpace(q))filtered=filtered.Where(a=>a.Title.Contains(q.Trim(),StringComparison.OrdinalIgnoreCase));
            if(key=="done")filtered=filtered.Where(a=>a.IsSubmitted||a.Score.HasValue); else if(key=="overdue")filtered=filtered.Where(a=>!a.IsSubmitted&&!a.Score.HasValue&&a.DueDate<DateTime.Today); else if(key=="upcoming")filtered=filtered.Where(a=>!a.IsSubmitted&&!a.Score.HasValue&&a.DueDate>=DateTime.Today);
            var asgVm=new StudyAssignmentsPageViewModel { Status=key,Kind=kindKey,CourseId=courseId,Q=q,Items=filtered.OrderBy(a=>a.DueDate).Select(a=>AssignmentItem(a,courses)).ToList(),Courses=courses.Select(c=>CourseItem(c,all)).ToList(),TotalCount=all.Count,PendingCount=all.Count(a=>!a.IsSubmitted&&!a.Score.HasValue&&a.DueDate>=DateTime.Today),OverdueCount=all.Count(a=>!a.IsSubmitted&&!a.Score.HasValue&&a.DueDate<DateTime.Today),DoneCount=all.Count(a=>a.IsSubmitted||a.Score.HasValue) }; await FillAssignmentExtrasAsync(asgVm.Items,uid); return View(asgVm);
        }

        // GET /Study/Grades
        public async Task<IActionResult> Grades()
        {
            Page("Điểm số", "Grade");
            var userId = User.GetUserId();
            var courses = await _context.Courses.AsNoTracking()
                .Where(course => course.UserId == userId)
                .OrderBy(course => course.CourseCode)
                .ToListAsync();
            var courseIds = courses.Select(course => course.Id).ToArray();
            var assignments = await _context.Assignments.AsNoTracking()
                .Where(assignment => courseIds.Contains(assignment.CourseId))
                .OrderBy(assignment => assignment.DueDate)
                .ToListAsync();

            var rows = courses.Select(course =>
            {
                var graded = assignments
                    .Where(assignment => assignment.CourseId == course.Id && assignment.Score.HasValue)
                    .ToList();
                var weighted = graded.Where(assignment => assignment.WeightPercent > 0).ToList();
                var gradedWeight = weighted.Sum(assignment => assignment.WeightPercent);

                decimal NormalizedScore(LifeHub.Models.StudyManagement.Assignment assignment)
                {
                    var maxScore = assignment.MaxScore.GetValueOrDefault(10m);
                    if (maxScore <= 0) maxScore = 10m;
                    return Math.Clamp(assignment.Score!.Value / maxScore * 10m, 0m, 10m);
                }

                decimal? assignmentAverage = graded.Count == 0
                    ? null
                    : gradedWeight > 0
                        ? weighted.Sum(assignment => NormalizedScore(assignment) * assignment.WeightPercent) / gradedWeight
                        : graded.Average(NormalizedScore);
                var currentScore = course.FinalScore ?? assignmentAverage;
                var courseItem = CourseItem(course, assignments);
                courseItem.CurrentScore = currentScore;
                var gpa4 = currentScore.HasValue ? currentScore.Value / 10m * 4m : (decimal?)null;
                var average = currentScore ?? 0m;

                return new StudyGradeRow
                {
                    Course = courseItem,
                    Graded = graded.Select(assignment => new StudyGradeItem
                    {
                        Title = assignment.Title,
                        KindLabel = assignment.AssignmentType,
                        Score = assignment.Score!.Value,
                        MaxScore = assignment.MaxScore ?? 10m,
                        WeightPercent = assignment.WeightPercent
                    }).ToList(),
                    GradedWeight = gradedWeight,
                    RemainingWeight = Math.Max(0, 100 - gradedWeight),
                    Gpa4 = gpa4,
                    Letter = !currentScore.HasValue ? "—" : average >= 9m ? "A" : average >= 8m ? "B" : average >= 7m ? "C" : average >= 6m ? "D" : "F",
                    Gap = currentScore.HasValue && course.TargetScore.HasValue ? currentScore.Value - course.TargetScore.Value : null,
                    RequiredLabel = course.TargetScore?.ToString("0.0") ?? ""
                };
            }).ToList();

            var scoredRows = rows.Where(row => row.Course.CurrentScore.HasValue).ToList();
            var scoredCredits = scoredRows.Sum(row => row.Course.Credits);
            var gpa = scoredCredits > 0
                ? scoredRows.Sum(row => row.Gpa4.GetValueOrDefault() * row.Course.Credits) / scoredCredits
                : (decimal?)null;

            return View(new StudyGradesPageViewModel
            {
                Rows = rows,
                Gpa = gpa,
                ScoredCredits = scoredCredits,
                TargetMetCount = rows.Count(row => row.MetTarget)
            });
        }

        // GET /Study/Documents?courseId=1&kind=pdf&q=...
        public async Task<IActionResult> Documents(int? courseId, string? kind, string? q)
        {
            Page("Tài liệu học tập", "StudyDocument");
            var uid=User.GetUserId(); var courses=await _context.Courses.AsNoTracking().Where(c=>c.UserId==uid).ToListAsync(); var ids=courses.Select(c=>c.Id).ToArray(); var key=(kind??"all").ToLowerInvariant(); var documents=await _context.Documents.AsNoTracking().Where(d=>d.UserId==uid&&d.CourseId.HasValue&&ids.Contains(d.CourseId.Value)).ToListAsync(); var query=documents.AsEnumerable(); if(courseId.HasValue)query=query.Where(d=>d.CourseId==courseId); if(key!="all")query=query.Where(d=>(d.FileType??"").ToLower()==key); if(!string.IsNullOrWhiteSpace(q))query=query.Where(d=>d.FileName.Contains(q.Trim(),StringComparison.OrdinalIgnoreCase)); var items=query.OrderByDescending(d=>d.UploadedAt).Select(d=>new StudyDocumentItem {Id=d.Id,Title=d.FileName,CourseId=d.CourseId!.Value,CourseCode=courses.First(c=>c.Id==d.CourseId).CourseCode,CourseColor=courses.First(c=>c.Id==d.CourseId).Color??"#2563eb",Kind=d.FileType??"pdf",Url=DocumentUrl(d),Source=DocumentSource(d),SizeLabel=d.FileSizeKb.HasValue?$"{d.FileSizeKb} KB":"",Note=DocumentNote(d),AddedAt=d.UploadedAt}).ToList(); return View(new StudyDocumentsPageViewModel {CourseId=courseId,Kind=key,Q=q,Items=items,Courses=courses.Select(c=>CourseItem(c,new())).ToList(),TotalCount=documents.Count});
        }

        private static StudyCourseItem CourseItem(LifeHub.Models.StudyManagement.Course c, List<LifeHub.Models.StudyManagement.Assignment> assignments) => new() { Id=c.Id,Code=c.CourseCode,Name=c.CourseName,Credits=c.Credits??0,Instructor=c.InstructorName??"",Room=c.RoomLocation??"",Color=c.Color??"#2563eb",TargetScore=c.TargetScore,Status=c.Status=="completed"?"Đã hoàn thành":"Đang học",CurrentScore=c.FinalScore,AssignmentProgressPercent=assignments.Count(a=>a.CourseId==c.Id)==0?0:assignments.Count(a=>a.CourseId==c.Id&&a.IsSubmitted)*100/assignments.Count(a=>a.CourseId==c.Id) };
        private static StudyAssignmentItem AssignmentItem(LifeHub.Models.StudyManagement.Assignment a,List<LifeHub.Models.StudyManagement.Course> courses)=>new(){Id=a.Id,Title=a.Title,CourseId=a.CourseId,CourseName=courses.FirstOrDefault(c=>c.Id==a.CourseId)?.CourseName??"",CourseCode=courses.FirstOrDefault(c=>c.Id==a.CourseId)?.CourseCode??"",DueDate=a.DueDate,WeightPercent=a.WeightPercent,IsSubmitted=a.IsSubmitted,Score=a.Score,Kind=a.AssignmentType,MaxScore=a.MaxScore??10,DurationMinutes=a.DurationMinutes,Location=a.Location};
        private static string DayLabel(DayOfWeek d)=>d switch {DayOfWeek.Monday=>"T2",DayOfWeek.Tuesday=>"T3",DayOfWeek.Wednesday=>"T4",DayOfWeek.Thursday=>"T5",DayOfWeek.Friday=>"T6",DayOfWeek.Saturday=>"T7",_=>"CN"};

        // ------------------------------------------------------------------ POST: mọi bản ghi được giới hạn theo người dùng hiện tại.

        // Môn học (bảng courses)
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> CreateCourse(CreateCourseInput input)
        {
            var uid=User.GetUserId();input.Code=input.Code?.Trim()??"";input.Name=input.Name?.Trim()??"";
            if(!ModelState.IsValid||!ValidColor(input.Color)||await _context.Courses.AnyAsync(c=>c.UserId==uid&&c.CourseCode==input.Code)){ModelState.AddModelError(string.Empty,"Thông tin môn học không hợp lệ hoặc mã môn đã tồn tại.");return RedirectToAction(nameof(Courses));}
            _context.Courses.Add(new LifeHub.Models.StudyManagement.Course{UserId=uid,CourseCode=input.Code,CourseName=input.Name,Credits=input.Credits,InstructorName=input.Instructor?.Trim(),RoomLocation=input.Room?.Trim(),Color=input.Color,TargetScore=input.TargetScore,Status="active",CreatedAt=DateTime.Now});
            await _context.SaveChangesAsync();TempData["SuccessMessage"]="Đã thêm môn học.";return RedirectToAction(nameof(Courses));
        }
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> UpdateCourse(UpdateCourseInput input)
        {
            var uid=User.GetUserId();if(!ModelState.IsValid||!ValidColor(input.Color)||input.Status is not ("Đang học" or "Đã hoàn thành")){TempData["ErrorMessage"]="Thông tin môn học không hợp lệ.";return RedirectToAction(nameof(Courses));}
            var c=await _context.Courses.FirstOrDefaultAsync(x=>x.Id==input.Id&&x.UserId==uid);if(c==null)return NotFound();
            input.Code=input.Code?.Trim()??"";input.Name=input.Name?.Trim()??"";if(await _context.Courses.AnyAsync(x=>x.UserId==uid&&x.Id!=c.Id&&x.CourseCode==input.Code)){TempData["ErrorMessage"]="Mã môn đã được sử dụng.";return RedirectToAction(nameof(Courses));}
            c.CourseCode=input.Code;c.CourseName=input.Name;c.Credits=input.Credits;c.InstructorName=input.Instructor?.Trim();c.RoomLocation=input.Room?.Trim();c.Color=input.Color;c.TargetScore=input.TargetScore;c.Status=input.Status=="Đã hoàn thành"?"completed":"active";await _context.SaveChangesAsync();return RedirectToAction(nameof(Courses));
        }
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> DeleteCourse(int id)
        {
            var uid=User.GetUserId();var c=await _context.Courses.FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==uid);if(c==null)return NotFound();
            if(await _context.Assignments.AnyAsync(a=>a.CourseId==id)||await _context.CourseSchedules.AnyAsync(s=>s.CourseId==id)||await _context.Tasks.AnyAsync(t=>t.CourseId==id)||await _context.Documents.AnyAsync(d=>d.CourseId==id)||await _context.Notes.AnyAsync(n=>n.CourseId==id))
            {TempData["ErrorMessage"]="Môn học đang được dữ liệu khác sử dụng nên chưa thể xóa.";return RedirectToAction(nameof(Courses));}
            _context.Courses.Remove(c);await _context.SaveChangesAsync();return RedirectToAction(nameof(Courses));
        }

        // Thời khóa biểu (bảng course_schedules). Back-end cần chặn buổi học trùng giờ cùng ngày.
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> CreateScheduleSlot(SaveScheduleSlotInput input)
        {var uid=User.GetUserId();if(!ModelState.IsValid||!ValidTime(input.Start,input.End)||!await _context.Courses.AnyAsync(c=>c.Id==input.CourseId&&c.UserId==uid)||await HasScheduleConflict(input.CourseId,input.Day,input.Start,input.End,0)){TempData["ErrorMessage"]="Buổi học không hợp lệ hoặc bị trùng giờ.";return RedirectToAction(nameof(Schedule));} _context.CourseSchedules.Add(new LifeHub.Models.StudyManagement.CourseSchedule{CourseId=input.CourseId,DayOfWeek=input.Day,StartTime=TimeSpan.Parse(input.Start),EndTime=TimeSpan.Parse(input.End),RoomOrLink=input.Room?.Trim()});await _context.SaveChangesAsync();return RedirectToAction(nameof(Schedule));}
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> UpdateScheduleSlot(SaveScheduleSlotInput input)
        {var uid=User.GetUserId();var slot=await _context.CourseSchedules.FirstOrDefaultAsync(s=>s.Id==input.Id&&_context.Courses.Any(c=>c.Id==s.CourseId&&c.UserId==uid));if(slot==null)return NotFound();if(!ModelState.IsValid||!ValidTime(input.Start,input.End)||!await _context.Courses.AnyAsync(c=>c.Id==input.CourseId&&c.UserId==uid)||await HasScheduleConflict(input.CourseId,input.Day,input.Start,input.End,input.Id)){TempData["ErrorMessage"]="Buổi học không hợp lệ hoặc bị trùng giờ.";return RedirectToAction(nameof(Schedule));}slot.CourseId=input.CourseId;slot.DayOfWeek=input.Day;slot.StartTime=TimeSpan.Parse(input.Start);slot.EndTime=TimeSpan.Parse(input.End);slot.RoomOrLink=input.Room?.Trim();await _context.SaveChangesAsync();return RedirectToAction(nameof(Schedule));}
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> DeleteScheduleSlot(int id)
        {var uid=User.GetUserId();var slot=await _context.CourseSchedules.FirstOrDefaultAsync(s=>s.Id==id&&_context.Courses.Any(c=>c.Id==s.CourseId&&c.UserId==uid));if(slot==null)return NotFound();_context.CourseSchedules.Remove(slot);await _context.SaveChangesAsync();return RedirectToAction(nameof(Schedule));}

        // Bài tập và kỳ thi (bảng assignments)
        [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(MaxRequestBytes), RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)] public async Task<IActionResult> CreateAssignment(CreateAssignmentInput input)
        {
            var uid=User.GetUserId();input.Title=input.Title?.Trim()??"";
            var uploadError=ValidateUpload(input.Attachment);
            if(!ModelState.IsValid||!AllowedAssignmentKind(input.Kind)||!await _context.Courses.AnyAsync(c=>c.Id==input.CourseId&&c.UserId==uid)||await WeightTotal(input.CourseId,0)+input.WeightPercent>100){TempData["ErrorMessage"]="Bài tập không hợp lệ hoặc tổng trọng số vượt 100%.";return RedirectToAction(nameof(Assignments));}
            if(uploadError!=null){TempData["ErrorMessage"]=uploadError;return RedirectToAction(nameof(Assignments));}
            var a=new LifeHub.Models.StudyManagement.Assignment{CourseId=input.CourseId,Title=input.Title,AssignmentType=input.Kind,DueDate=input.DueDate,WeightPercent=input.WeightPercent,MaxScore=input.MaxScore,DurationMinutes=input.DurationMinutes,Location=input.Location?.Trim()};
            _context.Assignments.Add(a);await _context.SaveChangesAsync();
            await SaveAssignmentExtrasAsync(a,input,uid);
            TempData["SuccessMessage"]="Đã thêm bài tập.";return RedirectToAction(nameof(Assignments));
        }
        [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(MaxRequestBytes), RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)] public async Task<IActionResult> UpdateAssignment(UpdateAssignmentInput input)
        {
            var uid=User.GetUserId();var a=await _context.Assignments.FirstOrDefaultAsync(x=>x.Id==input.Id&&_context.Courses.Any(c=>c.Id==x.CourseId&&c.UserId==uid));if(a==null)return NotFound();
            var uploadError=ValidateUpload(input.Attachment);
            if(!ModelState.IsValid||!AllowedAssignmentKind(input.Kind)||!await _context.Courses.AnyAsync(c=>c.Id==input.CourseId&&c.UserId==uid)||await WeightTotal(input.CourseId,input.Id)+input.WeightPercent>100){TempData["ErrorMessage"]="Bài tập không hợp lệ hoặc tổng trọng số vượt 100%.";return RedirectToAction(nameof(Assignments));}
            if(uploadError!=null){TempData["ErrorMessage"]=uploadError;return RedirectToAction(nameof(Assignments));}
            a.CourseId=input.CourseId;a.Title=input.Title.Trim();a.AssignmentType=input.Kind;a.DueDate=input.DueDate;a.WeightPercent=input.WeightPercent;a.MaxScore=input.MaxScore;a.DurationMinutes=input.DurationMinutes;a.Location=input.Location?.Trim();
            await _context.SaveChangesAsync();await SaveAssignmentExtrasAsync(a,input,uid);
            return RedirectToAction(nameof(Assignments));
        }
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> DeleteAssignment(int id)
        {
            var uid=User.GetUserId();var a=await _context.Assignments.FirstOrDefaultAsync(x=>x.Id==id&&_context.Courses.Any(c=>c.Id==x.CourseId&&c.UserId==uid));if(a==null)return NotFound();
            // Xóa luôn tệp đính kèm và nội dung soạn của bài tập này.
            var marker=AssignmentMarker+a.Id;var extras=await _context.Documents.Where(d=>d.UserId==uid&&d.Description==marker).ToListAsync();
            foreach(var d in extras)DeleteStored(d.FilePath);_context.Documents.RemoveRange(extras);
            _context.Assignments.Remove(a);await _context.SaveChangesAsync();return RedirectToAction(nameof(Assignments));
        }
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> ToggleSubmitted(int id)
        {var uid=User.GetUserId();var a=await _context.Assignments.FirstOrDefaultAsync(x=>x.Id==id&&_context.Courses.Any(c=>c.Id==x.CourseId&&c.UserId==uid));if(a==null)return NotFound();a.IsSubmitted=!a.IsSubmitted;await _context.SaveChangesAsync();return RedirectToAction(nameof(Assignments));}
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> RecordScore(RecordScoreInput input)
        {var uid=User.GetUserId();var a=await _context.Assignments.FirstOrDefaultAsync(x=>x.Id==input.Id&&_context.Courses.Any(c=>c.Id==x.CourseId&&c.UserId==uid));if(a==null)return NotFound();if(!ModelState.IsValid||input.Score>(a.MaxScore??10)){TempData["ErrorMessage"]="Điểm phải nằm trong thang điểm của bài.";return RedirectToAction(nameof(Assignments));}a.Score=input.Score;a.IsSubmitted=true;await _context.SaveChangesAsync();return RedirectToAction(nameof(Grades));}

        // Điểm số
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> SetTargetScore(SetTargetScoreInput input)
        {var uid=User.GetUserId();if(!ModelState.IsValid)return RedirectToAction(nameof(Grades));var c=await _context.Courses.FirstOrDefaultAsync(x=>x.Id==input.CourseId&&x.UserId==uid);if(c==null)return NotFound();c.TargetScore=input.TargetScore;await _context.SaveChangesAsync();return RedirectToAction(nameof(Grades));}

        // Tài liệu học tập
        [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(MaxRequestBytes), RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)] public async Task<IActionResult> CreateDocument(CreateDocumentInput input)
        {
            var uid=User.GetUserId();input.Title=input.Title?.Trim()??"";var url=input.Url?.Trim()??"";var hasContent=HasText(input.Content);var uploadError=ValidateUpload(input.File);
            if(!ModelState.IsValid||!AllowedDocumentKind(input.Kind)||input.Note?.Length>255||!await _context.Courses.AnyAsync(c=>c.Id==input.CourseId&&c.UserId==uid)||(url.Length>0&&!SafeDocumentUrl(url))||input.Note?.TrimStart().StartsWith(AssignmentMarker)==true||(input.File==null&&!hasContent&&url.Length==0)){TempData["ErrorMessage"]="Thông tin tài liệu không hợp lệ. Hãy chọn tệp, soạn nội dung hoặc nhập đường dẫn.";return RedirectToAction(nameof(Documents));}
            if(uploadError!=null){TempData["ErrorMessage"]=uploadError;return RedirectToAction(nameof(Documents));}
            var note=input.Note?.Trim();
            // Mỗi nguồn người dùng cung cấp (tệp, nội dung, liên kết) tạo một dòng tài liệu riêng.
            if(input.File!=null){var rel=await SaveUploadAsync(input.File,uid);_context.Documents.Add(NewDoc(uid,input.CourseId,input.Title,input.Kind=="link"?"note":input.Kind,"file:"+rel,(int)Math.Max(1,input.File.Length/1024),note));}
            if(hasContent){var rel=await SaveContentAsync(input.Content!,uid);_context.Documents.Add(NewDoc(uid,input.CourseId,input.Title,"note","content:"+rel,null,note));}
            if(url.Length>0)_context.Documents.Add(NewDoc(uid,input.CourseId,input.Title,input.Kind,url,null,note));
            await _context.SaveChangesAsync();TempData["SuccessMessage"]="Đã thêm tài liệu.";return RedirectToAction(nameof(Documents));
        }
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> DeleteDocument(int id)
        {var uid=User.GetUserId();var d=await _context.Documents.FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==uid);if(d==null)return NotFound();DeleteStored(d.FilePath);_context.Documents.Remove(d);await _context.SaveChangesAsync();return RedirectToAction(nameof(Documents));}

        private async Task<bool> HasScheduleConflict(int courseId,int day,string start,string end,int excludeId)
        {var startTime=TimeSpan.Parse(start);var endTime=TimeSpan.Parse(end);var uid=User.GetUserId();return await _context.CourseSchedules.AnyAsync(s=>s.Id!=excludeId&&s.DayOfWeek==day&&s.StartTime<endTime&&startTime<s.EndTime&&_context.Courses.Any(c=>c.Id==s.CourseId&&c.UserId==uid));}
        private async Task<int> WeightTotal(int courseId,int excludeId)=>await _context.Assignments.Where(a=>a.CourseId==courseId&&a.Id!=excludeId).SumAsync(a=>(int?)a.WeightPercent)??0;
        private static bool ValidTime(string start,string end)=>TimeSpan.TryParse(start,out var s)&&TimeSpan.TryParse(end,out var e)&&s>=TimeSpan.Zero&&e<=TimeSpan.FromDays(1)&&e>s;
        private static bool AllowedAssignmentKind(string? kind)=>kind is "assignment" or "quiz" or "project" or "midterm" or "final";
        private static bool AllowedDocumentKind(string? kind)=>kind is "pdf" or "slide" or "link" or "video" or "note";
        private static bool SafeDocumentUrl(string value)=>value.Length<=500&&(Uri.TryCreate(value,UriKind.Absolute,out var uri)&&uri.Scheme is "http" or "https"||value.StartsWith('/')&&!value.Contains("..")&&!value.Contains('\\'));
        private static bool ValidColor(string? color)=>color is {Length:7}&&color[0]=='#'&&color.Skip(1).All(Uri.IsHexDigit);

        // ================================================================== Tệp tải lên và nội dung soạn trong web
        // Tệp được lưu NGOÀI wwwroot (App_Data/study-uploads/study/{userId}/...) nên không ai mở được bằng đường dẫn trực tiếp;
        // chỉ chủ sở hữu tải được qua OpenDocument / ViewDocument. FilePath trong bảng documents có tiền tố:
        //   "file:..."    tệp tải lên từ máy     "content:..."  nội dung soạn trong web (HTML đã làm sạch)
        //   không tiền tố = liên kết ngoài.   Tệp thuộc bài tập có Description = "assignment:{id}".
        private const long MaxUploadBytes = 20L * 1024 * 1024;
        private const long MaxRequestBytes = 26L * 1024 * 1024;
        private const string AssignmentMarker = "assignment:";

        private static readonly Dictionary<string, (string Type, bool Inline)> AllowedUploads = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ("application/pdf", true), [".png"] = ("image/png", true), [".jpg"] = ("image/jpeg", true),
            [".jpeg"] = ("image/jpeg", true), [".webp"] = ("image/webp", true), [".mp4"] = ("video/mp4", true),
            [".txt"] = ("text/plain; charset=utf-8", true), [".md"] = ("text/plain; charset=utf-8", true),
            [".doc"] = ("application/msword", false),
            [".docx"] = ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", false),
            [".ppt"] = ("application/vnd.ms-powerpoint", false),
            [".pptx"] = ("application/vnd.openxmlformats-officedocument.presentationml.presentation", false),
            [".xls"] = ("application/vnd.ms-excel", false),
            [".xlsx"] = ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", false)
        };

        // Chỉ các thẻ định dạng văn bản; mọi thuộc tính, script, style, ảnh, liên kết đều bị loại.
        private static readonly HashSet<string> AllowedTags = new() { "p", "strong", "em", "u", "h2", "h3", "ul", "ol", "li", "blockquote" };

        private string UploadRoot => Path.Combine(_env.ContentRootPath, "App_Data", "study-uploads");

        private static string? ValidateUpload(IFormFile? file)
        {
            if (file == null) return null;
            if (file.Length == 0) return "Tệp tải lên đang trống.";
            if (file.Length > MaxUploadBytes) return "Tệp quá lớn, tối đa 20 MB.";
            if (!AllowedUploads.ContainsKey(Path.GetExtension(file.FileName))) return "Định dạng tệp chưa được hỗ trợ (PDF, Word, PowerPoint, Excel, ảnh, văn bản, MP4).";
            return null;
        }

        private async Task<string> SaveUploadAsync(IFormFile file, int uid)
        {
            var rel = $"study/{uid}/{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
            var full = Path.Combine(UploadRoot, rel.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            await using var stream = System.IO.File.Create(full);
            await file.CopyToAsync(stream);
            return rel;
        }

        private async Task<string> SaveContentAsync(string html, int uid)
        {
            var rel = $"study/{uid}/{Guid.NewGuid():N}.html";
            var full = Path.Combine(UploadRoot, rel.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            await System.IO.File.WriteAllTextAsync(full, SanitizeHtml(html), Encoding.UTF8);
            return rel;
        }

        // Đổi đường dẫn tương đối thành đường dẫn thật và bảo đảm nó nằm trong thư mục lưu trữ.
        private bool TryResolve(string rel, out string full)
        {
            var root = Path.GetFullPath(UploadRoot) + Path.DirectorySeparatorChar;
            full = Path.GetFullPath(Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar)));
            return full.StartsWith(root, StringComparison.Ordinal);
        }

        private void DeleteStored(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            var rel = filePath.StartsWith("file:") ? filePath[5..] : filePath.StartsWith("content:") ? filePath[8..] : null;
            if (rel == null) return;
            try { if (TryResolve(rel, out var full) && System.IO.File.Exists(full)) System.IO.File.Delete(full); }
            catch (IOException) { /* tệp đang bị khóa: bỏ qua, dòng dữ liệu vẫn được xóa */ }
        }

        private static bool HasText(string? html)
            => !string.IsNullOrWhiteSpace(WebUtility.HtmlDecode(Regex.Replace(html ?? "", "<[^>]*>", " ")).Replace('\u00a0', ' '));

        private static string SanitizeHtml(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            if (html.Length > 60000) html = html[..60000];
            var sb = new StringBuilder();
            var pos = 0;
            foreach (Match m in Regex.Matches(html, @"<(/?)([a-zA-Z][a-zA-Z0-9]*)\b[^>]*>"))
            {
                sb.Append(EncodeText(html.Substring(pos, m.Index - pos)));
                pos = m.Index + m.Length;
                var closing = m.Groups[1].Value == "/";
                var name = m.Groups[2].Value.ToLowerInvariant();
                name = name switch { "b" => "strong", "i" => "em", "div" => "p", _ => name };
                if (name == "br") { if (!closing) sb.Append("<br>"); continue; }
                if (AllowedTags.Contains(name)) sb.Append(closing ? $"</{name}>" : $"<{name}>");
            }
            sb.Append(EncodeText(html[pos..]));
            return sb.ToString();
        }

        private static string EncodeText(string s) => s.Replace("<", "&lt;").Replace(">", "&gt;");

        private static string SafeFileName(string name)
        {
            var clean = new string(Path.GetFileNameWithoutExtension(name).Where(ch => !Path.GetInvalidFileNameChars().Contains(ch)).ToArray()).Trim();
            if (clean.Length == 0) clean = "tai-lieu";
            return clean.Length > 80 ? clean[..80] : clean;
        }

        private static LifeHub.Models.KnowledgeManagement.Document NewDoc(int uid, int courseId, string name, string kind, string path, int? sizeKb, string? desc)
            => new() { UserId = uid, CourseId = courseId, FileName = name.Length > 200 ? name[..200] : name, FileType = kind, FilePath = path, FileSizeKb = sizeKb, Description = desc, UploadedAt = DateTime.Now };

        private string DocumentUrl(LifeHub.Models.KnowledgeManagement.Document d)
            => d.FilePath.StartsWith("file:") ? Url.Action(nameof(OpenDocument), new { id = d.Id }) ?? "#"
             : d.FilePath.StartsWith("content:") ? Url.Action(nameof(ViewDocument), new { id = d.Id }) ?? "#"
             : d.FilePath;

        private static string DocumentSource(LifeHub.Models.KnowledgeManagement.Document d)
            => d.FilePath.StartsWith("file:") ? "file" : d.FilePath.StartsWith("content:") ? "content" : "link";

        private static string DocumentNote(LifeHub.Models.KnowledgeManagement.Document d)
            => d.Description != null && d.Description.StartsWith(AssignmentMarker) ? "Đính kèm của bài tập" : d.Description ?? "";

        // Lưu tệp đính kèm và nội dung soạn của bài tập (bảng documents, Description = "assignment:{id}").
        private async Task SaveAssignmentExtrasAsync(LifeHub.Models.StudyManagement.Assignment a, CreateAssignmentInput input, int uid)
        {
            var marker = AssignmentMarker + a.Id;
            var docs = await _context.Documents.Where(d => d.UserId == uid && d.Description == marker).ToListAsync();
            var oldFile = docs.FirstOrDefault(d => d.FilePath.StartsWith("file:"));
            var oldContent = docs.FirstOrDefault(d => d.FilePath.StartsWith("content:"));
            foreach (var d in docs) d.CourseId = a.CourseId;   // đổi môn học thì tệp đi theo

            if (oldFile != null && (input.Attachment != null || input.RemoveAttachment))
            {
                DeleteStored(oldFile.FilePath);
                _context.Documents.Remove(oldFile);
            }
            if (input.Attachment != null)
            {
                var ext = Path.GetExtension(input.Attachment.FileName).ToLowerInvariant();
                var kind = ext == ".pdf" ? "pdf" : ext is ".ppt" or ".pptx" ? "slide" : ext == ".mp4" ? "video" : "note";
                var rel = await SaveUploadAsync(input.Attachment, uid);
                _context.Documents.Add(NewDoc(uid, a.CourseId, Path.GetFileName(input.Attachment.FileName), kind, "file:" + rel, (int)Math.Max(1, input.Attachment.Length / 1024), marker));
            }

            if (HasText(input.Content))
            {
                var rel = await SaveContentAsync(input.Content!, uid);
                if (oldContent != null)
                {
                    DeleteStored(oldContent.FilePath);
                    oldContent.FilePath = "content:" + rel;
                    oldContent.FileName = a.Title;
                    oldContent.UploadedAt = DateTime.Now;
                }
                else _context.Documents.Add(NewDoc(uid, a.CourseId, a.Title, "note", "content:" + rel, null, marker));
            }
            else if (oldContent != null)
            {
                DeleteStored(oldContent.FilePath);
                _context.Documents.Remove(oldContent);
            }
            await _context.SaveChangesAsync();
        }

        private async Task FillAssignmentExtrasAsync(List<StudyAssignmentItem> items, int uid)
        {
            if (items.Count == 0) return;
            var markers = items.Select(i => AssignmentMarker + i.Id).ToList();
            var docs = await _context.Documents.AsNoTracking().Where(d => d.UserId == uid && d.Description != null && markers.Contains(d.Description)).ToListAsync();
            foreach (var it in items)
            {
                var m = AssignmentMarker + it.Id;
                var f = docs.FirstOrDefault(d => d.Description == m && d.FilePath.StartsWith("file:"));
                var c = docs.FirstOrDefault(d => d.Description == m && d.FilePath.StartsWith("content:"));
                if (f != null) { it.AttachmentDocId = f.Id; it.AttachmentName = f.FileName; }
                if (c != null) it.ContentDocId = c.Id;
            }
        }

        private async Task<string> ReadContentAsync(LifeHub.Models.KnowledgeManagement.Document d)
            => TryResolve(d.FilePath[8..], out var full) && System.IO.File.Exists(full)
                ? SanitizeHtml(await System.IO.File.ReadAllTextAsync(full)) : string.Empty;

        // GET /Study/OpenDocument/5            mở tệp (PDF, ảnh, văn bản xem ngay; Word, PowerPoint... tải về)
        // GET /Study/OpenDocument/5?download=true  luôn tải về
        [HttpGet]
        public async Task<IActionResult> OpenDocument(int id, bool download = false)
        {
            var uid = User.GetUserId();
            var d = await _context.Documents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
            if (d == null || !d.FilePath.StartsWith("file:")) return NotFound();
            if (!TryResolve(d.FilePath[5..], out var full) || !System.IO.File.Exists(full)) return NotFound();
            var ext = Path.GetExtension(full);
            if (!AllowedUploads.TryGetValue(ext, out var info)) return NotFound();

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            var name = d.FileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase) ? d.FileName : SafeFileName(d.FileName) + ext;
            return PhysicalFile(full, info.Type, download || !info.Inline ? name : null);
        }

        // GET /Study/ViewDocument/5  đọc nội dung đã soạn trong web
        [HttpGet]
        public async Task<IActionResult> ViewDocument(int id)
        {
            Page("Tài liệu học tập", "StudyDocument");
            var uid = User.GetUserId();
            var d = await _context.Documents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
            if (d == null || !d.FilePath.StartsWith("content:")) return NotFound();

            var course = d.CourseId.HasValue ? await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == d.CourseId && c.UserId == uid) : null;
            var context = string.Empty;
            if (d.Description != null && d.Description.StartsWith(AssignmentMarker) && int.TryParse(d.Description[AssignmentMarker.Length..], out var asgId))
            {
                var title = await _context.Assignments.AsNoTracking().Where(a => a.Id == asgId && _context.Courses.Any(c => c.Id == a.CourseId && c.UserId == uid)).Select(a => a.Title).FirstOrDefaultAsync();
                if (title != null) context = "Bài tập: " + title;
            }

            ViewData["Title"] = d.FileName;
            return View("DocumentView", new StudyDocumentContentViewModel
            {
                Id = d.Id, Title = d.FileName, CourseCode = course?.CourseCode ?? "", CourseName = course?.CourseName ?? "",
                CourseColor = course?.Color ?? "#2563eb", ContextLabel = context, Html = await ReadContentAsync(d), UpdatedAt = d.UploadedAt
            });
        }

        // GET /Study/ExportDocument/5  tải nội dung về dạng .doc (Word mở trực tiếp)
        [HttpGet]
        public async Task<IActionResult> ExportDocument(int id)
        {
            var uid = User.GetUserId();
            var d = await _context.Documents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
            if (d == null || !d.FilePath.StartsWith("content:")) return NotFound();

            var body = await ReadContentAsync(d);
            var html = "<html xmlns:o='urn:schemas-microsoft-com:office:office' xmlns:w='urn:schemas-microsoft-com:office:word' xmlns='http://www.w3.org/TR/REC-html40'>"
                     + "<head><meta charset='utf-8'><title>" + WebUtility.HtmlEncode(d.FileName) + "</title></head><body>"
                     + "<h1>" + WebUtility.HtmlEncode(d.FileName) + "</h1>" + body + "</body></html>";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(Encoding.UTF8.GetBytes(html), "application/msword", SafeFileName(d.FileName) + ".doc");
        }

        // GET /Study/DocumentHtml?id=5  nội dung (đã làm sạch) để điền lại vào trình soạn thảo khi sửa bài tập
        [HttpGet]
        public async Task<IActionResult> DocumentHtml(int id)
        {
            var uid = User.GetUserId();
            var d = await _context.Documents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
            if (d == null || !d.FilePath.StartsWith("content:")) return NotFound();
            return Json(new { html = await ReadContentAsync(d) });
        }
    }
}
