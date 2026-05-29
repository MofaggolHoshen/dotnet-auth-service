using AuthService.Application.DTOs.Requests;
using AuthService.Application.Validators;
using FluentAssertions;
using FluentValidation;

namespace AuthService.UnitTests.Validators;

public class LoginRequestValidatorTests
{
    private readonly IValidator<LoginRequest> _validator = new LoginRequestValidator();

    [Fact]
    public async Task Validate_ValidRequest_PassesValidation()
    {
        var result = await _validator.ValidateAsync(new LoginRequest
        {
            Email = "user@example.com",
            Password = "anypassword"
        });

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("notanemail", "password")]
    [InlineData("@nodomain", "password")]
    public async Task Validate_InvalidEmail_FailsValidation(string email, string password)
    {
        var result = await _validator.ValidateAsync(new LoginRequest { Email = email, Password = password });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_EmptyPassword_FailsValidation()
    {
        var result = await _validator.ValidateAsync(new LoginRequest
        {
            Email = "user@example.com",
            Password = ""
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}

public class ResetPasswordRequestValidatorTests
{
    private readonly IValidator<ResetPasswordRequest> _validator = new ResetPasswordRequestValidator();

    [Fact]
    public async Task Validate_ValidRequest_PassesValidation()
    {
        var result = await _validator.ValidateAsync(new ResetPasswordRequest
        {
            Token = "sometoken",
            NewPassword = "Password1!",
            ConfirmPassword = "Password1!"
        });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_MissingToken_FailsValidation()
    {
        var result = await _validator.ValidateAsync(new ResetPasswordRequest
        {
            Token = "",
            NewPassword = "Password1!",
            ConfirmPassword = "Password1!"
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Token");
    }

    [Fact]
    public async Task Validate_PasswordMismatch_FailsValidation()
    {
        var result = await _validator.ValidateAsync(new ResetPasswordRequest
        {
            Token = "token",
            NewPassword = "Password1!",
            ConfirmPassword = "Different1!"
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfirmPassword");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("alllowercase1!")]
    [InlineData("ALLUPPERCASE1!")]
    [InlineData("NoDigits!")]
    [InlineData("NoSpecial1")]
    public async Task Validate_WeakNewPassword_FailsValidation(string password)
    {
        var result = await _validator.ValidateAsync(new ResetPasswordRequest
        {
            Token = "token",
            NewPassword = password,
            ConfirmPassword = password
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }
}

public class ForgotPasswordRequestValidatorTests
{
    private readonly IValidator<ForgotPasswordRequest> _validator = new ForgotPasswordRequestValidator();

    [Fact]
    public async Task Validate_ValidEmail_PassesValidation()
    {
        var result = await _validator.ValidateAsync(new ForgotPasswordRequest { Email = "user@example.com" });
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("notanemail")]
    [InlineData("@nodomain.com")]
    public async Task Validate_InvalidEmail_FailsValidation(string email)
    {
        var result = await _validator.ValidateAsync(new ForgotPasswordRequest { Email = email });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}

public class RefreshTokenRequestValidatorTests
{
    private readonly IValidator<RefreshTokenRequest> _validator = new RefreshTokenRequestValidator();

    [Fact]
    public async Task Validate_WithToken_PassesValidation()
    {
        var result = await _validator.ValidateAsync(new RefreshTokenRequest { RefreshToken = "sometoken" });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyToken_FailsValidation()
    {
        var result = await _validator.ValidateAsync(new RefreshTokenRequest { RefreshToken = "" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RefreshToken");
    }
}

public class VerifyEmailRequestValidatorTests
{
    private readonly IValidator<VerifyEmailRequest> _validator = new VerifyEmailRequestValidator();

    [Fact]
    public async Task Validate_WithToken_PassesValidation()
    {
        var result = await _validator.ValidateAsync(new VerifyEmailRequest { Token = "sometoken" });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyToken_FailsValidation()
    {
        var result = await _validator.ValidateAsync(new VerifyEmailRequest { Token = "" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Token");
    }
}
