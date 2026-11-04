// Il namespace corrisponde alla cartella LifeOS.Api/Models.
namespace LifeOS.Api.Models;

/// <summary>
/// Un libro della lista fissa di una <see cref="CompetitiveChallenge"/>, con il suo punteggio.
/// Chi lo finisce nell'anno della sfida guadagna <see cref="Points"/> punti.
/// </summary>
public class ChallengeListItem
{
    /// <summary>Identificativo unico interno, assegnato automaticamente dal database.</summary>
    public int Id { get; set; }

    /// <summary>La sfida a cui appartiene.</summary>
    public int ChallengeId { get; set; }

    /// <summary>Titolo del libro (si confronta con i libri letti ignorando maiuscole, accenti e sottotitolo).</summary>
    public required string Title { get; set; }

    /// <summary>Autore, facoltativo: se c'è serve a distinguere libri con lo stesso titolo.</summary>
    public string? Author { get; set; }

    /// <summary>Punti assegnati a chi lo legge (da 0,5 a 10, anche con decimali: es. 6,5; 5 se non indicato).</summary>
    public decimal Points { get; set; }

    /// <summary>
    /// Vero = vale UN libro qualsiasi di quell'autore (es. "un libro di Asimov"): il titolo è solo
    /// una descrizione e conta il primo libro letto di <see cref="Author"/>.
    /// </summary>
    public bool AnyBookByAuthor { get; set; }
}
