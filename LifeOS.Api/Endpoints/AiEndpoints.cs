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
