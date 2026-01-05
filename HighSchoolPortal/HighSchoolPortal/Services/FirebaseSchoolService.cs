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

        // Update the ParseStudentDocument method in FirebaseSchoolService.cs
        private StudentProfile ParseStudentDocument(JsonElement document)
        {
            try
            {
                // Check if document has fields and name properties
                if (document.TryGetProperty("fields", out var fields) &&
                    document.TryGetProperty("name", out var name))
                {
                    // Extract document ID from the name field
                    string documentPath = name.GetString() ?? "";
                    string id = documentPath.Split('/').LastOrDefault() ?? Guid.NewGuid().ToString();

                    // Try to parse the fields - they should be a JSON object
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

        // Also update the ParseTeacherDocument method
        private TeacherProfile ParseTeacherDocument(JsonElement document)
        {
            try
            {
                if (document.TryGetProperty("fields", out var fields) &&
                    document.TryGetProperty("name", out var name))
                {
                    // Extract document ID from the name field
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

        // Update the GetAllStudentsAsync method to debug the response
        public async Task<IEnumerable<StudentProfile>> GetAllStudentsAsync()
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/students";
                _logger.LogInformation($"Fetching students from: {url}");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"Firestore response: {content.Substring(0, Math.Min(500, content.Length))}");

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
                else
                {
                    _logger.LogWarning("No 'documents' property found in response");
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

        // Update the GetStudentByIdAsync to debug response
        public async Task<StudentProfile> GetStudentByIdAsync(string id)
        {
            try
            {
                _logger.LogInformation($"Getting student by ID: {id}");

                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/students/{id}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Student not found with ID: {id}, Status: {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"Student response: {content}");

                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                // Parse directly from the response
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

                _logger.LogWarning($"Invalid student document structure for ID: {id}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting student by ID: {id}");
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
                    _logger.LogInformation($"Found {documents.GetArrayLength()} teacher documents");

                    foreach (var document in documents.EnumerateArray())
                    {
                        try
                        {
                            var teacher = ParseTeacherDocument(document);
                            if (teacher != null)
                            {
                                teachers.Add(teacher);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to parse teacher document: {document}");
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("No 'documents' property found in teacher response");
                }

                _logger.LogInformation($"Successfully parsed {teachers.Count} teachers");
                return teachers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing teachers from Firestore");
                return new List<TeacherProfile>();
            }
        }

        // Helper methods for parsing Firestore values
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

        #region Service Methods - IMPLEMENTED
        public async Task<int> GetStudentCountAsync()
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/students";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("documents", out var documents))
                {
                    return documents.GetArrayLength();
                }
                return 0;
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
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/teachers";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("documents", out var documents))
                {
                    return documents.GetArrayLength();
                }
                return 0;
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
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/classes";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("documents", out var documents))
                {
                    return documents.GetArrayLength();
                }
                return 0;
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
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/hr";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("documents", out var documents))
                {
                    return documents.GetArrayLength();
                }
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting HR count");
                return 0;
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

                _logger.LogInformation($"✅ Student added successfully: {student.FullName}");
                return student;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error adding student: {student.FullName}");
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

                _logger.LogInformation($"✅ Student updated successfully: {student.FullName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error updating student: {student.FullName}");
                throw;
            }
        }

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

        // Update the GetTeacherByIdAsync method in FirebaseSchoolService.cs
        public async Task<TeacherProfile> GetTeacherByIdAsync(string id)
        {
            try
            {
                _logger.LogInformation($"Getting teacher by ID: {id}");

                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/teachers/{id}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Teacher not found with ID: {id}, Status: {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"Teacher response: {content}");

                // Parse the single teacher document
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

                _logger.LogWarning($"Invalid teacher document structure for ID: {id}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting teacher by ID: {id}");
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

                _logger.LogInformation($"✅ Teacher added successfully: {teacher.FullName}");
                return teacher;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error adding teacher: {teacher.FullName}");
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

                _logger.LogInformation($"✅ Teacher updated successfully: {teacher.FullName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error updating teacher: {teacher.FullName}");
                throw;
            }
        }

        // Add these methods for Class parsing
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

        private Class ParseClassDocument(JsonElement document)
        {
            try
            {
                if (document.TryGetProperty("fields", out var fields) &&
                    document.TryGetProperty("name", out var name))
                {
                    string id = name.GetProperty("name").GetString()?.Split('/').Last() ?? Guid.NewGuid().ToString();

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
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing class document");
                return null;
            }
        }

        private Class ParseClassFromFirestore(string id, string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("fields", out var fields))
                {
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
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing class from Firestore: {id}");
                return null;
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
                var classes = ParseClassesFromFirestore(content);

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
                _logger.LogInformation($"Getting class by ID: {id}");

                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/classes/{id}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Class not found with ID: {id}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var classItem = ParseClassFromFirestore(id, content);

                if (classItem != null)
                {
                    _logger.LogInformation($"✅ Found class: {classItem.Name}");
                }

                return classItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting class by ID: {id}");
                return null;
            }
        }

        // MISSING METHOD IMPLEMENTATION: AddClassAsync
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
                        ["gradeLevel"] = new { stringValue = classItem.GradeLevel ?? "" },
                        ["teacherId"] = new { stringValue = classItem.TeacherId ?? "" },
                        ["teacherName"] = new { stringValue = classItem.TeacherName ?? "" },
                        ["subject"] = new { stringValue = classItem.Subject ?? "" },
                        ["roomNumber"] = new { stringValue = classItem.RoomNumber ?? "" },
                        ["schedule"] = new { stringValue = classItem.Schedule ?? "" },
                        ["studentIds"] = new
                        {
                            arrayValue = new
                            {
                                values = classItem.StudentIds?.Select(id => new { stringValue = id }).ToArray() ?? Array.Empty<object>()
                            }
                        },
                        ["createdAt"] = new { timestampValue = DateTime.UtcNow.ToString("o") },
                        ["updatedAt"] = new { timestampValue = DateTime.UtcNow.ToString("o") }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(firestoreDoc), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{url}?documentId={documentId}", content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation($"✅ Class added successfully: {classItem.Name}");
                return classItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error adding class: {classItem.Name}");
                throw;
            }
        }

        // MISSING METHOD IMPLEMENTATION: UpdateClassAsync
        public async Task UpdateClassAsync(Class classItem)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/classes/{classItem.Id}";

                var updateMask = new List<string>
                {
                    "name", "gradeLevel", "teacherId", "teacherName", "subject", "roomNumber",
                    "schedule", "studentIds", "updatedAt"
                };

                var firestoreDoc = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["name"] = new { stringValue = classItem.Name },
                        ["gradeLevel"] = new { stringValue = classItem.GradeLevel ?? "" },
                        ["teacherId"] = new { stringValue = classItem.TeacherId ?? "" },
                        ["teacherName"] = new { stringValue = classItem.TeacherName ?? "" },
                        ["subject"] = new { stringValue = classItem.Subject ?? "" },
                        ["roomNumber"] = new { stringValue = classItem.RoomNumber ?? "" },
                        ["schedule"] = new { stringValue = classItem.Schedule ?? "" },
                        ["studentIds"] = new
                        {
                            arrayValue = new
                            {
                                values = classItem.StudentIds?.Select(id => new { stringValue = id }).ToArray() ?? Array.Empty<object>()
                            }
                        },
                        ["updatedAt"] = new { timestampValue = DateTime.UtcNow.ToString("o") }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(firestoreDoc), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync($"{url}?updateMask.fieldPaths={string.Join("&updateMask.fieldPaths=", updateMask)}", content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation($"✅ Class updated successfully: {classItem.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error updating class: {classItem.Name}");
                throw;
            }
        }

        // Add these methods for Grade parsing
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

                    var grade = new Grade
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

                    return grade;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing grade document");
                return null;
            }
        }

        // MISSING METHOD: AddGradeAsync (from interface)
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

                _logger.LogInformation($"✅ Grade added for student: {grade.StudentName}");
                return grade;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error adding grade for student: {grade.StudentName}");
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
                var grades = ParseGradesFromFirestore(responseContent);

                return grades;
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
                // First get all students in the class
                var classItem = await GetClassByIdAsync(classId);
                if (classItem == null || classItem.StudentIds == null || !classItem.StudentIds.Any())
                    return new List<Grade>();

                var allGrades = new List<Grade>();

                // Get grades for each student in the class for the specific subject
                foreach (var studentId in classItem.StudentIds)
                {
                    var studentGrades = await GetStudentGradesAsync(studentId);
                    var subjectGrades = studentGrades.Where(g => g.Subject?.Equals(subject, StringComparison.OrdinalIgnoreCase) == true);
                    allGrades.AddRange(subjectGrades);
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

                _logger.LogInformation($"✅ Grade updated for student: {grade.StudentName}");
                return grade;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error updating grade for student: {grade.StudentName}");
                throw;
            }
        }

        public async Task DeleteGradeAsync(string gradeId)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/grades/{gradeId}";
                var response = await _httpClient.DeleteAsync(url);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation($"✅ Grade deleted: {gradeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error deleting grade: {gradeId}");
                throw;
            }
        }

        // Add these methods for Attendance parsing
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

                _logger.LogInformation($"✅ Attendance recorded for student: {attendance.StudentName}");
                return attendance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error recording attendance for student: {attendance.StudentName}");
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
                var attendanceRecords = ParseAttendanceFromFirestore(responseContent);

                return attendanceRecords;
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

                // Create filters with the same structure
                var filter1 = new
                {
                    fieldFilter = new
                    {
                        field = new { fieldPath = "classId" },
                        op = "EQUAL",
                        value = new { stringValue = classId }
                    }
                };

                var filter2 = new
                {
                    fieldFilter = new
                    {
                        field = new { fieldPath = "date" },
                        op = "EQUAL",
                        value = new { timestampValue = date.ToString("o") }
                    }
                };

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
                                filters = new object[] { filter1, filter2 }
                            }
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(query), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var attendanceRecords = ParseAttendanceFromFirestore(responseContent);

                return attendanceRecords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting class attendance for class: {classId} on date: {date}");
                return new List<Attendance>();
            }
        }

        public async Task DeleteUserAsync(string id)
        {
            try
            {
                // Try to delete from students
                var studentUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/students/{id}";
                var studentResponse = await _httpClient.DeleteAsync(studentUrl);

                if (studentResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"✅ Student deleted: {id}");
                    return;
                }

                // Try to delete from teachers
                var teacherUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/teachers/{id}";
                var teacherResponse = await _httpClient.DeleteAsync(teacherUrl);

                if (teacherResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"✅ Teacher deleted: {id}");
                    return;
                }

                // Try to delete from HR
                var hrUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/hr/{id}";
                var hrResponse = await _httpClient.DeleteAsync(hrUrl);

                if (hrResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"✅ HR deleted: {id}");
                    return;
                }

                _logger.LogWarning($"⚠️ User not found for deletion: {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error deleting user: {id}");
                throw;
            }
        }

        public async Task<IEnumerable<Grade>> GenerateGradeReportAsync(string classId, string term, int year)
        {
            try
            {
                // Get the class
                var classItem = await GetClassByIdAsync(classId);
                if (classItem == null)
                    return new List<Grade>();

                var reportGrades = new List<Grade>();

                // Get grades for each student in the class for the specified term and year
                if (classItem.StudentIds != null)
                {
                    foreach (var studentId in classItem.StudentIds)
                    {
                        var studentGrades = await GetStudentGradesAsync(studentId);
                        var filteredGrades = studentGrades
                            .Where(g => g.Term?.Equals(term, StringComparison.OrdinalIgnoreCase) == true &&
                                       g.Year == year);
                        reportGrades.AddRange(filteredGrades);
                    }
                }

                return reportGrades;
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
                // Get counts
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

                // Get recent activity (last 30 days)
                var recentDate = DateTime.UtcNow.AddDays(-30).ToString("o");
                statistics["reportPeriod"] = "Last 30 days";
                statistics["periodStart"] = DateTime.UtcNow.AddDays(-30);
                statistics["periodEnd"] = DateTime.UtcNow;

                _logger.LogInformation($"✅ Statistics report generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generating statistics report");
                statistics["error"] = "Failed to generate statistics";
            }

            return statistics;
        }
        #endregion
    }
}