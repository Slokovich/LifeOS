using LifeOS.Api.Agents;       // BookAgent
using LifeOS.Api.Dtos;         // ChatMessageDto
using Microsoft.Extensions.AI; // IChatClient: l'interfaccia standard per "parlare" con un modello IA

namespace LifeOS.Api.Endpoints;

/// <summary>
/// Endpoint di servizio per l'IA. Per ora contiene solo una prova di collegamento con Claude.
/// </summary>
public static class AiEndpoints
{
    /// <summary>Registra gli endpoint IA sotto /api. Viene chiamato da Program.cs.</summary>
    public static void MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/ping-ai → PROVA TEMPORANEA: manda a Claude una domanda brevissima e restituisce
        // la risposta, per verificare che chiave, modello e connessione funzionino.
        // Da eliminare quando il primo agente è operativo.
        // POST /api/agents/books → conversazione con l'agente Libri (usato dalla pagina /chat).
        // Il corpo è la lista dei messaggi finora, l'ultimo è la nuova domanda dell'utente.
        // Sarà sostituito dall'orchestratore. Ogni chiamata costa pochi centesimi.
        app.MapPost("/api/agents/books", async (List<ChatMessageDto> messages, BookAgent agent) =>
        {
            // Controlli sui dati ricevuti (il frontend non è mai "fidato").
            if (messages.Count == 0 || messages[^1].Role != "user")
                return Results.BadRequest("L'ultimo messaggio deve essere una domanda dell'utente.");
            if (messages.Any(m => m.Role is not ("user" or "assistant")))
                return Results.BadRequest("Messaggi non validi (ruolo sconosciuto).");
            // La domanda nuova dell'utente deve essere non vuota e al massimo di 4000 caratteri.
            if (string.IsNullOrWhiteSpace(messages[^1].Text) || messages[^1].Text.Length > 4000)
                return Results.BadRequest("La domanda è vuota o supera i 4000 caratteri.");

            // CRONOLOGIA: le risposte dell'agente non vanno rifiutate (le ha scritte lui, non l'utente):
            // quelle vuote (es. di quando il modello si fermava senza rispondere) si scartano, quelle
            // troppo lunghe (es. i prospetti) si accorciano a 4000 caratteri per contenere i costi.
            var history = messages
                .Where(m => !string.IsNullOrWhiteSpace(m.Text))
                .Select(m => m.Text.Length > 4000 ? new ChatMessageDto { Role = m.Role, Text = m.Text[..4000] } : m);

            // Per contenere i costi teniamo solo gli ultimi 20 messaggi, partendo sempre da uno dell'utente
            // (l'API di Claude richiede che la conversazione inizi con l'utente).
            var recent = history.TakeLast(20).SkipWhile(m => m.Role != "user").ToList();

            try
            {
                // Risposta: { answer, proposals: [{ id, summary }] }
                return Results.Ok(await agent.AskAsync(recent));
            }
            catch (Exception ex)
            {
                return Results.Problem($"{ex.GetType().Name}: {ex.Message}");
            }
        });

        app.MapGet("/api/ping-ai", async (IChatClient chat) =>
        {
            try
            {
                // Una sola domanda, con risposta cortissima, per spendere il meno possibile.
                var response = await chat.GetResponseAsync("Rispondi con una sola parola: ciao");
                return Results.Ok(new { answer = response.Text });
            }
            catch (Exception ex)
            {
                // Mostriamo solo il tipo di errore e il messaggio: la chiave non compare mai qui.
                return Results.Problem($"{ex.GetType().Name}: {ex.Message}");
            }
        });
    }
}
