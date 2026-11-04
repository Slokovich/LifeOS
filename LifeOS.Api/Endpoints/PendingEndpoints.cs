using LifeOS.Api.Services; // PendingActionService

namespace LifeOS.Api.Endpoints;

/// <summary>
/// Endpoint per confermare o rifiutare le modifiche proposte dagli agenti
/// (i pulsanti "Conferma" e "Rifiuta" della chat).
/// </summary>
public static class PendingEndpoints
{
    /// <summary>Registra gli endpoint sotto /api/pending. Viene chiamato da Program.cs.</summary>
    public static void MapPendingEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /api/pending/{id}/confirm → esegue la modifica proposta.
        app.MapPost("/api/pending/{id:int}/confirm", async (int id, PendingActionService service) =>
        {
            var (found, error) = await service.ConfirmAsync(id);
            return !found ? Results.NotFound()
                 : error is not null ? Results.Conflict(error)
                 : Results.Ok();
        });

        // POST /api/pending/{id}/reject → scarta la proposta senza modificare nulla.
        app.MapPost("/api/pending/{id:int}/reject", async (int id, PendingActionService service) =>
        {
            var (found, error) = await service.RejectAsync(id);
            return !found ? Results.NotFound()
                 : error is not null ? Results.Conflict(error)
                 : Results.Ok();
        });
    }
}
