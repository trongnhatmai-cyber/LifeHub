
using System;
using System.Collections.Generic;

namespace LifeHub.Models.ViewModels.SystemManagement
{
    public class NotificationPageViewModel
    {
        public List<NotificationItem> Items { get; set; } = new();

        public int TotalCount => Items.Count;

        public int UnreadCount => Items.Count(x => !x.IsRead);
    }

    public class NotificationItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string? LinkUrl { get; set; }

        public string NotificationType { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public bool IsRead { get; set; }

        public int ProjectId { get; set; }

        public string ProjectName { get; set; } = string.Empty;

        public int? InvitationId { get; set; }

        public string? InvitationStatus { get; set; }

        public string Category
        {
            get
            {
                return NotificationType.ToLowerInvariant() switch
                {
                    "project" or "project_invitation" => "project",
                    "task" => "task",
                    "study" => "study",
                    "reminder" or "time" => "time",
                    "system" => "system",
                    _ => "other"
                };
            }
        }

        public string Icon => Category switch
        {
            "project" => "users",
            "task" => "briefcase",
            "study" => "book",
            "time" => "clock",
            "system" => "info",
            _ => "bell"
        };

        public string Tone => Category switch
        {
            "project" => "purple",
            "task" => "blue",
            "study" => "green",
            "time" => "orange",
            "system" => "gray",
            _ => "gray"
        };

        public string? SafeLink
        {
            get
            {
                if (string.IsNullOrWhiteSpace(LinkUrl))
                    return null;

                if (Uri.TryCreate(LinkUrl, UriKind.Relative, out _))
                    return LinkUrl.StartsWith("/") ? LinkUrl : null;

                if (Uri.TryCreate(LinkUrl, UriKind.Absolute, out var uri)
                    && uri.Scheme == Uri.UriSchemeHttps)
                    return LinkUrl;

                return null;
            }
        }

        public string TimeAgo(DateTime now)
        {
            var elapsed = now - CreatedAt;

            if (elapsed.TotalMinutes < 1)
                return "Vừa xong";

            if (elapsed.TotalHours < 1)
                return $"{(int)elapsed.TotalMinutes} phút trước";

            if (elapsed.TotalDays < 1)
                return $"{(int)elapsed.TotalHours} giờ trước";

            if (elapsed.TotalDays < 2)
                return "Hôm qua";

            if (elapsed.TotalDays < 7)
                return $"{(int)elapsed.TotalDays} ngày trước";

            return CreatedAt.ToString("dd/MM/yyyy");
        }
    }
}
