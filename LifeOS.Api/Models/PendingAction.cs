// Il namespace corrisponde alla cartella LifeOS.Api/Models.
namespace LifeOS.Api.Models;

/// <summary>Che cosa farà la modifica proposta, quando l'utente la conferma.</summary>
public enum PendingActionType
{
    /// <summary>Crea o aggiorna la challenge a numero di un anno (quanti libri leggere).</summary>
    SetChallengeGoal,
    /// <summary>Crea la challenge alfabeto di un anno.</summary>
    CreateAlphabetChallenge,
    /// <summary>Crea una sfida competitiva contro un'altra persona (la crea se non esiste).</summary>
    CreateCompetitiveChallenge,
    /// <summary>Aggiunge un libro con punteggio alla lista di una sfida competitiva.</summary>
    AddChallengeItem,
    /// <summary>Registra un libro iniziato o finito da un'altra persona (es. "Fra ha finito X").</summary>
    AddReaderBook,
    /// <summary>Registra un libro iniziato o finito dall'utente (es. "ho iniziato X il 30 ottobre").</summary>
    AddMyBook
}

/// <summary>A che punto è una proposta.</summary>
public enum PendingActionStatus
{
    /// <summary>Proposta dall'agente, in attesa che l'utente decida.</summary>
    Pending,
    /// <summary>Confermata dall'utente: la modifica è stata eseguita.</summary>
    Confirmed,
    /// <summary>Rifiutata dall'utente: non è stato modificato nulla.</summary>
    Rejected
}

/// <summary>
/// Una modifica ai dati PROPOSTA dall'agente e non ancora eseguita (CLAUDE.md §12.1 regola 2).
/// Gli strumenti dell'agente creano solo questo record; la modifica vera avviene soltanto
/// quando l'utente preme "Conferma" (vedi PendingActionService).
/// </summary>
public class PendingAction
{
    /// <summary>Identificativo unico interno, assegnato automaticamente dal database.</summary>
    public int Id { get; set; }

    /// <summary>Tipo di modifica proposta.</summary>
    public PendingActionType Type { get; set; }

    /// <summary>I dati della modifica in formato JSON (dipendono dal tipo).</summary>
    public required string PayloadJson { get; set; }

    /// <summary>Frase in italiano mostrata all'utente accanto ai pulsanti. La scrive il codice, non il modello.</summary>
    public required string Summary { get; set; }

    /// <summary>Stato della proposta.</summary>
    public PendingActionStatus Status { get; set; } = PendingActionStatus.Pending;

    /// <summary>Quando è stata proposta.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Quando l'utente ha confermato o rifiutato (vuoto finché è in attesa).</summary>
    public DateTime? ResolvedAt { get; set; }
}
