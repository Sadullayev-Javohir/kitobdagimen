using FluentValidation;

namespace KitobdaGimen.Application.Features.Stories.Commands.CreateStory;

public class CreateStoryCommandValidator : AbstractValidator<CreateStoryCommand>
{
    public CreateStoryCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Sarlavhani kiriting.")
            .MinimumLength(3).WithMessage("Sarlavha kamida 3 ta belgidan iborat bo'lsin.")
            .MaximumLength(50).WithMessage("Sarlavha 50 ta belgidan oshmasin.");

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Matnni kiriting.")
            .MinimumLength(3).WithMessage("Matn kamida 3 ta belgidan iborat bo'lsin.")
            .MaximumLength(140).WithMessage("Matn 140 ta belgidan oshmasin.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(2048).WithMessage("Rasm manzili juda uzun.");

        RuleFor(x => x.DurationHours)
            .Must(h => h == 12 || h == 24 || h == 48)
            .WithMessage("Muddatni tanlang: 12, 24 yoki 48 soat.");
    }
}
