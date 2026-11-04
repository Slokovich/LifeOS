// Il namespace corrisponde alla cartella LifeOS.Api/Models.
namespace LifeOS.Api.Models;

/// <summary>
/// I dati che servono per calcolare una challenge: i tuoi libri finiti nell'anno, quelli delle
/// altre persone (per le sfide competitive) e la data di oggi. Ogni tipo di challenge usa ciò che gli serve.
/// </summary>
/// <param name="MyBooksRead">I tuoi libri finiti nell'anno della challenge.</param>
/// <param name="ReaderBooksRead">Per ogni Reader (chiave = ReaderId), i suoi libri finiti nell'anno.</param>
/// <param name="Today">La data di oggi.</param>
public record ChallengeContext(
    IReadOnlyList<Book> MyBooksRead,
    IReadOnlyDictionary<int, IReadOnlyList<ReaderBook>> ReaderBooksRead,
    DateOnly Today);
