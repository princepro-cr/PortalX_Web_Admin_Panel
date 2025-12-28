using HighSchoolPortal.Models;
using System.Threading.Tasks;

namespace HighSchoolPortal.Interfaces
{
    public interface IFirebaseAuthService
    {
        Task<(bool Success, string Message, UserProfile User)> LoginAsync(string email, string password);
        Task<(bool Success, string Message, UserProfile User)> RegisterAsync(RegisterModel model);
        Task<(bool Success, string Message)> LogoutAsync();
        Task<UserProfile> GetCurrentUserAsync();
        Task<bool> IsAuthenticatedAsync();
        Task UpdateUserProfileAsync(UserProfile profile);
        Task SendPasswordResetEmailAsync(string email);
        Task<(bool Success, string Message)> ResetPasswordAsync(string email, string newPassword, string token);
    }
}