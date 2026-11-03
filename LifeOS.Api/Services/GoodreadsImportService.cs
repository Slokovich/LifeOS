using System.Globalization;   // CultureInfo: legge i numeri sempre allo stesso modo, a prescindere dalla lingua del PC
using CsvHelper;              // CsvReader: legge il CSV gestendo virgolette, virgole e a capo dentro i campi
using CsvHelper.Configuration; // CsvConfiguration: impostazioni di lettura
using LifeOS.Api.Data;        // LifeOSDbContext (il database)
using LifeOS.Api.Models;      // Book, ReadingStatus
using Microsoft.EntityFrameworkCore; // ToDictionaryAsync e altri strumenti di EF Core

namespace LifeOS.Api.Services;

/// <summary>Riepilogo di un import: quanti libri sono stati aggiunti, aggiornati o saltati.</summary>
/// <param name="Added">Libri nuovi inseriti nel database.</param>
/// <param name="Updated">Libri già presenti (stesso GoodreadsId) aggiornati con i dati del CSV.</param>
/// <param name="Skipped">Righe ignorate perché senza "Book Id", titolo o autore.</param>
public record ImportResult(int Added, int Updated, int Skipped);

/// <summary>
/// Legge il file CSV esportato da Goodreads e salva i libri nel database.
/// Se un libro è già presente (stesso GoodreadsId) viene aggiornato invece di duplicarlo,
/// così si può reimportare il CSV quante volte si vuole (CLAUDE.md §8).
/// </summary>
public class GoodreadsImportService(LifeOSDbContext db)
{
    /// <summary>Importa i libri dal flusso di dati del CSV e restituisce il riepilogo.</summary>
    public async Task<ImportResult> ImportAsync(Stream csvStream)
    {
        // ─── 1. PREPARAZIONE ───
        // Impostazioni di lettura: ignoriamo le colonne mancanti o extra, perché Goodreads
        // potrebbe cambiare il formato; leggiamo i campi per nome, non per posizione.
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,    // non fallire se manca una colonna di intestazione
            MissingFieldFound = null,  // non fallire se una riga ha meno campi
            BadDataFound = null        // non fallire per virgolette scritte male
        };
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, config);

        // Libri già nel DB che hanno un GoodreadsId, indicizzati per trovarli subito.
        // Lo stesso dizionario si aggiorna man mano: copre anche righe doppie nello stesso file.
        var existing = await db.Books
            .Where(b => b.GoodreadsId != null)
            .ToDictionaryAsync(b => b.GoodreadsId!.Value);

        int added = 0, updated = 0, skipped = 0;

        // ─── 2. LETTURA RIGA PER RIGA ───
        await csv.ReadAsync();       // prima riga: le intestazioni
        csv.ReadHeader();
        while (await csv.ReadAsync())
        {
            // "Book Id" è la chiave per riconoscere i duplicati: senza, saltiamo la riga.
            var title = Truncate(csv.GetField("Title"), 500);
            var author = Truncate(csv.GetField("Author"), 200);
            if (!long.TryParse(csv.GetField("Book Id"), out var goodreadsId)
                || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(author))
            {
                skipped++;
                continue;
            }

            // Se il libro esiste già lo aggiorniamo, altrimenti ne creiamo uno nuovo.
            var isNew = !existing.TryGetValue(goodreadsId, out var book);
            if (isNew)
            {
                book = new Book { GoodreadsId = goodreadsId, Title = title, Author = author };
                existing[goodreadsId] = book;
                db.Books.Add(book);
            }

            // Copia i campi del CSV nel libro.
            book!.Title = title;
            book.Author = author;
            book.Isbn13 = CleanIsbn(csv.GetField("ISBN13"));
            book.Pages = int.TryParse(csv.GetField("Number of Pages"), out var pages) ? pages : null;
            // Il voto nel CSV è decimale (es. "3.5"): InvariantCulture = punto come separatore decimale.
            book.MyRating = decimal.TryParse(csv.GetField("My Rating"), NumberStyles.Number,
                CultureInfo.InvariantCulture, out var rating)
                ? Math.Clamp(rating, 0m, 5m) : 0m;
            book.Status = ParseStatus(csv.GetField("Exclusive Shelf"));
            book.DateAdded = ParseDate(csv.GetField("Date Added"));
            book.DateRead = ParseDate(csv.GetField("Date Read"));

            if (isNew) added++; else updated++;
        }

        // ─── 3. SALVATAGGIO ───
        // Un solo SaveChanges per tutto il file: o si salva tutto o niente.
        await db.SaveChangesAsync();
        return new ImportResult(added, updated, skipped);
    }

    /// <summary>Traduce lo scaffale di Goodreads ("read", "currently-reading", "to-read") nel nostro enum.</summary>
    private static ReadingStatus ParseStatus(string? shelf) => shelf?.Trim() switch
    {
        "read" => ReadingStatus.Read,
        "currently-reading" => ReadingStatus.Reading,
        _ => ReadingStatus.ToRead // "to-read" e qualsiasi valore sconosciuto
    };

    /// <summary>Legge le date di Goodreads, scritte come 2024/03/15. Vuoto o illeggibile → null.</summary>
    private static DateOnly? ParseDate(string? text) =>
        DateOnly.TryParseExact(text?.Trim(), "yyyy/MM/dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date) ? date : null;

    /// <summary>
    /// Goodreads scrive gli ISBN come ="9780141036144" (per non farli rovinare da Excel).
    /// Togliamo =, virgolette e spazi; se non resta nulla restituiamo null.
    /// </summary>
    private static string? CleanIsbn(string? raw)
    {
        var clean = raw?.Replace("=", "").Replace("\"", "").Trim();
        return string.IsNullOrEmpty(clean) ? null : clean;
    }

    /// <summary>Accorcia il testo alla lunghezza massima prevista dal database.</summary>
    private static string Truncate(string? text, int max)
    {
        var trimmed = text?.Trim() ?? "";
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
