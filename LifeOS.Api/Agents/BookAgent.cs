using LifeOS.Api.Data;          // LifeOSDbContext (il database)
using LifeOS.Api.Dtos;          // ChatMessageDto, AgentReply, ProposalDto
using LifeOS.Api.Services;      // PendingActionService
using Microsoft.Agents.AI;       // ChatClientAgent: l'agente vero e proprio
using Microsoft.Extensions.AI;   // IChatClient, AIFunctionFactory

namespace LifeOS.Api.Agents;

/// <summary>
/// Agente Libri: risponde a domande sui libri e sulla reading challenge usando gli strumenti
/// di <see cref="BookTools"/>. Riceve da Program.cs il collegamento a Claude (IChatClient) e il database.
/// </summary>
public class BookAgent(IChatClient chat, LifeOSDbContext db, PendingActionService pending)
{
    /// <summary>
    /// Invia all'agente la conversazione (ultimo messaggio = la nuova domanda) e restituisce la risposta.
    /// L'agente non ricorda nulla tra una chiamata e l'altra: la memoria è la lista di messaggi che riceve.
    /// </summary>
    public async Task<AgentReply> AskAsync(IEnumerable<ChatMessageDto> conversation)
    {
        // Gli strumenti che l'agente può usare. AIFunctionFactory.Create trasforma un metodo C#
        // in uno "strumento" descritto a Claude (nome, parametri e [Description]).
        var tools = new BookTools(db, pending);

        // Istruzioni di sistema: chi è l'agente e come deve comportarsi. La data di oggi serve
        // perché Claude non la conosce e "quest'anno" altrimenti non avrebbe significato.
        var instructions = $"""
            Sei l'agente Libri di LifeOS, l'assistente personale di Elisa per la lettura.
            Oggi è il {DateOnly.FromDateTime(DateTime.Today):yyyy-MM-dd}.
            Rispondi sempre in italiano, in modo breve e chiaro.
            Per rispondere usa SEMPRE gli strumenti: non inventare titoli, numeri o date.
            Challenge disponibili: "a numero" (N libri nell'anno), "alfabeto" (un autore per ogni
            lettera A-Z, conta l'iniziale del COGNOME) e "competitiva" (contro un'altra persona,
            per esempio Fra: lista fissa di libri, ognuno con i suoi punti; vince chi ha più punti a
            fine anno). Per le domande sulle challenge usa GetChallengeProgress (dà anche gli id);
            per l'alfabeto GetBooksForMissingLetters; per recuperare una sfida competitiva
            GetCatchUpOptions; per le statistiche di un'altra persona GetReaderYearSummary
            (confrontala con GetYearSummary dell'utente).
            Per modificare i dati usa gli strumenti Propose... (challenge a numero, alfabeto,
            competitiva, libro nella lista della sfida; ProposeReading per un libro iniziato o
            finito, dall'utente oppure da un'altra persona, con date di inizio e/o fine):
            NON salvano, creano una proposta e all'utente compaiono i pulsanti Conferma/Rifiuta.
            Dopo averla creata di' che hai proposto la modifica e che deve confermarla; non dire
            mai che è già stata salvata.
            CONSIGLI DI LETTURA: prima chiama GetTasteProfile (gusti dai voti). Poi consiglia
            PRIMA dalla pila dei "da leggere" con GetToReadCandidates (filtra per pagine se chiede
            qualcosa di breve). Libri nuovi, che non possiede, solo se li chiede o se la pila non
            basta: in quel caso controllali con CheckBooksInLibrary e scarta quelli già letti o
            abbandonati; di' chiaramente che sono fuori dalla sua libreria. Evita libri simili a
            quelli abbandonati o bocciati e autori che non le piacciono. Per ogni consiglio spiega
            in una riga il perché, legato ai suoi gusti; generi e temi sono tue deduzioni.
            Se dice che ha abbandonato un libro, usa ProposeReading con abandoned = true.
            Se i dati non bastano per altre domande, dillo senza inventare.
            Dopo aver usato gli strumenti scrivi SEMPRE la risposta finale per l'utente.
            """;

        var agent = new ChatClientAgent(
            chat,
            instructions: instructions,
            name: "Libri",
            tools:
            [
                AIFunctionFactory.Create(tools.GetYearSummary),
                AIFunctionFactory.Create(tools.GetBooksByStatus),
                AIFunctionFactory.Create(tools.GetChallengeProgress),
                AIFunctionFactory.Create(tools.GetBooksForMissingLetters),
                AIFunctionFactory.Create(tools.ProposeChallengeGoal),
                AIFunctionFactory.Create(tools.ProposeAlphabetChallenge),
                AIFunctionFactory.Create(tools.GetReaderYearSummary),
                AIFunctionFactory.Create(tools.GetCatchUpOptions),
                AIFunctionFactory.Create(tools.ProposeCompetitiveChallenge),
                AIFunctionFactory.Create(tools.ProposeChallengeItem),
                AIFunctionFactory.Create(tools.ProposeReading),
                AIFunctionFactory.Create(tools.GetTasteProfile),
                AIFunctionFactory.Create(tools.GetToReadCandidates),
                AIFunctionFactory.Create(tools.CheckBooksInLibrary)
            ]);

        // Trasformiamo i nostri messaggi in quelli che capisce l'agente (ruolo + testo).
        var messages = conversation.Select(m =>
            new ChatMessage(m.Role == "assistant" ? ChatRole.Assistant : ChatRole.User, m.Text)).ToList();

        // RunAsync invia la conversazione; l'agente chiama gli strumenti che gli servono e poi risponde.
        // PERCHÉ IL CICLO: con domande che richiedono più strumenti (es. un prospetto) il modello a volte
        // legge i dati e poi si ferma SENZA scrivere la risposta (testo vuoto, circa 4 volte su 10).
        // In quel caso riproviamo, al massimo 3 volte in tutto. Riproviamo solo se non è stata creata
        // nessuna proposta di modifica, per non duplicarle; le letture si possono ripetere senza rischi.
        var text = "";
        IList<ChatMessage> lastRun = [];
        for (var attempt = 1; attempt <= 2 && string.IsNullOrWhiteSpace(text); attempt++)
        {
            var response = await agent.RunAsync(messages);
            text = response.Text;
            lastRun = response.Messages;
            if (tools.Proposals.Count > 0) break; // proposta creata: non ripetere
        }

        // SECONDO LIVELLO: se il modello continua a fermarsi, prendiamo i dati che gli strumenti
        // hanno già letto e gli chiediamo SOLO di scrivere la risposta, senza strumenti (così non può
        // fermarsi di nuovo dopo averli chiamati). I dati sono solo letture, nessun rischio.
        if (string.IsNullOrWhiteSpace(text))
        {
            var data = string.Join("\n", lastRun
                .SelectMany(m => m.Contents.OfType<FunctionResultContent>())
                .Select(r => System.Text.Json.JsonSerializer.Serialize(r.Result)));
            if (data.Length > 0)
            {
                var followUp = new List<ChatMessage> { new(ChatRole.System, instructions) };
                followUp.AddRange(messages);
                followUp.Add(new ChatMessage(ChatRole.User,
                    "Dati già letti dagli strumenti (usa solo questi, non inventare):\n" + data +
                    "\n\nScrivi ora la risposta finale alla mia ultima domanda."));
                text = (await chat.GetResponseAsync(followUp)).Text;
            }
        }

        if (string.IsNullOrWhiteSpace(text))
            text = "Non sono riuscito a scrivere la risposta, riprova o riformula la domanda.";

        // Insieme al testo restituiamo le proposte create dagli strumenti: la chat mostrerà i pulsanti.
        return new AgentReply(
            text,
            tools.Proposals.Select(p => new ProposalDto(p.Id, p.Summary)).ToList());
    }
}
