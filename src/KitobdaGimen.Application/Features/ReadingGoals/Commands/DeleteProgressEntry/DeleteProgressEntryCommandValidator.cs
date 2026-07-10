using FluentValidation;

namespace KitobdaGimen.Application.Features.ReadingGoals.Commands.DeleteProgressEntry;

public class DeleteProgressEntryCommandValidator : AbstractValidator<DeleteProgressEntryCommand>
{
    public DeleteProgressEntryCommandValidator()
    {
        RuleFor(x => x.ReadingGoalId)
            .GreaterThan(0).WithMessage("Maqsad tanlanmadi.");

        RuleFor(x => x.Date)
            .NotEqual(default(DateOnly)).WithMessage("Sana ko'rsatilmadi.");
    }
}
