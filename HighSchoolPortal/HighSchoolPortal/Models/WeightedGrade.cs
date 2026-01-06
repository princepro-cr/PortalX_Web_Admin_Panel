using System.ComponentModel.DataAnnotations;

namespace HighSchoolPortal.Models
{
    public class WeightedGrade
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Term { get; set; } = "First";

        [Required]
        [Range(2020, 2030)]
        public int Year { get; set; } = DateTime.Now.Year;

        // Assessment components with weights
        [Range(0, 100)]
        public decimal Test1 { get; set; } = 0;

        [Range(0, 100)]
        public decimal Test2 { get; set; } = 0;

        [Range(0, 100)]
        public decimal Midterm { get; set; } = 0;

        [Range(0, 100)]
        public decimal FinalExam { get; set; } = 0;

        [Range(0, 100)]
        public decimal Project { get; set; } = 0;

        [Range(0, 100)]
        public decimal ClassParticipation { get; set; } = 0;

        [Range(0, 100)]
        public decimal Homework { get; set; } = 0;

        // Weights (should total 100%)
        public decimal Test1Weight { get; set; } = 15;
        public decimal Test2Weight { get; set; } = 15;
        public decimal MidtermWeight { get; set; } = 20;
        public decimal FinalExamWeight { get; set; } = 30;
        public decimal ProjectWeight { get; set; } = 10;
        public decimal ParticipationWeight { get; set; } = 5;
        public decimal HomeworkWeight { get; set; } = 5;

        // Calculated fields
        public decimal TotalScore { get; set; } = 0;
        public string GradeLetter { get; set; } = "F";

        [StringLength(500)]
        public string Remarks { get; set; } = string.Empty;

        public DateTime DateRecorded { get; set; } = DateTime.UtcNow;
        public string TeacherId { get; set; } = string.Empty;

        public void CalculateWeightedTotal()
        {
            TotalScore = (Test1 * Test1Weight / 100) +
                        (Test2 * Test2Weight / 100) +
                        (Midterm * MidtermWeight / 100) +
                        (FinalExam * FinalExamWeight / 100) +
                        (Project * ProjectWeight / 100) +
                        (ClassParticipation * ParticipationWeight / 100) +
                        (Homework * HomeworkWeight / 100);

            TotalScore = Math.Round(TotalScore, 2);

            // Calculate grade letter
            GradeLetter = TotalScore switch
            {
                >= 93 => "A",
                >= 90 => "A-",
                >= 87 => "B+",
                >= 83 => "B",
                >= 80 => "B-",
                >= 77 => "C+",
                >= 73 => "C",
                >= 70 => "C-",
                >= 67 => "D+",
                >= 63 => "D",
                >= 60 => "D-",
                _ => "F"
            };
        }
    }
}