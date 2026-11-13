
using System.Security.Claims;

namespace LifeHub.Extensions;

public static class ClaimsPrincipalExtensions
{
    // Lấy ID người dùng đang đăng nhập
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (!int.TryParse(userId, out int id) || id <= 0)
        {
            throw new InvalidOperationException(
                "Không tìm thấy thông tin UserId hợp lệ."
            );
        }

        return id;
    }
}
