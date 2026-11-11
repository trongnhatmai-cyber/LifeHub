using System;

namespace LifeHub.Models.LifestyleManagement;

public class UserAchievement
{
    public int UserId { get; set; }
    public int AchievementId { get; set; }
    public DateTime EarnedAt { get; set; }
}