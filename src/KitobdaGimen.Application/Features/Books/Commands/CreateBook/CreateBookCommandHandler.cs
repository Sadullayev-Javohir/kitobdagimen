using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Books.Dtos;
using KitobdaGimen.Domain.Entities;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Books.Commands.CreateBook;

public class CreateBookCommandHandler : IRequestHandler<CreateBookCommand, BookDto>
{
    private readonly IAppDbContext _db;

    public CreateBookCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<BookDto> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        var title = request.Title.Trim();
        var author = request.Author.Trim();

        // Reuse an existing identical book to avoid duplicate catalogue entries.
        // Case-insensitive: matching by exact case previously let "Kitob Nomi" and
        // "kitob nomi" create separate Book rows, silently splitting that book's
        // reviews/quotes across two records.
        var titleLower = title.ToLower();
        var authorLower = author.ToLower();
        var existing = await _db.Books
            .FirstOrDefaultAsync(b => b.Title.ToLower() == titleLower && b.Author.ToLower() == authorLower, cancellationToken);

        if (existing is not null)
        {
            // Avval qo'lda kiritilgan kitob endi tashqi manbadan import qilinsa,
            // manbani (attribution) to'ldiramiz — mavjud yozuvni qaytadan yaratmaymiz.
            if (existing.Source is null && request.Source is not null)
            {
                existing.Source = request.Source;
                await _db.SaveChangesAsync(cancellationToken);
            }

            return existing.Adapt<BookDto>();
        }

        var book = new Book
        {
            Title = title,
            Author = author,
            TotalPages = request.TotalPages,
            CoverUrl = request.CoverUrl,
            GenreId = request.GenreId,
            Source = request.Source
        };

        _db.Books.Add(book);
        await _db.SaveChangesAsync(cancellationToken);

        return book.Adapt<BookDto>();
    }
}
