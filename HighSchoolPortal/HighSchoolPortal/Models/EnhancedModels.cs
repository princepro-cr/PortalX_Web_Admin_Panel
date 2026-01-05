// Models/EnhancedModels.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolPortal.Models
{
    // Enhanced Grade Model with Validation
    public class EnhancedGrade : Grade
    {
        [Range(0, 25, ErrorMessage = "Test 1 score must be between 0 and 25")]
        public new decimal Test1 { get; set; }

        [Range(0, 25, ErrorMessage = "Test 2 score must be between 0 and 25")]
        public new decimal Test2 { get; set; }

        [Range(0, 40, ErrorMessage = "Exam score must be between 0 and 40")]
        public new decimal Exam { get; set; }

        [Range(0, 10, ErrorMessage = "Assignment score must be between 0 and 10")]
        public new decimal Assignment { get; set; }

        [NotMapped]
        public decimal WeightedTotal => (Test1 * 0.25m) + (Test2 * 0.25m) + (Exam * 0.4m) + (Assignment * 0.1m);
    }

    // Enhanced Student Profile
    public class EnhancedStudentProfile : StudentProfile
    {
        [NotMapped]
        public List<GradeSummary> GradeSummaries { get; set; } = new List<GradeSummary>();

        [NotMapped]
        public List<AttendanceSummary> AttendanceSummaries { get; set; } = new List<AttendanceSummary>();

        [NotMapped]
        public ParentContactInfo ParentContact { get; set; } = new ParentContactInfo();

        [NotMapped]
        public List<BehaviorNote> BehaviorNotes { get; set; } = new List<BehaviorNote>();

        [NotMapped]
        public List<MedicalInfo> MedicalInformation { get; set; } = new List<MedicalInfo>();
    }

    // Enhanced Teacher Profile
    public class EnhancedTeacherProfile : TeacherProfile
    {
        [NotMapped]
        public TeacherStatistics Statistics { get; set; } = new TeacherStatistics();

        [NotMapped]
        public List<ClassAssignment> ClassAssignments { get; set; } = new List<ClassAssignment>();

        [NotMapped]
        public List<ProfessionalDevelopment> ProfessionalDevelopment { get; set; } = new List<ProfessionalDevelopment>();

        [NotMapped]
        public List<TeacherAward> Awards { get; set; } = new List<TeacherAward>();
    }

    // Supporting Models
    public class GradeSummary
    {
        public string Subject { get; set; }
        public decimal AverageScore { get; set; }
        public int TotalGrades { get; set; }
        public decimal HighestScore { get; set; }
        public decimal LowestScore { get; set; }
        public string GradeTrend { get; set; } // "improving", "declining", "stable"
    }

    public class AttendanceSummary
    {
        public string Month { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateDays { get; set; }
        public double AttendancePercentage { get; set; }
    }

    public class ParentContactInfo
    {
        public string PrimaryPhone { get; set; }
        public string SecondaryPhone { get; set; }
        public string Email { get; set; }
        public string PreferredContactMethod { get; set; } // "phone", "email", "sms"
        public bool CanReceiveSMS { get; set; }
        public bool CanReceiveEmails { get; set; }
    }

    public class BehaviorNote
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } // "positive", "negative", "warning"
        public string Description { get; set; }
        public string RecordedBy { get; set; }
        public string FollowUpRequired { get; set; }
    }

    public class MedicalInfo
    {
        public string Condition { get; set; }
        public string Medication { get; set; }
        public string Allergies { get; set; }
        public string EmergencyInstructions { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class TeacherStatistics
    {
        public int TotalStudents { get; set; }
        public decimal AverageStudentGPA { get; set; }
        public double AverageAttendance { get; set; }
        public int YearsOfService { get; set; }
        public List<SubjectPerformance> SubjectPerformance { get; set; } = new List<SubjectPerformance>();
    }

    public class SubjectPerformance
    {
        public string Subject { get; set; }
        public decimal AverageScore { get; set; }
        public double PassRate { get; set; }
        public int TotalStudents { get; set; }
    }
 

    public class ProfessionalDevelopment
    {
        public string CourseName { get; set; }
        public string Provider { get; set; }
        public DateTime CompletionDate { get; set; }
        public int Hours { get; set; }
        public string CertificateId { get; set; }
    }

    public class TeacherAward
    {
        public string AwardName { get; set; }
        public DateTime AwardDate { get; set; }
        public string GivenBy { get; set; }
        public string Description { get; set; }
    }

    // School Year Model
    public class SchoolYear
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } // e.g., "2024-2025"
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public List<Term> Terms { get; set; } = new List<Term>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Term
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } // "First Term", "Second Term", etc.
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrent { get; set; }
    }

    // Announcement Model
    public class Announcement
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; }
        public string Content { get; set; }
        public string Category { get; set; } // "general", "academic", "event", "emergency"
        public string TargetAudience { get; set; } // "all", "teachers", "students", "parents"
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;
        public int Priority { get; set; } // 1-5, 5 being highest
    }

    // Event Model
    public class SchoolEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Location { get; set; }
        public string EventType { get; set; } // "meeting", "exam", "holiday", "celebration"
        public string Audience { get; set; } // "all", "teachers", "students", "parents"
        public string Organizer { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}