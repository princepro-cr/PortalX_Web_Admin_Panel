using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using HighSchoolPortal.Interfaces;
using HighSchoolPortal.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HighSchoolPortal.Controllers
{
    public class AuthController : Controller
    {
        private readonly IFirebaseAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IFirebaseAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null, string role = null)
        {
            // Check if user is already authenticated
            if (User.Identity?.IsAuthenticated == true)
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.IsNullOrEmpty(userRole))
                {
                    return RedirectToDashboard(userRole);
                }
                else
                {
                    // User is authenticated but has no role - log them out
                    return RedirectToAction("Logout");
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            ViewData["SelectedRole"] = role;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authService.LoginAsync(model.Email, model.Password);

            if (result.Success)
            {
                // 🔥 FIXED: Check if user role matches selected role
                if (!string.IsNullOrEmpty(model.Role) && result.User.Role != model.Role)
                {
                    ModelState.AddModelError(string.Empty, $"Please login with a {model.Role} account.");
                    ViewData["SelectedRole"] = model.Role;
                    return View(model);
                }

                // Create claims for authentication
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, result.User.Id),
                    new Claim(ClaimTypes.Email, result.User.Email),
                    new Claim(ClaimTypes.Name, result.User.FullName),
                    new Claim(ClaimTypes.Role, result.User.Role)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                TempData["SuccessMessage"] = $"Welcome back, {result.User.FullName}!";

                // 🔥 FIXED: Redirect to correct dashboard
                return RedirectToDashboard(result.User.Role);
            }

            ModelState.AddModelError(string.Empty, result.Message);
            ViewData["SelectedRole"] = model.Role;
            return View(model);
        }

        [HttpGet]
        public IActionResult Register(string role = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.IsNullOrEmpty(userRole))
                {
                    return RedirectToDashboard(userRole);
                }
            }

            ViewBag.SelectedRole = role;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            _logger.LogInformation($"Registration POST for: {model.Email}, Role: {model.Role}");

            // Debug: Log all model values
            _logger.LogInformation($"Model values - FullName: {model.FullName}, Email: {model.Email}, Role: {model.Role}");

            // Manual validation for role-specific fields
            if (!ValidateRoleSpecificFields(model))
            {
                ViewBag.SelectedRole = model.Role;
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed!");

                // Log ALL validation errors
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state.Errors.Count > 0)
                    {
                        foreach (var error in state.Errors)
                        {
                            _logger.LogError($"Validation error for {key}: {error.ErrorMessage}");
                        }
                    }
                }

                ViewBag.SelectedRole = model.Role;
                return View(model);
            }

            try
            {
                // Call the auth service to register the user
                var result = await _authService.RegisterAsync(model);

                if (result.Success)
                {
                    // Create claims for immediate login after registration
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, result.User.Id),
                        new Claim(ClaimTypes.Email, result.User.Email),
                        new Claim(ClaimTypes.Name, result.User.FullName),
                        new Claim(ClaimTypes.Role, result.User.Role)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    TempData["SuccessMessage"] = $"Registration successful! Welcome {result.User.FullName}!";

                    // Redirect to appropriate dashboard
                    return RedirectToDashboard(result.User.Role);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, result.Message);
                    ViewBag.SelectedRole = model.Role;
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Registration Error for {model.Email}");
                ModelState.AddModelError(string.Empty, $"An error occurred during registration: {ex.Message}");
                ViewBag.SelectedRole = model.Role;
                return View(model);
            }
        }

        private bool ValidateRoleSpecificFields(RegisterModel model)
        {
            if (model.Role == "teacher")
            {
                if (string.IsNullOrEmpty(model.TeacherId))
                {
                    ModelState.AddModelError("TeacherId", "Teacher ID is required for teacher registration.");
                    return false;
                }
                if (string.IsNullOrEmpty(model.Department))
                {
                    ModelState.AddModelError("Department", "Department is required for teacher registration.");
                    return false;
                }
                if (string.IsNullOrEmpty(model.Qualification))
                {
                    ModelState.AddModelError("Qualification", "Qualification is required for teacher registration.");
                    return false;
                }
            }
            else if (model.Role == "hr")
            {
                if (string.IsNullOrEmpty(model.EmployeeId))
                {
                    ModelState.AddModelError("EmployeeId", "Employee ID is required for HR registration.");
                    return false;
                }
            }

            return true;
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _authService.LogoutAsync();
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                TempData["SuccessMessage"] = "You have been logged out successfully.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["SuccessMessage"] = "You have been logged out.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Email is required");
                return View();
            }

            try
            {
                await _authService.SendPasswordResetEmailAsync(email);
                TempData["SuccessMessage"] = "Password reset email sent. Please check your inbox.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error: {ex.Message}");
                return View();
            }
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToDashboard(string role)
        {
            switch (role.ToLower())
            {
                case "hr":
                    return RedirectToAction("Dashboard", "HR");
                case "teacher":
                    return RedirectToAction("Dashboard", "Teacher");
                default:
                    return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult TestRedirect()
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine($"User authenticated: {User.Identity?.IsAuthenticated}");
            result.AppendLine($"User role: {User.FindFirst(ClaimTypes.Role)?.Value}");

            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                result.AppendLine($"\nRedirecting to: /{role}/Dashboard");
            }

            return Content(result.ToString(), "text/plain");
        }
    }
}