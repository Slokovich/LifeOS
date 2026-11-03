using System.ComponentModel; // [Description]: il testo che spiega a Claude a cosa serve uno strumento
using LifeOS.Api.Data;        // LifeOSDbContext (il database)
using LifeOS.Api.Models;      // Book, ReadingStatus
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Api.Agents;

/// <summary>
/// Gli "strumenti" dell'agente Libri: metodi che Claude può chiamare per leggere i dati.
/// Claude non vede il database: vede solo la descrizione di ogni metodo e ciò che restituisce.
/// Di proposito leggono SOLO la tabella Books e restituiscono solo i campi necessari (CLAUDE.md §6).
/// </summary>
public class BookTools(LifeOSDbContext db)
{
    /// <summary>Riepilogo dei libri finiti in un certo anno (per la reading challenge).</summary>
    [Description("Riepilogo dei libri letti (finiti) in un anno: quanti, pagine totali, voto medio e titoli.")]
    public async Task<object> GetYearSummary(
        [Description("L'anno, per esempio 2026")] int year)
    {
        // Libri con stato "Letto" e data di fine lettura dentro l'anno richiesto.
        // Confrontiamo le date invece di usare .Year perché è più sicuro con SQLite.
        var from = new DateOnly(year, 1, 1);
        var to = new DateOnly(year, 12, 31);
        var read = await db.Books
            .Where(b => b.Status == ReadingStatus.Read && b.DateRead >= from && b.DateRead <= to)
            .OrderBy(b => b.DateRead)
            .ToListAsync();

        // Voto medio calcolato qui in memoria (SQLite non sa fare la media di un decimal) e
        // solo sui libri votati: 0 significa "nessun voto" (CLAUDE.md §8).
        var rated = read.Where(b => b.MyRating > 0).ToList();
        return new
        {
            year,
            booksRead = read.Count,
            totalPages = read.Sum(b => b.Pages ?? 0),
            averageRating = rated.Count > 0 ? Math.Round(rated.Average(b => b.MyRating), 2) : (decimal?)null,
            books = read.Select(b => new { b.Title, b.Author, finished = b.DateRead })
        };
    }

    /// <summary>Elenco di libri in un certo stato di lettura.</summary>
    [Description("Elenco di libri per stato: ToRead (da leggere), Reading (in lettura) o Read (letti).")]
    public async Task<object> GetBooksByStatus(
        [Description("Lo stato: ToRead, Reading oppure Read")] string status,
        [Description("Quanti libri al massimo restituire (da 1 a 50)")] int max = 15)
    {
        // Claude potrebbe scrivere lo stato in modo imperfetto: se non lo riconosciamo lo diciamo.
        if (!Enum.TryParse<ReadingStatus>(status, ignoreCase: true, out var parsed))
            return new { error = "Stato non valido. Valori ammessi: ToRead, Reading, Read." };

        // Limitiamo il numero di risultati: meno dati inviati = meno token = meno costo.
        max = Math.Clamp(max, 1, 50);
        var total = await db.Books.CountAsync(b => b.Status == parsed);
        var books = await db.Books
            .Where(b => b.Status == parsed)
            .OrderBy(b => b.Title)
            .Take(max)
            .Select(b => new { b.Title, b.Author, b.Pages })
            .ToListAsync();
        return new { status = parsed.ToString(), total, shown = books.Count, books };
    }
}
