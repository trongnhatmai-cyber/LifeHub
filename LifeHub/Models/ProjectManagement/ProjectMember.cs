using System;


namespace LifeHub.Models.ProjectManagement;

public class ProjectMember
{
    public int ProjectId { get; set; }

    public int UserId { get; set; }

    public int ProjectRoleId { get; set; }

    public DateTime JoinedAt { get; set; }
}
