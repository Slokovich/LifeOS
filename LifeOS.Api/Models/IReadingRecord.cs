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
    int? Pages { get; }

    /// <summary>Quando è stato finito (vuoto se non ancora letto).</summary>
    DateOnly? DateRead { get; }
}
