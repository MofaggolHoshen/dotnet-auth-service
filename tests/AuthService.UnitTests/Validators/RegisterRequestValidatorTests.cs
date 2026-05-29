using AuthService.Application.DTOs.Requests;
using AuthService.Application.Validators;
using FluentAssertions;
using FluentValidation;

namespace AuthService.UnitTests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly IValidator<RegisterRequest> _validator = new RegisterRequestValidator();

    [Fact]
    public async Task Validate_ValidRequest_PassesValidation()
    {
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = "Password1!",
            ConfirmPassword = "Password1!"
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("notanemail")]
    [InlineData("@nodomain")]
    public async Task Validate_InvalidEmail_FailsValidation(string email)
    {
        var request = new RegisterRequest
        {
            Email = email,
            Password = "Password1!",
            ConfirmPassword = "Password1!"
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("short")]           // too short
    [InlineData("alllowercase1!")]  // no uppercase
    [InlineData("ALLUPPERCASE1!")]  // no lowercase
    [InlineData("NoDigits!")]       // no digit
    [InlineData("NoSpecial1")]      // no special char
    public async Task Validate_WeakPassword_FailsValidation(string password)
    {
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = password,
            ConfirmPassword = password
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Validate_PasswordMismatch_FailsValidation()
    {
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = "Password1!",
            ConfirmPassword = "Different1!"
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfirmPassword");
    }
}
