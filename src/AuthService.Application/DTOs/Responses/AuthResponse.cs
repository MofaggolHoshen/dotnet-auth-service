namespace AuthService.Application.DTOs.Responses
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public AuthToken? Token { get; set; }
    }
}
