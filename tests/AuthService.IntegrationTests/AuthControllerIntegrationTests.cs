using System.Net;
using System.Net.Http.Json;
using AuthService.Application.DTOs.Responses;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.IntegrationTests;

public class AuthControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public AuthControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── POST /api/auth/register ────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidData_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"new_{Guid.NewGuid():N}@example.com",
            password = "Password1!",
            confirmPassword = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_WithExistingEmail_Returns400()
    {
        var email = $"dup_{Guid.NewGuid():N}@example.com";

        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email, password = "Password1!", confirmPassword = "Password1!"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email, password = "Password1!", confirmPassword = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "notanemail",
            password = "Password1!",
            confirmPassword = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "user@example.com",
            password = "weak",
            confirmPassword = "weak"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithMismatchedPasswords_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "user@example.com",
            password = "Password1!",
            confirmPassword = "Different1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /api/auth/login ───────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithUnregisteredEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "nobody@example.com",
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns401()
    {
        var email = await RegisterAndGetEmail();

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "WrongPassword1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnverifiedEmail_Returns401()
    {
        var email = await RegisterAndGetEmail();

        // Email not verified yet
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithVerifiedAccount_Returns200WithTokens()
    {
        var email = await RegisterAndVerifyEmail();

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = await response.Content.ReadFromJsonAsync<AuthToken>();
        token.Should().NotBeNull();
        token!.AccessToken.Should().NotBeNullOrEmpty();
        token.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidEmailFormat_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "notanemail",
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /api/auth/refresh-token ───────────────────────────────────────────

    [Fact]
    public async Task RefreshToken_WithEmptyToken_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh-token", new
        {
            refreshToken = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh-token", new
        {
            refreshToken = "this-token-does-not-exist"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/auth/verify-email ────────────────────────────────────────────

    [Fact]
    public async Task VerifyEmail_WithValidToken_Returns200()
    {
        var (_, verificationToken) = await RegisterAndGetVerificationToken();

        var response = await _client.PostAsJsonAsync("/api/auth/verify-email", new
        {
            token = verificationToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidToken_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email", new
        {
            token = "invalid-verification-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VerifyEmail_WithEmptyToken_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email", new
        {
            token = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /api/auth/forgot-password ────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_WithRegisteredEmail_Returns200()
    {
        var email = await RegisterAndGetEmail();

        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_WithUnregisteredEmail_StillReturns200()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = "nobody@example.com"
        });

        // Always 200 to prevent email enumeration
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_WithInvalidEmailFormat_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = "notanemail"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /api/auth/reset-password ─────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_WithValidToken_Returns200()
    {
        var (email, resetToken) = await RegisterAndGetPasswordResetToken();

        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = resetToken,
            newPassword = "NewPassword1!",
            confirmPassword = "NewPassword1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = "invalid-token",
            newPassword = "NewPassword1!",
            confirmPassword = "NewPassword1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_WithMismatchedPasswords_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = "sometoken",
            newPassword = "Password1!",
            confirmPassword = "Different1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_WithWeakPassword_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = "sometoken",
            newPassword = "weak",
            confirmPassword = "weak"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<string> RegisterAndGetEmail()
    {
        var email = $"user_{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password1!",
            confirmPassword = "Password1!"
        });
        return email;
    }

    private async Task<(string email, string token)> RegisterAndGetVerificationToken()
    {
        var email = $"user_{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password1!",
            confirmPassword = "Password1!"
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.First(u => u.Email == email);
        return (email, user.EmailVerificationToken!);
    }

    private async Task<string> RegisterAndVerifyEmail()
    {
        var (email, token) = await RegisterAndGetVerificationToken();
        await _client.PostAsJsonAsync("/api/auth/verify-email", new { token });
        return email;
    }

    private async Task<(string email, string resetToken)> RegisterAndGetPasswordResetToken()
    {
        var email = await RegisterAndGetEmail();
        await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.First(u => u.Email == email);
        return (email, user.PasswordResetToken!);
    }
}
