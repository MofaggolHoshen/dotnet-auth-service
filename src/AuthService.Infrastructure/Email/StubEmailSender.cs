using AuthService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Email;

public class StubEmailSender : IEmailSender
{
    private readonly ILogger<StubEmailSender> _logger;

    public StubEmailSender(ILogger<StubEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailVerificationAsync(string toEmail, string token)
    {
        _logger.LogInformation($"[STUB] Verification email would be sent to {toEmail}");
        _logger.LogInformation($"[STUB] Verification token: {token}");
        _logger.LogInformation($"[STUB] Verification link: /verify-email?token={token}");
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string toEmail, string token)
    {
        _logger.LogInformation($"[STUB] Password reset email would be sent to {toEmail}");
        _logger.LogInformation($"[STUB] Password reset token: {token}");
        _logger.LogInformation($"[STUB] Password reset link: /reset-password?token={token}");
        return Task.CompletedTask;
    }
}
