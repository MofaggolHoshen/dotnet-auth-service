# Phase 2 Implementation Guide

**Quick Reference for Phase 2 Development**

---

## Step-by-Step Implementation

### Step 1: Create Interfaces Folder

```bash
mkdir src/AuthService.Application/Interfaces
```

### Step 2: Implement IAuthService

Create `src/AuthService.Application/Interfaces/IAuthService.cs`:

- Register new users
- Login authentication
- Refresh tokens
- Verify email
- Password reset flow

### Step 3: Implement ITokenService

Create `src/AuthService.Application/Interfaces/ITokenService.cs`:

- Generate JWT access tokens
- Generate refresh tokens
- Validate tokens
- Revoke tokens

### Step 4: Implement IEmailSender

Create `src/AuthService.Application/Interfaces/IEmailSender.cs`:

- Send verification emails
- Send password reset emails

### Step 5: Implement IUserRepository

Create `src/AuthService.Application/Interfaces/IUserRepository.cs`:

- Get user by email
- Get user by ID
- Check email exists
- Create user
- Update user
- Delete user

### Step 6: Create DTOs

Create `src/AuthService.Application/DTOs/` folder structure:

**Requests:**

- RegisterRequest
- LoginRequest
- RefreshTokenRequest
- VerifyEmailRequest
- ForgotPasswordRequest
- ResetPasswordRequest

**Responses:**

- AuthResponse
- AuthToken
- MessageResponse

### Step 7: Create Validators

Create `src/AuthService.Application/Validators/` with:

- RegisterRequestValidator
- LoginRequestValidator
- RefreshTokenRequestValidator
- VerifyEmailRequestValidator
- ForgotPasswordRequestValidator
- ResetPasswordRequestValidator

**Password Validation Rules:**

```
✓ Minimum 8 characters
✓ At least one uppercase letter (A-Z)
✓ At least one lowercase letter (a-z)
✓ At least one digit (0-9)
✓ At least one special character (!@#$%^&*)
```

### Step 8: Implement AuthService

Create `src/AuthService.Application/Services/AuthService.cs`:

- Implements IAuthService
- Orchestrates business logic
- Uses IUserRepository, ITokenService, IEmailSender
- Returns consistent AuthResponse objects

---

## Code Snippets Quick Reference

### Implementing an Interface (Template)

```csharp
namespace AuthService.Application.Interfaces
{
    public interface IMyService
    {
        Task<MyResponse> DoSomethingAsync(MyRequest request);
    }
}
```

### Creating a DTO (Template)

```csharp
namespace AuthService.Application.DTOs.Requests
{
    public class MyRequest
    {
        public string Field1 { get; set; } = string.Empty;
        public string Field2 { get; set; } = string.Empty;
    }
}
```

### Creating a Validator (Template)

```csharp
using FluentValidation;
using AuthService.Application.DTOs.Requests;

namespace AuthService.Application.Validators
{
    public class MyRequestValidator : AbstractValidator<MyRequest>
    {
        public MyRequestValidator()
        {
            RuleFor(x => x.Field1)
                .NotEmpty().WithMessage("Field1 is required")
                .MaximumLength(255).WithMessage("Field1 max 255 chars");
        }
    }
}
```

### Creating a Service (Template)

```csharp
using AuthService.Application.Interfaces;

namespace AuthService.Application.Services
{
    public class MyService : IMyService
    {
        private readonly IMyDependency _dependency;

        public MyService(IMyDependency dependency)
        {
            _dependency = dependency;
        }

        public async Task<MyResponse> DoSomethingAsync(MyRequest request)
        {
            // Implementation
            return new MyResponse { Success = true };
        }
    }
}
```

---

## Password Hashing Reference

### Hashing a Password

```csharp
using BCrypt.Net;

string plainPassword = "MyPassword123!";
string hashedPassword = BCrypt.HashPassword(plainPassword);
```

### Verifying a Password

```csharp
bool isValid = BCrypt.Verify(plainPassword, hashedPassword);
```

---

## Token Generation Reference

### Generate Email Verification Token

```csharp
string token = Guid.NewGuid().ToString("N");
```

### Generate Reset Token

```csharp
string token = Guid.NewGuid().ToString("N");
```

---

## AuthResponse Pattern

### Success Response

```csharp
return new AuthResponse
{
    Success = true,
    Message = "Operation successful",
    Token = new AuthToken
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        ExpiresInMinutes = 15
    }
};
```

### Failure Response

```csharp
return new AuthResponse
{
    Success = false,
    Message = "User-friendly error message",
    Token = null
};
```

---

## Common Validation Rules

### Email Validation

```csharp
RuleFor(x => x.Email)
    .NotEmpty().WithMessage("Email is required")
    .EmailAddress().WithMessage("Invalid email format")
    .MaximumLength(255).WithMessage("Email too long");
```

### Password Validation (Strong)

```csharp
RuleFor(x => x.Password)
    .NotEmpty().WithMessage("Password required")
    .MinimumLength(8).WithMessage("Min 8 characters")
    .Matches(@"[A-Z]").WithMessage("Needs uppercase")
    .Matches(@"[a-z]").WithMessage("Needs lowercase")
    .Matches(@"[0-9]").WithMessage("Needs digit")
    .Matches(@"[!@#$%^&*]").WithMessage("Needs special char");
```

### Confirm Password

```csharp
RuleFor(x => x.ConfirmPassword)
    .Equal(x => x.Password).WithMessage("Passwords don't match");
```

---

## Build & Test Commands

### Build Only Application Layer

```bash
dotnet build src/AuthService.Application/
```

### Build All

```bash
dotnet build AuthService.sln
```

### Run Specific Tests

```bash
dotnet test AuthService.UnitTests --filter "AuthService"
```

### Check for Compilation Errors

```bash
dotnet build src/AuthService.Application/ --no-restore
```

---

## Troubleshooting

### Issue: "Cannot find namespace"

**Solution:** Add using statements at top of file

```csharp
using AuthService.Domain.Entities;
using AuthService.Application.Interfaces;
```

### Issue: "Constructor not found"

**Solution:** Ensure interface parameters are passed to constructor

```csharp
private readonly IUserRepository _userRepository;

public AuthService(IUserRepository userRepository)
{
    _userRepository = userRepository;
}
```

### Issue: "Task is not defined"

**Solution:** Add `using System.Threading.Tasks;` or use `System.Threading.Tasks.Task`

### Issue: "ClaimsPrincipal not found"

**Solution:** Add `using System.Security.Claims;`

---

## Key Namespace Imports

### For Services

```csharp
using System;
using System.Threading.Tasks;
using BCrypt.Net;
using AuthService.Domain.Entities;
using AuthService.Application.Interfaces;
using AuthService.Application.DTOs.Requests;
using AuthService.Application.DTOs.Responses;
```

### For Validators

```csharp
using FluentValidation;
using AuthService.Application.DTOs.Requests;
```

### For Interfaces

```csharp
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using AuthService.Domain.Entities;
using AuthService.Application.DTOs.Requests;
using AuthService.Application.DTOs.Responses;
```

---

## File Organization

After Phase 2 completion:

```
src/AuthService.Application/
├── AuthService.Application.csproj
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

## Dependency Injection Setup (Phase 3 Preview)

In Phase 3's Program.cs, you'll register services:

```csharp
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<ITokenService, TokenService>();
services.AddScoped<IEmailSender, SmtpEmailSender>();
services.AddScoped<IUserRepository, UserRepository>();
```

This enables constructor injection throughout the app.

---

## Testing Checklist

### For Each Validator

- [ ] Test with valid input → should pass
- [ ] Test with empty fields → should fail
- [ ] Test with invalid email → should fail
- [ ] Test with weak password → should fail
- [ ] Test with mismatched passwords → should fail
- [ ] Test boundary conditions → should behave correctly

### For AuthService

- [ ] RegisterAsync with new email → should succeed
- [ ] RegisterAsync with existing email → should fail
- [ ] LoginAsync with correct credentials → should return tokens
- [ ] LoginAsync with wrong password → should fail
- [ ] LoginAsync with unverified email → should fail
- [ ] RefreshTokenAsync with valid token → should return new token
- [ ] VerifyEmailAsync with correct token → should succeed

---

## Performance Considerations

- ✅ All methods are async (don't block)
- ✅ Use Task-based APIs throughout
- ✅ Repository handles database optimization
- ✅ Token service handles caching (in Phase 3)

---

## Security Checklist

- ✅ Passwords hashed with BCrypt
- ✅ Generic error messages (don't leak info)
- ✅ Email verification required before login
- ✅ Password reset tokens expire
- ✅ Refresh tokens can be revoked

---

**Quick Links:**

- Main PLAN.md with all phases
- Phase 1: Project setup (completed)
- Phase 3: Infrastructure (upcoming)
- Phase 4: API controllers (upcoming)
