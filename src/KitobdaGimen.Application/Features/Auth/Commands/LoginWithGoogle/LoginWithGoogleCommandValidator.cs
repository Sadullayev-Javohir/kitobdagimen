using FluentValidation;

namespace KitobdaGimen.Application.Features.Auth.Commands.LoginWithGoogle;

public class LoginWithGoogleCommandValidator : AbstractValidator<LoginWithGoogleCommand>
{
    public LoginWithGoogleCommandValidator()
    {
        RuleFor(x => x.GoogleId)
            .NotEmpty().WithMessage("Google identifikatori topilmadi.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email manzili topilmadi.")
            .EmailAddress().WithMessage("Email manzili noto'g'ri.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ism topilmadi.");
    }
}
