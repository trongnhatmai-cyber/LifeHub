using Microsoft.EntityFrameworkCore;
using LifeHub.Models;
using LifeHub.Models.SystemManagement;
using LifeHub.Models.StudyManagement;
using LifeHub.Models.ProjectManagement;
using LifeHub.Models.TaskManagement;
using LifeHub.Models.TimeManagement;
using LifeHub.Models.GoalManagement;
using LifeHub.Models.HabitManagement;
using LifeHub.Models.FinanceManagement;
using LifeHub.Models.KnowledgeManagement;
using LifeHub.Models.LifestyleManagement;
using LifeHub.Models.AIManagement;
using System.Text.RegularExpressions;
namespace LifeHub.DataAccessLayer;

public class ApplicationDbContext :DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }
     // 1. Account
    public DbSet<User> Users => Set<User>();

    // 2. System Management
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();

    // 3. Study Management
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseSchedule> CourseSchedules => Set<CourseSchedule>();
    public DbSet<Assignment> Assignments => Set<Assignment>();

    // 4. Project Management
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectRole> ProjectRoles => Set<ProjectRole>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<ProjectInvitation> ProjectInvitations => Set<ProjectInvitation>();
    public DbSet<ProjectMilestone> ProjectMilestones => Set<ProjectMilestone>();
    public DbSet<ProjectActivity> ProjectActivities => Set<ProjectActivity>();

    // 5. Task Management
    public DbSet<LifeHub.Models.TaskManagement.Task> Tasks
        => Set<LifeHub.Models.TaskManagement.Task>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TaskTag> TaskTags => Set<TaskTag>();

    // 6. Time Management
    public DbSet<Event> Events => Set<Event>();
    public DbSet<PomodoroSession> PomodoroSessions => Set<PomodoroSession>();

    // 7. Goal Management
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<KeyResult> KeyResults => Set<KeyResult>();

    // 8. Habit Management
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<HabitLog> HabitLogs => Set<HabitLog>();

    // 9. Finance Management
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<SavingsGoal> SavingsGoals => Set<SavingsGoal>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();

    // 10. Knowledge Management
    public DbSet<NoteCategory> NoteCategories => Set<NoteCategory>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<NoteTag> NoteTags => Set<NoteTag>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<SharedItem> SharedItems => Set<SharedItem>();

    // 11. Lifestyle Management
    public DbSet<LifestyleLog> LifestyleLogs => Set<LifestyleLog>();
    public DbSet<ProductivityScore> ProductivityScores => Set<ProductivityScore>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();

    // 12. AI Management
    public DbSet<AiConversation> AiConversations => Set<AiConversation>();
    public DbSet<AiMessage> AiMessages => Set<AiMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Project Management
        modelBuilder.Entity<ProjectMember>()
            .HasKey(pm => new { pm.ProjectId, pm.UserId });

        // 2. Task Management
        modelBuilder.Entity<TaskTag>()
            .HasKey(tt => new { tt.TaskId, tt.TagId });

        // 3. Knowledge Management
        modelBuilder.Entity<NoteTag>()
            .HasKey(nt => new { nt.NoteId, nt.TagId });

        // 4. Lifestyle Management
        modelBuilder.Entity<UserAchievement>()
            .HasKey(ua => new { ua.UserId, ua.AchievementId });

        // 5. Habit Management
        modelBuilder.Entity<HabitLog>()
            .HasIndex(hl => new { hl.HabitId, hl.CheckDate })
            .IsUnique();

        // 6. Finance Management
        modelBuilder.Entity<Budget>()
            .HasIndex(b => new
            {
                b.UserId,
                b.CategoryId,
                b.Month,
                b.Year
            })
            .IsUnique();
        // ==========================================
        // 7. ACCOUNT & SYSTEM MANAGEMENT
        // ==========================================

        // User - UserSettings (1 - 1)
        modelBuilder.Entity<UserSettings>()
            .HasOne<User>()
            .WithOne()
            .HasForeignKey<UserSettings>(us => us.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - UserToken (1 - N)
        modelBuilder.Entity<UserToken>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(ut => ut.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - Notification (1 - N)
        modelBuilder.Entity<Notification>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - Reminder (1 - N)
        modelBuilder.Entity<Reminder>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - SystemLog (1 - N, UserId nullable)
        modelBuilder.Entity<SystemLog>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(sl => sl.UserId)
            .OnDelete(DeleteBehavior.Restrict);


        // ==========================================
        // 8. STUDY MANAGEMENT
        // ==========================================

        // User - Semester
        modelBuilder.Entity<Semester>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - Course
        modelBuilder.Entity<Course>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Semester - Course (SemesterId nullable)
        modelBuilder.Entity<Course>()
            .HasOne<Semester>()
            .WithMany()
            .HasForeignKey(c => c.SemesterId)
            .OnDelete(DeleteBehavior.Restrict);

        // Course - CourseSchedule
        modelBuilder.Entity<CourseSchedule>()
            .HasOne<Course>()
            .WithMany()
            .HasForeignKey(cs => cs.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Course - Assignment
        modelBuilder.Entity<Assignment>()
            .HasOne<Course>()
            .WithMany()
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
        // ==========================================
        // 9. PROJECT MANAGEMENT
        // ==========================================

        // User - Project (người tạo dự án)
        modelBuilder.Entity<Project>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Project - ProjectMember
        modelBuilder.Entity<ProjectMember>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - ProjectMember
        modelBuilder.Entity<ProjectMember>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ProjectRole - ProjectMember
        modelBuilder.Entity<ProjectMember>()
            .HasOne<ProjectRole>()
            .WithMany()
            .HasForeignKey(pm => pm.ProjectRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Project - ProjectInvitation
        modelBuilder.Entity<ProjectInvitation>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(pi => pi.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - ProjectInvitation (người gửi lời mời)
        modelBuilder.Entity<ProjectInvitation>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(pi => pi.InvitedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // User - ProjectInvitation (người nhận lời mời)
        modelBuilder.Entity<ProjectInvitation>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(pi => pi.InvitedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ProjectRole - ProjectInvitation
        modelBuilder.Entity<ProjectInvitation>()
            .HasOne<ProjectRole>()
            .WithMany()
            .HasForeignKey(pi => pi.ProjectRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Project - ProjectMilestone
        modelBuilder.Entity<ProjectMilestone>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // Project - ProjectActivity
        modelBuilder.Entity<ProjectActivity>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(pa => pa.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - ProjectActivity
        modelBuilder.Entity<ProjectActivity>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(pa => pa.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        // ==========================================
        // 10. TASK MANAGEMENT
        // ==========================================

        // User - Task (người tạo)
        modelBuilder.Entity<LifeHub.Models.TaskManagement.Task>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - Task (người được giao)
        modelBuilder.Entity<LifeHub.Models.TaskManagement.Task>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.AssignedTo)
            .OnDelete(DeleteBehavior.Restrict);

        // Project - Task
        modelBuilder.Entity<LifeHub.Models.TaskManagement.Task>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // Task - Task (Subtask)
        modelBuilder.Entity<LifeHub.Models.TaskManagement.Task>()
            .HasOne<LifeHub.Models.TaskManagement.Task>()
            .WithMany()
            .HasForeignKey(t => t.ParentTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        // Goal - Task
        modelBuilder.Entity<LifeHub.Models.TaskManagement.Task>()
            .HasOne<Goal>()
            .WithMany()
            .HasForeignKey(t => t.GoalId)
            .OnDelete(DeleteBehavior.Restrict);

        // Course - Task
        modelBuilder.Entity<LifeHub.Models.TaskManagement.Task>()
            .HasOne<Course>()
            .WithMany()
            .HasForeignKey(t => t.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Task - TaskComment
        modelBuilder.Entity<TaskComment>()
            .HasOne<LifeHub.Models.TaskManagement.Task>()
            .WithMany()
            .HasForeignKey(tc => tc.TaskId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - TaskComment
        modelBuilder.Entity<TaskComment>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(tc => tc.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - Tag
        modelBuilder.Entity<Tag>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Task - TaskTag
        modelBuilder.Entity<TaskTag>()
            .HasOne<LifeHub.Models.TaskManagement.Task>()
            .WithMany()
            .HasForeignKey(tt => tt.TaskId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tag - TaskTag
        modelBuilder.Entity<TaskTag>()
            .HasOne<Tag>()
            .WithMany()
            .HasForeignKey(tt => tt.TagId)
            .OnDelete(DeleteBehavior.Restrict);
        // ==========================================
        // 11. TIME MANAGEMENT
        // ==========================================

        // User - Event
        modelBuilder.Entity<Event>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - PomodoroSession
        modelBuilder.Entity<PomodoroSession>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Task - PomodoroSession
        modelBuilder.Entity<PomodoroSession>()
            .HasOne<LifeHub.Models.TaskManagement.Task>()
            .WithMany()
            .HasForeignKey(p => p.TaskId)
            .OnDelete(DeleteBehavior.Restrict);

        // Course - PomodoroSession
        modelBuilder.Entity<PomodoroSession>()
            .HasOne<Course>()
            .WithMany()
            .HasForeignKey(p => p.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
        // ==========================================
        // 12. GOAL MANAGEMENT
        // ==========================================

        // User - Goal
        modelBuilder.Entity<Goal>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Goal - KeyResult
        modelBuilder.Entity<KeyResult>()
            .HasOne<Goal>()
            .WithMany()
            .HasForeignKey(kr => kr.GoalId)
            .OnDelete(DeleteBehavior.Restrict);
        // ==========================================
        // 13. HABIT MANAGEMENT
        // ==========================================

        // User - Habit
        modelBuilder.Entity<Habit>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Goal - Habit (GoalId nullable)
        modelBuilder.Entity<Habit>()
            .HasOne<Goal>()
            .WithMany()
            .HasForeignKey(h => h.GoalId)
            .OnDelete(DeleteBehavior.Restrict);

        // Habit - HabitLog
        modelBuilder.Entity<HabitLog>()
            .HasOne<Habit>()
            .WithMany()
            .HasForeignKey(hl => hl.HabitId)
            .OnDelete(DeleteBehavior.Restrict);
        // ==========================================
        // 14. FINANCE MANAGEMENT
        // ==========================================

        // User - Category (Category.UserId nullable)
        modelBuilder.Entity<Category>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Category - Category (Danh mục cha/con)
        modelBuilder.Entity<Category>()
            .HasOne<Category>()
            .WithMany()
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - Wallet
        modelBuilder.Entity<Wallet>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - SavingsGoal
        modelBuilder.Entity<SavingsGoal>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(sg => sg.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Goal - SavingsGoal (GoalId nullable)
        modelBuilder.Entity<SavingsGoal>()
            .HasOne<Goal>()
            .WithMany()
            .HasForeignKey(sg => sg.GoalId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - Transaction
        modelBuilder.Entity<Transaction>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Wallet - Transaction (Ví nguồn)
        modelBuilder.Entity<Transaction>()
            .HasOne<Wallet>()
            .WithMany()
            .HasForeignKey(t => t.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        // Wallet - Transaction (Ví đích)
        modelBuilder.Entity<Transaction>()
            .HasOne<Wallet>()
            .WithMany()
            .HasForeignKey(t => t.ToWalletId)
            .OnDelete(DeleteBehavior.Restrict);

        // Category - Transaction (CategoryId nullable)
        modelBuilder.Entity<Transaction>()
            .HasOne<Category>()
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // SavingsGoal - Transaction (SavingsGoalId nullable)
        modelBuilder.Entity<Transaction>()
            .HasOne<SavingsGoal>()
            .WithMany()
            .HasForeignKey(t => t.SavingsGoalId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - Budget
        modelBuilder.Entity<Budget>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Category - Budget
        modelBuilder.Entity<Budget>()
            .HasOne<Category>()
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        // ==========================================
        // 15. KNOWLEDGE MANAGEMENT
        // ==========================================

        // User - NoteCategory
        modelBuilder.Entity<NoteCategory>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(nc => nc.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // NoteCategory - NoteCategory (Danh mục cha/con)
        modelBuilder.Entity<NoteCategory>()
            .HasOne<NoteCategory>()
            .WithMany()
            .HasForeignKey(nc => nc.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - Note
        modelBuilder.Entity<Note>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // NoteCategory - Note
        modelBuilder.Entity<Note>()
            .HasOne<NoteCategory>()
            .WithMany()
            .HasForeignKey(n => n.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Course - Note
        modelBuilder.Entity<Note>()
            .HasOne<Course>()
            .WithMany()
            .HasForeignKey(n => n.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Project - Note
        modelBuilder.Entity<Note>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(n => n.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // Note - NoteTag
        modelBuilder.Entity<NoteTag>()
            .HasOne<Note>()
            .WithMany()
            .HasForeignKey(nt => nt.NoteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tag - NoteTag
        modelBuilder.Entity<NoteTag>()
            .HasOne<Tag>()
            .WithMany()
            .HasForeignKey(nt => nt.TagId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - Document
        modelBuilder.Entity<Document>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Project - Document
        modelBuilder.Entity<Document>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // Course - Document
        modelBuilder.Entity<Document>()
            .HasOne<Course>()
            .WithMany()
            .HasForeignKey(d => d.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Task - Document
        modelBuilder.Entity<Document>()
            .HasOne<LifeHub.Models.TaskManagement.Task>()
            .WithMany()
            .HasForeignKey(d => d.TaskId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - SharedItem (Owner)
        modelBuilder.Entity<SharedItem>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(si => si.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - SharedItem (SharedWith)
        modelBuilder.Entity<SharedItem>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(si => si.SharedWithUserId)
            .OnDelete(DeleteBehavior.Restrict);
        // ==========================================
        // 16. LIFESTYLE MANAGEMENT
        // ==========================================

        // User - LifestyleLog
        modelBuilder.Entity<LifestyleLog>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - ProductivityScore
        modelBuilder.Entity<ProductivityScore>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - UserAchievement
        modelBuilder.Entity<UserAchievement>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(ua => ua.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Achievement - UserAchievement
        modelBuilder.Entity<UserAchievement>()
            .HasOne<Achievement>()
            .WithMany()
            .HasForeignKey(ua => ua.AchievementId)
            .OnDelete(DeleteBehavior.Restrict);
        // ==========================================
        // 17. AI MANAGEMENT
        // ==========================================

        // User - AiConversation
        modelBuilder.Entity<AiConversation>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(ac => ac.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // AiConversation - AiMessage
        modelBuilder.Entity<AiMessage>()
            .HasOne<AiConversation>()
            .WithMany()
            .HasForeignKey(am => am.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);
        // ==========================================
        // 18. ADDITIONAL INDEXES
        // ==========================================

        // Account
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Notification
        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.IsRead });

        // Reminder
        modelBuilder.Entity<Reminder>()
            .HasIndex(r => new { r.IsSent, r.RemindAt });

        // Project Invitation
        modelBuilder.Entity<ProjectInvitation>()
            .HasIndex(pi => pi.Token)
            .IsUnique();

        // Task
        modelBuilder.Entity<LifeHub.Models.TaskManagement.Task>()
            .HasIndex(t => new { t.AssignedTo, t.Status });

        modelBuilder.Entity<LifeHub.Models.TaskManagement.Task>()
            .HasIndex(t => new { t.ProjectId, t.Status });

        modelBuilder.Entity<LifeHub.Models.TaskManagement.Task>()
            .HasIndex(t => t.Deadline);

        // Tag
        modelBuilder.Entity<Tag>()
            .HasIndex(t => new { t.UserId, t.Name })
            .IsUnique();

        // Event
        modelBuilder.Entity<Event>()
            .HasIndex(e => new { e.UserId, e.StartTime });

        modelBuilder.Entity<Event>()
            .HasIndex(e => new { e.SourceType, e.SourceId });

        // Transaction
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => new { t.UserId, t.TransactionDate });

        // Lifestyle
        modelBuilder.Entity<LifestyleLog>()
            .HasIndex(l => new { l.UserId, l.LogDate })
            .IsUnique();

        // Productivity
        modelBuilder.Entity<ProductivityScore>()
            .HasIndex(p => new { p.UserId, p.ScoreDate })
            .IsUnique();

        // Achievement
        modelBuilder.Entity<Achievement>()
            .HasIndex(a => a.BadgeCode)
            .IsUnique();
        // ==========================================
        // 19. TABLE MAPPING
        // ==========================================

        // 1. Account & System Management
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<UserSettings>().ToTable("user_settings");
        modelBuilder.Entity<UserToken>().ToTable("user_tokens");
        modelBuilder.Entity<Notification>().ToTable("notifications");
        modelBuilder.Entity<Reminder>().ToTable("reminders");
        modelBuilder.Entity<SystemLog>().ToTable("system_logs");

        // 2. Study Management
        modelBuilder.Entity<Semester>().ToTable("semesters");
        modelBuilder.Entity<Course>().ToTable("courses");
        modelBuilder.Entity<CourseSchedule>().ToTable("course_schedules");
        modelBuilder.Entity<Assignment>().ToTable("assignments");

        // 3. Project Management
        modelBuilder.Entity<Project>().ToTable("projects");
        modelBuilder.Entity<ProjectRole>().ToTable("project_roles");
        modelBuilder.Entity<ProjectMember>().ToTable("project_members");
        modelBuilder.Entity<ProjectInvitation>().ToTable("project_invitations");
        modelBuilder.Entity<ProjectMilestone>().ToTable("project_milestones");
        modelBuilder.Entity<ProjectActivity>().ToTable("project_activities");

        // 4. Task Management
        modelBuilder.Entity<LifeHub.Models.TaskManagement.Task>().ToTable("tasks");
        modelBuilder.Entity<TaskComment>().ToTable("task_comments");
        modelBuilder.Entity<Tag>().ToTable("tags");
        modelBuilder.Entity<TaskTag>().ToTable("task_tags");

        // 5. Time Management
        modelBuilder.Entity<Event>().ToTable("events");
        modelBuilder.Entity<PomodoroSession>().ToTable("pomodoro_sessions");

        // 6. Goal Management
        modelBuilder.Entity<Goal>().ToTable("goals");
        modelBuilder.Entity<KeyResult>().ToTable("key_results");

        // 7. Habit Management
        modelBuilder.Entity<Habit>().ToTable("habits");
        modelBuilder.Entity<HabitLog>().ToTable("habit_logs");

        // 8. Finance Management
        modelBuilder.Entity<Category>().ToTable("categories");
        modelBuilder.Entity<Wallet>().ToTable("wallets");
        modelBuilder.Entity<SavingsGoal>().ToTable("savings_goals");
        modelBuilder.Entity<Transaction>().ToTable("transactions");
        modelBuilder.Entity<Budget>().ToTable("budgets");

        // 9. Knowledge Management
        modelBuilder.Entity<NoteCategory>().ToTable("note_categories");
        modelBuilder.Entity<Note>().ToTable("notes");
        modelBuilder.Entity<NoteTag>().ToTable("note_tags");
        modelBuilder.Entity<Document>().ToTable("documents");
        modelBuilder.Entity<SharedItem>().ToTable("shared_items");

        // 10. Lifestyle Management
        modelBuilder.Entity<LifestyleLog>().ToTable("lifestyle_logs");
        modelBuilder.Entity<ProductivityScore>().ToTable("productivity_scores");
        modelBuilder.Entity<Achievement>().ToTable("achievements");
        modelBuilder.Entity<UserAchievement>().ToTable("user_achievements");

        // 11. AI Management
        modelBuilder.Entity<AiConversation>().ToTable("ai_conversations");
        modelBuilder.Entity<AiMessage>().ToTable("ai_messages");

        // ==========================================
        // 20. COLUMN NAME MAPPING (PascalCase -> snake_case)
        // ==========================================

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                string columnName = Regex.Replace(
                    property.Name,
                    "([A-Z]+)([A-Z][a-z])|([a-z0-9])([A-Z])",
                    "$1$3_$2$4"
                ).ToLowerInvariant();

                property.SetColumnName(columnName);
            }
        }
        // UserRole enum được lưu vào cột role_id
        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasColumnName("role_id")
            .HasConversion<int>();

        // ==========================================
        // 21. USER CONFIGURATION
        // ==========================================

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Email)
                .HasColumnType("varchar(100)")
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(u => u.PasswordHash)
                .HasColumnType("varchar(255)")
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.Property(u => u.FullName)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);

            entity.Property(u => u.PhoneNumber)
                .HasColumnType("varchar(15)")
                .HasMaxLength(15)
                .IsUnicode(false);

            entity.Property(u => u.AvatarUrl)
                .HasColumnType("nvarchar(255)")
                .HasMaxLength(255);

            entity.Property(u => u.EmailVerified)
                .HasDefaultValue(false);

            entity.Property(u => u.Active)
                .HasDefaultValue(true);

            entity.Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });
        // ==========================================
        // 22. USER TOKEN CONFIGURATION
        // ==========================================

        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.Property(x => x.TokenType)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.Property(x => x.TokenHash)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // ==========================================
        // 23. NOTIFICATION CONFIGURATION
        // ==========================================

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(150);

            entity.Property(x => x.NotificationType)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.Property(x => x.RefType)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.Property(x => x.LinkUrl)
                .HasMaxLength(255)
                .HasColumnType("nvarchar(255)");

            entity.Property(x => x.IsRead).HasDefaultValue(false);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // ==========================================
        // 24. REMINDER CONFIGURATION
        // ==========================================

        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.Property(x => x.RefType)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.Property(x => x.IsSent).HasDefaultValue(false);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // ==========================================
        // 25. SYSTEM LOG CONFIGURATION
        // ==========================================

        modelBuilder.Entity<SystemLog>(entity =>
        {
            entity.Property(x => x.Action)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(x => x.IpAddress)
                .HasMaxLength(45)
                .IsUnicode(false);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });
        // ==========================================
        // 26. STUDY MANAGEMENT CONFIGURATION
        // ==========================================

        // Semester
        modelBuilder.Entity<Semester>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.IsCurrent).HasDefaultValue(false);
        });

        // Course
        modelBuilder.Entity<Course>(entity =>
        {
            entity.Property(x => x.CourseCode)
                .HasMaxLength(30).IsUnicode(false);

            entity.Property(x => x.CourseName).HasMaxLength(150);
            entity.Property(x => x.InstructorName).HasMaxLength(100);
            entity.Property(x => x.RoomLocation).HasMaxLength(100);

            entity.Property(x => x.Color)
                .HasMaxLength(15).IsUnicode(false);

            entity.Property(x => x.TargetScore).HasPrecision(4, 2);
            entity.Property(x => x.FinalScore).HasPrecision(4, 2);

            entity.Property(x => x.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("active");

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // Course Schedule
        modelBuilder.Entity<CourseSchedule>(entity =>
        {
            entity.Property(x => x.RoomOrLink).HasMaxLength(255);
        });

        // Assignment
        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(150);

            entity.Property(x => x.AssignmentType)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.Property(x => x.WeightPercent).HasDefaultValue(0);
            entity.Property(x => x.MaxScore).HasPrecision(4, 2).HasDefaultValue(10m);
            entity.Property(x => x.Score).HasPrecision(4, 2);
            entity.Property(x => x.IsSubmitted).HasDefaultValue(false);
            entity.Property(x => x.Location).HasMaxLength(100);
        });
        // ==========================================
        // 27. PROJECT MANAGEMENT CONFIGURATION
        // ==========================================

        // Project
        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(150);

            entity.Property(x => x.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("planning");

            entity.Property(x => x.ProgressPercent).HasDefaultValue(0);

            entity.Property(x => x.Color)
                .HasMaxLength(15)
                .IsUnicode(false);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // Project Member
        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.Property(x => x.JoinedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // Project Invitation
        modelBuilder.Entity<ProjectInvitation>(entity =>
        {
            entity.Property(x => x.InvitedEmail)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(x => x.Token)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(x => x.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending");

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // Project Milestone
        modelBuilder.Entity<ProjectMilestone>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(150);
            entity.Property(x => x.IsCompleted).HasDefaultValue(false);
        });

        // Project Activity
        modelBuilder.Entity<ProjectActivity>(entity =>
        {
            entity.Property(x => x.Action)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(x => x.Detail).HasMaxLength(500);

            entity.Property(x => x.TargetType)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });
        // ==========================================
        // 28. TASK MANAGEMENT CONFIGURATION
        // ==========================================

        // Task
        modelBuilder.Entity<LifeHub.Models.TaskManagement.Task>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200);

            entity.Property(x => x.Priority)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("medium");

            entity.Property(x => x.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("todo");

            entity.Property(x => x.RecurrenceRule)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(x => x.SortOrder).HasDefaultValue(0);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // Task Comment
        modelBuilder.Entity<TaskComment>(entity =>
        {
            entity.Property(x => x.Content).IsRequired();

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // Tag
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(50);

            entity.Property(x => x.Color)
                .HasMaxLength(15)
                .IsUnicode(false);
        });

        // Task Tag
        modelBuilder.Entity<TaskTag>();
        // ==========================================
        // 29. TIME MANAGEMENT CONFIGURATION
        // ==========================================

        // Event
        modelBuilder.Entity<Event>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200);

            entity.Property(x => x.EventType)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.Property(x => x.Location).HasMaxLength(150);

            entity.Property(x => x.Color)
                .HasMaxLength(15)
                .IsUnicode(false);

            entity.Property(x => x.RecurrenceRule)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(x => x.SourceType)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.Property(x => x.IsAllDay).HasDefaultValue(false);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // Pomodoro Session
        modelBuilder.Entity<PomodoroSession>(entity =>
        {
            entity.Property(x => x.IsCompleted).HasDefaultValue(false);
        });
        // ==========================================
        // 30. GOAL MANAGEMENT CONFIGURATION
        // ==========================================

        // Goal
        modelBuilder.Entity<Goal>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200);

            entity.Property(x => x.Category)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.Property(x => x.GoalType)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.Property(x => x.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("active");

            entity.Property(x => x.ProgressPercent).HasDefaultValue(0);
            entity.Property(x => x.IsCompleted).HasDefaultValue(false);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // Key Result
        modelBuilder.Entity<KeyResult>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200);

            entity.Property(x => x.TargetValue).HasPrecision(10, 2);
            entity.Property(x => x.CurrentValue)
                .HasPrecision(10, 2)
                .HasDefaultValue(0m);

            entity.Property(x => x.Unit).HasMaxLength(30);
            entity.Property(x => x.IsCompleted).HasDefaultValue(false);
        });
        // ==========================================
        // 31. HABIT MANAGEMENT CONFIGURATION
        // ==========================================

        // Habit
        modelBuilder.Entity<Habit>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(150);

            entity.Property(x => x.ColorCode)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.Property(x => x.Frequency)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.Property(x => x.TargetPerPeriod).HasDefaultValue(1);
            entity.Property(x => x.CurrentStreak).HasDefaultValue(0);
            entity.Property(x => x.LongestStreak).HasDefaultValue(0);
            entity.Property(x => x.Active).HasDefaultValue(true);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // Habit Log
        modelBuilder.Entity<HabitLog>(entity =>
        {
            entity.Property(x => x.IsCompleted).HasDefaultValue(true);
            entity.Property(x => x.CountValue).HasDefaultValue(1);
            entity.Property(x => x.Note).HasMaxLength(200);
        });
        // ==========================================
        // 32. FINANCE MANAGEMENT CONFIGURATION
        // ==========================================

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100);

            entity.Property(x => x.Type)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.Property(x => x.Icon).HasMaxLength(50);

            entity.Property(x => x.Color)
                .HasMaxLength(15)
                .IsUnicode(false);

            entity.Property(x => x.IsDefault).HasDefaultValue(false);
        });

        // Wallet
        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100);

            entity.Property(x => x.WalletType)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.Property(x => x.InitialBalance)
                .HasPrecision(18, 2)
                .HasDefaultValue(0);

            entity.Property(x => x.Currency)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue("VND");

            entity.Property(x => x.Color)
                .HasMaxLength(15)
                .IsUnicode(false);

            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // Savings Goal
        modelBuilder.Entity<SavingsGoal>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(150);

            entity.Property(x => x.TargetAmount).HasPrecision(18, 2);

            entity.Property(x => x.CurrentAmount)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            entity.Property(x => x.IsAchieved).HasDefaultValue(false);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // Transaction
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(x => x.TransactionType)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.Property(x => x.Amount).HasPrecision(18, 2);

            entity.Property(x => x.Note).HasMaxLength(255);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // Budget
        modelBuilder.Entity<Budget>(entity =>
        {
            entity.Property(x => x.LimitAmount).HasPrecision(18, 2);

            entity.Property(x => x.AlertThresholdPercent)
                .HasDefaultValue(80);
        });
        // ==========================================
        // 33. KNOWLEDGE MANAGEMENT CONFIGURATION
        // ==========================================

        // Note Category
        modelBuilder.Entity<NoteCategory>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100);
        });

        // Note
        modelBuilder.Entity<Note>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200);

            entity.Property(x => x.IsPinned).HasDefaultValue(false);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // Note Tag
        modelBuilder.Entity<NoteTag>();

        // Document
        modelBuilder.Entity<Document>(entity =>
        {
            entity.Property(x => x.FileName).HasMaxLength(255);
            entity.Property(x => x.FilePath).HasMaxLength(255);

            entity.Property(x => x.FileType)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.Property(x => x.Description).HasMaxLength(255);

            entity.Property(x => x.UploadedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // Shared Item
        modelBuilder.Entity<SharedItem>(entity =>
        {
            entity.Property(x => x.ItemType)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.Property(x => x.Permission)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("view");

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });
        // ==========================================
        // 34. LIFESTYLE MANAGEMENT CONFIGURATION
        // ==========================================

        // Lifestyle Log
        modelBuilder.Entity<LifestyleLog>(entity =>
        {
            entity.Property(x => x.SleepHours).HasPrecision(3, 1);
            entity.Property(x => x.WeightKg).HasPrecision(4, 1);
            entity.Property(x => x.ActivityType).HasMaxLength(100);
            entity.Property(x => x.Notes).HasMaxLength(255);
        });

        // Productivity Score
        modelBuilder.Entity<ProductivityScore>(entity =>
        {
            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });
        // ==========================================
        // 35. ACHIEVEMENT CONFIGURATION
        // ==========================================

        // Achievement
        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.Property(x => x.BadgeCode)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(x => x.Title).HasMaxLength(150);
            entity.Property(x => x.Description).HasMaxLength(255);
            entity.Property(x => x.BadgeIcon).HasMaxLength(255);

            entity.Property(x => x.ConditionType)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.Property(x => x.PointsReward).HasDefaultValue(0);
        });

        // User Achievement
        modelBuilder.Entity<UserAchievement>(entity =>
        {
            entity.Property(x => x.EarnedAt)
                .HasDefaultValueSql("GETDATE()");
        });
        // ==========================================
        // 36. AI MANAGEMENT CONFIGURATION
        // ==========================================

        // AI Conversation
        modelBuilder.Entity<AiConversation>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(150);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // AI Message
        modelBuilder.Entity<AiMessage>(entity =>
        {
            entity.Property(x => x.Role)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.Property(x => x.Content).IsRequired();

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });
    }
}