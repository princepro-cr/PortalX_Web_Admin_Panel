using HighSchoolPortal.Interfaces;
using HighSchoolPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HighSchoolPortal.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IFirebaseAuthService _authService;
        private readonly IFirebaseSchoolService _schoolService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            IFirebaseAuthService authService,
            IFirebaseSchoolService schoolService,
            ILogger<ProfileController> logger)
        {
            _authService = authService;
            _schoolService = schoolService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _authService.GetCurrentUserAsync();
                if (user == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Get detailed profile based on role
                if (user.Role == "student")
                {
                    var student = await _schoolService.GetStudentByIdAsync(user.Id);
                    if (student != null)
                    {
                        return View("StudentProfile", student);
                    }
                }
                else if (user.Role == "teacher")
                {
                    var teacher = await _schoolService.GetTeacherByIdAsync(user.Id);
                    if (teacher != null)
                    {
                        return View("TeacherProfile", teacher);
                    }
                }
                else if (user.Role == "hr")
                {
                    // HR user profile
                    return View("HRProfile", user);
                }

                // If no specific profile found, show generic profile
                // Check if UserProfile view exists, otherwise use Index
                return View("UserProfile", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile");
                TempData["ErrorMessage"] = "Error loading profile information.";

                // Redirect to appropriate dashboard based on role
                var user = await _authService.GetCurrentUserAsync();
                if (user != null)
                {
                    return user.Role switch
                    {
                        "teacher" => RedirectToAction("Dashboard", "Teacher"),
                         "hr" => RedirectToAction("Dashboard", "HR"),
                        _ => RedirectToAction("Index", "Home")
                    };
                }
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            try
            {
                var user = await _authService.GetCurrentUserAsync();
                if (user == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                if (user.Role == "teacher")
                {
                    var teacher = await _schoolService.GetTeacherByIdAsync(user.Id);
                    return View("EditTeacherProfile", teacher ?? user);
                }
                

                return View("EditProfile", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit profile");
                TempData["ErrorMessage"] = "Error loading profile editor.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(UserProfile model)
        {
            if (!ModelState.IsValid)
            {
                return View("EditProfile", model);
            }

            try
            {
                var user = await _authService.GetCurrentUserAsync();
                if (user == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Update basic user info
                user.FullName = model.FullName;
                user.Phone = model.Phone;
                user.Address = model.Address;
                user.AvatarUrl = model.AvatarUrl;

                // Update based on role
                if (user.Role == "teacher")
                {
                    var teacher = await _schoolService.GetTeacherByIdAsync(user.Id);
                    if (teacher != null)
                    {
                        teacher.FullName = model.FullName;
                        teacher.Phone = model.Phone;
                        teacher.Address = model.Address;
                        teacher.AvatarUrl = model.AvatarUrl;
                        await _schoolService.UpdateTeacherAsync(teacher);
                    }
                }
                else if (user.Role == "student")
                {
                    var student = await _schoolService.GetStudentByIdAsync(user.Id);
                    if (student != null)
                    {
                        student.FullName = model.FullName;
                        student.Phone = model.Phone;
                        student.Address = model.Address;
                        student.AvatarUrl = model.AvatarUrl;
                        await _schoolService.UpdateStudentAsync(student);
                    }
                }

                // Update auth service cache
                await _authService.UpdateUserProfileAsync(user);

                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                ModelState.AddModelError(string.Empty, $"Error updating profile: {ex.Message}");
                return View("EditProfile", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTeacher(TeacherProfile model)
        {
            if (!ModelState.IsValid)
            {
                return View("EditTeacherProfile", model);
            }

            try
            {
                var user = await _authService.GetCurrentUserAsync();
                if (user == null || user.Role != "teacher")
                {
                    return RedirectToAction("Login", "Auth");
                }

                var teacher = await _schoolService.GetTeacherByIdAsync(user.Id);
                if (teacher != null)
                {
                    teacher.FullName = model.FullName;
                    teacher.Phone = model.Phone;
                    teacher.Address = model.Address;
                    teacher.AvatarUrl = model.AvatarUrl;
                    teacher.Department = model.Department;
                    teacher.Qualification = model.Qualification;
                    teacher.Specialization = model.Specialization;

                    await _schoolService.UpdateTeacherAsync(teacher);

                    // Update auth service cache
                    user.FullName = model.FullName;
                    user.AvatarUrl = model.AvatarUrl;
                    await _authService.UpdateUserProfileAsync(user);
                }

                TempData["SuccessMessage"] = "Teacher profile updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating teacher profile");
                ModelState.AddModelError(string.Empty, $"Error updating profile: {ex.Message}");
                return View("EditTeacherProfile", model);
            }
        }
    }
}