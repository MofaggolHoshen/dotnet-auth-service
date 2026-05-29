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
    [InlineData("user@example.com", "")]
    public async Task Validate_InvalidInput_FailsValidation(string email, string password)
    {
        var result = await _validator.ValidateAsync(new LoginRequest
        {
            Email = email,
            Password = password
        });

        result.IsValid.Should().BeFalse();
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
}
