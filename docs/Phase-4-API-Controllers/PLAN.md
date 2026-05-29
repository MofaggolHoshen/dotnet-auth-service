# Phase 4: API Controllers & Middleware Implementation Plan

**Framework:** ASP.NET Core 9 | **API Style:** RESTful | **Auth:** JWT Bearer  
**Status:** PLANNING  
**Estimated LOC:** 800+

---

## Overview

Phase 4 implements the API layer with:

- **AuthController** - 6 RESTful endpoints for authentication workflows
- **Exception Middleware** - Global error handling and consistent responses
- **JWT Authentication** - Bearer token validation in Program.cs
- **CORS Configuration** - Cross-origin resource sharing

---

## Task Breakdown

### Task 4.1: AuthController Implementation

**File to Create:**

- `src/AuthService.API/Controllers/AuthController.cs` (400 lines)

**Endpoints to Implement:**

#### 1. POST /api/auth/register

```http
Request:
{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!"
}

Response (Success - 201):
{
  "success": true,
  "message": "Registration successful. Please verify your email.",
  "token": null
}

Response (Error - 400):
{
  "success": false,
  "message": "Email already registered"
}
```

#### 2. POST /api/auth/login

```http
Request:
{
  "email": "user@example.com",
  "password": "SecurePass123!"
}

Response (Success - 200):
{
  "success": true,
  "message": "Login successful",
  "token": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "abc123def456ghi789...",
    "expiresInMinutes": 15
  }
}

Response (Error - 401):
{
  "success": false,
  "message": "Invalid email or password"
}
```

#### 3. POST /api/auth/refresh-token

```http
Request:
{
  "refreshToken": "abc123def456ghi789..."
}

Response (Success - 200):
{
  "success": true,
  "message": "Token refreshed successfully",
  "token": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "abc123def456ghi789...",
    "expiresInMinutes": 15
  }
}
```

#### 4. POST /api/auth/verify-email

```http
Request:
{
  "token": "email-verification-token-here"
}

Response (Success - 200):
{
  "success": true,
  "message": "Email verified successfully"
}
```

#### 5. POST /api/auth/forgot-password

```http
Request:
{
  "email": "user@example.com"
}

Response (Always 200 - Security):
{
  "success": true,
  "message": "If email exists, password reset link will be sent"
}
```

#### 6. POST /api/auth/reset-password

```http
Request:
{
  "token": "password-reset-token-here",
  "newPassword": "NewSecurePass456!",
  "confirmPassword": "NewSecurePass456!"
}

Response (Success - 200):
{
  "success": true,
  "message": "Password reset successfully"
}
```

**Features:**

- Attribute routing with [ApiController]
- FluentValidation for request validation
- Dependency injection of IAuthService
- Proper HTTP status codes (201 for create, 200 for success, 400 for validation, 401 for auth)
- Async/await for all operations
- Problem Details responses (RFC 7807)

---

### Task 4.2: Exception Handling Middleware

**Files to Create:**

- `src/AuthService.API/Middleware/ExceptionHandlingMiddleware.cs` (200 lines)
- `src/AuthService.API/Exceptions/ApiException.cs` (50 lines)
- `src/AuthService.API/Models/ErrorResponse.cs` (50 lines)

**Features:**

- Global try-catch for all exceptions
- Exception type mapping to HTTP status codes
- Logging of exceptions
- Consistent error response format
- No sensitive information in responses

**Exception Types:**
| Exception | Status | Message |
|-----------|--------|---------|
| ValidationException | 400 | Validation failed: {details} |
| AuthException | 401 | Authentication failed |
| UnauthorizedAccessException | 403 | Forbidden |
| NotFoundException | 404 | Resource not found |
| DbUpdateException | 409 | Conflict (duplicate email) |
| Exception | 500 | Internal server error |

---

### Task 4.3: JWT Authentication Configuration

**File to Modify:**

- `src/AuthService.API/Program.cs` - Add authentication middleware

**Configuration:**

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true
        };
    });

app.UseAuthentication();
app.UseAuthorization();
```

**Features:**

- JWT Bearer token validation
- Issuer and audience validation
- Lifetime/expiry validation
- Signature verification
- Claims principal extraction

---

### Task 4.4: CORS Configuration

**File to Modify:**

- `src/AuthService.API/Program.cs` - Add CORS policy

**Configuration:**

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

app.UseCors("AllowAll");
```

**Policy Options (for production):**

- Specific origins
- Specific methods
- Specific headers
- Credentials handling
- Max age

---

### Task 4.5: API Documentation & Swagger

**File to Modify:**

- `src/AuthService.API/Program.cs` - Add Swagger/OpenAPI

**Features:**

- OpenAPI/Swagger UI
- Endpoint documentation
- Request/response schemas
- Authentication scheme
- Authorization buttons

**Implementation:**

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Auth Service API",
        Version = "v1",
        Description = "JWT-based authentication service"
    });
});

app.UseSwagger();
app.UseSwaggerUI();
```

---

## API Structure

```
AuthController
├── [HttpPost] Register
│   ├── Validates request (FluentValidation)
│   ├── Calls AuthService.RegisterAsync()
│   └── Returns 201 or 400
│
├── [HttpPost] Login
│   ├── Validates request
│   ├── Calls AuthService.LoginAsync()
│   └── Returns 200 with tokens or 401
│
├── [HttpPost] RefreshToken
│   ├── Validates request
│   ├── Calls AuthService.RefreshTokenAsync()
│   └── Returns 200 with new token or 401
│
├── [HttpPost] VerifyEmail
│   ├── Validates request
│   ├── Calls AuthService.VerifyEmailAsync()
│   └── Returns 200 or 400
│
├── [HttpPost] ForgotPassword
│   ├── Validates request
│   ├── Calls AuthService.ForgotPasswordAsync()
│   └── Returns 200 (always for security)
│
└── [HttpPost] ResetPassword
    ├── Validates request
    ├── Calls AuthService.ResetPasswordAsync()
    └── Returns 200 or 400
```

---

## HTTP Status Codes

| Code | Usage                                   |
| ---- | --------------------------------------- |
| 200  | Successful operation (GET, POST update) |
| 201  | Resource created (registration)         |
| 400  | Bad request (validation error)          |
| 401  | Unauthorized (invalid credentials)      |
| 403  | Forbidden (insufficient permissions)    |
| 404  | Not found                               |
| 409  | Conflict (duplicate email)              |
| 500  | Internal server error                   |

---

## Error Response Format

All errors follow RFC 7807 Problem Details:

```json
{
  "type": "https://api.example.com/errors/validation-failed",
  "title": "Validation Failed",
  "status": 400,
  "detail": "Email is required",
  "instance": "/api/auth/register",
  "errors": {
    "email": ["Email is required"]
  }
}
```

---

## Files Summary

| File                           | Lines | Purpose                  |
| ------------------------------ | ----- | ------------------------ |
| AuthController.cs              | 400   | 6 API endpoints          |
| ExceptionHandlingMiddleware.cs | 200   | Global error handling    |
| ApiException.cs                | 50    | Custom exception         |
| ErrorResponse.cs               | 50    | Error model              |
| Program.cs (modified)          | +150  | Middleware configuration |

**Total:** 800+ lines of code

---

## Success Criteria

- [ ] AuthController builds without errors
- [ ] All 6 endpoints respond correctly
- [ ] FluentValidation validates requests
- [ ] JWT authentication works
- [ ] Exception middleware catches all errors
- [ ] Swagger/OpenAPI working
- [ ] CORS configured
- [ ] All status codes correct

---

## Testing Endpoints

```bash
# Register
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"user@test.com","password":"Pass123!","confirmPassword":"Pass123!"}'

# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@test.com","password":"Pass123!"}'

# Refresh Token
curl -X POST http://localhost:5000/api/auth/refresh-token \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"<token>"}'
```
