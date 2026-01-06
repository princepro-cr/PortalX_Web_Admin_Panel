using System;
using System.Collections.Generic;

namespace HighSchoolPortal.Models
{
    public class ClassPerformanceReport
    {
        public string ClassId { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string TeacherId { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public int Year { get; set; }
        public DateTime ReportDate { get; set; } = DateTime.Now;

        // Class Statistics
        public int TotalStudents { get; set; }
        public int PresentStudents { get; set; }
        public int AbsentStudents { get; set; }
        public decimal AttendanceRate { get; set; }
        public decimal AverageGPA { get; set; }
        public decimal AverageScore { get; set; }
        public decimal PassRate { get; set; }
        public decimal FailRate { get; set; }

        // Grade Distribution
        public GradeDistribution GradeDistribution { get; set; } = new GradeDistribution();

        // Subject Performance
        public List<SubjectPerformance> SubjectPerformances { get; set; } = new List<SubjectPerformance>();

        // Student Rankings
        public List<StudentRanking> TopPerformers { get; set; } = new List<StudentRanking>();
        public List<StudentRanking> NeedImprovement { get; set; } = new List<StudentRanking>();

        // Assessment Analysis
        public AssessmentAnalysis AssessmentAnalysis { get; set; } = new AssessmentAnalysis();

        // Attendance Analysis
        public AttendanceAnalysis AttendanceAnalysis { get; set; } = new AttendanceAnalysis();

        // Recommendations
        public List<string> Recommendations { get; set; } = new List<string>();
        public string OverallRemarks { get; set; } = string.Empty;
    }

    public class GradeDistribution
    {
        public int ACount { get; set; }      // 90-100
        public int BCount { get; set; }      // 80-89
        public int CCount { get; set; }      // 70-79
        public int DCount { get; set; }      // 60-69
        public int FCount { get; set; }      // Below 60

        public decimal APercentage => Total > 0 ? Math.Round((decimal)ACount / Total * 100, 2) : 0;
        public decimal BPercentage => Total > 0 ? Math.Round((decimal)BCount / Total * 100, 2) : 0;
        public decimal CPercentage => Total > 0 ? Math.Round((decimal)CCount / Total * 100, 2) : 0;
        public decimal DPercentage => Total > 0 ? Math.Round((decimal)DCount / Total * 100, 2) : 0;
        public decimal FPercentage => Total > 0 ? Math.Round((decimal)FCount / Total * 100, 2) : 0;

        public int Total => ACount + BCount + CCount + DCount + FCount;

        public string GetGradeSummary()
        {
            return $"A: {APercentage}%, B: {BPercentage}%, C: {CPercentage}%, D: {DPercentage}%, F: {FPercentage}%";
        }
    }

    public class SubjectPerformance
    {
        public string Subject { get; set; } = string.Empty;
        public decimal AverageScore { get; set; }
        public decimal HighestScore { get; set; }
        public decimal LowestScore { get; set; }
        public decimal PassRate { get; set; } // Percentage of students with score >= 60
        public int TotalStudents { get; set; }
        public int PassingStudents { get; set; }
        public int FailingStudents { get; set; }
        public string PerformanceLevel { get; set; } = "Average"; // Excellent, Good, Average, Poor
        public string Remarks { get; set; } = string.Empty;
    }

    public class StudentRanking
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public decimal AverageScore { get; set; }
        public decimal GPA { get; set; }
        public int AttendancePercentage { get; set; }
        public string Rank { get; set; } = "N/A";
        public string PerformanceTrend { get; set; } = "Stable"; // Improving, Declining, Stable
        public List<string> Strengths { get; set; } = new List<string>();
        public List<string> AreasToImprove { get; set; } = new List<string>();
    }

    public class AssessmentAnalysis
    {
        public List<AssessmentComponent> Components { get; set; } = new List<AssessmentComponent>();
        public decimal AverageTestScore { get; set; }
        public decimal AverageExamScore { get; set; }
        public decimal AverageAssignmentScore { get; set; }
        public decimal AverageProjectScore { get; set; }

        // Identify weakest assessment type
        public string WeakestComponent
        {
            get
            {
                if (Components.Count == 0) return "N/A";

                return Components.OrderBy(c => c.AverageScore).FirstOrDefault()?.ComponentName ?? "N/A";
            }
        }

        public string StrongestComponent
        {
            get
            {
                if (Components.Count == 0) return "N/A";

                return Components.OrderByDescending(c => c.AverageScore).FirstOrDefault()?.ComponentName ?? "N/A";
            }
        }
    }

    public class AssessmentComponent
    {
        public string ComponentName { get; set; } = string.Empty; // Test1, Test2, Exam, Assignment, Project
        public decimal Weight { get; set; } // e.g., 25% for Test1
        public decimal AverageScore { get; set; }
        public decimal MaxPossibleScore { get; set; }
        public decimal PercentageAchieved => MaxPossibleScore > 0 ?
            Math.Round(AverageScore / MaxPossibleScore * 100, 2) : 0;
        public string Performance { get; set; } = "Average"; // Excellent, Good, Average, Poor
    }

    public class AttendanceAnalysis
    {
        public decimal OverallAttendanceRate { get; set; }
        public int TotalAbsences { get; set; }
        public int TotalTardies { get; set; }
        public int TotalExcusedAbsences { get; set; }
        public int TotalUnexcusedAbsences { get; set; }

        public List<MonthlyAttendance> MonthlyAttendance { get; set; } = new List<MonthlyAttendance>();
        public List<StudentAttendanceRecord> AttendanceConcerns { get; set; } = new List<StudentAttendanceRecord>();

        public string AttendanceStatus
        {
            get
            {
                return OverallAttendanceRate switch
                {
                    >= 95 => "Excellent",
                    >= 90 => "Good",
                    >= 85 => "Satisfactory",
                    >= 80 => "Needs Improvement",
                    _ => "Poor"
                };
            }
        }
    }

    public class MonthlyAttendance
    {
        public string Month { get; set; } = string.Empty; // e.g., "January 2024"
        public decimal AttendanceRate { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public string Trend { get; set; } = "Stable"; // Improving, Declining, Stable
    }

    public class StudentAttendanceRecord
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public decimal AttendancePercentage { get; set; }
        public int TotalAbsences { get; set; }
        public int ConsecutiveAbsences { get; set; }
        public bool HasAttendanceConcern => AttendancePercentage < 80 || ConsecutiveAbsences >= 3;
        public string ConcernLevel
        {
            get
            {
                if (AttendancePercentage < 70 || ConsecutiveAbsences >= 5) return "Critical";
                if (AttendancePercentage < 80 || ConsecutiveAbsences >= 3) return "Warning";
                return "Normal";
            }
        }
    }

    // Helper model for generating the report
    public class ClassPerformanceReportRequest
    {
        public string ClassId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Term { get; set; } = "First";
        public int Year { get; set; } = DateTime.Now.Year;
        public bool IncludeAttendance { get; set; } = true;
        public bool IncludeRecommendations { get; set; } = true;
        public bool IncludeStudentDetails { get; set; } = true;
    }

    // View Model for displaying the report
    public class ClassPerformanceReportViewModel
    {
        public ClassPerformanceReport Report { get; set; } = new ClassPerformanceReport();
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public string GeneratedBy { get; set; } = string.Empty;
        public bool IsPrintable { get; set; } = true;

        // Chart data for visualization
        public object GradeDistributionChartData { get; set; }
        public object SubjectPerformanceChartData { get; set; }
        public object AttendanceTrendChartData { get; set; }
    }
}