IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [achievements] (
    [id] int NOT NULL IDENTITY,
    [badge_code] varchar(50) NOT NULL,
    [title] nvarchar(150) NOT NULL,
    [description] nvarchar(255) NULL,
    [badge_icon] nvarchar(255) NULL,
    [condition_type] varchar(30) NULL,
    [condition_value] int NULL,
    [points_reward] int NOT NULL DEFAULT 0,
    CONSTRAINT [PK_achievements] PRIMARY KEY ([id])
);

CREATE TABLE [project_roles] (
    [id] int NOT NULL IDENTITY,
    [name] nvarchar(max) NOT NULL,
    [description] nvarchar(max) NULL,
    [is_default] bit NOT NULL,
    [created_at] datetime2 NOT NULL,
    CONSTRAINT [PK_project_roles] PRIMARY KEY ([id])
);

CREATE TABLE [users] (
    [id] int NOT NULL IDENTITY,
    [role_id] int NOT NULL,
    [email] varchar(100) NOT NULL,
    [password_hash] varchar(255) NOT NULL,
    [full_name] nvarchar(100) NOT NULL,
    [phone_number] varchar(15) NULL,
    [avatar_url] nvarchar(255) NULL,
    [email_verified] bit NOT NULL DEFAULT CAST(0 AS bit),
    [active] bit NOT NULL DEFAULT CAST(1 AS bit),
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    [updated_at] datetime2 NULL,
    [last_login_at] datetime2 NULL,
    CONSTRAINT [PK_users] PRIMARY KEY ([id])
);

CREATE TABLE [ai_conversations] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [title] nvarchar(150) NULL,
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_ai_conversations] PRIMARY KEY ([id]),
    CONSTRAINT [FK_ai_conversations_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [categories] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NULL,
    [parent_id] int NULL,
    [name] nvarchar(100) NOT NULL,
    [type] varchar(10) NOT NULL,
    [icon] nvarchar(50) NULL,
    [color] varchar(15) NULL,
    [is_default] bit NOT NULL DEFAULT CAST(0 AS bit),
    CONSTRAINT [PK_categories] PRIMARY KEY ([id]),
    CONSTRAINT [FK_categories_categories_parent_id] FOREIGN KEY ([parent_id]) REFERENCES [categories] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_categories_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [events] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [title] nvarchar(200) NOT NULL,
    [description] nvarchar(max) NULL,
    [event_type] varchar(30) NOT NULL,
    [location] nvarchar(150) NULL,
    [start_time] datetime2 NOT NULL,
    [end_time] date NOT NULL,
    [is_all_day] bit NOT NULL DEFAULT CAST(0 AS bit),
    [color] varchar(15) NULL,
    [recurrence_rule] varchar(100) NULL,
    [source_type] varchar(30) NULL,
    [source_id] int NULL,
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_events] PRIMARY KEY ([id]),
    CONSTRAINT [FK_events_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [goals] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [title] nvarchar(200) NOT NULL,
    [description] nvarchar(max) NULL,
    [category] varchar(30) NULL,
    [goal_type] varchar(20) NOT NULL,
    [start_date] date NULL,
    [target_date] date NULL,
    [status] varchar(20) NOT NULL DEFAULT 'active',
    [progress_percent] int NOT NULL DEFAULT 0,
    [is_completed] bit NOT NULL DEFAULT CAST(0 AS bit),
    [review_note] nvarchar(max) NULL,
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_goals] PRIMARY KEY ([id]),
    CONSTRAINT [FK_goals_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [lifestyle_logs] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [log_date] date NOT NULL,
    [sleep_hours] decimal(3,1) NULL,
    [bed_time] time NULL,
    [wake_time] time NULL,
    [sleep_quality] int NULL,
    [water_ml] int NULL,
    [physical_activity_minutes] int NULL,
    [activity_type] nvarchar(100) NULL,
    [weight_kg] decimal(4,1) NULL,
    [mood_rate] int NULL,
    [notes] nvarchar(255) NULL,
    CONSTRAINT [PK_lifestyle_logs] PRIMARY KEY ([id]),
    CONSTRAINT [FK_lifestyle_logs_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [note_categories] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [parent_id] int NULL,
    [name] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_note_categories] PRIMARY KEY ([id]),
    CONSTRAINT [FK_note_categories_note_categories_parent_id] FOREIGN KEY ([parent_id]) REFERENCES [note_categories] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_note_categories_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [notifications] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [title] nvarchar(150) NOT NULL,
    [message] nvarchar(max) NOT NULL,
    [link_url] nvarchar(255) NULL,
    [notification_type] varchar(30) NOT NULL,
    [ref_type] varchar(30) NULL,
    [ref_id] int NULL,
    [is_read] bit NOT NULL DEFAULT CAST(0 AS bit),
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_notifications] PRIMARY KEY ([id]),
    CONSTRAINT [FK_notifications_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [productivity_scores] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [score_date] date NOT NULL,
    [score_value] int NOT NULL,
    [tasks_completed] int NULL,
    [habits_completed] int NULL,
    [pomodoros_done] int NULL,
    [overdue_penalties] int NULL,
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_productivity_scores] PRIMARY KEY ([id]),
    CONSTRAINT [FK_productivity_scores_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [projects] (
    [id] int NOT NULL IDENTITY,
    [created_by] int NOT NULL,
    [name] nvarchar(150) NOT NULL,
    [description] nvarchar(max) NULL,
    [start_date] date NOT NULL,
    [due_date] date NULL,
    [status] varchar(20) NOT NULL DEFAULT 'planning',
    [progress_percent] int NOT NULL DEFAULT 0,
    [color] varchar(15) NULL,
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    [updated_at] datetime2 NULL,
    CONSTRAINT [PK_projects] PRIMARY KEY ([id]),
    CONSTRAINT [FK_projects_users_created_by] FOREIGN KEY ([created_by]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [reminders] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [ref_type] varchar(30) NOT NULL,
    [ref_id] int NOT NULL,
    [remind_at] datetime2 NOT NULL,
    [offset_minutes] int NULL,
    [is_sent] bit NOT NULL DEFAULT CAST(0 AS bit),
    [sent_at] datetime2 NULL,
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_reminders] PRIMARY KEY ([id]),
    CONSTRAINT [FK_reminders_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [semesters] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [name] nvarchar(100) NOT NULL,
    [start_date] date NULL,
    [end_date] date NULL,
    [is_current] bit NOT NULL DEFAULT CAST(0 AS bit),
    CONSTRAINT [PK_semesters] PRIMARY KEY ([id]),
    CONSTRAINT [FK_semesters_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [shared_items] (
    [id] int NOT NULL IDENTITY,
    [owner_id] int NOT NULL,
    [shared_with_user_id] int NOT NULL,
    [item_type] varchar(20) NOT NULL,
    [item_id] int NOT NULL,
    [permission] varchar(10) NOT NULL DEFAULT 'view',
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_shared_items] PRIMARY KEY ([id]),
    CONSTRAINT [FK_shared_items_users_owner_id] FOREIGN KEY ([owner_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_shared_items_users_shared_with_user_id] FOREIGN KEY ([shared_with_user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [system_logs] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NULL,
    [action] varchar(100) NOT NULL,
    [details] nvarchar(max) NULL,
    [ip_address] varchar(45) NULL,
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_system_logs] PRIMARY KEY ([id]),
    CONSTRAINT [FK_system_logs_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [tags] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [name] nvarchar(50) NOT NULL,
    [color] varchar(15) NULL,
    CONSTRAINT [PK_tags] PRIMARY KEY ([id]),
    CONSTRAINT [FK_tags_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [user_achievements] (
    [user_id] int NOT NULL,
    [achievement_id] int NOT NULL,
    [earned_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_user_achievements] PRIMARY KEY ([user_id], [achievement_id]),
    CONSTRAINT [FK_user_achievements_achievements_achievement_id] FOREIGN KEY ([achievement_id]) REFERENCES [achievements] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_user_achievements_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [user_settings] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [theme_mode] nvarchar(max) NOT NULL,
    [language] nvarchar(max) NOT NULL,
    [timezone] nvarchar(max) NULL,
    [dashboard_layout] nvarchar(max) NULL,
    [visible_modules] nvarchar(max) NULL,
    [pomodoro_work_min] int NOT NULL,
    [pomodoro_break_min] int NOT NULL,
    [pomodoro_long_break_min] int NOT NULL,
    [notify_email] bit NOT NULL,
    [notify_push] bit NOT NULL,
    CONSTRAINT [PK_user_settings] PRIMARY KEY ([id]),
    CONSTRAINT [FK_user_settings_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [user_tokens] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [token_type] varchar(20) NOT NULL,
    [token_hash] varchar(255) NOT NULL,
    [expires_at] datetime2 NOT NULL,
    [used_at] datetime2 NULL,
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_user_tokens] PRIMARY KEY ([id]),
    CONSTRAINT [FK_user_tokens_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [wallets] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [name] nvarchar(100) NOT NULL,
    [wallet_type] varchar(20) NOT NULL,
    [initial_balance] decimal(18,2) NOT NULL DEFAULT 0.0,
    [currency] varchar(3) NOT NULL DEFAULT 'VND',
    [color] varchar(15) NULL,
    [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_wallets] PRIMARY KEY ([id]),
    CONSTRAINT [FK_wallets_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [ai_messages] (
    [id] int NOT NULL IDENTITY,
    [conversation_id] int NOT NULL,
    [role] varchar(10) NOT NULL,
    [content] nvarchar(max) NOT NULL,
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_ai_messages] PRIMARY KEY ([id]),
    CONSTRAINT [FK_ai_messages_ai_conversations_conversation_id] FOREIGN KEY ([conversation_id]) REFERENCES [ai_conversations] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [budgets] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [category_id] int NOT NULL,
    [limit_amount] decimal(18,2) NOT NULL,
    [month] int NOT NULL,
    [year] int NOT NULL,
    [alert_threshold_percent] int NULL DEFAULT 80,
    CONSTRAINT [PK_budgets] PRIMARY KEY ([id]),
    CONSTRAINT [FK_budgets_categories_category_id] FOREIGN KEY ([category_id]) REFERENCES [categories] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_budgets_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [habits] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [goal_id] int NULL,
    [title] nvarchar(150) NOT NULL,
    [color_code] varchar(10) NULL,
    [frequency] varchar(20) NOT NULL,
    [target_per_period] int NOT NULL DEFAULT 1,
    [reminder_time] time NULL,
    [current_streak] int NOT NULL DEFAULT 0,
    [longest_streak] int NOT NULL DEFAULT 0,
    [active] bit NOT NULL DEFAULT CAST(1 AS bit),
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_habits] PRIMARY KEY ([id]),
    CONSTRAINT [FK_habits_goals_goal_id] FOREIGN KEY ([goal_id]) REFERENCES [goals] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_habits_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [key_results] (
    [id] int NOT NULL IDENTITY,
    [goal_id] int NOT NULL,
    [title] nvarchar(200) NOT NULL,
    [target_value] decimal(10,2) NOT NULL,
    [current_value] decimal(10,2) NULL DEFAULT 0.0,
    [unit] nvarchar(30) NULL,
    [is_completed] bit NOT NULL DEFAULT CAST(0 AS bit),
    CONSTRAINT [PK_key_results] PRIMARY KEY ([id]),
    CONSTRAINT [FK_key_results_goals_goal_id] FOREIGN KEY ([goal_id]) REFERENCES [goals] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [savings_goals] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [goal_id] int NULL,
    [title] nvarchar(150) NOT NULL,
    [target_amount] decimal(18,2) NOT NULL,
    [current_amount] decimal(18,2) NULL DEFAULT 0.0,
    [deadline] date NULL,
    [is_achieved] bit NOT NULL DEFAULT CAST(0 AS bit),
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_savings_goals] PRIMARY KEY ([id]),
    CONSTRAINT [FK_savings_goals_goals_goal_id] FOREIGN KEY ([goal_id]) REFERENCES [goals] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_savings_goals_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [project_activities] (
    [id] int NOT NULL IDENTITY,
    [project_id] int NOT NULL,
    [user_id] int NOT NULL,
    [action] varchar(50) NOT NULL,
    [detail] nvarchar(500) NULL,
    [target_type] varchar(30) NULL,
    [target_id] int NULL,
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_project_activities] PRIMARY KEY ([id]),
    CONSTRAINT [FK_project_activities_projects_project_id] FOREIGN KEY ([project_id]) REFERENCES [projects] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_project_activities_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [project_invitations] (
    [id] int NOT NULL IDENTITY,
    [project_id] int NOT NULL,
    [invited_by] int NOT NULL,
    [invited_user_id] int NULL,
    [invited_email] varchar(100) NOT NULL,
    [project_role_id] int NOT NULL,
    [token] varchar(100) NOT NULL,
    [status] varchar(20) NOT NULL DEFAULT 'pending',
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    [expires_at] datetime2 NULL,
    [responded_at] datetime2 NULL,
    CONSTRAINT [PK_project_invitations] PRIMARY KEY ([id]),
    CONSTRAINT [FK_project_invitations_project_roles_project_role_id] FOREIGN KEY ([project_role_id]) REFERENCES [project_roles] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_project_invitations_projects_project_id] FOREIGN KEY ([project_id]) REFERENCES [projects] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_project_invitations_users_invited_by] FOREIGN KEY ([invited_by]) REFERENCES [users] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_project_invitations_users_invited_user_id] FOREIGN KEY ([invited_user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [project_members] (
    [project_id] int NOT NULL,
    [user_id] int NOT NULL,
    [project_role_id] int NOT NULL,
    [joined_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_project_members] PRIMARY KEY ([project_id], [user_id]),
    CONSTRAINT [FK_project_members_project_roles_project_role_id] FOREIGN KEY ([project_role_id]) REFERENCES [project_roles] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_project_members_projects_project_id] FOREIGN KEY ([project_id]) REFERENCES [projects] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_project_members_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [project_milestones] (
    [id] int NOT NULL IDENTITY,
    [project_id] int NOT NULL,
    [title] nvarchar(150) NOT NULL,
    [due_date] date NULL,
    [is_completed] bit NOT NULL DEFAULT CAST(0 AS bit),
    CONSTRAINT [PK_project_milestones] PRIMARY KEY ([id]),
    CONSTRAINT [FK_project_milestones_projects_project_id] FOREIGN KEY ([project_id]) REFERENCES [projects] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [courses] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [semester_id] int NULL,
    [course_code] varchar(30) NOT NULL,
    [course_name] nvarchar(150) NOT NULL,
    [credits] int NULL,
    [instructor_name] nvarchar(100) NULL,
    [room_location] nvarchar(100) NULL,
    [color] varchar(15) NULL,
    [target_score] decimal(4,2) NULL,
    [final_score] decimal(4,2) NULL,
    [status] varchar(20) NOT NULL DEFAULT 'active',
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_courses] PRIMARY KEY ([id]),
    CONSTRAINT [FK_courses_semesters_semester_id] FOREIGN KEY ([semester_id]) REFERENCES [semesters] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_courses_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [habit_logs] (
    [id] int NOT NULL IDENTITY,
    [habit_id] int NOT NULL,
    [check_date] date NOT NULL,
    [is_completed] bit NOT NULL DEFAULT CAST(1 AS bit),
    [count_value] int NOT NULL DEFAULT 1,
    [note] nvarchar(200) NULL,
    CONSTRAINT [PK_habit_logs] PRIMARY KEY ([id]),
    CONSTRAINT [FK_habit_logs_habits_habit_id] FOREIGN KEY ([habit_id]) REFERENCES [habits] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [transactions] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [wallet_id] int NOT NULL,
    [to_wallet_id] int NULL,
    [category_id] int NULL,
    [savings_goal_id] int NULL,
    [transaction_type] varchar(10) NOT NULL,
    [amount] decimal(18,2) NOT NULL,
    [transaction_date] date NOT NULL,
    [note] nvarchar(255) NULL,
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_transactions] PRIMARY KEY ([id]),
    CONSTRAINT [FK_transactions_categories_category_id] FOREIGN KEY ([category_id]) REFERENCES [categories] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_transactions_savings_goals_savings_goal_id] FOREIGN KEY ([savings_goal_id]) REFERENCES [savings_goals] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_transactions_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_transactions_wallets_to_wallet_id] FOREIGN KEY ([to_wallet_id]) REFERENCES [wallets] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_transactions_wallets_wallet_id] FOREIGN KEY ([wallet_id]) REFERENCES [wallets] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [assignments] (
    [id] int NOT NULL IDENTITY,
    [course_id] int NOT NULL,
    [title] nvarchar(150) NOT NULL,
    [assignment_type] varchar(30) NOT NULL,
    [due_date] datetime2 NOT NULL,
    [duration_minutes] int NULL,
    [location] nvarchar(100) NULL,
    [weight_percent] int NOT NULL DEFAULT 0,
    [score] decimal(4,2) NULL,
    [max_score] decimal(4,2) NULL DEFAULT 10.0,
    [is_submitted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [notes] nvarchar(max) NULL,
    CONSTRAINT [PK_assignments] PRIMARY KEY ([id]),
    CONSTRAINT [FK_assignments_courses_course_id] FOREIGN KEY ([course_id]) REFERENCES [courses] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [course_schedules] (
    [id] int NOT NULL IDENTITY,
    [course_id] int NOT NULL,
    [day_of_week] int NOT NULL,
    [start_time] time NOT NULL,
    [end_time] time NOT NULL,
    [room_or_link] nvarchar(255) NULL,
    [start_date] date NULL,
    [end_date] date NULL,
    CONSTRAINT [PK_course_schedules] PRIMARY KEY ([id]),
    CONSTRAINT [FK_course_schedules_courses_course_id] FOREIGN KEY ([course_id]) REFERENCES [courses] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [notes] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [category_id] int NULL,
    [course_id] int NULL,
    [project_id] int NULL,
    [title] nvarchar(200) NOT NULL,
    [content] nvarchar(max) NULL,
    [is_pinned] bit NOT NULL DEFAULT CAST(0 AS bit),
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    [updated_at] datetime2 NULL,
    CONSTRAINT [PK_notes] PRIMARY KEY ([id]),
    CONSTRAINT [FK_notes_courses_course_id] FOREIGN KEY ([course_id]) REFERENCES [courses] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_notes_note_categories_category_id] FOREIGN KEY ([category_id]) REFERENCES [note_categories] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_notes_projects_project_id] FOREIGN KEY ([project_id]) REFERENCES [projects] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_notes_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [tasks] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [assigned_to] int NULL,
    [project_id] int NULL,
    [parent_task_id] int NULL,
    [goal_id] int NULL,
    [course_id] int NULL,
    [title] nvarchar(200) NOT NULL,
    [description] nvarchar(max) NULL,
    [priority] varchar(20) NOT NULL DEFAULT 'medium',
    [status] varchar(20) NOT NULL DEFAULT 'todo',
    [deadline] datetime2 NULL,
    [estimated_minutes] int NULL,
    [recurrence_rule] varchar(100) NULL,
    [sort_order] int NOT NULL DEFAULT 0,
    [completed_at] datetime2 NULL,
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    [updated_at] datetime2 NULL,
    CONSTRAINT [PK_tasks] PRIMARY KEY ([id]),
    CONSTRAINT [FK_tasks_courses_course_id] FOREIGN KEY ([course_id]) REFERENCES [courses] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_tasks_goals_goal_id] FOREIGN KEY ([goal_id]) REFERENCES [goals] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_tasks_projects_project_id] FOREIGN KEY ([project_id]) REFERENCES [projects] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_tasks_tasks_parent_task_id] FOREIGN KEY ([parent_task_id]) REFERENCES [tasks] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_tasks_users_assigned_to] FOREIGN KEY ([assigned_to]) REFERENCES [users] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_tasks_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [note_tags] (
    [note_id] int NOT NULL,
    [tag_id] int NOT NULL,
    CONSTRAINT [PK_note_tags] PRIMARY KEY ([note_id], [tag_id]),
    CONSTRAINT [FK_note_tags_notes_note_id] FOREIGN KEY ([note_id]) REFERENCES [notes] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_note_tags_tags_tag_id] FOREIGN KEY ([tag_id]) REFERENCES [tags] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [documents] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [project_id] int NULL,
    [course_id] int NULL,
    [task_id] int NULL,
    [file_name] nvarchar(255) NOT NULL,
    [file_path] nvarchar(255) NOT NULL,
    [file_type] varchar(20) NULL,
    [file_size_kb] int NULL,
    [description] nvarchar(255) NULL,
    [uploaded_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_documents] PRIMARY KEY ([id]),
    CONSTRAINT [FK_documents_courses_course_id] FOREIGN KEY ([course_id]) REFERENCES [courses] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_documents_projects_project_id] FOREIGN KEY ([project_id]) REFERENCES [projects] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_documents_tasks_task_id] FOREIGN KEY ([task_id]) REFERENCES [tasks] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_documents_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [pomodoro_sessions] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [task_id] int NULL,
    [course_id] int NULL,
    [duration_minutes] int NOT NULL,
    [break_minutes] int NULL,
    [started_at] datetime2 NOT NULL,
    [completed_at] datetime2 NULL,
    [is_completed] bit NOT NULL DEFAULT CAST(0 AS bit),
    CONSTRAINT [PK_pomodoro_sessions] PRIMARY KEY ([id]),
    CONSTRAINT [FK_pomodoro_sessions_courses_course_id] FOREIGN KEY ([course_id]) REFERENCES [courses] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_pomodoro_sessions_tasks_task_id] FOREIGN KEY ([task_id]) REFERENCES [tasks] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_pomodoro_sessions_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [task_comments] (
    [id] int NOT NULL IDENTITY,
    [task_id] int NOT NULL,
    [user_id] int NOT NULL,
    [content] nvarchar(max) NOT NULL,
    [created_at] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_task_comments] PRIMARY KEY ([id]),
    CONSTRAINT [FK_task_comments_tasks_task_id] FOREIGN KEY ([task_id]) REFERENCES [tasks] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_task_comments_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [task_tags] (
    [task_id] int NOT NULL,
    [tag_id] int NOT NULL,
    CONSTRAINT [PK_task_tags] PRIMARY KEY ([task_id], [tag_id]),
    CONSTRAINT [FK_task_tags_tags_tag_id] FOREIGN KEY ([tag_id]) REFERENCES [tags] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_task_tags_tasks_task_id] FOREIGN KEY ([task_id]) REFERENCES [tasks] ([id]) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX [IX_achievements_badge_code] ON [achievements] ([badge_code]);

CREATE INDEX [IX_ai_conversations_user_id] ON [ai_conversations] ([user_id]);

CREATE INDEX [IX_ai_messages_conversation_id] ON [ai_messages] ([conversation_id]);

CREATE INDEX [IX_assignments_course_id] ON [assignments] ([course_id]);

CREATE INDEX [IX_budgets_category_id] ON [budgets] ([category_id]);

CREATE UNIQUE INDEX [IX_budgets_user_id_category_id_month_year] ON [budgets] ([user_id], [category_id], [month], [year]);

CREATE INDEX [IX_categories_parent_id] ON [categories] ([parent_id]);

CREATE INDEX [IX_categories_user_id] ON [categories] ([user_id]);

CREATE INDEX [IX_course_schedules_course_id] ON [course_schedules] ([course_id]);

CREATE INDEX [IX_courses_semester_id] ON [courses] ([semester_id]);

CREATE INDEX [IX_courses_user_id] ON [courses] ([user_id]);

CREATE INDEX [IX_documents_course_id] ON [documents] ([course_id]);

CREATE INDEX [IX_documents_project_id] ON [documents] ([project_id]);

CREATE INDEX [IX_documents_task_id] ON [documents] ([task_id]);

CREATE INDEX [IX_documents_user_id] ON [documents] ([user_id]);

CREATE INDEX [IX_events_source_type_source_id] ON [events] ([source_type], [source_id]);

CREATE INDEX [IX_events_user_id_start_time] ON [events] ([user_id], [start_time]);

CREATE INDEX [IX_goals_user_id] ON [goals] ([user_id]);

CREATE UNIQUE INDEX [IX_habit_logs_habit_id_check_date] ON [habit_logs] ([habit_id], [check_date]);

CREATE INDEX [IX_habits_goal_id] ON [habits] ([goal_id]);

CREATE INDEX [IX_habits_user_id] ON [habits] ([user_id]);

CREATE INDEX [IX_key_results_goal_id] ON [key_results] ([goal_id]);

CREATE UNIQUE INDEX [IX_lifestyle_logs_user_id_log_date] ON [lifestyle_logs] ([user_id], [log_date]);

CREATE INDEX [IX_note_categories_parent_id] ON [note_categories] ([parent_id]);

CREATE INDEX [IX_note_categories_user_id] ON [note_categories] ([user_id]);

CREATE INDEX [IX_note_tags_tag_id] ON [note_tags] ([tag_id]);

CREATE INDEX [IX_notes_category_id] ON [notes] ([category_id]);

CREATE INDEX [IX_notes_course_id] ON [notes] ([course_id]);

CREATE INDEX [IX_notes_project_id] ON [notes] ([project_id]);

CREATE INDEX [IX_notes_user_id] ON [notes] ([user_id]);

CREATE INDEX [IX_notifications_user_id_is_read] ON [notifications] ([user_id], [is_read]);

CREATE INDEX [IX_pomodoro_sessions_course_id] ON [pomodoro_sessions] ([course_id]);

CREATE INDEX [IX_pomodoro_sessions_task_id] ON [pomodoro_sessions] ([task_id]);

CREATE INDEX [IX_pomodoro_sessions_user_id] ON [pomodoro_sessions] ([user_id]);

CREATE UNIQUE INDEX [IX_productivity_scores_user_id_score_date] ON [productivity_scores] ([user_id], [score_date]);

CREATE INDEX [IX_project_activities_project_id] ON [project_activities] ([project_id]);

CREATE INDEX [IX_project_activities_user_id] ON [project_activities] ([user_id]);

CREATE INDEX [IX_project_invitations_invited_by] ON [project_invitations] ([invited_by]);

CREATE INDEX [IX_project_invitations_invited_user_id] ON [project_invitations] ([invited_user_id]);

CREATE INDEX [IX_project_invitations_project_id] ON [project_invitations] ([project_id]);

CREATE INDEX [IX_project_invitations_project_role_id] ON [project_invitations] ([project_role_id]);

CREATE UNIQUE INDEX [IX_project_invitations_token] ON [project_invitations] ([token]);

CREATE INDEX [IX_project_members_project_role_id] ON [project_members] ([project_role_id]);

CREATE INDEX [IX_project_members_user_id] ON [project_members] ([user_id]);

CREATE INDEX [IX_project_milestones_project_id] ON [project_milestones] ([project_id]);

CREATE INDEX [IX_projects_created_by] ON [projects] ([created_by]);

CREATE INDEX [IX_reminders_is_sent_remind_at] ON [reminders] ([is_sent], [remind_at]);

CREATE INDEX [IX_reminders_user_id] ON [reminders] ([user_id]);

CREATE INDEX [IX_savings_goals_goal_id] ON [savings_goals] ([goal_id]);

CREATE INDEX [IX_savings_goals_user_id] ON [savings_goals] ([user_id]);

CREATE INDEX [IX_semesters_user_id] ON [semesters] ([user_id]);

CREATE INDEX [IX_shared_items_owner_id] ON [shared_items] ([owner_id]);

CREATE INDEX [IX_shared_items_shared_with_user_id] ON [shared_items] ([shared_with_user_id]);

CREATE INDEX [IX_system_logs_user_id] ON [system_logs] ([user_id]);

CREATE UNIQUE INDEX [IX_tags_user_id_name] ON [tags] ([user_id], [name]);

CREATE INDEX [IX_task_comments_task_id] ON [task_comments] ([task_id]);

CREATE INDEX [IX_task_comments_user_id] ON [task_comments] ([user_id]);

CREATE INDEX [IX_task_tags_tag_id] ON [task_tags] ([tag_id]);

CREATE INDEX [IX_tasks_assigned_to_status] ON [tasks] ([assigned_to], [status]);

CREATE INDEX [IX_tasks_course_id] ON [tasks] ([course_id]);

CREATE INDEX [IX_tasks_deadline] ON [tasks] ([deadline]);

CREATE INDEX [IX_tasks_goal_id] ON [tasks] ([goal_id]);

CREATE INDEX [IX_tasks_parent_task_id] ON [tasks] ([parent_task_id]);

CREATE INDEX [IX_tasks_project_id_status] ON [tasks] ([project_id], [status]);

CREATE INDEX [IX_tasks_user_id] ON [tasks] ([user_id]);

CREATE INDEX [IX_transactions_category_id] ON [transactions] ([category_id]);

CREATE INDEX [IX_transactions_savings_goal_id] ON [transactions] ([savings_goal_id]);

CREATE INDEX [IX_transactions_to_wallet_id] ON [transactions] ([to_wallet_id]);

CREATE INDEX [IX_transactions_user_id_transaction_date] ON [transactions] ([user_id], [transaction_date]);

CREATE INDEX [IX_transactions_wallet_id] ON [transactions] ([wallet_id]);

CREATE INDEX [IX_user_achievements_achievement_id] ON [user_achievements] ([achievement_id]);

CREATE UNIQUE INDEX [IX_user_settings_user_id] ON [user_settings] ([user_id]);

CREATE INDEX [IX_user_tokens_user_id] ON [user_tokens] ([user_id]);

CREATE UNIQUE INDEX [IX_users_email] ON [users] ([email]);

CREATE INDEX [IX_wallets_user_id] ON [wallets] ([user_id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261111084956_InitialCreate', N'10.0.0');

COMMIT;
GO

