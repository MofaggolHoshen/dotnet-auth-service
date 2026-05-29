using AuthService.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailVerificationAsync(string toEmail, string token)
    {
        var smtpSettings = _configuration.GetSection("SmtpSettings");
        var verificationLink = $"{_configuration["AppUrl"]}/verify-email?token={token}";

        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress(
            smtpSettings["SenderName"],
            smtpSettings["SenderEmail"]
        ));
        emailMessage.To.Add(new MailboxAddress("User", toEmail));
        emailMessage.Subject = "Verify Your Email Address";
        emailMessage.Body = new TextPart("html")
        {
            Text = $@"
                <h1>Email Verification</h1>
                <p>Please click the link below to verify your email address:</p>
                <a href='{verificationLink}'>Verify Email</a>
                <p>Or copy and paste this link: {verificationLink}</p>
                <p>This link expires in 24 hours.</p>
            "
        };

        try
        {
            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(
                    smtpSettings["Host"],
                    int.Parse(smtpSettings["Port"] ?? "587"),
                    SecureSocketOptions.StartTls
                );

                await client.AuthenticateAsync(
                    smtpSettings["Username"],
                    smtpSettings["Password"]
                );

                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }

            _logger.LogInformation($"Verification email sent to {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send verification email to {toEmail}");
            throw;
        }
    }

    public async Task SendPasswordResetAsync(string toEmail, string token)
    {
        var smtpSettings = _configuration.GetSection("SmtpSettings");
        var resetLink = $"{_configuration["AppUrl"]}/reset-password?token={token}";

        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress(
            smtpSettings["SenderName"],
            smtpSettings["SenderEmail"]
        ));
        emailMessage.To.Add(new MailboxAddress("User", toEmail));
        emailMessage.Subject = "Reset Your Password";
        emailMessage.Body = new TextPart("html")
        {
            Text = $@"
                <h1>Password Reset</h1>
                <p>Click the link below to reset your password:</p>
                <a href='{resetLink}'>Reset Password</a>
                <p>Or copy and paste this link: {resetLink}</p>
                <p>This link expires in 1 hour.</p>
                <p>If you didn't request this, please ignore this email.</p>
            "
        };

        try
        {
            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(
                    smtpSettings["Host"],
                    int.Parse(smtpSettings["Port"] ?? "587"),
                    SecureSocketOptions.StartTls
                );

                await client.AuthenticateAsync(
                    smtpSettings["Username"],
                    smtpSettings["Password"]
                );

                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }

            _logger.LogInformation($"Password reset email sent to {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send password reset email to {toEmail}");
            throw;
        }
    }
}
