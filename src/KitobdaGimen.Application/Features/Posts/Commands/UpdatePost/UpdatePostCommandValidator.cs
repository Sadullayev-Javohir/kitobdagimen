using FluentValidation;

namespace KitobdaGimen.Application.Features.Posts.Commands.UpdatePost;

public class UpdatePostCommandValidator : AbstractValidator<UpdatePostCommand>
{
    public UpdatePostCommandValidator()
    {
        RuleFor(x => x.PostId)
            .GreaterThan(0).WithMessage("Post aniqlanmadi.");

        RuleFor(x => x.ReviewText)
            .NotEmpty().WithMessage("Fikr matnini kiriting.")
            .MinimumLength(3).WithMessage("Fikr kamida 3 belgidan iborat bo'lishi kerak.")
            .MaximumLength(5000).WithMessage("Fikr 5000 belgidan oshmasligi kerak.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(2048).WithMessage("Rasm manzili juda uzun.");
    }
}
