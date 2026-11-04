using System.Globalization; // UnicodeCategory: per togliere gli accenti
using System.Text;          // NormalizationForm

// Il namespace corrisponde alla cartella LifeOS.Api/Models.
namespace LifeOS.Api.Models;

/// <summary>
/// Challenge "alfabeto": nell'anno, leggere un autore per ognuna delle 26 lettere A-Z,
/// in base all'iniziale del COGNOME (scelta dell'utente, 2026-11-03).
/// </summary>
public class AlphabetChallenge : Challenge
{
    /// <summary>Le 26 lettere da coprire.</summary>
    public const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>Parti finali del nome che non sono il cognome (confronto senza maiuscole/minuscole).</summary>
    private static readonly HashSet<string> NameSuffixes = new(StringComparer.OrdinalIgnoreCase) { "Jr", "Sr", "II", "III", "IV" };

    /// <summary>Lettere coperte (con il primo libro che le copre), mancanti e ritmo atteso.</summary>
    public override ChallengeProgress Evaluate(ChallengeContext context)
    {
        // Per ogni lettera teniamo il primo libro letto (in ordine di data) che la copre.
        var covered = new SortedDictionary<char, Book>();
        foreach (var book in context.MyBooksRead.OrderBy(b => b.DateRead))
            if (SurnameInitial(book) is char letter && !covered.ContainsKey(letter))
                covered[letter] = book;

        var missing = Letters.Where(l => !covered.ContainsKey(l)).Select(l => l.ToString()).ToList();
        var expected = Math.Round(Letters.Length * YearFractionElapsed(context.Today), 1);
        return new ChallengeProgress(Id, "Alphabet", Name, missing.Count == 0, new
        {
            lettersTotal = Letters.Length,
            lettersCovered = covered.Count,
            expectedByToday = expected,
            difference = Math.Round(covered.Count - expected, 1),
            covered = covered.Select(c => new { letter = c.Key.ToString(), c.Value.Author, c.Value.Title }),
            missing
        });
    }

    /// <summary>
    /// Iniziale del cognome dell'autore, senza accenti (É → E). Null se non è una lettera A-Z.
    /// Usa "Author l-f" di Goodreads ("Levi, Primo") se c'è; altrimenti l'ultima parola dell'autore
    /// ("Primo Levi" → Levi), che sbaglia con i cognomi composti (es. "García Márquez" → M).
    /// </summary>
    public static char? SurnameInitial(IReadingRecord book)
    {
        // Senza "Author l-f": ultima parola dell'autore, saltando suffissi come "Jr." (Kurt Vonnegut Jr. → Vonnegut).
        var surname = !string.IsNullOrWhiteSpace(book.AuthorLastFirst)
            ? book.AuthorLastFirst
            : book.Author.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault(w => !NameSuffixes.Contains(w.TrimEnd('.', ',')));
        if (string.IsNullOrWhiteSpace(surname)) return null;

        // Scompone le lettere accentate (é = e + accento) e tiene solo la lettera base.
        var first = surname.Trim().Normalize(NormalizationForm.FormD)
            .FirstOrDefault(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        var upper = char.ToUpperInvariant(first);
        return Letters.Contains(upper) ? upper : null;
    }
}
