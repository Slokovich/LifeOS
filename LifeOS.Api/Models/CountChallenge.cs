// Il namespace corrisponde alla cartella LifeOS.Api/Models.
namespace LifeOS.Api.Models;

/// <summary>
/// Challenge "a numero": leggere almeno <see cref="TargetBooks"/> libri nell'anno (es. 60 nel 2026).
/// Al massimo una per anno (controllato da PendingActionService).
/// </summary>
public class CountChallenge : Challenge
{
    /// <summary>Quanti libri leggere nell'anno.</summary>
    public int TargetBooks { get; set; }

    /// <summary>Libri letti, mancanti e ritmo atteso a oggi.</summary>
    public override ChallengeProgress Evaluate(ChallengeContext context)
    {
        var read = context.MyBooksRead.Count;
        // Ritmo atteso: quanti libri dovresti aver finito a oggi per arrivare all'obiettivo a fine anno.
        var expected = Math.Round(TargetBooks * YearFractionElapsed(context.Today), 1);
        return new ChallengeProgress(Id, "Count", Name, read >= TargetBooks, new
        {
            target = TargetBooks,
            booksRead = read,
            remaining = Math.Max(0, TargetBooks - read),
            expectedByToday = expected,
            difference = Math.Round(read - expected, 1) // positivo = in anticipo, negativo = in ritardo
        });
    }
}
