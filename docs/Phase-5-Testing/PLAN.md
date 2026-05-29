# Phase 5: Testing & Quality Assurance Implementation Plan

**Framework:** xUnit | **Mocking:** Moq | **Assertions:** FluentAssertions  
**Status:** PLANNING  
**Estimated LOC:** 2,000+

---

## Overview

Phase 5 implements comprehensive testing:

- **Unit Tests** - AuthService, validators, TokenService business logic (~80% coverage)
- **Integration Tests** - API endpoints with real database (in-memory or LocalDB)
- **Test Infrastructure** - Fixtures, factories, test database setup
- **CI/CD Ready** - Tests run in build pipeline

---

## Task Breakdown

### Task 5.1: Unit Tests for AuthService

**File to Create:**

- `tests/AuthService.UnitTests/Services/AuthServiceTests.cs` (600 lines)

**Test Classes & Methods:**

#### RegisterAsyncTests

- [ ] RegisterAsync_WithValidRequest_ReturnsSuccess
  - Valid registration data → Success response
- [ ] RegisterAsync_WithDuplicateEmail_ReturnsFails
  - Email already exists → Failure response
- [ ] RegisterAsync_WithPasswordMismatch_ReturnsValidationError
  - Password != ConfirmPassword → Error
- [ ] RegisterAsync_SendsVerificationEmail
  - RegisterAsync called → IEmailSender.SendAsync called
- [ ] RegisterAsync_GeneratesVerificationToken
  - User.EmailVerificationToken should be set

#### LoginAsyncTests

- [ ] LoginAsync_WithValidCredentials_ReturnsTokens
  - Valid email/password → AccessToken and RefreshToken returned
- [ ] LoginAsync_WithInvalidEmail_ReturnsFailure
  - Email doesn't exist → Generic failure message
- [ ] LoginAsync_WithInvalidPassword_ReturnsFailure
  - Wrong password → Generic failure message (no email enumeration)
- [ ] LoginAsync_WithUnverifiedEmail_ReturnsFailure
  - IsEmailVerified = false → Failure
- [ ] LoginAsync_CreatesRefreshToken
  - RefreshToken saved to repository

#### RefreshTokenAsyncTests

- [ ] RefreshTokenAsync_WithValidToken_ReturnsNewAccessToken
  - Valid token → New access token generated
- [ ] RefreshTokenAsync_WithRevokedToken_ReturnsFails
  - Token marked revoked → Failure
- [ ] RefreshTokenAsync_WithExpiredToken_ReturnsFails
  - Token expired → Failure
- [ ] RefreshTokenAsync_WithInvalidSignature_ReturnsFails
  - Token tampered → Failure

#### VerifyEmailAsyncTests

- [ ] VerifyEmailAsync_WithValidToken_MarksEmailVerified
  - Valid token → IsEmailVerified = true
- [ ] VerifyEmailAsync_WithInvalidToken_ReturnsFails
  - Token doesn't exist → Failure
- [ ] VerifyEmailAsync_ClearsTokenAfterUse
  - After verification, token cleared (single-use)

#### ForgotPasswordAsyncTests

- [ ] ForgotPasswordAsync_WithValidEmail_SendsResetEmail
  - Email exists → Reset email sent
- [ ] ForgotPasswordAsync_WithUnknownEmail_ReturnsSuccess
  - Email doesn't exist → Generic success (security)
- [ ] ForgotPasswordAsync_SetResetTokenExpiry
  - Reset token expiry set to 1 hour

#### ResetPasswordAsyncTests

- [ ] ResetPasswordAsync_WithValidToken_ResetsPassword
  - Valid token → Password updated
- [ ] ResetPasswordAsync_WithExpiredToken_ReturnsFails
  - Token expired → Failure
- [ ] ResetPasswordAsync_ClearsTokenAfterUse
  - Token cleared (single-use)
- [ ] ResetPasswordAsync_HashesNewPassword
  - New password BCrypt hashed

**Mocking Strategy:**

```csharp
var mockUserRepository = new Mock<IUserRepository>();
var mockTokenService = new Mock<ITokenService>();
var mockEmailSender = new Mock<IEmailSender>();

mockUserRepository
    .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
    .ReturnsAsync((User)null);

var authService = new AuthService(
    mockUserRepository.Object,
    mockTokenService.Object,
    mockEmailSender.Object);
```

---

### Task 5.2: Unit Tests for Validators

**File to Create:**

- `tests/AuthService.UnitTests/Validators/ValidatorTests.cs` (400 lines)

**Test Classes:**

#### RegisterRequestValidatorTests

- [ ] Validate_WithValidRequest_Succeeds
  - All valid fields → No errors
- [ ] Validate_WithoutEmail_Fails
  - Email missing → Validation error
- [ ] Validate_WithInvalidEmail_Fails
  - Bad email format → Validation error
- [ ] Validate_WithWeakPassword_Fails
  - Password < 8 chars → Multiple validation errors
- [ ] Validate_WithoutUppercase_Fails
  - No uppercase in password → Error
- [ ] Validate_WithoutLowercase_Fails
  - No lowercase in password → Error
- [ ] Validate_WithoutDigit_Fails
  - No digit in password → Error
- [ ] Validate_WithoutSpecialChar_Fails
  - No special char in password → Error
- [ ] Validate_WithMismatchedPassword_Fails
  - Password != ConfirmPassword → Error

#### LoginRequestValidatorTests

- [ ] Validate_WithValidRequest_Succeeds
- [ ] Validate_WithoutEmail_Fails
- [ ] Validate_WithoutPassword_Fails
- [ ] Validate_WithInvalidEmail_Fails

#### Other Validators

- [ ] RefreshTokenRequestValidator tests
- [ ] VerifyEmailRequestValidator tests
- [ ] ForgotPasswordRequestValidator tests
- [ ] ResetPasswordRequestValidator tests

---

### Task 5.3: Unit Tests for TokenService

**File to Create:**

- `tests/AuthService.UnitTests/Services/TokenServiceTests.cs` (300 lines)

**Test Methods:**

#### GenerateAccessTokenTests

- [ ] GenerateAccessToken_WithValidUser_ReturnsValidJwt
  - Valid user → JWT returned
- [ ] GenerateAccessToken_WithValidExpiry_TokenExpires
  - Expiry minutes respected → Token expires at right time
- [ ] GenerateAccessToken_IncludesUserIdClaim
  - JWT contains UserId claim
- [ ] GenerateAccessToken_IncludesEmailClaim
  - JWT contains Email claim

#### GenerateRefreshTokenTests

- [ ] GenerateRefreshToken_ReturnsCryptographicallyRandomToken
  - Random tokens on each call (not sequential)
- [ ] GenerateRefreshToken_WithValidExpiry_TokenExpires
  - Expiry respected

#### ValidateTokenTests

- [ ] ValidateToken_WithValidToken_ReturnsClaimsPrincipal
  - Valid JWT → Claims extracted
- [ ] ValidateToken_WithExpiredToken_ThrowsException
  - Expired token → Exception
- [ ] ValidateToken_WithInvalidSignature_ThrowsException
  - Tampered token → Exception
- [ ] ValidateToken_ExtractsUserIdAndEmail
  - Claims extracted correctly

---

### Task 5.4: Integration Tests for API Endpoints

**File to Create:**

- `tests/AuthService.IntegrationTests/Controllers/AuthControllerIntegrationTests.cs` (800 lines)

**Test Infrastructure:**

- WebApplicationFactory fixture
- In-memory database (or LocalDB)
- Test data seeding
- HttpClient for requests

**Test Classes:**

#### RegisterEndpointTests

- [ ] Post_Register_WithValidRequest_Returns201Created
  - Valid request → 201 status
- [ ] Post_Register_WithDuplicateEmail_Returns400
  - Email exists → 400 status
- [ ] Post_Register_WithInvalidPassword_Returns400
  - Weak password → 400 status
- [ ] Post_Register_CreatesUserInDatabase
  - User persisted to DB

#### LoginEndpointTests

- [ ] Post_Login_WithValidCredentials_Returns200WithTokens
  - Valid creds → 200 with AccessToken and RefreshToken
- [ ] Post_Login_WithUnverifiedEmail_Returns401
  - Email not verified → 401
- [ ] Post_Login_WithInvalidPassword_Returns401
  - Wrong password → 401
- [ ] Post_Login_ReturnsValidJwt
  - Can decode returned JWT

#### RefreshTokenEndpointTests

- [ ] Post_RefreshToken_WithValidToken_Returns200WithNewToken
  - Valid token → 200 with new token
- [ ] Post_RefreshToken_WithInvalidToken_Returns401
  - Invalid token → 401

#### VerifyEmailEndpointTests

- [ ] Post_VerifyEmail_WithValidToken_Returns200
  - Valid token → 200
- [ ] Post_VerifyEmail_WithInvalidToken_Returns400
  - Invalid token → 400

#### ForgotPasswordEndpointTests

- [ ] Post_ForgotPassword_WithValidEmail_Returns200
  - Valid email → 200 (generic)
- [ ] Post_ForgotPassword_WithUnknownEmail_Returns200
  - Unknown email → 200 (no enumeration)

#### ResetPasswordEndpointTests

- [ ] Post_ResetPassword_WithValidToken_Returns200
  - Valid token → 200
- [ ] Post_ResetPassword_WithInvalidPassword_Returns400
  - Weak password → 400

---

### Task 5.5: Test Database Setup

**Files to Create:**

- `tests/AuthService.IntegrationTests/Infrastructure/TestDatabaseFixture.cs` (200 lines)
- `tests/AuthService.IntegrationTests/Infrastructure/WebApplicationFactorySetup.cs` (150 lines)

**Features:**

- In-memory database or LocalDB
- Automatic migrations on startup
- Clean database per test
- Seed test data
- Async operation support

```csharp
public class TestDatabaseFixture : IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public async Task InitializeAsync()
    {
        // Create database
        var context = new AppDbContext(_dbOptions);
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up
        var context = new AppDbContext(_dbOptions);
        await context.Database.EnsureDeletedAsync();
    }
}
```

---

### Task 5.6: Code Coverage

**Target:** >80% coverage for critical paths

**Coverage Areas:**

- AuthService methods: 100% (all business logic)
- Validators: 100% (all rules)
- TokenService: 95% (complex JWT logic)
- Controllers: 90% (happy path + errors)
- Repository: 80% (CRUD + queries)

**Tools:**

- OpenCover or Coverlet for coverage analysis
- Coverage reports in CI/CD pipeline

---

## Test Structure

```
tests/
├── AuthService.UnitTests/
│   ├── Services/
│   │   ├── AuthServiceTests.cs (600 lines)
│   │   └── TokenServiceTests.cs (300 lines)
│   └── Validators/
│       └── ValidatorTests.cs (400 lines)
│
└── AuthService.IntegrationTests/
    ├── Controllers/
    │   └── AuthControllerIntegrationTests.cs (800 lines)
    ├── Infrastructure/
    │   ├── TestDatabaseFixture.cs (200 lines)
    │   └── WebApplicationFactorySetup.cs (150 lines)
    └── Data/
        └── TestDataSeeding.cs (100 lines)
```

---

## NuGet Dependencies

- xUnit
- xUnit.Runner.VisualStudio
- Moq
- FluentAssertions
- Microsoft.AspNetCore.Mvc.Testing
- Microsoft.EntityFrameworkCore.InMemory (or LocalDB)

---

## Files Summary

| Category       | File                   | Lines | Tests |
| -------------- | ---------------------- | ----- | ----- |
| Unit Tests     | AuthServiceTests.cs    | 600   | 18    |
| Unit Tests     | TokenServiceTests.cs   | 300   | 8     |
| Unit Tests     | ValidatorTests.cs      | 400   | 25    |
| Integration    | AuthControllerTests.cs | 800   | 20    |
| Infrastructure | Fixtures & Setup       | 350   | -     |
| Data           | Seeding                | 100   | -     |

**Total:** ~2,550 lines, 71+ test cases, >80% coverage

---

## Success Criteria

- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Code coverage >80%
- [ ] CI/CD pipeline runs tests
- [ ] No flaky tests
- [ ] Test execution <30 seconds
- [ ] All critical paths tested

---

## Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover

# Run specific test class
dotnet test --filter "ClassName=AuthServiceTests"

# Watch mode
dotnet watch test
```
