// Il namespace raggruppa le classi; corrisponde alla cartella LifeOS.Api/Models.
namespace LifeOS.Api.Models;

/// <summary>
/// Un ciclo mestruale, usato dall'agente Sport per adattare gli allenamenti alla fase del ciclo.
/// Ogni oggetto = un ciclo = una riga della tabella MenstrualCycles.
/// </summary>
public class MenstrualCycle
{
    /// <summary>Identificativo unico, assegnato automaticamente dal database.</summary>
    public int Id { get; set; }

    /// <summary>Primo giorno del ciclo. Non possono esistere due cicli con la stessa data (indice unico).</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Ultimo giorno della mestruazione (null se in corso).</summary>
    public DateOnly? PeriodEndDate { get; set; }

    /// <summary>Durata totale del ciclo in giorni (null finché non inizia il successivo).</summary>
    public int? CycleLengthDays { get; set; }

    /// <summary>Note libere facoltative (es. sintomi, energia).</summary>
    public string? Notes { get; set; }
}
