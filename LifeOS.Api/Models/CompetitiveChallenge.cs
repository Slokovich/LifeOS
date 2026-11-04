// Il namespace corrisponde alla cartella LifeOS.Api/Models.
namespace LifeOS.Api.Models;

/// <summary>
/// Sfida "competitiva": tu contro un'altra persona (<see cref="Opponent"/>, es. Fra) su una lista
/// fissa di libri, ognuno con il suo punteggio. Nell'anno della sfida, chi finisce un libro della
/// lista ne guadagna i punti; vince chi ha più punti a fine anno.
/// </summary>
public class CompetitiveChallenge : Challenge
{
    /// <summary>Id dell'avversaria.</summary>
    public int OpponentId { get; set; }

    /// <summary>L'avversaria (proprietà di navigazione).</summary>
    public Reader? Opponent { get; set; }

    /// <summary>La lista fissa dei libri con i loro punti.</summary>
    public List<ChallengeListItem> Items { get; set; } = [];

    /// <summary>
    /// Punteggi tuoi e dell'avversaria, libro per libro. "Raggiunta" = a fine anno sei davanti.
    /// Richiede che Opponent e Items siano stati caricati dal database (Include).
    /// </summary>
    public override ChallengeProgress Evaluate(ChallengeContext context)
    {
        var mine = context.MyBooksRead;
        var theirs = context.ReaderBooksRead.GetValueOrDefault(OpponentId) ?? [];

        // Per ogni libro della lista: chi l'ha letto nell'anno e quando.
        var items = Items.OrderByDescending(i => i.Points).ThenBy(i => i.Title).Select(i => new
        {
            i.Id,
            i.Title,
            i.Author,
            i.Points,
            i.AnyBookByAuthor,
            // Il libro letto che vale per questo elemento (null = non letto). La data può mancare
            // per i libri dell'avversaria registrati senza data: per questo restituiamo anche il titolo.
            readByMe = mine.FirstOrDefault(b => BookMatching.Matches(i, b)) is { } m ? new { m.Title, m.DateRead } : null,
            readByOpponent = theirs.FirstOrDefault(b => BookMatching.Matches(i, b)) is { } t ? new { t.Title, t.DateRead } : null
        }).ToList();

        var myScore = items.Where(i => i.readByMe is not null).Sum(i => i.Points);
        var opponentScore = items.Where(i => i.readByOpponent is not null).Sum(i => i.Points);
        var yearOver = context.Today > new DateOnly(Year, 12, 31);

        return new ChallengeProgress(Id, "Competitive", Name, yearOver && myScore > opponentScore, new
        {
            opponent = Opponent?.Name,
            myScore,
            opponentScore,
            difference = myScore - opponentScore, // positivo = sei avanti
            pointsAvailable = items.Sum(i => i.Points),
            yearOver,
            items
        });
    }
}
