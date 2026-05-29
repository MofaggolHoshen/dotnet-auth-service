# Phase 3: Implementation Guide

Step-by-step code implementation reference for the infrastructure layer.

---

## Implementation Order

1. AppDbContext (foundation for everything)
2. Migrations (create database schema)
3. UserRepository (data access)
4. TokenService (JWT operations)
5. Email Providers (SMTP, SendGrid, Stub)
6. Configuration & DI (wire everything together)

---

## Task 3.1: AppDbContext Implementation

### File: `src/AuthService.Infrastructure/Data/AppDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using AuthService.Domain.Entities;

namespace AuthService.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Configuration
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);

            modelBuilder.Entity<User>()
                .Property(u => u.PasswordHash)
                .IsRequired();

            // RefreshToken Configuration
            modelBuilder.Entity<RefreshToken>()
                .HasKey(rt => rt.Id);

            modelBuilder.Entity<RefreshToken>()
                .HasOne<User>()
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
```

### Create Migration

```bash
dotnet ef migrations add InitialCreate --project src/AuthService.Infrastructure
dotnet ef database update --project src/AuthService.Infrastructure
```

---

## Task 3.2: UserRepository Implementation

### File: `src/AuthService.Infrastructure/Repositories/UserRepository.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Data;

namespace AuthService.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByEmailVerificationTokenAsync(string token)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
        }

        public async Task<User?> GetByPasswordResetTokenAsync(string token)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.PasswordResetToken == token);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email == email);
        }
    }
}
```

---

## Task 3.3: TokenService Implementation

### File: `src/AuthService.Infrastructure/Services/TokenService.cs`

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using AuthService.Application.Interfaces;
using AuthService.Application.DTOs;
using AuthService.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace AuthService.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly string _jwtSecret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenExpiryMinutes;
        private readonly int _refreshTokenExpiryDays;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
            _jwtSecret = _configuration["Jwt:Secret"]
                ?? throw new InvalidOperationException("JWT Secret not configured");
            _issuer = _configuration["Jwt:Issuer"] ?? "AuthService";
            _audience = _configuration["Jwt:Audience"] ?? "AuthServiceClients";
            _accessTokenExpiryMinutes = int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15");
            _refreshTokenExpiryDays = int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "7");

            if (_jwtSecret.Length < 32)
                throw new InvalidOperationException("JWT Secret must be at least 32 characters");
        }

        public string GenerateAccessToken(User user, int expiryMinutes = 0)
        {
            expiryMinutes = expiryMinutes > 0 ? expiryMinutes : _accessTokenExpiryMinutes;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email)
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public AuthToken GenerateRefreshToken()
        {
            return new AuthToken
            {
                RefreshToken = Guid.NewGuid().ToString("N"),
                ExpiresInMinutes = _refreshTokenExpiryDays * 24 * 60
            };
        }

        public ClaimsPrincipal ValidateToken(string token)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out _);

                return principal;
            }
            catch
            {
                return null;
            }
        }

        public async Task RevokeTokenAsync(string token)
        {
            // Implementation depends on token revocation strategy
            // For now, store revoked tokens in database or cache
            await Task.CompletedTask;
        }

        public async Task<bool> IsTokenRevokedAsync(string token)
        {
            // Check if token is revoked
            return await Task.FromResult(false);
        }
    }
}
```

---

## Task 3.4: Email Providers Implementation

### File: `src/AuthService.Infrastructure/Email/SmtpEmailSender.cs`

```csharp
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using AuthService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            try
            {
                var smtpHost = _configuration["Email:Smtp:Host"];
                var smtpPort = int.Parse(_configuration["Email:Smtp:Port"] ?? "587");
                var smtpUsername = _configuration["Email:Smtp:Username"];
                var smtpPassword = _configuration["Email:Smtp:Password"];
                var fromEmail = _configuration["Email:Smtp:From"];

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Auth Service", fromEmail));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = body };

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpUsername, smtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent to {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                throw;
            }
        }
    }
}
```

### File: `src/AuthService.Infrastructure/Email/SendGridEmailSender.cs`

```csharp
using SendGrid;
using SendGrid.Helpers.Mail;
using AuthService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Email
{
    public class SendGridEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SendGridEmailSender> _logger;

        public SendGridEmailSender(IConfiguration configuration, ILogger<SendGridEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            try
            {
                var apiKey = _configuration["Email:SendGrid:ApiKey"];
                var fromEmail = _configuration["Email:SendGrid:From"];

                var client = new SendGridClient(apiKey);
                var from = new EmailAddress(fromEmail, "Auth Service");
                var toEmail = new EmailAddress(to);
                var htmlContent = body;

                var msg = MailHelper.CreateSingleEmail(from, toEmail, subject, null, htmlContent);
                var response = await client.SendEmailAsync(msg);

                _logger.LogInformation("Email sent to {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                throw;
            }
        }
    }
}
```

### File: `src/AuthService.Infrastructure/Email/StubEmailSender.cs`

```csharp
using AuthService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Email
{
    public class StubEmailSender : IEmailSender
    {
        private readonly ILogger<StubEmailSender> _logger;

        public StubEmailSender(ILogger<StubEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string to, string subject, string body)
        {
            _logger.LogInformation("STUB: Email to {To}, Subject: {Subject}", to, subject);
            _logger.LogInformation("STUB: Body: {Body}", body);
            return Task.CompletedTask;
        }
    }
}
```

### File: `src/AuthService.Infrastructure/Email/EmailSenderFactory.cs`

```csharp
using AuthService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Email
{
    public static class EmailSenderFactory
    {
        public static IEmailSender CreateEmailSender(
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            var provider = configuration["Email:Provider"];
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>();

            return provider switch
            {
                "Smtp" => new SmtpEmailSender(configuration, logger.CreateLogger<SmtpEmailSender>()),
                "SendGrid" => new SendGridEmailSender(configuration, logger.CreateLogger<SendGridEmailSender>()),
                "Stub" => new StubEmailSender(logger.CreateLogger<StubEmailSender>()),
                _ => throw new InvalidOperationException($"Unknown email provider: {provider}")
            };
        }
    }
}
```

---

## Task 3.5: Dependency Injection Configuration

### File: `src/AuthService.API/Program.cs` (modifications)

```csharp
// Add these using statements
using AuthService.Infrastructure.Data;
using AuthService.Infrastructure.Repositories;
using AuthService.Infrastructure.Services;
using AuthService.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;

// In CreateBuilder section:
var builder = WebApplicationBuilder.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped(provider =>
    EmailSenderFactory.CreateEmailSender(builder.Configuration, provider));

// Validate JWT configuration
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 32)
    throw new InvalidOperationException("JWT Secret must be at least 32 characters");
```

---

## Template: Environment Configuration

```bash
# .env or appsettings.Development.json
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=Server=.;Database=AuthServiceDb;Trusted_Connection=true;Encrypt=false;
Jwt__Secret=your-minimum-32-character-secret-key-here
Jwt__Issuer=AuthService
Jwt__Audience=AuthServiceClients
Jwt__AccessTokenExpiryMinutes=15
Jwt__RefreshTokenExpiryDays=7
Email__Provider=Smtp
Email__Smtp__Host=smtp.gmail.com
Email__Smtp__Port=587
Email__Smtp__Username=your-email@gmail.com
Email__Smtp__Password=your-app-password
Email__Smtp__From=noreply@authservice.com
```

---

## Testing Checklist

- [ ] AppDbContext created and migrated
- [ ] UserRepository CRUD operations verified
- [ ] TokenService generates valid JWT
- [ ] Token validation works
- [ ] Email sending works (or stubs correctly)
- [ ] Dependency injection configured
- [ ] No build errors
- [ ] All async operations functioning
