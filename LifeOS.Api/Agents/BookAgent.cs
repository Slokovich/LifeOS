using LifeOS.Api.Data;          // LifeOSDbContext (il database)
using Microsoft.Agents.AI;       // ChatClientAgent: l'agente vero e proprio
using Microsoft.Extensions.AI;   // IChatClient, AIFunctionFactory

namespace LifeOS.Api.Agents;

/// <summary>
/// Agente Libri: risponde a domande sui libri e sulla reading challenge usando gli strumenti
/// di <see cref="BookTools"/>. Riceve da Program.cs il collegamento a Claude (IChatClient) e il database.
/// </summary>
public class BookAgent(IChatClient chat, LifeOSDbContext db)
{
    /// <summary>Fa una domanda all'agente e restituisce la sua risposta in testo.</summary>
    public async Task<string> AskAsync(string question)
    {
        // Gli strumenti che l'agente può usare. AIFunctionFactory.Create trasforma un metodo C#
        // in uno "strumento" descritto a Claude (nome, parametri e [Description]).
        var tools = new BookTools(db);

        // Istruzioni di sistema: chi è l'agente e come deve comportarsi. La data di oggi serve
        // perché Claude non la conosce e "quest'anno" altrimenti non avrebbe significato.
        var instructions = $"""
            Sei l'agente Libri di LifeOS, l'assistente personale di Elisa per la lettura.
            Oggi è il {DateOnly.FromDateTime(DateTime.Today):yyyy-MM-dd}.
            Rispondi sempre in italiano, in modo breve e chiaro.
            Per rispondere usa SEMPRE gli strumenti: non inventare titoli, numeri o date.
            Se i dati non bastano per rispondere (per esempio l'obiettivo annuale della challenge,
            che non è salvato), dillo e chiedi all'utente di indicarlo.
            """;

        var agent = new ChatClientAgent(
            chat,
            instructions: instructions,
            name: "Libri",
            tools: [AIFunctionFactory.Create(tools.GetYearSummary), AIFunctionFactory.Create(tools.GetBooksByStatus)]);

        // RunAsync invia la domanda; l'agente chiama gli strumenti che gli servono e poi risponde.
        var response = await agent.RunAsync(question);
        return response.Text;
    }
}
