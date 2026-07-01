using FluentValidation;

namespace KitobdaGimen.Application.Features.Quotes.Commands.AddQuoteComment;

public class AddQuoteCommentCommandValidator : AbstractValidator<AddQuoteCommentCommand>
{
    public AddQuoteCommentCommandValidator()
    {
        RuleFor(x => x.QuoteId)
            .GreaterThan(0).WithMessage("Iqtibos tanlanmadi.");

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Izoh matnini kiriting.")
            .MinimumLength(3).WithMessage("Izoh kamida 3 belgidan iborat bo'lishi kerak.")
            .MaximumLength(500).WithMessage("Izoh 500 belgidan oshmasligi kerak.");
    }
}
