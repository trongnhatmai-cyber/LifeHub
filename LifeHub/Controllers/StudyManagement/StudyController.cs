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
        public StudyController(ApplicationDbContext context) => _context = context;
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
            return View(model);
        }

        // GET /Study/Courses?status=current|completed|all
        public async Task<IActionResult> Courses(string? status, int? semesterId)
        {
            Page("Môn học và khóa học", "Course");
            var userId = User.GetUserId();
            var courses = await _context.Courses.AsNoTracking().Where(c => c.UserId == userId).ToListAsync();
            var courseIds = courses.Select(c => c.Id).ToArray();
            var assignments = await _context.Assignments.AsNoTracking().Where(a => courseIds.Contains(a.CourseId)).ToListAsync();
            var semesters = await _context.Semesters.AsNoTracking().Where(s => s.UserId == userId).OrderByDescending(s => s.IsCurrent).ThenBy(s => s.StartDate).ToListAsync();
            var semesterNames = semesters.ToDictionary(s => s.Id, s => s.Name);
            var schedules = await _context.CourseSchedules.AsNoTracking().Where(s => courseIds.Contains(s.CourseId)).ToListAsync();
            var key = (status ?? "current").ToLowerInvariant();
            if (key is not ("current" or "completed" or "all")) key = "current";
            if (semesterId.HasValue && !semesters.Any(s => s.Id == semesterId.Value)) semesterId = null;

            var filtered = courses.Where(c => key == "all" || (key == "completed" ? c.Status == "completed" : c.Status != "completed"));
            if (semesterId.HasValue) filtered = filtered.Where(c => c.SemesterId == semesterId.Value);
            var rows = filtered.Select(c => new StudyCourseRow
            {
                Course = CourseItem(c, assignments),
                SemesterId = c.SemesterId,
                SemesterName = c.SemesterId.HasValue && semesterNames.TryGetValue(c.SemesterId.Value, out var name) ? name : "",
                AssignmentCount = assignments.Count(a => a.CourseId == c.Id),
                SubmittedCount = assignments.Count(a => a.CourseId == c.Id && a.IsSubmitted),
                ScheduleSummary = string.Join(", ", schedules.Where(s => s.CourseId == c.Id).Select(s => $"{DayLabel((DayOfWeek)s.DayOfWeek)} {s.StartTime:hh\\:mm}"))
            }).ToList();
            var scoredCourses = courses.Select(c => new { Course = c, Score = CourseScore(c, assignments) }).Where(x => x.Score.HasValue).ToList();

            return View(new StudyCoursesPageViewModel
            {
                Status = key,
                SemesterId = semesterId,
                SemesterFilterName = semesterId.HasValue ? semesterNames[semesterId.Value] : "",
                Semesters = semesters.Select(SemesterItem).ToList(),
                CurrentCount = courses.Count(c => c.Status != "completed"),
                CompletedCount = courses.Count(c => c.Status == "completed"),
                CurrentCredits = courses.Where(c => c.Status != "completed").Sum(c => c.Credits ?? 0),
                AverageScore = scoredCourses.Count == 0 ? null : scoredCourses.Average(x => x.Score!.Value),
                Rows = rows
            });
        }

        // GET /Study/Semesters
        public async Task<IActionResult> Semesters()
        {
            Page("Học kỳ", "Semester");
            var userId = User.GetUserId();
            var semesters = await _context.Semesters.AsNoTracking().Where(s => s.UserId == userId).OrderBy(s => s.StartDate).ToListAsync();
            var courses = await _context.Courses.AsNoTracking().Where(c => c.UserId == userId).ToListAsync();
            var courseIds = courses.Select(c => c.Id).ToArray();
            var assignments = await _context.Assignments.AsNoTracking().Where(a => courseIds.Contains(a.CourseId)).ToListAsync();
            var today = DateOnly.FromDateTime(DateTime.Today);

            var rows = semesters.Select(semester =>
            {
                var semesterCourses = courses.Where(c => c.SemesterId == semester.Id).ToList();
                var scored = semesterCourses.Select(c => new { Course = c, Score = CourseScore(c, assignments) }).Where(x => x.Score.HasValue).ToList();
                var scoredCredits = scored.Sum(x => x.Course.Credits ?? 0);
                var startDay = semester.StartDate?.DayNumber;
                var endDay = semester.EndDate?.DayNumber;
                var span = startDay.HasValue && endDay.HasValue ? endDay.Value - startDay.Value : 0;
                var elapsed = startDay.HasValue ? today.DayNumber - startDay.Value : 0;
                var progress = span <= 0 ? 0 : Math.Clamp(elapsed * 100 / span, 0, 100);
                var statusKey = SemesterItem(semester).StatusKey(today);

                return new StudySemesterRow
                {
                    Semester = SemesterItem(semester),
                    StatusKey = statusKey,
                    StatusLabel = statusKey switch { "current" => "Đang học", "finished" => "Đã kết thúc", _ => "Sắp tới" },
                    CourseCount = semesterCourses.Count,
                    Credits = semesterCourses.Sum(c => c.Credits ?? 0),
                    Gpa4 = scoredCredits == 0 ? null : scored.Sum(x => x.Score!.Value / 10m * 4m * (x.Course.Credits ?? 0)) / scoredCredits,
                    TimeProgressPercent = progress,
                    DaysLeft = endDay.HasValue ? Math.Max(0, endDay.Value - today.DayNumber) : null,
                    Courses = semesterCourses.Select(c => CourseItem(c, assignments)).ToList()
                };
            }).ToList();

            var allScored = courses.Select(c => new { Course = c, Score = CourseScore(c, assignments) }).Where(x => x.Score.HasValue && (x.Course.Credits ?? 0) > 0).ToList();
            var totalCredits = allScored.Sum(x => x.Course.Credits ?? 0);
            return View(new StudySemestersPageViewModel
            {
                Rows = rows,
                CumulativeCredits = totalCredits,
                CumulativeGpa = totalCredits == 0 ? null : allScored.Sum(x => x.Score!.Value / 10m * 4m * (x.Course.Credits ?? 0)) / totalCredits
            });
        }

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
            return View(new StudyAssignmentsPageViewModel { Status=key,Kind=kindKey,CourseId=courseId,Q=q,Items=filtered.OrderBy(a=>a.DueDate).Select(a=>AssignmentItem(a,courses)).ToList(),Courses=courses.Select(c=>CourseItem(c,all)).ToList(),TotalCount=all.Count,PendingCount=all.Count(a=>!a.IsSubmitted&&!a.Score.HasValue&&a.DueDate>=DateTime.Today),OverdueCount=all.Count(a=>!a.IsSubmitted&&!a.Score.HasValue&&a.DueDate<DateTime.Today),DoneCount=all.Count(a=>a.IsSubmitted||a.Score.HasValue) });
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
            var uid=User.GetUserId(); var courses=await _context.Courses.AsNoTracking().Where(c=>c.UserId==uid).ToListAsync(); var ids=courses.Select(c=>c.Id).ToArray(); var key=(kind??"all").ToLowerInvariant(); var documents=await _context.Documents.AsNoTracking().Where(d=>d.UserId==uid&&d.CourseId.HasValue&&ids.Contains(d.CourseId.Value)).ToListAsync(); var query=documents.AsEnumerable(); if(courseId.HasValue)query=query.Where(d=>d.CourseId==courseId); if(key!="all")query=query.Where(d=>(d.FileType??"").ToLower()==key); if(!string.IsNullOrWhiteSpace(q))query=query.Where(d=>d.FileName.Contains(q.Trim(),StringComparison.OrdinalIgnoreCase)); var items=query.OrderByDescending(d=>d.UploadedAt).Select(d=>new StudyDocumentItem {Id=d.Id,Title=d.FileName,CourseId=d.CourseId!.Value,CourseCode=courses.First(c=>c.Id==d.CourseId).CourseCode,CourseColor=courses.First(c=>c.Id==d.CourseId).Color??"#2563eb",Kind=d.FileType??"pdf",Url=d.FilePath,SizeLabel=d.FileSizeKb.HasValue?$"{d.FileSizeKb} KB":"",Note=d.Description??"",AddedAt=d.UploadedAt}).ToList(); return View(new StudyDocumentsPageViewModel {CourseId=courseId,Kind=key,Q=q,Items=items,Courses=courses.Select(c=>CourseItem(c,new())).ToList(),TotalCount=documents.Count});
        }

        private static StudyCourseItem CourseItem(LifeHub.Models.StudyManagement.Course c, List<LifeHub.Models.StudyManagement.Assignment> assignments) => new() { Id=c.Id,Code=c.CourseCode,Name=c.CourseName,Credits=c.Credits??0,Instructor=c.InstructorName??"",Room=c.RoomLocation??"",Color=c.Color??"#2563eb",TargetScore=c.TargetScore,Status=c.Status=="completed"?"Đã hoàn thành":"Đang học",CurrentScore=CourseScore(c,assignments),AssignmentProgressPercent=assignments.Count(a=>a.CourseId==c.Id)==0?0:assignments.Count(a=>a.CourseId==c.Id&&a.IsSubmitted)*100/assignments.Count(a=>a.CourseId==c.Id) };
        private static StudySemesterItem SemesterItem(LifeHub.Models.StudyManagement.Semester s) => new() { Id=s.Id,Name=s.Name,StartDate=s.StartDate,EndDate=s.EndDate,IsCurrent=s.IsCurrent };
        private static decimal? CourseScore(LifeHub.Models.StudyManagement.Course course, List<LifeHub.Models.StudyManagement.Assignment> assignments)
        {
            if (course.FinalScore.HasValue) return course.FinalScore.Value;
            var graded = assignments.Where(a => a.CourseId == course.Id && a.Score.HasValue).ToList();
            if (graded.Count == 0) return null;
            decimal Normalize(LifeHub.Models.StudyManagement.Assignment a)
            {
                var max = a.MaxScore.GetValueOrDefault(10m);
                return max <= 0 ? 0 : Math.Clamp(a.Score!.Value / max * 10m, 0m, 10m);
            }
            var weighted = graded.Where(a => a.WeightPercent > 0).ToList();
            var weight = weighted.Sum(a => a.WeightPercent);
            return weight > 0
                ? weighted.Sum(a => Normalize(a) * a.WeightPercent) / weight
                : graded.Average(Normalize);
        }
        private static StudyAssignmentItem AssignmentItem(LifeHub.Models.StudyManagement.Assignment a,List<LifeHub.Models.StudyManagement.Course> courses)=>new(){Id=a.Id,Title=a.Title,CourseId=a.CourseId,CourseName=courses.FirstOrDefault(c=>c.Id==a.CourseId)?.CourseName??"",CourseCode=courses.FirstOrDefault(c=>c.Id==a.CourseId)?.CourseCode??"",DueDate=a.DueDate,WeightPercent=a.WeightPercent,IsSubmitted=a.IsSubmitted,Score=a.Score,Kind=a.AssignmentType,MaxScore=a.MaxScore??10,DurationMinutes=a.DurationMinutes,Location=a.Location};
        private static string DayLabel(DayOfWeek d)=>d switch {DayOfWeek.Monday=>"T2",DayOfWeek.Tuesday=>"T3",DayOfWeek.Wednesday=>"T4",DayOfWeek.Thursday=>"T5",DayOfWeek.Friday=>"T6",DayOfWeek.Saturday=>"T7",_=>"CN"};

        // ------------------------------------------------------------------ POST: mọi bản ghi được giới hạn theo người dùng hiện tại.

        // Học kỳ (bảng semesters)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSemester(SaveSemesterInput input)
        {
            var uid = User.GetUserId();
            input.Name = input.Name?.Trim() ?? string.Empty;
            if (!ValidSemester(input))
            {
                TempData["ErrorMessage"] = "Tên học kỳ và khoảng ngày không hợp lệ.";
                return RedirectToAction(nameof(Semesters));
            }

            var isFirst = !await _context.Semesters.AnyAsync(s => s.UserId == uid && s.IsCurrent);
            _context.Semesters.Add(new LifeHub.Models.StudyManagement.Semester
            {
                UserId = uid,
                Name = input.Name,
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                IsCurrent = isFirst
            });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã thêm học kỳ.";
            return RedirectToAction(nameof(Semesters));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSemester(SaveSemesterInput input)
        {
            var uid = User.GetUserId();
            var semester = await _context.Semesters.FirstOrDefaultAsync(s => s.Id == input.Id && s.UserId == uid);
            if (semester == null) return NotFound();
            input.Name = input.Name?.Trim() ?? string.Empty;
            if (!ValidSemester(input))
            {
                TempData["ErrorMessage"] = "Tên học kỳ và khoảng ngày không hợp lệ.";
                return RedirectToAction(nameof(Semesters));
            }

            semester.Name = input.Name;
            semester.StartDate = input.StartDate;
            semester.EndDate = input.EndDate;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật học kỳ.";
            return RedirectToAction(nameof(Semesters));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetCurrentSemester(int id)
        {
            var uid = User.GetUserId();
            var semesters = await _context.Semesters.Where(s => s.UserId == uid).ToListAsync();
            var selected = semesters.FirstOrDefault(s => s.Id == id);
            if (selected == null) return NotFound();
            foreach (var semester in semesters) semester.IsCurrent = semester.Id == id;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã đặt học kỳ hiện tại.";
            return RedirectToAction(nameof(Semesters));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSemester(int id)
        {
            var uid = User.GetUserId();
            var semester = await _context.Semesters.FirstOrDefaultAsync(s => s.Id == id && s.UserId == uid);
            if (semester == null) return NotFound();

            var affectedCourses = await _context.Courses.Where(c => c.UserId == uid && c.SemesterId == id).ToListAsync();
            foreach (var course in affectedCourses) course.SemesterId = null;
            _context.Semesters.Remove(semester);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa học kỳ. Các môn học đã được giữ lại và bỏ liên kết học kỳ.";
            return RedirectToAction(nameof(Semesters));
        }

        private static bool ValidSemester(SaveSemesterInput input) =>
            !string.IsNullOrWhiteSpace(input.Name) && input.Name.Length <= 100 &&
            input.StartDate.HasValue && input.EndDate.HasValue && input.EndDate.Value > input.StartDate.Value;

        // Môn học (bảng courses)
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> CreateCourse(CreateCourseInput input)
        {
            var uid=User.GetUserId();input.Code=input.Code?.Trim()??"";input.Name=input.Name?.Trim()??"";
            if(!ModelState.IsValid||!ValidColor(input.Color)||(input.SemesterId.HasValue&&!await _context.Semesters.AnyAsync(s=>s.Id==input.SemesterId&&s.UserId==uid))||await _context.Courses.AnyAsync(c=>c.UserId==uid&&c.CourseCode==input.Code)){ModelState.AddModelError(string.Empty,"Thông tin môn học không hợp lệ hoặc mã môn đã tồn tại.");return RedirectToAction(nameof(Courses));}
            _context.Courses.Add(new LifeHub.Models.StudyManagement.Course{UserId=uid,SemesterId=input.SemesterId,CourseCode=input.Code,CourseName=input.Name,Credits=input.Credits,InstructorName=input.Instructor?.Trim(),RoomLocation=input.Room?.Trim(),Color=input.Color,TargetScore=input.TargetScore,Status="active",CreatedAt=DateTime.Now});
            await _context.SaveChangesAsync();TempData["SuccessMessage"]="Đã thêm môn học.";return RedirectToAction(nameof(Courses));
        }
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> UpdateCourse(UpdateCourseInput input)
        {
            var uid=User.GetUserId();if(!ModelState.IsValid||!ValidColor(input.Color)||input.Status is not ("Đang học" or "Đã hoàn thành")||(input.SemesterId.HasValue&&!await _context.Semesters.AnyAsync(s=>s.Id==input.SemesterId&&s.UserId==uid))){TempData["ErrorMessage"]="Thông tin môn học không hợp lệ.";return RedirectToAction(nameof(Courses));}
            var c=await _context.Courses.FirstOrDefaultAsync(x=>x.Id==input.Id&&x.UserId==uid);if(c==null)return NotFound();
            input.Code=input.Code?.Trim()??"";input.Name=input.Name?.Trim()??"";if(await _context.Courses.AnyAsync(x=>x.UserId==uid&&x.Id!=c.Id&&x.CourseCode==input.Code)){TempData["ErrorMessage"]="Mã môn đã được sử dụng.";return RedirectToAction(nameof(Courses));}
            c.SemesterId=input.SemesterId;c.CourseCode=input.Code;c.CourseName=input.Name;c.Credits=input.Credits;c.InstructorName=input.Instructor?.Trim();c.RoomLocation=input.Room?.Trim();c.Color=input.Color;c.TargetScore=input.TargetScore;c.Status=input.Status=="Đã hoàn thành"?"completed":"active";await _context.SaveChangesAsync();return RedirectToAction(nameof(Courses));
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
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> CreateAssignment(CreateAssignmentInput input)
        {var uid=User.GetUserId();input.Title=input.Title?.Trim()??"";if(!ModelState.IsValid||!AllowedAssignmentKind(input.Kind)||!await _context.Courses.AnyAsync(c=>c.Id==input.CourseId&&c.UserId==uid)||await WeightTotal(input.CourseId,0)+input.WeightPercent>100){TempData["ErrorMessage"]="Bài tập không hợp lệ hoặc tổng trọng số vượt 100%.";return RedirectToAction(nameof(Assignments));}_context.Assignments.Add(new LifeHub.Models.StudyManagement.Assignment{CourseId=input.CourseId,Title=input.Title,AssignmentType=input.Kind,DueDate=input.DueDate,WeightPercent=input.WeightPercent,MaxScore=input.MaxScore,DurationMinutes=input.DurationMinutes,Location=input.Location?.Trim()});await _context.SaveChangesAsync();return RedirectToAction(nameof(Assignments));}
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> UpdateAssignment(UpdateAssignmentInput input)
        {var uid=User.GetUserId();var a=await _context.Assignments.FirstOrDefaultAsync(x=>x.Id==input.Id&&_context.Courses.Any(c=>c.Id==x.CourseId&&c.UserId==uid));if(a==null)return NotFound();if(!ModelState.IsValid||!AllowedAssignmentKind(input.Kind)||!await _context.Courses.AnyAsync(c=>c.Id==input.CourseId&&c.UserId==uid)||await WeightTotal(input.CourseId,input.Id)+input.WeightPercent>100){TempData["ErrorMessage"]="Bài tập không hợp lệ hoặc tổng trọng số vượt 100%.";return RedirectToAction(nameof(Assignments));}a.CourseId=input.CourseId;a.Title=input.Title.Trim();a.AssignmentType=input.Kind;a.DueDate=input.DueDate;a.WeightPercent=input.WeightPercent;a.MaxScore=input.MaxScore;a.DurationMinutes=input.DurationMinutes;a.Location=input.Location?.Trim();await _context.SaveChangesAsync();return RedirectToAction(nameof(Assignments));}
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> DeleteAssignment(int id)
        {var uid=User.GetUserId();var a=await _context.Assignments.FirstOrDefaultAsync(x=>x.Id==id&&_context.Courses.Any(c=>c.Id==x.CourseId&&c.UserId==uid));if(a==null)return NotFound();_context.Assignments.Remove(a);await _context.SaveChangesAsync();return RedirectToAction(nameof(Assignments));}
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> ToggleSubmitted(int id)
        {var uid=User.GetUserId();var a=await _context.Assignments.FirstOrDefaultAsync(x=>x.Id==id&&_context.Courses.Any(c=>c.Id==x.CourseId&&c.UserId==uid));if(a==null)return NotFound();a.IsSubmitted=!a.IsSubmitted;await _context.SaveChangesAsync();return RedirectToAction(nameof(Assignments));}
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> RecordScore(RecordScoreInput input)
        {var uid=User.GetUserId();var a=await _context.Assignments.FirstOrDefaultAsync(x=>x.Id==input.Id&&_context.Courses.Any(c=>c.Id==x.CourseId&&c.UserId==uid));if(a==null)return NotFound();if(!ModelState.IsValid||input.Score>(a.MaxScore??10)){TempData["ErrorMessage"]="Điểm phải nằm trong thang điểm của bài.";return RedirectToAction(nameof(Assignments));}a.Score=input.Score;a.IsSubmitted=true;await _context.SaveChangesAsync();return RedirectToAction(nameof(Grades));}

        // Điểm số
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> SetTargetScore(SetTargetScoreInput input)
        {var uid=User.GetUserId();if(!ModelState.IsValid)return RedirectToAction(nameof(Grades));var c=await _context.Courses.FirstOrDefaultAsync(x=>x.Id==input.CourseId&&x.UserId==uid);if(c==null)return NotFound();c.TargetScore=input.TargetScore;await _context.SaveChangesAsync();return RedirectToAction(nameof(Grades));}

        // Tài liệu học tập
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> CreateDocument(CreateDocumentInput input)
        {var uid=User.GetUserId();input.Title=input.Title?.Trim()??"";var url=input.Url?.Trim()??"";if(!ModelState.IsValid||!AllowedDocumentKind(input.Kind)||input.Note?.Length>255||!await _context.Courses.AnyAsync(c=>c.Id==input.CourseId&&c.UserId==uid)||!SafeDocumentUrl(url)){TempData["ErrorMessage"]="Thông tin tài liệu không hợp lệ.";return RedirectToAction(nameof(Documents));}_context.Documents.Add(new LifeHub.Models.KnowledgeManagement.Document{UserId=uid,CourseId=input.CourseId,FileName=input.Title,FilePath=url,FileType=input.Kind,Description=input.Note?.Trim(),UploadedAt=DateTime.Now});await _context.SaveChangesAsync();return RedirectToAction(nameof(Documents));}
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> DeleteDocument(int id)
        {var uid=User.GetUserId();var d=await _context.Documents.FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==uid);if(d==null)return NotFound();_context.Documents.Remove(d);await _context.SaveChangesAsync();return RedirectToAction(nameof(Documents));}

        private async Task<bool> HasScheduleConflict(int courseId,int day,string start,string end,int excludeId)
        {var startTime=TimeSpan.Parse(start);var endTime=TimeSpan.Parse(end);var uid=User.GetUserId();return await _context.CourseSchedules.AnyAsync(s=>s.Id!=excludeId&&s.DayOfWeek==day&&s.StartTime<endTime&&startTime<s.EndTime&&_context.Courses.Any(c=>c.Id==s.CourseId&&c.UserId==uid));}
        private async Task<int> WeightTotal(int courseId,int excludeId)=>await _context.Assignments.Where(a=>a.CourseId==courseId&&a.Id!=excludeId).SumAsync(a=>(int?)a.WeightPercent)??0;
        private static bool ValidTime(string start,string end)=>TimeSpan.TryParse(start,out var s)&&TimeSpan.TryParse(end,out var e)&&s>=TimeSpan.Zero&&e<=TimeSpan.FromDays(1)&&e>s;
        private static bool AllowedAssignmentKind(string? kind)=>kind is "assignment" or "quiz" or "project" or "midterm" or "final";
        private static bool AllowedDocumentKind(string? kind)=>kind is "pdf" or "slide" or "link" or "video" or "note";
        private static bool SafeDocumentUrl(string value)=>value.Length<=500&&(Uri.TryCreate(value,UriKind.Absolute,out var uri)&&uri.Scheme is "http" or "https"||value.StartsWith('/')&&!value.Contains("..")&&!value.Contains('\\'));
        private static bool ValidColor(string? color)=>color is {Length:7}&&color[0]=='#'&&color.Skip(1).All(Uri.IsHexDigit);
    }
}
