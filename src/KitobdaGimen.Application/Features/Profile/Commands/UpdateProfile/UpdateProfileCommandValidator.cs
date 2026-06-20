using FluentValidation;

namespace KitobdaGimen.Application.Features.Profile.Commands.UpdateProfile;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username kiriting.")
            .Matches("^[a-zA-Z0-9_]{3,32}$")
                .WithMessage("Username 3-32 belgidan iborat bo'lishi va faqat harf, raqam va pastki chiziqdan (_) tashkil topishi kerak.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("To'liq ismni kiriting.")
            .Length(3, 32).WithMessage("To'liq ism 3-32 belgidan iborat bo'lishi kerak.");

        RuleFor(x => x.Bio)
            .MaximumLength(100).WithMessage("Bio 100 belgidan oshmasligi kerak.");

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(2048).WithMessage("Avatar manzili juda uzun.");
    }
}
