using FluentValidation;

namespace KitobdaGimen.Application.Features.Quotes.Commands.CreateQuote;

public class CreateQuoteCommandValidator : AbstractValidator<CreateQuoteCommand>
{
    public CreateQuoteCommandValidator()
    {
        RuleFor(x => x.BookId)
            .GreaterThan(0).WithMessage("Kitobni tanlang.");

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Iqtibos matnini kiriting.")
            .MinimumLength(3).WithMessage("Iqtibos kamida 3 belgidan iborat bo'lishi kerak.")
            .MaximumLength(400).WithMessage("Iqtibos 400 belgidan oshmasligi kerak.");
    }
}
