
using LifeHub.DataAccessLayer;
using LifeHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.BusinessLogicLayer.Services;

public class AuthService
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthService(ApplicationDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
    }

    // Đăng ký tài khoản
    public async Task<bool> RegisterAsync(
        string email,
        string password,
        string fullName)
    {
        email = email.Trim().ToLowerInvariant();

        // Kiểm tra email đã tồn tại
        bool emailExists = await _context.Users
            .AnyAsync(u => u.Email == email);

        if (emailExists)
            return false;

        var user = new User
        {
            Email = email,
            FullName = fullName.Trim(),
            Role = UserRole.User,
            Active = true,
            EmailVerified = false
        };

        // Băm mật khẩu trước khi lưu
        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            password
        );

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        return true;
    }

    // Kiểm tra thông tin đăng nhập
    public async Task<User?> ValidateUserAsync(
        string email,
        string password)
    {
        email = email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !user.Active)
            return null;

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password
        );

        if (result == PasswordVerificationResult.Failed)
            return null;

        // Cập nhật thời gian đăng nhập
        user.LastLoginAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return user;
    }
}
