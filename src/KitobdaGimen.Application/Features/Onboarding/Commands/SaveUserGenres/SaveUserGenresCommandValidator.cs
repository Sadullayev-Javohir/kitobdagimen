using FluentValidation;

namespace KitobdaGimen.Application.Features.Onboarding.Commands.SaveUserGenres;

public class SaveUserGenresCommandValidator : AbstractValidator<SaveUserGenresCommand>
{
    public SaveUserGenresCommandValidator()
    {
        RuleFor(x => x.GenreIds)
            .NotNull().WithMessage("Janrlarni tanlang.")
            .Must(ids => ids.Count >= 1).WithMessage("Kamida bitta janr tanlang.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("Janrlar takrorlanmasligi kerak.");
    }
}
