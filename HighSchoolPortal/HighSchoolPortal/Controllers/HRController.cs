using HighSchoolPortal.Interfaces;
using HighSchoolPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

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
                var students = await _schoolService.GetAllStudentsAsync();
                var teachers = await _schoolService.GetAllTeachersAsync();
                var classes = await _schoolService.GetAllClassesAsync();

                // Calculate statistics
                var stats = new HRDashboardStats
                {
                    TotalStudents = students.Count(),
                    TotalTeachers = teachers.Count(),
                    TotalClasses = classes.Count(),
                    ActiveStudents = students.Count(s => s.IsActive),
                    ActiveTeachers = teachers.Count(t => t.IsActive),
                    AverageStudentGPA = students.Any() ?
                        Math.Round(students.Average(s => s.GPA), 2) : 0,
                    AverageStudentAttendance = students.Any() ?
                        (int)Math.Round(students.Average(s => s.AttendancePercentage)) : 100
                };

                // Get recent activities
                var recentStudents = students.OrderByDescending(s => s.EnrollmentDate).Take(5);
                var recentTeachers = teachers.OrderByDescending(t => t.HireDate).Take(5);

                // Calculate pass rates by grade
                var passRates = new Dictionary<string, decimal>();
                var gradeLevels = students.Select(s => s.GradeLevel).Distinct();
                foreach (var grade in gradeLevels)
                {
                    var gradeStudents = students.Where(s => s.GradeLevel == grade);
                    var passCount = gradeStudents.Count(s => s.GPA >= 2.0m);
                    passRates[grade] = gradeStudents.Any() ?
                        Math.Round((decimal)passCount / gradeStudents.Count() * 100, 1) : 0;
                }

                ViewBag.Stats = stats;
                ViewBag.RecentStudents = recentStudents;
                ViewBag.RecentTeachers = recentTeachers;
                ViewBag.PassRates = passRates;
                ViewBag.TotalHR = await _schoolService.GetHRCountAsync();

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading HR dashboard");
                TempData["ErrorMessage"] = "Error loading dashboard data.";
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> ManageStudents(string search = "", string grade = "", string sortBy = "name", int page = 1, int pageSize = 20)
        {
            try
            {
                var students = await _schoolService.GetAllStudentsAsync();

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    students = students.Where(s =>
                        s.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        s.StudentId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        s.Email.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(grade))
                {
                    students = students.Where(s => s.GradeLevel == grade).ToList();
                }

                // Apply sorting
                students = sortBy.ToLower() switch
                {
                    "name" => students.OrderBy(s => s.FullName).ToList(),
                    "gpa" => students.OrderByDescending(s => s.GPA).ToList(),
                    "grade" => students.OrderBy(s => s.GradeLevel).ToList(),
                    "attendance" => students.OrderByDescending(s => s.AttendancePercentage).ToList(),
                    "date" => students.OrderByDescending(s => s.EnrollmentDate).ToList(),
                    _ => students.OrderBy(s => s.FullName).ToList()
                };

                // Pagination
                var totalStudents = students.Count();
                var totalPages = (int)Math.Ceiling((double)totalStudents / pageSize);
                var pagedStudents = students.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                ViewBag.SearchTerm = search;
                ViewBag.GradeFilter = grade;
                ViewBag.SortBy = sortBy;
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalStudents = totalStudents;
                ViewBag.TotalPages = totalPages;

                // Statistics
                ViewBag.GradeLevels = students.Select(s => s.GradeLevel).Distinct().OrderBy(g => g);
                ViewBag.ActiveCount = students.Count(s => s.IsActive);
                ViewBag.AverageGPA = students.Any() ? Math.Round(students.Average(s => s.GPA), 2) : 0;

                return View(pagedStudents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error managing students");
                TempData["ErrorMessage"] = "Error loading students.";
                return View(new List<StudentProfile>());
            }
        }
        [HttpGet]
        public async Task<IActionResult> StudentDetails(string id) // Keep original name
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

                var grades = await _schoolService.GetStudentGradesAsync(id);
                var attendance = await _schoolService.GetStudentAttendanceAsync(id);

                // Calculate statistics
                var stats = new StudentHRStats
                {
                    TotalGrades = grades.Count(),
                    AverageScore = grades.Any() ? Math.Round(grades.Average(g => g.TotalScore), 2) : 0,
                    AttendancePercentage = student.AttendancePercentage,
                    DaysEnrolled = (DateTime.Now - student.EnrollmentDate).Days,
                    Subjects = grades.Select(g => g.Subject).Distinct().ToList()
                };

                ViewBag.Grades = grades.OrderByDescending(g => g.DateRecorded).ToList();
                ViewBag.Attendance = attendance.OrderByDescending(a => a.Date).Take(30).ToList();
                ViewBag.Stats = stats;

                return View(student); // Will look for StudentDetails.cshtml
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error viewing student details for ID: {id}");
                TempData["ErrorMessage"] = "Error loading student details.";
                return RedirectToAction("ManageStudents");
            }
        }

        // Add these methods to your existing HRController

        [HttpGet]
        public async Task<IActionResult> AssignTeacherSubjects(string id)
        {
            try
            {
                _logger.LogInformation($"Loading assignment page for teacher ID: {id}");

                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "Teacher ID is required.";
                    return RedirectToAction("ManageTeachers");
                }

                // Get teacher profile
                var teacher = await _schoolService.GetTeacherByIdAsync(id);
                if (teacher == null)
                {
                    _logger.LogWarning($"Teacher not found with ID: {id}");
                    TempData["ErrorMessage"] = $"Teacher with ID {id} not found.";
                    return RedirectToAction("ManageTeachers");
                }

                _logger.LogInformation($"Teacher found: {teacher.FullName} (ID: {teacher.Id})");

                // Get all available classes
                var allClasses = await _schoolService.GetAllClassesAsync();

                // If no classes exist, create default ones
                if (!allClasses.Any())
                {
                    TempData["InfoMessage"] = "No classes found. Creating default classes...";
                    await CreateDefaultClasses();
                    allClasses = await _schoolService.GetAllClassesAsync();
                }

                var availableClasses = allClasses
                    .Select(c => c.Name)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                // If teacher already has classes assigned, include them even if they don't exist in allClasses
                if (teacher.Classes != null && teacher.Classes.Any())
                {
                    foreach (var assignedClass in teacher.Classes)
                    {
                        if (!availableClasses.Contains(assignedClass))
                        {
                            availableClasses.Add(assignedClass);
                        }
                    }
                }

                ViewBag.Teacher = teacher;
                ViewBag.AvailableClasses = availableClasses.OrderBy(c => c).ToList();
                ViewBag.Subjects = GetSubjectList();

                return View(teacher);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading teacher assignment page for ID: {id}");
                TempData["ErrorMessage"] = "Error loading assignment data.";
                return RedirectToAction("ManageTeachers");
            }
        }

        private async Task CreateDefaultClasses()
        {
            try
            {
                var classes = new List<string> { "10A", "10B", "11A", "11B", "12A", "12B" };

                foreach (var className in classes)
                {
                    var grade = className.Substring(0, 2);
                    var classItem = new Class
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = className,
                        Code = $"GR{className}",
                        GradeLevel = grade,
                        TeacherId = "",
                        TeacherName = "",
                        Subject = "General",
                        AcademicYear = DateTime.Now.Year.ToString(),
                        Term = "First",
                        Schedule = "Mon-Fri 9:00-10:00",
                        RoomNumber = $"Room {100 + new Random().Next(1, 20)}",
                         MaxCapacity = 30,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _schoolService.AddClassAsync(classItem);
                }

                _logger.LogInformation($"Created {classes.Count} default classes.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating default classes");
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTeacherSubjects(string teacherId, List<string> selectedClasses, List<string> selectedSubjects)
        {
            try
            {
                _logger.LogInformation($"Processing assignment for teacher ID: {teacherId}");

                if (string.IsNullOrEmpty(teacherId))
                {
                    TempData["ErrorMessage"] = "Teacher ID is required.";
                    return RedirectToAction("ManageTeachers");
                }

                // Get teacher first
                var teacher = await _schoolService.GetTeacherByIdAsync(teacherId);
                if (teacher == null)
                {
                    TempData["ErrorMessage"] = "Teacher not found.";
                    return RedirectToAction("ManageTeachers");
                }

                if (selectedClasses == null || !selectedClasses.Any())
                {
                    TempData["ErrorMessage"] = "Please select at least one class.";
                    return RedirectToAction("AssignTeacherSubjects", new { id = teacherId });
                }

                if (selectedSubjects == null || !selectedSubjects.Any())
                {
                    TempData["ErrorMessage"] = "Please select at least one subject.";
                    return RedirectToAction("AssignTeacherSubjects", new { id = teacherId });
                }

                _logger.LogInformation($"Assigning {selectedSubjects.Count} subjects and {selectedClasses.Count} classes to {teacher.FullName}");

                // Update teacher with assigned classes and subjects
                teacher.Classes = selectedClasses;
                teacher.Subjects = selectedSubjects;
                teacher.UpdatedAt = DateTime.UtcNow;

                await _schoolService.UpdateTeacherAsync(teacher);

                // Update classes with teacher assignment
                var allClasses = await _schoolService.GetAllClassesAsync();
                foreach (var classItem in allClasses.Where(c => selectedClasses.Contains(c.Name)))
                {
                    classItem.TeacherId = teacherId;
                    classItem.TeacherName = teacher.FullName;
                    await _schoolService.UpdateClassAsync(classItem);
                }

                TempData["SuccessMessage"] = $"Successfully assigned {selectedSubjects.Count} subjects and {selectedClasses.Count} classes to {teacher.FullName}.";
                return RedirectToAction("ViewTeacher", new { id = teacherId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning teacher subjects");
                TempData["ErrorMessage"] = $"Error assigning subjects: {ex.Message}";
                return RedirectToAction("AssignTeacherSubjects", new { id = teacherId });
            }
        }

        // Helper method to get subject list
        private List<string> GetSubjectList()
        {
            return new List<string>
    {
        "Mathematics",
        "Science",
        "English",
        "History",
        "Geography",
        "Physics",
        "Chemistry",
        "Biology",
        "Computer Science",
        "Art",
        "Music",
        "Physical Education",
        "Economics",
        "Business Studies",
        "Psychology",
        "Sociology"
    };
        }


        [HttpGet]
        public IActionResult AddStudent()
        {
            var student = new StudentProfile
            {
                EnrollmentDate = DateTime.Today,
                GradeLevel = "10",
                IsActive = true
            };

            ViewBag.GradeLevels = new List<string> { "9", "10", "11", "12" };
            ViewBag.Classes = GetClassList();

            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudent(StudentProfile student)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.GradeLevels = new List<string> { "9", "10", "11", "12" };
                    ViewBag.Classes = GetClassList();
                    return View(student);
                }

                // Generate student ID if not provided
                if (string.IsNullOrEmpty(student.StudentId))
                {
                    student.StudentId = $"STU{DateTime.Now:yyyyMMddHHmmss}";
                }

                // Let the service generate the ID
                student.Id = null; // Let Firebase generate the ID
                student.Role = "student";
                student.CreatedAt = DateTime.UtcNow;
                student.UpdatedAt = DateTime.UtcNow;
                student.IsActive = true;
                student.AttendancePercentage = 100;
                student.GPA = 0.0m;

                // Set default avatar if not provided
                if (string.IsNullOrEmpty(student.AvatarUrl))
                {
                    student.AvatarUrl = "/images/default-avatar.png";
                }

                var result = await _schoolService.AddStudentAsync(student);

                if (result != null && !string.IsNullOrEmpty(result.Id))
                {
                    TempData["SuccessMessage"] = $"Student '{student.FullName}' added successfully!";
                    return RedirectToAction("ViewStudent", new { id = result.Id });
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to add student. Please try again.";
                    ViewBag.GradeLevels = new List<string> { "9", "10", "11", "12" };
                    ViewBag.Classes = GetClassList();
                    return View(student);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding student");
                TempData["ErrorMessage"] = $"Error adding student: {ex.Message}";
                ViewBag.GradeLevels = new List<string> { "9", "10", "11", "12" };
                ViewBag.Classes = GetClassList();
                return View(student);
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

            ViewBag.GradeLevels = new List<string> { "9", "10", "11", "12" };
            ViewBag.Classes = GetClassList();

            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(StudentProfile student)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.GradeLevels = new List<string> { "9", "10", "11", "12" };
                    ViewBag.Classes = GetClassList();
                    return View(student);
                }

                // Get existing student to preserve some fields
                var existingStudent = await _schoolService.GetStudentByIdAsync(student.Id);
                if (existingStudent == null)
                {
                    TempData["ErrorMessage"] = "Student not found.";
                    return RedirectToAction("ManageStudents");
                }

                // Preserve fields that shouldn't be changed
                student.CreatedAt = existingStudent.CreatedAt;
                student.StudentId = existingStudent.StudentId;
                student.Role = "student";
                student.UpdatedAt = DateTime.UtcNow;

                // Ensure required fields have values
                if (string.IsNullOrEmpty(student.AvatarUrl))
                {
                    student.AvatarUrl = "/images/default-avatar.png";
                }

                await _schoolService.UpdateStudentAsync(student);

                TempData["SuccessMessage"] = $"Student '{student.FullName}' updated successfully!";
                return RedirectToAction("ViewStudent", new { id = student.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student");
                TempData["ErrorMessage"] = $"Error updating student: {ex.Message}";
                ViewBag.GradeLevels = new List<string> { "9", "10", "11", "12" };
                ViewBag.Classes = GetClassList();
                return View(student);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudent(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "Invalid student ID.";
                    return RedirectToAction("ManageStudents");
                }

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
                _logger.LogError(ex, $"Error deleting student: {id}");
                TempData["ErrorMessage"] = $"Error deleting student: {ex.Message}";
                return RedirectToAction("ManageStudents");
            }
        }




        [HttpGet]
        public IActionResult AddTeacher()
        {
            var teacher = new TeacherProfile
            {
                HireDate = DateTime.Today,
                Department = "General",
                IsActive = true
            };

            ViewBag.Departments = GetDepartmentList();
            ViewBag.Qualifications = GetQualificationList();

            return View(teacher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeacher(TeacherProfile teacher)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Departments = GetDepartmentList();
                    ViewBag.Qualifications = GetQualificationList();
                    return View(teacher);
                }

                // Generate teacher ID if not provided
                if (string.IsNullOrEmpty(teacher.TeacherId))
                {
                    teacher.TeacherId = $"TCH{DateTime.Now:yyyyMMddHHmmss}";
                }

                // Let the service generate the ID
                teacher.Id = null;
                teacher.Role = "teacher";
                teacher.CreatedAt = DateTime.UtcNow;
                teacher.UpdatedAt = DateTime.UtcNow;
                teacher.IsActive = true;
                teacher.Classes = teacher.Classes ?? new List<string>();
                teacher.Subjects = teacher.Subjects ?? new List<string>();

                // Set default avatar if not provided
                if (string.IsNullOrEmpty(teacher.AvatarUrl))
                {
                    teacher.AvatarUrl = "/images/default-avatar.png";
                }

                var result = await _schoolService.AddTeacherAsync(teacher);

                if (result != null && !string.IsNullOrEmpty(result.Id))
                {
                    TempData["SuccessMessage"] = $"Teacher '{teacher.FullName}' added successfully!";
                    return RedirectToAction("ViewTeacher", new { id = result.Id });
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to add teacher. Please try again.";
                    ViewBag.Departments = GetDepartmentList();
                    ViewBag.Qualifications = GetQualificationList();
                    return View(teacher);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding teacher");
                TempData["ErrorMessage"] = $"Error adding teacher: {ex.Message}";
                ViewBag.Departments = GetDepartmentList();
                ViewBag.Qualifications = GetQualificationList();
                return View(teacher);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditTeacher(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Invalid teacher ID.";
                return RedirectToAction("ManageTeachers");
            }

            try
            {
                var teacher = await _schoolService.GetTeacherByIdAsync(id);
                if (teacher == null)
                {
                    TempData["ErrorMessage"] = "Teacher not found.";
                    return RedirectToAction("ManageTeachers");
                }

                ViewBag.Departments = GetDepartmentList();
                ViewBag.Qualifications = GetQualificationList();

                return View(teacher);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading teacher for edit: {id}");
                TempData["ErrorMessage"] = "Error loading teacher data.";
                return RedirectToAction("ManageTeachers");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeacher(TeacherProfile teacher)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Departments = GetDepartmentList();
                    ViewBag.Qualifications = GetQualificationList();
                    return View(teacher);
                }

                // Get existing teacher to preserve some fields
                var existingTeacher = await _schoolService.GetTeacherByIdAsync(teacher.Id);
                if (existingTeacher == null)
                {
                    TempData["ErrorMessage"] = "Teacher not found.";
                    return RedirectToAction("ManageTeachers");
                }

                // Preserve fields that shouldn't be changed
                teacher.CreatedAt = existingTeacher.CreatedAt;
                teacher.TeacherId = existingTeacher.TeacherId;
                teacher.Role = "teacher";
                teacher.UpdatedAt = DateTime.UtcNow;

                // Ensure collections are not null
                teacher.Classes = teacher.Classes ?? new List<string>();
                teacher.Subjects = teacher.Subjects ?? new List<string>();

                // Ensure required fields have values
                if (string.IsNullOrEmpty(teacher.AvatarUrl))
                {
                    teacher.AvatarUrl = "/images/default-avatar.png";
                }

                await _schoolService.UpdateTeacherAsync(teacher);

                TempData["SuccessMessage"] = $"Teacher '{teacher.FullName}' updated successfully!";
                return RedirectToAction("ViewTeacher", new { id = teacher.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating teacher");
                TempData["ErrorMessage"] = $"Error updating teacher: {ex.Message}";
                ViewBag.Departments = GetDepartmentList();
                ViewBag.Qualifications = GetQualificationList();
                return View(teacher);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTeacher(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "Invalid teacher ID.";
                    return RedirectToAction("ManageTeachers");
                }

                var teacher = await _schoolService.GetTeacherByIdAsync(id);
                if (teacher == null)
                {
                    TempData["ErrorMessage"] = "Teacher not found.";
                    return RedirectToAction("ManageTeachers");
                }

                await _schoolService.DeleteUserAsync(id);

                TempData["SuccessMessage"] = $"Teacher '{teacher.FullName}' deleted successfully.";
                return RedirectToAction("ManageTeachers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting teacher: {id}");
                TempData["ErrorMessage"] = $"Error deleting teacher: {ex.Message}";
                return RedirectToAction("ManageTeachers");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ManageTeachers(string search = "", string department = "", string sortBy = "name", int page = 1, int pageSize = 20)
        {
            try
            {
                var teachers = await _schoolService.GetAllTeachersAsync();

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    teachers = teachers.Where(t =>
                        t.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        t.TeacherId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        t.Email.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(department))
                {
                    teachers = teachers.Where(t => t.Department == department).ToList();
                }

                // Apply sorting
                teachers = sortBy.ToLower() switch
                {
                    "name" => teachers.OrderBy(t => t.FullName).ToList(),
                    "department" => teachers.OrderBy(t => t.Department).ToList(),
                    "hiredate" => teachers.OrderByDescending(t => t.HireDate).ToList(),
                    "classes" => teachers.OrderByDescending(t => t.Classes?.Count ?? 0).ToList(),
                    _ => teachers.OrderBy(t => t.FullName).ToList()
                };

                // Pagination
                var totalTeachers = teachers.Count();
                var totalPages = (int)Math.Ceiling((double)totalTeachers / pageSize);
                var pagedTeachers = teachers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                ViewBag.SearchTerm = search;
                ViewBag.DepartmentFilter = department;
                ViewBag.SortBy = sortBy;
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalTeachers = totalTeachers;
                ViewBag.TotalPages = totalPages;

                // Statistics
                ViewBag.Departments = teachers.Select(t => t.Department).Distinct().OrderBy(d => d);
                ViewBag.ActiveCount = teachers.Count(t => t.IsActive);
                ViewBag.AverageClasses = teachers.Any() ?
                    Math.Round(teachers.Average(t => t.Classes?.Count ?? 0), 1) : 0;

                return View(pagedTeachers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error managing teachers");
                TempData["ErrorMessage"] = "Error loading teachers.";
                return View(new List<TeacherProfile>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> TeacherDetails(string id) // Keep original name
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Invalid teacher ID.";
                return RedirectToAction("ManageTeachers");
            }

            try
            {
                var teacher = await _schoolService.GetTeacherByIdAsync(id);
                if (teacher == null)
                {
                    TempData["ErrorMessage"] = "Teacher not found.";
                    return RedirectToAction("ManageTeachers");
                }

                // Get teacher's students
                var allStudents = await _schoolService.GetAllStudentsAsync();
                var teacherStudents = allStudents.Where(s =>
                    !string.IsNullOrEmpty(s.ClassId) &&
                    teacher.Classes.Contains(s.ClassId)).ToList();

                // Calculate teacher statistics
                var stats = new TeacherHRStats
                {
                    TotalStudents = teacherStudents.Count,
                    TotalClasses = teacher.Classes.Count,
                    TotalSubjects = teacher.Subjects.Count,
                    AverageStudentGPA = teacherStudents.Any() ?
                        Math.Round(teacherStudents.Average(s => s.GPA), 2) : 0,
                    AverageStudentAttendance = teacherStudents.Any() ?
                        (int)Math.Round(teacherStudents.Average(s => s.AttendancePercentage)) : 100
                };

                ViewBag.TeacherStudents = teacherStudents;
                ViewBag.Stats = stats;

                return View(teacher); // Will look for TeacherDetails.cshtml
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error viewing teacher details for ID: {id}");
                TempData["ErrorMessage"] = "Error loading teacher details.";
                return RedirectToAction("ManageTeachers");
            }
        }



        [HttpGet]
        public async Task<IActionResult> SchoolStatistics()
        {
            try
            {
                var students = await _schoolService.GetAllStudentsAsync();
                var teachers = await _schoolService.GetAllTeachersAsync();
                var classes = await _schoolService.GetAllClassesAsync();

                var statistics = new SchoolStatistics
                {
                    TotalStudents = students.Count(),
                    TotalTeachers = teachers.Count(),
                    TotalClasses = classes.Count(),
                    StudentTeacherRatio = teachers.Any() ?
                        Math.Round((double)students.Count() / teachers.Count(), 2) : 0,
                    AverageClassSize = classes.Any() ?
                        Math.Round(classes.Average(c => c.StudentCount), 1) : 0
                };

                // Grade distribution
                statistics.GradeDistribution = students
                    .GroupBy(s => s.GradeLevel)
                    .Select(g => new GradeDistribution
                    {
                        GradeLevel = g.Key,
                        StudentCount = g.Count(),
                        AverageGPA = Math.Round(g.Average(s => s.GPA), 2),
                        AverageAttendance = (int)Math.Round(g.Average(s => s.AttendancePercentage))
                    })
                    .OrderBy(g => g.GradeLevel)
                    .ToList();

                // Department statistics
                statistics.DepartmentStats = teachers
                    .GroupBy(t => t.Department)
                    .Select(g => new DepartmentStats
                    {
                        Department = g.Key,
                        TeacherCount = g.Count(),
                        AverageExperience = Math.Round(g.Average(t =>
                            (DateTime.Now - t.HireDate).TotalDays / 365), 1)
                    })
                    .OrderByDescending(d => d.TeacherCount)
                    .ToList();

                // Pass/Fail statistics
                var passingStudents = students.Count(s => s.GPA >= 2.0m);
                statistics.PassRate = students.Any() ?
                    Math.Round((decimal)passingStudents / students.Count() * 100, 1) : 0;

                ViewBag.Statistics = statistics;
                ViewBag.GeneratedDate = DateTime.Now.ToString("MMMM dd, yyyy HH:mm");

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating school statistics");
                TempData["ErrorMessage"] = "Error generating statistics.";
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GenerateReport(string reportType = "overview", string format = "html")
        {
            try
            {
                var report = new SchoolReport
                {
                    ReportType = reportType,
                    GeneratedDate = DateTime.Now,
                    AcademicYear = DateTime.Now.Year
                };

                var students = await _schoolService.GetAllStudentsAsync();
                var teachers = await _schoolService.GetAllTeachersAsync();

                switch (reportType.ToLower())
                {
                    case "overview":
                        report.Title = "School Overview Report";
                        report.Data = new
                        {
                            TotalStudents = students.Count(),
                            TotalTeachers = teachers.Count(),
                            StudentTeacherRatio = teachers.Any() ?
                                Math.Round((double)students.Count() / teachers.Count(), 2) : 0,
                            AverageGPA = students.Any() ?
                                Math.Round(students.Average(s => s.GPA), 2) : 0,
                            AverageAttendance = students.Any() ?
                                (int)Math.Round(students.Average(s => s.AttendancePercentage)) : 100
                        };
                        break;

                    case "performance":
                        report.Title = "Academic Performance Report";
                        report.Data = students
                            .GroupBy(s => s.GradeLevel)
                            .Select(g => new
                            {
                                GradeLevel = g.Key,
                                StudentCount = g.Count(),
                                AverageGPA = Math.Round(g.Average(s => s.GPA), 2),
                                PassingRate = Math.Round((decimal)g.Count(s => s.GPA >= 2.0m) / g.Count() * 100, 1),
                                TopStudent = g.OrderByDescending(s => s.GPA).FirstOrDefault()?.FullName
                            })
                            .OrderBy(g => g.GradeLevel);
                        break;

                    case "attendance":
                        report.Title = "Attendance Report";
                        report.Data = students
                            .Select(s => new
                            {
                                s.FullName,
                                s.StudentId,
                                s.GradeLevel,
                                s.ClassId,
                                s.AttendancePercentage,
                                Status = s.AttendancePercentage >= 90 ? "Excellent" :
                                         s.AttendancePercentage >= 75 ? "Good" :
                                         s.AttendancePercentage >= 60 ? "Fair" : "Poor"
                            })
                            .OrderByDescending(s => s.AttendancePercentage);
                        break;

                    case "teacher":
                        report.Title = "Teacher Performance Report";
                        report.Data = teachers
                            .Select(t => new
                            {
                                t.FullName,
                                t.Department,
                                t.Qualification,
                                ExperienceYears = Math.Round((DateTime.Now - t.HireDate).TotalDays / 365, 1),
                                ClassCount = t.Classes?.Count ?? 0,
                                SubjectCount = t.Subjects?.Count ?? 0
                            })
                            .OrderBy(t => t.Department)
                            .ThenBy(t => t.FullName);
                        break;
                }

                ViewBag.Report = report;
                ViewBag.Format = format;

                if (format == "pdf")
                {
                    // In production, implement PDF generation using a library like iTextSharp or QuestPDF
                    TempData["InfoMessage"] = "PDF export feature coming soon. Please use HTML view for now.";
                    return RedirectToAction("GenerateReport", new { reportType, format = "html" });
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                TempData["ErrorMessage"] = "Error generating report.";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentCount()
        {
            try
            {
                var count = await _schoolService.GetStudentCountAsync();
                return Json(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student count");
                return Json(new { count = 0 });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTeacherCount()
        {
            try
            {
                var count = await _schoolService.GetTeacherCountAsync();
                return Json(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teacher count");
                return Json(new { count = 0 });
            }
        }

        // Helper Methods
        private List<string> GetClassList()
        {
            return new List<string>
            {
                "10A", "10B", "10C",
                "11A", "11B", "11C",
                "12A", "12B", "12C"
            };
        }

        private List<string> GetDepartmentList()
        {
            return new List<string>
            {
                "Mathematics",
                "Science",
                "English",
                "History",
                "Arts",
                "Physical Education",
                "Technology",
                "Administration"
            };
        }

        private List<string> GetQualificationList()
        {
            return new List<string>
            {
                "B.Ed",
                "M.Ed",
                "B.Sc + B.Ed",
                "M.Sc + B.Ed",
                "Ph.D",
                "Other"
            };
        }
    }

    // View Models for HR
    public class HRDashboardStats
    {
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalClasses { get; set; }
        public int ActiveStudents { get; set; }
        public int ActiveTeachers { get; set; }
        public decimal AverageStudentGPA { get; set; }
        public int AverageStudentAttendance { get; set; }
    }

    public class StudentHRStats
    {
        public int TotalGrades { get; set; }
        public decimal AverageScore { get; set; }
        public int AttendancePercentage { get; set; }
        public int DaysEnrolled { get; set; }
        public List<string> Subjects { get; set; } = new List<string>();
    }

    public class TeacherHRStats
    {
        public int TotalStudents { get; set; }
        public int TotalClasses { get; set; }
        public int TotalSubjects { get; set; }
        public decimal AverageStudentGPA { get; set; }
        public int AverageStudentAttendance { get; set; }
    }

    public class SchoolStatistics
    {
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalClasses { get; set; }
        public double StudentTeacherRatio { get; set; }
        public double AverageClassSize { get; set; }
        public decimal PassRate { get; set; }
        public List<GradeDistribution> GradeDistribution { get; set; } = new List<GradeDistribution>();
        public List<DepartmentStats> DepartmentStats { get; set; } = new List<DepartmentStats>();
    }

    public class GradeDistribution
    {
        public string GradeLevel { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public decimal AverageGPA { get; set; }
        public int AverageAttendance { get; set; }
    }

    public class DepartmentStats
    {
        public string Department { get; set; } = string.Empty;
        public int TeacherCount { get; set; }
        public double AverageExperience { get; set; }
    }

    public class SchoolReport
    {
        public string Title { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; }
        public int AcademicYear { get; set; }
        public object? Data { get; set; }
    }
}