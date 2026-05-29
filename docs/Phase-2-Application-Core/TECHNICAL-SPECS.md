# Phase 2: Technical Specifications

**Detailed Technical Reference for Application Core Layer**

---

## Overview

This document provides detailed technical specifications for Phase 2 implementation, including:

- Service interface contracts
- DTO specifications with field details
- Validator rules and constraints
- Business logic flow diagrams
- Error handling specifications
- Security requirements

---

## Service Interface Specifications

### 1. IAuthService

**Location:** `src/AuthService.Application/Interfaces/IAuthService.cs`

**Purpose:** Primary authentication service interface orchestrating all auth flows

#### Method: RegisterAsync

```csharp
Task<AuthResponse> RegisterAsync(RegisterRequest request);
```

**Input:** RegisterRequest

```
{
    Email: string (email format, max 255 chars),
    Password: string (8+ chars, complexity rules),
    ConfirmPassword: string (must equal Password)
}
```

**Output:** AuthResponse

```
{
    Success: bool,
    Message: string,
    Token: null
}
```

**Business Logic:**

1. Validate request with RegisterRequestValidator
2. Check if email already exists in database
3. If exists: return {Success: false, Message: "Email already registered"}
4. Hash password using BCrypt.HashPassword()
5. Generate email verification token (Guid.NewGuid().ToString("N"))
6. Create User entity with:
   - New Guid for Id
   - IsEmailVerified = false
   - EmailVerificationToken = generated token
   - Status = UserStatus.Active
   - CreatedAt/UpdatedAt = DateTime.UtcNow
7. Call IUserRepository.CreateAsync(user)
8. Call IEmailSender.SendEmailVerificationAsync(email, token)
9. Return {Success: true, Message: "Registration successful..."}

**Error Cases:**

- Email already exists
- Database write failure (propagate)
- Email send failure (non-critical, still return success)

---

#### Method: LoginAsync

```csharp
Task<AuthResponse> LoginAsync(LoginRequest request);
```

**Input:** LoginRequest

```
{
    Email: string,
    Password: string
}
```

**Output:** AuthResponse

```
{
    Success: bool,
    Message: string,
    Token: AuthToken or null
}
```

**Business Logic:**

1. Validate request with LoginRequestValidator
2. Call IUserRepository.GetByEmailAsync(email)
3. If user not found: return {Success: false, Message: "Invalid credentials"}
4. Call BCrypt.Verify(password, user.PasswordHash)
5. If password invalid: return {Success: false, Message: "Invalid credentials"}
6. If user.IsEmailVerified == false: return {Success: false, Message: "Email not verified"}
7. Call ITokenService.GenerateAccessToken(user.Id, user.Email)
8. Call ITokenService.GenerateRefreshToken()
9. Return success with tokens

**Token Response Structure:**

```
{
    AccessToken: string (JWT),
    RefreshToken: string (random GUID),
    ExpiresInMinutes: 15
}
```

---

#### Method: RefreshTokenAsync

```csharp
Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
```

**Input:** RefreshTokenRequest

```
{
    RefreshToken: string
}
```

**Output:** AuthResponse with new AccessToken

**Business Logic:**

1. Validate request with RefreshTokenRequestValidator
2. Call ITokenService.IsTokenRevokedAsync() - check if revoked
3. If revoked: return failure
4. Call ITokenService.ValidateToken(refreshToken)
5. Extract userId and email from token claims
6. Call ITokenService.GenerateAccessToken(userId, email)
7. Return new access token with same refresh token

---

#### Method: VerifyEmailAsync

```csharp
Task<bool> VerifyEmailAsync(string token);
```

**Business Logic:**

1. Search for user with EmailVerificationToken == token
2. If not found: return false
3. If token expired (implementation dependent): return false
4. Set user.IsEmailVerified = true
5. Set user.EmailVerificationToken = null
6. Set user.UpdatedAt = DateTime.UtcNow
7. Call IUserRepository.UpdateAsync(user)
8. Return true

**Note:** Repository may need extension method to search by verification token

---

#### Method: RequestPasswordResetAsync

```csharp
Task<bool> RequestPasswordResetAsync(string email);
```

**Business Logic:**

1. Call IUserRepository.GetByEmailAsync(email)
2. If not found: return true (security: don't reveal email existence)
3. Generate reset token (Guid.NewGuid().ToString("N"))
4. Set user.PasswordResetToken = token
5. Set user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1)
6. Set user.UpdatedAt = DateTime.UtcNow
7. Call IUserRepository.UpdateAsync(user)
8. Call IEmailSender.SendPasswordResetAsync(email, token)
9. Return true

**Token Expiry:** 1 hour from generation

---

#### Method: ResetPasswordAsync

```csharp
Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
```

**Input:** ResetPasswordRequest

```
{
    Token: string,
    NewPassword: string,
    ConfirmPassword: string
}
```

**Business Logic:**

1. Validate request with ResetPasswordRequestValidator
2. Search for user with PasswordResetToken == token
3. If not found: return false
4. If token expired (check PasswordResetTokenExpiry): return false
5. Hash new password with BCrypt
6. Set user.PasswordHash = hashedPassword
7. Set user.PasswordResetToken = null
8. Set user.PasswordResetTokenExpiry = null
9. Set user.UpdatedAt = DateTime.UtcNow
10. Call IUserRepository.UpdateAsync(user)
11. Return true

---

### 2. ITokenService

**Location:** `src/AuthService.Application/Interfaces/ITokenService.cs`

**Purpose:** JWT token generation, validation, and revocation

#### Method: GenerateAccessToken

```csharp
string GenerateAccessToken(Guid userId, string email);
```

**Returns:** JWT token string

**Token Claims:**

```
{
    "sub": userId (string),
    "email": email,
    "iat": issued at (Unix timestamp),
    "exp": expiration (Unix timestamp),
    "iss": "AuthService",
    "aud": "AuthServiceClients"
}
```

**Expiry:** 15 minutes

---

#### Method: GenerateRefreshToken

```csharp
string GenerateRefreshToken();
```

**Returns:** Random token string (typically Guid)

**Implementation:**

```csharp
return Guid.NewGuid().ToString();
```

---

#### Method: ValidateToken

```csharp
ClaimsPrincipal? ValidateToken(string token);
```

**Returns:**

- ClaimsPrincipal if valid (contains claims)
- null if invalid or expired

**Validation Checks:**

1. Token format valid
2. Signature valid
3. Expiry not passed
4. Issuer matches
5. Audience matches

---

#### Method: RevokeTokenAsync

```csharp
Task<bool> RevokeTokenAsync(Guid userId, string refreshToken);
```

**Business Logic:**

1. Add refresh token to revocation store (database)
2. Associate with userId
3. Mark timestamp

**Returns:** true if successful

---

#### Method: IsTokenRevokedAsync

```csharp
Task<bool> IsTokenRevokedAsync(Guid userId, string refreshToken);
```

**Returns:**

- true if token is in revocation store
- false if token is still valid

---

### 3. IEmailSender

**Location:** `src/AuthService.Application/Interfaces/IEmailSender.cs`

**Purpose:** Abstract email sending across providers (SMTP, SendGrid, Stub)

#### Method: SendEmailVerificationAsync

```csharp
Task<bool> SendEmailVerificationAsync(string email, string verificationToken);
```

**Email Content Example:**

```
Subject: Verify Your Email - Authentication Service

Hello,

Please verify your email by clicking this link:
https://yourdomain.com/auth/verify-email?token={verificationToken}

This link expires in 24 hours.

Best regards,
Authentication Service
```

**Returns:** true if sent successfully

---

#### Method: SendPasswordResetAsync

```csharp
Task<bool> SendPasswordResetAsync(string email, string resetToken);
```

**Email Content Example:**

```
Subject: Reset Your Password - Authentication Service

Hello,

Click this link to reset your password:
https://yourdomain.com/auth/reset-password?token={resetToken}

This link expires in 1 hour.

If you didn't request this, ignore this email.

Best regards,
Authentication Service
```

**Returns:** true if sent successfully

---

### 4. IUserRepository

**Location:** `src/AuthService.Application/Interfaces/IUserRepository.cs`

**Purpose:** Data access abstraction for User entity

#### Method: GetByEmailAsync

```csharp
Task<User?> GetByEmailAsync(string email);
```

**Behavior:** Case-insensitive email lookup (application or DB level)

---

#### Method: GetByIdAsync

```csharp
Task<User?> GetByIdAsync(Guid id);
```

---

#### Method: EmailExistsAsync

```csharp
Task<bool> EmailExistsAsync(string email);
```

**Optimization:** Use SELECT COUNT instead of fetching full entity

---

#### Method: CreateAsync

```csharp
Task<User> CreateAsync(User user);
```

**Database Operation:** INSERT

**Returns:** The created user (may include generated values)

---

#### Method: UpdateAsync

```csharp
Task<User> UpdateAsync(User user);
```

**Database Operation:** UPDATE

**Returns:** The updated user

---

#### Method: DeleteAsync

```csharp
Task<bool> DeleteAsync(Guid userId);
```

**Database Operation:** DELETE

**Returns:** true if deleted, false if not found

---

## DTO Specifications

### Request DTOs

#### RegisterRequest

```csharp
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

**Validation Rules:**

- Email: required, valid format, max 255 chars
- Password: required, min 8 chars, uppercase, lowercase, digit, special char
- ConfirmPassword: required, must equal Password

---

#### LoginRequest

```csharp
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

**Validation Rules:**

- Email: required, valid format
- Password: required, non-empty

---

#### RefreshTokenRequest

```csharp
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
```

**Validation Rules:**

- RefreshToken: required, non-empty

---

#### VerifyEmailRequest

```csharp
public class VerifyEmailRequest
{
    public string Token { get; set; } = string.Empty;
}
```

**Validation Rules:**

- Token: required, non-empty

---

#### ForgotPasswordRequest

```csharp
public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}
```

**Validation Rules:**

- Email: required, valid format

---

#### ResetPasswordRequest

```csharp
public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

**Validation Rules:**

- Token: required, non-empty
- NewPassword: required, min 8 chars, complexity
- ConfirmPassword: required, must equal NewPassword

---

### Response DTOs

#### AuthResponse

```csharp
public class AuthResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public AuthToken? Token { get; set; }
}
```

**Usage:** All auth endpoints that may return tokens

---

#### AuthToken

```csharp
public class AuthToken
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; }
}
```

**ExpiresInMinutes:** Always 15 for access tokens

---

#### MessageResponse

```csharp
public class MessageResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
```

**Usage:** Endpoints that don't return tokens (verify email, password reset request)

---

## Validator Specifications

### Password Pattern

```regex
(?=.*[A-Z])       # At least one uppercase
(?=.*[a-z])       # At least one lowercase
(?=.*[0-9])       # At least one digit
(?=.*[!@#$%^&*])  # At least one special char
.{8,}             # At least 8 characters
```

**FluentValidation Implementation:**

```csharp
RuleFor(x => x.Password)
    .NotEmpty()
    .MinimumLength(8)
    .Matches(@"[A-Z]").WithMessage("Uppercase required")
    .Matches(@"[a-z]").WithMessage("Lowercase required")
    .Matches(@"[0-9]").WithMessage("Digit required")
    .Matches(@"[!@#$%^&*]").WithMessage("Special char required");
```

---

## Error Messages

### Registration Errors

- "Email is required" - Empty email
- "Email must be valid" - Invalid format
- "Email cannot exceed 255 characters" - Too long
- "Email is already registered" - Duplicate email
- "Password is required" - Empty password
- "Password must be at least 8 characters" - Too short
- "Password must contain at least one uppercase letter" - No uppercase
- "Password must contain at least one lowercase letter" - No lowercase
- "Password must contain at least one digit" - No digit
- "Password must contain at least one special character" - No special char
- "Passwords do not match" - Confirm != Password

### Login Errors

- "Invalid email or password" - User not found or password wrong
- "Email not verified. Please verify your email first." - Email unverified

### Token Refresh Errors

- "Refresh token is required" - Missing token
- "Invalid refresh token" - Malformed or revoked
- "Invalid or expired refresh token" - Expired

### Password Reset Errors

- "Reset token is required" - Missing token
- "Invalid or expired reset token" - Invalid token

---

## Business Rules

### Registration

1. Email must be unique (case-insensitive)
2. Password must meet complexity requirements
3. New users start with IsEmailVerified = false
4. Email verification required before login

### Login

1. Email must exist
2. Password must match hash
3. Email must be verified
4. Cannot login without verification

### Token Refresh

1. Refresh token must not be revoked
2. Refresh token must not be expired
3. Claims must be extractable (userId, email)

### Email Verification

1. Token must be valid (exist in database)
2. Token must not be already used
3. After verification, token is cleared

### Password Reset

1. Token must be valid
2. Token must not be expired (1 hour max)
3. Token must not be already used
4. New password must meet complexity rules

---

## Security Requirements

### Authentication

- [ ] Passwords hashed with BCrypt (cost factor 11+)
- [ ] No plaintext passwords stored
- [ ] No passwords in logs or error messages

### Tokens

- [ ] JWT signed with strong secret (32+ characters)
- [ ] Token expiry enforced
- [ ] Refresh token revocation supported

### Email

- [ ] Verification tokens single-use
- [ ] Reset tokens expire after 1 hour
- [ ] Tokens cryptographically random

### API

- [ ] No email existence disclosure (404 instead of 400)
- [ ] Generic error messages to clients
- [ ] Detailed errors in logs only

---

## Performance Requirements

### Async Operations

- [ ] All methods async (no blocking)
- [ ] Proper task configuration
- [ ] Async/await pattern

### Database

- [ ] Email lookup indexed
- [ ] Id lookup indexed
- [ ] No N+1 queries

### Token Generation

- [ ] <10ms for JWT generation
- [ ] <5ms for refresh token generation
- [ ] <1ms for validation

---

## Testability

### For Unit Tests

- [ ] Mock IUserRepository
- [ ] Mock ITokenService
- [ ] Mock IEmailSender
- [ ] Test success paths
- [ ] Test failure paths
- [ ] Test edge cases

### For Integration Tests

- [ ] Real database (LocalDB/InMemory)
- [ ] Real JWT token generation
- [ ] Optional: Mock email sender

---

## Logging Strategy (Phase 3)

**Events to Log:**

- User registration attempt
- Login attempt (failed/successful)
- Email verification attempt
- Password reset request
- Token refresh attempt

**Avoid Logging:**

- Passwords (any form)
- Sensitive tokens (partial OK with masking)
- Email addresses (optional, use Pii redaction)

---

**Document Version:** 1.0  
**Created:** 2026-05-29  
**Phase:** 2 of 6  
**Status:** Reference Document
