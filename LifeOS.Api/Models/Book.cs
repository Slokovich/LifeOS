// Il namespace raggruppa le classi; corrisponde alla cartella LifeOS.Api/Models.
namespace LifeOS.Api.Models;

/// <summary>Stato di lettura, come gli scaffali di Goodreads. Salvato nel DB come testo.</summary>
public enum ReadingStatus
{
    ToRead,   // da leggere ("to-read")
    Reading,  // in lettura ("currently-reading")
    Read      // letto ("read")
}

/// <summary>
/// Libro importato dal CSV di Goodreads, usato dall'agente Libri per la reading challenge annuale.
/// Ogni oggetto = una riga della tabella Books.
/// </summary>
public class Book
{
    /// <summary>Identificativo unico interno, assegnato automaticamente dal database.</summary>
    public int Id { get; set; }

    /// <summary>"Book Id" del CSV Goodreads, per evitare duplicati sugli import successivi (indice unico).</summary>
    public long? GoodreadsId { get; set; }

    /// <summary>Titolo (obbligatorio, max 500 caratteri).</summary>
    public required string Title { get; set; }

    /// <summary>Autore principale (obbligatorio, max 200 caratteri).</summary>
    public required string Author { get; set; }

    /// <summary>Codice ISBN a 13 cifre, se presente nel CSV. È testo perché può iniziare con zero.</summary>
    public string? Isbn13 { get; set; }

    /// <summary>Numero di pagine, se noto.</summary>
    public int? Pages { get; set; }

    /// <summary>Voto 0-5, anche con decimali (es. 3.5 o 2.25); 0 = nessun voto, come in Goodreads.</summary>
    public decimal MyRating { get; set; }

    /// <summary>Da leggere, in lettura o letto.</summary>
    public ReadingStatus Status { get; set; }

    /// <summary>Quando il libro è stato aggiunto alla libreria.</summary>
    public DateOnly? DateAdded { get; set; }

    /// <summary>Quando è stato finito: serve a contare i libri letti nell'anno (challenge).</summary>
    public DateOnly? DateRead { get; set; }
}
