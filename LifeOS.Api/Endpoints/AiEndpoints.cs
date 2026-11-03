using LifeOS.Api.Agents;       // BookAgent
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
        // GET /api/agents/books?question=... → fa una domanda all'agente Libri.
        // È un GET (e non un POST) solo per poterlo provare dalla barra del browser; sarà
        // sostituito dall'orchestratore e da una pagina di chat. Ogni chiamata costa pochi centesimi.
        app.MapGet("/api/agents/books", async (string question, BookAgent agent) =>
        {
            if (string.IsNullOrWhiteSpace(question))
                return Results.BadRequest("Scrivi una domanda (parametro 'question').");
            try
            {
                return Results.Ok(new { answer = await agent.AskAsync(question) });
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
