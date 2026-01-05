using HighSchoolPortal.Interfaces;
using HighSchoolPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Claims;

namespace HighSchoolPortal.Controllers
{
    [Authorize(Roles = "teacher")]
    public class TeacherController : Controller
    {
        private readonly IFirebaseSchoolService _schoolService;
        private readonly IFirebaseAuthService _authService;
        private readonly ILogger<TeacherController> _logger;
        private readonly string _teacherId;

        public TeacherController(
            IFirebaseSchoolService schoolService,
            IFirebaseAuthService authService,
            ILogger<TeacherController> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _schoolService = schoolService;
            _authService = authService;
            _logger = logger;

            // Get teacher ID from claims
            var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            _teacherId = userId ?? string.Empty;
        }


        // In TeacherController.cs, add these methods:

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            try
            {
                // Get current teacher's profile
                var teacher = await _schoolService.GetTeacherByIdAsync(_teacherId);

                if (teacher == null)
                {
                    TempData["ErrorMessage"] = "Teacher profile not found.";
                    return RedirectToAction("Dashboard");
                }

                // Get teacher's statistics
                var allStudents = await _schoolService.GetAllStudentsAsync();

                // Filter to teacher's classes if available
                var teacherStudents = teacher.Classes != null && teacher.Classes.Any()
                    ? allStudents.Where(s => teacher.Classes.Contains(s.ClassId)).ToList()
                    : allStudents.ToList();

                var stats = new TeacherProfileStats
                {
                    TotalStudents = teacherStudents.Count,
                    AverageGPA = teacherStudents.Any() ?
                        Math.Round(teacherStudents.Average(s => s.GPA), 2) : 0,
                    AverageAttendance = teacherStudents.Any() ?
                        (int)Math.Round(teacherStudents.Average(s => s.AttendancePercentage)) : 100,
                    TotalClasses = teacher.Classes?.Count ?? 0,
                    TotalSubjects = teacher.Subjects?.Count ?? 0,
                    YearsOfService = (DateTime.Now.Year - teacher.HireDate.Year)
                };

                // Get recent activity (last 5 grades added)
                var recentActivity = new List<Grade>();
                foreach (var student in teacherStudents.Take(10))
                {
                    try
                    {
                        var studentGrades = await _schoolService.GetStudentGradesAsync(student.Id);
                        if (studentGrades != null)
                        {
                            var teacherGrades = studentGrades
                                .Where(g => g.TeacherId == _teacherId)
                                .OrderByDescending(g => g.DateRecorded)
                                .Take(2);
                            recentActivity.AddRange(teacherGrades);
                        }
                    }
                    catch { /* Ignore errors */ }
                }

                ViewBag.Stats = stats;
                ViewBag.RecentActivity = recentActivity.OrderByDescending(g => g.DateRecorded).Take(5).ToList();
                ViewBag.TeacherStudents = teacherStudents.Take(6).ToList(); // Show top 6 students

                return View(teacher);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading teacher profile");
                TempData["ErrorMessage"] = "Error loading profile data.";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(TeacherProfile model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Please fix validation errors.";
                    return RedirectToAction("Profile");
                }

                // Get current teacher
                var teacher = await _schoolService.GetTeacherByIdAsync(_teacherId);
                if (teacher == null)
                {
                    TempData["ErrorMessage"] = "Teacher not found.";
                    return RedirectToAction("Profile");
                }

                // Update basic info
                teacher.FullName = model.FullName;
                teacher.Email = model.Email;
                teacher.Phone = model.Phone;
                teacher.Address = model.Address;
                teacher.DateOfBirth = model.DateOfBirth;
                teacher.AvatarUrl = model.AvatarUrl;

                // Update teacher-specific info
                teacher.Department = model.Department;
                teacher.Qualification = model.Qualification;
                teacher.Specialization = model.Specialization;
                teacher.UpdatedAt = DateTime.UtcNow;

                await _schoolService.UpdateTeacherAsync(teacher);
                TempData["SuccessMessage"] = "Profile updated successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating teacher profile");
                TempData["ErrorMessage"] = $"Error updating profile: {ex.Message}";
            }

            return RedirectToAction("Profile");
        }

        // Add this class to TeacherController (inside the namespace but outside the TeacherController class)
        public class TeacherProfileStats
        {
            public int TotalStudents { get; set; }
            public decimal AverageGPA { get; set; }
            public int AverageAttendance { get; set; }
            public int TotalClasses { get; set; }
            public int TotalSubjects { get; set; }
            public int YearsOfService { get; set; }
        }
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Get teacher profile
                var teacher = await _schoolService.GetTeacherByIdAsync(_teacherId);

                // Get ALL students from Firebase (no filtering)
                var allStudents = await _schoolService.GetAllStudentsAsync();

                // Calculate statistics - HR style
                var stats = new TeacherDashboardStats
                {
                    // HR style counts
                    TotalStudents = allStudents.Count(),
                    ActiveStudents = allStudents.Count(s => s.IsActive),

                    // Teacher specific - use teacher data if available, otherwise use all students data
                    TotalClasses = teacher?.Classes?.Count ?? 0,
                    TotalSubjects = teacher?.Subjects?.Count ?? 0,
                    AverageGPA = allStudents.Any() ?
                        Math.Round(allStudents.Average(s => s.GPA), 2) : 0,
                    AverageAttendance = allStudents.Any() ?
                        (int)Math.Round(allStudents.Average(s => s.AttendancePercentage)) : 100
                };

                // Get today's attendance (get from all classes)
                var todaysAttendance = new List<Attendance>();
                try
                {
                    // Try to get attendance for today from all sources
                    var allClasses = await _schoolService.GetAllClassesAsync();
                    foreach (var classItem in allClasses)
                    {
                        var classAttendance = await _schoolService.GetClassAttendanceAsync(classItem.Id, DateTime.Today);
                        if (classAttendance != null)
                            todaysAttendance.AddRange(classAttendance);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not load attendance data");
                }

                // Get recent grades
                var recentGrades = new List<Grade>();
                foreach (var student in allStudents.Take(10))
                {
                    try
                    {
                        var studentGrades = await _schoolService.GetStudentGradesAsync(student.Id);
                        if (studentGrades != null)
                            recentGrades.AddRange(studentGrades.OrderByDescending(g => g.DateRecorded).Take(2));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Could not load grades for student {student.Id}");
                    }
                }

                ViewBag.Teacher = teacher;
                ViewBag.Stats = stats;
                ViewBag.TeacherStudents = allStudents.Take(8).ToList(); // Show first 8 students
                ViewBag.RecentGrades = recentGrades.OrderByDescending(g => g.DateRecorded).Take(10).ToList();
                ViewBag.TodaysAttendance = todaysAttendance;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading teacher dashboard");
                TempData["ErrorMessage"] = "Error loading dashboard data.";
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyStudents(string search = "", string grade = "", string classFilter = "", string sortBy = "name", int page = 1, int pageSize = 20)
        {
            try
            {
                // Get teacher profile
                var teacher = await _schoolService.GetTeacherByIdAsync(_teacherId);

                // Get ALL students from Firebase (NO filtering by teacher classes)
                var allStudents = await _schoolService.GetAllStudentsAsync();

                // Apply search filter
                var students = allStudents.ToList(); // Start with all students

                if (!string.IsNullOrEmpty(search))
                {
                    students = students.Where(s =>
                        s.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        s.StudentId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        s.Email.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Apply grade filter
                if (!string.IsNullOrEmpty(grade))
                {
                    students = students.Where(s => s.GradeLevel == grade).ToList();
                }

                // Apply class filter
                if (!string.IsNullOrEmpty(classFilter))
                {
                    students = students.Where(s => s.ClassId == classFilter).ToList();
                }

                // Apply sorting
                students = sortBy.ToLower() switch
                {
                    "name" => students.OrderBy(s => s.FullName).ToList(),
                    "gpa" => students.OrderByDescending(s => s.GPA).ToList(),
                    "grade" => students.OrderBy(s => s.GradeLevel).ToList(),
                    "attendance" => students.OrderByDescending(s => s.AttendancePercentage).ToList(),
                    "class" => students.OrderBy(s => s.ClassId).ToList(),
                    _ => students.OrderBy(s => s.FullName).ToList()
                };

                // Pagination
                var totalStudents = students.Count;
                var totalPages = (int)Math.Ceiling((double)totalStudents / pageSize);
                var pagedStudents = students.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                ViewBag.SearchTerm = search;
                ViewBag.GradeFilter = grade;
                ViewBag.ClassFilter = classFilter;
                ViewBag.SortBy = sortBy;
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalStudents = totalStudents;
                ViewBag.TotalPages = totalPages;

                // Get all unique classes for filtering dropdown
                var allClasses = await _schoolService.GetAllClassesAsync();
                ViewBag.TeacherClasses = allClasses.Select(c => c.Name).Distinct().ToList() ?? new List<string>();

                ViewBag.ActiveCount = students.Count(s => s.IsActive);

                // Calculate average GPA and attendance like HR does
                ViewBag.AverageGPA = students.Any() ?
                    Math.Round(students.Average(s => s.GPA), 2) : 0;
                ViewBag.AverageAttendance = students.Any() ?
                    (int)Math.Round(students.Average(s => s.AttendancePercentage)) : 100;

                return View(pagedStudents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading teacher's students");
                TempData["ErrorMessage"] = "Error loading students.";
                return View(new List<StudentProfile>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> StudentDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                // Get student from Firebase
                var student = await _schoolService.GetStudentByIdAsync(id);
                if (student == null)
                {
                    return NotFound();
                }

                // NO access restrictions - teacher can view any student

                // Get student grades from Firebase
                var grades = await _schoolService.GetStudentGradesAsync(id);
                grades = grades ?? new List<Grade>();

                // Get student attendance from Firebase
                var attendance = await _schoolService.GetStudentAttendanceAsync(id);
                attendance = attendance ?? new List<Attendance>();

                // Calculate subject summaries
                var subjectGrades = grades
                    .GroupBy(g => g.Subject)
                    .Select(g => new SubjectGradeSummary
                    {
                        Subject = g.Key,
                        AverageScore = Math.Round(g.Average(x => x.TotalScore), 2),
                        TotalGrades = g.Count(),
                        LatestGrade = g.OrderByDescending(x => x.DateRecorded).FirstOrDefault()?.GradeLetter ?? "N/A"
                    })
                    .OrderByDescending(s => s.AverageScore)
                    .ToList();

                // Calculate student stats - HR style
                var stats = new StudentStats
                {
                    TotalGrades = grades.Count(),
                    AverageScore = grades.Any() ? Math.Round(grades.Average(g => g.TotalScore), 2) : 0,
                    AttendancePercentage = student.AttendancePercentage,
                    DaysEnrolled = (DateTime.Now - student.EnrollmentDate).Days,
                    BestSubject = subjectGrades.OrderByDescending(s => s.AverageScore).FirstOrDefault(),
                    NeedsImprovement = subjectGrades.OrderBy(s => s.AverageScore).FirstOrDefault()
                };

                ViewBag.Student = student;
                ViewBag.Grades = grades.OrderByDescending(g => g.DateRecorded).ToList();
                ViewBag.AttendanceRecords = attendance.OrderByDescending(a => a.Date).Take(30).ToList();
                ViewBag.SubjectGrades = subjectGrades;
                ViewBag.Stats = stats;

                return View(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error viewing student details for ID: {id}");
                TempData["ErrorMessage"] = "Error loading student details.";
                return RedirectToAction("MyStudents");
            }
        }

        // Add HR-style methods for teacher dashboard
        [HttpGet]
        public async Task<IActionResult> GetStudentCount()
        {
            try
            {
                // Get ALL students count
                var allStudents = await _schoolService.GetAllStudentsAsync();
                return Json(new { count = allStudents.Count() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student count");
                return Json(new { count = 0 });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStatistics()
        {
            try
            {
                var teacher = await _schoolService.GetTeacherByIdAsync(_teacherId);

                // Get ALL students from Firebase
                var allStudents = await _schoolService.GetAllStudentsAsync();

                // Calculate statistics - HR style
                var stats = new
                {
                    totalStudents = allStudents.Count(),
                    activeStudents = allStudents.Count(s => s.IsActive),
                    averageGPA = allStudents.Any() ?
                        Math.Round(allStudents.Average(s => s.GPA), 2) : 0,
                    averageAttendance = allStudents.Any() ?
                        (int)Math.Round(allStudents.Average(s => s.AttendancePercentage)) : 100,
                    totalClasses = teacher?.Classes?.Count ?? 0,
                    totalSubjects = teacher?.Subjects?.Count ?? 0
                };

                return Json(new { success = true, stats = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard statistics");
                return Json(new { success = false, message = "Error loading statistics" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ClassPerformance(string classId = "", string subject = "")
        {
            try
            {
                var teacher = await _schoolService.GetTeacherByIdAsync(_teacherId);

                // Get all classes
                var allClasses = await _schoolService.GetAllClassesAsync();

                // If no class specified, use first class
                if (string.IsNullOrEmpty(classId) && allClasses.Any())
                {
                    classId = allClasses.First().Id;
                }

                // Get class details from Firebase
                var classDetails = await _schoolService.GetClassByIdAsync(classId);

                // Get all students from Firebase
                var allStudents = await _schoolService.GetAllStudentsAsync();
                var classStudents = allStudents.Where(s => s.ClassId == classId).ToList();

                // Get class grades
                var classGrades = new List<Grade>();
                foreach (var student in classStudents)
                {
                    var studentGrades = await _schoolService.GetStudentGradesAsync(student.Id);
                    if (studentGrades != null)
                        classGrades.AddRange(studentGrades);
                }

                // Calculate subject averages
                var subjectAverages = classGrades
                    .GroupBy(g => g.Subject)
                    .Select(g => new
                    {
                        Subject = g.Key,
                        AverageScore = Math.Round(g.Average(x => x.TotalScore), 2),
                        StudentCount = g.Select(x => x.StudentId).Distinct().Count()
                    })
                    .OrderByDescending(s => s.AverageScore)
                    .ToList();

                // Get attendance for this class
                var classAttendance = await _schoolService.GetClassAttendanceAsync(classId, DateTime.Today);
                classAttendance = classAttendance ?? new List<Attendance>();

                ViewBag.ClassId = classId;
                ViewBag.ClassName = classDetails?.Name ?? classId;
                ViewBag.ClassStudents = classStudents;
                ViewBag.SubjectAverages = subjectAverages;
                ViewBag.ClassAttendance = classAttendance;
                ViewBag.TeacherClasses = allClasses.Select(c => c.Name).ToList();
                ViewBag.Subject = subject;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading class performance");
                TempData["ErrorMessage"] = "Error loading class performance data.";
                return View();
            }
        }

        // Add Grade method
        [HttpGet]
        public IActionResult AddGrade(string studentId = "", string subject = "")
        {
            var grade = new Grade
            {
                StudentId = studentId,
                Subject = subject,
                Term = "First",
                Year = DateTime.Now.Year,
                DateRecorded = DateTime.Now
            };

            ViewBag.Terms = new List<string> { "First", "Second", "Third", "Final" };
            ViewBag.Subjects = GetSubjectList();
            ViewBag.CurrentYear = DateTime.Now.Year;

            return View(grade);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddGrade(Grade grade)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Terms = new List<string> { "First", "Second", "Third", "Final" };
                    ViewBag.Subjects = GetSubjectList();
                    ViewBag.CurrentYear = DateTime.Now.Year;
                    return View(grade);
                }

                // Verify student exists
                var student = await _schoolService.GetStudentByIdAsync(grade.StudentId);
                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found.";
                    return RedirectToAction("MyStudents");
                }

                // Calculate total score
                grade.TotalScore = (grade.Test1 * 0.25m) + (grade.Test2 * 0.25m) + (grade.Exam * 0.4m) + (grade.Assignment * 0.1m);

                // Assign grade letter
                grade.GradeLetter = CalculateGradeLetter(grade.TotalScore);

                // Set recorded by
                grade.TeacherId = _teacherId;

                // Set student name
                grade.StudentName = student.FullName;

                var result = await _schoolService.AddGradeAsync(grade);

                if (result != null)
                {
                    TempData["SuccessMessage"] = $"Grade added successfully for {student.FullName}!";
                    return RedirectToAction("StudentDetails", new { id = student.Id });
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to add grade. Please try again.";
                    ViewBag.Terms = new List<string> { "First", "Second", "Third", "Final" };
                    ViewBag.Subjects = GetSubjectList();
                    ViewBag.CurrentYear = DateTime.Now.Year;
                    return View(grade);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding grade");
                TempData["ErrorMessage"] = $"Error adding grade: {ex.Message}";
                ViewBag.Terms = new List<string> { "First", "Second", "Third", "Final" };
                ViewBag.Subjects = GetSubjectList();
                ViewBag.CurrentYear = DateTime.Now.Year;
                return View(grade);
            }
        }

        // Record Attendance method
        [HttpGet]
        public async Task<IActionResult> RecordAttendance(string classId = "", DateTime? date = null)
        {
            try
            {
                var teacher = await _schoolService.GetTeacherByIdAsync(_teacherId);

                // Get all classes
                var allClasses = await _schoolService.GetAllClassesAsync();

                // Use first class if none specified
                if (string.IsNullOrEmpty(classId) && allClasses.Any())
                {
                    classId = allClasses.First().Id;
                }

                // Use today's date if none specified
                var attendanceDate = date ?? DateTime.Today;

                // Get class details from Firebase
                var classDetails = await _schoolService.GetClassByIdAsync(classId);

                // Get all students from Firebase
                var allStudents = await _schoolService.GetAllStudentsAsync();
                var classStudents = allStudents.Where(s => s.ClassId == classId).ToList();

                // Get existing attendance for this date
                var existingAttendance = await _schoolService.GetClassAttendanceAsync(classId, attendanceDate);
                existingAttendance = existingAttendance ?? new List<Attendance>();

                ViewBag.ClassId = classId;
                ViewBag.ClassName = classDetails?.Name ?? classId;
                ViewBag.ClassStudents = classStudents;
                ViewBag.ExistingAttendance = existingAttendance;
                ViewBag.AttendanceDate = attendanceDate;
                ViewBag.TeacherClasses = allClasses.Select(c => c.Name).ToList();

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading attendance page");
                TempData["ErrorMessage"] = "Error loading attendance data.";
                return View();
            }
        }

        // Helper Methods
        private string CalculateGradeLetter(decimal totalScore)
        {
            return totalScore switch
            {
                >= 90 => "A",
                >= 80 => "B",
                >= 70 => "C",
                >= 60 => "D",
                _ => "F"
            };
        }

        private List<string> GetSubjectList()
        {
            return new List<string>
            {
                "Mathematics",
                "English",
                "Science",
                "History",
                "Geography",
                "Physics",
                "Chemistry",
                "Biology",
                "Computer Science",
                "Art",
                "Music",
                "Physical Education"
            };
        }
    }

    // View Models
    public class TeacherDashboardStats
    {
        // HR style counts
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }

        // Teacher specific
        public int TotalClasses { get; set; }
        public int TotalSubjects { get; set; }
        public decimal AverageGPA { get; set; }
        public int AverageAttendance { get; set; }
    }

    public class SubjectGradeSummary
    {
        public string Subject { get; set; } = string.Empty;
        public decimal AverageScore { get; set; }
        public int TotalGrades { get; set; }
        public string LatestGrade { get; set; } = string.Empty;
    }

    public class StudentStats
    {
        public int TotalGrades { get; set; }
        public decimal AverageScore { get; set; }
        public int AttendancePercentage { get; set; }
        public int DaysEnrolled { get; set; }
        public SubjectGradeSummary BestSubject { get; set; }
        public SubjectGradeSummary NeedsImprovement { get; set; }
    }
}