# Phase 3: Architecture Decisions

Key architectural decisions and rationale for the infrastructure layer.

---

## 1. Entity Framework Core (EF Core 9) vs Dapper

**Decision:** Use Entity Framework Core 9

**Rationale:**

- Type-safe LINQ queries
- Automatic change tracking
- Built-in migration system
- Lazy loading and eager loading support
- Works seamlessly with async/await
- Integrates with dependency injection
- Better for rapid development

**Trade-off:** Slight performance overhead vs raw SQL, but negligible for auth service

---

## 2. Repository Pattern

**Decision:** Implement IUserRepository interface

**Rationale:**

- Abstracts data access from business logic
- Makes testing easier (mock repository)
- Allows swapping data sources (SQL → NoSQL, etc.)
- Follows SOLID principle (dependency inversion)
- Clear separation of concerns

**Implementation:**

```csharp
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
    // etc.
}
```

---

## 3. Async/Await Throughout

**Decision:** All I/O operations are async

**Rationale:**

- Better resource utilization
- Scales better under load
- Prevents thread pool starvation
- Industry standard for modern .NET
- Required for Entity Framework Core

**Implementation:**

```csharp
public async Task<User?> GetByEmailAsync(string email)
{
    return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
}
```

---

## 4. JWT Token Strategy

**Decision:** JWT for stateless authentication + Refresh tokens for refresh

**Rationale:**

- No server-side session storage needed
- Scales horizontally (no session affinity)
- Works well for microservices
- Can be validated offline (after key rotation period)
- Standard for REST APIs

**Trade-off:** Can't immediately revoke tokens (mitigated by short expiry)

---

## 5. Short-Lived Access Tokens

**Decision:** 15-minute access token expiry

**Rationale:**

- Limited window if token is compromised
- Refresh token solves need for long-lived token
- Standard practice for OAuth 2.0
- Balances security vs. UX

**Longer Refresh Token:**

- 7-day refresh token expiry
- Refresh stored in database for revocation
- Can be revoked immediately

---

## 6. Password Hashing: BCrypt

**Decision:** BCrypt with cost factor 11

**Rationale:**

- Adaptive algorithm (increases cost over time)
- Built-in salt generation
- Resistant to GPU attacks
- Industry standard
- No rainbow table attacks possible

**Configuration:**

```csharp
BCrypt.BCrypt.HashPassword(password, cost: 11);
```

---

## 7. Multiple Email Providers

**Decision:** Strategy pattern with factory

**Rationale:**

- Different environments use different providers
- Can switch without code changes
- Testing with stub provider
- Production with SMTP or SendGrid
- Easy to add new providers

**Selection:**

```json
{
  "Email": {
    "Provider": "Smtp" // or "SendGrid" or "Stub"
  }
}
```

---

## 8. Configuration from appsettings.json

**Decision:** Externalize configuration, validate at startup

**Rationale:**

- Different configs per environment
- Secrets not in code
- Fail-fast if config is invalid
- Follows 12-factor app principles

**Validation:**

```csharp
if (jwtSecret.Length < 32)
    throw new InvalidOperationException("JWT Secret too short");
```

---

## 9. Connection Pooling

**Decision:** Let EF Core handle connection pooling

**Rationale:**

- Automatic pool management
- Configurable limits
- Better than manual connections
- Scales well

---

## 10. Single AppDbContext

**Decision:** One DbContext for the entire application

**Rationale:**

- Simpler than multiple contexts
- Easier unit of work pattern
- Consistent transaction handling
- Clear data access point

**Alternative (not chosen):** Multiple contexts per aggregate root (more complex)

---

## 11. Database Migrations

**Decision:** Code-first migrations with EF Core

**Rationale:**

- Version control for schema
- Repeatable deployments
- Easy rollback
- Integrated with C# code

**Naming Convention:**

```
[timestamp]_InitialCreate.cs
[timestamp]_AddEmailField.cs
```

---

## 12. Cascading Deletes

**Decision:** Cascade delete on RefreshTokens when User deleted

**Rationale:**

- Maintains referential integrity
- No orphaned tokens
- Automatic cleanup
- User deletion fully cleans up

---

## 13. Email Token Format

**Decision:** Guid.NewGuid().ToString("N") for tokens

**Rationale:**

- Cryptographically random
- 32 hexadecimal characters
- No special characters
- URL-safe
- Standard .NET approach

**Example:** `a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6`

---

## 14. Password Reset Token Expiry

**Decision:** 1 hour expiry for password reset tokens

**Rationale:**

- Enough time for user to check email and act
- Limited window for brute force attempts
- Security-focused
- Standard practice

---

## 15. Error Handling Strategy

**Decision:** Log detailed errors, return generic messages to client

**Rationale:**

- No information disclosure
- Prevents account enumeration
- Helps debugging (logs)
- Security best practice

**Example:**

```csharp
// Log everything
_logger.LogError("User not found: {Email}", email);

// Return generic to client
return new AuthResponse { Success = false, Message = "Invalid credentials" };
```

---

## Comparison Table: Design Patterns

| Pattern          | Decision               | Alternative           |
| ---------------- | ---------------------- | --------------------- |
| Data Access      | Repository             | Direct EF Core        |
| Token Generation | Token Service          | Direct in AuthService |
| Email Sending    | Multiple Providers     | Single provider       |
| Configuration    | External (appsettings) | Hardcoded             |
| Password Hash    | BCrypt                 | PBKDF2, Argon2        |
| Token Storage    | Server (for refresh)   | Client only           |

---

## Technology Stack Rationale

| Technology           | Why Chosen                  |
| -------------------- | --------------------------- |
| EF Core 9            | ORM, migrations, async      |
| SQL Server           | Industry standard, scalable |
| JWT                  | Stateless, REST-friendly    |
| BCrypt               | Secure, adaptive hashing    |
| MailKit              | SMTP support, reliable      |
| SendGrid             | Cloud email, reliable       |
| Dependency Injection | Built into .NET Core        |

---

## Scalability Considerations

**Horizontal Scaling:**

- ✅ Stateless JWT tokens
- ✅ Repository abstraction
- ✅ Configuration centralized
- ✅ No server-side sessions
- ⚠️ Refresh token revocation needs distributed cache

**Vertical Scaling:**

- ✅ Async/await for concurrency
- ✅ Connection pooling
- ✅ Query optimization
- ✅ Indexed lookups

---

## Security Considerations

| Concern            | Mitigation                    |
| ------------------ | ----------------------------- |
| Token compromise   | Short expiry (15 min)         |
| Key rotation       | Version JWT secret in config  |
| Brute force        | Temporary account lockout     |
| Email enumeration  | Generic error messages        |
| SQL injection      | EF Core parameterized queries |
| Credential storage | Never log passwords           |
