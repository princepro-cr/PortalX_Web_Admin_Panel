using System;
using System.Collections.Generic;

namespace HighSchoolPortal.Models
{
    public class Class
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string GradeLevel { get; set; } = string.Empty;
        public string TeacherId { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string Schedule { get; set; } = string.Empty;
        public List<string> StudentIds { get; set; } = new List<string>();
        public int StudentCount => StudentIds?.Count ?? 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string Code { get; internal set; }
        public string AcademicYear { get; internal set; }
        public string Term { get; internal set; }
        public int MaxCapacity { get; internal set; }
        public bool IsActive { get; internal set; }
    }
}