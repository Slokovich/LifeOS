// Il namespace raggruppa le classi; corrisponde alla cartella LifeOS.Api/Models.
namespace LifeOS.Api.Models;

/// <summary>
/// Amica/amico, usato dall'agente Sociale per suggerire uscite
/// (es. "non vedi Giulia da 40 giorni"). Ogni oggetto = una riga della tabella Friends.
/// </summary>
public class Friend
{
    /// <summary>Identificativo unico, assegnato automaticamente dal database.</summary>
    public int Id { get; set; }

    /// <summary>Nome (obbligatorio, max 100 caratteri).</summary>
    public required string Name { get; set; }

    /// <summary>Ultima volta che vi siete visti, se nota.</summary>
    public DateOnly? LastSeen { get; set; }

    /// <summary>Ogni quanti giorni vorresti vederla/o (null = nessun obiettivo).</summary>
    public int? DesiredFrequencyDays { get; set; }

    /// <summary>Attività preferite, testo libero (es. "cinema, brunch").</summary>
    public string? Interests { get; set; }

    /// <summary>Note libere facoltative.</summary>
    public string? Notes { get; set; }
}
