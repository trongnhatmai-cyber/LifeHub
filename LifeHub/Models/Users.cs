using System;

namespace LifeHub.Models;

public class User
{
    public int Id { get; set; }

    public UserRole Role { get; set; } = UserRole.User;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }

    public bool EmailVerified { get; set; } = false;

    public bool Active { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }
}