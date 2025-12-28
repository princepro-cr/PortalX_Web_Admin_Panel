using HighSchoolPortal.Interfaces;
using HighSchoolPortal.Models;
using HighSchoolPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HighSchoolPortal.Controllers
{
    [Authorize(Roles = "teacher")]
    public class TeacherController : Controller
    {
        private readonly IFirebaseSchoolService _schoolService;
        private readonly IFirebaseAuthService _authService;
        private readonly ILogger<TeacherController> _logger;

        public TeacherController(
            IFirebaseSchoolService schoolService,
            IFirebaseAuthService authService,
            ILogger<TeacherController> logger)
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

                var teacher = await _schoolService.GetTeacherByIdAsync(user.Id);
                if (teacher == null)
                {
                    // Create a default teacher profile if not found
                    teacher = new TeacherProfile
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Email = user.Email,
                        Role = user.Role,
                        Classes = new List<string>(), // Initialize empty list
                        Subjects = new List<string>()  // Initialize empty list
                    };
                }
                else
                {
                    // Ensure lists are initialized
                    teacher.Classes ??= new List<string>();
                    teacher.Subjects ??= new List<string>();
                }

                // Get all students
                var students = await _schoolService.GetAllStudentsAsync();

                // Filter students by teacher's classes
                var teacherStudents = new List<StudentProfile>();
                if (teacher.Classes != null && teacher.Classes.Any() && students != null)
                {
                    teacherStudents = students.Where(s => s != null &&
                                                          teacher.Classes.Contains(s.ClassId)).ToList();
                }

                // Get all classes
                var classes = await _schoolService.GetAllClassesAsync();
                var teacherClasses = classes?.Where(c => teacher.Classes.Contains(c.Id)).ToList() ?? new List<Class>();

                ViewBag.User = teacher;
                ViewBag.StudentCount = teacherStudents.Count;
                ViewBag.ClassCount = teacher.Classes.Count;
                ViewBag.SubjectCount = teacher.Subjects.Count;
                ViewBag.TeacherClasses = teacherClasses;
                ViewBag.TeacherStudents = teacherStudents;

                return View(teacher);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading teacher dashboard");

                // Return default values on error
                ViewBag.StudentCount = 0;
                ViewBag.ClassCount = 0;
                ViewBag.SubjectCount = 0;
                ViewBag.TeacherClasses = new List<Class>();
                ViewBag.TeacherStudents = new List<StudentProfile>();

                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyStudents()
        {
            try
            {
                var user = await _authService.GetCurrentUserAsync();
                var teacher = await _schoolService.GetTeacherByIdAsync(user.Id);

                var allStudents = await _schoolService.GetAllStudentsAsync();
                var myStudents = allStudents.Where(s => teacher.Classes.Contains(s.ClassId));

                return View(myStudents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading teacher's students");
                return View(new List<StudentProfile>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddGrade(string studentId)
        {
            if (string.IsNullOrEmpty(studentId))
            {
                return NotFound();
            }

            var student = await _schoolService.GetStudentByIdAsync(studentId);
            if (student == null)
            {
                return NotFound();
            }

            var grade = new Grade
            {
                StudentId = studentId,
                StudentName = student.FullName
            };

            ViewBag.Student = student;
            return View(grade);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddGrade(Grade grade)
        {
            if (!ModelState.IsValid)
            {
                return View(grade);
            }

            try
            {
                var user = await _authService.GetCurrentUserAsync();
                grade.TeacherId = user.Id;
                grade.CalculateTotal();

                var result = await _schoolService.AddGradeAsync(grade);

                if (result != null)
                {
                    TempData["SuccessMessage"] = $"Grade added for {grade.StudentName} successfully!";
                    return RedirectToAction("ViewStudent", new { id = grade.StudentId });
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to add grade.";
                    return View(grade);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding grade");
                ModelState.AddModelError(string.Empty, $"Error: {ex.Message}");
                return View(grade);
            }
        }

        [HttpGet]
        public async Task<IActionResult> RecordAttendance(string classId)
        {
            try
            {
                if (string.IsNullOrEmpty(classId))
                {
                    // Get teacher's classes
                    var user = await _authService.GetCurrentUserAsync();
                    var teacher = await _schoolService.GetTeacherByIdAsync(user.Id);

                    if (teacher?.Classes?.Any() == true)
                    {
                        classId = teacher.Classes.First();
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "No classes assigned to you.";
                        return View(new List<Attendance>());
                    }
                }

                var students = await _schoolService.GetAllStudentsAsync();
                var classStudents = students.Where(s => s.ClassId == classId).ToList();

                var attendanceList = new List<Attendance>();
                foreach (var student in classStudents)
                {
                    attendanceList.Add(new Attendance
                    {
                        StudentId = student.Id,
                        StudentName = student.FullName,
                        ClassId = classId,
                        ClassName = $"Grade {student.GradeLevel} - {student.ClassId}",
                        Date = DateTime.Today,
                        Status = "Present", // Default to Present
                        RecordedBy = string.Empty // Will be filled on POST
                    });
                }

                ViewBag.ClassId = classId;
                ViewBag.ClassStudents = classStudents;

                // Get available classes for teacher
                var user2 = await _authService.GetCurrentUserAsync();
                var teacher2 = await _schoolService.GetTeacherByIdAsync(user2.Id);
                ViewBag.AvailableClasses = teacher2?.Classes ?? new List<string>();

                return View(attendanceList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording attendance");
                return View(new List<Attendance>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordAttendance(List<Attendance> attendanceList, string classId)
        {
            if (attendanceList == null || !attendanceList.Any())
            {
                TempData["ErrorMessage"] = "No attendance data provided.";
                return RedirectToAction("RecordAttendance", new { classId });
            }

            try
            {
                var user = await _authService.GetCurrentUserAsync();
                var teacher = await _schoolService.GetTeacherByIdAsync(user.Id);

                int savedCount = 0;
                foreach (var attendance in attendanceList)
                {
                    // Validate required fields
                    if (string.IsNullOrEmpty(attendance.StudentId) ||
                        string.IsNullOrEmpty(attendance.Status))
                    {
                        continue; // Skip invalid records
                    }

                    attendance.RecordedBy = $"{teacher?.FullName ?? user.FullName} ({user.Id})";
                    attendance.RecordedAt = DateTime.UtcNow;

                    // Set ClassName if empty
                    if (string.IsNullOrEmpty(attendance.ClassName))
                    {
                        attendance.ClassName = $"Grade {classId}";
                    }

                    var result = await _schoolService.RecordAttendanceAsync(attendance);
                    if (result != null)
                    {
                        savedCount++;
                    }
                }

                if (savedCount > 0)
                {
                    TempData["SuccessMessage"] = $"Attendance recorded successfully for {savedCount} students!";
                    return RedirectToAction("Dashboard");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to record attendance.";
                    return RedirectToAction("RecordAttendance", new { classId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording attendance");
                TempData["ErrorMessage"] = $"Error recording attendance: {ex.Message}";
                return RedirectToAction("RecordAttendance", new { classId });
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

                var grades = await _schoolService.GetStudentGradesAsync(id);
                var attendance = await _schoolService.GetStudentAttendanceAsync(id);

                ViewBag.Grades = grades;
                ViewBag.Attendance = attendance;

                return View(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing student");
                TempData["ErrorMessage"] = "Error loading student details.";
                return RedirectToAction("MyStudents");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentTeacher()
        {
            try
            {
                var user = await _authService.GetCurrentUserAsync();
                if (user == null)
                {
                    return Json(new { error = "User not found" });
                }

                var teacher = await _schoolService.GetTeacherByIdAsync(user.Id);
                if (teacher == null)
                {
                    return Json(new
                    {
                        id = user.Id,
                        fullName = user.FullName,
                        email = user.Email,
                        avatarUrl = user.AvatarUrl
                    });
                }

                return Json(new
                {
                    id = teacher.Id,
                    fullName = teacher.FullName,
                    email = teacher.Email,
                    avatarUrl = teacher.AvatarUrl,
                    department = teacher.Department,
                    subjects = teacher.Subjects,
                    classes = teacher.Classes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current teacher");
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        private async Task<IActionResult> GetDashboardData()
        {
            try
            {
                var user = await _authService.GetCurrentUserAsync();
                if (user == null)
                {
                    return Json(new { error = "User not found" });
                }

                var teacher = await _schoolService.GetTeacherByIdAsync(user.Id);
                var students = await _schoolService.GetAllStudentsAsync();

                var teacherStudents = students?.Where(s => teacher?.Classes?.Contains(s.ClassId) == true).ToList()
                                      ?? new List<StudentProfile>();

                return Json(new
                {
                    studentCount = teacherStudents.Count,
                    classCount = teacher?.Classes?.Count ?? 0,
                    subjectCount = teacher?.Subjects?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data");
                return Json(new { error = ex.Message });
            }
        }
    }
}