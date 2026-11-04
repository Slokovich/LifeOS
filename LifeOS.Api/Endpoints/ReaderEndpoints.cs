using LifeOS.Api.Data;     // LifeOSDbContext (il database)
using LifeOS.Api.Services; // GoodreadsImportService (import del CSV)
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Api.Endpoints;

/// <summary>
/// Endpoint delle altre persone che leggono (es. Fra): elenco e import del loro CSV di Goodreads.
/// </summary>
public static class ReaderEndpoints
{
    /// <summary>Registra gli endpoint sotto /api/readers. Viene chiamato da Program.cs.</summary>
    public static void MapReaderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/readers");

        // GET /api/readers → elenco delle persone con quanti libri hanno nel database.
        group.MapGet("/", async (LifeOSDbContext db) =>
            await db.Readers.OrderBy(r => r.Name)
                .Select(r => new { r.Id, r.Name, books = r.Books.Count })
                .ToListAsync());

        // POST /api/readers/{name}/import → importa il CSV di Goodreads di quella persona (la crea se non esiste).
        // È un'azione dell'utente dall'interfaccia, non dell'agente: non serve la proposta/conferma.
        // DisableAntiforgery: stesso motivo di /api/books/import (CLAUDE.md §8).
        group.MapPost("/{name}/import", async (string name, IFormFile file, GoodreadsImportService importer) =>
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
                return Results.BadRequest("Nome della persona mancante o troppo lungo.");
            if (file.Length == 0)
                return Results.BadRequest("Il file è vuoto.");
            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest("Serve un file .csv esportato da Goodreads.");

            await using var stream = file.OpenReadStream();
            try
            {
                return Results.Ok(await importer.ImportForReaderAsync(stream, name));
            }
            catch (CsvHelper.CsvHelperException)
            {
                return Results.BadRequest("Il file non sembra un CSV valido di Goodreads.");
            }
        }).DisableAntiforgery();
    }
}
