using AuthService.Application.DTOs.Requests;
using AuthService.Application.DTOs.Responses;
using AuthService.Application.Interfaces;
using AuthService.Application.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using FluentAssertions;
using Moq;

namespace AuthService.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IEmailSender> _emailSenderMock = new();
    private readonly global::AuthService.Application.Services.AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new global::AuthService.Application.Services.AuthService(
            _userRepoMock.Object,
            _tokenServiceMock.Object,
            _emailSenderMock.Object
        );
    }

    // ── RegisterAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ReturnsFalse()
    {
        _userRepoMock.Setup(r => r.ExistsByEmailAsync("test@example.com")).ReturnsAsync(true);

        var result = await _sut.RegisterAsync(new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password1!"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already registered");
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailIsNew_ReturnsSuccessAndSendsEmail()
    {
        _userRepoMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>())).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _emailSenderMock.Setup(e => e.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.RegisterAsync(new RegisterRequest
        {
            Email = "new@example.com",
            Password = "Password1!"
        });

        result.Success.Should().BeTrue();
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _emailSenderMock.Verify(e => e.SendEmailVerificationAsync("new@example.com", It.IsAny<string>()), Times.Once);
    }

    // ── LoginAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ReturnsUnauthorized()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var result = await _sut.LoginAsync(new LoginRequest
        {
            Email = "nobody@example.com",
            Password = "Password1!"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_WhenEmailNotVerified_ReturnsFalse()
    {
        var user = MakeUser(isEmailVerified: false);
        _userRepoMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "Password1!"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not verified");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokens()
    {
        var user = MakeVerifiedUser();
        _userRepoMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _tokenServiceMock.Setup(t => t.GenerateAccessToken(user.Id, user.Email)).Returns("access_token");
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh_token");

        var result = await _sut.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "Password1!"
        });

        result.Success.Should().BeTrue();
        result.Token.Should().NotBeNull();
        result.Token!.AccessToken.Should().Be("access_token");
        result.Token.RefreshToken.Should().Be("refresh_token");
    }

    // ── VerifyEmailAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyEmailAsync_WhenTokenInvalid_ReturnsFalse()
    {
        _userRepoMock.Setup(r => r.GetByEmailVerificationTokenAsync("badtoken")).ReturnsAsync((User?)null);

        var result = await _sut.VerifyEmailAsync(new VerifyEmailRequest { Token = "badtoken" });

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyEmailAsync_WhenTokenValid_VerifiesEmail()
    {
        var user = MakeUser(isEmailVerified: false, emailVerificationToken: "validtoken");
        _userRepoMock.Setup(r => r.GetByEmailVerificationTokenAsync("validtoken")).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var result = await _sut.VerifyEmailAsync(new VerifyEmailRequest { Token = "validtoken" });

        result.Success.Should().BeTrue();
        _userRepoMock.Verify(r => r.UpdateAsync(It.Is<User>(u => u.IsEmailVerified)), Times.Once);
    }

    // ── ForgotPasswordAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPasswordAsync_WhenEmailNotFound_StillReturnsSuccess()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var result = await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "ghost@example.com" });

        result.Success.Should().BeTrue();
        _emailSenderMock.Verify(e => e.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenEmailExists_SendsResetEmail()
    {
        var user = MakeVerifiedUser();
        _userRepoMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _emailSenderMock.Setup(e => e.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = user.Email });

        result.Success.Should().BeTrue();
        _emailSenderMock.Verify(e => e.SendPasswordResetAsync(user.Email, It.IsAny<string>()), Times.Once);
    }

    // ── ResetPasswordAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPasswordAsync_WhenTokenInvalid_ReturnsFalse()
    {
        _userRepoMock.Setup(r => r.GetByPasswordResetTokenAsync("badtoken")).ReturnsAsync((User?)null);

        var result = await _sut.ResetPasswordAsync(new ResetPasswordRequest
        {
            Token = "badtoken",
            NewPassword = "NewPassword1!"
        });

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenTokenExpired_ReturnsFalse()
    {
        var user = MakeUser();
        user.PasswordResetToken = "expiredtoken";
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(-2);
        _userRepoMock.Setup(r => r.GetByPasswordResetTokenAsync("expiredtoken")).ReturnsAsync(user);

        var result = await _sut.ResetPasswordAsync(new ResetPasswordRequest
        {
            Token = "expiredtoken",
            NewPassword = "NewPassword1!"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("expired");
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenTokenValid_UpdatesPassword()
    {
        var user = MakeUser();
        user.PasswordResetToken = "validtoken";
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        _userRepoMock.Setup(r => r.GetByPasswordResetTokenAsync("validtoken")).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var result = await _sut.ResetPasswordAsync(new ResetPasswordRequest
        {
            Token = "validtoken",
            NewPassword = "NewPassword1!"
        });

        result.Success.Should().BeTrue();
        _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static User MakeUser(bool isEmailVerified = true, string? emailVerificationToken = null) => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@example.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"),
        IsEmailVerified = isEmailVerified,
        EmailVerificationToken = emailVerificationToken,
        Status = UserStatus.Active,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static User MakeVerifiedUser() => MakeUser(isEmailVerified: true);
}
