# Phase 2: Architecture & Design Decisions

**Rationale for Phase 2 Architectural Choices**

---

## Overview

This document explains the architectural decisions made for Phase 2 and their rationale. Understanding these decisions helps maintain consistency throughout development.

---

## 1. Service-Based Architecture

### Decision

Use **three service interfaces** (IAuthService, ITokenService, IEmailSender) plus **repository pattern** (IUserRepository).

### Why?

✅ **Single Responsibility:**

- IAuthService = workflow orchestration
- ITokenService = token concerns
- IEmailSender = communication concerns
- IUserRepository = data access concerns

✅ **Testability:**

- Each service independently mockable
- Easy to test workflows in isolation
- Can mock external dependencies

✅ **Flexibility:**

- Swap email providers without changing auth logic
- Swap token implementation without changing auth
- Swap database without changing auth

✅ **Maintainability:**

- Changes to token logic don't affect auth service
- Email provider changes isolated
- Repository changes isolated

### Alternative Rejected

**Single MonolithicService** - All in one class

- ❌ Harder to test
- ❌ Harder to maintain
- ❌ Harder to reuse components

---

## 2. Interface-Based Dependency Injection

### Decision

Depend on **interfaces**, not implementations.

```csharp
public class AuthService
{
    private readonly IUserRepository _repo;
    private readonly ITokenService _tokenService;

    public AuthService(IUserRepository repo, ITokenService tokenService)
    {
        _repo = repo;
        _tokenService = tokenService;
    }
}
```

### Why?

✅ **Loose Coupling:** AuthService doesn't know about UserRepository implementation

✅ **Testing:** Can inject mock implementations

```csharp
var mockRepo = new Mock<IUserRepository>();
var authService = new AuthService(mockRepo.Object, mockTokenService);
```

✅ **Swappability:** Easy to swap implementations without changing AuthService

### Alternative Rejected

**Direct Dependencies** - Create instances internally

```csharp
public class AuthService
{
    public AuthService()
    {
        _repo = new UserRepository();  // ❌ Tight coupling
        _tokenService = new TokenService();
    }
}
```

---

## 3. Async/Await Throughout

### Decision

All I/O operations are async.

```csharp
Task<AuthResponse> RegisterAsync(RegisterRequest request);
Task<User?> GetByEmailAsync(string email);
Task<bool> SendEmailVerificationAsync(string email, string token);
```

### Why?

✅ **Scalability:** Non-blocking I/O allows handling more concurrent requests

✅ **Performance:** Other requests can run while database/email is being accessed

✅ **Standard Practice:** Modern .NET applications are async

✅ **Future-Ready:** Aligns with ASP.NET Core expectations

### Metrics

- Synchronous app: 100 concurrent requests, 5 connections blocked on DB
- Asynchronous app: 100 concurrent requests, 5 connections blocked on DB, but threads available for others

---

## 4. DTO Layer Between API and Business Logic

### Decision

Create separate DTOs (Request/Response) instead of using Domain entities directly.

```
API Request → RegisterRequest (DTO)
            ↓
         RegisterAsync()
            ↓
         AuthResponse (DTO) → HTTP Response
```

### Why?

✅ **API Contract Independence:** API shape doesn't change with domain model

✅ **Security:** Don't expose internal domain properties

```csharp
// Bad - exposes internal fields
public class UserResponse
{
    public string PasswordHash { get; set; }  // ❌ Security issue
}

// Good - only what's needed for API
public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public AuthToken Token { get; set; }  // ✅ No sensitive data
}
```

✅ **Flexibility:** Change internal domain without breaking API clients

✅ **Validation:** DTOs are validated before business logic

### Disadvantages

- More classes to maintain
- Mapping between DTOs and entities needed (Phase 3)

### Mitigation

- Use AutoMapper (add in Phase 3 if needed)
- Keep mapping simple and explicit
- DTOs live in Application layer (clean separation)

---

## 5. FluentValidation Over Data Annotations

### Decision

Use **FluentValidation** library instead of data annotation attributes.

### Why?

✅ **Readability:** Fluent syntax is clear and readable

```csharp
// FluentValidation - ✅ Readable
RuleFor(x => x.Email)
    .NotEmpty().WithMessage("Email required")
    .EmailAddress().WithMessage("Invalid email");

// Data Annotations - Less readable
[Required(ErrorMessage = "Email required")]
[EmailAddress(ErrorMessage = "Invalid email")]
public string Email { get; set; }
```

✅ **Complex Rules:** Easier to express complex validation logic

```csharp
// Conditional validation
RuleFor(x => x.ConfirmPassword)
    .Equal(x => x.Password).When(x => x.IsChangingPassword);
```

✅ **Reusability:** Can register validators with dependency injection

✅ **Separation:** Validation logic in Application layer (not domain)

### Alternative Rejected

**Data Annotations** - Attributes on models

- ❌ Less readable for complex rules
- ❌ Hard to share validation rules
- ❌ Validation lives on domain (violates clean arch)

---

## 6. Generic Error Messages

### Decision

Return **same error message** for login failures, regardless of cause.

```csharp
// Correct - doesn't reveal which failed
if (user == null || !BCrypt.Verify(password, user.PasswordHash))
{
    return new AuthResponse { Success = false, Message = "Invalid credentials" };
}

// Incorrect - reveals email exists
if (user == null)
    return new AuthResponse { Success = false, Message = "Email not found" };
```

### Why?

✅ **Security:** Prevents email enumeration attacks

- Attacker can't discover which emails are registered
- Adds friction to brute force attempts

✅ **Privacy:** Users' email addresses are sensitive data

### Tradeoff

- Less helpful error messages for legitimate users
- Mitigation: Log detailed errors server-side

---

## 7. Password Hashing with BCrypt

### Decision

Use **BCrypt.Net-Next** library for password hashing.

```csharp
// Registration
string hash = BCrypt.HashPassword(plainPassword);

// Login
bool isValid = BCrypt.Verify(plainPassword, storedHash);
```

### Why?

✅ **Secure:** Designed specifically for passwords

- Salts automatically
- Adaptive cost factor (can increase over time)
- Resistant to timing attacks

✅ **Industry Standard:** Used by most production applications

✅ **Simplicity:** Easy to use API, hard to misuse

### Compared to Alternatives

| Method     | Security         | Ease       |
| ---------- | ---------------- | ---------- |
| **BCrypt** | ✅✅✅ Excellent | ✅✅ Easy  |
| PBKDF2     | ✅✅ Good        | ✅✅ Easy  |
| SHA256     | ❌ Weak          | ✅✅ Easy  |
| Plaintext  | ❌❌ Terrible    | ✅ Trivial |

---

## 8. JWT with Refresh Tokens

### Decision

Use **JWT for access tokens** (15 min expiry) + **refresh tokens** for renewal.

```csharp
// Access token - Short lived JWT
{
    "sub": "user-id",
    "email": "user@example.com",
    "exp": 1623456789  // 15 minutes
}

// Refresh token - Long lived, stored in DB
{
    Token: "random-guid-stored-in-db",
    ExpiresAt: 7 days from now
}
```

### Why?

✅ **Security:**

- Access token expires quickly
- Compromised token is short-lived
- Refresh token can be revoked

✅ **UX:**

- Users stay logged in (refresh silently)
- No need to re-enter credentials

✅ **Stateless (partially):**

- Access token doesn't require DB lookup
- Only refresh requires DB lookup

### Architecture

```
Client Login
    ↓
Verify Credentials
    ↓
Generate Access Token (JWT) + Refresh Token
    ↓
Store Refresh Token in DB, send both to client
    ↓
Client: Use Access Token for API calls
    ↓
Access Token Expires
    ↓
Client: Send Refresh Token to /refresh endpoint
    ↓
Generate New Access Token
    ↓
Client: Resume API calls with new token
```

### Alternative Rejected

**Session-Based (cookies):**

- ❌ Not suitable for mobile/SPA clients
- ❌ Server state (not scalable)

**Long-Lived JWT (no refresh):**

- ❌ Can't revoke compromised token
- ❌ Server can't invalidate users

---

## 9. Token Revocation Pattern

### Decision

Store revoked refresh tokens in database for validation.

```
User Logout
    ↓
Add refresh token to revocation store
    ↓
Later: IsTokenRevokedAsync() checks store
    ↓
If revoked: reject refresh request
```

### Why?

✅ **Security:** Can invalidate tokens without waiting for expiry

✅ **Compliance:** Meet security requirements for logout

✅ **Audit:** Track which tokens have been revoked

### Implementation

- Revocation store: separate table or flag in RefreshToken entity
- Cleanup job (Phase 4): delete expired tokens periodically

---

## 10. Email Verification Required for Login

### Decision

Users **cannot login until email is verified**.

```csharp
if (!user.IsEmailVerified)
{
    return new AuthResponse { Success = false, Message = "Email not verified" };
}
```

### Why?

✅ **Data Quality:** Ensures valid email addresses

✅ **Compliance:** Required by many applications/regulations

✅ **Security:** Prevents using fake emails to create accounts

✅ **Communication:** Ensures users can receive password resets

### Tradeoff

- Extra step in registration flow
- Can implement "resend verification" endpoint (Phase 4)

---

## 11. Password Reset Token Expiry (1 hour)

### Decision

Password reset tokens expire after **1 hour**.

```csharp
user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
```

### Why?

✅ **Security:** Limits exposure if reset email is leaked

✅ **Balance:** Long enough for legitimate users to reset, short enough for security

### Comparison

| Expiry    | Security         | UX               |
| --------- | ---------------- | ---------------- |
| 15 min    | ✅✅✅ Excellent | ❌ Too short     |
| 1 hour    | ✅✅ Good        | ✅✅ Good        |
| 24 hours  | ✅ Acceptable    | ✅✅✅ Excellent |
| No expiry | ❌ Poor          | ✅✅✅ Excellent |

**Chosen: 1 hour** - Best balance

---

## 12. Application Layer Independence

### Decision

**Application layer depends only on:**

- Domain entities
- External packages (FluentValidation)
- Its own interfaces

**Application layer does NOT depend on:**

- Database (EF Core)
- Email providers
- HTTP infrastructure

### Why?

✅ **Clean Architecture:** Business logic independent of infrastructure

✅ **Testability:** Can test business logic without infrastructure

✅ **Flexibility:** Change infrastructure without changing business logic

### Example: RegisterAsync

```csharp
// ✅ Good - Application layer
public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
{
    var user = new User { ... };
    await _userRepository.CreateAsync(user);  // Abstract interface
    await _emailSender.SendEmailVerificationAsync(...);  // Abstract interface
    return new AuthResponse { ... };
}

// ❌ Bad - Infrastructure leaking into Application
public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
{
    var user = new User { ... };
    await _dbContext.Users.AddAsync(user);  // EF Core specific
    await _dbContext.SaveChangesAsync();  // Database specific
    await Task.Delay(100);  // Infrastructure details
    return new AuthResponse { ... };
}
```

---

## 13. Stateless Service Design

### Decision

AuthService has no state (no instance fields except dependencies).

```csharp
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;  // ✅ Dependency
    private readonly ITokenService _tokenService;  // ✅ Dependency

    // ❌ NO: Instance state
    // private int LoginAttempts;
    // private DateTime LastLoginTime;
}
```

### Why?

✅ **Thread-Safe:** Multiple threads can call same instance safely

✅ **Scalable:** Same instance can be reused across requests

✅ **Testable:** Predictable behavior between calls

### Registration

```csharp
// Register once, reuse across requests
services.AddScoped<IAuthService, AuthService>();
// Each request gets same instance
// Each scope (request) can be concurrent
```

---

## 14. Exception Handling Strategy (Phase 3 Preview)

### Decision

**Services return response objects, not throw exceptions** (for business logic failures).

```csharp
// ✅ Good - business logic returns result
public async Task<AuthResponse> LoginAsync(LoginRequest request)
{
    if (user == null)
        return new AuthResponse { Success = false, Message = "..." };
    return new AuthResponse { Success = true, Message = "..." };
}

// ❌ Bad - business logic throws exceptions
public async Task<AuthResponse> LoginAsync(LoginRequest request)
{
    if (user == null)
        throw new InvalidOperationException("User not found");
}
```

### Why?

✅ **Expected Failures:** Business logic failures are expected, not exceptional

✅ **Performance:** Exceptions have overhead

✅ **Clarity:** Response objects more obvious than exception handling

✅ **Consistency:** All methods return AuthResponse

### Exception Use Cases (Phase 3)

- Unexpected database failures → throw
- Middleware catches and returns 500
- Business logic failures → return response

---

## 15. DTO Response Objects Pattern

### Decision

Create response DTOs for **all endpoints** (not just errors).

```csharp
// ✅ Good - consistent response
new AuthResponse
{
    Success = true,
    Message = "Login successful",
    Token = new AuthToken { ... }
}

// Alternative - raw data
return user;  // ❌ Inconsistent with error responses
```

### Why?

✅ **Consistency:** All endpoints return same structure

✅ **Extensibility:** Easy to add metadata (timestamps, request ids, etc.)

✅ **API Contract:** Clear specification of response format

---

## Summary of Architectural Decisions

| Decision           | Choice                    | Rationale                      |
| ------------------ | ------------------------- | ------------------------------ |
| Service Design     | Multiple focused services | SRP, testability, flexibility  |
| Dependencies       | Interfaces & DI           | Loose coupling, testability    |
| I/O Operations     | Async/await               | Scalability, performance       |
| Data Transfer      | DTOs                      | API independence, security     |
| Validation         | FluentValidation          | Readability, complex rules     |
| Error Messages     | Generic                   | Security, email privacy        |
| Password Hashing   | BCrypt                    | Industry standard, secure      |
| Authentication     | JWT + Refresh             | Security, UX, scalability      |
| Revocation         | Database revocation list  | Token invalidation             |
| Email Verification | Required for login        | Data quality, security         |
| Token Expiry       | 1 hour                    | Security/UX balance            |
| Architecture       | Clean (layered)           | Testability, maintainability   |
| Service State      | Stateless                 | Thread-safe, scalable          |
| Failure Handling   | Response objects          | Expected failures, performance |

---

## Phase 2 Principles

When implementing Phase 2, keep these principles in mind:

1. **Single Responsibility** - Each service has one reason to change
2. **Dependency Inversion** - Depend on abstractions, not concretions
3. **Interface Segregation** - Small, focused interfaces
4. **Open/Closed** - Open for extension, closed for modification
5. **Testability First** - Design for unit testing
6. **Clean Code** - Clear names, no obscure logic
7. **Security First** - Default to secure option
8. **Performance Aware** - Use async, avoid blocking

---

**Document Version:** 1.0  
**Created:** 2026-05-29  
**Phase:** 2 of 6  
**Status:** Architecture Decision Record
