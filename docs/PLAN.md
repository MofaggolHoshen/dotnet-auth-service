# Authentication Service Implementation Plan

**Project:** dotnet-auth-service  
**Framework:** .NET 9 | **Database:** SQL Server | **Testing:** xUnit  
**Status:** In Progress  
**Last Updated:** 2026-05-29

---

## Overview

Building a production-ready ASP.NET Core 9 authentication service with:
- ✅ User Registration & Login
- ✅ JWT + Refresh Token Management
- ✅ Email Verification
- ✅ Password Reset (Forgot Password)
- ✅ Configurable Email Providers (SMTP, SendGrid, Stub)

---

## Project Structure

```
dotnet-auth-service/
├── docs/                              # 📋 Documentation
├── src/                               # 💻 Source Code
│   ├── AuthService.Domain/            # Domain layer (entities)
│   ├── AuthService.Application/       # Application layer (DTOs, services)
│   ├── AuthService.Infrastructure/    # Infrastructure layer (DB, email)
│   └── AuthService.API/               # API layer (controllers, middleware)
├── tests/                             # 🧪 Tests
│   ├── AuthService.UnitTests/
│   └── AuthService.IntegrationTests/
├── AuthService.sln
└── README.md
```

---

## Implementation Phases

### Phase 1: Project Setup & Foundation
**Status:** 🔄 IN PROGRESS

- [ ] **scaffold-solution** - Create solution and all projects with proper references
- [ ] **domain-entities** - Define User, RefreshToken entities and UserStatus enum
- [x] **planning** - Create comprehensive plan with architecture decisions

### Phase 2: Application Core
**Status:** ⏳ PENDING

- [ ] **application-layer** - Create interfaces, DTOs, validators, and AuthService
  - Interfaces: IAuthService, ITokenService, IEmailSender, IUserRepository
  - DTOs: LoginRequest, RegisterRequest, RefreshTokenRequest, etc.
  - Validators: FluentValidation for all requests
  - Services: AuthService with business logic

### Phase 3: Infrastructure & Persistence
**Status:** ⏳ PENDING

- [ ] **infrastructure-persistence** - EF Core 9 + SQL Server setup
  - AppDbContext configuration
  - UserRepository implementation
  - EF Core migrations
  
- [ ] **infrastructure-identity** - JWT Token Management
  - TokenService: Generate/Validate JWT access tokens
  - Refresh token generation and validation
  - Token revocation
  
- [ ] **infrastructure-email** - Configurable Email Providers
  - SmtpEmailSender (SMTP via MailKit)
  - SendGridEmailSender (SendGrid API)
  - StubEmailSender (Development/Testing)
  - EmailSenderFactory for provider selection

### Phase 4: API & Controllers
**Status:** ⏳ PENDING

- [ ] **api-controller** - AuthController with 6 endpoints
  - POST /api/auth/register
  - POST /api/auth/login
  - POST /api/auth/refresh-token
  - POST /api/auth/verify-email
  - POST /api/auth/forgot-password
  - POST /api/auth/reset-password
  
- [ ] **api-middleware** - Global setup
  - ExceptionHandlingMiddleware
  - JWT authentication configuration
  - Dependency injection registration
  - Program.cs configuration

### Phase 5: Testing & Quality Assurance
**Status:** ⏳ PENDING

- [ ] **unit-tests** - Business logic testing
  - AuthService method tests (xUnit + Moq)
  - Validator tests
  - TokenService tests
  
- [ ] **integration-tests** - End-to-end API testing
  - AuthController endpoint tests
  - Database integration tests
  - WebApplicationFactory setup

### Phase 6: Documentation & Finalization
**Status:** ⏳ PENDING

- [ ] **docs** - Comprehensive documentation
  - README with setup instructions
  - Environment variables reference
  - API endpoint documentation
  - Architecture diagrams

---

## Configuration (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=AuthServiceDb;Trusted_Connection=true;Encrypt=false;"
  },
  "Jwt": {
    "Secret": "your-secret-key-min-32-chars-required",
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

**Provider Options:**
- `"Smtp"` - SMTP via MailKit
- `"SendGrid"` - SendGrid API
- `"Stub"` - Development/Testing (logs to console)

---

## API Endpoints

| HTTP | Endpoint | Description | Auth Required |
|------|----------|-------------|----------------|
| POST | `/api/auth/register` | Register new user + send verification email | No |
| POST | `/api/auth/login` | Login with email & password → JWT + refresh token | No |
| POST | `/api/auth/refresh-token` | Get new access token using refresh token | No |
| POST | `/api/auth/verify-email` | Verify email with token | No |
| POST | `/api/auth/forgot-password` | Request password reset email | No |
| POST | `/api/auth/reset-password` | Reset password with token | No |

---

## Key Technologies & Packages

### Domain & Application
- **FluentValidation** - Request validation
- **BCrypt.Net-Next** - Password hashing

### Infrastructure
- **Entity Framework Core 9** - ORM
- **Microsoft.Data.SqlClient** - SQL Server driver
- **System.IdentityModel.Tokens.Jwt** - JWT creation
- **MailKit** - SMTP email sending
- **SendGrid** - SendGrid API integration

### Testing
- **xUnit** - Test framework
- **Moq** - Mocking library
- **FluentAssertions** - Assertion library
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing

---

## Implementation Notes

### Database Schema

**Users Table**
```sql
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Email NVARCHAR(255) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    IsEmailVerified BIT NOT NULL DEFAULT 0,
    EmailVerificationToken NVARCHAR(MAX),
    PasswordResetToken NVARCHAR(MAX),
    PasswordResetTokenExpiry DATETIME2,
    Status INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);
```

**RefreshTokens Table**
```sql
CREATE TABLE RefreshTokens (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Token NVARCHAR(MAX) NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    IsRevoked BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
```

### Security Considerations
- ✅ Passwords hashed with BCrypt
- ✅ JWT tokens signed with secret key
- ✅ Refresh tokens stored in database
- ✅ Email verification tokens single-use
- ✅ Password reset tokens expiring
- ✅ HTTPS enforcement (configure in production)
- ✅ CORS configuration (configure per environment)

### Error Handling
- Custom exception types (AuthException, ValidationException)
- Global exception middleware for consistent responses
- Problem Details RFC 7807 compliant responses

---

## Progress Tracking

| Phase | Tasks | Completed | Status |
|-------|-------|-----------|--------|
| 1 | 3 | 1 | 🔄 In Progress |
| 2 | 1 | 0 | ⏳ Pending |
| 3 | 3 | 0 | ⏳ Pending |
| 4 | 2 | 0 | ⏳ Pending |
| 5 | 2 | 0 | ⏳ Pending |
| 6 | 1 | 0 | ⏳ Pending |
| **TOTAL** | **12** | **1** | **8%** |

---

## Next Steps

1. ✅ Complete `scaffold-solution` - Create all projects and add NuGet packages
2. Complete `domain-entities` - Define core entities
3. Complete `application-layer` - Implement business logic interfaces and services
4. Continue with infrastructure layer components
5. Implement API controllers and middleware
6. Write comprehensive unit and integration tests
7. Create detailed API documentation

---

## Checklist for Success

- [ ] All projects created with correct references
- [ ] Database migrations run successfully
- [ ] All 6 API endpoints working end-to-end
- [ ] Unit tests passing (>80% coverage)
- [ ] Integration tests passing
- [ ] Email sending verified (all 3 providers)
- [ ] Token refresh working correctly
- [ ] Password reset flow complete
- [ ] Email verification flow complete
- [ ] Documentation complete and accurate

---

*For detailed implementation progress, see individual phase documentation files.*
