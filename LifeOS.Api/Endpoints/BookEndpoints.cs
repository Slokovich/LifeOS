// Il namespace corrisponde alla cartella LifeOS.Api/Endpoints.
using LifeOS.Api.Data;    // LifeOSDbContext (il database)
using LifeOS.Api.Models;  // Book, ReadingStatus
using LifeOS.Api.Services; // GoodreadsImportService (import del CSV)
using Microsoft.EntityFrameworkCore; // ToListAsync e altri strumenti di EF Core

namespace LifeOS.Api.Endpoints;

/// <summary>
/// Gli "endpoint" dei libri: indirizzi HTTP a cui il frontend può chiedere o inviare dati.
/// Sono raccolti qui, in un file solo, per non riempire Program.cs.
/// </summary>
public static class BookEndpoints
{
    /// <summary>
    /// Registra gli endpoint dei libri sotto /api/books. Viene chiamato da Program.cs.
    /// "this IEndpointRouteBuilder app" = metodo di estensione: permette di scrivere app.MapBookEndpoints().
    /// </summary>
    public static void MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        // Tutti gli endpoint di questo gruppo iniziano con /api/books.
        var group = app.MapGroup("/api/books");

        // GET /api/books → elenco di tutti i libri, in ordine alfabetico di titolo.
        // LifeOSDbContext viene "iniettato" da ASP.NET: basta dichiararlo come parametro.
        group.MapGet("/", async (LifeOSDbContext db) =>
            await db.Books.OrderBy(b => b.Title).ToListAsync());

        // POST /api/books → aggiunge un libro. Il JSON ricevuto diventa un oggetto Book.
        group.MapPost("/", async (Book book, LifeOSDbContext db) =>
        {
            // Controlli minimi sui dati ricevuti (il frontend non è mai "fidato").
            if (string.IsNullOrWhiteSpace(book.Title) || string.IsNullOrWhiteSpace(book.Author))
                return Results.BadRequest("Titolo e autore sono obbligatori.");
            if (book.MyRating is < 0 or > 5)
                return Results.BadRequest("Il voto deve essere tra 0 e 5.");

            book.Id = 0; // l'Id lo assegna il database: ignoriamo quello eventualmente inviato
            db.Books.Add(book);          // prepara l'inserimento...
            await db.SaveChangesAsync(); // ...e lo esegue davvero sul file lifeos.db

            // 201 Created: risponde con il libro salvato (ora con il suo Id) e il suo indirizzo.
            return Results.Created($"/api/books/{book.Id}", book);
        });

        // POST /api/books/import → riceve il file CSV esportato da Goodreads e importa i libri.
        // Il file arriva come modulo "multipart/form-data" nel campo chiamato "file".
        // DisableAntiforgery: la protezione antiforgery serve per i moduli con cookie di login;
        // questa API non usa cookie e accetta richieste solo dal frontend (CORS), quindi la disattiviamo.
        group.MapPost("/import", async (IFormFile file, GoodreadsImportService importer) =>
        {
            if (file.Length == 0)
                return Results.BadRequest("Il file è vuoto.");
            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest("Serve un file .csv esportato da Goodreads.");

            await using var stream = file.OpenReadStream();
            try
            {
                // Risponde con il riepilogo: quanti libri aggiunti, aggiornati, saltati.
                return Results.Ok(await importer.ImportAsync(stream));
            }
            catch (CsvHelper.CsvHelperException)
            {
                // Il file non è un CSV leggibile (es. un altro tipo di file rinominato .csv).
                return Results.BadRequest("Il file non sembra un CSV valido di Goodreads.");
            }
        }).DisableAntiforgery();
    }
}
