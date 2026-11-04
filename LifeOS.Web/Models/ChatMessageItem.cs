using System.Text.Json.Serialization; // JsonIgnore

// Il namespace corrisponde alla cartella LifeOS.Web/Models.
namespace LifeOS.Web.Models;

/// <summary>
/// Un messaggio della chat così come lo vede il frontend: copia di LifeOS.Api.Dtos.ChatMessageDto
/// (vedi CLAUDE.md §9 punto 8). I nomi devono coincidere con quelli del JSON scambiato con l'API.
/// </summary>
public class ChatMessageItem
{
    /// <summary>Chi ha scritto: "user" (tu) oppure "assistant" (l'agente).</summary>
    public string Role { get; set; } = "user";

    /// <summary>Il testo del messaggio.</summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// Proposte di modifica legate a questo messaggio dell'agente (pulsanti Conferma/Rifiuta).
    /// [JsonIgnore]: non viene rimandata all'API insieme alla cronologia.
    /// </summary>
    [JsonIgnore]
    public List<ProposalItem> Proposals { get; set; } = [];
}
