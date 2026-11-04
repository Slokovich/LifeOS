// Il namespace corrisponde alla cartella LifeOS.Api/Models.
namespace LifeOS.Api.Models;

/// <summary>
/// Il risultato del calcolo di una challenge (non è una tabella: si ricalcola ogni volta).
/// </summary>
/// <param name="Id">Id della challenge.</param>
/// <param name="Kind">Tipo: "Count" (a numero), "Alphabet" (alfabeto) o "Competitive" (sfida con un'altra persona).</param>
/// <param name="Name">Nome della challenge.</param>
/// <param name="IsReached">Vero se l'obiettivo è raggiunto.</param>
/// <param name="Details">Numeri specifici del tipo (letti, mancanti, lettere coperte...), inviati all'agente.</param>
public record ChallengeProgress(int Id, string Kind, string Name, bool IsReached, object Details);
