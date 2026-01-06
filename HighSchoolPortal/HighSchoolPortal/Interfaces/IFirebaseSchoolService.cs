using HighSchoolPortal.Models;
using HighSchoolPortal.Services;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HighSchoolPortal.Interfaces
{
    public interface IFirebaseSchoolService
    {
        // Dashboard Statistics
        Task<int> GetStudentCountAsync();
        Task<int> GetTeacherCountAsync();
        Task<int> GetClassCountAsync();
        Task<int> GetHRCountAsync();

        // Student Management
        Task<IEnumerable<StudentProfile>> GetAllStudentsAsync();
        Task<StudentProfile> GetStudentByIdAsync(string id);
        Task<StudentProfile> AddStudentAsync(StudentProfile student);
        Task UpdateStudentAsync(StudentProfile student);

        // Teacher Management
        Task<IEnumerable<TeacherProfile>> GetAllTeachersAsync();
        Task<TeacherProfile> GetTeacherByIdAsync(string id);
        Task<TeacherProfile> AddTeacherAsync(TeacherProfile teacher);
        Task UpdateTeacherAsync(TeacherProfile teacher);

        // Class Management
        Task<Class> AddClassAsync(Class classItem);
        Task<IEnumerable<Class>> GetAllClassesAsync();
        Task<Class> GetClassByIdAsync(string id);
        Task UpdateClassAsync(Class classItem);

        // Grade Management
        Task<Grade> AddGradeAsync(Grade grade);
        Task<IEnumerable<Grade>> GetStudentGradesAsync(string studentId);
        Task<IEnumerable<Grade>> GetClassGradesAsync(string classId, string subject);
        Task<Grade> UpdateGradeAsync(Grade grade);
        Task DeleteGradeAsync(string gradeId);

        // Attendance Management
        Task<Attendance> RecordAttendanceAsync(Attendance attendance);
        Task<IEnumerable<Attendance>> GetStudentAttendanceAsync(string studentId);
        Task<IEnumerable<Attendance>> GetClassAttendanceAsync(string classId, DateTime date);

        // User Management
        Task DeleteUserAsync(string id);

        // Report Generation
        Task<IEnumerable<Grade>> GenerateGradeReportAsync(string classId, string term, int year);
        Task<Dictionary<string, object>> GenerateStatisticsReportAsync();

        // NEW METHODS ADDED FOR TEACHER-STUDENT RELATIONSHIP
        Task<List<StudentProfile>> GetStudentsByTeacherAsync(string teacherId);
        Task<List<StudentProfile>> GetStudentsByClassIdAsync(string classId);

       // HR can assign teachers to classes and subjects
    Task<TeacherAssignment> AssignTeacherToClassesAsync(string teacherId, List<string> classIds, List<string> subjects);

         

        // Get classes by teacher
        Task<List<Class>> GetClassesByTeacherAsync(string teacherId);

        // Get teacher's pass rate
        Task<decimal> GetTeacherPassRateAsync(string teacherId);

        // Add weighted grade
        Task<Grade> AddWeightedGradeAsync(WeightedGrade grade);

        // Get student report
        Task<StudentReport> GetStudentReportAsync(string studentId, string term, int year);

        Task<ClassPerformanceReport> GenerateClassPerformanceReportAsync(string classId, string term, int year);
        Task<ClassPerformanceReport> GenerateClassPerformanceReportAsync(ClassPerformanceReportRequest request);

        // Helper methods
         Task<List<Grade>> GetClassGradesAsync(string classId, string subject, string term, int year);
 
        public Task<ClassPerformanceReport> GetClassPerformanceReportAsync(string classId, string term, int year)
        {
            throw new NotImplementedException();
        }
     }
}
