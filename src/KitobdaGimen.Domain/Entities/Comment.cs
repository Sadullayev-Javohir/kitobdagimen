using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// A comment on a post. Supports one level of threaded replies via <see cref="ParentCommentId"/>.
/// </summary>
public class Comment : BaseEntity
{
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Text { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public int? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}
