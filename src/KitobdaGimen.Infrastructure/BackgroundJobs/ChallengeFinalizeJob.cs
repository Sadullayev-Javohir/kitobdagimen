using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Features.Challenge.Commands.FinalizeChallengeMonth;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KitobdaGimen.Infrastructure.BackgroundJobs;

/// <summary>
/// Challenge g'oliblarini avtomatik aniqlash jobi. Har kuni (O'zbekiston vaqti bilan kech soatda)
/// ishga tushadi va davr (2 oy) oxirgi kunida joriy davrni e'lon qiladi; qo'shimcha ravishda
/// oxirgi yakunlangan davrni ham "catch-up" tarzida e'lon qiladi (agar bir kun o'tkazib
/// yuborilgan bo'lsa). Amal idempotent: allaqachon e'lon qilingan davr qayta yaratilmaydi.
/// Avtomatik jarayon ishlamay qolsa, super admin qo'lda ishga tushira oladi (/challenge/admin).
/// </summary>
public class ChallengeFinalizeJob
{
    /// <summary>Hangfire recurring job identifikatori.</summary>
    public const string RecurringJobId = "challenge-finalize";

    private readonly ISender _mediator;
    private readonly ILogger<ChallengeFinalizeJob> _logger;

    public ChallengeFinalizeJob(ISender mediator, ILogger<ChallengeFinalizeJob> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // 1) Joriy davr aynan bugun tugayaptimi — bugun (oxirgi kun) g'oliblarni aniqlaymiz.
        var (cy, cm) = ChallengeCalendar.CurrentPeriod();
        var (_, to) = ChallengeCalendar.Range(cy, cm);
        if (UzTime.Today == to)
        {
            await FinalizeAsync(cy, cm, cancellationToken);
        }

        // 2) Oxirgi yakunlangan davr — catch-up (o'tkazib yuborilgan bo'lsa). Idempotent.
        var (ly, lm) = ChallengeCalendar.LastCompletedPeriod();
        await FinalizeAsync(ly, lm, cancellationToken);
    }

    private async Task FinalizeAsync(int year, int month, CancellationToken ct)
    {
        try
        {
            var count = await _mediator.Send(
                new FinalizeChallengeMonthCommand(year, month) { BypassAdminCheck = true }, ct);
            if (count > 0)
            {
                _logger.LogInformation(
                    "Challenge {Period}: {Count} ta g'olib avtomatik e'lon qilindi.",
                    ChallengeCalendar.PeriodLabel(year, month), count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Challenge {Period} avtomatik e'lon qilinmadi.",
                ChallengeCalendar.PeriodLabel(year, month));
        }
    }
}
