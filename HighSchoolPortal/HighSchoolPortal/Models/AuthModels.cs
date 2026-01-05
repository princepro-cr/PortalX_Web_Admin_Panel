using System.ComponentModel.DataAnnotations;

namespace HighSchoolPortal.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
        public string Role { get; set; } = "student";
    }

    public class RegisterModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "Role")]
        public string Role { get; set; } = "student";

        // Student specific - Required for students
        [Display(Name = "Grade Level")]
        public string? GradeLevel { get; set; } = "10";

        
        

        [Display(Name = "Student ID")]
        public string? StudentId { get; set; } = string.Empty;

        // Teacher specific - Required for teachers
        [Display(Name = "Teacher ID")]
        public string? TeacherId { get; set; } = string.Empty;

        [Display(Name = "Department")]
        public string? Department { get; set; } = string.Empty;

        [Display(Name = "Qualification")]
        public string? Qualification { get; set; } = string.Empty;

        // HR specific - Required for HR
        [Display(Name = "Employee ID")]
        public string? EmployeeId { get; set; } = string.Empty;

        // Address fields for all
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; } = string.Empty;

        [Display(Name = "Address")]
        public string? Address { get; set; } = string.Empty;

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }
    }

    public class ResetPasswordModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}