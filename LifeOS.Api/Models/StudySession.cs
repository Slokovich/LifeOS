// Il namespace raggruppa le classi; corrisponde alla cartella LifeOS.Api/Models.
namespace LifeOS.Api.Models;

/// <summary>
/// Sessione di studio pianificata o svolta per un corso.
/// Ogni oggetto = una riga della tabella StudySessions.
/// </summary>
public class StudySession
{
    /// <summary>Identificativo unico, assegnato automaticamente dal database.</summary>
    public int Id { get; set; }

    /// <summary>Chiave esterna: l'Id del corso a cui appartiene la sessione.</summary>
    public int CourseId { get; set; }

    /// <summary>
    /// Il corso collegato ("proprietà di navigazione"). "null!" dice al compilatore:
    /// "sarà EF Core a riempirlo", così non segnala avvisi.
    /// </summary>
    public Course Course { get; set; } = null!;

    /// <summary>Data e ora di inizio previste.</summary>
    public DateTime ScheduledStart { get; set; }

    /// <summary>Minuti di studio pianificati.</summary>
    public int PlannedMinutes { get; set; }

    /// <summary>Minuti effettivamente studiati (null se non ancora svolta).</summary>
    public int? ActualMinutes { get; set; }

    /// <summary>True quando la sessione è stata svolta.</summary>
    public bool IsCompleted { get; set; }

    /// <summary>Note libere facoltative (es. capitoli fatti).</summary>
    public string? Notes { get; set; }
}
