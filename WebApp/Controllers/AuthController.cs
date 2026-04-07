using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Models.Dtos;


namespace WebApp.Controllers;

public class AuthController: Controller
{
    private readonly UserManager<WebAppUser> _userManager;
    private readonly SignInManager<WebAppUser> _signInManager;
    public AuthController(
        UserManager<WebAppUser> userManager,
        SignInManager<WebAppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }
    [HttpGet]
    public IActionResult Login() => View();
    [HttpPost]
    public async Task<IActionResult> Login(LoginDto model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, isPersistent: true, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            return RedirectToAction("Index", "Home");
        }
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }
    [HttpGet]
    public IActionResult Register() => View();
    [HttpPost]
    public async Task<IActionResult> Register(RegisterDto model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = new WebAppUser
        {
            UserName = model.Username,
            Email = model.Email
        };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, isPersistent: true);
            return RedirectToAction("Index", "Home");
        }
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }
    public IActionResult Logout()
    {
        _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
    public IActionResult Denied()
    {
        return View();
    }
}