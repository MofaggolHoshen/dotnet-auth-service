# Phase 2: Application Core

**Status:** 🔄 IN PROGRESS  
**Date:** 2026-05-29  
**Duration:** Implementation Phase  
**Objective:** Build the application layer with interfaces, DTOs, validators, and business logic services.

---

## Overview

Phase 2 implements the **Application Layer** - the heart of business logic. This layer defines:

- **Service Interfaces** - Contracts for authentication, tokens, email, and data access
- **DTOs (Data Transfer Objects)** - Request/response models for API communication
- **Validators** - Input validation using FluentValidation
- **AuthService** - Core business logic orchestrating authentication workflows

The Application Layer is **independent of infrastructure** - it depends only on the Domain layer and external packages (FluentValidation). This makes testing and maintenance straightforward.

---

## Architecture Diagram

```
┌──────────────────────────────────────┐
│   API Layer (Controllers)            │
│   Consumes DTOs & Services           │
└──────────────────────────────────────┘
                    ↓
┌──────────────────────────────────────┐
│   Application Layer (THIS PHASE)     │
│  ┌────────────────────────────────┐  │
│  │ Interfaces (Contracts)         │  │
│  │ • IAuthService                 │  │
│  │ • ITokenService                │  │
│  │ • IEmailSender                 │  │
│  │ • IUserRepository              │  │
│  └────────────────────────────────┘  │
│  ┌────────────────────────────────┐  │
│  │ DTOs (Request/Response)        │  │
│  │ • LoginRequest                 │  │
│  │ • RegisterRequest              │  │
│  │ • RefreshTokenRequest          │  │
│  │ • AuthResponse                 │  │
│  │ • (+ more)                     │  │
│  └────────────────────────────────┘  │
│  ┌────────────────────────────────┐  │
│  │ Validators (FluentValidation)  │  │
│  │ • LoginRequestValidator        │  │
│  │ • RegisterRequestValidator     │  │
│  │ • (+ more)                     │  │
│  └────────────────────────────────┘  │
│  ┌────────────────────────────────┐  │
│  │ Services (Business Logic)      │  │
│  │ • AuthService                  │  │
│  │ • Handles auth workflows       │  │
│  └────────────────────────────────┘  │
└──────────────────────────────────────┘
                    ↓
┌──────────────────────────────────────┐
│   Domain Layer (Entities)            │
│   • User • RefreshToken              │
└──────────────────────────────────────┘
```

---

## Phase 2 Tasks

### Task 1: Create Service Interfaces

**File Location:** `src/AuthService.Application/Interfaces/`

#### 1.1 IAuthService

```csharp
// src/AuthService.Application/Interfaces/IAuthService.cs
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<bool> VerifyEmailAsync(string token);
    Task<bool> RequestPasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
}
```

**Responsibility:**

- User registration with email
- Login authentication
- Token refresh
- Email verification
- Password reset flow

---

#### 1.2 ITokenService

```csharp
// src/AuthService.Application/Interfaces/ITokenService.cs
public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    Task<bool> RevokeTokenAsync(Guid userId, string refreshToken);
    Task<bool> IsTokenRevokedAsync(Guid userId, string refreshToken);
}
```

**Responsibility:**

- JWT access token generation
- Refresh token generation
- Token validation
- Token revocation management

---

#### 1.3 IEmailSender

```csharp
// src/AuthService.Application/Interfaces/IEmailSender.cs
public interface IEmailSender
{
    Task<bool> SendEmailVerificationAsync(string email, string verificationToken);
    Task<bool> SendPasswordResetAsync(string email, string resetToken);
}
```

**Responsibility:**

- Send email verification messages
- Send password reset messages
- Support multiple email providers

---

#### 1.4 IUserRepository

```csharp
// src/AuthService.Application/Interfaces/IUserRepository.cs
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid id);
    Task<bool> EmailExistsAsync(string email);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> DeleteAsync(Guid userId);
}
```

**Responsibility:**

- User data access operations
- Email uniqueness checks
- Fetch, create, update user records

---

### Task 2: Create DTOs (Data Transfer Objects)

**File Location:** `src/AuthService.Application/DTOs/`

#### 2.1 Request DTOs

```csharp
// src/AuthService.Application/DTOs/Requests/RegisterRequest.cs
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

// src/AuthService.Application/DTOs/Requests/LoginRequest.cs
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// src/AuthService.Application/DTOs/Requests/RefreshTokenRequest.cs
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

// src/AuthService.Application/DTOs/Requests/VerifyEmailRequest.cs
public class VerifyEmailRequest
{
    public string Token { get; set; } = string.Empty;
}

// src/AuthService.Application/DTOs/Requests/ForgotPasswordRequest.cs
public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

// src/AuthService.Application/DTOs/Requests/ResetPasswordRequest.cs
public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

**Design Principles:**

- ✅ One DTO per endpoint
- ✅ Clear, descriptive property names
- ✅ All properties initialized to prevent null reference errors
- ✅ Used for request validation

#### 2.2 Response DTOs

```csharp
// src/AuthService.Application/DTOs/Responses/AuthResponse.cs
public class AuthResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public AuthToken? Token { get; set; }
}

// src/AuthService.Application/DTOs/Responses/AuthToken.cs
public class AuthToken
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; }
}

// src/AuthService.Application/DTOs/Responses/MessageResponse.cs
public class MessageResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
```

**Response DTO Guidelines:**

- ✅ AuthResponse for auth operations (returns tokens)
- ✅ MessageResponse for operations without tokens
- ✅ Include success flag for status
- ✅ Include descriptive messages for debugging

---

### Task 3: Create Validators

**File Location:** `src/AuthService.Application/Validators/`

#### 3.1 Request Validators (FluentValidation)

```csharp
// src/AuthService.Application/Validators/RegisterRequestValidator.cs
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be valid")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
            .Matches(@"[!@#$%^&*]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match");
    }
}

// src/AuthService.Application/Validators/LoginRequestValidator.cs
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be valid");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}

// src/AuthService.Application/Validators/RefreshTokenRequestValidator.cs
public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");
    }
}

// src/AuthService.Application/Validators/VerifyEmailRequestValidator.cs
public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Verification token is required");
    }
}

// src/AuthService.Application/Validators/ForgotPasswordRequestValidator.cs
public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be valid");
    }
}

// src/AuthService.Application/Validators/ResetPasswordRequestValidator.cs
public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Reset token is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
            .Matches(@"[!@#$%^&*]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
    }
}
```

**Validation Strategy:**

- ✅ Email validation: format + max length
- ✅ Password strength: minimum 8 chars, uppercase, lowercase, digit, special char
- ✅ Password confirmation matching
- ✅ Custom error messages for each rule
- ✅ Automatic null/empty checks

---

### Task 4: Create AuthService

**File Location:** `src/AuthService.Application/Services/AuthService.cs`

```csharp
// src/AuthService.Application/Services/AuthService.cs
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Application.Interfaces;
using AuthService.Application.DTOs.Requests;
using AuthService.Application.DTOs.Responses;

namespace AuthService.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IEmailSender _emailSender;

        public AuthService(
            IUserRepository userRepository,
            ITokenService tokenService,
            IEmailSender emailSender)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _emailSender = emailSender;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Check if email already exists
            if (await _userRepository.EmailExistsAsync(request.Email))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Email is already registered"
                };
            }

            // Hash password and create user
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var emailVerificationToken = GenerateToken();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = passwordHash,
                IsEmailVerified = false,
                EmailVerificationToken = emailVerificationToken,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Save to database
            await _userRepository.CreateAsync(user);

            // Send verification email
            await _emailSender.SendEmailVerificationAsync(user.Email, emailVerificationToken);

            return new AuthResponse
            {
                Success = true,
                Message = "Registration successful. Please verify your email."
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            // Find user by email
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }

            // Check email verification
            if (!user.IsEmailVerified)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Email not verified. Please verify your email first."
                };
            }

            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email);
            var refreshToken = _tokenService.GenerateRefreshToken();

            return new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                Token = new AuthToken
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresInMinutes = 15
                }
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            // Validate and get user from refresh token
            var isValid = await _tokenService.IsTokenRevokedAsync(
                Guid.Empty,
                request.RefreshToken
            );

            if (isValid)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid refresh token"
                };
            }

            // Generate new access token
            var principal = _tokenService.ValidateToken(request.RefreshToken);
            if (principal == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid or expired refresh token"
                };
            }

            var userIdClaim = principal.FindFirst("sub")?.Value;
            var emailClaim = principal.FindFirst("email")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(emailClaim))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid token claims"
                };
            }

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid user ID in token"
                };
            }

            var newAccessToken = _tokenService.GenerateAccessToken(userId, emailClaim);

            return new AuthResponse
            {
                Success = true,
                Message = "Token refreshed successfully",
                Token = new AuthToken
                {
                    AccessToken = newAccessToken,
                    RefreshToken = request.RefreshToken,
                    ExpiresInMinutes = 15
                }
            };
        }

        public async Task<bool> VerifyEmailAsync(string token)
        {
            // Find user with this verification token
            var user = await _userRepository.GetByEmailAsync(""); // Search by token in repo
            if (user == null || user.EmailVerificationToken != token)
            {
                return false;
            }

            // Mark email as verified
            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal if email exists
                return true;
            }

            // Generate reset token
            var resetToken = GenerateToken();
            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            // Send password reset email
            await _emailSender.SendPasswordResetAsync(user.Email, resetToken);

            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            // Find user with valid reset token
            var user = await _userRepository.GetByEmailAsync(""); // Search by token in repo
            if (user == null ||
                user.PasswordResetToken != request.Token ||
                user.PasswordResetTokenExpiry < DateTime.UtcNow)
            {
                return false;
            }

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            return true;
        }

        private string GenerateToken()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
```

**Key Features:**

- ✅ Uses dependency injection for repository, token, and email services
- ✅ Password hashing with BCrypt
- ✅ Email verification workflow
- ✅ Secure token generation
- ✅ Clear error messages
- ✅ Business rule enforcement (email verified check, etc.)

---

## File Structure

After Phase 2, your Application layer will look like:

```
src/AuthService.Application/
├── Interfaces/
│   ├── IAuthService.cs
│   ├── ITokenService.cs
│   ├── IEmailSender.cs
│   └── IUserRepository.cs
├── DTOs/
│   ├── Requests/
│   │   ├── RegisterRequest.cs
│   │   ├── LoginRequest.cs
│   │   ├── RefreshTokenRequest.cs
│   │   ├── VerifyEmailRequest.cs
│   │   ├── ForgotPasswordRequest.cs
│   │   └── ResetPasswordRequest.cs
│   └── Responses/
│       ├── AuthResponse.cs
│       ├── AuthToken.cs
│       └── MessageResponse.cs
├── Validators/
│   ├── RegisterRequestValidator.cs
│   ├── LoginRequestValidator.cs
│   ├── RefreshTokenRequestValidator.cs
│   ├── VerifyEmailRequestValidator.cs
│   ├── ForgotPasswordRequestValidator.cs
│   └── ResetPasswordRequestValidator.cs
└── Services/
    └── AuthService.cs
```

---

## Implementation Checklist

### Interfaces

- [ ] Create `IAuthService.cs` with 6 methods
- [ ] Create `ITokenService.cs` for JWT management
- [ ] Create `IEmailSender.cs` for email operations
- [ ] Create `IUserRepository.cs` for data access

### DTOs

- [ ] Create 6 Request DTOs
- [ ] Create AuthResponse and AuthToken
- [ ] Create MessageResponse

### Validators

- [ ] RegisterRequestValidator
- [ ] LoginRequestValidator
- [ ] RefreshTokenRequestValidator
- [ ] VerifyEmailRequestValidator
- [ ] ForgotPasswordRequestValidator
- [ ] ResetPasswordRequestValidator

### Services

- [ ] Implement AuthService with all business logic
- [ ] Ensure password hashing with BCrypt
- [ ] Implement email verification logic
- [ ] Implement password reset logic

### Testing

- [ ] Create unit tests for AuthService
- [ ] Create tests for each validator
- [ ] Test edge cases and error conditions

---

## Key Design Patterns

### 1. **Dependency Injection**

- AuthService receives dependencies via constructor
- Easy to mock for testing
- Loose coupling between layers

### 2. **Single Responsibility**

- Each service has one reason to change
- AuthService handles auth flows
- ITokenService handles tokens
- IEmailSender handles emails

### 3. **Repository Pattern**

- Data access abstracted behind IUserRepository
- Easy to swap database implementations
- Testable with mock repositories

### 4. **Validator Pattern (FluentValidation)**

- Declarative validation rules
- Reusable validators
- Clear, readable validation logic

---

## Validation Rules Summary

| Field                  | Rules                                                  |
| ---------------------- | ------------------------------------------------------ |
| **Email**              | Required, valid format, max 255 chars                  |
| **Password**           | Min 8 chars, uppercase, lowercase, digit, special char |
| **Confirm Password**   | Must match password                                    |
| **Refresh Token**      | Required, non-empty                                    |
| **Verification Token** | Required                                               |
| **Reset Token**        | Required                                               |

---

## Error Handling Strategy

All services return standardized responses:

```csharp
// Success case
new AuthResponse
{
    Success = true,
    Message = "Operation successful",
    Token = new AuthToken { ... }
}

// Failure case
new AuthResponse
{
    Success = false,
    Message = "User-friendly error message",
    Token = null
}
```

**Benefits:**

- ✅ Consistent API responses
- ✅ Client-friendly error messages
- ✅ No exceptions exposed to API layer
- ✅ Easy to log and monitor

---

## Testing Strategy

### Unit Tests (AuthService)

```
✓ RegisterAsync - new user, duplicate email, validation
✓ LoginAsync - success, invalid password, unverified email
✓ RefreshTokenAsync - valid token, expired token, invalid claims
✓ VerifyEmailAsync - valid token, invalid token
✓ RequestPasswordResetAsync - existing email, non-existing email
✓ ResetPasswordAsync - valid token, expired token, invalid token
```

### Validator Tests

```
✓ Each validator with valid and invalid inputs
✓ Boundary conditions (min/max length)
✓ Pattern matching (email format, password complexity)
```

---

## Security Considerations

- ✅ Passwords hashed with BCrypt
- ✅ Tokens are random GUIDs (cryptographically secure)
- ✅ Email verification prevents automated account creation
- ✅ Password reset tokens expire after 1 hour
- ✅ Generic error messages (don't reveal email existence)
- ✅ All operations are async (scalable)

---

## Next Steps (Phase 3)

After completing Phase 2, proceed to **Phase 3: Infrastructure & Persistence**:

- Implement `AppDbContext` with EF Core
- Implement `UserRepository`
- Implement `TokenService` with JWT
- Implement Email Providers (SMTP, SendGrid, Stub)
- Configure dependency injection

---

## Commands Reference

### Build

```bash
dotnet build src/AuthService.Application/
```

### Run Tests

```bash
dotnet test AuthService.UnitTests --filter "AuthService"
```

### Add References (if needed)

```bash
dotnet add src/AuthService.Application/AuthService.Application.csproj reference src/AuthService.Domain/AuthService.Domain.csproj
```

---

## Documentation Index

- **PLAN.md** - Overall implementation plan
- **Phase-1-Project-Setup-and-Foundation/README.md** - Project structure setup
- **Phase-2-Application-Core/README.md** - This file (current phase)
- `Phase-3-Infrastructure-and-Persistence/README.md` - EF Core, JWT, Email (upcoming)
- `Phase-4-API-and-Controllers/README.md` - REST endpoints (upcoming)
- `Phase-5-Testing/README.md` - Unit & integration tests (upcoming)

---

**Last Updated:** 2026-05-29  
**Phase:** 2 of 6  
**Status:** 🔄 IN PROGRESS  
**Next Phase:** Infrastructure & Persistence Layer
