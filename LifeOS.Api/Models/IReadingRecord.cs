// Il namespace corrisponde alla cartella LifeOS.Api/Models.
namespace LifeOS.Api.Models;

/// <summary>
/// Ciò che hanno in comune un tuo libro (<see cref="Book"/>) e un libro di un'altra persona
/// (<see cref="ReaderBook"/>): serve a calcolare le challenge allo stesso modo per entrambi.
/// </summary>
public interface IReadingRecord
{
    /// <summary>Titolo.</summary>
    string Title { get; }

    /// <summary>Autore ("Nome Cognome").</summary>
    string Author { get; }

    /// <summary>Autore come "Cognome, Nome", se noto.</summary>
    string? AuthorLastFirst { get; }

    /// <summary>Numero di pagine, se noto.</summary>
    int? Pages { get; set; }

    /// <summary>Da leggere, in lettura o letto.</summary>
    ReadingStatus Status { get; set; }

    /// <summary>Quando è stato iniziato, se noto.</summary>
    DateOnly? DateStarted { get; set; }

    /// <summary>Quando è stato finito (vuoto se non ancora letto).</summary>
    DateOnly? DateRead { get; set; }
}
// Nota: le proprietà che cambiano con la lettura hanno anche "set", così la registrazione di una
// lettura dalla chat (PendingActionService) aggiorna allo stesso modo un tuo libro o uno di Fra.
