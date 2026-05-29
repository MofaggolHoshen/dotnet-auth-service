using AuthService.Application.DTOs.Requests;
using AuthService.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshTokenValidator;
    private readonly IValidator<VerifyEmailRequest> _verifyEmailValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshTokenValidator,
        IValidator<VerifyEmailRequest> verifyEmailValidator,
        IValidator<ForgotPasswordRequest> forgotPasswordValidator,
        IValidator<ResetPasswordRequest> resetPasswordValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshTokenValidator = refreshTokenValidator;
        _verifyEmailValidator = verifyEmailValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
    }

    /// <summary>Register a new user account. Sends a verification email.</summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var validation = await _registerValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var result = await _authService.RegisterAsync(request);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return StatusCode(StatusCodes.Status201Created, new { message = result.Message });
    }

    /// <summary>Authenticate with email and password. Returns JWT access + refresh tokens.</summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var result = await _authService.LoginAsync(request);

        if (!result.Success)
            return Unauthorized(new { message = result.Message });

        return Ok(result.Token);
    }

    /// <summary>Exchange a valid refresh token for a new access token.</summary>
    [HttpPost("refresh-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var validation = await _refreshTokenValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var result = await _authService.RefreshTokenAsync(request);

        if (!result.Success)
            return Unauthorized(new { message = result.Message });

        return Ok(result.Token);
    }

    /// <summary>Verify a user's email address using the token sent by email.</summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var validation = await _verifyEmailValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var result = await _authService.VerifyEmailAsync(request);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    /// <summary>Initiate a password reset. Sends a reset link to the email address.</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var validation = await _forgotPasswordValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        // Always return 200 to prevent email enumeration
        await _authService.ForgotPasswordAsync(request);

        return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
    }

    /// <summary>Complete a password reset using the token received by email.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var validation = await _resetPasswordValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var result = await _authService.ResetPasswordAsync(request);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }
}
