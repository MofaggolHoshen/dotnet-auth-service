# Phase 3: Infrastructure & Persistence Layer

**Overview:** Implementation of the infrastructure layer including Entity Framework Core, database persistence, JWT token management, and email provider integrations.

**Duration:** 3-4 days | **Team Size:** 1-2 developers | **Status:** READY FOR IMPLEMENTATION

---

## What Gets Built

### 1. Database Layer (EF Core 9)

- **AppDbContext** - Entity Framework Core database context configuration
- **Database Migrations** - Users and RefreshTokens table creation
- **Data Annotations** - Entity configurations for constraints and indexes

### 2. Data Access Layer

- **UserRepository** - CRUD operations and custom queries
- **Generic Repository** - Base repository pattern implementation
- **Query Optimization** - Indexes, query filtering, async operations

### 3. Token Management

- **TokenService** - JWT generation and validation
- **Refresh Token Management** - Token creation, validation, revocation
- **Claims Processing** - Extract and validate token claims

### 4. Email Delivery

- **SMTP Provider** - MailKit integration for SMTP email
- **SendGrid Provider** - SendGrid API integration
- **Stub Provider** - Development/testing email logger
- **Email Factory** - Provider selection and instantiation

### 5. Dependency Injection

- **Service Registration** - Register all infrastructure services
- **Configuration Loading** - Load JWT and email settings
- **Provider Factory** - Dynamic email provider selection

---

## File Structure

```
src/AuthService.Infrastructure/
├── Data/
│   ├── AppDbContext.cs
│   └── Migrations/
│       ├── [timestamp]_InitialCreate.cs
│       ├── [timestamp]_InitialCreate.Designer.cs
│       └── AppDbContextModelSnapshot.cs
│
├── Repositories/
│   ├── UserRepository.cs
│   └── IGenericRepository.cs
│
├── Services/
│   └── TokenService.cs
│
├── Email/
│   ├── SmtpEmailSender.cs
│   ├── SendGridEmailSender.cs
│   ├── StubEmailSender.cs
│   └── EmailSenderFactory.cs
│
├── Configuration/
│   ├── JwtSettings.cs
│   ├── EmailSettings.cs
│   └── SmtpSettings.cs
│
└── Infrastructure.csproj
```

---

## Phase 3 Tasks

### Task 3.1: AppDbContext & Migrations (150 LOC)

- [ ] Create AppDbContext class
- [ ] Configure User entity
- [ ] Configure RefreshToken entity
- [ ] Set up relationships and constraints
- [ ] Create initial database migration
- [ ] Test migration rollback/forward

### Task 3.2: UserRepository (250 LOC)

- [ ] Implement IUserRepository interface
- [ ] GetByIdAsync method
- [ ] GetByEmailAsync method
- [ ] GetByEmailVerificationTokenAsync method
- [ ] GetByPasswordResetTokenAsync method
- [ ] AddAsync method
- [ ] UpdateAsync method
- [ ] ExistsByEmailAsync method

### Task 3.3: TokenService (300 LOC)

- [ ] Implement ITokenService interface
- [ ] GenerateAccessToken method
- [ ] GenerateRefreshToken method
- [ ] ValidateToken method
- [ ] RevokeTokenAsync method
- [ ] IsTokenRevokedAsync method
- [ ] Claims extraction and validation

### Task 3.4: Email Providers (450 LOC)

- [ ] Implement SmtpEmailSender
- [ ] Implement SendGridEmailSender
- [ ] Implement StubEmailSender
- [ ] Create EmailSenderFactory
- [ ] Configuration binding for SMTP
- [ ] Configuration binding for SendGrid

### Task 3.5: Dependency Injection (50 LOC)

- [ ] Register DbContext in Program.cs
- [ ] Register UserRepository
- [ ] Register TokenService
- [ ] Register email provider factory
- [ ] Load and validate JWT settings
- [ ] Load and validate email settings

---

## Success Metrics

- ✅ Solution compiles with 0 errors
- ✅ Database migrations run successfully
- ✅ UserRepository CRUD operations work
- ✅ TokenService generates valid JWT tokens
- ✅ Email sending works (or stubs correctly)
- ✅ Dependency injection configured
- ✅ All async operations functioning
- ✅ Connection pooling active

---

## Key Technologies

| Technology                      | Version | Purpose                  |
| ------------------------------- | ------- | ------------------------ |
| Entity Framework Core           | 9.0.0   | ORM and database access  |
| Microsoft.Data.SqlClient        | Latest  | SQL Server driver        |
| System.IdentityModel.Tokens.Jwt | Latest  | JWT token handling       |
| MailKit                         | Latest  | SMTP email sending       |
| SendGrid                        | Latest  | SendGrid API integration |
| BCrypt.Net-Next                 | 4.0.3   | Password hashing         |

---

## Critical Dependencies

- ✅ Phase 1 (Domain entities User, RefreshToken)
- ✅ Phase 2 (DTOs, validators, AuthService interface)
- ❌ Phase 4 (API controllers) - Not yet

---

## Configuration Schema

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

---

## Implementation Checklist

**Database Setup**

- [ ] AppDbContext created
- [ ] Entities configured with Fluent API
- [ ] Indexes created (Email unique)
- [ ] Foreign key relationships configured
- [ ] Cascade delete configured

**UserRepository**

- [ ] Interface fully implemented
- [ ] All methods async/await
- [ ] Query optimization applied
- [ ] Error handling in place
- [ ] Connection pooling working

**TokenService**

- [ ] JWT generation working
- [ ] Token validation working
- [ ] Claims extraction working
- [ ] Token expiry enforced
- [ ] Signature verification working

**Email Providers**

- [ ] SMTP provider functional
- [ ] SendGrid provider functional
- [ ] Stub provider logging correctly
- [ ] Factory selecting correct provider
- [ ] Configuration loading correctly

**Dependency Injection**

- [ ] All services registered
- [ ] Configuration bound correctly
- [ ] Factory working in DI container
- [ ] No circular dependencies
- [ ] All interfaces resolved

---

## Testing Strategy

**Manual Testing**

- [ ] Create test user through AuthService
- [ ] Verify user stored in database
- [ ] Generate JWT token
- [ ] Validate token
- [ ] Test token expiry
- [ ] Test refresh token
- [ ] Test email sending

**Test Data**

- [ ] Test user email: test@example.com
- [ ] Test password: SecurePass123!
- [ ] Verification token format: Guid
- [ ] Reset token format: Guid

---

## Common Issues & Solutions

| Issue                     | Cause            | Solution                        |
| ------------------------- | ---------------- | ------------------------------- |
| Migration fails           | DB doesn't exist | Run `dotnet ef database create` |
| JWT secret too short      | Config error     | Use min 32 characters           |
| Connection string invalid | Configuration    | Verify SQL Server running       |
| Email send fails          | SMTP config      | Check credentials, port 587     |
| DI registration fails     | Missing service  | Check Program.cs configuration  |

---

## Next Phase Dependency

Phase 4 (API Controllers) depends on:

- ✅ UserRepository functional
- ✅ TokenService generating valid JWTs
- ✅ Email provider working
- ✅ All DI services registered

**Do not start Phase 4 until Phase 3 is verified working.**
