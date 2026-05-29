namespace AuthService.Application.Interfaces;

public interface IEmailSender
{
    Task SendEmailVerificationAsync(string toEmail, string token);
    Task SendPasswordResetAsync(string toEmail, string token);
}
