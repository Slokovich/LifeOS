// Il namespace corrisponde alla cartella LifeOS.Web/Models.
namespace LifeOS.Web.Models;

/// <summary>
/// Un libro così come lo vede il frontend: copia semplificata di LifeOS.Api.Models.Book.
/// Il frontend non può usare direttamente le classi dell'API (vedi CLAUDE.md §9 punto 8:
/// in futuro i modelli condivisi andranno in un progetto LifeOS.Shared).
/// I nomi delle proprietà devono coincidere con quelli del JSON inviato dall'API.
/// </summary>
public class BookItem
{
    /// <summary>Identificativo assegnato dal database.</summary>
    public int Id { get; set; }

    /// <summary>Titolo del libro.</summary>
    public string Title { get; set; } = "";

    /// <summary>Autore principale.</summary>
    public string Author { get; set; } = "";

    /// <summary>
    /// Stato di lettura come testo: "ToRead", "Reading", "Read" o "Abandoned".
    /// Qui è una stringa (e non un enum) perché l'API invia gli enum come parole.
    /// </summary>
    public string Status { get; set; } = "ToRead";
}
