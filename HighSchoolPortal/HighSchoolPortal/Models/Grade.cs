using System;

namespace HighSchoolPortal.Models
{
    public class Grade
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string TeacherId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Term { get; set; } = "First";
        public int Year { get; set; } = DateTime.Now.Year;
        public decimal Test1 { get; set; } = 0;
        public decimal Test2 { get; set; } = 0;
        public decimal Exam { get; set; } = 0;
        public decimal Assignment { get; set; } = 0;
        public decimal TotalScore { get; set; } = 0;
        public string GradeLetter { get; set; } = "F";
        public string Remarks { get; set; } = string.Empty;
        public DateTime DateRecorded { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public void CalculateTotal()
        {
            TotalScore = Test1 + Test2 + Exam + Assignment;

            if (TotalScore >= 90) GradeLetter = "A";
            else if (TotalScore >= 80) GradeLetter = "B";
            else if (TotalScore >= 70) GradeLetter = "C";
            else if (TotalScore >= 60) GradeLetter = "D";
            else GradeLetter = "F";
        }
    }

  

 
}