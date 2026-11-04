using System.ComponentModel; // [Description]: il testo che spiega a Claude a cosa serve uno strumento
using LifeOS.Api.Data;        // LifeOSDbContext (il database)
using LifeOS.Api.Models;      // Book, ReadingStatus, PendingAction
using LifeOS.Api.Services;    // PendingActionService
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Api.Agents;

/// <summary>
/// Gli "strumenti" dell'agente Libri: metodi che Claude può chiamare per leggere i dati.
/// Claude non vede il database: vede solo la descrizione di ogni metodo e ciò che restituisce.
/// Di proposito leggono SOLO la tabella Books e restituiscono solo i campi necessari (CLAUDE.md §6).
/// </summary>
public class BookTools(LifeOSDbContext db, PendingActionService pending)
{
    /// <summary>Le proposte di modifica create durante questa richiesta (le legge BookAgent).</summary>
    public List<PendingAction> Proposals { get; } = [];

    /// <summary>Riepilogo dei libri finiti in un certo anno (per la reading challenge).</summary>
    [Description("Riepilogo dei libri letti (finiti) in un anno: quanti, pagine totali, voto medio e titoli.")]
    public async Task<object> GetYearSummary(
        [Description("L'anno, per esempio 2026")] int year)
    {
        // Libri con stato "Letto" e data di fine lettura dentro l'anno richiesto.
        var read = await BooksReadIn(year);

        // Voto medio calcolato qui in memoria (SQLite non sa fare la media di un decimal) e
        // solo sui libri votati: 0 significa "nessun voto" (CLAUDE.md §8).
        var rated = read.Where(b => b.MyRating > 0).ToList();
        return new
        {
            year,
            booksRead = read.Count,
            totalPages = read.Sum(b => b.Pages ?? 0),
            averageRating = rated.Count > 0 ? Math.Round(rated.Average(b => b.MyRating), 2) : (decimal?)null,
            books = read.Select(b => new { b.Title, b.Author, finished = b.DateRead })
        };
    }

    /// <summary>Stato di TUTTE le challenge di un anno, calcolato dal codice di ogni tipo di challenge.</summary>
    [Description("Stato di tutte le challenge di lettura di un anno (a numero: libri letti, mancanti, ritmo; alfabeto: lettere coperte e mancanti; competitiva: punteggio tuo e dell'avversaria libro per libro). Restituisce anche l'id di ogni challenge.")]
    public async Task<object> GetChallengeProgress(
        [Description("L'anno, per esempio 2026")] int year)
    {
        var challenges = await LoadChallenges(year);
        if (challenges.Count == 0)
            return new { error = $"Nessuna challenge salvata per il {year}. Chiedi all'utente quali vuole (a numero, alfabeto, competitiva) e proponile." };

        // Ogni challenge si calcola da sola con le sue regole (metodo Evaluate della sottoclasse).
        var context = await BuildContext(year, challenges);
        return new { year, challenges = challenges.Select(c => c.Evaluate(context)) };
    }

    /// <summary>Statistiche dell'anno di un'altra persona (es. Fra), da confrontare con GetYearSummary.</summary>
    [Description("Riepilogo dei libri letti in un anno da un'altra persona (es. Fra): quanti, pagine, voto medio, titoli. Per confrontarla con l'utente usa anche GetYearSummary.")]
    public async Task<object> GetReaderYearSummary(
        [Description("Il nome della persona, per esempio Fra")] string readerName,
        [Description("L'anno, per esempio 2026")] int year)
    {
        var reader = await FindReader(readerName);
        if (reader is null) return new { error = $"Nessuna persona chiamata {readerName}. Le persone si creano con una sfida o registrando un suo libro." };

        var read = await ReaderBooksReadIn(reader.Id, year);
        var rated = read.Where(b => b.MyRating > 0).ToList();
        // Letti in un anno sconosciuto (non contano per nessun anno): li diciamo a parte.
        var yearUnknown = await db.ReaderBooks.CountAsync(b => b.ReaderId == reader.Id && b.Status == ReadingStatus.Read
                                                               && b.DateRead == null && b.ReadYearUnknown);
        return new
        {
            reader = reader.Name,
            year,
            booksRead = read.Count,
            booksReadYearUnknownAllTime = yearUnknown,
            totalPages = read.Sum(b => b.Pages ?? 0),
            averageRating = rated.Count > 0 ? Math.Round(rated.Average(b => b.MyRating), 2) : (decimal?)null,
            books = read.Select(b => new { b.Title, b.Author, finished = b.DateRead })
        };
    }

    /// <summary>
    /// Per recuperare una sfida competitiva: libri della lista non ancora letti, ordinati per punti
    /// per pagina, e il gruppo più "economico" (meno pagine) per superare l'avversaria. Calcolo del codice.
    /// </summary>
    [Description("Per una sfida competitiva: libri della lista che l'utente non ha ancora letto con punti, pagine e punti per pagina, più una selezione (dal miglior rapporto punti/pagine) che basta per superare l'avversaria leggendo poche pagine. Serve l'id della sfida (da GetChallengeProgress).")]
    public async Task<object> GetCatchUpOptions(
        [Description("Id della sfida competitiva")] int challengeId)
    {
        var challenge = await db.Challenges.OfType<CompetitiveChallenge>()
            .Include(c => c.Items).Include(c => c.Opponent)
            .FirstOrDefaultAsync(c => c.Id == challengeId);
        if (challenge is null) return new { error = "Sfida competitiva non trovata: controlla l'id con GetChallengeProgress." };

        var context = await BuildContext(challenge.Year, [challenge]);
        var myRead = context.MyBooksRead;
        var opponentRead = context.ReaderBooksRead.GetValueOrDefault(challenge.OpponentId) ?? [];
        var myScore = challenge.Items.Where(i => myRead.Any(b => BookMatching.Matches(i, b))).Sum(i => i.Points);
        var opponentScore = challenge.Items.Where(i => opponentRead.Any(b => BookMatching.Matches(i, b))).Sum(i => i.Points);

        // Pagine: le prendiamo dalla tua libreria se il libro c'è (es. tra i "da leggere").
        var library = await db.Books.Where(b => b.Status == ReadingStatus.ToRead || b.Status == ReadingStatus.Reading).ToListAsync();
        var options = challenge.Items
            .Where(i => !myRead.Any(b => BookMatching.Matches(i, b)))
            .Select(i =>
            {
                // Il libro corrispondente nella tua libreria; per "un libro qualsiasi di X" il più breve.
                var owned = library.Where(b => BookMatching.Matches(i, b))
                    .OrderBy(b => b.Pages is > 0 ? b.Pages : int.MaxValue).FirstOrDefault();
                return new
                {
                    i.Title, i.Author, i.Points, i.AnyBookByAuthor,
                    pages = owned?.Pages,
                    inMyLibrary = owned is not null,
                    myLibraryTitle = owned?.Title,
                    pointsPer100Pages = owned?.Pages > 0 ? Math.Round(i.Points * 100m / owned.Pages!.Value, 2) : (decimal?)null
                };
            })
            .OrderByDescending(o => o.pointsPer100Pages ?? -1).ThenByDescending(o => o.Points)
            .ToList();

        // Selezione "golosa": prendiamo i libri col miglior rapporto punti/pagine finché non superi l'avversaria.
        // Non è sempre la combinazione perfetta, ma è vicina e facile da spiegare.
        var plan = new List<(string Title, string? Author, decimal Points, int Pages)>();
        var score = myScore;
        foreach (var o in options.Where(o => o.pages > 0))
        {
            if (score > opponentScore) break;
            plan.Add((o.Title, o.Author, o.Points, o.pages!.Value));
            score += o.Points;
        }
        var overtakes = score > opponentScore;

        return new
        {
            challenge = challenge.Name,
            myScore, opponentScore,
            pointsToOvertake = Math.Max(0, opponentScore - myScore + 1),
            options,
            cheapestPlanToOvertake = overtakes ? plan.Select(p => new { p.Title, p.Author, p.Points, pages = p.Pages }) : null,
            planPages = overtakes ? plan.Sum(p => p.Pages) : (int?)null,
            note = "I libri senza pagine note (non nella tua libreria) sono esclusi dal piano."
        };
    }

    /// <summary>Libri non ancora letti che coprirebbero le lettere mancanti della challenge alfabeto.</summary>
    [Description("Per la challenge alfabeto: libri 'da leggere' o 'in lettura' i cui autori coprono le lettere ancora mancanti nell'anno, dai più brevi. Usalo per consigliare cosa leggere per completare l'alfabeto.")]
    public async Task<object> GetBooksForMissingLetters(
        [Description("L'anno, per esempio 2026")] int year,
        [Description("Quanti libri al massimo per lettera (da 1 a 5)")] int perLetter = 3)
    {
        if (!await db.Challenges.OfType<AlphabetChallenge>().AnyAsync(c => c.Year == year))
            return new { error = $"Nessuna challenge alfabeto per il {year}." };

        // Lettere già coperte dai libri letti nell'anno (stessa regola della challenge).
        var coveredLetters = (await BooksReadIn(year))
            .Select(AlphabetChallenge.SurnameInitial).OfType<char>().ToHashSet();

        // Candidati: libri non ancora letti. Il filtro per lettera si fa in memoria perché
        // l'iniziale del cognome si calcola in C# (accenti, "Cognome, Nome").
        perLetter = Math.Clamp(perLetter, 1, 5);
        var candidates = await db.Books.Where(b => b.Status == ReadingStatus.ToRead || b.Status == ReadingStatus.Reading).ToListAsync();
        var byLetter = candidates
            .Select(b => (book: b, letter: AlphabetChallenge.SurnameInitial(b)))
            .Where(x => x.letter is char l && !coveredLetters.Contains(l))
            .GroupBy(x => x.letter!.Value)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                letter = g.Key.ToString(),
                books = g.OrderBy(x => x.book.Pages ?? int.MaxValue).Take(perLetter)
                         .Select(x => new { x.book.Title, x.book.Author, x.book.Pages, status = x.book.Status.ToString() })
            })
            .ToList();

        // Lettere mancanti per cui non hai nessun libro in lista: servirà un consiglio esterno.
        var withoutCandidates = AlphabetChallenge.Letters
            .Where(l => !coveredLetters.Contains(l) && byLetter.All(g => g.letter[0] != l))
            .Select(l => l.ToString());
        return new { year, missingLetters = byLetter, lettersWithNoBookInList = withoutCandidates };
    }

    /// <summary>
    /// PROPONE la challenge a numero di un anno. Non modifica nulla: crea una proposta che
    /// l'utente deve confermare con il pulsante nella chat (CLAUDE.md §12.1 regola 2).
    /// </summary>
    [Description("PROPONE la challenge a numero di un anno (quanti libri leggere); se esiste già, ne propone la modifica. Non salva nulla: all'utente comparirà un pulsante Conferma. Usalo solo se l'utente lo chiede esplicitamente.")]
    public async Task<object> ProposeChallengeGoal(
        [Description("L'anno, per esempio 2026")] int year,
        [Description("Numero di libri da leggere (da 1 a 1000)")] int targetBooks)
    {
        if (targetBooks is < 1 or > 1000)
            return new { error = "L'obiettivo deve essere tra 1 e 1000 libri." };

        var proposal = await pending.ProposeSetGoalAsync(year, targetBooks);
        Proposals.Add(proposal); // l'agente le restituirà alla chat insieme al testo della risposta
        return new { proposed = true, note = "Proposta creata, NON ancora salvata: l'utente deve premere Conferma. Non dire che è stato salvato." };
    }

    /// <summary>PROPONE la challenge alfabeto di un anno (stessa regola: serve la conferma dell'utente).</summary>
    [Description("PROPONE la challenge alfabeto di un anno: leggere un autore per ogni lettera A-Z (iniziale del cognome). Non salva nulla: all'utente comparirà un pulsante Conferma. Usalo solo se l'utente lo chiede esplicitamente.")]
    public async Task<object> ProposeAlphabetChallenge(
        [Description("L'anno, per esempio 2026")] int year)
    {
        if (await db.Challenges.OfType<AlphabetChallenge>().AnyAsync(c => c.Year == year))
            return new { error = $"Esiste già una challenge alfabeto per il {year}." };

        Proposals.Add(await pending.ProposeAlphabetAsync(year));
        return new { proposed = true, note = "Proposta creata, NON ancora salvata: l'utente deve premere Conferma. Non dire che è stato salvato." };
    }

    /// <summary>PROPONE una sfida competitiva contro un'altra persona (serve la conferma dell'utente).</summary>
    [Description("PROPONE una sfida competitiva per un anno contro un'altra persona (es. Fra): vince chi ottiene più punti leggendo libri di una lista fissa. Non salva nulla: compare un pulsante Conferma. Poi i libri della lista si aggiungono con ProposeChallengeItem.")]
    public async Task<object> ProposeCompetitiveChallenge(
        [Description("L'anno, per esempio 2026")] int year,
        [Description("Nome dell'avversaria, per esempio Fra")] string opponentName)
    {
        if (string.IsNullOrWhiteSpace(opponentName) || opponentName.Length > 100)
            return new { error = "Nome dell'avversaria mancante o troppo lungo." };
        var reader = await FindReader(opponentName);
        if (reader is not null && await db.Challenges.OfType<CompetitiveChallenge>().AnyAsync(c => c.Year == year && c.OpponentId == reader.Id))
            return new { error = $"Esiste già una sfida {year} contro {reader.Name}." };

        Proposals.Add(await pending.ProposeCompetitiveAsync(year, opponentName));
        return ProposedNote;
    }

    /// <summary>PROPONE di aggiungere un libro (con punti) alla lista di una sfida competitiva.</summary>
    [Description("PROPONE di aggiungere un libro con il suo punteggio (0,5-10, 5 se non indicato) alla lista di una sfida competitiva. Serve l'id della sfida (da GetChallengeProgress). Non salva nulla: compare un pulsante Conferma.")]
    public async Task<object> ProposeChallengeItem(
        [Description("Id della sfida competitiva")] int challengeId,
        [Description("Titolo del libro")] string title,
        [Description("Autore del libro: ricavalo tu dal titolo se l'utente non lo dice (lascia vuoto solo se non lo sai con sicurezza)")] string? author = null,
        [Description("Punti del libro, da 0.5 a 10, anche decimali (es. 6.5); se l'utente non li dice usa 5")] decimal points = 5,
        [Description("true se vale UN libro qualsiasi dell'autore (es. 'un libro di Asimov'); in quel caso l'autore è obbligatorio")] bool anyBookByAuthor = false)
    {
        if (points is < 0.5m or > 10m) return new { error = "I punti devono essere tra 0,5 e 10." };
        if (anyBookByAuthor && string.IsNullOrWhiteSpace(author)) return new { error = "Per 'un libro qualsiasi di' serve l'autore." };
        if (string.IsNullOrWhiteSpace(title) || title.Length > 500 || author?.Length > 200)
            return new { error = "Titolo mancante, o titolo/autore troppo lunghi." };
        var challenge = await db.Challenges.OfType<CompetitiveChallenge>().FirstOrDefaultAsync(c => c.Id == challengeId);
        if (challenge is null) return new { error = "Sfida competitiva non trovata: controlla l'id con GetChallengeProgress." };

        Proposals.Add(await pending.ProposeItemAsync(challenge, title.Trim(),
            string.IsNullOrWhiteSpace(author) ? null : author.Trim(), points, anyBookByAuthor));
        return ProposedNote;
    }

    /// <summary>
    /// PROPONE di registrare una lettura iniziata o finita, dell'utente o di un'altra persona
    /// (es. "ho iniziato X il 30 ottobre", "Fra ha finito Y"). Se il libro c'è già, la conferma lo aggiorna.
    /// </summary>
    [Description("PROPONE di registrare un libro INIZIATO (in lettura), FINITO o ABBANDONATO, dall'utente o da un'altra persona (es. Fra). Le date sono facoltative: se l'utente dà solo il mese usa il primo del mese; un libro finito senza data conta per l'anno in corso. Se il libro è già registrato (es. iniziato prima, o tra i 'da leggere'), alla conferma viene aggiornato invece che duplicato. Per i libri della sfida usa lo stesso titolo e autore della lista. Non salva nulla: compare un pulsante Conferma.")]
    public async Task<object> ProposeReading(
        [Description("Titolo del libro")] string title,
        [Description("Autore del libro: ricavalo tu dal titolo se l'utente non lo dice")] string author,
        [Description("true se il libro è stato finito, false se è solo iniziato (in lettura)")] bool finished,
        [Description("Nome dell'altra persona (es. Fra); lascia vuoto se è l'utente stessa a leggere")] string? readerName = null,
        [Description("Data di inizio lettura, formato yyyy-MM-dd (facoltativa)")] string? dateStarted = null,
        [Description("Data di fine lettura, formato yyyy-MM-dd (facoltativa, solo se finito)")] string? dateRead = null,
        [Description("Numero di pagine, se noto")] int? pages = null,
        [Description("true se il libro è stato ABBANDONATO (iniziato e lasciato): in quel caso finished = false e niente data di fine")] bool abandoned = false)
    {
        if (ParseDate(dateStarted, "inizio", out var started) is { } startError) return startError;
        if (ParseDate(dateRead, "fine", out var ended) is { } endError) return endError;
        if (abandoned && (finished || ended is not null))
            return new { error = "Un libro abbandonato non è finito: usa finished = false e nessuna data di fine." };
        if (!finished && ended is not null)
            return new { error = "C'è una data di fine: allora il libro è finito (finished = true)." };
        if (started is not null && ended is not null && started > ended)
            return new { error = "La data di inizio è dopo quella di fine." };
        if (readerName?.Length > 100
            || string.IsNullOrWhiteSpace(title) || title.Length > 500 || string.IsNullOrWhiteSpace(author) || author.Length > 200)
            return new { error = "Titolo o autore mancanti, o nome/titolo/autore troppo lunghi." };
        if (pages is < 1 or > 20000) pages = null;

        Proposals.Add(await pending.ProposeReadingAsync(readerName?.Trim(), title.Trim(), author.Trim(), pages,
            started, ended, finished, abandoned));
        return ProposedNote;
    }

    /// <summary>
    /// Legge una data facoltativa nel formato yyyy-MM-dd. Restituisce un errore (da girare a Claude)
    /// se è scritta male o nel futuro, altrimenti null; la data letta finisce in "date".
    /// </summary>
    private static object? ParseDate(string? text, string which, out DateOnly? date)
    {
        date = null;
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (!DateOnly.TryParseExact(text.Trim(), "yyyy-MM-dd", out var parsed))
            return new { error = $"Data di {which} non valida: usa il formato yyyy-MM-dd, oppure lasciala vuota." };
        if (parsed > DateOnly.FromDateTime(DateTime.Today))
            return new { error = $"La data di {which} lettura non può essere nel futuro." };
        date = parsed;
        return null;
    }

    /// <summary>Risposta comune degli strumenti che propongono: ricorda al modello che non è salvato.</summary>
    private static readonly object ProposedNote =
        new { proposed = true, note = "Proposta creata, NON ancora salvata: l'utente deve premere Conferma. Non dire che è stato salvato." };

    /// <summary>Le challenge di un anno, con lista e avversaria già caricate per quelle competitive.</summary>
    private Task<List<Challenge>> LoadChallenges(int year) =>
        db.Challenges
            .Include(c => ((CompetitiveChallenge)c).Items)
            .Include(c => ((CompetitiveChallenge)c).Opponent)
            .Where(c => c.Year == year).OrderBy(c => c.Id).ToListAsync();

    /// <summary>Prepara i dati per calcolare le challenge: i tuoi libri dell'anno e quelli delle avversarie coinvolte.</summary>
    private async Task<ChallengeContext> BuildContext(int year, IEnumerable<Challenge> challenges)
    {
        var readers = new Dictionary<int, IReadOnlyList<ReaderBook>>();
        foreach (var opponentId in challenges.OfType<CompetitiveChallenge>().Select(c => c.OpponentId).Distinct())
            readers[opponentId] = await ReaderBooksReadIn(opponentId, year);
        return new ChallengeContext(await BooksReadIn(year), readers, DateOnly.FromDateTime(DateTime.Today));
    }

    /// <summary>Cerca una persona per nome, senza distinguere maiuscole e minuscole.</summary>
    private Task<Reader?> FindReader(string name)
    {
        var clean = name.Trim().ToLower();
        return db.Readers.FirstOrDefaultAsync(r => r.Name.ToLower() == clean);
    }

    /// <summary>
    /// Libri finiti in un anno da un'altra persona, in ordine di data. Quelli senza data di fine
    /// lettura contano per l'anno in cui sono stati registrati (DateAdded).
    /// </summary>
    private Task<List<ReaderBook>> ReaderBooksReadIn(int readerId, int year)
    {
        var from = new DateOnly(year, 1, 1);
        var to = new DateOnly(year, 12, 31);
        return db.ReaderBooks
            .Where(b => b.ReaderId == readerId && b.Status == ReadingStatus.Read
                        && ((b.DateRead >= from && b.DateRead <= to)
                            || (b.DateRead == null && !b.ReadYearUnknown && b.DateAdded >= from && b.DateAdded <= to)))
            .OrderBy(b => b.DateRead)
            .ToListAsync();
    }

    /// <summary>Libri finiti in un anno, in ordine di data (usato da più strumenti).</summary>
    private Task<List<Book>> BooksReadIn(int year)
    {
        // Confrontiamo le date invece di usare .Year perché è più sicuro con SQLite.
        var from = new DateOnly(year, 1, 1);
        var to = new DateOnly(year, 12, 31);
        return db.Books
            .Where(b => b.Status == ReadingStatus.Read && b.DateRead >= from && b.DateRead <= to)
            .OrderBy(b => b.DateRead)
            .ToListAsync();
    }

    /// <summary>Elenco di libri in un certo stato di lettura.</summary>
    [Description("Elenco di libri per stato: ToRead (da leggere), Reading (in lettura) o Read (letti).")]
    public async Task<object> GetBooksByStatus(
        [Description("Lo stato: ToRead, Reading, Read oppure Abandoned")] string status,
        [Description("Quanti libri al massimo restituire (da 1 a 50)")] int max = 15)
    {
        // Claude potrebbe scrivere lo stato in modo imperfetto: se non lo riconosciamo lo diciamo.
        if (!Enum.TryParse<ReadingStatus>(status, ignoreCase: true, out var parsed))
            return new { error = "Stato non valido. Valori ammessi: ToRead, Reading, Read, Abandoned." };

        // Limitiamo il numero di risultati: meno dati inviati = meno token = meno costo.
        max = Math.Clamp(max, 1, 50);
        var total = await db.Books.CountAsync(b => b.Status == parsed);
        var books = await db.Books
            .Where(b => b.Status == parsed)
            .OrderBy(b => b.Title)
            .Take(max)
            .Select(b => new { b.Title, b.Author, b.Pages, started = b.DateStarted }) // data di inizio: utile per i libri "in lettura"
            .ToListAsync();
        return new { status = parsed.ToString(), total, shown = books.Count, books };
    }

    // ─── CONSIGLI (fase 2): profilo dei gusti, pila dei "da leggere", controllo libreria ───

    /// <summary>
    /// Profilo dei gusti calcolato dai voti: autori amati e bocciati, libri preferiti e detestati,
    /// abbandonati, voto medio per lunghezza. Generi e temi non sono nei dati: li deduce Claude.
    /// </summary>
    [Description("Profilo dei gusti dell'utente calcolato dai suoi voti: autori amati e bocciati, libri preferiti (voto >= 4,5), libri bocciati (voto <= 2), libri abbandonati, voto medio per lunghezza del libro. Usalo PRIMA di consigliare qualsiasi libro.")]
    public async Task<object> GetTasteProfile()
    {
        var books = await db.Books
            .Where(b => b.Status == ReadingStatus.Read || b.Status == ReadingStatus.Abandoned)
            .ToListAsync();
        // Solo i letti con voto: 0 = nessun voto (CLAUDE.md §8). Calcoli in memoria (decimal in SQLite).
        var rated = books.Where(b => b.Status == ReadingStatus.Read && b.MyRating > 0).ToList();
        var byAuthor = rated
            .GroupBy(b => b.Author)
            .Select(g => new { author = g.Key, booksRated = g.Count(), averageRating = Math.Round(g.Average(b => b.MyRating), 2) })
            .ToList();

        return new
        {
            booksRead = books.Count(b => b.Status == ReadingStatus.Read),
            booksRated = rated.Count,
            averageRating = rated.Count > 0 ? Math.Round(rated.Average(b => b.MyRating), 2) : (decimal?)null,
            // Amati: almeno 2 libri con media alta, così un solo bel libro non basta a fare un "autore preferito".
            lovedAuthors = byAuthor.Where(a => a.booksRated >= 2 && a.averageRating >= 4m)
                .OrderByDescending(a => a.averageRating).ThenByDescending(a => a.booksRated).Take(20),
            dislikedAuthors = byAuthor.Where(a => a.averageRating <= 2.5m)
                .OrderBy(a => a.averageRating).Take(15),
            favoriteBooks = rated.Where(b => b.MyRating >= 4.5m).OrderByDescending(b => b.DateRead).Take(25)
                .Select(b => new { b.Title, b.Author, rating = b.MyRating, b.Pages }),
            dislikedBooks = rated.Where(b => b.MyRating <= 2m).OrderByDescending(b => b.DateRead).Take(15)
                .Select(b => new { b.Title, b.Author, rating = b.MyRating }),
            abandonedBooks = books.Where(b => b.Status == ReadingStatus.Abandoned).Take(30)
                .Select(b => new { b.Title, b.Author }),
            // Voto medio per fascia di pagine: dice se preferisce libri brevi o lunghi.
            ratingByLength = rated.Where(b => b.Pages > 0)
                .GroupBy(b => b.Pages < 200 ? "meno di 200" : b.Pages < 400 ? "200-399" : b.Pages < 600 ? "400-599" : "600 e oltre")
                .Select(g => new { pages = g.Key, books = g.Count(), averageRating = Math.Round(g.Average(b => b.MyRating), 2) }),
            note = "Generi e temi non sono nei dati: deducili dai titoli e dagli autori che conosci, dicendo che è una tua deduzione."
        };
    }

    /// <summary>
    /// I libri della pila "da leggere", con il voto medio che l'utente ha dato ai loro autori:
    /// prima quelli di autori amati, poi quelli di autori nuovi (dai più vecchi in pila).
    /// </summary>
    [Description("Libri della pila 'da leggere' dell'utente (già posseduti/desiderati), filtrabili per pagine e autore. Per ognuno: pagine, data di aggiunta, voto medio che l'utente ha dato all'autore e quanti suoi libri ha letto, e se ha abbandonato un libro di quell'autore. Ordinati: prima gli autori amati, poi quelli nuovi, poi i meno amati. Consiglia SEMPRE prima da qui.")]
    public async Task<object> GetToReadCandidates(
        [Description("Numero massimo di pagine (facoltativo, es. 250 per un libro breve)")] int? maxPages = null,
        [Description("Numero minimo di pagine (facoltativo)")] int? minPages = null,
        [Description("Parte del nome dell'autore (facoltativo)")] string? author = null,
        [Description("Quanti libri al massimo restituire (da 1 a 60)")] int max = 30)
    {
        // Statistiche per autore dai tuoi libri letti e abbandonati.
        var history = await db.Books
            .Where(b => b.Status == ReadingStatus.Read || b.Status == ReadingStatus.Abandoned)
            .ToListAsync();
        var ratings = history.Where(b => b.Status == ReadingStatus.Read && b.MyRating > 0)
            .GroupBy(b => b.Author)
            .ToDictionary(g => g.Key, g => (avg: Math.Round(g.Average(b => b.MyRating), 2), count: g.Count()));
        var abandonedAuthors = history.Where(b => b.Status == ReadingStatus.Abandoned).Select(b => b.Author).ToHashSet();

        var query = db.Books.Where(b => b.Status == ReadingStatus.ToRead);
        if (maxPages is > 0) query = query.Where(b => b.Pages != null && b.Pages <= maxPages);
        if (minPages is > 0) query = query.Where(b => b.Pages != null && b.Pages >= minPages);
        var pile = await query.ToListAsync();
        // Filtro per autore in memoria: senza maiuscole/minuscole.
        if (!string.IsNullOrWhiteSpace(author))
            pile = pile.Where(b => b.Author.Contains(author.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

        max = Math.Clamp(max, 1, 60);
        var ranked = pile
            .Select(b => new
            {
                b.Title, b.Author, b.Pages,
                addedOn = b.DateAdded,
                myAverageForAuthor = ratings.TryGetValue(b.Author, out var r) ? r.avg : (decimal?)null,
                booksReadByAuthor = ratings.TryGetValue(b.Author, out var r2) ? r2.count : 0,
                abandonedThisAuthor = abandonedAuthors.Contains(b.Author)
            })
            // Autore sconosciuto = 3,5 (neutro): sta dopo gli amati e prima dei bocciati.
            .OrderBy(b => b.abandonedThisAuthor)
            .ThenByDescending(b => b.myAverageForAuthor ?? 3.5m)
            .ThenBy(b => b.addedOn ?? DateOnly.MaxValue) // a parità, i più vecchi in pila
            .Take(max)
            .ToList();

        return new { totalInPile = pile.Count, shown = ranked.Count, books = ranked };
    }

    /// <summary>Un libro da cercare nella libreria (parametro di CheckBooksInLibrary).</summary>
    public record BookQuery(
        [property: Description("Titolo")] string Title,
        [property: Description("Autore (consigliato: evita confusioni tra titoli simili)")] string? Author);

    /// <summary>
    /// Controlla se dei libri sono già nella libreria dell'utente (letti, da leggere, abbandonati…),
    /// per non consigliare un libro già letto o già in pila come se fosse nuovo.
    /// </summary>
    [Description("Controlla se dei libri sono già nella libreria dell'utente e in che stato (Read con voto, ToRead, Reading, Abandoned). Usalo SEMPRE prima di consigliare libri che non vengono da GetToReadCandidates, ed escludi quelli già letti o abbandonati.")]
    public async Task<object> CheckBooksInLibrary(
        [Description("I libri da controllare (al massimo 30)")] List<BookQuery> books)
    {
        var library = await db.Books.ToListAsync();
        return books.Take(30).Select(q =>
        {
            // Stessa regola di riconoscimento della sfida competitiva (titolo normalizzato + autore).
            var found = library.FirstOrDefault(b => BookMatching.Matches(q.Title, q.Author, b));
            return new
            {
                q.Title, q.Author,
                inLibrary = found is not null,
                status = found?.Status.ToString(),
                rating = found is { MyRating: > 0 } ? found.MyRating : (decimal?)null
            };
        }).ToList();
    }
}
