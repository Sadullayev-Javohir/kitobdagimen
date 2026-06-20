using FluentValidation;

namespace KitobdaGimen.Application.Features.Posts.Commands.AddComment;

public class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.PostId)
            .GreaterThan(0).WithMessage("Post tanlanmadi.");

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Izoh matnini kiriting.")
            .MinimumLength(3).WithMessage("Izoh kamida 3 belgidan iborat bo'lishi kerak.")
            .MaximumLength(500).WithMessage("Izoh 500 belgidan oshmasligi kerak.");
    }
}
