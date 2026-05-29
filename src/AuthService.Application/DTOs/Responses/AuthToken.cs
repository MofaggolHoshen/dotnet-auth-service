namespace AuthService.Application.DTOs.Responses
{
    public class AuthToken
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresInMinutes { get; set; }
    }
}
