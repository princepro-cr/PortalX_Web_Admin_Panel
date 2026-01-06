namespace HighSchoolPortal.Models
{
    public class StudentReport
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string ClassId { get; set; } = string.Empty;
        public string GradeLevel { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public int Year { get; set; }
        public DateTime ReportDate { get; set; } = DateTime.Now;

        // Academic Performance
        public decimal TermGPA { get; set; }
        public decimal CumulativeGPA { get; set; }
        public int AttendancePercentage { get; set; }
        public string Conduct { get; set; } = "Satisfactory";
        public string OverallGrade { get; set; } = "C";

        // Subject Grades
        public List<SubjectGrade> SubjectGrades { get; set; } = new List<SubjectGrade>();

        // Teacher Comments
        public string TeacherComments { get; set; } = string.Empty;
        public string PrincipalComments { get; set; } = string.Empty;

        // Statistics
        public int TotalSubjects { get; set; }
        public int PassedSubjects { get; set; }
        public decimal PassRate { get; set; }
        public string RankInClass { get; set; } = "N/A";
        public int TotalStudentsInClass { get; set; }
    }

    public class SubjectGrade
    {
        public string Subject { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public string Grade { get; set; } = "F";
        public string TeacherName { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public List<AssessmentScore> Assessments { get; set; } = new List<AssessmentScore>();
    }

    public class AssessmentScore
    {
        public string AssessmentType { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public decimal Weight { get; set; }
        public decimal WeightedScore { get; set; }
        public DateTime Date { get; set; }
    }
}