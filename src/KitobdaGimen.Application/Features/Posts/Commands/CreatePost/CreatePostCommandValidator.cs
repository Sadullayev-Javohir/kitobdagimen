using FluentValidation;

namespace KitobdaGimen.Application.Features.Posts.Commands.CreatePost;

public class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(x => x.BookId)
            .GreaterThan(0).WithMessage("Kitobni tanlang.");

        RuleFor(x => x.ReviewText)
            .NotEmpty().WithMessage("Fikr matnini kiriting.")
            .MinimumLength(3).WithMessage("Fikr kamida 3 belgidan iborat bo'lishi kerak.")
            .MaximumLength(5000).WithMessage("Fikr 5000 belgidan oshmasligi kerak.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(2048).WithMessage("Rasm manzili juda uzun.");
    }
}
