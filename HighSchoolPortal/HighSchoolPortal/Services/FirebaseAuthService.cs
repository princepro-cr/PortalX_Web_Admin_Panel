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
    public class FirebaseAuthService : IFirebaseAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FirebaseAuthService> _logger;
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly IFirebaseSchoolService _firestoreService;
        private UserProfile _currentUser;

        public FirebaseAuthService(
            IConfiguration configuration,
            ILogger<FirebaseAuthService> logger,
            IHttpClientFactory httpClientFactory,
            IFirebaseSchoolService firestoreService)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _firestoreService = firestoreService;

            _apiKey = _configuration["Firebase:ApiKey"];
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogError("Firebase API Key is not configured!");
                throw new InvalidOperationException("Firebase API Key must be configured in appsettings.json");
            }

            _logger.LogInformation($"FirebaseAuthService initialized with API Key: {_apiKey.Substring(0, Math.Min(5, _apiKey.Length))}...");
        }

        public async Task<(bool Success, string Message, UserProfile User)> LoginAsync(string email, string password)
        {
            try
            {
                _logger.LogInformation($"Login attempt for: {email}");

                var requestData = new
                {
                    email = email,
                    password = password,
                    returnSecureToken = true
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}",
                    requestData
                );

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Login failed for {email}: {response.StatusCode}");

                    // Parse error message
                    try
                    {
                        using var doc = JsonDocument.Parse(responseContent);
                        if (doc.RootElement.TryGetProperty("error", out var errorElement))
                        {
                            if (errorElement.TryGetProperty("message", out var messageElement))
                            {
                                var errorMsg = messageElement.GetString();

                                if (errorMsg.Contains("INVALID_LOGIN_CREDENTIALS"))
                                    return (false, "Invalid email or password. Please try again.", null);

                                if (errorMsg.Contains("USER_DISABLED"))
                                    return (false, "This account has been disabled.", null);

                                return (false, "Login failed. Please check your credentials.", null);
                            }
                        }
                    }
                    catch
                    {
                        return (false, "Login failed. Please check your credentials.", null);
                    }

                    return (false, "Login failed. Please check your credentials.", null);
                }

                // Parse successful response
                using var responseDoc = JsonDocument.Parse(responseContent);
                var root = responseDoc.RootElement;

                var userId = root.GetProperty("localId").GetString();
                var userEmail = root.GetProperty("email").GetString();
                var displayName = root.GetProperty("displayName").GetString() ?? userEmail.Split('@')[0];

                _logger.LogInformation($"✅ Firebase authentication successful for: {userEmail}");

                // Get user role from Firestore collections
                var userProfile = await GetUserProfileFromCollectionsAsync(userId, userEmail, displayName);

                _currentUser = userProfile;
                _logger.LogInformation($"✅ Login successful! User: {userEmail}, Role: {userProfile.Role}");
                return (true, "Login successful!", userProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Login Error for {email}: {ex.Message}");
                return (false, "An unexpected error occurred. Please try again.", null);
            }
        }

        private async Task<UserProfile> GetUserProfileFromCollectionsAsync(string userId, string email, string displayName)
        {
            try
            {
                _logger.LogInformation($"Getting user profile from collections for userId: {userId}");

                // 1. Check students collection
                var student = await _firestoreService.GetStudentByIdAsync(userId);
                if (student != null)
                {
                    _logger.LogInformation($"Found student profile for {userId}");
                    return new UserProfile
                    {
                        Id = student.Id,
                        Email = student.Email,
                        FullName = student.FullName,
                        Role = student.Role,
                        CreatedAt = student.CreatedAt,
                        UpdatedAt = student.UpdatedAt,
                        IsActive = student.IsActive,
                        AvatarUrl = student.AvatarUrl
                    };
                }

                _logger.LogInformation($"No student found for {userId}, checking teachers...");

                // 2. Check teachers collection
                var teacher = await _firestoreService.GetTeacherByIdAsync(userId);
                if (teacher != null)
                {
                    _logger.LogInformation($"Found teacher profile for {userId}");
                    return new UserProfile
                    {
                        Id = teacher.Id,
                        Email = teacher.Email,
                        FullName = teacher.FullName,
                        Role = teacher.Role,
                        CreatedAt = teacher.CreatedAt,
                        UpdatedAt = teacher.UpdatedAt,
                        IsActive = teacher.IsActive,
                        AvatarUrl = teacher.AvatarUrl
                    };
                }

                _logger.LogInformation($"No teacher found for {userId}, checking HR users...");

                // 3. Check HR users
                var hrUser = await GetHRUserByIdAsync(userId);
                if (hrUser != null)
                {
                    _logger.LogInformation($"Found HR profile for {userId}");
                    return hrUser;
                }

                _logger.LogWarning($"No profile found in any collection for {userId}");

                // Create a default profile with student role
                return new UserProfile
                {
                    Id = userId,
                    Email = email,
                    FullName = displayName,
                    Role = "student", // Default role
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    AvatarUrl = "/images/default-avatar.png"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user from collections for {userId}");

                // Return a default profile even if there's an error
                return new UserProfile
                {
                    Id = userId,
                    Email = email,
                    FullName = displayName,
                    Role = "student", // Default role
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    AvatarUrl = "/images/default-avatar.png"
                };
            }
        }

        private async Task<UserProfile> GetHRUserByIdAsync(string userId)
        {
            try
            {
                var projectId = _configuration["Firebase:ProjectId"];
                var url = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/users/{userId}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(responseBody);

                if (jsonDoc.RootElement.TryGetProperty("fields", out var fields))
                {
                    var role = GetStringValue(fields, "role");

                    // Only return if role is "hr"
                    if (role == "hr")
                    {
                        return new UserProfile
                        {
                            Id = userId,
                            Email = GetStringValue(fields, "email"),
                            FullName = GetStringValue(fields, "fullName"),
                            Role = role,
                            AvatarUrl = GetStringValue(fields, "avatarUrl", "/images/default-avatar.png"),
                            IsActive = GetBoolValue(fields, "isActive", true),
                            CreatedAt = GetDateTimeValue(fields, "createdAt"),
                            UpdatedAt = GetDateTimeValue(fields, "updatedAt")
                        };
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool Success, string Message, UserProfile User)> RegisterAsync(RegisterModel model)
        {
            try
            {
                _logger.LogInformation($"Registration attempt for: {model.Email}, Role: {model.Role}");

                // Validate input
                if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                {
                    return (false, "Email and password are required.", null);
                }

                if (model.Password != model.ConfirmPassword)
                {
                    return (false, "Passwords do not match.", null);
                }

                if (model.Password.Length < 6)
                {
                    return (false, "Password must be at least 6 characters.", null);
                }

                // Register user in Firebase Authentication
                var requestData = new
                {
                    email = model.Email,
                    password = model.Password,
                    displayName = model.FullName,
                    returnSecureToken = true
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_apiKey}",
                    requestData
                );

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Firebase registration failed! Status: {response.StatusCode}");

                    if (responseContent.Contains("EMAIL_EXISTS"))
                    {
                        return (false, "This email is already registered. Please use a different email.", null);
                    }

                    return (false, "Registration failed. Please try again.", null);
                }

                // Parse successful response
                using var responseDoc = JsonDocument.Parse(responseContent);
                var root = responseDoc.RootElement;

                var userId = root.GetProperty("localId").GetString();
                var userEmail = root.GetProperty("email").GetString();

                _logger.LogInformation($"✅ Firebase Auth registration successful for: {userEmail}");

                // Save to appropriate Firestore collection based on role
                if (model.Role == "student")
                {
                    var studentProfile = new StudentProfile
                    {
                        Id = userId,
                        Email = userEmail,
                        FullName = model.FullName,
                        Role = "student",
                        StudentId = model.StudentId ?? $"STU{DateTime.Now:yyyyMMddHHmmss}",
                        GradeLevel = model.GradeLevel ?? "10",
                        ParentName = model.ParentName ?? "",
                        ParentEmail = model.ParentEmail ?? "",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true,
                        AvatarUrl = "/images/default-avatar.png",
                        AttendancePercentage = 100,
                        GPA = 0.0m
                    };

                    var result = await _firestoreService.AddStudentAsync(studentProfile);
                    if (result != null)
                    {
                        _logger.LogInformation($"✅ Student profile saved to Firestore: {userEmail}");
                        _currentUser = studentProfile;
                    }
                    else
                    {
                        return (false, "Failed to create student profile. Please try again.", null);
                    }
                }
                else if (model.Role == "teacher")
                {
                    var teacherProfile = new TeacherProfile
                    {
                        Id = userId,
                        Email = userEmail,
                        FullName = model.FullName,
                        Role = "teacher",
                        TeacherId = model.TeacherId ?? $"TCH{DateTime.Now:yyyyMMddHHmmss}",
                        Department = model.Department ?? "General",
                        Qualification = model.Qualification ?? "",
                        HireDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true,
                        AvatarUrl = "/images/default-avatar.png"
                    };

                    var result = await _firestoreService.AddTeacherAsync(teacherProfile);
                    if (result != null)
                    {
                        _logger.LogInformation($"✅ Teacher profile saved to Firestore: {userEmail}");
                        _currentUser = teacherProfile;
                    }
                    else
                    {
                        return (false, "Failed to create teacher profile. Please try again.", null);
                    }
                }
                else if (model.Role == "hr")
                {
                    // For HR, we save to users collection
                    var hrProfile = new UserProfile
                    {
                        Id = userId,
                        Email = userEmail,
                        FullName = model.FullName,
                        Role = "hr",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true,
                        AvatarUrl = "/images/default-avatar.png"
                    };

                    await SaveHRUserToFirestoreAsync(hrProfile);
                    _logger.LogInformation($"✅ HR profile saved to Firestore: {userEmail}");
                    _currentUser = hrProfile;
                }
                else
                {
                    return (false, "Invalid role selected.", null);
                }

                _logger.LogInformation($"✅ Registration completed successfully for: {userEmail}");
                return (true, "Registration successful! You are now logged in.", _currentUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Registration Error for {model.Email}: {ex.Message}");
                return (false, $"Registration failed: {ex.Message}", null);
            }
        }

        private async Task SaveHRUserToFirestoreAsync(UserProfile userProfile)
        {
            try
            {
                var firestoreData = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["id"] = new { stringValue = userProfile.Id },
                        ["email"] = new { stringValue = userProfile.Email },
                        ["fullName"] = new { stringValue = userProfile.FullName },
                        ["role"] = new { stringValue = "hr" },
                        ["avatarUrl"] = new { stringValue = userProfile.AvatarUrl },
                        ["isActive"] = new { booleanValue = true },
                        ["createdAt"] = new { timestampValue = userProfile.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                        ["updatedAt"] = new { timestampValue = userProfile.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
                    }
                };

                var projectId = _configuration["Firebase:ProjectId"];
                var url = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/users?documentId={userProfile.Id}";

                var response = await _httpClient.PostAsync(url, JsonContent.Create(firestoreData));

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to save HR user: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving HR user to Firestore");
                throw;
            }
        }

        // Helper methods for Firestore parsing
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

        public async Task<(bool Success, string Message)> ResetPasswordAsync(string email, string newPassword, string token)
        {
            try
            {
                var requestData = new
                {
                    oobCode = token,
                    newPassword = newPassword
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:resetPassword?key={_apiKey}",
                    requestData
                );

                if (!response.IsSuccessStatusCode)
                {
                    return (false, "Password reset failed. Please try again.");
                }

                return (true, "Password has been reset successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset failed");
                return (false, "Password reset failed. Please try again.");
            }
        }

        public Task<(bool Success, string Message)> LogoutAsync()
        {
            _currentUser = null;
            _logger.LogInformation("User logged out");
            return Task.FromResult((true, "Logged out successfully"));
        }

        public Task<UserProfile> GetCurrentUserAsync()
        {
            return Task.FromResult(_currentUser);
        }

        public Task<bool> IsAuthenticatedAsync()
        {
            return Task.FromResult(_currentUser != null);
        }

        public Task UpdateUserProfileAsync(UserProfile profile)
        {
            _currentUser = profile;
            return Task.CompletedTask;
        }

        public async Task SendPasswordResetEmailAsync(string email)
        {
            try
            {
                var requestData = new
                {
                    requestType = "PASSWORD_RESET",
                    email = email
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={_apiKey}",
                    requestData
                );

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Failed to send password reset email");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email");
                throw;
            }
        }
    }
}