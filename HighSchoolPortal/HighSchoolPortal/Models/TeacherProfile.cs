// Current TeacherProfile.cs
using HighSchoolPortal.Models;

public class TeacherProfile : UserProfile
{
    public string TeacherId { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public List<string> Subjects { get; set; } = new List<string>();
    public List<string> Classes { get; set; } = new List<string>(); // This should contain class IDs like ["10A", "10B"]
    public DateTime HireDate { get; set; } = DateTime.UtcNow;
    public string Qualification { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public bool IsHeadOfDepartment { get; set; } = false;
}