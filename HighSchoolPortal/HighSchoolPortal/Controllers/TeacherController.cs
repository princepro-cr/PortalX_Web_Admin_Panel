using HighSchoolPortal.Interfaces;
using HighSchoolPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

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

        // Dashboard - Shows only teacher's students
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Get teacher profile
                var teacher = await _schoolService.GetTeacherByIdAsync(_teacherId);

                // Get ONLY teacher's students using new method
                var teacherStudents = await _schoolService.GetStudentsByTeacherAsync(_teacherId);

                // Calculate statistics - ONLY for teacher's students
                var stats = new TeacherDashboardStats
                {
                    TotalStudents = teacherStudents.Count,
                    ActiveStudents = teacherStudents.Count(s => s.IsActive),
                    TotalClasses = teacher?.Classes?.Count ?? 0,
                    TotalSubjects = teacher?.Subjects?.Count ?? 0,
                    AverageGPA = teacherStudents.Any() ?
                        Math.Round(teacherStudents.Average(s => s.GPA), 2) : 0,
                    AverageAttendance = teacherStudents.Any() ?
                        (int)Math.Round(teacherStudents.Average(s => s.AttendancePercentage)) : 100
                };

                // Get today's attendance (from teacher's classes only)
                var todaysAttendance = new List<Attendance>();
                try
                {
                    if (teacher?.Classes != null)
                    {
                        foreach (var classId in teacher.Classes)
                        {
                            var classAttendance = await _schoolService.GetClassAttendanceAsync(classId, DateTime.Today);
                            if (classAttendance != null)
                                todaysAttendance.AddRange(classAttendance);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not load attendance data");
                }

                // Get recent grades from teacher's students
                var recentGrades = new List<Grade>();
                foreach (var student in teacherStudents.Take(10))
                {
                    try
                    {
                        var studentGrades = await _schoolService.GetStudentGradesAsync(student.Id);
                        if (studentGrades != null)
                        {
                            // Filter to grades added by this teacher
                            var teacherGrades = studentGrades
                                .Where(g => g.TeacherId == _teacherId)
                                .OrderByDescending(g => g.DateRecorded)
                                .Take(2);
                            recentGrades.AddRange(teacherGrades);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Could not load grades for student {student.Id}");
                    }
                }

                ViewBag.Teacher = teacher;
                ViewBag.Stats = stats;
                ViewBag.TeacherStudents = teacherStudents.Take(8).ToList();
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

        // My Students - Shows only teacher's students
        [HttpGet]
        public async Task<IActionResult> MyStudents(string search = "", string grade = "", string classFilter = "", string sortBy = "name", int page = 1, int pageSize = 20)
        {
            try
            {
                // Get teacher's students using new method
                var teacherStudents = await _schoolService.GetStudentsByTeacherAsync(_teacherId);

                // Convert to list for filtering
                var students = teacherStudents.ToList();

                // Apply search filter
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

                // Get teacher for class list
                var teacher = await _schoolService.GetTeacherByIdAsync(_teacherId);
                ViewBag.TeacherClasses = teacher?.Classes?.ToList() ?? new List<string>();

                // Calculate statistics
                ViewBag.ActiveCount = students.Count(s => s.IsActive);
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

        // Student Details - With access control
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

                // CHECK: Ensure student is in teacher's class
                var teacher = await _schoolService.GetTeacherByIdAsync(_teacherId);
                var teacherClasses = teacher?.Classes ?? new List<string>();

                if (!teacherClasses.Contains(student.ClassId))
                {
                    TempData["ErrorMessage"] = "You don't have access to this student's information.";
                    return RedirectToAction("MyStudents");
                }

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

                // Calculate student stats
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

        // Get Dashboard Statistics - Only teacher's students
        [HttpGet]
        public async Task<IActionResult> GetDashboardStatistics()
        {
            try
            {
                var teacher = await _schoolService.GetTeacherByIdAsync(_teacherId);
                var teacherStudents = await _schoolService.GetStudentsByTeacherAsync(_teacherId);

                // Calculate statistics - ONLY for teacher's students
                var stats = new
                {
                    totalStudents = teacherStudents.Count,
                    activeStudents = teacherStudents.Count(s => s.IsActive),
                    averageGPA = teacherStudents.Any() ?
                        Math.Round(teacherStudents.Average(s => s.GPA), 2) : 0,
                    averageAttendance = teacherStudents.Any() ?
                        (int)Math.Round(teacherStudents.Average(s => s.AttendancePercentage)) : 100,
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

        // Record Attendance - Only teacher's classes
        [HttpGet]
        public async Task<IActionResult> RecordAttendance(string classId = "", DateTime? date = null)
        {
            try
            {
                var teacher = await _schoolService.GetTeacherByIdAsync(_teacherId);
                var teacherClasses = teacher?.Classes ?? new List<string>();

                // If no class specified AND teacher has classes, use first one
                if (string.IsNullOrEmpty(classId) && teacherClasses.Any())
                {
                    classId = teacherClasses.First();
                }

                // Use today's date if none specified
                var attendanceDate = date ?? DateTime.Today;

                // Only proceed if the selected class is one of teacher's classes
                List<StudentProfile> classStudents = new List<StudentProfile>();
                List<Attendance> existingAttendance = new List<Attendance>();
                string className = classId;

                if (!string.IsNullOrEmpty(classId) && teacherClasses.Contains(classId))
                {
                    // Get students for this specific class
                    classStudents = await _schoolService.GetStudentsByClassIdAsync(classId);

                    // Get class details from Firebase
                    var classDetails = await _schoolService.GetClassByIdAsync(classId);
                    className = classDetails?.Name ?? classId;

                    // Get existing attendance for this date
                    existingAttendance = (await _schoolService.GetClassAttendanceAsync(classId, attendanceDate))?.ToList() ?? new List<Attendance>();
                }

                ViewBag.ClassId = classId;
                ViewBag.ClassName = className;
                ViewBag.ClassStudents = classStudents;
                ViewBag.ExistingAttendance = existingAttendance;
                ViewBag.AttendanceDate = attendanceDate;
                ViewBag.TeacherClasses = teacherClasses; // Only show teacher's classes

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading attendance page");
                TempData["ErrorMessage"] = "Error loading attendance data.";
                return View();
            }
        }

        // Class Performance - Only teacher's classes
        [HttpGet]
        public async Task<IActionResult> ClassPerformance(string classId = "", string subject = "")
        {
            try
            {
                var teacher = await _schoolService.GetTeacherByIdAsync(_teacherId);
                var teacherClasses = teacher?.Classes ?? new List<string>();

                // If no class specified, use first of teacher's classes
                if (string.IsNullOrEmpty(classId) && teacherClasses.Any())
                {
                    classId = teacherClasses.First();
                }

                // Only proceed if class is one of teacher's classes
                if (!teacherClasses.Contains(classId))
                {
                    TempData["ErrorMessage"] = "You don't have access to this class.";
                    return RedirectToAction("Dashboard");
                }

                // Get class details from Firebase
                var classDetails = await _schoolService.GetClassByIdAsync(classId);

                // Get students for this class
                var classStudents = await _schoolService.GetStudentsByClassIdAsync(classId);

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
                ViewBag.TeacherClasses = teacherClasses;
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
        // Add to TeacherController

        [HttpGet]
        public async Task<IActionResult> AddWeightedGrade(string studentId = "", string subject = "")
        {
            var grade = new WeightedGrade
            {
                StudentId = studentId,
                Subject = subject,
                Term = "First",
                Year = DateTime.Now.Year,
                DateRecorded = DateTime.Now,
                TeacherId = _teacherId
            };

            // Get student for verification
            if (!string.IsNullOrEmpty(studentId))
            {
                var student = await _schoolService.GetStudentByIdAsync(studentId);
                if (student != null)
                {
                    ViewBag.Student = student;
                }
            }

            ViewBag.Terms = new List<string> { "First", "Second", "Third", "Final" };
            ViewBag.Subjects = GetSubjectList();
            ViewBag.CurrentYear = DateTime.Now.Year;

            return View(grade);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWeightedGrade(WeightedGrade grade)
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

                // Verify student exists and is in teacher's class
                var student = await _schoolService.GetStudentByIdAsync(grade.StudentId);
                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found.";
                    return RedirectToAction("MyStudents");
                }

                // Check if student is in teacher's class
                var teacher = await _schoolService.GetTeacherByIdAsync(_teacherId);
                if (!teacher?.Classes?.Contains(student.ClassId) == true)
                {
                    TempData["ErrorMessage"] = "You can only add grades for students in your classes.";
                    return RedirectToAction("MyStudents");
                }

                // Calculate weighted total
                grade.CalculateWeightedTotal();

                // Set teacher ID
                grade.TeacherId = _teacherId;

                // Save grade (you'll need to implement this method in FirebaseSchoolService)
                var result = await _schoolService.AddWeightedGradeAsync(grade);

                if (result != null)
                {
                    TempData["SuccessMessage"] = $"Weighted grade added successfully for {student.FullName}!";
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
                _logger.LogError(ex, "Error adding weighted grade");
                TempData["ErrorMessage"] = $"Error adding grade: {ex.Message}";
                ViewBag.Terms = new List<string> { "First", "Second", "Third", "Final" };
                ViewBag.Subjects = GetSubjectList();
                ViewBag.CurrentYear = DateTime.Now.Year;
                return View(grade);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GenerateStudentReport(string studentId, string term = "", int year = 0)
        {
            if (string.IsNullOrEmpty(studentId))
            {
                return NotFound();
            }

            try
            {
                // Verify student is in teacher's class
                var student = await _schoolService.GetStudentByIdAsync(studentId);
                if (student == null)
                {
                    return NotFound();
                }

                var teacher = await _schoolService.GetTeacherByIdAsync(_teacherId);
                if (!teacher?.Classes?.Contains(student.ClassId) == true)
                {
                    TempData["ErrorMessage"] = "You can only generate reports for students in your classes.";
                    return RedirectToAction("MyStudents");
                }

                // Use current term/year if not specified
                term = string.IsNullOrEmpty(term) ? "First" : term;
                year = year == 0 ? DateTime.Now.Year : year;

                // Get student report
                var report = await _schoolService.GetStudentReportAsync(studentId, term, year);

                // Get teacher's pass rate
                var passRate = await _schoolService.GetTeacherPassRateAsync(_teacherId);

                ViewBag.Report = report;
                ViewBag.TeacherPassRate = passRate;
                ViewBag.Student = student;
                ViewBag.Term = term;
                ViewBag.Year = year;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating report for student: {studentId}");
                TempData["ErrorMessage"] = "Error generating report.";
                return RedirectToAction("StudentDetails", new { id = studentId });
            }
        }
        [HttpGet]
        public async Task<IActionResult> ClassPerformanceReport(string classId, string term = "", int year = 0)
        {
            try
            {
                term = string.IsNullOrEmpty(term) ? "First" : term;
                year = year == 0 ? DateTime.Now.Year : year;

                // Generate the report
                var report = await _schoolService.GetClassPerformanceReportAsync(classId, term, year);

                // Get class details for display
                var classDetails = await _schoolService.GetClassByIdAsync(classId);

                var viewModel = new ClassPerformanceReportViewModel
                {
                    Report = report,
                    GeneratedAt = DateTime.Now,
                    GeneratedBy = User.Identity?.Name ?? "System"
                };

                ViewBag.ClassDetails = classDetails;
                ViewBag.Term = term;
                ViewBag.Year = year;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating class performance report for class {classId}");
                TempData["ErrorMessage"] = "Error generating report.";
                return RedirectToAction("MyClasses");
            }
        }

        [HttpGet]
        public async Task<IActionResult> TeacherPassRate()
        {
            try
            {
                var teacher = await _schoolService.GetTeacherByIdAsync(_teacherId);
                var teacherStudents = await _schoolService.GetStudentsByTeacherAsync(_teacherId);

                // Calculate pass rate
                int totalStudents = teacherStudents.Count;
                int passingStudents = teacherStudents.Count(s => s.GPA >= 2.0m);
                decimal passRate = totalStudents > 0 ? Math.Round((decimal)passingStudents / totalStudents * 100, 2) : 0;

                // Calculate by subject if available
                var subjectPassRates = new Dictionary<string, decimal>();
                foreach (var student in teacherStudents)
                {
                    var grades = await _schoolService.GetStudentGradesAsync(student.Id);
                    if (grades != null)
                    {
                        var subjectGroups = grades.GroupBy(g => g.Subject);
                        foreach (var group in subjectGroups)
                        {
                            var subject = group.Key;
                            var subjectGrades = group.ToList();
                            var avgScore = subjectGrades.Average(g => g.TotalScore);
                            var isPassing = avgScore >= 60; // 60% is passing

                            if (!subjectPassRates.ContainsKey(subject))
                            {
                                subjectPassRates[subject] = 0;
                            }

                            if (isPassing)
                            {
                                subjectPassRates[subject]++;
                            }
                        }
                    }
                }

                // Convert counts to percentages
                foreach (var subject in subjectPassRates.Keys.ToList())
                {
                    var studentCount = teacherStudents.Count(s =>
                        s.EnrolledSubjects?.Contains(subject) == true);
                    if (studentCount > 0)
                    {
                        subjectPassRates[subject] = Math.Round(subjectPassRates[subject] / studentCount * 100, 2);
                    }
                }

                ViewBag.Teacher = teacher;
                ViewBag.TotalStudents = totalStudents;
                ViewBag.PassingStudents = passingStudents;
                ViewBag.PassRate = passRate;
                ViewBag.SubjectPassRates = subjectPassRates;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating teacher pass rate");
                TempData["ErrorMessage"] = "Error calculating pass rate.";
                return View();
            }
        }

        // Helper method for report card view
        [HttpGet]
        public async Task<IActionResult> ViewReportCard(string studentId)
        {
            if (string.IsNullOrEmpty(studentId))
            {
                return NotFound();
            }

            try
            {
                var student = await _schoolService.GetStudentByIdAsync(studentId);
                if (student == null)
                {
                    return NotFound();
                }

                // Verify access
                var teacher = await _schoolService.GetTeacherByIdAsync(_teacherId);
                if (!teacher?.Classes?.Contains(student.ClassId) == true)
                {
                    TempData["ErrorMessage"] = "You don't have access to this student's report card.";
                    return RedirectToAction("MyStudents");
                }

                // Get all grades for the student
                var grades = await _schoolService.GetStudentGradesAsync(studentId);

                // Group by term and year
                var termGrades = grades?.GroupBy(g => new { g.Term, g.Year })
                    .Select(g => new
                    {
                        Term = g.Key.Term,
                        Year = g.Key.Year,
                        Grades = g.ToList(),
                        Average = g.Average(x => x.TotalScore)
                    })
                    .OrderByDescending(g => g.Year)
                    .ThenBy(g => g.Term)
                    .ToList();

                // Calculate statistics
                var stats = new
                {
                    TotalGrades = grades?.Count() ?? 0,
                    AverageScore = grades?.Any() == true ? Math.Round(grades.Average(g => g.TotalScore), 2) : 0,
                    BestSubject = grades?.GroupBy(g => g.Subject)
                        .Select(g => new
                        {
                            Subject = g.Key,
                            Average = Math.Round(g.Average(x => x.TotalScore), 2)
                        })
                        .OrderByDescending(g => g.Average)
                        .FirstOrDefault(),
                    NeedsImprovement = grades?.GroupBy(g => g.Subject)
                        .Select(g => new
                        {
                            Subject = g.Key,
                            Average = Math.Round(g.Average(x => x.TotalScore), 2)
                        })
                        .OrderBy(g => g.Average)
                        .FirstOrDefault()
                };

                ViewBag.Student = student;
                ViewBag.TermGrades = termGrades;
                ViewBag.Stats = stats;
                ViewBag.CurrentTerm = "First"; // Default term
                ViewBag.CurrentYear = DateTime.Now.Year;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading report card for student: {studentId}");
                TempData["ErrorMessage"] = "Error loading report card.";
                return RedirectToAction("StudentDetails", new { id = studentId });
            }
        }



        // My Classes - New page showing teacher's assigned classes
        [HttpGet]
        public async Task<IActionResult> MyClasses()
        {
            try
            {
                // Get teacher profile
                var teacher = await _schoolService.GetTeacherByIdAsync(_teacherId);
                var teacherClasses = teacher?.Classes ?? new List<string>();

                // Get all students
                var allStudents = await _schoolService.GetAllStudentsAsync();

                // Create class statistics
                var classStats = new List<ClassStats>();

                foreach (var classId in teacherClasses)
                {
                    var classStudents = allStudents.Where(s => s.ClassId == classId).ToList();

                    classStats.Add(new ClassStats
                    {
                        ClassId = classId,
                        StudentCount = classStudents.Count,
                        AverageGPA = classStudents.Any() ?
                            Math.Round(classStudents.Average(s => s.GPA), 2) : 0,
                        AverageAttendance = classStudents.Any() ?
                            (int)Math.Round(classStudents.Average(s => s.AttendancePercentage)) : 100,
                        TopStudent = classStudents.OrderByDescending(s => s.GPA)
                            .FirstOrDefault()?.FullName ?? "No students"
                    });
                }

                ViewBag.ClassStats = classStats;
                ViewBag.Teacher = teacher;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading teacher classes");
                TempData["ErrorMessage"] = "Error loading classes.";
                return View();
            }
        }

        // Add Grade - With teacher ID auto-fill
        [HttpGet]
        public IActionResult AddGrade(string studentId = "", string subject = "")
        {
            var grade = new Grade
            {
                StudentId = studentId,
                Subject = subject,
                Term = "First",
                Year = DateTime.Now.Year,
                DateRecorded = DateTime.Now,
                TeacherId = _teacherId // Auto-fill with current teacher's ID
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

                // Verify student exists and is in teacher's class
                var student = await _schoolService.GetStudentByIdAsync(grade.StudentId);
                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found.";
                    return RedirectToAction("MyStudents");
                }

                // Check if student is in teacher's class
                var teacher = await _schoolService.GetTeacherByIdAsync(_teacherId);
                if (!teacher?.Classes?.Contains(student.ClassId) == true)
                {
                    TempData["ErrorMessage"] = "You can only add grades for students in your classes.";
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

        // Teacher Profile
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
                var teacherStudents = await _schoolService.GetStudentsByTeacherAsync(_teacherId);

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
                ViewBag.TeacherStudents = teacherStudents.Take(6).ToList();

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

        // Helper class for MyClasses page
        public class ClassStats
        {
            public string ClassId { get; set; } = string.Empty;
            public int StudentCount { get; set; }
            public decimal AverageGPA { get; set; }
            public int AverageAttendance { get; set; }
            public string TopStudent { get; set; } = string.Empty;
        }
    }

    // View Models
    public class TeacherDashboardStats
    {
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }
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

    public class TeacherProfileStats
    {
        public int TotalStudents { get; set; }
        public decimal AverageGPA { get; set; }
        public int AverageAttendance { get; set; }
        public int TotalClasses { get; set; }
        public int TotalSubjects { get; set; }
        public int YearsOfService { get; set; }
    }
}