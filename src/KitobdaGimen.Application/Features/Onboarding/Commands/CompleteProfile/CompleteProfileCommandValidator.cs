using FluentValidation;

namespace KitobdaGimen.Application.Features.Onboarding.Commands.CompleteProfile;

public class CompleteProfileCommandValidator : AbstractValidator<CompleteProfileCommand>
{
    public CompleteProfileCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username kiriting.")
            .Matches("^[a-zA-Z0-9_]{3,32}$")
                .WithMessage("Username 3-32 belgidan iborat bo'lishi va faqat harf, raqam va pastki chiziqdan (_) tashkil topishi kerak.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("To'liq ismni kiriting.")
            .Length(3, 32).WithMessage("To'liq ism 3-32 belgidan iborat bo'lishi kerak.");

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(2048).WithMessage("Avatar manzili juda uzun.");
    }
}
