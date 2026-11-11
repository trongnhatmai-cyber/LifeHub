using System;

namespace LifeHub.Models.LifestyleManagement;

public class Achievement
{
    public int Id { get; set; }
    public string BadgeCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BadgeIcon { get; set; }
    public string? ConditionType { get; set; }
    public int? ConditionValue { get; set; }
    public int PointsReward { get; set; }
}
