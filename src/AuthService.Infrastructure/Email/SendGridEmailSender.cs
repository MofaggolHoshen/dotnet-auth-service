using AuthService.Application.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Email;

public class SendGridEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridEmailSender> _logger;

    public SendGridEmailSender(IConfiguration configuration, ILogger<SendGridEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailVerificationAsync(string toEmail, string token)
    {
        var apiKey = _configuration["SendGridSettings:ApiKey"] ?? throw new InvalidOperationException("SendGrid API key not configured");
        var senderEmail = _configuration["SendGridSettings:SenderEmail"] ?? throw new InvalidOperationException("SendGrid sender email not configured");
        var senderName = _configuration["SendGridSettings:SenderName"] ?? "Auth Service";
        var appUrl = _configuration["AppUrl"] ?? throw new InvalidOperationException("AppUrl not configured");

        var verificationLink = $"{appUrl}/verify-email?token={token}";

        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(senderEmail, senderName);
        var to = new EmailAddress(toEmail);
        var subject = "Verify Your Email Address";
        var htmlContent = $@"
            <h1>Email Verification</h1>
            <p>Please click the link below to verify your email address:</p>
            <a href='{verificationLink}'>Verify Email</a>
            <p>Or copy and paste this link: {verificationLink}</p>
            <p>This link expires in 24 hours.</p>
        ";

        var msg = new SendGridMessage()
        {
            From = from,
            Subject = subject,
            HtmlContent = htmlContent
        };

        msg.AddTo(to);

        try
        {
            var response = await client.SendEmailAsync(msg);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Verification email sent to {toEmail}");
            }
            else
            {
                _logger.LogError($"Failed to send verification email to {toEmail}: {response.StatusCode}");
                throw new InvalidOperationException($"SendGrid returned status {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send verification email to {toEmail}");
            throw;
        }
    }

    public async Task SendPasswordResetAsync(string toEmail, string token)
    {
        var apiKey = _configuration["SendGridSettings:ApiKey"] ?? throw new InvalidOperationException("SendGrid API key not configured");
        var senderEmail = _configuration["SendGridSettings:SenderEmail"] ?? throw new InvalidOperationException("SendGrid sender email not configured");
        var senderName = _configuration["SendGridSettings:SenderName"] ?? "Auth Service";
        var appUrl = _configuration["AppUrl"] ?? throw new InvalidOperationException("AppUrl not configured");

        var resetLink = $"{appUrl}/reset-password?token={token}";

        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(senderEmail, senderName);
        var to = new EmailAddress(toEmail);
        var subject = "Reset Your Password";
        var htmlContent = $@"
            <h1>Password Reset</h1>
            <p>Click the link below to reset your password:</p>
            <a href='{resetLink}'>Reset Password</a>
            <p>Or copy and paste this link: {resetLink}</p>
            <p>This link expires in 1 hour.</p>
            <p>If you didn't request this, please ignore this email.</p>
        ";

        var msg = new SendGridMessage()
        {
            From = from,
            Subject = subject,
            HtmlContent = htmlContent
        };

        msg.AddTo(to);

        try
        {
            var response = await client.SendEmailAsync(msg);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Password reset email sent to {toEmail}");
            }
            else
            {
                _logger.LogError($"Failed to send password reset email to {toEmail}: {response.StatusCode}");
                throw new InvalidOperationException($"SendGrid returned status {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send password reset email to {toEmail}");
            throw;
        }
    }
}
