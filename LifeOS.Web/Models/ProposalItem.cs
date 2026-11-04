// Il namespace corrisponde alla cartella LifeOS.Web/Models.
namespace LifeOS.Web.Models;

/// <summary>
/// Una modifica proposta dall'agente, in attesa di Conferma/Rifiuta.
/// Copia di LifeOS.Api.Dtos.ProposalDto (vedi CLAUDE.md §9 punto 8) più lo stato mostrato nella chat.
/// </summary>
public class ProposalItem
{
    /// <summary>Identificativo della proposta, usato negli endpoint /api/pending/{id}/...</summary>
    public int Id { get; set; }

    /// <summary>La frase da mostrare, per esempio "Imposta a 60 libri l'obiettivo ... 2026".</summary>
    public string Summary { get; set; } = "";

    /// <summary>Solo frontend: "pending" (in attesa), "confirmed" o "rejected".</summary>
    public string State { get; set; } = "pending";
}
