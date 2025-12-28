using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using HighSchoolPortal.Interfaces;
using HighSchoolPortal.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HighSchoolPortal.Services
{
    public class FirebaseSchoolService : IFirebaseSchoolService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FirebaseSchoolService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _projectId;

        public FirebaseSchoolService(
            IConfiguration configuration,
            ILogger<FirebaseSchoolService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _projectId = _configuration["Firebase:ProjectId"] ?? throw new InvalidOperationException("Firebase ProjectId is not configured");
        }

        #region Dashboard Statistics
        public async Task<int> GetStudentCountAsync()
        {
            try
            {
                var students = await GetAllStudentsAsync();
                return students.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student count");
                return 0;
            }
        }

        public async Task<int> GetTeacherCountAsync()
        {
            try
            {
                var teachers = await GetAllTeachersAsync();
                return teachers.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teacher count");
                return 0;
            }
        }

        public async Task<int> GetClassCountAsync()
        {
            try
            {
                var classes = await GetAllClassesAsync();
                return classes.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting class count");
                return 0;
            }
        }

        public async Task<int> GetHRCountAsync()
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";
                var query = new
                {
                    structuredQuery = new
                    {
                        from = new[] { new { collectionId = "users" } },
                        where = new
                        {
                            fieldFilter = new
                            {
                                field = new { fieldPath = "role" },
                                op = "EQUAL",
                                value = new { stringValue = "hr" }
                            }
                        }
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(url, query);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    var count = doc.RootElement.EnumerateArray().Count();
                    return count;
                }
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting HR count");
                return 0;
            }
        }
        #endregion

        #region Student Management
        public async Task<IEnumerable<StudentProfile>> GetAllStudentsAsync()
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/students";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to get students: {response.StatusCode}");
                    return new List<StudentProfile>();
                }

                var content = await response.Content.ReadAsStringAsync();
                return ParseStudentsFromFirestore(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all students");
                return new List<StudentProfile>();
            }
        }

        public async Task<StudentProfile> GetStudentByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/students/{id}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                return ParseStudentFromFirestore(id, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting student by id: {id}");
                return null;
            }
        }

        public async Task<StudentProfile> AddStudentAsync(StudentProfile student)
        {
            try
            {
                var firestoreData = new
                {
                    fields = ConvertStudentToFirestoreFields(student)
                };

                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/students?documentId={student.Id}";
                var response = await _httpClient.PostAsync(url, JsonContent.Create(firestoreData));

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Student {student.Email} added successfully");
                    return student;
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to add student: {error}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding student");
                return null;
            }
        }

        public async Task UpdateStudentAsync(StudentProfile student)
        {
            try
            {
                student.UpdatedAt = DateTime.UtcNow;
                var firestoreData = new
                {
                    fields = ConvertStudentToFirestoreFields(student),
                    updateMask = new { fieldPaths = GetStudentFieldPaths() }
                };

                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/students/{student.Id}";
                var response = await _httpClient.PatchAsync(url, JsonContent.Create(firestoreData));

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to update student: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student");
                throw;
            }
        }
        #endregion

        #region Teacher Management
        public async Task<IEnumerable<TeacherProfile>> GetAllTeachersAsync()
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/teachers";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to get teachers: {response.StatusCode}");
                    return new List<TeacherProfile>();
                }

                var content = await response.Content.ReadAsStringAsync();
                return ParseTeachersFromFirestore(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all teachers");
                return new List<TeacherProfile>();
            }
        }

        public async Task<TeacherProfile> GetTeacherByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/teachers/{id}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                return ParseTeacherFromFirestore(id, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting teacher by id: {id}");
                return null;
            }
        }

        public async Task<TeacherProfile> AddTeacherAsync(TeacherProfile teacher)
        {
            try
            {
                var firestoreData = new
                {
                    fields = ConvertTeacherToFirestoreFields(teacher)
                };

                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/teachers?documentId={teacher.Id}";
                var response = await _httpClient.PostAsync(url, JsonContent.Create(firestoreData));

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Teacher {teacher.Email} added successfully");
                    return teacher;
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to add teacher: {error}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding teacher");
                return null;
            }
        }

        public async Task UpdateTeacherAsync(TeacherProfile teacher)
        {
            try
            {
                teacher.UpdatedAt = DateTime.UtcNow;
                var firestoreData = new
                {
                    fields = ConvertTeacherToFirestoreFields(teacher),
                    updateMask = new { fieldPaths = GetTeacherFieldPaths() }
                };

                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/teachers/{teacher.Id}";
                var response = await _httpClient.PatchAsync(url, JsonContent.Create(firestoreData));

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to update teacher: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating teacher");
                throw;
            }
        }
        #endregion

        #region Class Management
        public async Task<Class> AddClassAsync(Class classItem)
        {
            try
            {
                var firestoreData = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["id"] = new { stringValue = classItem.Id },
                        ["name"] = new { stringValue = classItem.Name },
                        ["gradeLevel"] = new { stringValue = classItem.GradeLevel },
                        ["teacherId"] = new { stringValue = classItem.TeacherId },
                        ["teacherName"] = new { stringValue = classItem.TeacherName },
                        ["subject"] = new { stringValue = classItem.Subject },
                        ["roomNumber"] = new { stringValue = classItem.RoomNumber },
                        ["schedule"] = new { stringValue = classItem.Schedule },
                        ["studentIds"] = new { arrayValue = new { values = classItem.StudentIds.Select(id => new { stringValue = id }).ToArray() } },
                        ["createdAt"] = new { timestampValue = classItem.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                        ["updatedAt"] = new { timestampValue = classItem.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
                    }
                };

                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/classes?documentId={classItem.Id}";
                var response = await _httpClient.PostAsync(url, JsonContent.Create(firestoreData));

                if (response.IsSuccessStatusCode)
                {
                    return classItem;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding class");
                return null;
            }
        }

        public async Task<IEnumerable<Class>> GetAllClassesAsync()
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/classes";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return new List<Class>();
                }

                var content = await response.Content.ReadAsStringAsync();
                return ParseClassesFromFirestore(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting classes");
                return new List<Class>();
            }
        }

        public async Task<Class> GetClassByIdAsync(string id)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/classes/{id}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                return ParseClassFromFirestore(id, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting class by id");
                return null;
            }
        }

        public async Task UpdateClassAsync(Class classItem)
        {
            try
            {
                classItem.UpdatedAt = DateTime.UtcNow;
                var firestoreData = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["name"] = new { stringValue = classItem.Name },
                        ["gradeLevel"] = new { stringValue = classItem.GradeLevel },
                        ["teacherId"] = new { stringValue = classItem.TeacherId },
                        ["teacherName"] = new { stringValue = classItem.TeacherName },
                        ["subject"] = new { stringValue = classItem.Subject },
                        ["roomNumber"] = new { stringValue = classItem.RoomNumber },
                        ["schedule"] = new { stringValue = classItem.Schedule },
                        ["studentIds"] = new { arrayValue = new { values = classItem.StudentIds.Select(id => new { stringValue = id }).ToArray() } },
                        ["updatedAt"] = new { timestampValue = classItem.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
                    },
                    updateMask = new { fieldPaths = new[] { "name", "gradeLevel", "teacherId", "teacherName", "subject", "roomNumber", "schedule", "studentIds", "updatedAt" } }
                };

                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/classes/{classItem.Id}";
                var response = await _httpClient.PatchAsync(url, JsonContent.Create(firestoreData));

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to update class: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating class");
                throw;
            }
        }
        #endregion

        #region Grade Management
        public async Task<Grade> AddGradeAsync(Grade grade)
        {
            try
            {
                grade.Id = Guid.NewGuid().ToString();
                grade.CalculateTotal();

                var firestoreData = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["id"] = new { stringValue = grade.Id },
                        ["studentId"] = new { stringValue = grade.StudentId },
                        ["studentName"] = new { stringValue = grade.StudentName },
                        ["teacherId"] = new { stringValue = grade.TeacherId },
                        ["subject"] = new { stringValue = grade.Subject },
                        ["term"] = new { stringValue = grade.Term },
                        ["year"] = new { integerValue = grade.Year },
                        ["test1"] = new { doubleValue = (double)grade.Test1 },
                        ["test2"] = new { doubleValue = (double)grade.Test2 },
                        ["exam"] = new { doubleValue = (double)grade.Exam },
                        ["assignment"] = new { doubleValue = (double)grade.Assignment },
                        ["totalScore"] = new { doubleValue = (double)grade.TotalScore },
                        ["gradeLetter"] = new { stringValue = grade.GradeLetter },
                        ["remarks"] = new { stringValue = grade.Remarks },
                        ["dateRecorded"] = new { timestampValue = grade.DateRecorded.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                        ["updatedAt"] = new { timestampValue = grade.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
                    }
                };

                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/grades?documentId={grade.Id}";
                var response = await _httpClient.PostAsync(url, JsonContent.Create(firestoreData));

                if (response.IsSuccessStatusCode)
                {
                    return grade;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding grade");
                return null;
            }
        }

        public async Task<IEnumerable<Grade>> GetStudentGradesAsync(string studentId)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";
                var query = new
                {
                    structuredQuery = new
                    {
                        from = new[] { new { collectionId = "grades" } },
                        where = new
                        {
                            fieldFilter = new
                            {
                                field = new { fieldPath = "studentId" },
                                op = "EQUAL",
                                value = new { stringValue = studentId }
                            }
                        },
                        orderBy = new[] { new { field = new { fieldPath = "dateRecorded" }, direction = "DESCENDING" } }
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(url, query);
                if (!response.IsSuccessStatusCode)
                {
                    return new List<Grade>();
                }

                var content = await response.Content.ReadAsStringAsync();
                return ParseGradesFromFirestore(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student grades");
                return new List<Grade>();
            }
        }

        public async Task<IEnumerable<Grade>> GetClassGradesAsync(string classId, string subject)
        {
            // Implementation depends on your data structure
            return new List<Grade>();
        }

        public async Task<Grade> UpdateGradeAsync(Grade grade)
        {
            // Similar to AddGradeAsync but with PATCH request
            return grade;
        }

        public async Task DeleteGradeAsync(string gradeId)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/grades/{gradeId}";
                await _httpClient.DeleteAsync(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting grade");
                throw;
            }
        }
        #endregion

        #region Attendance Management
        public async Task<Attendance> RecordAttendanceAsync(Attendance attendance)
        {
            try
            {
                attendance.Id = Guid.NewGuid().ToString();
                var firestoreData = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["id"] = new { stringValue = attendance.Id },
                        ["studentId"] = new { stringValue = attendance.StudentId },
                        ["studentName"] = new { stringValue = attendance.StudentName },
                        ["classId"] = new { stringValue = attendance.ClassId },
                        ["className"] = new { stringValue = attendance.ClassName },
                        ["date"] = new { timestampValue = attendance.Date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                        ["status"] = new { stringValue = attendance.Status },
                        ["remarks"] = new { stringValue = attendance.Remarks },
                        ["recordedBy"] = new { stringValue = attendance.RecordedBy },
                        ["recordedAt"] = new { timestampValue = attendance.RecordedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
                    }
                };

                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/attendance?documentId={attendance.Id}";
                var response = await _httpClient.PostAsync(url, JsonContent.Create(firestoreData));

                if (response.IsSuccessStatusCode)
                {
                    return attendance;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording attendance");
                return null;
            }
        }

        public async Task<IEnumerable<Attendance>> GetStudentAttendanceAsync(string studentId)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";
                var query = new
                {
                    structuredQuery = new
                    {
                        from = new[] { new { collectionId = "attendance" } },
                        where = new
                        {
                            fieldFilter = new
                            {
                                field = new { fieldPath = "studentId" },
                                op = "EQUAL",
                                value = new { stringValue = studentId }
                            }
                        },
                        orderBy = new[] { new { field = new { fieldPath = "date" }, direction = "DESCENDING" } }
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(url, query);
                if (!response.IsSuccessStatusCode)
                {
                    return new List<Attendance>();
                }

                var content = await response.Content.ReadAsStringAsync();
                return ParseAttendanceFromFirestore(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student attendance");
                return new List<Attendance>();
            }
        }

        public async Task<IEnumerable<Attendance>> GetClassAttendanceAsync(string classId, DateTime date)
        {
            // Implementation depends on your data structure
            return new List<Attendance>();
        }
        #endregion

        #region User Management
        public async Task DeleteUserAsync(string id)
        {
            try
            {
                // Delete from appropriate collection based on role
                var user = await GetStudentByIdAsync(id) ?? (UserProfile)await GetTeacherByIdAsync(id);

                if (user == null)
                    return;

                string collection = user.Role switch
                {
                    "student" => "students",
                    "teacher" => "teachers",
                    "hr" => "users",
                    _ => throw new InvalidOperationException($"Unknown role: {user.Role}")
                };

                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/{collection}/{id}";
                await _httpClient.DeleteAsync(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                throw;
            }
        }
        #endregion

        #region Report Generation
        public async Task<IEnumerable<Grade>> GenerateGradeReportAsync(string classId, string term, int year)
        {
            // Implementation depends on your data structure
            return new List<Grade>();
        }

        public async Task<Dictionary<string, object>> GenerateStatisticsReportAsync()
        {
            try
            {
                var studentCount = await GetStudentCountAsync();
                var teacherCount = await GetTeacherCountAsync();
                var classCount = await GetClassCountAsync();
                var hrCount = await GetHRCountAsync();

                return new Dictionary<string, object>
                {
                    ["totalStudents"] = studentCount,
                    ["totalTeachers"] = teacherCount,
                    ["totalClasses"] = classCount,
                    ["totalHR"] = hrCount,
                    ["studentTeacherRatio"] = teacherCount > 0 ? Math.Round((double)studentCount / teacherCount, 2) : 0,
                    ["reportGenerated"] = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating statistics report");
                return new Dictionary<string, object>();
            }
        }
        #endregion

        #region Helper Methods
        private Dictionary<string, object> ConvertStudentToFirestoreFields(StudentProfile student)
        {
            return new Dictionary<string, object>
            {
                ["id"] = new { stringValue = student.Id },
                ["email"] = new { stringValue = student.Email },
                ["fullName"] = new { stringValue = student.FullName },
                ["phone"] = new { stringValue = student.Phone },
                ["role"] = new { stringValue = student.Role },
                ["avatarUrl"] = new { stringValue = student.AvatarUrl },
                ["dateOfBirth"] = new { timestampValue = student.DateOfBirth.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                ["address"] = new { stringValue = student.Address },
                ["createdAt"] = new { timestampValue = student.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                ["updatedAt"] = new { timestampValue = student.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                ["isActive"] = new { booleanValue = student.IsActive },
                ["studentId"] = new { stringValue = student.StudentId },
                ["gradeLevel"] = new { stringValue = student.GradeLevel },
                ["parentName"] = new { stringValue = student.ParentName },
                ["parentEmail"] = new { stringValue = student.ParentEmail },
                ["parentPhone"] = new { stringValue = student.ParentPhone },
                ["enrolledSubjects"] = new { arrayValue = new { values = student.EnrolledSubjects.Select(s => new { stringValue = s }).ToArray() } },
                ["classId"] = new { stringValue = student.ClassId },
                ["emergencyContact"] = new { stringValue = student.EmergencyContact },
                ["gpa"] = new { doubleValue = (double)student.GPA },
                ["attendancePercentage"] = new { integerValue = student.AttendancePercentage },
                ["enrollmentDate"] = new { timestampValue = student.EnrollmentDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
            };
        }

        private Dictionary<string, object> ConvertTeacherToFirestoreFields(TeacherProfile teacher)
        {
            return new Dictionary<string, object>
            {
                ["id"] = new { stringValue = teacher.Id },
                ["email"] = new { stringValue = teacher.Email },
                ["fullName"] = new { stringValue = teacher.FullName },
                ["phone"] = new { stringValue = teacher.Phone },
                ["role"] = new { stringValue = teacher.Role },
                ["avatarUrl"] = new { stringValue = teacher.AvatarUrl },
                ["dateOfBirth"] = new { timestampValue = teacher.DateOfBirth.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                ["address"] = new { stringValue = teacher.Address },
                ["createdAt"] = new { timestampValue = teacher.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                ["updatedAt"] = new { timestampValue = teacher.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                ["isActive"] = new { booleanValue = teacher.IsActive },
                ["teacherId"] = new { stringValue = teacher.TeacherId },
                ["department"] = new { stringValue = teacher.Department },
                ["subjects"] = new { arrayValue = new { values = teacher.Subjects.Select(s => new { stringValue = s }).ToArray() } },
                ["classes"] = new { arrayValue = new { values = teacher.Classes.Select(c => new { stringValue = c }).ToArray() } },
                ["hireDate"] = new { timestampValue = teacher.HireDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                ["qualification"] = new { stringValue = teacher.Qualification },
                ["specialization"] = new { stringValue = teacher.Specialization },
                ["employeeId"] = new { stringValue = teacher.EmployeeId },
                ["isHeadOfDepartment"] = new { booleanValue = teacher.IsHeadOfDepartment }
            };
        }

        private string[] GetStudentFieldPaths()
        {
            return new[] {
                "email", "fullName", "phone", "avatarUrl", "dateOfBirth", "address", "updatedAt", "isActive",
                "studentId", "gradeLevel", "parentName", "parentEmail", "parentPhone", "enrolledSubjects",
                "classId", "emergencyContact", "gpa", "attendancePercentage"
            };
        }

        private string[] GetTeacherFieldPaths()
        {
            return new[] {
                "email", "fullName", "phone", "avatarUrl", "dateOfBirth", "address", "updatedAt", "isActive",
                "teacherId", "department", "subjects", "classes", "qualification", "specialization",
                "employeeId", "isHeadOfDepartment"
            };
        }

        private IEnumerable<StudentProfile> ParseStudentsFromFirestore(string content)
        {
            var students = new List<StudentProfile>();

            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("documents", out var documents))
                {
                    foreach (var document in documents.EnumerateArray())
                    {
                        var student = ParseStudentDocument(document);
                        if (student != null)
                        {
                            students.Add(student);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing students from Firestore");
            }

            return students;
        }

        private StudentProfile ParseStudentFromFirestore(string id, string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                return ParseStudentDocument(doc.RootElement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing student {id} from Firestore");
                return null;
            }
        }

        private StudentProfile ParseStudentDocument(JsonElement document)
        {
            try
            {
                if (!document.TryGetProperty("name", out var nameElement))
                    return null;

                var id = nameElement.GetString()?.Split('/').Last();
                if (string.IsNullOrEmpty(id))
                    return null;

                if (!document.TryGetProperty("fields", out var fields))
                    return null;

                return new StudentProfile
                {
                    Id = id,
                    Email = GetStringValue(fields, "email"),
                    FullName = GetStringValue(fields, "fullName"),
                    Phone = GetStringValue(fields, "phone"),
                    Role = GetStringValue(fields, "role", "student"),
                    AvatarUrl = GetStringValue(fields, "avatarUrl", "/images/default-avatar.png"),
                    DateOfBirth = GetDateTimeValue(fields, "dateOfBirth"),
                    Address = GetStringValue(fields, "address"),
                    CreatedAt = GetDateTimeValue(fields, "createdAt"),
                    UpdatedAt = GetDateTimeValue(fields, "updatedAt"),
                    IsActive = GetBoolValue(fields, "isActive", true),
                    StudentId = GetStringValue(fields, "studentId"),
                    GradeLevel = GetStringValue(fields, "gradeLevel", "10"),
                    ParentName = GetStringValue(fields, "parentName"),
                    ParentEmail = GetStringValue(fields, "parentEmail"),
                    ParentPhone = GetStringValue(fields, "parentPhone"),
                    EnrolledSubjects = GetStringArrayValue(fields, "enrolledSubjects"),
                    ClassId = GetStringValue(fields, "classId"),
                    EmergencyContact = GetStringValue(fields, "emergencyContact"),
                    GPA = GetDecimalValue(fields, "gpa"),
                    AttendancePercentage = GetIntValue(fields, "attendancePercentage", 100),
                    EnrollmentDate = GetDateTimeValue(fields, "enrollmentDate")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing student document");
                return null;
            }
        }

        private IEnumerable<TeacherProfile> ParseTeachersFromFirestore(string content)
        {
            var teachers = new List<TeacherProfile>();

            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("documents", out var documents))
                {
                    foreach (var document in documents.EnumerateArray())
                    {
                        var teacher = ParseTeacherDocument(document);
                        if (teacher != null)
                        {
                            teachers.Add(teacher);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing teachers from Firestore");
            }

            return teachers;
        }

        private TeacherProfile ParseTeacherFromFirestore(string id, string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                return ParseTeacherDocument(doc.RootElement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing teacher {id} from Firestore");
                return null;
            }
        }

        private TeacherProfile ParseTeacherDocument(JsonElement document)
        {
            try
            {
                if (!document.TryGetProperty("name", out var nameElement))
                    return null;

                var id = nameElement.GetString()?.Split('/').Last();
                if (string.IsNullOrEmpty(id))
                    return null;

                if (!document.TryGetProperty("fields", out var fields))
                    return null;

                return new TeacherProfile
                {
                    Id = id,
                    Email = GetStringValue(fields, "email"),
                    FullName = GetStringValue(fields, "fullName"),
                    Phone = GetStringValue(fields, "phone"),
                    Role = GetStringValue(fields, "role", "teacher"),
                    AvatarUrl = GetStringValue(fields, "avatarUrl", "/images/default-avatar.png"),
                    DateOfBirth = GetDateTimeValue(fields, "dateOfBirth"),
                    Address = GetStringValue(fields, "address"),
                    CreatedAt = GetDateTimeValue(fields, "createdAt"),
                    UpdatedAt = GetDateTimeValue(fields, "updatedAt"),
                    IsActive = GetBoolValue(fields, "isActive", true),
                    TeacherId = GetStringValue(fields, "teacherId"),
                    Department = GetStringValue(fields, "department"),
                    Subjects = GetStringArrayValue(fields, "subjects"),
                    Classes = GetStringArrayValue(fields, "classes"),
                    HireDate = GetDateTimeValue(fields, "hireDate"),
                    Qualification = GetStringValue(fields, "qualification"),
                    Specialization = GetStringValue(fields, "specialization"),
                    EmployeeId = GetStringValue(fields, "employeeId"),
                    IsHeadOfDepartment = GetBoolValue(fields, "isHeadOfDepartment", false)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing teacher document");
                return null;
            }
        }

        private IEnumerable<Class> ParseClassesFromFirestore(string content)
        {
            var classes = new List<Class>();

            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("documents", out var documents))
                {
                    foreach (var document in documents.EnumerateArray())
                    {
                        var classItem = ParseClassDocument(document);
                        if (classItem != null)
                        {
                            classes.Add(classItem);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing classes from Firestore");
            }

            return classes;
        }

        private Class ParseClassFromFirestore(string id, string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                return ParseClassDocument(doc.RootElement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing class {id} from Firestore");
                return null;
            }
        }

        private Class ParseClassDocument(JsonElement document)
        {
            try
            {
                if (!document.TryGetProperty("name", out var nameElement))
                    return null;

                var id = nameElement.GetString()?.Split('/').Last();
                if (string.IsNullOrEmpty(id))
                    return null;

                if (!document.TryGetProperty("fields", out var fields))
                    return null;

                return new Class
                {
                    Id = id,
                    Name = GetStringValue(fields, "name"),
                    GradeLevel = GetStringValue(fields, "gradeLevel"),
                    TeacherId = GetStringValue(fields, "teacherId"),
                    TeacherName = GetStringValue(fields, "teacherName"),
                    Subject = GetStringValue(fields, "subject"),
                    RoomNumber = GetStringValue(fields, "roomNumber"),
                    Schedule = GetStringValue(fields, "schedule"),
                    StudentIds = GetStringArrayValue(fields, "studentIds"),
                    CreatedAt = GetDateTimeValue(fields, "createdAt"),
                    UpdatedAt = GetDateTimeValue(fields, "updatedAt")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing class document");
                return null;
            }
        }

        private IEnumerable<Grade> ParseGradesFromFirestore(string content)
        {
            var grades = new List<Grade>();

            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                foreach (var item in root.EnumerateArray())
                {
                    if (item.TryGetProperty("document", out var document))
                    {
                        var grade = ParseGradeDocument(document);
                        if (grade != null)
                        {
                            grades.Add(grade);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing grades from Firestore");
            }

            return grades;
        }

        private Grade ParseGradeDocument(JsonElement document)
        {
            try
            {
                if (!document.TryGetProperty("name", out var nameElement))
                    return null;

                var id = nameElement.GetString()?.Split('/').Last();
                if (string.IsNullOrEmpty(id))
                    return null;

                if (!document.TryGetProperty("fields", out var fields))
                    return null;

                return new Grade
                {
                    Id = id,
                    StudentId = GetStringValue(fields, "studentId"),
                    StudentName = GetStringValue(fields, "studentName"),
                    TeacherId = GetStringValue(fields, "teacherId"),
                    Subject = GetStringValue(fields, "subject"),
                    Term = GetStringValue(fields, "term"),
                    Year = GetIntValue(fields, "year", DateTime.Now.Year),
                    Test1 = GetDecimalValue(fields, "test1"),
                    Test2 = GetDecimalValue(fields, "test2"),
                    Exam = GetDecimalValue(fields, "exam"),
                    Assignment = GetDecimalValue(fields, "assignment"),
                    TotalScore = GetDecimalValue(fields, "totalScore"),
                    GradeLetter = GetStringValue(fields, "gradeLetter"),
                    Remarks = GetStringValue(fields, "remarks"),
                    DateRecorded = GetDateTimeValue(fields, "dateRecorded"),
                    UpdatedAt = GetDateTimeValue(fields, "updatedAt")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing grade document");
                return null;
            }
        }

        private IEnumerable<Attendance> ParseAttendanceFromFirestore(string content)
        {
            var attendanceList = new List<Attendance>();

            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                foreach (var item in root.EnumerateArray())
                {
                    if (item.TryGetProperty("document", out var document))
                    {
                        var attendance = ParseAttendanceDocument(document);
                        if (attendance != null)
                        {
                            attendanceList.Add(attendance);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing attendance from Firestore");
            }

            return attendanceList;
        }

        private Attendance ParseAttendanceDocument(JsonElement document)
        {
            try
            {
                if (!document.TryGetProperty("name", out var nameElement))
                    return null;

                var id = nameElement.GetString()?.Split('/').Last();
                if (string.IsNullOrEmpty(id))
                    return null;

                if (!document.TryGetProperty("fields", out var fields))
                    return null;

                return new Attendance
                {
                    Id = id,
                    StudentId = GetStringValue(fields, "studentId"),
                    StudentName = GetStringValue(fields, "studentName"),
                    ClassId = GetStringValue(fields, "classId"),
                    ClassName = GetStringValue(fields, "className"),
                    Date = GetDateTimeValue(fields, "date"),
                    Status = GetStringValue(fields, "status", "Present"),
                    Remarks = GetStringValue(fields, "remarks"),
                    RecordedBy = GetStringValue(fields, "recordedBy"),
                    RecordedAt = GetDateTimeValue(fields, "recordedAt")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing attendance document");
                return null;
            }
        }

        private string GetStringValue(JsonElement fields, string fieldName, string defaultValue = "")
        {
            try
            {
                if (fields.TryGetProperty(fieldName, out var field) &&
                    field.TryGetProperty("stringValue", out var stringValue))
                {
                    return stringValue.GetString() ?? defaultValue;
                }
            }
            catch { }
            return defaultValue;
        }

        private DateTime GetDateTimeValue(JsonElement fields, string fieldName)
        {
            try
            {
                if (fields.TryGetProperty(fieldName, out var field) &&
                    field.TryGetProperty("timestampValue", out var timestampValue))
                {
                    var timestamp = timestampValue.GetString();
                    if (DateTime.TryParse(timestamp, out var result))
                    {
                        return result;
                    }
                }
            }
            catch { }
            return DateTime.UtcNow;
        }

        private bool GetBoolValue(JsonElement fields, string fieldName, bool defaultValue = false)
        {
            try
            {
                if (fields.TryGetProperty(fieldName, out var field) &&
                    field.TryGetProperty("booleanValue", out var boolValue))
                {
                    return boolValue.GetBoolean();
                }
            }
            catch { }
            return defaultValue;
        }

        private int GetIntValue(JsonElement fields, string fieldName, int defaultValue = 0)
        {
            try
            {
                if (fields.TryGetProperty(fieldName, out var field) &&
                    field.TryGetProperty("integerValue", out var intValue))
                {
                    if (int.TryParse(intValue.GetString(), out var result))
                    {
                        return result;
                    }
                }
            }
            catch { }
            return defaultValue;
        }

        private decimal GetDecimalValue(JsonElement fields, string fieldName, decimal defaultValue = 0)
        {
            try
            {
                if (fields.TryGetProperty(fieldName, out var field))
                {
                    if (field.TryGetProperty("doubleValue", out var doubleValue))
                    {
                        if (double.TryParse(doubleValue.GetString(), out var result))
                        {
                            return (decimal)result;
                        }
                    }
                    else if (field.TryGetProperty("integerValue", out var intValue))
                    {
                        if (int.TryParse(intValue.GetString(), out var result))
                        {
                            return result;
                        }
                    }
                }
            }
            catch { }
            return defaultValue;
        }

        private List<string> GetStringArrayValue(JsonElement fields, string fieldName)
        {
            var result = new List<string>();

            try
            {
                if (fields.TryGetProperty(fieldName, out var field) &&
                    field.TryGetProperty("arrayValue", out var arrayValue) &&
                    arrayValue.TryGetProperty("values", out var values))
                {
                    foreach (var value in values.EnumerateArray())
                    {
                        if (value.TryGetProperty("stringValue", out var stringValue))
                        {
                            result.Add(stringValue.GetString() ?? string.Empty);
                        }
                    }
                }
            }
            catch { }

            return result;
        }
        #endregion
    }
}