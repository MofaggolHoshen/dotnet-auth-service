# dotnet-auth-service

![Status](https://img.shields.io/badge/Status-✅%20Complete-brightgreen)
![Framework](https://img.shields.io/badge/Framework-.NET%209-blue)
![Database](https://img.shields.io/badge/Database-SQL%20Server-CC2927)
![Testing](https://img.shields.io/badge/Testing-xUnit-512BD4)

A **production-ready ASP.NET Core 9 authentication service** with JWT tokens, email verification, password reset, and configurable email providers.

## 🎯 Project Status: ✅ 100% Complete

All 6 implementation phases are **COMPLETED** with comprehensive documentation, full test coverage, and production-ready code.

| Phase   | Description                  | Status      |
| ------- | ---------------------------- | ----------- |
| Phase 1 | Project Setup & Foundation   | ✅ Complete |
| Phase 2 | Application Core             | ✅ Complete |
| Phase 3 | Infrastructure & Persistence | ✅ Complete |
| Phase 4 | API & Controllers            | ✅ Complete |
| Phase 5 | Testing & QA                 | ✅ Complete |
| Phase 6 | Documentation & Finalization | ✅ Complete |

---

## ✨ Features

- ✅ **User Registration & Login** - Email-based authentication with password hashing (BCrypt)
- ✅ **JWT + Refresh Tokens** - Secure token management with automatic refresh
- ✅ **Email Verification** - Verify user emails with single-use tokens
- ✅ **Password Reset** - Forgot password flow with time-limited reset tokens
- ✅ **Multiple Email Providers** - SMTP, SendGrid, or Stub (for development)
- ✅ **Global Exception Handling** - RFC 7807 compliant error responses
- ✅ **Input Validation** - FluentValidation on all requests
- ✅ **Unit & Integration Tests** - Comprehensive test coverage with xUnit & Moq

---

## 📋 API Endpoints

All endpoints are fully implemented and tested:

| Method | Endpoint                    | Description                                       | Auth Required |
| ------ | --------------------------- | ------------------------------------------------- | ------------- |
| POST   | `/api/auth/register`        | Register new user + send verification email       | ❌ No         |
| POST   | `/api/auth/login`           | Login with email & password → JWT + refresh token | ❌ No         |
| POST   | `/api/auth/refresh-token`   | Get new access token using refresh token          | ❌ No         |
| POST   | `/api/auth/verify-email`    | Verify email with token                           | ❌ No         |
| POST   | `/api/auth/forgot-password` | Request password reset email                      | ❌ No         |
| POST   | `/api/auth/reset-password`  | Reset password with token                         | ❌ No         |

---

## 🏗️ Project Structure

```
dotnet-auth-service/
├── docs/                              # 📋 Comprehensive documentation for all phases
│   ├── PLAN.md                        # Master implementation plan
│   ├── Phase-1-Project-Setup-and-Foundation/
│   ├── Phase-2-Application-Core/
│   ├── Phase-3-Infrastructure-Persistence/
│   ├── Phase-4-API-Controllers/
│   ├── Phase-5-Testing/
│   └── Phase-6-Finalization/
├── src/                               # 💻 Source Code
│   ├── AuthService.Domain/            # Domain entities & enums
│   ├── AuthService.Application/       # Application services, DTOs, validators
│   ├── AuthService.Infrastructure/    # EF Core, repositories, email providers
│   └── AuthService.API/               # Controllers, middleware, configuration
├── tests/                             # 🧪 Unit & Integration Tests
│   ├── AuthService.UnitTests/         # Service & validator tests
│   └── AuthService.IntegrationTests/  # API endpoint tests
├── AuthService.sln                    # Solution file
└── README.md                          # This file
```

---

## 🚀 Quick Start

### Prerequisites

- .NET 9 SDK or later
- SQL Server (local or remote)
- Visual Studio 2022, VS Code, or JetBrains Rider

### Installation

1. **Clone the repository**

   ```bash
   git clone https://github.com/MofaggolHoshen/dotnet-auth-service.git
   cd dotnet-auth-service
   ```

2. **Restore NuGet packages**

   ```bash
   dotnet restore
   ```

3. **Update database connection**
   Edit `src/AuthService.API/appsettings.json`:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=AuthServiceDb;Trusted_Connection=true;Encrypt=false;"
     },
     "JwtSettings": {
       "SecretKey": "your-secret-key-min-32-characters-required",
       "Issuer": "AuthService",
       "Audience": "AuthServiceClients",
       "AccessTokenExpiryMinutes": 15,
       "RefreshTokenExpiryDays": 7
     },
     "Email": {
       "Provider": "Stub"
     }
   }
   ```

4. **Apply database migrations**

   ```bash
   dotnet ef database update --project src/AuthService.Infrastructure --startup-project src/AuthService.API
   ```

5. **Build the solution**

   ```bash
   dotnet build
   ```

6. **Run the API**

   ```bash
   dotnet run --project src/AuthService.API
   ```

   The API will be available at `https://localhost:7000` (or specified port)

---

## ⚙️ Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=AuthServiceDb;Trusted_Connection=true;Encrypt=false;"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-min-32-characters-required",
    "Issuer": "AuthService",
    "Audience": "AuthServiceClients",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "Email": {
    "Provider": "Stub",
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

### Environment Variables

Supported email providers:

- `Stub` - Development/Testing (logs to console)
- `Smtp` - SMTP via MailKit (Gmail, Outlook, etc.)
- `SendGrid` - SendGrid API integration

---

## 🧪 Testing

All tests are implemented and passing:

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test -v normal

# Run specific test project
dotnet test tests/AuthService.UnitTests

# Run integration tests
dotnet test tests/AuthService.IntegrationTests
```

### Test Coverage

- **Unit Tests**: AuthService, Validators, TokenService, UserRepository, Middleware
- **Integration Tests**: All 6 API endpoints with real database interactions

---

## 🔐 Security Considerations

- ✅ Passwords hashed with **BCrypt** (not reversible)
- ✅ JWT tokens signed with **secret key** (configurable length)
- ✅ Refresh tokens stored in **database** (traceable & revocable)
- ✅ Email verification tokens are **single-use**
- ✅ Password reset tokens have **expiry times**
- ✅ Global exception handling prevents **information leakage**
- ✅ Input validation on **all requests**

### JWT Token Structure

```json
{
  "sub": "user-id",
  "email": "user@example.com",
  "iat": 1234567890,
  "exp": 1234568890,
  "iss": "AuthService",
  "aud": "AuthServiceClients"
}
```

---

## 🛠️ Technologies & Packages

### Framework & ORM

- **ASP.NET Core 9** - Web framework
- **Entity Framework Core 9** - ORM for data access
- **Microsoft.Data.SqlClient** - SQL Server driver

### Authentication & Security

- **System.IdentityModel.Tokens.Jwt** - JWT creation and validation
- **BCrypt.Net-Next** - Password hashing

### Email

- **MailKit** - SMTP email sending
- **SendGrid** - SendGrid API integration

### Validation & Testing

- **FluentValidation** - Input validation
- **xUnit** - Test framework
- **Moq** - Mocking library
- **FluentAssertions** - Fluent assertions for tests
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing

---

## 📚 Documentation

Comprehensive documentation is available in the `docs/` folder:

- **[PLAN.md](docs/PLAN.md)** - Master implementation plan with all phases
- **[Phase 1](docs/Phase-1-Project-Setup-and-Foundation/)** - Project setup & scaffolding
- **[Phase 2](docs/Phase-2-Application-Core/)** - Application layer implementation
- **[Phase 3](docs/Phase-3-Infrastructure-Persistence/)** - Infrastructure & database
- **[Phase 4](docs/Phase-4-API-Controllers/)** - API controllers & middleware
- **[Phase 5](docs/Phase-5-Testing/)** - Unit & integration tests
- **[Phase 6](docs/Phase-6-Finalization/)** - Documentation & finalization

---

## 📦 Database Schema

### Users Table

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

### RefreshTokens Table

```sql
CREATE TABLE RefreshTokens (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Token NVARCHAR(MAX) NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    IsRevoked BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
```

---

## 🔄 Request/Response Examples

### Register User

**Request:**

```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!"
}
```

**Response (201 Created):**

```json
{
  "success": true,
  "message": "Registration successful. Please verify your email.",
  "data": {
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "email": "user@example.com"
  }
}
```

### Login User

**Request:**

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```

**Response (200 OK):**

```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "abc123xyz789...",
    "expiresIn": 900
  }
}
```

---

## 📋 Checklist for Production

- [ ] Update `appsettings.json` with production database connection
- [ ] Set JWT `SecretKey` to a strong, random value (min 32 characters)
- [ ] Configure email provider (SMTP or SendGrid)
- [ ] Update CORS settings for your domain
- [ ] Enable HTTPS in production
- [ ] Set up database backups
- [ ] Configure logging and monitoring
- [ ] Review security settings in `Program.cs`
- [ ] Run all tests: `dotnet test`
- [ ] Deploy to your hosting environment

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 👨‍💻 Author

**MofaggolHoshen**

- GitHub: [@MofaggolHoshen](https://github.com/MofaggolHoshen)
- Repository: [dotnet-auth-service](https://github.com/MofaggolHoshen/dotnet-auth-service)

---

## 📞 Support

For issues, questions, or suggestions:

1. Check the [docs/](docs/) folder for detailed documentation
2. Open an issue on GitHub
3. Review the implementation in each phase folder

---

## 🎉 Project Completion Summary

✅ **All 18 tasks across 6 phases completed (100%)**

- 40+ source code files implemented
- 8+ test files with comprehensive coverage
- Complete documentation for all phases
- Production-ready error handling
- Secure authentication flow
- Multiple email provider support

**Build Status:** ✅ Succeeds with 0 errors  
**Test Status:** ✅ All tests passing  
**Code Quality:** ✅ Production-ready
