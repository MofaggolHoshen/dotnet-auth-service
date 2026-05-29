# Phase 3: Infrastructure & Persistence Implementation Plan

**Framework:** Entity Framework Core 9 | **Database:** SQL Server | **Providers:** JWT, SMTP, SendGrid, Stub  
**Status:** PLANNING  
**Estimated LOC:** 1,500+

---

## Overview

Phase 3 implements the infrastructure layer connecting the application core to external systems:

- **Data Persistence:** EF Core 9 + SQL Server with AppDbContext and UserRepository
- **Token Management:** JWT generation, validation, and refresh token handling
- **Email Delivery:** Multiple configurable providers (SMTP, SendGrid, Stub)
- **Dependency Injection:** Wire up all services in Program.cs

---

## Task Breakdown

### Task 3.1: Infrastructure Setup & AppDbContext

**Files to Create:**

- `src/AuthService.Infrastructure/Data/AppDbContext.cs` (150 lines)
- Database migrations (auto-generated)

**Key Features:**

- DbSet<User> Users
- DbSet<RefreshToken> RefreshTokens
- Fluent API configuration for indexes, constraints
- Shadow properties for audit timestamps
- SQL Server provider configuration

---

### Task 3.2: UserRepository Implementation

**Files to Create:**

- `src/AuthService.Infrastructure/Repositories/UserRepository.cs` (250 lines)

**Methods to Implement:**

- GetByIdAsync(Guid id)
- GetByEmailAsync(string email)
- GetByEmailVerificationTokenAsync(string token)
- GetByPasswordResetTokenAsync(string token)
- AddAsync(User user)
- UpdateAsync(User user)
- ExistsByEmailAsync(string email)

---

### Task 3.3: TokenService Implementation

**Files to Create:**

- `src/AuthService.Infrastructure/Services/TokenService.cs` (300 lines)

**Methods to Implement:**

- GenerateAccessToken(User user, int expiryMinutes)
- GenerateRefreshToken()
- ValidateToken(string token)
- RevokeTokenAsync(string token)
- IsTokenRevokedAsync(string token)

**Key Features:**

- JWT signing with HS256
- Claims extraction (UserId, Email)
- Token expiry validation
- Signature verification

---

### Task 3.4: Email Provider Implementations

**Files to Create:**

- `src/AuthService.Infrastructure/Email/SmtpEmailSender.cs` (150 lines)
- `src/AuthService.Infrastructure/Email/SendGridEmailSender.cs` (120 lines)
- `src/AuthService.Infrastructure/Email/StubEmailSender.cs` (80 lines)
- `src/AuthService.Infrastructure/Email/EmailSenderFactory.cs` (100 lines)

**Providers:**

1. **SmtpEmailSender** - SMTP via MailKit
2. **SendGridEmailSender** - SendGrid API
3. **StubEmailSender** - Console logging (dev/test)
4. **Factory** - Dynamic provider selection

---

### Task 3.5: Dependency Injection Configuration

**Files to Modify:**

- `src/AuthService.API/Program.cs` - Add infrastructure services

**Services to Register:**

- AppDbContext with SQL Server
- UserRepository
- TokenService
- IEmailSender (via factory)
- FluentValidation validators

---

## Database Schema

### Users Table

```sql
CREATE TABLE [Users] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [Email] NVARCHAR(255) NOT NULL UNIQUE,
    [PasswordHash] NVARCHAR(MAX) NOT NULL,
    [IsEmailVerified] BIT NOT NULL DEFAULT 0,
    [EmailVerificationToken] NVARCHAR(MAX),
    [PasswordResetToken] NVARCHAR(MAX),
    [PasswordResetTokenExpiry] DATETIME2,
    [Status] INT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL,
    [UpdatedAt] DATETIME2 NOT NULL
);
```

### RefreshTokens Table

```sql
CREATE TABLE [RefreshTokens] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [Token] NVARCHAR(MAX) NOT NULL,
    [ExpiresAt] DATETIME2 NOT NULL,
    [IsRevoked] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL,
    FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
);
```

---

## Configuration

**appsettings.json:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=AuthServiceDb;Trusted_Connection=true;Encrypt=false;"
  },
  "Jwt": {
    "Secret": "your-very-long-secret-key-minimum-32-characters",
    "Issuer": "AuthService",
    "Audience": "AuthServiceClients",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "Email": {
    "Provider": "Smtp",
    "Smtp": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "Username": "your-email@gmail.com",
      "Password": "your-app-password",
      "From": "noreply@authservice.com"
    },
    "SendGrid": {
      "ApiKey": "SG.your-sendgrid-key",
      "From": "noreply@authservice.com"
    }
  }
}
```

---

## NuGet Dependencies

- Entity Framework Core 9.0.0
- Microsoft.Data.SqlClient
- Microsoft.EntityFrameworkCore.SqlServer
- System.IdentityModel.Tokens.Jwt
- MailKit (for SMTP)
- SendGrid (for SendGrid)

---

## Files Summary

| File                   | Lines | Purpose               |
| ---------------------- | ----- | --------------------- |
| AppDbContext.cs        | 150   | EF Core configuration |
| UserRepository.cs      | 250   | Data access           |
| TokenService.cs        | 300   | JWT operations        |
| SmtpEmailSender.cs     | 150   | SMTP provider         |
| SendGridEmailSender.cs | 120   | SendGrid provider     |
| StubEmailSender.cs     | 80    | Development provider  |
| EmailSenderFactory.cs  | 100   | Provider factory      |
| Program.cs (modified)  | +50   | DI configuration      |

**Total:** 1,500+ lines of code

---

## Success Criteria

- [ ] Solution builds with 0 errors
- [ ] Migrations run successfully
- [ ] All repository methods work
- [ ] TokenService generates valid JWTs
- [ ] Email providers functional
- [ ] Dependency injection configured
- [ ] Infrastructure unit tests pass
