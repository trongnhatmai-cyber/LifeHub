using System;


namespace LifeHub.Models.ProjectManagement;

public class ProjectInvitation
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public int InvitedBy { get; set; }

    public int? InvitedUserId { get; set; }

    public string InvitedEmail { get; set; } = string.Empty;

    public int ProjectRoleId { get; set; }

    public string Token { get; set; } = string.Empty;

    public string Status { get; set; } = "pending";

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? RespondedAt { get; set; }
}
