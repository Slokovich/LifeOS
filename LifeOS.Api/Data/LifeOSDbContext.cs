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
    public DbSet<Challenge> Challenges => Set<Challenge>(); // challenge di lettura, tutti i tipi (agente Libri)
    public DbSet<ChallengeListItem> ChallengeListItems => Set<ChallengeListItem>(); // lista libri delle sfide competitive
    public DbSet<Reader> Readers => Set<Reader>();             // altre persone che leggono (es. Fra)
    public DbSet<ReaderBook> ReaderBooks => Set<ReaderBook>(); // i loro libri
    public DbSet<PendingAction> PendingActions => Set<PendingAction>(); // modifiche proposte dagli agenti, in attesa di conferma
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
            e.Property(b => b.AuthorLastFirst).HasMaxLength(200);
            e.Property(b => b.Status).HasConversion<string>();  // enum salvato come testo ("Read")
            e.HasIndex(b => b.GoodreadsId).IsUnique();          // niente duplicati reimportando il CSV
            e.HasIndex(b => b.DateRead);                        // conteggio veloce dei libri letti per anno
        });

        // Challenge: una sola tabella per tutte le sottoclassi (TPH). La colonna "Kind" dice
        // il tipo di ogni riga; i campi di una sottoclasse (es. TargetBooks) restano vuoti per le altre.
        modelBuilder.Entity<Challenge>(e =>
        {
            e.HasDiscriminator<string>("Kind")
             .HasValue<CountChallenge>("Count")
             .HasValue<AlphabetChallenge>("Alphabet")
             .HasValue<CompetitiveChallenge>("Competitive");
            e.Property("Kind").HasMaxLength(20);
            e.Property(c => c.Name).HasMaxLength(200);
            e.HasIndex(c => c.Year);  // ricerca veloce delle challenge di un anno
        });

        // Sfida competitiva: l'avversaria (Restrict = non si può eliminare una persona che ha
        // sfide attive) e la lista dei libri (Cascade = eliminando la sfida si elimina la sua lista).
        modelBuilder.Entity<CompetitiveChallenge>(e =>
        {
            e.HasOne(c => c.Opponent).WithMany().HasForeignKey(c => c.OpponentId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(c => c.Items).WithOne().HasForeignKey(i => i.ChallengeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChallengeListItem>(e =>
        {
            e.Property(i => i.Title).HasMaxLength(500);
            e.Property(i => i.Author).HasMaxLength(200);
        });

        modelBuilder.Entity<Reader>(e =>
        {
            e.Property(r => r.Name).HasMaxLength(100);
            e.HasIndex(r => r.Name).IsUnique();  // "Fra" una volta sola
            e.HasMany(r => r.Books).WithOne(b => b.Reader).HasForeignKey(b => b.ReaderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReaderBook>(e =>
        {
            e.Property(b => b.Title).HasMaxLength(500);
            e.Property(b => b.Author).HasMaxLength(200);
            e.Property(b => b.AuthorLastFirst).HasMaxLength(200);
            e.Property(b => b.Status).HasConversion<string>();
            e.HasIndex(b => new { b.ReaderId, b.GoodreadsId }).IsUnique(); // niente duplicati reimportando il CSV di Fra
            e.HasIndex(b => b.DateRead);
        });

        modelBuilder.Entity<PendingAction>(e =>
        {
            e.Property(p => p.Type).HasConversion<string>();    // enum salvato come testo
            e.Property(p => p.Status).HasConversion<string>();
            e.Property(p => p.Summary).HasMaxLength(500);
            e.HasIndex(p => p.Status);                          // ricerca veloce delle proposte in attesa
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
