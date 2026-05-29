# Phase 3: Visual Guide - Diagrams & Flows

Visual representations of Phase 3 architecture and data flows.

---

## Architecture Layer Diagram

```
┌─────────────────────────────────────────────────────────┐
│              Phase 3: Infrastructure Layer              │
└─────────────────────────────────────────────────────────┘

Phase 2 (Application Core)
├── AuthService (business logic)
├── DTOs (request/response models)
├── Validators (FluentValidation)
└── Interfaces (service contracts)
        ↓ depends on ↓
┌─────────────────────────────────────────────────────────┐
│ Phase 3: INFRASTRUCTURE LAYER                           │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Services                Repositories                   │
│  ┌──────────────┐       ┌──────────────────────┐       │
│  │ TokenService│       │ UserRepository       │       │
│  ├──────────────┤       ├──────────────────────┤       │
│  │ • JWT Gen   │       │ • GetByIdAsync       │       │
│  │ • JWT Valid │       │ • GetByEmailAsync    │       │
│  │ • Claims    │       │ • AddAsync           │       │
│  └──────────────┘       │ • UpdateAsync       │       │
│                         │ • ExistsByEmailAsync│       │
│  Email Services        └──────────────────────┘       │
│  ┌──────────────┐                                     │
│  │SmtpEmailSend│       Database Context              │
│  │SendGridEmail│       ┌──────────────────────┐       │
│  │StubEmailSend│       │ AppDbContext         │       │
│  │EmailFactory │       ├──────────────────────┤       │
│  └──────────────┘       │ • DbSet<User>        │       │
│                         │ • DbSet<RefreshToken│       │
│  Configuration         │ • Migrations         │       │
│  ┌──────────────┐       └──────────────────────┘       │
│  │ JWT Settings │                                     │
│  │ Email Config │                                     │
│  └──────────────┘                                     │
│                                                         │
└─────────────────────────────────────────────────────────┘
        ↓ persists to ↓
┌─────────────────────────────────────────────────────────┐
│ Phase 1: DOMAIN LAYER                                   │
├─────────────────────────────────────────────────────────┤
│ • User (entity)          • RefreshToken (entity)       │
│ • UserStatus (enum)      • Domain Interfaces           │
└─────────────────────────────────────────────────────────┘
        ↓ stores in ↓
┌─────────────────────────────────────────────────────────┐
│ SQL SERVER DATABASE                                    │
├─────────────────────────────────────────────────────────┤
│ Users Table          RefreshTokens Table               │
│ • Id (PK)            • Id (PK)                         │
│ • Email (unique)     • UserId (FK)                     │
│ • PasswordHash       • Token                          │
│ • IsEmailVerified    • ExpiresAt                       │
│ • Tokens (verify)    • IsRevoked                       │
│ • Status             • CreatedAt                       │
│ • CreatedAt/UpdatedAt                                 │
└─────────────────────────────────────────────────────────┘
```

---

## Data Flow: User Registration

```
AuthService.RegisterAsync()
    │
    ├─→ UserRepository.ExistsByEmailAsync(email)
    │   └─→ AppDbContext.Users.AnyAsync()
    │       └─→ SQL: SELECT COUNT(*) FROM Users WHERE Email = @email
    │
    ├─→ BCrypt.HashPassword(password)
    │   └─→ 'SecurePass123!' → '$2a$11$...'
    │
    ├─→ Create User entity
    │   ├── Id: Guid.NewGuid()
    │   ├── Email: 'user@example.com'
    │   ├── PasswordHash: (BCrypt hashed)
    │   ├── EmailVerificationToken: Guid.NewGuid().ToString("N")
    │   └── CreatedAt: DateTime.UtcNow
    │
    ├─→ UserRepository.AddAsync(user)
    │   └─→ AppDbContext.Users.AddAsync(user)
    │       └─→ AppDbContext.SaveChangesAsync()
    │           └─→ SQL: INSERT INTO Users (...)
    │
    └─→ IEmailSender.SendAsync(email, "Verify Email", body)
        ├─→ SmtpEmailSender
        │   └─→ MailKit → SMTP Server
        ├─→ SendGridEmailSender
        │   └─→ SendGrid API
        └─→ StubEmailSender
            └─→ ILogger → Console
```

---

## Data Flow: User Login

```
AuthService.LoginAsync()
    │
    ├─→ UserRepository.GetByEmailAsync(email)
    │   └─→ SQL: SELECT * FROM Users WHERE Email = @email
    │
    ├─→ BCrypt.Verify(password, user.PasswordHash)
    │   └─→ true / false
    │
    ├─→ Check user.IsEmailVerified
    │   └─→ true / false
    │
    ├─→ TokenService.GenerateAccessToken(user)
    │   └─→ JWT Claims: [NameIdentifier, Email]
    │       └─→ Signed with secret key
    │           └─→ Expires in 15 minutes
    │
    ├─→ TokenService.GenerateRefreshToken()
    │   └─→ Guid.NewGuid().ToString("N")
    │       └─→ Create RefreshToken entity
    │           └─→ Expires in 7 days
    │
    ├─→ UserRepository.UpdateAsync(user)
    │   └─→ AppDbContext.RefreshTokens.AddAsync()
    │       └─→ SQL: INSERT INTO RefreshTokens (...)
    │
    └─→ Return AuthResponse with tokens
```

---

## Data Flow: Token Validation

```
TokenService.ValidateToken(jwtToken)
    │
    ├─→ JwtSecurityTokenHandler.ValidateToken()
    │   │
    │   ├─→ Verify Signature (HMAC-SHA256 + secret key)
    │   │   └─→ Valid / Invalid
    │   │
    │   ├─→ Verify Issuer (AuthService)
    │   │   └─→ Match / Mismatch
    │   │
    │   ├─→ Verify Audience (AuthServiceClients)
    │   │   └─→ Match / Mismatch
    │   │
    │   ├─→ Verify Expiry (exp claim)
    │   │   └─→ Not Expired / Expired
    │   │
    │   └─→ Extract Claims
    │       ├── NameIdentifier (UserId)
    │       ├── Email
    │       ├── Issuer
    │       └── Audience
    │
    └─→ Return ClaimsPrincipal (if valid) / null (if invalid)
```

---

## Email Provider Selection

```
Program.cs Startup
    │
    ├─→ var provider = configuration["Email:Provider"]
    │
    ├─→ EmailSenderFactory.CreateEmailSender(provider)
    │   │
    │   ├─ If "Smtp"
    │   │  └─→ new SmtpEmailSender(config, logger)
    │   │      └─→ Use MailKit for SMTP
    │   │
    │   ├─ If "SendGrid"
    │   │  └─→ new SendGridEmailSender(config, logger)
    │   │      └─→ Use SendGrid API
    │   │
    │   └─ If "Stub"
    │      └─→ new StubEmailSender(logger)
    │          └─→ Log to console
    │
    └─→ Register in DI container
        └─→ services.AddScoped(sp => emailSender)
```

---

## Database Relationship Diagram

```
┌─────────────────────────┐
│ Users Table             │
├─────────────────────────┤
│ PK  Id: GUID            │
│ UNQ Email: VARCHAR(255) │
│     PasswordHash: TEXT  │
│     IsEmailVerified: BIT│
│     EmailVerifToken: VAR│
│     ResetToken: VARCHAR │
│     ResetTokenExpiry: DT│
│     Status: INT         │
│     CreatedAt: DATETIME │
│     UpdatedAt: DATETIME │
└─────────────────────────┘
         │ 1
         │
         │ *
         │
    ┌────────────────────────────┐
    │ RefreshTokens Table         │
    ├────────────────────────────┤
    │ PK Id: GUID                 │
    │ FK UserId: GUID ────┐       │
    │    Token: VARCHAR   │───────┤──→ Unique Index
    │    ExpiresAt: DATETIME      │
    │    IsRevoked: BIT           │
    │    CreatedAt: DATETIME      │
    └────────────────────────────┘

Cascade Delete: User deletion removes all RefreshTokens
```

---

## JWT Token Lifecycle

```
1. User Logs In
   └─→ TokenService.GenerateAccessToken()
       └─→ JWT created, signed, expires in 15 min

2. Token in Use (< 15 min)
   └─→ API validates token
       └─→ Signature OK, not expired
       └─→ Request proceeds

3. Token Expires (≥ 15 min)
   └─→ API rejects token
       └─→ Client gets 401 Unauthorized

4. Client Refresh (using RefreshToken)
   └─→ TokenService.RefreshToken()
       └─→ New AccessToken generated
       └─→ Same RefreshToken reused (or new one)
       └─→ New AccessToken valid for 15 min

5. RefreshToken Expires (≥ 7 days)
   └─→ User must login again
       └─→ Generate new tokens

6. Optional: Token Revocation
   └─→ TokenService.RevokeTokenAsync()
       └─→ Mark token as revoked in DB
       └─→ Future validations check revoked status
```

---

## Configuration Load Flow

```
Program.cs Startup
    │
    ├─→ builder.Configuration loads appsettings.json
    │
    ├─→ Validate JWT Settings
    │   ├─ Secret.Length >= 32 ✓
    │   ├─ Issuer != null ✓
    │   └─ Audience != null ✓
    │
    ├─→ Validate Email Settings
    │   ├─ Provider in [Smtp, SendGrid, Stub] ✓
    │   └─ Provider-specific settings ✓
    │
    ├─→ Validate Database
    │   ├─ ConnectionString valid ✓
    │   └─ SQL Server accessible ✓
    │
    └─→ If any validation fails
        └─→ Throw InvalidOperationException
            └─→ Fail Fast (don't start app)
```
