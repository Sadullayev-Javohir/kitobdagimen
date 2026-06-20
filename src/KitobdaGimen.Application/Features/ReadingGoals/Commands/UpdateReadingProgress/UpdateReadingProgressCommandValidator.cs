using FluentValidation;

namespace KitobdaGimen.Application.Features.ReadingGoals.Commands.UpdateReadingProgress;

public class UpdateReadingProgressCommandValidator : AbstractValidator<UpdateReadingProgressCommand>
{
    public UpdateReadingProgressCommandValidator()
    {
        RuleFor(x => x.ReadingGoalId)
            .GreaterThan(0).WithMessage("Maqsad tanlanmadi.");

        RuleFor(x => x.PagesRead)
            .GreaterThan(0).WithMessage("O'qilgan sahifa soni 0 dan katta bo'lishi kerak.")
            .LessThanOrEqualTo(5000).WithMessage("O'qilgan sahifa soni juda katta.");
    }
}
