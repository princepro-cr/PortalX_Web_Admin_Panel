// Models/FirebaseSettings.cs
namespace HighSchoolPortal.Models
{
    public class FirebaseSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string AuthDomain { get; set; } = string.Empty;
        public string StorageBucket { get; set; } = string.Empty;
        public string MessagingSenderId { get; set; } = string.Empty;
        public string AppId { get; set; } = string.Empty;
    }
}