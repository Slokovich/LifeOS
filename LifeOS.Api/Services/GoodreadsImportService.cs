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
    /// <summary>Una riga valida del CSV di Goodreads, già letta e ripulita.</summary>
    private record GoodreadsRow(long GoodreadsId, string Title, string Author, string? AuthorLastFirst,
        string? Isbn13, int? Pages, decimal MyRating, ReadingStatus Status, DateOnly? DateAdded, DateOnly? DateRead);

    /// <summary>Importa i TUOI libri dal CSV (tabella Books) e restituisce il riepilogo.</summary>
    public async Task<ImportResult> ImportAsync(Stream csvStream)
    {
        // Libri già nel DB che hanno un GoodreadsId, indicizzati per trovarli subito.
        // Lo stesso dizionario si aggiorna man mano: copre anche righe doppie nello stesso file.
        var existing = await db.Books
            .Where(b => b.GoodreadsId != null)
            .ToDictionaryAsync(b => b.GoodreadsId!.Value);
        // Libri registrati dalla chat (senza GoodreadsId): si collegano alla riga del CSV invece di duplicarli.
        var unlinked = await db.Books.Where(b => b.GoodreadsId == null).ToListAsync();

        int added = 0, updated = 0;
        var (rows, skipped) = await ReadRowsAsync(csvStream);
        foreach (var row in rows)
        {
            // Se il libro esiste già (stesso GoodreadsId, o registrato dalla chat) lo aggiorniamo,
            // altrimenti ne creiamo uno nuovo.
            var isNew = !existing.TryGetValue(row.GoodreadsId, out var book);
            if (isNew && FindUnlinked(unlinked, row) is { } fromChat)
            {
                book = fromChat;
                book.GoodreadsId = row.GoodreadsId;
                unlinked.Remove(fromChat);
                existing[row.GoodreadsId] = book;
                isNew = false;
            }
            if (isNew)
            {
                book = new Book { GoodreadsId = row.GoodreadsId, Title = row.Title, Author = row.Author, DateAdded = row.DateAdded };
                existing[row.GoodreadsId] = book;
                db.Books.Add(book);
            }

            // Copia i campi del CSV nel libro.
            book!.Title = row.Title;
            book.Author = row.Author;
            book.AuthorLastFirst = row.AuthorLastFirst;
            book.Isbn13 = row.Isbn13;
            book.MyRating = row.MyRating;
            book.DateAdded = row.DateAdded ?? book.DateAdded;
            MergeReading(book, row);
            if (isNew) added++; else updated++;
        }

        // Un solo SaveChanges per tutto il file: o si salva tutto o niente.
        await db.SaveChangesAsync();
        return new ImportResult(added, updated, skipped);
    }

    /// <summary>
    /// Importa i libri di un'ALTRA persona (es. Fra) dal suo CSV di Goodreads (tabella ReaderBooks).
    /// La persona viene creata se non esiste. Anche qui reimportare aggiorna senza duplicare.
    /// </summary>
    public async Task<ImportResult> ImportForReaderAsync(Stream csvStream, string readerName)
    {
        var clean = readerName.Trim();
        var reader = await db.Readers.FirstOrDefaultAsync(r => r.Name.ToLower() == clean.ToLower());
        if (reader is null)
        {
            reader = new Reader { Name = clean };
            db.Readers.Add(reader);
        }

        // Libri già importati per questa persona (solo se esiste già: una nuova non ne ha).
        var existing = reader.Id == 0
            ? new Dictionary<long, ReaderBook>()
            : await db.ReaderBooks.Where(b => b.ReaderId == reader.Id && b.GoodreadsId != null)
                .ToDictionaryAsync(b => b.GoodreadsId!.Value);
        // Suoi libri registrati dalla chat (senza GoodreadsId): da collegare al CSV invece di duplicarli.
        List<ReaderBook> unlinked = reader.Id == 0 ? []
            : await db.ReaderBooks.Where(b => b.ReaderId == reader.Id && b.GoodreadsId == null).ToListAsync();
        var thisYear = DateTime.Today.Year;

        int added = 0, updated = 0;
        var (rows, skipped) = await ReadRowsAsync(csvStream);
        foreach (var row in rows)
        {
            var isNew = !existing.TryGetValue(row.GoodreadsId, out var book);
            if (isNew && FindUnlinked(unlinked, row) is { } fromChat)
            {
                // Collegato a un libro registrato dalla chat: si tiene la sua data di registrazione
                // (DateAdded), perché è l'informazione data dall'utente su quando conta.
                book = fromChat;
                book.GoodreadsId = row.GoodreadsId;
                unlinked.Remove(fromChat);
                existing[row.GoodreadsId] = book;
                isNew = false;
            }
            if (isNew)
            {
                book = new ReaderBook
                {
                    Reader = reader, GoodreadsId = row.GoodreadsId, Title = row.Title, Author = row.Author,
                    DateAdded = row.DateAdded,
                    // Letto senza data e aggiunto su Goodreads in un anno passato: probabilmente un
                    // caricamento in blocco di vecchie letture, quindi l'anno non si sa (CLAUDE.md §8).
                    // Solo per i libri nuovi: a un reimport il valore già salvato non cambia.
                    ReadYearUnknown = row.Status == ReadingStatus.Read && row.DateRead is null
                                      && row.DateAdded is { } added0 && added0.Year < thisYear
                };
                existing[row.GoodreadsId] = book;
                db.ReaderBooks.Add(book);
            }

            book!.Title = row.Title;
            book.Author = row.Author;
            book.AuthorLastFirst = row.AuthorLastFirst;
            book.MyRating = row.MyRating;
            MergeReading(book, row);
            if (isNew) added++; else updated++;
        }

        await db.SaveChangesAsync();
        return new ImportResult(added, updated, skipped);
    }

    /// <summary>
    /// Cerca, tra i libri registrati dalla chat (senza GoodreadsId), quello che corrisponde alla riga
    /// del CSV: stesse regole della sfida (titolo + cognome dell'autore), oppure titolo identico
    /// se l'autore è scritto in modo diverso (es. "Dostoevskij" in chat, "Dostoevsky" su Goodreads).
    /// </summary>
    private static T? FindUnlinked<T>(List<T> unlinked, GoodreadsRow row) where T : class, IReadingRecord
    {
        var rowAsRecord = new ReaderBook { Title = row.Title, Author = row.Author }; // solo per il confronto
        return unlinked.FirstOrDefault(b => BookMatching.Matches(row.Title, row.Author, b)
                                            || BookMatching.Matches(b.Title, null, rowAsRecord));
    }

    /// <summary>
    /// Copia stato, data di fine e pagine dal CSV SENZA perdere ciò che è stato registrato dalla chat:
    /// se Goodreads dice ancora "da leggere" ma qui il libro risulta iniziato, letto o abbandonato,
    /// lo stato resta; una data di fine o un numero di pagine mancanti nel CSV non cancellano quelli salvati.
    /// La data di inizio (DateStarted) non è nel CSV e non viene toccata.
    /// </summary>
    private static void MergeReading(IReadingRecord record, GoodreadsRow row)
    {
        if (!(row.Status == ReadingStatus.ToRead && record.Status != ReadingStatus.ToRead))
            record.Status = row.Status;
        record.DateRead = row.DateRead ?? record.DateRead;
        record.Pages = row.Pages ?? record.Pages;
    }

    /// <summary>
    /// Legge tutte le righe del CSV di Goodreads. Restituisce quelle valide e quante sono state
    /// saltate (senza "Book Id", titolo o autore). Comune ai due import qui sopra.
    /// </summary>
    private static async Task<(List<GoodreadsRow> Rows, int Skipped)> ReadRowsAsync(Stream csvStream)
    {
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

        var rows = new List<GoodreadsRow>();
        var skipped = 0;
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

            // "Cognome, Nome": serve alla challenge alfabeto. Vuoto → null.
            var lastFirst = Truncate(csv.GetField("Author l-f"), 200);
            rows.Add(new GoodreadsRow(
                goodreadsId, title, author,
                lastFirst.Length > 0 ? lastFirst : null,
                CleanIsbn(csv.GetField("ISBN13")),
                int.TryParse(csv.GetField("Number of Pages"), out var pages) ? pages : null,
                // Il voto nel CSV è decimale (es. "3.5"): InvariantCulture = punto come separatore decimale.
                decimal.TryParse(csv.GetField("My Rating"), NumberStyles.Number, CultureInfo.InvariantCulture, out var rating)
                    ? Math.Clamp(rating, 0m, 5m) : 0m,
                ParseStatus(csv.GetField("Exclusive Shelf")),
                ParseDate(csv.GetField("Date Added")),
                ParseDate(csv.GetField("Date Read"))));
        }
        return (rows, skipped);
    }

    /// <summary>
    /// Traduce lo scaffale di Goodreads ("read", "currently-reading", "to-read") nel nostro enum.
    /// Gli scaffali personali per i libri lasciati a metà ("dnf", "did-not-finish", "abandoned",
    /// "abbandonati"…) diventano Abandoned.
    /// </summary>
    private static ReadingStatus ParseStatus(string? shelf)
    {
        var s = shelf?.Trim().ToLowerInvariant() ?? "";
        if (s is "dnf" or "did-not-finish" || s.Contains("abandon") || s.Contains("abbandon"))
            return ReadingStatus.Abandoned;
        return s switch
        {
            "read" => ReadingStatus.Read,
            "currently-reading" => ReadingStatus.Reading,
            _ => ReadingStatus.ToRead // "to-read" e qualsiasi valore sconosciuto
        };
    }

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
