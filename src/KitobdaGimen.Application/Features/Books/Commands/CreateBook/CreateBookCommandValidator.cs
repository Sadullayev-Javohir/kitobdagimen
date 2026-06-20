using FluentValidation;

namespace KitobdaGimen.Application.Features.Books.Commands.CreateBook;

public class CreateBookCommandValidator : AbstractValidator<CreateBookCommand>
{
    public CreateBookCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Kitob nomini kiriting.")
            .MinimumLength(3).WithMessage("Kitob nomi kamida 3 ta belgidan iborat bo'lishi kerak.")
            .MaximumLength(100).WithMessage("Kitob nomi 100 ta belgidan oshmasligi kerak.");

        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("Muallifni kiriting.")
            .MinimumLength(3).WithMessage("Muallif nomi kamida 3 ta belgidan iborat bo'lishi kerak.")
            .MaximumLength(100).WithMessage("Muallif nomi 100 ta belgidan oshmasligi kerak.");

        RuleFor(x => x.TotalPages)
            .GreaterThan(0).WithMessage("Sahifalar soni 0 dan katta bo'lishi kerak.")
            .LessThanOrEqualTo(100000).WithMessage("Sahifalar soni juda katta.");
    }
}
