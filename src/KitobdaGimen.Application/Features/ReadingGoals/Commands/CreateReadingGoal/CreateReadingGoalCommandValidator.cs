using FluentValidation;

namespace KitobdaGimen.Application.Features.ReadingGoals.Commands.CreateReadingGoal;

public class CreateReadingGoalCommandValidator : AbstractValidator<CreateReadingGoalCommand>
{
    public CreateReadingGoalCommandValidator()
    {
        RuleFor(x => x.BookId)
            .GreaterThan(0).WithMessage("Kitobni tanlang.");

        RuleFor(x => x.DailyPageGoal)
            .GreaterThan(0).WithMessage("Kunlik sahifa maqsadi 0 dan katta bo'lishi kerak.")
            .LessThanOrEqualTo(1000).WithMessage("Kunlik sahifa maqsadi juda katta.");

        // The reading start date may be today or in the past, never in the future.
        When(x => x.StartDate.HasValue, () =>
        {
            RuleFor(x => x.StartDate!.Value)
                .Must(d => d.Date <= DateTime.UtcNow.Date)
                .WithMessage("O'qish boshlangan sana kelajakda bo'lishi mumkin emas.");
        });
    }
}
