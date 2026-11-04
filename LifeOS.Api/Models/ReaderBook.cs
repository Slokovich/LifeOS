// Il namespace corrisponde alla cartella LifeOS.Api/Models.
namespace LifeOS.Api.Models;

/// <summary>
/// Un libro di un'altra persona (<see cref="Reader"/>), inserito dalla chat o importato dal suo CSV
/// di Goodreads. Tabella separata da Books, così le tue statistiche non si mescolano con le sue.
/// </summary>
public class ReaderBook : IReadingRecord
{
    /// <summary>Identificativo unico interno, assegnato automaticamente dal database.</summary>
    public int Id { get; set; }

    /// <summary>Di chi è il libro.</summary>
    public int ReaderId { get; set; }

    /// <summary>La persona a cui appartiene (proprietà di navigazione).</summary>
    public Reader? Reader { get; set; }

    /// <summary>"Book Id" di Goodreads se importato dal CSV (evita duplicati); vuoto se inserito dalla chat.</summary>
    public long? GoodreadsId { get; set; }

    /// <summary>Titolo (obbligatorio, max 500 caratteri).</summary>
    public required string Title { get; set; }

    /// <summary>Autore (obbligatorio, max 200 caratteri).</summary>
    public required string Author { get; set; }

    /// <summary>"Cognome, Nome", se noto (dal CSV).</summary>
    public string? AuthorLastFirst { get; set; }

    /// <summary>Numero di pagine, se noto.</summary>
    public int? Pages { get; set; }

    /// <summary>Voto 0-5 (0 = nessun voto).</summary>
    public decimal MyRating { get; set; }

    /// <summary>Da leggere, in lettura o letto.</summary>
    public ReadingStatus Status { get; set; }

    /// <summary>Quando è stato iniziato (facoltativo, si registra dalla chat).</summary>
    public DateOnly? DateStarted { get; set; }

    /// <summary>Quando è stato finito (facoltativo: dalla chat si può registrare senza data).</summary>
    public DateOnly? DateRead { get; set; }

    /// <summary>
    /// Quando è stato registrato (dalla chat) o aggiunto su Goodreads (dal CSV). Un libro letto
    /// SENZA <see cref="DateRead"/> conta per l'anno di questa data.
    /// </summary>
    public DateOnly? DateAdded { get; set; }
}
