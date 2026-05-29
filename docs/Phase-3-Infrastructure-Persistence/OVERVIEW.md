# Phase 3: Overview & Summary

Quick reference for Phase 3 infrastructure implementation.

---

## Phase 3 at a Glance

**What:** Build the data persistence and token management layer  
**Why:** Foundation for API layer (Phase 4)  
**Duration:** 3-4 days  
**Team:** 1-2 developers  
**Status:** READY FOR IMPLEMENTATION

---

## Deliverables Summary

### 1. Database Layer

- AppDbContext with EF Core 9
- Users and RefreshTokens tables
- Migrations for schema creation
- Indexes and constraints

### 2. Data Access

- UserRepository with 7 methods
- CRUD operations
- Custom queries (by email, by token)
- EF Core best practices

### 3. Token Management

- TokenService with JWT operations
- Access token generation (15 min expiry)
- Refresh token generation (7 day expiry)
- Token validation and claims extraction

### 4. Email Integration

- SMTP provider (MailKit)
- SendGrid provider (SendGrid API)
- Stub provider (development/testing)
- Dynamic provider selection via factory

### 5. Dependency Injection

- Service registration in Program.cs
- Configuration binding and validation
- Factory pattern for email providers
- All async/await setup

---

## Files Created: 8

| File                   | Lines | Purpose                |
| ---------------------- | ----- | ---------------------- |
| AppDbContext.cs        | 150   | EF Core configuration  |
| UserRepository.cs      | 250   | Data access operations |
| TokenService.cs        | 300   | JWT token operations   |
| SmtpEmailSender.cs     | 150   | SMTP email delivery    |
| SendGridEmailSender.cs | 120   | SendGrid integration   |
| StubEmailSender.cs     | 80    | Development stub       |
| EmailSenderFactory.cs  | 100   | Provider selection     |
| Program.cs (modified)  | +50   | DI configuration       |

**Total:** ~1,500 lines of code

---

## Task Sequence

```
1. AppDbContext
   └─ Create tables (Users, RefreshTokens)

2. Migrations
   └─ Apply schema to database

3. UserRepository
   └─ CRUD operations on User entity

4. TokenService
   └─ JWT generation and validation

5. Email Providers
   └─ SMTP, SendGrid, Stub implementations

6. Factory & DI
   └─ Wire everything together in Program.cs
```

---

## Technologies

| Technology                      | Version | Purpose      |
| ------------------------------- | ------- | ------------ |
| .NET 9 SDK                      | 9.0+    | Runtime      |
| EF Core                         | 9.0.0   | ORM          |
| SQL Server                      | 2019+   | Database     |
| System.IdentityModel.Tokens.Jwt | Latest  | JWT          |
| MailKit                         | Latest  | SMTP         |
| SendGrid                        | Latest  | SendGrid API |

---

## Configuration Example

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=AuthServiceDb;Trusted_Connection=true;"
  },
  "Jwt": {
    "Secret": "your-32-character-minimum-secret-key",
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
    }
  }
}
```

---

## Prerequisites

- Phase 1 complete (Domain entities)
- Phase 2 complete (DTOs, validators, AuthService)
- .NET 9 SDK installed
- SQL Server (Express/LocalDB) running
- Database migration tools configured

---

## Success Criteria

✅ All 8 files created  
✅ Solution builds (0 errors)  
✅ Database migrates successfully  
✅ UserRepository methods work  
✅ TokenService generates valid JWTs  
✅ Email providers functional  
✅ DI container configured  
✅ No runtime errors

---

## Testing Strategy

**Manual Testing:**

- Create test user via repository
- Generate JWT token
- Validate token
- Test token expiry
- Send test email
- Refresh access token

**Automated Testing:**

- Unit tests for TokenService
- Integration tests for repository
- Email provider mocking
- JWT claim validation

---

## Critical Points

⚠️ **JWT Secret:** Minimum 32 characters  
⚠️ **SQL Connection:** Verify SQL Server running  
⚠️ **Email Config:** SMTP credentials required  
⚠️ **Migrations:** Run before using repository  
⚠️ **Async/Await:** All I/O is async

---

## Next Phase Dependency

Phase 4 (API Controllers) requires:

- ✅ UserRepository working
- ✅ TokenService generating JWTs
- ✅ Email provider operational
- ✅ DI services registered

**Do NOT start Phase 4 until Phase 3 is verified.**

---

## Documentation Index

1. **README.md** (this file) - Overview
2. **IMPLEMENTATION-GUIDE.md** - Step-by-step code
3. **TECHNICAL-SPECS.md** - Detailed specifications
4. **ARCHITECTURE-DECISIONS.md** - Design rationale
5. **VISUAL-GUIDE.md** - Diagrams and flowcharts
6. **INDEX.md** - Navigation and quick links
