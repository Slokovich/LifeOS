using System.Text.Json;
using LifeOS.Api.Data;
using LifeOS.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Api.Services;

/// <summary>
/// Gestisce le proposte di modifica (<see cref="PendingAction"/>): le crea (usato dagli strumenti
/// dell'agente) e le esegue o rifiuta (usato dagli endpoint, cioè dai pulsanti dell'utente).
/// È l'unico punto in cui una modifica proposta dall'agente diventa reale: così la regola
/// "prima conferma, poi scrivi" vale per il codice e non dipende dal modello.
/// </summary>
public class PendingActionService(LifeOSDbContext db)
{
    /// <summary>Dati della modifica SetChallengeGoal (salvati come JSON nella proposta).</summary>
    public record SetGoalPayload(int Year, int TargetBooks);

    /// <summary>Dati della modifica CreateAlphabetChallenge.</summary>
    public record AlphabetPayload(int Year);

    /// <summary>Dati della modifica CreateCompetitiveChallenge.</summary>
    public record CompetitivePayload(int Year, string OpponentName);

    /// <summary>Dati della modifica AddChallengeItem.</summary>
    public record ItemPayload(int ChallengeId, string Title, string? Author, decimal Points, bool AnyBookByAuthor = false);

    /// <summary>
    /// Dati delle modifiche AddReaderBook (ReaderName = la persona) e AddMyBook (ReaderName vuoto = l'utente).
    /// Finished = false significa "iniziato, ancora in lettura". DateStarted e Finished hanno un valore
    /// predefinito perché le proposte salvate prima del 2026-11-04 non li contengono (erano tutte "finito").
    /// </summary>
    public record ReaderBookPayload(string? ReaderName, string Title, string Author, int? Pages, DateOnly? DateRead,
        DateOnly? DateStarted = null, bool Finished = true);

    // ─── 1. CREAZIONE DELLE PROPOSTE (non modificano nulla) ───

    /// <summary>Propone di creare/aggiornare la challenge a numero di un anno.</summary>
    public Task<PendingAction> ProposeSetGoalAsync(int year, int targetBooks) =>
        AddAsync(PendingActionType.SetChallengeGoal, new SetGoalPayload(year, targetBooks),
            $"Imposta a {targetBooks} libri la challenge a numero del {year}");

    /// <summary>Propone di creare la challenge alfabeto di un anno.</summary>
    public Task<PendingAction> ProposeAlphabetAsync(int year) =>
        AddAsync(PendingActionType.CreateAlphabetChallenge, new AlphabetPayload(year),
            $"Crea la challenge alfabeto del {year} (un autore per ogni lettera A-Z, iniziale del cognome)");

    /// <summary>Propone di creare una sfida competitiva contro un'altra persona.</summary>
    public Task<PendingAction> ProposeCompetitiveAsync(int year, string opponentName) =>
        AddAsync(PendingActionType.CreateCompetitiveChallenge, new CompetitivePayload(year, opponentName),
            $"Crea la sfida {year} contro {opponentName} (lista fissa di libri, punti per libro)");

    /// <summary>Propone di aggiungere un libro alla lista di una sfida competitiva.</summary>
    public Task<PendingAction> ProposeItemAsync(CompetitiveChallenge challenge, string title, string? author, decimal points, bool anyBookByAuthor = false) =>
        AddAsync(PendingActionType.AddChallengeItem, new ItemPayload(challenge.Id, title, author, points, anyBookByAuthor),
            anyBookByAuthor
                ? $"Aggiungi \"un libro qualsiasi di {author}\" ({points:0.##} punti) alla lista di \"{challenge.Name}\""
                : $"Aggiungi \"{title}\"{(string.IsNullOrWhiteSpace(author) ? "" : $" di {author}")} ({points:0.##} punti) alla lista di \"{challenge.Name}\"");

    /// <summary>
    /// Propone di registrare una lettura iniziata o finita, dell'utente (readerName vuoto) o di
    /// un'altra persona. La frase mostrata cambia a seconda dei casi.
    /// </summary>
    public Task<PendingAction> ProposeReadingAsync(string? readerName, string title, string author, int? pages,
        DateOnly? dateStarted, DateOnly? dateRead, bool finished)
    {
        var mine = string.IsNullOrWhiteSpace(readerName);
        var who = mine ? "hai" : $"{readerName} ha";
        var summary = finished
            ? $"Segna che {who} finito \"{title}\" di {author}" +
              (dateRead is { } d ? $" il {d:dd/MM/yyyy}" : " (senza data: conta per quest'anno)") +
              (dateStarted is { } s ? $", iniziato il {s:dd/MM/yyyy}" : "")
            : $"Segna che {who} iniziato \"{title}\" di {author}" +
              (dateStarted is { } s2 ? $" il {s2:dd/MM/yyyy}" : "") + " (in lettura)";
        return AddAsync(mine ? PendingActionType.AddMyBook : PendingActionType.AddReaderBook,
            new ReaderBookPayload(mine ? null : readerName, title, author, pages, dateRead, dateStarted, finished), summary);
    }

    /// <summary>Salva una proposta in attesa. La frase mostrata all'utente la scrive il codice.</summary>
    private async Task<PendingAction> AddAsync(PendingActionType type, object payload, string summary)
    {
        var proposal = new PendingAction
        {
            Type = type,
            PayloadJson = JsonSerializer.Serialize(payload),
            Summary = summary
        };
        db.PendingActions.Add(proposal);
        await db.SaveChangesAsync();
        return proposal;
    }

    // ─── 2. CONFERMA E RIFIUTO (chiamati dai pulsanti) ───

    /// <summary>
    /// Esegue la modifica proposta. Found=false se la proposta non esiste; Error valorizzato se non è
    /// più in attesa o non si può eseguire; Error vuoto = eseguita.
    /// </summary>
    public async Task<(bool Found, string? Error)> ConfirmAsync(int id)
    {
        var action = await db.PendingActions.FindAsync(id);
        if (action is null) return (false, null);
        if (AlreadyResolved(action) is string resolved) return (true, resolved);

        switch (action.Type)
        {
            case PendingActionType.SetChallengeGoal:
                var goal = JsonSerializer.Deserialize<SetGoalPayload>(action.PayloadJson)!;
                // Una sola challenge a numero per anno: se c'è la aggiorniamo, altrimenti la creiamo.
                var count = await db.Challenges.OfType<CountChallenge>().FirstOrDefaultAsync(c => c.Year == goal.Year);
                if (count is null)
                    db.Challenges.Add(new CountChallenge { Year = goal.Year, TargetBooks = goal.TargetBooks, Name = $"{goal.TargetBooks} libri nel {goal.Year}" });
                else
                {
                    count.TargetBooks = goal.TargetBooks;
                    count.Name = $"{goal.TargetBooks} libri nel {goal.Year}";
                }
                break;

            case PendingActionType.CreateAlphabetChallenge:
                var alpha = JsonSerializer.Deserialize<AlphabetPayload>(action.PayloadJson)!;
                // Una sola challenge alfabeto per anno: due uguali non avrebbero senso.
                if (await db.Challenges.OfType<AlphabetChallenge>().AnyAsync(c => c.Year == alpha.Year))
                    return (true, $"Esiste già una challenge alfabeto per il {alpha.Year}.");
                db.Challenges.Add(new AlphabetChallenge { Year = alpha.Year, Name = $"Alfabeto {alpha.Year}" });
                break;

            case PendingActionType.CreateCompetitiveChallenge:
                var comp = JsonSerializer.Deserialize<CompetitivePayload>(action.PayloadJson)!;
                var opponent = await GetOrCreateReaderAsync(comp.OpponentName);
                if (opponent.Id != 0 && await db.Challenges.OfType<CompetitiveChallenge>()
                        .AnyAsync(c => c.Year == comp.Year && c.OpponentId == opponent.Id))
                    return (true, $"Esiste già una sfida {comp.Year} contro {opponent.Name}.");
                db.Challenges.Add(new CompetitiveChallenge { Year = comp.Year, Opponent = opponent, Name = $"Sfida {comp.Year} contro {opponent.Name}" });
                break;

            case PendingActionType.AddChallengeItem:
                var item = JsonSerializer.Deserialize<ItemPayload>(action.PayloadJson)!;
                var target = await db.Challenges.OfType<CompetitiveChallenge>().Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.Id == item.ChallengeId);
                if (target is null) return (true, "La sfida non esiste più.");
                if (target.Items.Any(i => BookMatching.Matches(i.Title, i.Author, new ReaderBook { Title = item.Title, Author = item.Author ?? "" })))
                    return (true, $"\"{item.Title}\" è già nella lista.");
                target.Items.Add(new ChallengeListItem
                {
                    Title = item.Title, Author = item.Author, Points = item.Points, AnyBookByAuthor = item.AnyBookByAuthor
                });
                break;

            case PendingActionType.AddReaderBook:
                var rb = JsonSerializer.Deserialize<ReaderBookPayload>(action.PayloadJson)!;
                var reader = await GetOrCreateReaderAsync(rb.ReaderName!);
                // Libri già registrati di questa persona (una persona nuova non ne ha).
                List<ReaderBook> theirs = reader.Id == 0 ? [] : await db.ReaderBooks.Where(b => b.ReaderId == reader.Id).ToListAsync();
                var (theirTarget, theirError) = FindReadingToUpdate(theirs, rb);
                if (theirError is not null) return (true, theirError);
                if (theirTarget is not null) ApplyReading(theirTarget, rb);
                else
                {
                    var newBook = new ReaderBook
                    {
                        Reader = reader, Title = rb.Title, Author = rb.Author,
                        DateAdded = DateOnly.FromDateTime(DateTime.Today) // finito senza data: conta per quest'anno
                    };
                    ApplyReading(newBook, rb);
                    db.ReaderBooks.Add(newBook);
                }
                break;

            case PendingActionType.AddMyBook:
                var mb = JsonSerializer.Deserialize<ReaderBookPayload>(action.PayloadJson)!;
                // Cerchiamo il libro nella tua libreria (es. era tra i "da leggere" importati da Goodreads).
                var (myTarget, myError) = FindReadingToUpdate(await db.Books.ToListAsync(), mb);
                if (myError is not null) return (true, myError);
                if (myTarget is not null) ApplyReading(myTarget, mb);
                else
                {
                    var newBook = new Book
                    {
                        Title = mb.Title, Author = mb.Author,
                        DateAdded = DateOnly.FromDateTime(DateTime.Today)
                    };
                    ApplyReading(newBook, mb);
                    db.Books.Add(newBook);
                }
                break;

            default:
                return (true, "Tipo di proposta non gestito.");
        }

        action.Status = PendingActionStatus.Confirmed;
        action.ResolvedAt = DateTime.Now;
        await db.SaveChangesAsync(); // un solo salvataggio: modifica e stato della proposta insieme
        return (true, null);
    }

    /// <summary>Rifiuta la proposta senza modificare nulla. Stessi valori di ritorno di ConfirmAsync.</summary>
    public async Task<(bool Found, string? Error)> RejectAsync(int id)
    {
        var action = await db.PendingActions.FindAsync(id);
        if (action is null) return (false, null);
        if (AlreadyResolved(action) is string resolved) return (true, resolved);

        action.Status = PendingActionStatus.Rejected;
        action.ResolvedAt = DateTime.Now;
        await db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>
    /// Trova la persona per nome (senza distinguere maiuscole) o ne prepara una nuova, che viene
    /// salvata insieme alla modifica. Id = 0 significa "nuova, non ancora nel database".
    /// </summary>
    public async Task<Reader> GetOrCreateReaderAsync(string name)
    {
        var clean = name.Trim();
        var existing = await db.Readers.FirstOrDefaultAsync(r => r.Name.ToLower() == clean.ToLower());
        if (existing is not null) return existing;
        var reader = new Reader { Name = clean };
        db.Readers.Add(reader);
        return reader;
    }

    /// <summary>
    /// Decide se una lettura registrata dalla chat aggiorna un libro già presente o ne crea uno nuovo:
    /// - stesso libro "da leggere" o "in lettura" → si aggiorna quello (es. iniziato il 30/10, poi finito);
    /// - stesso libro già letto SENZA data e ora finito → si aggiorna (aggiunge le date);
    /// - stesso libro già letto con la STESSA data di fine → errore, sarebbe un doppione;
    /// - altrimenti (es. una rilettura) → Target null, si crea un libro nuovo.
    /// </summary>
    private static (IReadingRecord? Target, string? Error) FindReadingToUpdate(IEnumerable<IReadingRecord> records, ReaderBookPayload p)
    {
        var same = records.Where(r => BookMatching.Matches(p.Title, p.Author, r)).ToList();
        if (same.FirstOrDefault(r => r.Status != ReadingStatus.Read) is { } open) return (open, null);
        if (!p.Finished) return (null, null); // rilettura appena iniziata
        if (same.Any(r => r.DateRead == p.DateRead && p.DateRead is not null))
            return (null, $"\"{p.Title}\" risulta già finito il {p.DateRead:dd/MM/yyyy}.");
        if (same.FirstOrDefault(r => r.DateRead is null) is { } undated)
            return p.DateRead is null && p.DateStarted is null
                ? (null, $"\"{p.Title}\" risulta già letto.")
                : (undated, null);
        return (null, null);
    }

    /// <summary>Scrive stato, date e pagine della lettura; le date non indicate restano quelle che c'erano.</summary>
    private static void ApplyReading(IReadingRecord record, ReaderBookPayload p)
    {
        record.Status = p.Finished ? ReadingStatus.Read : ReadingStatus.Reading;
        record.DateStarted = p.DateStarted ?? record.DateStarted;
        if (p.Finished) record.DateRead = p.DateRead ?? record.DateRead;
        record.Pages ??= p.Pages;
    }

    /// <summary>Messaggio di errore se la proposta è già stata decisa, altrimenti null.</summary>
    private static string? AlreadyResolved(PendingAction action) => action.Status switch
    {
        PendingActionStatus.Confirmed => "La proposta è già stata confermata.",
        PendingActionStatus.Rejected => "La proposta è già stata rifiutata.",
        _ => null
    };
}
