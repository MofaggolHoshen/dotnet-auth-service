# Phase 3: Technical Specifications

Detailed specifications for all infrastructure components.

---

## AppDbContext Specification

### Entity: User

```csharp
public class User
{
    public Guid Id { get; set; }                          // Primary key
    public string Email { get; set; }                     // Unique, required
    public string PasswordHash { get; set; }              // BCrypt hashed
    public bool IsEmailVerified { get; set; }             // Default: false
    public string EmailVerificationToken { get; set; }    // Single-use
    public string PasswordResetToken { get; set; }        // Single-use
    public DateTime? PasswordResetTokenExpiry { get; set; } // 1 hour
    public UserStatus Status { get; set; }                // Active/Inactive/etc
    public DateTime CreatedAt { get; set; }               // Auto-set
    public DateTime UpdatedAt { get; set; }               // Auto-update
    public ICollection<RefreshToken> RefreshTokens { get; set; }
}
```

### Entity: RefreshToken

```csharp
public class RefreshToken
{
    public Guid Id { get; set; }                    // Primary key
    public Guid UserId { get; set; }                // Foreign key
    public string Token { get; set; }               // Cryptographically random
    public DateTime ExpiresAt { get; set; }         // 7 days
    public bool IsRevoked { get; set; }             // Default: false
    public DateTime CreatedAt { get; set; }         // Auto-set
}
```

### Database Migrations

**Initial Migration Creates:**

1. **Users Table**
   - Columns: Id, Email, PasswordHash, IsEmailVerified, EmailVerificationToken, PasswordResetToken, PasswordResetTokenExpiry, Status, CreatedAt, UpdatedAt
   - Indexes: Unique on Email
   - Constraints: NOT NULL on Email, PasswordHash, CreatedAt, UpdatedAt
   - Default: IsEmailVerified = 0, Status = 0

2. **RefreshTokens Table**
   - Columns: Id, UserId, Token, ExpiresAt, IsRevoked, CreatedAt
   - Indexes: Non-unique on UserId
   - Constraints: NOT NULL on UserId, Token, ExpiresAt, CreatedAt
   - Default: IsRevoked = 0
   - Foreign Key: UserId → Users.Id (CASCADE DELETE)

---

## UserRepository Specification

### Interface: IUserRepository

```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByEmailVerificationTokenAsync(string token);
    Task<User?> GetByPasswordResetTokenAsync(string token);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task<bool> ExistsByEmailAsync(string email);
}
```

### Methods

| Method                           | Input        | Returns      | Logic                  |
| -------------------------------- | ------------ | ------------ | ---------------------- |
| GetByIdAsync                     | Guid id      | User or null | Query by primary key   |
| GetByEmailAsync                  | string email | User or null | Case-insensitive query |
| GetByEmailVerificationTokenAsync | string token | User or null | Query by token         |
| GetByPasswordResetTokenAsync     | string token | User or null | Query by token         |
| AddAsync                         | User user    | void         | Insert and save        |
| UpdateAsync                      | User user    | void         | Update and save        |
| ExistsByEmailAsync               | string email | bool         | Check existence        |

### Error Handling

| Scenario             | Exception                    | Message                    |
| -------------------- | ---------------------------- | -------------------------- |
| Email already exists | DbUpdateException            | "Email already registered" |
| User not found       | (returns null)               | -                          |
| Database error       | SqlException                 | Log and rethrow            |
| Concurrency conflict | DbUpdateConcurrencyException | Log and rethrow            |

---

## TokenService Specification

### Interface: ITokenService

```csharp
public interface ITokenService
{
    string GenerateAccessToken(User user, int expiryMinutes = 0);
    AuthToken GenerateRefreshToken();
    ClaimsPrincipal ValidateToken(string token);
    Task RevokeTokenAsync(string token);
    Task<bool> IsTokenRevokedAsync(string token);
}
```

### JWT Token Structure

**Header:**

```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

**Payload:**

```json
{
  "sub": "user-id-guid",
  "email": "user@example.com",
  "iss": "AuthService",
  "aud": "AuthServiceClients",
  "exp": 1234567890,
  "iat": 1234567200
}
```

**Signature:** HMAC SHA-256 (secret key)

### Token Expiry

| Token Type    | Default    | Configurable                 |
| ------------- | ---------- | ---------------------------- |
| Access Token  | 15 minutes | Jwt:AccessTokenExpiryMinutes |
| Refresh Token | 7 days     | Jwt:RefreshTokenExpiryDays   |

### Claims

| Claim          | Type                      | Example                              |
| -------------- | ------------------------- | ------------------------------------ |
| NameIdentifier | ClaimTypes.NameIdentifier | 550e8400-e29b-41d4-a716-446655440000 |
| Email          | ClaimTypes.Email          | user@example.com                     |
| Issuer         | (JWT standard)            | AuthService                          |
| Audience       | (JWT standard)            | AuthServiceClients                   |

---

## Email Provider Specification

### Interface: IEmailSender

```csharp
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body);
}
```

### SmtpEmailSender

| Property | Type   | Required | Example                 |
| -------- | ------ | -------- | ----------------------- |
| Host     | string | Yes      | smtp.gmail.com          |
| Port     | int    | Yes      | 587                     |
| Username | string | Yes      | your-email@gmail.com    |
| Password | string | Yes      | app-password            |
| From     | string | Yes      | noreply@authservice.com |

**Protocol:** TLS/StartTLS  
**Authentication:** Basic Auth (username/password)  
**Timeout:** 30 seconds  
**Retry:** 3 attempts with exponential backoff

### SendGridEmailSender

| Property | Type   | Required | Example                 |
| -------- | ------ | -------- | ----------------------- |
| ApiKey   | string | Yes      | SG.your-api-key         |
| From     | string | Yes      | noreply@authservice.com |

**Endpoint:** https://api.sendgrid.com/v3/mail/send  
**Authentication:** Bearer token  
**Timeout:** 30 seconds  
**Retry:** 3 attempts

### StubEmailSender

Logs to console/logger for development/testing.

```
STUB: Email to user@example.com, Subject: Verify Email
STUB: Body: <html>Click here to verify...</html>
```

---

## Configuration Specification

### JWT Settings

```json
{
  "Jwt": {
    "Secret": "string (min 32 chars)",
    "Issuer": "string",
    "Audience": "string",
    "AccessTokenExpiryMinutes": "int",
    "RefreshTokenExpiryDays": "int"
  }
}
```

### Email Settings

```json
{
  "Email": {
    "Provider": "Smtp|SendGrid|Stub",
    "Smtp": {
      "Host": "string",
      "Port": "int",
      "Username": "string",
      "Password": "string",
      "From": "string"
    },
    "SendGrid": {
      "ApiKey": "string",
      "From": "string"
    }
  }
}
```

---

## Performance Specifications

| Operation         | Target | Actual              |
| ----------------- | ------ | ------------------- |
| GetByEmailAsync   | <100ms | EF Core optimized   |
| AddUserAsync      | <100ms | Batch inserts       |
| GenerateJWT       | <10ms  | In-memory           |
| SendEmail (async) | <5s    | Depends on provider |
| ValidateToken     | <5ms   | In-memory           |

---

## Security Specifications

### JWT Secret

- Minimum 32 characters
- Alphanumeric + special characters
- Stored in configuration (not in code)
- Rotated annually

### Token Validation

- Signature verified
- Issuer validated
- Audience validated
- Expiry checked
- No clock skew tolerance in production

### Email Security

- No credentials in logs
- TLS/HTTPS for email transmission
- No sensitive data in email body
- Rate limiting on send (if applicable)

---

## Database Specifications

### Connection Pool

- Min: 5 connections
- Max: 100 connections
- Timeout: 30 seconds
- Idle Timeout: 10 minutes

### Query Optimization

- Indexes on Email (unique)
- Indexes on UserId (RefreshTokens)
- No SELECT \* queries
- Use AsNoTracking for reads
- Batch updates where possible
