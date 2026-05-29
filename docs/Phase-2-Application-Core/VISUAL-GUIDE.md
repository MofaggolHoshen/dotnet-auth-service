# Phase 2: Visual Implementation Guide

**Diagrams and flowcharts for Phase 2 implementation**

---

## 1. Application Layer Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                    API Layer (Controllers)                    │
│                                                               │
│  AuthController handles HTTP requests and delegates to       │
│  AuthService (business logic)                                │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│              Application Layer (THIS PHASE)                   │
│                                                               │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │ Interfaces (Abstract contracts)                         │ │
│  │ • IAuthService ─────────────────────────┐               │ │
│  │ • ITokenService                         │               │ │
│  │ • IEmailSender                          │               │ │
│  │ • IUserRepository                       │               │ │
│  └─────────────────────────────────────────┼───────────────┘ │
│                                            │                 │
│  ┌─────────────────────────────────────────┼───────────────┐ │
│  │ AuthService (Implementation)            │               │ │
│  │                                         ↓               │ │
│  │ public class AuthService : IAuthService │               │ │
│  │ {                                       │               │ │
│  │   - RegisterAsync()                     │               │ │
│  │   - LoginAsync()                        │               │ │
│  │   - RefreshTokenAsync()                 │               │ │
│  │   - VerifyEmailAsync()                  │               │ │
│  │   - RequestPasswordResetAsync()         │               │ │
│  │   - ResetPasswordAsync()                │               │ │
│  │ }                                       │               │ │
│  └─────────────────────────────────────────┼───────────────┘ │
│                                            │                 │
│  ┌─────────────────────────────────────────┼───────────────┐ │
│  │ DTOs (Data Transfer Objects)            │               │ │
│  │ Requests:                               │               │ │
│  │ • RegisterRequest ├─→ Validated ────┐   │               │ │
│  │ • LoginRequest    ├─→ Validated ────┼─→ AuthService    │ │
│  │ • (4 more)        ├─→ Validated ────┘   │               │ │
│  │                                          │               │ │
│  │ Responses:                               │               │ │
│  │ • AuthResponse  ←─┤                      │               │ │
│  │ • AuthToken     ←─┤ Returned by service  │               │ │
│  │ • MessageResponse                        │               │ │
│  └──────────────────────────────────────────┘               │ │
│                                                               │ │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │ Validators (FluentValidation)                            │ │
│  │                                                           │ │
│  │ Each DTO has a corresponding Validator:                 │ │
│  │ • RegisterRequestValidator  ←─────────────┐             │ │
│  │ • LoginRequestValidator                   │             │ │
│  │ • (4 more)                                │ validates   │ │
│  │                                           ↓             │ │
│  │                                   Request DTOs          │ │
│  └──────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│                 Domain Layer (Entities)                       │
│                                                               │
│  • User (entity with properties)                             │
│  • RefreshToken (entity for token management)               │
│  • UserStatus (enum: Active, Inactive, etc.)                │
└──────────────────────────────────────────────────────────────┘
```

---

## 2. Dependency Injection Flow

```
Phase 3 Configuration (Program.cs):
│
├─ services.AddScoped<IAuthService, AuthService>();
│  │
│  └─ AuthService ctor needs:
│     ├─ IUserRepository
│     ├─ ITokenService
│     └─ IEmailSender
│
├─ services.AddScoped<IUserRepository, UserRepository>();
│  │
│  └─ UserRepository ctor needs:
│     └─ AppDbContext
│
├─ services.AddScoped<ITokenService, TokenService>();
│  └─ (Phase 3: JWT implementation)
│
└─ services.AddScoped<IEmailSender, SmtpEmailSender>();
   └─ (Phase 3: Email provider implementation)

Result:
When API Controller requests IAuthService:
  DI Container provides AuthService with:
    - Real UserRepository (connected to DB)
    - Real TokenService (JWT generation)
    - Real EmailSender (SMTP/SendGrid)
```

---

## 3. Registration Flow (Detailed)

```
┌─ Client submits POST /api/auth/register
│  Body: { email, password, confirmPassword }
│
├─ Controller: AuthController.Register()
│  │
│  ├─ Deserialize → RegisterRequest DTO
│  │
│  ├─ Validate using RegisterRequestValidator
│  │  │
│  │  └─ FluentValidation checks:
│  │     ├─ email is not empty
│  │     ├─ email is valid format
│  │     ├─ email ≤ 255 chars
│  │     ├─ password is not empty
│  │     ├─ password ≥ 8 chars
│  │     ├─ password has uppercase
│  │     ├─ password has lowercase
│  │     ├─ password has digit
│  │     ├─ password has special char
│  │     └─ confirmPassword == password
│  │
│  ├─ If validation fails → Return 400 Bad Request
│  │
│  └─ Call authService.RegisterAsync(request)
│
├─ AuthService.RegisterAsync(request)
│  │
│  ├─ Check if email already exists
│  │  └─ Call _userRepository.EmailExistsAsync(email)
│  │     If TRUE: return { Success: false, Message: "Already registered" }
│  │
│  ├─ Hash password
│  │  └─ string hash = BCrypt.HashPassword(password)
│  │     Input: "MyPass123!"
│  │     Output: "$2a$11$abcdefghijklmnopqrst..." (60 chars)
│  │
│  ├─ Generate verification token
│  │  └─ string token = Guid.NewGuid().ToString("N")
│  │     Output: "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6"
│  │
│  ├─ Create User entity
│  │  └─ new User
│  │     {
│  │       Id = Guid.NewGuid(),
│  │       Email = request.Email,
│  │       PasswordHash = hash,
│  │       IsEmailVerified = false,
│  │       EmailVerificationToken = token,
│  │       Status = UserStatus.Active,
│  │       CreatedAt = DateTime.UtcNow,
│  │       UpdatedAt = DateTime.UtcNow
│  │     }
│  │
│  ├─ Save to database
│  │  └─ await _userRepository.CreateAsync(user)
│  │     INSERT INTO Users (Id, Email, PasswordHash, ...)
│  │
│  ├─ Send verification email
│  │  └─ await _emailSender.SendEmailVerificationAsync(email, token)
│  │     Email to: user@example.com
│  │     Body: "Click here: https://...?token={token}"
│  │
│  └─ Return success response
│     return new AuthResponse
│     {
│       Success = true,
│       Message = "Registration successful. Check your email.",
│       Token = null
│     }
│
└─ API Response: 200 OK
   {
     "success": true,
     "message": "Registration successful...",
     "token": null
   }
```

---

## 4. Login Flow (Detailed)

```
┌─ Client submits POST /api/auth/login
│  Body: { email, password }
│
├─ Validate RegisterRequestValidator
│  └─ email required and valid format
│     password required
│
├─ AuthService.LoginAsync(request)
│  │
│  ├─ Find user by email
│  │  └─ User? user = await _userRepository.GetByEmailAsync(email)
│  │     SELECT * FROM Users WHERE Email = ? (case-insensitive)
│  │
│  ├─ If user not found
│  │  └─ Return { Success: false, Message: "Invalid credentials" }
│  │     ⚠️  Don't say "Email not found" (security)
│  │
│  ├─ Verify password
│  │  └─ bool valid = BCrypt.Verify(request.Password, user.PasswordHash)
│  │     Input password: "MyPass123!"
│  │     Stored hash: "$2a$11$abcdefghijklmnopqrst..."
│  │     Result: true or false
│  │
│  ├─ If password invalid
│  │  └─ Return { Success: false, Message: "Invalid credentials" }
│  │
│  ├─ Check email verified
│  │  ├─ if (!user.IsEmailVerified)
│  │  └─ Return { Success: false, Message: "Email not verified" }
│  │
│  ├─ Generate access token
│  │  └─ string accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email)
│  │     Returns JWT:
│  │     eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.
│  │     eyJzdWIiOiJ1c2VyLWlkIiwiZW1haWwiOiJ1c2VyQGV4YW1wbGUuY29tIn0.
│  │     Signature...
│  │
│  ├─ Generate refresh token
│  │  └─ string refreshToken = _tokenService.GenerateRefreshToken()
│  │     Returns: "a1b2c3d4-e5f6-g7h8-i9j0-k1l2m3n4o5p6"
│  │
│  └─ Return success with tokens
│     return new AuthResponse
│     {
│       Success = true,
│       Message = "Login successful",
│       Token = new AuthToken
│       {
│         AccessToken = accessToken,
│         RefreshToken = refreshToken,
│         ExpiresInMinutes = 15
│       }
│     }
│
└─ API Response: 200 OK
   {
     "success": true,
     "message": "Login successful",
     "token": {
       "accessToken": "eyJhbGci...",
       "refreshToken": "a1b2c3d4-e5f6-...",
       "expiresInMinutes": 15
     }
   }
```

---

## 5. Password Complexity Visual

```
Valid Password: "MyPass123!"

✓ Length ≥ 8 characters
  M y P a s s 1 2 3 !
  ^ 10 characters

✓ At least one uppercase (A-Z)
  M (position 0)
  P (position 2)

✓ At least one lowercase (a-z)
  y (position 1)
  a, s, s (positions 3-5)

✓ At least one digit (0-9)
  1, 2, 3 (positions 6-8)

✓ At least one special character (!@#$%^&*)
  ! (position 9)

Result: ✅ VALID PASSWORD


Invalid Password: "pass1"

✗ Length < 8 characters
  p a s s 1
  ^ Only 5 characters

✗ At least one uppercase (A-Z)
  (none present)

✓ At least one lowercase (a-z)
  p, a, s, s (present)

✓ At least one digit (0-9)
  1 (present)

✗ At least one special character (!@#$%^&*)
  (none present)

Result: ❌ INVALID PASSWORD
Errors:
  - "Password must be at least 8 characters"
  - "Password must contain at least one uppercase letter"
  - "Password must contain at least one special character"
```

---

## 6. Email Verification Flow

```
After Registration:

┌─ User receives email
│  Subject: "Verify Your Email - Authentication Service"
│  Body: "Click here: https://yourdomain.com/verify?token={token}"
│
├─ User clicks link
│  │
│  └─ GET /api/auth/verify-email?token={token}
│
├─ Controller: AuthController.VerifyEmail(token)
│  │
│  └─ await authService.VerifyEmailAsync(token)
│
├─ AuthService.VerifyEmailAsync(token)
│  │
│  ├─ Find user with matching verification token
│  │  └─ User? user = await _userRepository.GetByTokenAsync(token)
│  │     SELECT * FROM Users WHERE EmailVerificationToken = ?
│  │
│  ├─ If not found → return false
│  │
│  ├─ Update user
│  │  └─ user.IsEmailVerified = true
│  │     user.EmailVerificationToken = null
│  │     user.UpdatedAt = DateTime.UtcNow
│  │     await _userRepository.UpdateAsync(user)
│  │     UPDATE Users SET IsEmailVerified = 1, EmailVerificationToken = NULL
│  │
│  └─ return true
│
└─ User can now login ✓
```

---

## 7. Token Refresh Flow

```
Initial Login:
┌─────────────────────────────────────────────────────┐
│ POST /api/auth/login                                │
│ Response:                                           │
│ {                                                   │
│   accessToken: "eyJhbGci..." (expires in 15 min)  │
│   refreshToken: "abc-123-def-456"                  │
│ }                                                   │
└─────────────────────────────────────────────────────┘

After 15 minutes:
┌─────────────────────────────────────────────────────┐
│ Client attempts API call with old accessToken      │
│ Response: 401 Unauthorized (token expired)         │
└─────────────────────────────────────────────────────┘

Then:
┌─────────────────────────────────────────────────────┐
│ POST /api/auth/refresh-token                        │
│ Body: { refreshToken: "abc-123-def-456" }          │
│                                                     │
│ AuthService.RefreshTokenAsync():                   │
│ 1. Check if refreshToken is revoked                │
│    await _tokenService.IsTokenRevokedAsync()       │
│    (check database revocation list)                │
│    If revoked → return failure                     │
│                                                     │
│ 2. Extract claims from old token                   │
│    userId, email → used in JWT payload             │
│                                                     │
│ 3. Generate new accessToken                        │
│    _tokenService.GenerateAccessToken(userId, email)│
│    (new JWT with same user, new exp time)          │
│                                                     │
│ Response:                                           │
│ {                                                   │
│   accessToken: "eyJhbGci..." (NEW token)           │
│   refreshToken: "abc-123-def-456" (same)           │
│ }                                                   │
└─────────────────────────────────────────────────────┘

Continue with new token:
┌─────────────────────────────────────────────────────┐
│ API calls with new accessToken: 200 OK ✓           │
└─────────────────────────────────────────────────────┘
```

---

## 8. Service Dependencies

```
AuthService depends on:

                    ┌─────────────────────┐
                    │   IAuthService      │
                    │   (Implement)       │
                    └─────────────────────┘
                           ↑
        ┌──────────────────┼──────────────────┐
        │                  │                  │
        ↓                  ↓                  ↓
   ┌────────────┐   ┌─────────────┐   ┌──────────────┐
   │IUserRepos  │   │ITokenService│   │IEmailSender  │
   │(Phase 3)   │   │(Phase 3)    │   │(Phase 3)     │
   │Interfaces: │   │Interfaces:  │   │Interfaces:   │
   │- GetById   │   │- GenAccToken│   │- SendVerify  │
   │- GetByEmail│   │- GenRefresh │   │- SendReset   │
   │- Create    │   │- Validate   │   │              │
   │- Update    │   │- Revoke     │   │Implementations
   │- Delete    │   │- IsRevoked  │   │- Smtp        │
   │- Exists    │   │             │   │- SendGrid    │
   │            │   │             │   │- Stub        │
   └────────────┘   └─────────────┘   └──────────────┘
```

---

## 9. Implementation Priority

```
MUST DO (Core):
┌──────────────────────────────────────────┐
│ 1. IAuthService interface                │
│    (6 methods, defines contract)         │
│ 2. DTOs (Request + Response)              │
│    (needed for validation & responses)    │
│ 3. Validators (FluentValidation)         │
│    (validate inputs)                     │
│ 4. AuthService implementation             │
│    (business logic)                      │
└──────────────────────────────────────────┘
           ↓
Must implement in this order
(later ones depend on earlier)


SUPPORTING (Interfaces, to be implemented in Phase 3):
┌──────────────────────────────────────────┐
│ 5. ITokenService interface               │
│    (needed by AuthService)               │
│ 6. IEmailSender interface                │
│    (needed by AuthService)               │
│ 7. IUserRepository interface             │
│    (needed by AuthService)               │
└──────────────────────────────────────────┘
     Phase 2: Define
     Phase 3: Implement
```

---

## 10. Testing Strategy

```
Unit Tests (Test in isolation):

┌─────────────────────────────────────┐
│ RegisterAsync Tests                 │
├─────────────────────────────────────┤
│ ✓ Register with valid email         │
│ ✓ Register with duplicate email     │
│ ✓ Register with invalid email       │
│ ✓ Register with weak password       │
│ ✓ Password verification sent        │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ LoginAsync Tests                    │
├─────────────────────────────────────┤
│ ✓ Login with correct credentials    │
│ ✓ Login with wrong password         │
│ ✓ Login with unverified email       │
│ ✓ Login with non-existent email     │
│ ✓ Tokens returned on success        │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ Validator Tests                     │
├─────────────────────────────────────┤
│ ✓ Empty email validation            │
│ ✓ Invalid email validation          │
│ ✓ Weak password validation          │
│ ✓ Password mismatch validation      │
│ ✓ Boundary conditions               │
└─────────────────────────────────────┘

Integration Tests (Test with real DB):

┌─────────────────────────────────────┐
│ End-to-End Flows                    │
├─────────────────────────────────────┤
│ ✓ Register → Verify → Login         │
│ ✓ Login → GetToken → Refresh        │
│ ✓ Forgot Password → Reset           │
│ ✓ Email notification triggers       │
└─────────────────────────────────────┘
```

---

**Visual Guide Complete**  
All diagrams are simplified for clarity.  
See detailed specs in TECHNICAL-SPECS.md for complete information.
