using System.Text.Json;
using HighSchoolPortal.Interfaces;
using HighSchoolPortal.Models;

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

        #region Helper Methods for Parsing
        private StudentProfile ParseStudentDocument(JsonElement document)
        {
            try
            {
                if (document.TryGetProperty("fields", out var fields) &&
                    document.TryGetProperty("name", out var name))
                {
                    string documentPath = name.GetString() ?? "";
                    string id = documentPath.Split('/').LastOrDefault() ?? Guid.NewGuid().ToString();

                    if (fields.ValueKind == JsonValueKind.Object)
                    {
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
                            GradeLevel = GetStringValue(fields, "gradeLevel"),
                            ParentName = GetStringValue(fields, "parentName"),
                            ParentEmail = GetStringValue(fields, "parentEmail"),
                            ParentPhone = GetStringValue(fields, "parentPhone"),
                            ClassId = GetStringValue(fields, "classId"),
                            EmergencyContact = GetStringValue(fields, "emergencyContact"),
                            GPA = GetDecimalValue(fields, "gpa"),
                            AttendancePercentage = GetIntValue(fields, "attendancePercentage", 100),
                            EnrollmentDate = GetDateTimeValue(fields, "enrollmentDate"),
                            EnrolledSubjects = GetStringArrayValue(fields, "enrolledSubjects")
                        };
                    }
                }

                _logger.LogWarning($"Invalid document structure: {document}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing student document. Document: {document}");
                return null;
            }
        }

        private TeacherProfile ParseTeacherDocument(JsonElement document)
        {
            try
            {
                if (document.TryGetProperty("fields", out var fields) &&
                    document.TryGetProperty("name", out var name))
                {
                    string documentPath = name.GetString() ?? "";
                    string id = documentPath.Split('/').LastOrDefault() ?? Guid.NewGuid().ToString();

                    if (fields.ValueKind == JsonValueKind.Object)
                    {
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
                            Qualification = GetStringValue(fields, "qualification"),
                            Specialization = GetStringValue(fields, "specialization"),
                            EmployeeId = GetStringValue(fields, "employeeId"),
                            HireDate = GetDateTimeValue(fields, "hireDate"),
                            IsHeadOfDepartment = GetBoolValue(fields, "isHeadOfDepartment", false),
                            Subjects = GetStringArrayValue(fields, "subjects"),
                            Classes = GetStringArrayValue(fields, "classes")
                        };
                    }
                }

                _logger.LogWarning($"Invalid teacher document structure: {document}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing teacher document");
                return null;
            }
        }

        private Class ParseClassDocument(JsonElement document)
        {
            try
            {
                if (document.TryGetProperty("fields", out var fields) &&
                    document.TryGetProperty("name", out var name))
                {
                    string documentPath = name.GetString() ?? "";
                    string id = documentPath.Split('/').LastOrDefault() ?? Guid.NewGuid().ToString();

                    return new Class
                    {
                        Id = id,
                        Name = GetStringValue(fields, "name"),
                        Code = GetStringValue(fields, "code"),
                        GradeLevel = GetStringValue(fields, "gradeLevel"),
                        TeacherId = GetStringValue(fields, "teacherId"),
                        TeacherName = GetStringValue(fields, "teacherName"),
                        Subject = GetStringValue(fields, "subject"),
                        AcademicYear = GetStringValue(fields, "academicYear", DateTime.Now.Year.ToString()),
                        Term = GetStringValue(fields, "term", "First"),
                        Schedule = GetStringValue(fields, "schedule"),
                        RoomNumber = GetStringValue(fields, "roomNumber"),
                         MaxCapacity = GetIntValue(fields, "maxCapacity", 30),
                        IsActive = GetBoolValue(fields, "isActive", true),
                        CreatedAt = GetDateTimeValue(fields, "createdAt"),
                        UpdatedAt = GetDateTimeValue(fields, "updatedAt")
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing class document");
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

        private decimal GetDecimalValue(JsonElement fields, string fieldName)
        {
            try
            {
                if (fields.TryGetProperty(fieldName, out var field))
                {
                    if (field.TryGetProperty("doubleValue", out var doubleValue))
                    {
                        return (decimal)doubleValue.GetDouble();
                    }
                    else if (field.TryGetProperty("integerValue", out var intValue))
                    {
                        return intValue.GetInt64();
                    }
                }
            }
            catch { }
            return 0;
        }

        private int GetIntValue(JsonElement fields, string fieldName, int defaultValue = 0)
        {
            try
            {
                if (fields.TryGetProperty(fieldName, out var field))
                {
                    if (field.TryGetProperty("integerValue", out var intValue))
                    {
                        return intValue.GetInt32();
                    }
                    else if (field.TryGetProperty("doubleValue", out var doubleValue))
                    {
                        return (int)doubleValue.GetDouble();
                    }
                }
            }
            catch { }
            return defaultValue;
        }

        private List<string> GetStringArrayValue(JsonElement fields, string fieldName)
        {
            var list = new List<string>();
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
                            list.Add(stringValue.GetString() ?? "");
                        }
                    }
                }
            }
            catch { }
            return list;
        }
        #endregion

        #region Student Management
        public async Task<IEnumerable<StudentProfile>> GetAllStudentsAsync()
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/students";
                _logger.LogInformation($"Fetching students from: {url}");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                var students = new List<StudentProfile>();

                if (root.TryGetProperty("documents", out var documents))
                {
                    _logger.LogInformation($"Found {documents.GetArrayLength()} student documents");

                    foreach (var document in documents.EnumerateArray())
                    {
                        try
                        {
                            var student = ParseStudentDocument(document);
                            if (student != null)
                            {
                                students.Add(student);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to parse student document: {document}");
                        }
                    }
                }

                _logger.LogInformation($"Successfully parsed {students.Count} students");
                return students;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all students");
                return new List<StudentProfile>();
            }
        }

        public async Task<StudentProfile> GetStudentByIdAsync(string id)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/students/{id}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Student not found with ID: {id}, Status: {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("fields", out var fields) &&
                    root.TryGetProperty("name", out var name))
                {
                    string documentPath = name.GetString() ?? "";
                    string documentId = documentPath.Split('/').LastOrDefault() ?? id;

                    return new StudentProfile
                    {
                        Id = documentId,
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
                        GradeLevel = GetStringValue(fields, "gradeLevel"),
                        ParentName = GetStringValue(fields, "parentName"),
                        ParentEmail = GetStringValue(fields, "parentEmail"),
                        ParentPhone = GetStringValue(fields, "parentPhone"),
                        ClassId = GetStringValue(fields, "classId"),
                        EmergencyContact = GetStringValue(fields, "emergencyContact"),
                        GPA = GetDecimalValue(fields, "gpa"),
                        AttendancePercentage = GetIntValue(fields, "attendancePercentage", 100),
                        EnrollmentDate = GetDateTimeValue(fields, "enrollmentDate"),
                        EnrolledSubjects = GetStringArrayValue(fields, "enrolledSubjects")
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting student by ID: {id}");
                return null;
            }
        }

        public async Task<StudentProfile> AddStudentAsync(StudentProfile student)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/students";

                var documentId = string.IsNullOrEmpty(student.Id) ? Guid.NewGuid().ToString() : student.Id;
                student.Id = documentId;

                var firestoreDoc = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["email"] = new { stringValue = student.Email },
                        ["fullName"] = new { stringValue = student.FullName },
                        ["phone"] = new { stringValue = student.Phone ?? "" },
                        ["role"] = new { stringValue = "student" },
                        ["avatarUrl"] = new { stringValue = student.AvatarUrl ?? "/images/default-avatar.png" },
                        ["dateOfBirth"] = new { timestampValue = student.DateOfBirth.ToString("o") },
                        ["address"] = new { stringValue = student.Address ?? "" },
                        ["createdAt"] = new { timestampValue = DateTime.UtcNow.ToString("o") },
                        ["updatedAt"] = new { timestampValue = DateTime.UtcNow.ToString("o") },
                        ["isActive"] = new { booleanValue = true },
                        ["studentId"] = new { stringValue = student.StudentId ?? documentId },
                        ["gradeLevel"] = new { stringValue = student.GradeLevel ?? "10" },
                        ["parentName"] = new { stringValue = student.ParentName ?? "" },
                        ["parentEmail"] = new { stringValue = student.ParentEmail ?? "" },
                        ["parentPhone"] = new { stringValue = student.ParentPhone ?? "" },
                        ["classId"] = new { stringValue = student.ClassId ?? "" },
                        ["emergencyContact"] = new { stringValue = student.EmergencyContact ?? "" },
                        ["gpa"] = new { doubleValue = (double)student.GPA },
                        ["attendancePercentage"] = new { integerValue = student.AttendancePercentage },
                        ["enrollmentDate"] = new { timestampValue = student.EnrollmentDate.ToString("o") },
                        ["enrolledSubjects"] = new
                        {
                            arrayValue = new
                            {
                                values = student.EnrolledSubjects?.Select(s => new { stringValue = s }).ToArray() ?? Array.Empty<object>()
                            }
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(firestoreDoc), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{url}?documentId={documentId}", content);
                response.EnsureSuccessStatusCode();

                return student;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding student: {student.FullName}");
                throw;
            }
        }

        public async Task UpdateStudentAsync(StudentProfile student)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/students/{student.Id}";

                var updateMask = new List<string>
                {
                    "email", "fullName", "phone", "avatarUrl", "dateOfBirth", "address", "updatedAt",
                    "isActive", "gradeLevel", "parentName", "parentEmail", "parentPhone", "classId",
                    "emergencyContact", "gpa", "attendancePercentage", "enrolledSubjects"
                };

                var firestoreDoc = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["email"] = new { stringValue = student.Email },
                        ["fullName"] = new { stringValue = student.FullName },
                        ["phone"] = new { stringValue = student.Phone ?? "" },
                        ["avatarUrl"] = new { stringValue = student.AvatarUrl ?? "/images/default-avatar.png" },
                        ["dateOfBirth"] = new { timestampValue = student.DateOfBirth.ToString("o") },
                        ["address"] = new { stringValue = student.Address ?? "" },
                        ["updatedAt"] = new { timestampValue = DateTime.UtcNow.ToString("o") },
                        ["isActive"] = new { booleanValue = student.IsActive },
                        ["gradeLevel"] = new { stringValue = student.GradeLevel ?? "10" },
                        ["parentName"] = new { stringValue = student.ParentName ?? "" },
                        ["parentEmail"] = new { stringValue = student.ParentEmail ?? "" },
                        ["parentPhone"] = new { stringValue = student.ParentPhone ?? "" },
                        ["classId"] = new { stringValue = student.ClassId ?? "" },
                        ["emergencyContact"] = new { stringValue = student.EmergencyContact ?? "" },
                        ["gpa"] = new { doubleValue = (double)student.GPA },
                        ["attendancePercentage"] = new { integerValue = student.AttendancePercentage },
                        ["enrolledSubjects"] = new
                        {
                            arrayValue = new
                            {
                                values = student.EnrolledSubjects?.Select(s => new { stringValue = s }).ToArray() ?? Array.Empty<object>()
                            }
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(firestoreDoc), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync($"{url}?updateMask.fieldPaths={string.Join("&updateMask.fieldPaths=", updateMask)}", content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating student: {student.FullName}");
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
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var teachers = ParseTeachersFromFirestore(content);
                return teachers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all teachers");
                return new List<TeacherProfile>();
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

        public async Task<TeacherProfile> GetTeacherByIdAsync(string id)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/teachers/{id}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("fields", out var fields) &&
                    root.TryGetProperty("name", out var name))
                {
                    string documentPath = name.GetString() ?? "";
                    string documentId = documentPath.Split('/').LastOrDefault() ?? id;

                    return new TeacherProfile
                    {
                        Id = documentId,
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
                        Qualification = GetStringValue(fields, "qualification"),
                        Specialization = GetStringValue(fields, "specialization"),
                        EmployeeId = GetStringValue(fields, "employeeId"),
                        HireDate = GetDateTimeValue(fields, "hireDate"),
                        IsHeadOfDepartment = GetBoolValue(fields, "isHeadOfDepartment", false),
                        Subjects = GetStringArrayValue(fields, "subjects"),
                        Classes = GetStringArrayValue(fields, "classes")
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting teacher by ID: {id}");
                return null;
            }
        }

        public async Task<TeacherProfile> AddTeacherAsync(TeacherProfile teacher)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/teachers";

                var documentId = string.IsNullOrEmpty(teacher.Id) ? Guid.NewGuid().ToString() : teacher.Id;
                teacher.Id = documentId;

                var firestoreDoc = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["email"] = new { stringValue = teacher.Email },
                        ["fullName"] = new { stringValue = teacher.FullName },
                        ["phone"] = new { stringValue = teacher.Phone ?? "" },
                        ["role"] = new { stringValue = "teacher" },
                        ["avatarUrl"] = new { stringValue = teacher.AvatarUrl ?? "/images/default-avatar.png" },
                        ["dateOfBirth"] = new { timestampValue = teacher.DateOfBirth.ToString("o") },
                        ["address"] = new { stringValue = teacher.Address ?? "" },
                        ["createdAt"] = new { timestampValue = DateTime.UtcNow.ToString("o") },
                        ["updatedAt"] = new { timestampValue = DateTime.UtcNow.ToString("o") },
                        ["isActive"] = new { booleanValue = true },
                        ["teacherId"] = new { stringValue = teacher.TeacherId ?? documentId },
                        ["department"] = new { stringValue = teacher.Department ?? "" },
                        ["qualification"] = new { stringValue = teacher.Qualification ?? "" },
                        ["specialization"] = new { stringValue = teacher.Specialization ?? "" },
                        ["employeeId"] = new { stringValue = teacher.EmployeeId ?? "" },
                        ["hireDate"] = new { timestampValue = teacher.HireDate.ToString("o") },
                        ["isHeadOfDepartment"] = new { booleanValue = teacher.IsHeadOfDepartment },
                        ["subjects"] = new
                        {
                            arrayValue = new
                            {
                                values = teacher.Subjects?.Select(s => new { stringValue = s }).ToArray() ?? Array.Empty<object>()
                            }
                        },
                        ["classes"] = new
                        {
                            arrayValue = new
                            {
                                values = teacher.Classes?.Select(c => new { stringValue = c }).ToArray() ?? Array.Empty<object>()
                            }
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(firestoreDoc), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{url}?documentId={documentId}", content);
                response.EnsureSuccessStatusCode();

                return teacher;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding teacher: {teacher.FullName}");
                throw;
            }
        }

        public async Task UpdateTeacherAsync(TeacherProfile teacher)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/teachers/{teacher.Id}";

                var updateMask = new List<string>
                {
                    "email", "fullName", "phone", "avatarUrl", "dateOfBirth", "address", "updatedAt",
                    "isActive", "department", "qualification", "specialization", "employeeId",
                    "isHeadOfDepartment", "subjects", "classes"
                };

                var firestoreDoc = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["email"] = new { stringValue = teacher.Email },
                        ["fullName"] = new { stringValue = teacher.FullName },
                        ["phone"] = new { stringValue = teacher.Phone ?? "" },
                        ["avatarUrl"] = new { stringValue = teacher.AvatarUrl ?? "/images/default-avatar.png" },
                        ["dateOfBirth"] = new { timestampValue = teacher.DateOfBirth.ToString("o") },
                        ["address"] = new { stringValue = teacher.Address ?? "" },
                        ["updatedAt"] = new { timestampValue = DateTime.UtcNow.ToString("o") },
                        ["isActive"] = new { booleanValue = teacher.IsActive },
                        ["department"] = new { stringValue = teacher.Department ?? "" },
                        ["qualification"] = new { stringValue = teacher.Qualification ?? "" },
                        ["specialization"] = new { stringValue = teacher.Specialization ?? "" },
                        ["employeeId"] = new { stringValue = teacher.EmployeeId ?? "" },
                        ["isHeadOfDepartment"] = new { booleanValue = teacher.IsHeadOfDepartment },
                        ["subjects"] = new
                        {
                            arrayValue = new
                            {
                                values = teacher.Subjects?.Select(s => new { stringValue = s }).ToArray() ?? Array.Empty<object>()
                            }
                        },
                        ["classes"] = new
                        {
                            arrayValue = new
                            {
                                values = teacher.Classes?.Select(c => new { stringValue = c }).ToArray() ?? Array.Empty<object>()
                            }
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(firestoreDoc), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync($"{url}?updateMask.fieldPaths={string.Join("&updateMask.fieldPaths=", updateMask)}", content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating teacher: {teacher.FullName}");
                throw;
            }
        }

        // NEW: HR can assign subjects and classes to teachers
        public async Task<TeacherAssignment> AssignTeacherToClassesAsync(string teacherId, List<string> classIds, List<string> subjects)
        {
            try
            {
                var teacher = await GetTeacherByIdAsync(teacherId);
                if (teacher == null)
                    throw new Exception($"Teacher {teacherId} not found");

                // Update teacher with assigned classes and subjects
                teacher.Classes = classIds;
                teacher.Subjects = subjects;
                teacher.UpdatedAt = DateTime.UtcNow;

                await UpdateTeacherAsync(teacher);

                // Create assignment record
                var assignment = new TeacherAssignment
                {
                    Id = Guid.NewGuid().ToString(),
                    TeacherId = teacherId,
                    TeacherName = teacher.FullName,
                    ClassIds = classIds,
                    Subjects = subjects,
                    AssignedDate = DateTime.UtcNow,
                    AssignedBy = "HR",
                    IsActive = true
                };

                await SaveTeacherAssignmentAsync(assignment);
                return assignment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error assigning teacher {teacherId} to classes");
                throw;
            }
        }

        private async Task SaveTeacherAssignmentAsync(TeacherAssignment assignment)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/teacher_assignments/{assignment.Id}";

                var firestoreDoc = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["teacherId"] = new { stringValue = assignment.TeacherId },
                        ["teacherName"] = new { stringValue = assignment.TeacherName },
                        ["classIds"] = new
                        {
                            arrayValue = new
                            {
                                values = assignment.ClassIds.Select(id => new { stringValue = id }).ToArray()
                            }
                        },
                        ["subjects"] = new
                        {
                            arrayValue = new
                            {
                                values = assignment.Subjects.Select(s => new { stringValue = s }).ToArray()
                            }
                        },
                        ["assignedDate"] = new { timestampValue = assignment.AssignedDate.ToString("o") },
                        ["assignedBy"] = new { stringValue = assignment.AssignedBy },
                        ["isActive"] = new { booleanValue = assignment.IsActive }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(firestoreDoc), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync($"{url}?updateMask.fieldPaths=teacherId,teacherName,classIds,subjects,assignedDate,assignedBy,isActive", content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving teacher assignment");
                throw;
            }
        }
        #endregion

        #region Class Management (NEW Methods)
        public async Task<Class> AddClassAsync(Class classItem)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/classes";

                var documentId = string.IsNullOrEmpty(classItem.Id) ? Guid.NewGuid().ToString() : classItem.Id;
                classItem.Id = documentId;

                var firestoreDoc = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["name"] = new { stringValue = classItem.Name },
                        ["code"] = new { stringValue = classItem.Code },
                        ["gradeLevel"] = new { stringValue = classItem.GradeLevel },
                        ["teacherId"] = new { stringValue = classItem.TeacherId ?? "" },
                        ["teacherName"] = new { stringValue = classItem.TeacherName ?? "" },
                        ["subject"] = new { stringValue = classItem.Subject ?? "" },
                        ["academicYear"] = new { stringValue = classItem.AcademicYear },
                        ["term"] = new { stringValue = classItem.Term },
                        ["schedule"] = new { stringValue = classItem.Schedule ?? "" },
                        ["roomNumber"] = new { stringValue = classItem.RoomNumber ?? "" },
                        ["studentCount"] = new { integerValue = classItem.StudentCount },
                        ["maxCapacity"] = new { integerValue = classItem.MaxCapacity },
                        ["isActive"] = new { booleanValue = classItem.IsActive },
                        ["createdAt"] = new { timestampValue = DateTime.UtcNow.ToString("o") },
                        ["updatedAt"] = new { timestampValue = DateTime.UtcNow.ToString("o") }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(firestoreDoc), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{url}?documentId={documentId}", content);
                response.EnsureSuccessStatusCode();

                return classItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding class: {classItem.Name}");
                throw;
            }
        }

        public async Task<IEnumerable<Class>> GetAllClassesAsync()
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/classes";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                var classes = new List<Class>();

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

                return classes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all classes");
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
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                return ParseClassDocument(root);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting class by ID: {id}");
                return null;
            }
        }

        public async Task UpdateClassAsync(Class classItem)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/classes/{classItem.Id}";

                var updateMask = new List<string>
                {
                    "name", "teacherId", "teacherName", "subject", "schedule", "roomNumber",
                    "studentCount", "isActive", "updatedAt"
                };

                var firestoreDoc = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["name"] = new { stringValue = classItem.Name },
                        ["teacherId"] = new { stringValue = classItem.TeacherId ?? "" },
                        ["teacherName"] = new { stringValue = classItem.TeacherName ?? "" },
                        ["subject"] = new { stringValue = classItem.Subject ?? "" },
                        ["schedule"] = new { stringValue = classItem.Schedule ?? "" },
                        ["roomNumber"] = new { stringValue = classItem.RoomNumber ?? "" },
                        ["studentCount"] = new { integerValue = classItem.StudentCount },
                        ["isActive"] = new { booleanValue = classItem.IsActive },
                        ["updatedAt"] = new { timestampValue = DateTime.UtcNow.ToString("o") }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(firestoreDoc), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync($"{url}?updateMask.fieldPaths={string.Join("&updateMask.fieldPaths=", updateMask)}", content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating class: {classItem.Name}");
                throw;
            }
        }
        #endregion

        #region Teacher-Student Relationship
        public async Task<List<StudentProfile>> GetStudentsByTeacherAsync(string teacherId)
        {
            try
            {
                // Get teacher's classes
                var teacher = await GetTeacherByIdAsync(teacherId);
                if (teacher?.Classes == null || !teacher.Classes.Any())
                    return new List<StudentProfile>();

                // Get all students and filter by teacher's classes
                var allStudents = await GetAllStudentsAsync();
                return allStudents
                    .Where(s => teacher.Classes.Contains(s.ClassId))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting students for teacher {teacherId}");
                return new List<StudentProfile>();
            }
        }

        public async Task<decimal> GetTeacherPassRateAsync(string teacherId)
        {
            try
            {
                var teacherStudents = await GetStudentsByTeacherAsync(teacherId);
                if (!teacherStudents.Any())
                    return 0;

                var passingStudents = teacherStudents.Count(s => s.GPA >= 2.0m);
                return Math.Round((decimal)passingStudents / teacherStudents.Count * 100, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating pass rate for teacher {teacherId}");
                return 0;
            }
        }

        public async Task<Grade> AddWeightedGradeAsync(WeightedGrade grade)
        {
            try
            {
                // Convert WeightedGrade to regular Grade for compatibility
                var regularGrade = new Grade
                {
                    Id = grade.Id,
                    StudentId = grade.StudentId,
                    Subject = grade.Subject,
                    Term = grade.Term,
                    Year = grade.Year,
                    Test1 = grade.Test1,
                    Test2 = grade.Test2,
                    Exam = grade.FinalExam,
                    Assignment = grade.Project, // Using Project as Assignment for compatibility
                    TotalScore = grade.TotalScore,
                    GradeLetter = grade.GradeLetter,
                    Remarks = grade.Remarks,
                    DateRecorded = grade.DateRecorded,
                    TeacherId = grade.TeacherId
                };

                regularGrade.CalculateTotal(); // Ensure total is calculated
                return await AddGradeAsync(regularGrade);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding weighted grade for student {grade.StudentId}");
                return null;
            }
        }

        public async Task<List<StudentProfile>> GetStudentsByClassIdAsync(string classId)
        {
            try
            {
                var allStudents = await GetAllStudentsAsync();
                return allStudents
                    .Where(s => s.ClassId == classId)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting students for class: {classId}");
                return new List<StudentProfile>();
            }
        }
        #endregion

        #region Grade Management
        public async Task<Grade> AddGradeAsync(Grade grade)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/grades";

                var documentId = string.IsNullOrEmpty(grade.Id) ? Guid.NewGuid().ToString() : grade.Id;
                grade.Id = documentId;

                var firestoreDoc = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["studentId"] = new { stringValue = grade.StudentId },
                        ["studentName"] = new { stringValue = grade.StudentName ?? "" },
                        ["teacherId"] = new { stringValue = grade.TeacherId ?? "" },
                        ["subject"] = new { stringValue = grade.Subject ?? "" },
                        ["term"] = new { stringValue = grade.Term ?? "First" },
                        ["year"] = new { integerValue = grade.Year },
                        ["test1"] = new { doubleValue = (double)grade.Test1 },
                        ["test2"] = new { doubleValue = (double)grade.Test2 },
                        ["exam"] = new { doubleValue = (double)grade.Exam },
                        ["assignment"] = new { doubleValue = (double)grade.Assignment },
                        ["totalScore"] = new { doubleValue = (double)grade.TotalScore },
                        ["gradeLetter"] = new { stringValue = grade.GradeLetter ?? "F" },
                        ["remarks"] = new { stringValue = grade.Remarks ?? "" },
                        ["dateRecorded"] = new { timestampValue = grade.DateRecorded.ToString("o") },
                        ["updatedAt"] = new { timestampValue = DateTime.UtcNow.ToString("o") }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(firestoreDoc), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{url}?documentId={documentId}", content);
                response.EnsureSuccessStatusCode();

                return grade;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding grade for student: {grade.StudentName}");
                throw;
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
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(query), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return ParseGradesFromFirestore(responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting grades for student: {studentId}");
                return new List<Grade>();
            }
        }

        public async Task<IEnumerable<Grade>> GetClassGradesAsync(string classId, string subject)
        {
            try
            {
                // Get all students in the class
                var classStudents = await GetStudentsByClassIdAsync(classId);
                var allGrades = new List<Grade>();

                foreach (var student in classStudents)
                {
                    var studentGrades = await GetStudentGradesAsync(student.Id);
                    var filteredGrades = studentGrades.Where(g =>
                        string.IsNullOrEmpty(subject) || g.Subject == subject);
                    allGrades.AddRange(filteredGrades);
                }

                return allGrades;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting class grades for class: {classId}, subject: {subject}");
                return new List<Grade>();
            }
        }

        public async Task<Grade> UpdateGradeAsync(Grade grade)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/grades/{grade.Id}";

                var updateMask = new List<string>
                {
                    "test1", "test2", "exam", "assignment", "totalScore", "gradeLetter",
                    "remarks", "updatedAt"
                };

                var firestoreDoc = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["test1"] = new { doubleValue = (double)grade.Test1 },
                        ["test2"] = new { doubleValue = (double)grade.Test2 },
                        ["exam"] = new { doubleValue = (double)grade.Exam },
                        ["assignment"] = new { doubleValue = (double)grade.Assignment },
                        ["totalScore"] = new { doubleValue = (double)grade.TotalScore },
                        ["gradeLetter"] = new { stringValue = grade.GradeLetter ?? "F" },
                        ["remarks"] = new { stringValue = grade.Remarks ?? "" },
                        ["updatedAt"] = new { timestampValue = DateTime.UtcNow.ToString("o") }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(firestoreDoc), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync($"{url}?updateMask.fieldPaths={string.Join("&updateMask.fieldPaths=", updateMask)}", content);
                response.EnsureSuccessStatusCode();

                return grade;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating grade for student: {grade.StudentName}");
                throw;
            }
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
                _logger.LogError(ex, $"Error deleting grade: {gradeId}");
                throw;
            }
        }

        private IEnumerable<Grade> ParseGradesFromFirestore(string content)
        {
            var grades = new List<Grade>();

            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                foreach (var element in root.EnumerateArray())
                {
                    if (element.TryGetProperty("document", out var document))
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
                if (document.TryGetProperty("fields", out var fields) &&
                    document.TryGetProperty("name", out var name))
                {
                    string id = name.GetProperty("name").GetString()?.Split('/').Last() ?? Guid.NewGuid().ToString();

                    return new Grade
                    {
                        Id = id,
                        StudentId = GetStringValue(fields, "studentId"),
                        StudentName = GetStringValue(fields, "studentName"),
                        TeacherId = GetStringValue(fields, "teacherId"),
                        Subject = GetStringValue(fields, "subject"),
                        Term = GetStringValue(fields, "term", "First"),
                        Year = GetIntValue(fields, "year", DateTime.Now.Year),
                        Test1 = GetDecimalValue(fields, "test1"),
                        Test2 = GetDecimalValue(fields, "test2"),
                        Exam = GetDecimalValue(fields, "exam"),
                        Assignment = GetDecimalValue(fields, "assignment"),
                        TotalScore = GetDecimalValue(fields, "totalScore"),
                        GradeLetter = GetStringValue(fields, "gradeLetter", "F"),
                        Remarks = GetStringValue(fields, "remarks"),
                        DateRecorded = GetDateTimeValue(fields, "dateRecorded"),
                        UpdatedAt = GetDateTimeValue(fields, "updatedAt")
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing grade document");
                return null;
            }
        }
        #endregion

        #region Attendance Management
        public async Task<Attendance> RecordAttendanceAsync(Attendance attendance)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/attendance";

                var documentId = string.IsNullOrEmpty(attendance.Id) ? Guid.NewGuid().ToString() : attendance.Id;
                attendance.Id = documentId;

                var firestoreDoc = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["studentId"] = new { stringValue = attendance.StudentId },
                        ["studentName"] = new { stringValue = attendance.StudentName ?? "" },
                        ["classId"] = new { stringValue = attendance.ClassId ?? "" },
                        ["className"] = new { stringValue = attendance.ClassName ?? "" },
                        ["date"] = new { timestampValue = attendance.Date.ToString("o") },
                        ["status"] = new { stringValue = attendance.Status ?? "Present" },
                        ["remarks"] = new { stringValue = attendance.Remarks ?? "" },
                        ["recordedBy"] = new { stringValue = attendance.RecordedBy ?? "" },
                        ["recordedAt"] = new { timestampValue = DateTime.UtcNow.ToString("o") }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(firestoreDoc), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{url}?documentId={documentId}", content);
                response.EnsureSuccessStatusCode();

                return attendance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error recording attendance for student: {attendance.StudentName}");
                throw;
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
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(query), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return ParseAttendanceFromFirestore(responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting attendance for student: {studentId}");
                return new List<Attendance>();
            }
        }

        public async Task<IEnumerable<Attendance>> GetClassAttendanceAsync(string classId, DateTime date)
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
                            compositeFilter = new
                            {
                                op = "AND",
                                filters = new object[]
                                {
                                    new
                                    {
                                        fieldFilter = new
                                        {
                                            field = new { fieldPath = "classId" },
                                            op = "EQUAL",
                                            value = new { stringValue = classId }
                                        }
                                    },
                                    new
                                    {
                                        fieldFilter = new
                                        {
                                            field = new { fieldPath = "date" },
                                            op = "EQUAL",
                                            value = new { timestampValue = date.ToString("o") }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(query), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return ParseAttendanceFromFirestore(responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting class attendance for class: {classId} on date: {date}");
                return new List<Attendance>();
            }
        }

        private IEnumerable<Attendance> ParseAttendanceFromFirestore(string content)
        {
            var attendanceRecords = new List<Attendance>();

            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                foreach (var element in root.EnumerateArray())
                {
                    if (element.TryGetProperty("document", out var document))
                    {
                        var attendance = ParseAttendanceDocument(document);
                        if (attendance != null)
                        {
                            attendanceRecords.Add(attendance);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing attendance from Firestore");
            }

            return attendanceRecords;
        }

        private Attendance ParseAttendanceDocument(JsonElement document)
        {
            try
            {
                if (document.TryGetProperty("fields", out var fields) &&
                    document.TryGetProperty("name", out var name))
                {
                    string id = name.GetProperty("name").GetString()?.Split('/').Last() ?? Guid.NewGuid().ToString();

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
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing attendance document");
                return null;
            }
        }
        #endregion

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
                // Query HR collection in Firestore
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/hr";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("documents", out var documents))
                    {
                        return documents.GetArrayLength();
                    }
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

        #region User Management
        public async Task DeleteUserAsync(string id)
        {
            try
            {
                // Try to delete from students
                var studentUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/students/{id}";
                var studentResponse = await _httpClient.DeleteAsync(studentUrl);

                if (studentResponse.IsSuccessStatusCode)
                {
                    return;
                }

                // Try to delete from teachers
                var teacherUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/teachers/{id}";
                var teacherResponse = await _httpClient.DeleteAsync(teacherUrl);

                if (teacherResponse.IsSuccessStatusCode)
                {
                    return;
                }

                // Try to delete from HR
                var hrUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/hr/{id}";
                await _httpClient.DeleteAsync(hrUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user: {id}");
                throw;
            }
        }
        #endregion

        #region Reports
        public async Task<IEnumerable<Grade>> GenerateGradeReportAsync(string classId, string term, int year)
        {
            try
            {
                var classStudents = await GetStudentsByClassIdAsync(classId);
                var allGrades = new List<Grade>();

                foreach (var student in classStudents)
                {
                    var studentGrades = await GetStudentGradesAsync(student.Id);
                    var termGrades = studentGrades
                        .Where(g => g.Term == term && g.Year == year)
                        .ToList();
                    allGrades.AddRange(termGrades);
                }

                return allGrades;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating grade report for class: {classId}, term: {term}, year: {year}");
                return new List<Grade>();
            }
        }

        public async Task<Dictionary<string, object>> GenerateStatisticsReportAsync()
        {
            var statistics = new Dictionary<string, object>();

            try
            {
                var studentCount = await GetStudentCountAsync();
                var teacherCount = await GetTeacherCountAsync();
                var classCount = await GetClassCountAsync();
                var hrCount = await GetHRCountAsync();

                statistics["studentCount"] = studentCount;
                statistics["teacherCount"] = teacherCount;
                statistics["classCount"] = classCount;
                statistics["hrCount"] = hrCount;
                statistics["totalUsers"] = studentCount + teacherCount + hrCount;
                statistics["reportGeneratedAt"] = DateTime.UtcNow;
                statistics["reportPeriod"] = "Last 30 days";
                statistics["periodStart"] = DateTime.UtcNow.AddDays(-30);
                statistics["periodEnd"] = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating statistics report");
                statistics["error"] = "Failed to generate statistics";
            }

            return statistics;
        }
        // In FirebaseSchoolService.cs - Add these methods
        public async Task<ClassPerformanceReport> GenerateClassPerformanceReportAsync(string classId, string term, int year)
        {
            try
            {
                var report = new ClassPerformanceReport
                {
                    ClassId = classId,
                    Term = term,
                    Year = year,
                    ReportDate = DateTime.UtcNow
                };

                // Get class details
                var classDetails = await GetClassByIdAsync(classId);
                if (classDetails != null)
                {
                    report.ClassName = classDetails.Name;
                    report.TeacherId = classDetails.TeacherId;
                    report.TeacherName = classDetails.TeacherName;
                    report.Subject = classDetails.Subject;
                }

                // Get students in this class
                var students = await GetStudentsByClassIdAsync(classId);
                report.TotalStudents = students.Count;

                if (report.TotalStudents == 0)
                {
                    return report; // Return empty report if no students
                }

                // Collect grades for all students in this class and subject
                var allGrades = new List<Grade>();
                foreach (var student in students)
                {
                    var studentGrades = await GetStudentGradesAsync(student.Id);
                    if (studentGrades != null)
                    {
                        // Filter by subject and term/year if specified
                        var filteredGrades = studentGrades
                            .Where(g => g.Subject == report.Subject &&
                                        g.Term == term &&
                                        g.Year == year)
                            .ToList();

                        allGrades.AddRange(filteredGrades);
                    }
                }

                // Calculate statistics if we have grades
                if (allGrades.Any())
                {
                    report.AverageScore = Math.Round(allGrades.Average(g => g.TotalScore), 2);

                    // Calculate grade distribution
                    report.GradeDistribution = new GradeDistribution
                    {
                        ACount = allGrades.Count(g => g.TotalScore >= 90),
                        BCount = allGrades.Count(g => g.TotalScore >= 80 && g.TotalScore < 90),
                        CCount = allGrades.Count(g => g.TotalScore >= 70 && g.TotalScore < 80),
                        DCount = allGrades.Count(g => g.TotalScore >= 60 && g.TotalScore < 70),
                        FCount = allGrades.Count(g => g.TotalScore < 60)
                    };

                    // Calculate pass rate
                    var passingGrades = allGrades.Count(g => g.TotalScore >= 60);
                    report.PassRate = Math.Round((decimal)passingGrades / allGrades.Count * 100, 2);
                    report.FailRate = 100 - report.PassRate;

                    // Calculate average GPA for the class
                    report.AverageGPA = students.Any() ?
                        Math.Round(students.Average(s => s.GPA), 2) : 0;
                }

                // Get attendance data for the class
                try
                {
                    // Get recent attendance (last 30 days)
                    var thirtyDaysAgo = DateTime.Today.AddDays(-30);
                    var recentAttendance = new List<Attendance>();

                    // You might need to implement GetClassAttendanceByDateRangeAsync
                    // For now, let's get today's attendance as a sample
                    var todayAttendance = await GetClassAttendanceAsync(classId, DateTime.Today);
                    if (todayAttendance != null)
                    {
                        recentAttendance.AddRange(todayAttendance);
                    }

                    if (recentAttendance.Any())
                    {
                        var presentCount = recentAttendance.Count(a => a.Status == "Present");
                        report.PresentStudents = presentCount;
                        report.AbsentStudents = report.TotalStudents - presentCount;
                        report.AttendanceRate = Math.Round((decimal)presentCount / (recentAttendance.Count) * 100, 2);
                    }
                }
                catch (Exception attendanceEx)
                {
                    _logger.LogWarning(attendanceEx, "Could not load attendance data for class {ClassId}", classId);
                    // Continue without attendance data
                }

                // Identify top performers (students with highest average scores)
                var studentAverages = new List<(StudentProfile student, decimal avgScore)>();
                foreach (var student in students)
                {
                    var studentGrades = allGrades.Where(g => g.StudentId == student.Id).ToList();
                    if (studentGrades.Any())
                    {
                        var avgScore = Math.Round(studentGrades.Average(g => g.TotalScore), 2);
                        studentAverages.Add((student, avgScore));
                    }
                }

                // Top 5 performers
                report.TopPerformers = studentAverages
                    .OrderByDescending(x => x.avgScore)
                    .Take(5)
                    .Select((x, index) => new StudentRanking
                    {
                        StudentId = x.student.Id,
                        StudentName = x.student.FullName,
                        AverageScore = x.avgScore,
                        GPA = x.student.GPA,
                        AttendancePercentage = x.student.AttendancePercentage,
                        Rank = $"#{index + 1}",
                        PerformanceTrend = "Stable" // You could calculate trend by comparing with previous terms
                    })
                    .ToList();

                // Students needing improvement (bottom 5)
                report.NeedImprovement = studentAverages
                    .OrderBy(x => x.avgScore)
                    .Take(5)
                    .Select((x, index) => new StudentRanking
                    {
                        StudentId = x.student.Id,
                        StudentName = x.student.FullName,
                        AverageScore = x.avgScore,
                        GPA = x.student.GPA,
                        AttendancePercentage = x.student.AttendancePercentage,
                        Rank = "Needs Improvement",
                        AreasToImprove = new List<string> { "Focus on key concepts", "Complete all assignments" }
                    })
                    .ToList();

                // Generate recommendations based on performance
                report.Recommendations = GeneratePerformanceRecommendations(report);

                // Overall remarks
                report.OverallRemarks = GenerateOverallRemarks(report);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating class performance report for class {ClassId}", classId);
                throw;
            }
        }

        public async Task<ClassPerformanceReport> GenerateClassPerformanceReportAsync(ClassPerformanceReportRequest request)
        {
            return await GenerateClassPerformanceReportAsync(request.ClassId, request.Term, request.Year);
        }

        

     

        // Helper method to get class grades
        public async Task<List<Grade>> GetClassGradesAsync(string classId, string subject, string term, int year)
        {
            try
            {
                var students = await GetStudentsByClassIdAsync(classId);
                var allGrades = new List<Grade>();

                foreach (var student in students)
                {
                    var studentGrades = await GetStudentGradesAsync(student.Id);
                    if (studentGrades != null)
                    {
                        var filteredGrades = studentGrades
                            .Where(g => g.Subject == subject &&
                                        g.Term == term &&
                                        g.Year == year)
                            .ToList();

                        allGrades.AddRange(filteredGrades);
                    }
                }

                return allGrades;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting class grades for class {ClassId}", classId);
                return new List<Grade>();
            }
        }

        // Helper methods for recommendations
        private List<string> GeneratePerformanceRecommendations(ClassPerformanceReport report)
        {
            var recommendations = new List<string>();

            if (report.PassRate < 70)
            {
                recommendations.Add("Consider additional review sessions for key concepts.");
                recommendations.Add("Provide extra practice materials for struggling students.");
            }

            if (report.GradeDistribution.FPercentage > 20)
            {
                recommendations.Add("Implement one-on-one tutoring sessions for students with F grades.");
                recommendations.Add("Review assessment difficulty and provide more formative assessments.");
            }

            if (report.AttendanceRate < 85)
            {
                recommendations.Add("Address attendance concerns - contact parents of frequently absent students.");
                recommendations.Add("Implement attendance incentives for improved participation.");
            }

            if (report.AverageScore < 70)
            {
                recommendations.Add("Break down complex topics into smaller, more manageable lessons.");
                recommendations.Add("Incorporate more hands-on activities and interactive learning.");
            }

            // Add positive reinforcement for good performance
            if (report.PassRate > 85 && report.AverageScore > 75)
            {
                recommendations.Add("Maintain current teaching strategies - class is performing well.");
                recommendations.Add("Challenge top performers with advanced materials or projects.");
            }

            return recommendations;
        }

        private string GenerateOverallRemarks(ClassPerformanceReport report)
        {
            if (report.TotalStudents == 0)
            {
                return "No students enrolled in this class.";
            }

            if (report.PassRate >= 85 && report.AverageScore >= 80)
            {
                return $"Excellent performance! Class is achieving strong results with {report.PassRate}% pass rate and {report.AverageScore} average score.";
            }
            else if (report.PassRate >= 70 && report.AverageScore >= 70)
            {
                return $"Good performance overall. Consider focusing on the {report.NeedImprovement.Count} students who need additional support.";
            }
            else if (report.PassRate >= 60 && report.AverageScore >= 60)
            {
                return $"Satisfactory performance. Areas for improvement identified. Implement recommendations to boost performance.";
            }
            else
            {
                return $"Performance needs attention. Focus on core concepts and provide additional support to struggling students.";
            }
        }
        private GradeDistribution CalculateGradeDistribution(List<Grade> grades)
        {
            var distribution = new GradeDistribution
            {
                ACount = grades.Count(g => g.TotalScore >= 90),
                BCount = grades.Count(g => g.TotalScore >= 80 && g.TotalScore < 90),
                CCount = grades.Count(g => g.TotalScore >= 70 && g.TotalScore < 80),
                DCount = grades.Count(g => g.TotalScore >= 60 && g.TotalScore < 70),
                FCount = grades.Count(g => g.TotalScore < 60)
            };

            return distribution;
        }

        private List<string> GenerateRecommendations(ClassPerformanceReport report)
        {
            var recommendations = new List<string>();

            if (report.PassRate < 70)
            {
                recommendations.Add("Implement additional tutoring sessions for struggling students.");
            }

            if (report.AttendanceRate < 85)
            {
                recommendations.Add("Address attendance issues - contact parents of frequently absent students.");
            }

            if (report.GradeDistribution.FPercentage > 20)
            {
                recommendations.Add("Review assessment methods - consider alternative evaluation approaches.");
            }

            if (report.AverageScore < 70)
            {
                recommendations.Add("Provide more practice materials and review sessions.");
            }

            return recommendations;
        }

        public Task<List<Class>> GetClassesByTeacherAsync(string teacherId)
        {
            throw new NotImplementedException();
        }

        public Task<StudentReport> GetStudentReportAsync(string studentId, string term, int year)
        {
            throw new NotImplementedException();
        }
    }

    #region Additional Models
    public class TeacherAssignment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TeacherId { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public List<string> ClassIds { get; set; } = new List<string>();
        public List<string> Subjects { get; set; } = new List<string>();
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        public string AssignedBy { get; set; } = string.Empty; // HR user ID
        public bool IsActive { get; set; } = true;
    }

    public class SubjectAssignment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TeacherId { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public List<string> ClassIds { get; set; } = new List<string>();
        public string AcademicYear { get; set; } = DateTime.Now.Year.ToString();
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        public string AssignedBy { get; set; } = string.Empty; // HR user ID
    }
    #endregion
}
#endregion