using FluentValidation;

namespace KitobdaGimen.Application.Features.Chat.Commands.SendMessage;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => x.ConversationId.HasValue || x.RecipientId.HasValue)
            .WithMessage("Suhbat yoki qabul qiluvchi ko'rsatilishi kerak.");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Text) || x.SharedPostId.HasValue)
            .WithMessage("Xabar matni yoki ulashilgan post bo'lishi kerak.");

        RuleFor(x => x.Text)
            .MaximumLength(5000).WithMessage("Xabar 5000 belgidan oshmasligi kerak.");
    }
}
