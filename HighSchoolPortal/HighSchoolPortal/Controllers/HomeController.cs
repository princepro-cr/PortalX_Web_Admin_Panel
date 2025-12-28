using HighSchoolPortal.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HighSchoolPortal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // If already authenticated, redirect to appropriate dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.IsNullOrEmpty(userRole))
                {
                    // Use the same switch logic from AuthController
                    return RedirectToRoleDashboard(userRole);
                }
            }

            // Not authenticated - go to role selection
            return RedirectToAction("ChooseRole");
        }

        [HttpGet]
        public IActionResult ChooseRole()
        {
            // If already authenticated, go directly to dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.IsNullOrEmpty(role))
                {
                    return RedirectToRoleDashboard(role);
                }
                else
                {
                    // No role found - log out
                    return RedirectToAction("Logout", "Auth");
                }
            }

            return View();
        }

        [HttpPost]
        public IActionResult ChooseRole(string role)
        {
            // Redirect to AuthController's Login with role parameter
            return RedirectToAction("Login", "Auth", new { role = role });
        }

        // Helper method to redirect to role-specific dashboard
        private IActionResult RedirectToRoleDashboard(string role)
        {
            return role switch
            {
                "hr" => RedirectToAction("Dashboard", "HR"),
                "teacher" => RedirectToAction("Dashboard", "Teacher"),
                "student" => RedirectToAction("Dashboard", "Student"),
                _ => RedirectToAction("ChooseRole") // Default to role selection
            };
        }

        [HttpGet]
        public IActionResult About()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Terms()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Help()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Error(string code = null)
        {
            ViewBag.ErrorCode = code;
            return View();
        }

        [HttpGet]
        public IActionResult Maintenance()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ComingSoon()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Features()
        {
            return View();
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Status(int? statusCode = null)
        {
            if (statusCode.HasValue)
            {
                ViewBag.StatusCode = statusCode.Value;
                switch (statusCode.Value)
                {
                    case 404:
                        ViewBag.Message = "Page not found";
                        break;
                    case 500:
                        ViewBag.Message = "Internal server error";
                        break;
                    case 403:
                        ViewBag.Message = "Access forbidden";
                        break;
                    default:
                        ViewBag.Message = "An error occurred";
                        break;
                }
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DirectFirebaseTest()
        {
            var apiKey = "AIzaSyCM6F_uaLlwF6aYrlNJ75QRlwlGdH0jDzk";
            var email = "test" + DateTime.Now.Ticks + "@test.com";
            var password = "Test123!";

            using var httpClient = new HttpClient();

            var requestData = new
            {
                email = email,
                password = password,
                returnSecureToken = true
            };

            var response = await httpClient.PostAsJsonAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}",
                requestData
            );

            var content = await response.Content.ReadAsStringAsync();

            return Content($"✅ Direct Firebase Test\nStatus: {response.StatusCode}\nEmail: {email}\nPassword: {password}\n\nResponse:\n{content}");
        }

        [HttpGet]
        public IActionResult CheckAuth()
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine($"IsAuthenticated: {User.Identity?.IsAuthenticated}");
            result.AppendLine($"Name: {User.Identity?.Name}");
            result.AppendLine($"AuthenticationType: {User.Identity?.AuthenticationType}");

            if (User.Identity?.IsAuthenticated == true)
            {
                result.AppendLine("\nClaims:");
                foreach (var claim in User.Claims)
                {
                    result.AppendLine($"  {claim.Type}: {claim.Value}");
                }

                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                result.AppendLine($"\nRole: {role}");

                if (!string.IsNullOrEmpty(role))
                {
                    result.AppendLine($"\nDashboard URL: /{role}/Dashboard");
                }
            }

            return Content(result.ToString(), "text/plain");
        }

        [HttpGet]
        public IActionResult Reset()
        {
            // Clear any session/cookie issues
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).Wait();

            // Redirect to home
            return RedirectToAction("Index");
        }
    }
}