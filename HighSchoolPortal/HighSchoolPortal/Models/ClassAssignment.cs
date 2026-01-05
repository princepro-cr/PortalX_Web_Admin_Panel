using System;

namespace HighSchoolPortal.Models
{
    public class ClassAssignment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TeacherId { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string ClassId { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = DateTime.Now.Year.ToString();
        public string Term { get; set; } = "First";
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Schedule information
        public string Schedule { get; set; } = "Mon-Fri 9:00-10:00";
        public string RoomNumber { get; set; } = string.Empty;
        public decimal StudentCount { get; internal set; }
    }
}