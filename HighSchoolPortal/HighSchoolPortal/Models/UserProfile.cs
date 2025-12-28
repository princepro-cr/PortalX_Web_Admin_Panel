using System;

namespace HighSchoolPortal.Models
{
    public class UserProfile
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = "student"; // "student", "teacher", "hr"
        public string AvatarUrl { get; set; } = "/images/default-avatar.png";
        public DateTime DateOfBirth { get; set; } = DateTime.UtcNow.AddYears(-15);
        public string Address { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}