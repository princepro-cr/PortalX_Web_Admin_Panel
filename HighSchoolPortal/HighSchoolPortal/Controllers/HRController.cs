using HighSchoolPortal.Interfaces;
using HighSchoolPortal.Models;
using HighSchoolPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HighSchoolPortal.Controllers
{
    [Authorize(Roles = "hr")]
    public class HRController : Controller
    {
        private readonly IFirebaseSchoolService _schoolService;
        private readonly IFirebaseAuthService _authService;
        private readonly ILogger<HRController> _logger;

        public HRController(
            IFirebaseSchoolService schoolService,
            IFirebaseAuthService authService,
            ILogger<HRController> logger)
        {
            _schoolService = schoolService;
            _authService = authService;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var user = await _authService.GetCurrentUserAsync();
                if (user == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Get statistics
                var studentCount = await _schoolService.GetStudentCountAsync();
                var teacherCount = await _schoolService.GetTeacherCountAsync();
                var classCount = await _schoolService.GetClassCountAsync();
                var hrCount = await _schoolService.GetHRCountAsync();

                // Get recent students
                var students = await _schoolService.GetAllStudentsAsync();
                var recentStudents = students.Take(5);

                // Get statistics report
                var statistics = await _schoolService.GenerateStatisticsReportAsync();

                ViewBag.User = user;
                ViewBag.StudentCount = studentCount;
                ViewBag.TeacherCount = teacherCount;
                ViewBag.ClassCount = classCount;
                ViewBag.HRCount = hrCount;
                ViewBag.RecentStudents = recentStudents;
                ViewBag.Statistics = statistics;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading HR dashboard");
                ViewBag.Error = "Error loading dashboard data";
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> ManageStudents()
        {
            try
            {
                var students = await _schoolService.GetAllStudentsAsync();
                ViewBag.TotalStudents = students.Count();

                return View(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading students");
                return View(new List<StudentProfile>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ManageTeachers()
        {
            try
            {
                var teachers = await _schoolService.GetAllTeachersAsync();
                ViewBag.TotalTeachers = teachers.Count();

                return View(teachers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading teachers");
                return View(new List<TeacherProfile>());
            }
        }

        [HttpGet]
        public IActionResult AddStudent()
        {
            return View(new StudentProfile());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudent(StudentProfile student)
        {
            if (!ModelState.IsValid)
            {
                return View(student);
            }

            try
            {
                student.Role = "student";
                student.CreatedAt = DateTime.UtcNow;
                student.UpdatedAt = DateTime.UtcNow;

                var result = await _schoolService.AddStudentAsync(student);

                if (result != null)
                {
                    TempData["SuccessMessage"] = $"Student '{student.FullName}' added successfully!";
                    return RedirectToAction("ManageStudents");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to add student.";
                    return View(student);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding student");
                ModelState.AddModelError(string.Empty, $"Error: {ex.Message}");
                return View(student);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewStudent(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var student = await _schoolService.GetStudentByIdAsync(id);
                if (student == null)
                {
                    return NotFound();
                }

                // Get student grades
                var grades = await _schoolService.GetStudentGradesAsync(id);
                ViewBag.Grades = grades;

                // Calculate GPA if grades exist
                if (grades.Any())
                {
                    student.GPA = Math.Round(grades.Average(g => g.TotalScore) / 100 * 4, 2);
                }

                return View(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing student");
                TempData["ErrorMessage"] = "Error loading student details.";
                return RedirectToAction("ManageStudents");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditStudent(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var student = await _schoolService.GetStudentByIdAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(StudentProfile student)
        {
            if (!ModelState.IsValid)
            {
                return View(student);
            }

            try
            {
                student.UpdatedAt = DateTime.UtcNow;
                await _schoolService.UpdateStudentAsync(student);

                TempData["SuccessMessage"] = $"Student '{student.FullName}' updated successfully!";
                return RedirectToAction("ManageStudents");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student");
                ModelState.AddModelError(string.Empty, $"Error: {ex.Message}");
                return View(student);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudent(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Invalid student ID.";
                return RedirectToAction("ManageStudents");
            }

            try
            {
                var student = await _schoolService.GetStudentByIdAsync(id);
                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found.";
                    return RedirectToAction("ManageStudents");
                }

                await _schoolService.DeleteUserAsync(id);

                TempData["SuccessMessage"] = $"Student '{student.FullName}' deleted successfully.";
                return RedirectToAction("ManageStudents");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student");
                TempData["ErrorMessage"] = $"Error deleting student: {ex.Message}";
                return RedirectToAction("ManageStudents");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GenerateReport(string reportType = "statistics")
        {
            try
            {
                if (reportType == "grades")
                {
                    var classes = await _schoolService.GetAllClassesAsync();
                    ViewBag.Classes = classes;

                    return View("GradeReport");
                }
                else if (reportType == "attendance")
                {
                    return View("AttendanceReport");
                }
                else
                {
                    var statistics = await _schoolService.GenerateStatisticsReportAsync();
                    ViewBag.Statistics = statistics;

                    return View("StatisticsReport", statistics);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                TempData["ErrorMessage"] = "Error generating report.";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateGradeReport(string classId, string term, int year)
        {
            try
            {
                var report = await _schoolService.GenerateGradeReportAsync(classId, term, year);
                ViewBag.ClassId = classId;
                ViewBag.Term = term;
                ViewBag.Year = year;

                return View("GradeReportResult", report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating grade report");
                TempData["ErrorMessage"] = "Error generating grade report.";
                return RedirectToAction("GenerateReport", new { reportType = "grades" });
            }
        }
    }
}