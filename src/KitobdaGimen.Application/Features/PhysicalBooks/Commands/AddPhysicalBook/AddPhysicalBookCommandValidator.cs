using FluentValidation;

namespace KitobdaGimen.Application.Features.PhysicalBooks.Commands.AddPhysicalBook;

public class AddPhysicalBookCommandValidator : AbstractValidator<AddPhysicalBookCommand>
{
    public AddPhysicalBookCommandValidator()
    {
        // Kitob katalogdan tanlanishi (BookId) YOKI qo'lda nom kiritilishi shart.
        RuleFor(x => x)
            .Must(x => x.BookId is not null || !string.IsNullOrWhiteSpace(x.ManualTitle))
            .WithMessage("Kitobni qidiruvdan tanlang yoki nomini qo'lda kiriting.");

        When(x => x.BookId is null, () =>
        {
            RuleFor(x => x.ManualTitle)
                .NotEmpty().WithMessage("Kitob nomini kiriting.")
                .MinimumLength(2).WithMessage("Kitob nomi kamida 2 ta belgidan iborat bo'lishi kerak.")
                .MaximumLength(150).WithMessage("Kitob nomi 150 ta belgidan oshmasligi kerak.");

            RuleFor(x => x.ManualAuthor)
                .MaximumLength(150).WithMessage("Muallif nomi 150 ta belgidan oshmasligi kerak.");
        });
    }
}
