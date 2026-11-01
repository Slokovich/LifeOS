// Il namespace raggruppa le classi; corrisponde alla cartella LifeOS.Api/Models.
namespace LifeOS.Api.Models;

/// <summary>Tipo di pasto. Nel database viene salvato come testo (es. "Lunch").</summary>
public enum MealType
{
    Breakfast,  // colazione
    Lunch,      // pranzo
    Dinner,     // cena
    Snack       // spuntino
}

/// <summary>
/// Pasto consumato, usato dall'agente Cibo per suggerire cosa mangiare
/// (es. evitare di ripetere lo stesso piatto). Ogni oggetto = una riga della tabella MealLogs.
/// </summary>
public class MealLog
{
    /// <summary>Identificativo unico, assegnato automaticamente dal database.</summary>
    public int Id { get; set; }

    /// <summary>Data e ora in cui è stato consumato il pasto.</summary>
    public DateTime EatenAt { get; set; }

    /// <summary>Colazione, pranzo, cena o spuntino.</summary>
    public MealType Type { get; set; }

    /// <summary>Cosa si è mangiato (obbligatorio, max 500 caratteri).</summary>
    public required string Description { get; set; }

    /// <summary>Calorie stimate; facoltativo ("?" = può essere vuoto).</summary>
    public int? Calories { get; set; }

    /// <summary>Note libere facoltative (es. "mangiato fuori").</summary>
    public string? Notes { get; set; }
}
