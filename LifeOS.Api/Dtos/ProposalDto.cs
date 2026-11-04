// Il namespace corrisponde alla cartella LifeOS.Api/Dtos.
namespace LifeOS.Api.Dtos;

/// <summary>
/// Una proposta dell'agente come la riceve il frontend: la frase da mostrare e l'id da usare
/// per confermare o rifiutare (POST /api/pending/{id}/confirm o /reject).
/// </summary>
public record ProposalDto(int Id, string Summary);

/// <summary>La risposta di un agente: il testo e le eventuali proposte di modifica create in quel turno.</summary>
public record AgentReply(string Answer, List<ProposalDto> Proposals);
