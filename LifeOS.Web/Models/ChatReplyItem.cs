// Il namespace corrisponde alla cartella LifeOS.Web/Models.
namespace LifeOS.Web.Models;

/// <summary>
/// La risposta dell'API a un messaggio di chat: il JSON { "answer": "...", "proposals": [...] }.
/// </summary>
public class ChatReplyItem
{
    /// <summary>Il testo scritto dall'agente (in formato Markdown).</summary>
    public string Answer { get; set; } = "";

    /// <summary>Le modifiche che l'agente propone in questo turno (vuoto se nessuna).</summary>
    public List<ProposalItem> Proposals { get; set; } = [];
}
