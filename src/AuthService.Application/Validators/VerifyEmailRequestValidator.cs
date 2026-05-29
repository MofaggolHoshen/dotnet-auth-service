using FluentValidation;
using AuthService.Application.DTOs.Requests;

namespace AuthService.Application.Validators
{
    public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
    {
        public VerifyEmailRequestValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Verification token is required");
        }
    }
}
