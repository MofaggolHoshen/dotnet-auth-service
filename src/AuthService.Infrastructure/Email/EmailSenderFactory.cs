using AuthService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Infrastructure.Email;

public class EmailSenderFactory
{
    public static IEmailSender Create(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var emailProvider = configuration["EmailSettings:Provider"]?.ToLower() ?? "stub";

        return emailProvider switch
        {
            "smtp" => serviceProvider.GetRequiredService<SmtpEmailSender>(),
            "sendgrid" => serviceProvider.GetRequiredService<SendGridEmailSender>(),
            "stub" => serviceProvider.GetRequiredService<StubEmailSender>(),
            _ => throw new InvalidOperationException($"Unknown email provider: {emailProvider}")
        };
    }
}
