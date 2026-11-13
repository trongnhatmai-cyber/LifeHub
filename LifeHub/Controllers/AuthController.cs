using AspNetCoreGeneratedDocument;
using LifeHub.BusinessLogicLayer.Services;
using LifeHub.Models;
using LifeHub.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LifeHub.Controllers;

[Authorize]
public class AuthController : Controller
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    // GET: /Auth/Register
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // POST: /Auth/Register
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        bool registered = await _authService.RegisterAsync(
            model.Email,
            model.Password,
            model.FullName
        );

        if (!registered)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "Email này đã được sử dụng."
            );

            return View(model);
        }

        TempData["SuccessMessage"] = "Đăng ký thành công. Vui lòng đăng nhập.";

        return RedirectToAction(nameof(Login));
    }

    // GET: /Auth/Login
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    // POST: /Auth/Login
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        User? user = await _authService.ValidateUserAsync(
            model.Email,
            model.Password
        );

        if (user == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Email hoặc mật khẩu không chính xác."
            );

            return View(model);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        var principal = new ClaimsPrincipal(identity);

        var properties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties
        );

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Success", "Home");
    }

    // POST: /Auth/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        return RedirectToAction(nameof(Login));
    }

    // GET: /Auth/AccessDenied
    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}

