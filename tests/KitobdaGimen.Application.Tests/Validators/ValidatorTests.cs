using KitobdaGimen.Application.Features.Auth.Commands.LoginWithGoogle;
using KitobdaGimen.Application.Features.Chat.Commands.SendMessage;
using KitobdaGimen.Application.Features.Onboarding.Commands.SaveUserGenres;
using KitobdaGimen.Application.Features.Posts.Commands.CreatePost;
using KitobdaGimen.Application.Features.ReadingGoals.Commands.UpdateReadingProgress;

namespace KitobdaGimen.Application.Tests.Validators;

public class ValidatorTests
{
    [Fact]
    public void LoginWithGoogle_requires_googleId_email_and_name()
    {
        var validator = new LoginWithGoogleCommandValidator();

        Assert.True(validator.Validate(new LoginWithGoogleCommand
        {
            GoogleId = "g-1", Email = "a@b.com", FullName = "Ali"
        }).IsValid);

        var bad = validator.Validate(new LoginWithGoogleCommand
        {
            GoogleId = "", Email = "not-an-email", FullName = ""
        });
        Assert.False(bad.IsValid);
        Assert.Contains(bad.Errors, e => e.PropertyName == nameof(LoginWithGoogleCommand.GoogleId));
        Assert.Contains(bad.Errors, e => e.PropertyName == nameof(LoginWithGoogleCommand.Email));
        Assert.Contains(bad.Errors, e => e.PropertyName == nameof(LoginWithGoogleCommand.FullName));
    }

    [Fact]
    public void SaveUserGenres_requires_at_least_one_distinct_genre()
    {
        var validator = new SaveUserGenresCommandValidator();

        Assert.True(validator.Validate(new SaveUserGenresCommand { GenreIds = new[] { 1, 2 } }).IsValid);
        Assert.False(validator.Validate(new SaveUserGenresCommand { GenreIds = Array.Empty<int>() }).IsValid);
        Assert.False(validator.Validate(new SaveUserGenresCommand { GenreIds = new[] { 1, 1 } }).IsValid); // duplicates
    }

    [Fact]
    public void CreatePost_requires_book_and_non_empty_text()
    {
        var validator = new CreatePostCommandValidator();

        Assert.True(validator.Validate(new CreatePostCommand { BookId = 1, ReviewText = "yaxshi" }).IsValid);
        Assert.False(validator.Validate(new CreatePostCommand { BookId = 0, ReviewText = "yaxshi" }).IsValid);
        Assert.False(validator.Validate(new CreatePostCommand { BookId = 1, ReviewText = "" }).IsValid);
        Assert.False(validator.Validate(new CreatePostCommand { BookId = 1, ReviewText = "ok" }).IsValid); // 3 belgidan kam
        Assert.False(validator.Validate(new CreatePostCommand { BookId = 1, ReviewText = new string('x', 5001) }).IsValid);
    }

    [Fact]
    public void UpdateReadingProgress_requires_positive_pages()
    {
        var validator = new UpdateReadingProgressCommandValidator();

        Assert.True(validator.Validate(new UpdateReadingProgressCommand { ReadingGoalId = 1, PagesRead = 10 }).IsValid);
        Assert.False(validator.Validate(new UpdateReadingProgressCommand { ReadingGoalId = 0, PagesRead = 10 }).IsValid);
        Assert.False(validator.Validate(new UpdateReadingProgressCommand { ReadingGoalId = 1, PagesRead = 0 }).IsValid);
        Assert.False(validator.Validate(new UpdateReadingProgressCommand { ReadingGoalId = 1, PagesRead = 99999 }).IsValid);
    }

    [Fact]
    public void SendMessage_requires_target_and_content()
    {
        var validator = new SendMessageCommandValidator();

        Assert.True(validator.Validate(new SendMessageCommand { RecipientId = 2, Text = "salom" }).IsValid);
        Assert.True(validator.Validate(new SendMessageCommand { ConversationId = 1, SharedPostId = 5 }).IsValid);

        // no target
        Assert.False(validator.Validate(new SendMessageCommand { Text = "salom" }).IsValid);
        // target but no content
        Assert.False(validator.Validate(new SendMessageCommand { RecipientId = 2 }).IsValid);
    }
}
