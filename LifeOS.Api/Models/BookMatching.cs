using System.Globalization; // UnicodeCategory: per togliere gli accenti
using System.Text;          // NormalizationForm, StringBuilder

// Il namespace corrisponde alla cartella LifeOS.Api/Models.
namespace LifeOS.Api.Models;

/// <summary>
/// Riconosce se due libri sono "lo stesso libro" anche se scritti in modo un po' diverso
/// (maiuscole, accenti, punteggiatura, sottotitolo). Usato dalla sfida competitiva per capire
/// se un libro della lista è stato letto. Limite: titoli tradotti diversamente non coincidono.
/// </summary>
public static class BookMatching
{
    /// <summary>Vero se il libro letto vale per questo elemento della lista della sfida.</summary>
    public static bool Matches(ChallengeListItem item, IReadingRecord read) =>
        item.AnyBookByAuthor
            ? AuthorMatches(item.Author, read)                  // "un libro qualsiasi di X"
            : Matches(item.Title, item.Author, read);

    /// <summary>
    /// Vero se il libro letto corrisponde a titolo + autore della lista:
    /// - senza autore: il titolo (normalizzato, senza sottotitolo) deve essere identico;
    /// - con autore: il cognome deve comparire nell'autore letto, e basta che un titolo sia l'inizio
    ///   dell'altro (Goodreads aggiunge spesso parole: "Jurassic Park (Jurassic Park, #1)",
    ///   "M. Il figlio del secolo"). L'autore evita gli abbinamenti sbagliati tra titoli corti come "It".
    /// </summary>
    public static bool Matches(string listTitle, string? listAuthor, IReadingRecord read)
    {
        var a = NormalizeTitle(listTitle);
        var b = NormalizeTitle(read.Title);
        if (string.IsNullOrWhiteSpace(listAuthor)) return a == b;
        if (!AuthorMatches(listAuthor, read)) return false;
        return a == b || StartsWithWords(a, b) || StartsWithWords(b, a);
    }

    /// <summary>Vero se il cognome (ultima parola) dell'autore della lista compare nell'autore letto.</summary>
    private static bool AuthorMatches(string? listAuthor, IReadingRecord read)
    {
        var surname = Normalize(listAuthor ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        return surname is not null && Normalize(read.Author).Split(' ').Contains(surname);
    }

    /// <summary>Vero se "text" inizia con "start" a parole intere, o finisce con esso ("m il figlio del secolo" finisce con "il figlio del secolo").</summary>
    private static bool StartsWithWords(string start, string text) =>
        start.Length > 0 && (text.StartsWith(start + " ") || text.EndsWith(" " + start));

    /// <summary>Titolo senza sottotitolo (dopo ":" o "(") e normalizzato.</summary>
    private static string NormalizeTitle(string title)
    {
        var cut = title.IndexOfAny([':', '(']);
        return Normalize(cut > 0 ? title[..cut] : title);
    }

    /// <summary>Minuscole, senza accenti, solo lettere/cifre separate da un singolo spazio.</summary>
    private static string Normalize(string text)
    {
        var sb = new StringBuilder();
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue; // accento
            sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        }
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
