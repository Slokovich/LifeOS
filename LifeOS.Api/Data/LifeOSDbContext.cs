using LifeOS.Api.Models;              // i modelli (MealLog, Book, ...)
using Microsoft.EntityFrameworkCore;  // DbContext, DbSet, ModelBuilder

namespace LifeOS.Api.Data;

/// <summary>
/// Il "ponte" tra il codice C# e il database SQLite (Entity Framework Core).
/// Ogni DbSet qui sotto corrisponde a una tabella; leggere o aggiungere oggetti
/// a un DbSet e chiamare SaveChanges() legge o scrive righe nel database.
/// Le opzioni (quale DB usare) arrivano da Program.cs tramite il costruttore.
/// </summary>
public class LifeOSDbContext(DbContextOptions<LifeOSDbContext> options) : DbContext(options)
{
    // ─── Tabelle ─────────────────────────────────────────────────────────────
    // "=> Set<T>()" restituisce la tabella corrispondente al modello T.
    public DbSet<MealLog> MealLogs => Set<MealLog>();                 // pasti (agente Cibo)
    public DbSet<MenstrualCycle> MenstrualCycles => Set<MenstrualCycle>(); // cicli (agente Sport)
    public DbSet<Book> Books => Set<Book>();                          // libri (agente Libri)
    public DbSet<Friend> Friends => Set<Friend>();                    // amici (agente Sociale)
    public DbSet<Course> Courses => Set<Course>();                    // corsi (agente Apprendimento)
    public DbSet<StudySession> StudySessions => Set<StudySession>();  // sessioni di studio

    /// <summary>
    /// Configurazione dettagliata delle tabelle: lunghezze massime, indici, relazioni,
    /// conversioni. EF Core la usa per generare le migrazioni (cioè gli script che creano le tabelle).
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MealLog>(e =>
        {
            e.Property(m => m.Description).HasMaxLength(500);  // max 500 caratteri
            e.Property(m => m.Type).HasConversion<string>();    // enum salvato come testo ("Lunch")
            e.HasIndex(m => m.EatenAt);                         // indice: ricerche per data più veloci
        });

        modelBuilder.Entity<MenstrualCycle>(e =>
        {
            e.HasIndex(c => c.StartDate).IsUnique();  // non possono esistere due cicli con lo stesso inizio
        });

        modelBuilder.Entity<Book>(e =>
        {
            e.Property(b => b.Title).HasMaxLength(500);
            e.Property(b => b.Author).HasMaxLength(200);
            e.Property(b => b.Status).HasConversion<string>();  // enum salvato come testo ("Read")
            e.HasIndex(b => b.GoodreadsId).IsUnique();          // niente duplicati reimportando il CSV
            e.HasIndex(b => b.DateRead);                        // conteggio veloce dei libri letti per anno
        });

        modelBuilder.Entity<Friend>(e =>
        {
            e.Property(f => f.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Course>(e =>
        {
            e.Property(c => c.Title).HasMaxLength(300);
            e.Property(c => c.Topic).HasMaxLength(100);
            e.Property(c => c.Platform).HasMaxLength(100);

            // Relazione uno-a-molti: un corso ha molte sessioni, ogni sessione ha un solo corso,
            // collegati da StudySession.CourseId. Cascade = eliminando il corso si eliminano le sue sessioni.
            e.HasMany(c => c.Sessions)
             .WithOne(s => s.Course)
             .HasForeignKey(s => s.CourseId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StudySession>(e =>
        {
            e.HasIndex(s => s.ScheduledStart);  // ricerche veloci per data (es. "cosa studio oggi?")
        });
    }
}
