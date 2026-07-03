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
            .Must(x => !string.IsNullOrWhiteSpace(x.Text)
                       || x.SharedPostId.HasValue
                       || !string.IsNullOrWhiteSpace(x.ImageUrl)
                       || !string.IsNullOrWhiteSpace(x.StickerKey)
                       || !string.IsNullOrWhiteSpace(x.VoiceUrl))
            .WithMessage("Xabar matni, rasm, ovoz, stiker yoki ulashilgan post bo'lishi kerak.");

        // Rasm URL'i faqat ichki yuklama papkasidan bo'lishi mumkin (tashqi/injeksiya emas).
        RuleFor(x => x.ImageUrl)
            .Must(url => string.IsNullOrWhiteSpace(url) || url.StartsWith("/uploads/", StringComparison.Ordinal))
            .WithMessage("Rasm manzili noto'g'ri.");

        // Ovoz URL'i ham faqat ichki yuklama papkasidan.
        RuleFor(x => x.VoiceUrl)
            .Must(url => string.IsNullOrWhiteSpace(url) || url.StartsWith("/uploads/", StringComparison.Ordinal))
            .WithMessage("Ovoz manzili noto'g'ri.");

        RuleFor(x => x.StickerKey)
            .MaximumLength(64).WithMessage("Stiker kaliti juda uzun.");

        RuleFor(x => x.Text)
            .MaximumLength(5000).WithMessage("Xabar 5000 belgidan oshmasligi kerak.");
    }
}
