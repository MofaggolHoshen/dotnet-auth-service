using BCrypt.Net;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Application.Interfaces;
using AuthService.Application.DTOs.Requests;
using AuthService.Application.DTOs.Responses;

namespace AuthService.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IEmailSender _emailSender;

        public AuthService(
            IUserRepository userRepository,
            ITokenService tokenService,
            IEmailSender emailSender)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _emailSender = emailSender;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Check if email already exists
            if (await _userRepository.ExistsByEmailAsync(request.Email))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Email is already registered"
                };
            }

            // Hash password and create user
            var passwordHash = BCrypt.BCrypt.HashPassword(request.Password);
            var emailVerificationToken = GenerateToken();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = passwordHash,
                IsEmailVerified = false,
                EmailVerificationToken = emailVerificationToken,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Save to database
            await _userRepository.AddAsync(user);

            // Send verification email
            await _emailSender.SendEmailVerificationAsync(user.Email, emailVerificationToken);

            return new AuthResponse
            {
                Success = true,
                Message = "Registration successful. Please verify your email."
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            // Find user by email
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }

            // Verify password
            if (!BCrypt.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }

            // Check email verification
            if (!user.IsEmailVerified)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Email not verified. Please verify your email first."
                };
            }

            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email);
            var refreshToken = _tokenService.GenerateRefreshToken();

            return new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                Token = new AuthToken
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresInMinutes = 15
                }
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            // Validate token isn't revoked
            var isRevoked = await _tokenService.IsTokenRevokedAsync(Guid.Empty, request.RefreshToken);
            if (isRevoked)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid refresh token"
                };
            }

            // Validate token and extract claims
            var principal = _tokenService.ValidateToken(request.RefreshToken);
            if (principal == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid or expired refresh token"
                };
            }

            var userIdClaim = principal.FindFirst("sub")?.Value;
            var emailClaim = principal.FindFirst("email")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(emailClaim))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid token claims"
                };
            }

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid user ID in token"
                };
            }

            // Generate new access token
            var newAccessToken = _tokenService.GenerateAccessToken(userId, emailClaim);

            return new AuthResponse
            {
                Success = true,
                Message = "Token refreshed successfully",
                Token = new AuthToken
                {
                    AccessToken = newAccessToken,
                    RefreshToken = request.RefreshToken,
                    ExpiresInMinutes = 15
                }
            };
        }

        public async Task<MessageResponse> VerifyEmailAsync(VerifyEmailRequest request)
        {
            // Find user with this verification token
            var user = await _userRepository.GetByEmailVerificationTokenAsync(request.Token);
            if (user == null || user.EmailVerificationToken != request.Token)
            {
                return new MessageResponse 
                { 
                    Success = false, 
                    Message = "Invalid verification token"
                };
            }

            // Mark email as verified
            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return new MessageResponse 
            { 
                Success = true, 
                Message = "Email verified successfully"
            };
        }

        public async Task<MessageResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                // Don't reveal if email exists (security best practice)
                return new MessageResponse 
                { 
                    Success = true, 
                    Message = "If email exists, password reset instructions have been sent."
                };
            }

            // Generate reset token
            var resetToken = GenerateToken();
            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            // Send password reset email
            await _emailSender.SendPasswordResetAsync(user.Email, resetToken);

            return new MessageResponse 
            { 
                Success = true, 
                Message = "Password reset instructions have been sent to your email."
            };
        }

        public async Task<MessageResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            // Find user with valid reset token
            var user = await _userRepository.GetByPasswordResetTokenAsync(request.Token);
            if (user == null ||
                user.PasswordResetToken != request.Token ||
                user.PasswordResetTokenExpiry < DateTime.UtcNow)
            {
                return new MessageResponse 
                { 
                    Success = false, 
                    Message = "Invalid or expired password reset token"
                };
            }

            // Update password
            user.PasswordHash = BCrypt.BCrypt.HashPassword(request.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            return new MessageResponse 
            { 
                Success = true, 
                Message = "Password reset successfully"
            };
        }

        private string GenerateToken()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
