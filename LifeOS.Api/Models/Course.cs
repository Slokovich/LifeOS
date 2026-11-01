// Il namespace raggruppa le classi; corrisponde alla cartella LifeOS.Api/Models.
namespace LifeOS.Api.Models;

/// <summary>
/// Corso da seguire, usato dall'agente Apprendimento per pianificare lo studio.
/// Ogni oggetto = una riga della tabella Courses.
/// </summary>
public class Course
{
    /// <summary>Identificativo unico, assegnato automaticamente dal database.</summary>
    public int Id { get; set; }

    /// <summary>Nome del corso (obbligatorio, max 300 caratteri).</summary>
    public required string Title { get; set; }

    /// <summary>Argomento (es. "C#", "Spagnolo"), max 100 caratteri.</summary>
    public required string Topic { get; set; }

    /// <summary>Piattaforma (es. "Udemy", "Coursera"), max 100 caratteri.</summary>
    public required string Platform { get; set; }

    /// <summary>Durata totale stimata del corso, in ore (double = numero con decimali, es. 12.5).</summary>
    public double TotalHours { get; set; }

    /// <summary>True quando il corso è finito.</summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Sessioni di studio di questo corso (relazione uno-a-molti).
    /// EF Core la riempie leggendo la tabella StudySessions; "= []" la inizializza vuota.
    /// </summary>
    public List<StudySession> Sessions { get; set; } = [];
}
