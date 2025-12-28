using System;
using System.Collections.Generic;

namespace HighSchoolPortal.Models
{
    public class StudentProfile : UserProfile
    {
        public string StudentId { get; set; } = string.Empty;
        public string GradeLevel { get; set; } = "10";
        public string ParentName { get; set; } = string.Empty;
        public string ParentEmail { get; set; } = string.Empty;
        public string ParentPhone { get; set; } = string.Empty;
        public List<string> EnrolledSubjects { get; set; } = new List<string>();
        public string ClassId { get; set; } = string.Empty;
        public string EmergencyContact { get; set; } = string.Empty;
        public decimal GPA { get; set; } = 0.0m;
        public int AttendancePercentage { get; set; } = 100;
        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    }
}