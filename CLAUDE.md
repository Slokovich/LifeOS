# LifeOS — Guida al progetto

> **A cosa serve questo file.** È il punto di partenza per chiunque lavori su LifeOS: persone (anche non esperte di programmazione) e assistenti IA (Claude Code, Copilot, ecc.). Spiega cos'è il progetto, com'è organizzato, come avviarlo, quali scelte sono state fatte e quali problemi sono noti.
> Claude Code lo legge automaticamente all'inizio di ogni sessione, quindi tutto ciò che è scritto qui diventa "contesto" per l'IA.

**Ultimo aggiornamento:** 2026-11-04 (sfida competitiva con Fra, fase 5a)
**Repository GitHub (pubblico):** https://github.com/Slokovich/LifeOS — branch principale `main`

---

## 0. Regola fondamentale: tenere aggiornato questo file

> **Perché questa sezione:** un file di istruzioni vecchio è peggio di nessun file, perché porta persone e IA a fare scelte sbagliate. Questa sezione stabilisce *quando* e *come* aggiornarlo.

**Istruzioni per gli assistenti IA (obbligatorie):**
Dopo ogni modifica rilevante al progetto, aggiorna questo file **nella stessa sessione**, prima di dichiarare il lavoro concluso. Una modifica è "rilevante" se:
- aggiunge, rimuove o rinomina file, cartelle, progetti, agenti, modelli, endpoint o pagine;
- aggiunge o aggiorna pacchetti NuGet;
- cambia la configurazione (porte, stringhe di connessione, segreti necessari, CORS…);
- crea una migrazione del database;
- introduce una convenzione o una scelta di stile/architettura;
- scopre, causa o risolve un bug o un problema.

**Cosa aggiornare:**
1. La sezione interessata (struttura, modelli, comandi…).
2. La **§9 Problemi noti**: aggiungi i nuovi problemi, segna come risolti quelli risolti (non cancellarli: spostali in "Risolti" con la soluzione).
3. La **§8 Decisioni** se hai fatto una scelta non ovvia, spiegando il *perché*.
4. La **§11 Registro modifiche** con una riga datata.
5. La data di "Ultimo aggiornamento" in cima.

**Per le persone:** se modifichi il progetto a mano, fai lo stesso. Se non sai come, chiedi a Claude: *"aggiorna CLAUDE.md con le modifiche che ho fatto"*.

---

## 1. Cos'è LifeOS

> **Perché questa sezione:** spiega lo scopo dell'app, così ogni modifica può essere valutata rispetto all'obiettivo.

LifeOS è un'**app personale** che aiuta a organizzare la vita quotidiana (sport, alimentazione, lettura, vita sociale, studio). È **multi-agente**: invece di un unico assistente, ci sono più "agenti" IA, ognuno esperto di un ambito, coordinati da un agente principale (l'orchestratore).

> **Glossario minimo**
> - **Agente IA:** un programma che usa un modello linguistico (qui Claude Haiku) per ragionare, con accesso a "strumenti" (es. leggere il database o il calendario).
> - **Orchestratore:** l'agente che riceve la richiesta dell'utente e decide quali agenti specializzati coinvolgere.
> - **API / backend:** la parte che gira "dietro le quinte": logica, database, chiamate all'IA.
> - **Frontend:** l'interfaccia che si vede nel browser.

### Gli agenti previsti

| Agente | Legge da | Cosa fa | Stato |
|---|---|---|---|
| **Orchestratore** | tutti gli agenti | Riceve la richiesta, la smista agli agenti giusti, combina le risposte | da fare |
| **Sport** | Google Calendar, `MenstrualCycle` | Pianifica allenamenti in base a impegni e fase del ciclo | da fare |
| **Cibo** | `MealLog`, impegni del calendario | Suggerisce cosa mangiare evitando ripetizioni e tenendo conto della giornata | da fare |
| **Libri** | `Book` (import CSV Goodreads) | Gestisce la reading challenge annuale (cosa vogliamo che faccia: vedi **§12**) | **prima versione funzionante**: risponde a domande su libri letti per anno e per stato e sulle challenge a numero, alfabeto e competitiva (ritmo atteso, lettere mancanti, punteggio contro Fra, piano di recupero con meno pagine); statistiche di Fra; crea/modifica challenge, liste e letture di Fra solo con la conferma dell'utente |
| **Sociale** | Google Calendar, `Friend` | Ricorda le sedute dallo psicologo, suggerisce uscite con gli amici | da fare |
| **Apprendimento** | `Course`, `StudySession` | Pianifica il tempo di studio nei corsi | da fare |

---

## 2. Tecnologie usate

> **Perché questa sezione:** dice con quali strumenti è costruito il progetto e con quali versioni. Le versioni contano: pacchetti di versioni diverse possono non funzionare insieme.

| Ambito | Tecnologia | Versione | Note |
|---|---|---|---|
| Linguaggio / runtime | C# su .NET | SDK 9.0.320, target `net9.0` | |
| Backend | ASP.NET Core (Minimal API) | 9.0 | progetto `LifeOS.Api` |
| Frontend | Blazor WebAssembly | 9.0.22 | progetto `LifeOS.Web`, gira interamente nel browser |
| Database | SQLite | — | un singolo file, `lifeos.db` |
| Accesso al DB | Entity Framework Core | 9.0.22 | `Microsoft.EntityFrameworkCore.Sqlite` + `.Design` |
| Agenti | Microsoft Agent Framework | 1.27.0 | pacchetto `Microsoft.Agents.AI` |
| Modello IA | Anthropic API, Claude Haiku | — | connettore: `Microsoft.Agents.AI.Anthropic` 1.27.0 (porta l'SDK ufficiale `Anthropic` 12.53.0 e `Microsoft.Extensions.AI` 10.10.0); registrato in `Program.cs` come `IChatClient`, modello `claude-haiku-5-5` (in `appsettings.json` → `Anthropic:Model`), limite risposta `Anthropic:MaxOutputTokens` = 8000 (non abbassarlo: vedi #18); provato con `/api/ping-ai` |
| Markdown → HTML | Markdig | 1.5.1 | nel progetto `LifeOS.Web`, per mostrare le risposte dell'agente nella chat |
| Lettura CSV | CsvHelper | 33.0.1 | usato da `GoodreadsImportService` per leggere l'export di Goodreads |
| Documentazione API | OpenAPI | 9.0.22 | disponibile su `/openapi/v1.json` in Development |
| UI | Bootstrap | (incluso in `wwwroot/lib`) | template di default |

> **Regola:** i pacchetti `Microsoft.EntityFrameworkCore.*` e `Microsoft.AspNetCore.*` devono restare sulla **stessa versione 9.0.x**. Non aggiornarli alla 10.x senza prima portare tutto il progetto a .NET 10.

---

## 3. Struttura delle cartelle

> **Perché questa sezione:** aiuta a capire dove si trova ogni cosa e dove mettere il codice nuovo.

```
LifeOS/
├── CLAUDE.md                  ← questo file
├── LifeOS.sln                 ← "soluzione": raggruppa i due progetti (aprila con Visual Studio)
│
├── LifeOS.Api/                ← BACKEND (server)
│   ├── Program.cs             ← punto di avvio: registra servizi, DB, endpoint
│   ├── LifeOS.Api.csproj      ← elenco pacchetti NuGet e impostazioni del progetto
│   ├── LifeOS.Api.http        ← richieste HTTP di prova (eseguibili da VS / VS Code)
│   ├── appsettings.json       ← configurazione (stringa di connessione, log)
│   ├── appsettings.Development.json ← configurazione solo in sviluppo
│   ├── Properties/launchSettings.json ← porte e profili di avvio
│   ├── Agents/                ← un file per agente + orchestratore (oggi: BookAgent.cs e i suoi strumenti BookTools.cs)
│   ├── Endpoints/             ← un file per area con gli endpoint HTTP (oggi: BookEndpoints.cs → /api/books e /api/books/import; ReaderEndpoints.cs → GET /api/readers e POST /api/readers/{nome}/import; PendingEndpoints.cs → /api/pending/{id}/confirm|reject; AiEndpoints.cs → POST /api/agents/books (conversazione) e GET /api/ping-ai, quest'ultimo è una prova temporanea da eliminare)
│   ├── Models/                ← classi che rappresentano le tabelle del DB (le challenge: Challenge astratta + CountChallenge, AlphabetChallenge; ChallengeProgress è solo il risultato di un calcolo)
│   ├── Dtos/                  ← classi che servono solo a trasportare dati tra frontend e API, non sono tabelle (oggi: ChatMessageDto, ProposalDto, AgentReply)
│   ├── Data/                  ← LifeOSDbContext + Migrations/ (migrazioni generate da EF: non modificarle a mano)
│   └── Services/              ← integrazioni esterne (oggi: GoodreadsImportService.cs = import CSV; PendingActionService.cs = crea/conferma/rifiuta le modifiche proposte dagli agenti); in futuro Google Calendar, client Anthropic
│
└── LifeOS.Web/                ← FRONTEND (Blazor WebAssembly, ancora template di default)
    ├── Program.cs             ← avvio dell'app nel browser, registra HttpClient
    ├── App.razor              ← router: decide quale pagina mostrare per ogni URL
    ├── _Imports.razor         ← "using" comuni a tutte le pagine
    ├── Layout/                ← struttura della pagina (menu laterale, layout)
    ├── Models/                ← classi dei dati come li vede il frontend (oggi: BookItem, ImportResultItem, ChatMessageItem, ChatReplyItem, ProposalItem)
    ├── Pages/                 ← pagine: Home (template), Books (/books: elenco, aggiunta libri, import CSV Goodreads tuo e di un'altra persona, es. Fra), Chat (/chat: conversazione con l'agente Libri)
    └── wwwroot/               ← file statici: index.html, appsettings.json (ApiBaseUrl), CSS, icone, Bootstrap
```

`bin/` e `obj/` (in entrambi i progetti) contengono i file generati dalla compilazione: **non vanno modificati né salvati in git**.

### Dove mettere il codice nuovo
- Nuovo agente → `LifeOS.Api/Agents/<Nome>Agent.cs`
- Nuovi endpoint HTTP → `LifeOS.Api/Endpoints/<Area>Endpoints.cs` (metodo `Map<Area>Endpoints` richiamato da `Program.cs`)
- Nuova tabella → classe in `LifeOS.Api/Models/` + `DbSet` e configurazione in `Data/LifeOSDbContext.cs` + migrazione
- Integrazione esterna (API, file) → `LifeOS.Api/Services/`
- Nuova pagina web → `LifeOS.Web/Pages/<Nome>.razor` + link in `Layout/NavMenu.razor`

---

## 4. Database

> **Perché questa sezione:** descrive quali dati salva l'app e come sono collegati. È il "vocabolario" condiviso da tutti gli agenti.

Il database è un file SQLite (`lifeos.db`). Il percorso è definito in `appsettings.json` → `ConnectionStrings:LifeOS` = `Data Source=lifeos.db` (percorso relativo: il file viene creato nella cartella da cui si avvia l'API, normalmente `LifeOS.Api/`).

`Data/LifeOSDbContext.cs` è la classe che "traduce" tra oggetti C# e tabelle; è registrata in `Program.cs` con `AddDbContext`.

### Tabelle (modelli in `Models/`)

| Modello | Tabella | Usato da | Campi principali | Note |
|---|---|---|---|---|
| `MealLog` | `MealLogs` | Cibo | `EatenAt`, `Type` (Breakfast/Lunch/Dinner/Snack), `Description` (max 500), `Calories?`, `Notes?` | indice su `EatenAt` |
| `MenstrualCycle` | `MenstrualCycles` | Sport | `StartDate`, `PeriodEndDate?`, `CycleLengthDays?`, `Notes?` | `StartDate` unico; un record = un ciclo |
| `Book` | `Books` | Libri | `GoodreadsId?`, `Title`, `Author`, `AuthorLastFirst?` ("Cognome, Nome", colonna "Author l-f" del CSV), `Isbn13?`, `Pages?`, `MyRating` (`decimal` 0–5, anche 3.5 o 2.25), `Status` (ToRead/Reading/Read), `DateAdded?`, `DateRead?` | `GoodreadsId` unico → evita duplicati reimportando il CSV |
| `Challenge` (astratta) → `CountChallenge`, `AlphabetChallenge` | `Challenges` (tabella unica, TPH) | Libri | `Name`, `Year`, `CreatedAt`, `Kind` (colonna discriminante: `Count`/`Alphabet`), `TargetBooks?` (solo Count) | metodo astratto `Evaluate(libriLettiNellAnno, oggi)` → `ChallengeProgress`; `IsGoalReached()`. Al massimo una per tipo per anno (controllo in `PendingActionService`). Scritte solo dopo conferma (`PendingAction`). Alfabeto: iniziale del **cognome**, 26 lettere A-Z, accenti ignorati |
| `CompetitiveChallenge` (sottoclasse di `Challenge`) | `Challenges` (`Kind` = `Competitive`) | Libri | `OpponentId` → `Reader` (Restrict), `Items` | sfida contro un'altra persona su una lista fissa con punti per libro, anno solare; vince chi ha più punti a fine anno |
| `ChallengeListItem` | `ChallengeListItems` | Libri | `ChallengeId`, `Title`, `Author?`, `Points` (`decimal`, 0,5–10, 5 se non indicato), `AnyBookByAuthor` | libro della lista di una sfida; eliminando la sfida si elimina la lista (cascade). Regole di `BookMatching`: senza autore → titolo normalizzato identico (senza accenti, maiuscole, punteggiatura, sottotitolo); con autore → cognome presente nell'autore letto **e** un titolo è l'inizio (o la fine) dell'altro ("Jurassic Park (Jurassic Park, #1)", "M. Il figlio del secolo"); `AnyBookByAuthor` → vale un libro qualsiasi di quell'autore (es. Asimov). L'autore serve anche a escludere edizioni diverse (Ben-Hur per ragazzi di Dirk Maggs ≠ Lew Wallace) |
| `Reader` | `Readers` | Libri | `Name` (unico, max 100) | un'altra persona che legge (es. Fra); creata confermando una sfida o una sua lettura, o importando il suo CSV |
| `ReaderBook` | `ReaderBooks` | Libri | `ReaderId`, `GoodreadsId?`, `Title`, `Author`, `AuthorLastFirst?`, `Pages?`, `MyRating`, `Status`, `DateRead?`, `DateAdded?` | i libri di un `Reader`, separati da `Books`; un libro letto **senza** `DateRead` conta per l'anno di `DateAdded` (data di registrazione in chat o "Date Added" del CSV); indice unico (`ReaderId`, `GoodreadsId`) per reimportare senza duplicati. `Book` e `ReaderBook` implementano `IReadingRecord` |
| `PendingAction` | `PendingActions` | tutti gli agenti | `Type` (SetChallengeGoal, CreateAlphabetChallenge, CreateCompetitiveChallenge, AddChallengeItem, AddReaderBook), `PayloadJson`, `Summary`, `Status` (Pending/Confirmed/Rejected), `CreatedAt`, `ResolvedAt?` | modifiche proposte dagli agenti in attesa di conferma (§12.1 regola 2); indice su `Status` |
| `Friend` | `Friends` | Sociale | `Name`, `LastSeen?`, `DesiredFrequencyDays?`, `Interests?`, `Notes?` | |
| `Course` | `Courses` | Apprendimento | `Title`, `Topic`, `Platform`, `TotalHours`, `IsCompleted` | ha molte `StudySession` |
| `StudySession` | `StudySessions` | Apprendimento | `CourseId`, `ScheduledStart`, `PlannedMinutes`, `ActualMinutes?`, `IsCompleted`, `Notes?` | eliminando un corso si eliminano le sue sessioni (cascade) |

`?` = campo facoltativo (può essere vuoto).

### Migrazioni
Una **migrazione** è uno script generato automaticamente che crea o modifica le tabelle nel DB per farle corrispondere ai modelli. Migrazioni esistenti (in `LifeOS.Api/Data/Migrations/`):

| Migrazione | Cosa fa |
|---|---|
| `20261102133805_InitialCreate` | Crea le 6 tabelle (MealLogs, MenstrualCycles, Books, Friends, Courses, StudySessions) e i relativi indici. |
| `MakeBookRatingDecimal` | Cambia `Books.MyRating` da intero a `decimal` (in SQLite è salvato come testo). Applicata il 2026-11-03. |
| `AddReadingChallenge` | Crea la tabella `ReadingChallenges` con indice unico su `Year`. Applicata il 2026-11-03. |
| `AddPendingActions` | Crea la tabella `PendingActions` (proposte in attesa di conferma). Applicata il 2026-11-03. |
| `ChallengeHierarchy` | Crea `Challenges` (TPH), aggiunge `Books.AuthorLastFirst`, **copia** gli obiettivi da `ReadingChallenges` come challenge `Count` e poi elimina `ReadingChallenges`. **Modificata a mano** (vedi §8). Applicata il 2026-11-03: l'obiettivo 60/2026 è stato conservato. |
| `CompetitiveChallenge` | Crea `Readers`, `ReaderBooks`, `ChallengeListItems` e la colonna `Challenges.OpponentId`. Applicata il 2026-11-04. |
| `DecimalChallengePoints` | `ChallengeListItems.Points` da intero a decimale, `Author` facoltativo. Applicata il 2026-11-04 (tabella vuota). |
| `ListItemAnyAuthorAndReaderDateAdded` | Aggiunge `ChallengeListItems.AnyBookByAuthor` e `ReaderBooks.DateAdded`. Applicata il 2026-11-04. |

I file in `Migrations/` sono tre: la migrazione (`..._InitialCreate.cs`), il suo `.Designer.cs` e `LifeOSDbContextModelSnapshot.cs` (fotografia dei modelli, usata da EF per calcolare la differenza alla prossima migrazione). Vanno **committati** su git. Il file `lifeos.db` invece **no** (è già nel `.gitignore`). EF crea anche due tabelle di servizio, `__EFMigrationsHistory` (migrazioni già applicate) e `__EFMigrationsLock` (blocco temporaneo durante l'applicazione).
Il database è stato creato (`dotnet ef database update`) il 2026-11-02; per ricrearlo da zero basta cancellare `lifeos.db*` (solo con conferma dell'utente, vedi §10) e rieseguire il comando.

---

## 5. Come avviare il progetto

> **Perché questa sezione:** i comandi pronti da copiare per compilare, avviare e gestire il database.

Tutti i comandi vanno eseguiti nella cartella principale `LifeOS/`.

```bash
# Compilare tutto (verifica che non ci siano errori)
dotnet build LifeOS.sln

# Avviare il backend  → http://localhost:5219  (profilo https: https://localhost:7290)
dotnet run --project LifeOS.Api

# Avviare il frontend → http://localhost:5222  (profilo https: https://localhost:7232)
dotnet run --project LifeOS.Web
```

Backend e frontend sono due programmi separati: per usarli insieme vanno avviati entrambi (due terminali).

### Salvare le modifiche su GitHub
```bash
git status                       # cosa è cambiato rispetto all'ultimo salvataggio
git add -A                       # prepara tutte le modifiche
git commit -m "Descrizione breve" # crea un "punto di salvataggio" locale
git push                         # lo carica su GitHub
```
La GitHub CLI è in `C:\Program Files\GitHub CLI\gh.exe` (login già fatto come `Slokovich`).

### Database (prima volta)
```bash
# 1. Installare lo strumento EF Core (una sola volta per PC; già installato su questo PC, versione 9.0.22)
dotnet tool install --global dotnet-ef --version 9.0.22

# 2. Creare una migrazione dopo ogni modifica ai modelli
dotnet ef migrations add <NomeDescrittivo> --project LifeOS.Api --output-dir Data/Migrations

# 3. Applicare le migrazioni al database (crea lifeos.db se non esiste)
dotnet ef database update --project LifeOS.Api
```

### Segreti (chiavi API)
Le chiavi **non vanno mai** in `appsettings.json` o nel codice. Usare *user-secrets* (salvati fuori dal progetto, solo sul proprio PC):
```bash
dotnet user-secrets init --project LifeOS.Api
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-..." --project LifeOS.Api
```
Nel codice si leggono con `builder.Configuration["Anthropic:ApiKey"]`.

---

## 6. Come funziona (architettura)

> **Perché questa sezione:** descrive come le parti dovranno parlare tra loro. Serve a non prendere strade incompatibili.

```
Browser (LifeOS.Web, Blazor)
        │  HTTP/JSON
        ▼
LifeOS.Api ──► Orchestratore ──► Agenti (Sport, Cibo, Libri, Sociale, Apprendimento)
        │                              │                │
        ▼                              ▼                ▼
   SQLite (lifeos.db)            Anthropic API     Google Calendar
```

- Il **frontend** non accede mai direttamente al DB né all'IA: passa sempre dall'API.
- Ogni **agente** riceve strumenti (*tools*) solo per i dati che gli servono (es. l'agente Libri legge solo `Books`).
- L'**orchestratore** è l'unico punto d'ingresso per le richieste in linguaggio naturale.

### 6.1 Come è fatto il frontend (Blazor WebAssembly)

> **Perché questa sottosezione:** chi apre `LifeOS.Web` trova file HTML e CSS e può pensare che il frontend non sia in Blazor. Qui si spiega perché ci sono e come si combinano.

**Blazor sostituisce JavaScript con C#, non HTML e CSS.** Il browser sa mostrare solo HTML e CSS: qualunque tecnologia web, Blazor compreso, alla fine produce quelli.

| Linguaggio | A cosa serve | In LifeOS |
|---|---|---|
| **HTML** | struttura (titoli, pulsanti, tabelle, moduli) | dentro i file `.razor` |
| **CSS** | aspetto (colori, spazi, disposizione) | `app.css`, file `.razor.css`, Bootstrap |
| **C#** | logica (clic, dati, chiamate all'API) | dentro i blocchi `@code { }` dei file `.razor` |
| **JavaScript** | — | **non si scrive**; solo `blazor.webassembly.js` (avvio di Blazor) |

**Come funziona all'avvio**
1. Il browser scarica `wwwroot/index.html`, un guscio quasi vuoto.
2. `index.html` carica `_framework/blazor.webassembly.js`, che scarica il runtime .NET e il codice C# compilato (WebAssembly) ed esegue tutto **dentro il browser**.
3. `Program.cs` inserisce il componente `App` in `<div id="app">`; da lì Blazor disegna e aggiorna la pagina da solo.

**I tipi di file**
- **`.razor`** — un *componente*: HTML + C# nello stesso file. Se contiene `@page "/indirizzo"` è una **pagina** (in `Pages/`), altrimenti è un pezzo riutilizzabile (es. `NavMenu`, `MainLayout` in `Layout/`). Esempio:
  ```razor
  <button @onclick="Aumenta">Clic</button>   @* HTML *@
  <p>Valore: @valore</p>                      @* @ inserisce un valore C# *@
  @code {
      private int valore = 0;                  // C#
      private void Aumenta() => valore++;      // Blazor ridisegna la pagina da solo
  }
  ```
- **`.razor.css`** — stile *isolato*: vale **solo** per il componente con lo stesso nome (es. `NavMenu.razor.css` → solo `NavMenu.razor`). Blazor li unisce nel file generato `LifeOS.Web.styles.css`.
- **`wwwroot/css/app.css`** — stile **globale**, vale per tutta l'app.
- **`wwwroot/index.html`** — pagina ospite: si modifica solo per titolo, lingua, icona o per aggiungere un foglio di stile.
- **`wwwroot/lib/bootstrap/`** — libreria CSS esterna (pulsanti, tabelle, griglia). Si usa tramite le classi (`class="btn btn-primary"`), **non si modifica**.
- **`_Imports.razor`** — `@using` validi per tutti i componenti.

**Regole per il frontend**
- Nuova pagina → `Pages/<Nome>.razor` con `@page "/nome"` + link in `Layout/NavMenu.razor`.
- Pezzo riutilizzabile in più pagine → componente in una cartella `Components/` (da creare quando serve).
- Stile di un solo componente → file `<Nome>.razor.css` accanto; stile comune → `app.css`. Prima di scrivere CSS, controlla se basta una classe Bootstrap.
- Logica complessa o chiamate all'API → non nel `.razor`, ma in una classe di servizio C# (`Services/`, da creare) iniettata con `@inject`.
- JavaScript solo se indispensabile (es. una libreria esterna senza equivalente .NET), tramite `IJSRuntime`; documentarlo in §8.
- Il frontend parla **solo** con `LifeOS.Api` via HTTP/JSON, mai direttamente con DB o IA.

---

## 7. Convenzioni di codice e stile

> **Perché questa sezione:** codice scritto sempre allo stesso modo è più facile da leggere e modificare, per persone e IA.

**Lingua**
- Codice (nomi di classi, proprietà, metodi, tabelle, enum): **inglese**.
- Commenti XML (`/// <summary>`), testi visibili all'utente, documentazione: **italiano**.

**Commenti (obbligatori)**
- Ogni pezzo di codice, nuovo o modificato, va **commentato in italiano** spiegando *cosa fa* e, quando non è ovvio, *perché*. Chi legge può non essere un programmatore esperto.
- C#: `/// <summary>` su classi, proprietà e metodi pubblici; `//` per i passaggi interni. Nei file lunghi, separatori di sezione come `// ─── 1. TITOLO ───`.
- Razor (`.razor`): `@* commento *@` per l'HTML, `//` dentro i blocchi `@code { }`.
- Il codice demo del template va segnalato come tale ("DEMO del template, da eliminare").
- Quando cambi il codice, aggiorna anche i commenti: un commento sbagliato è peggio di nessun commento.
- Non si commentano i file generati o di librerie esterne (`bin/`, `obj/`, `wwwroot/lib/`, migrazioni).

**C#**
- Namespace in stile file (`namespace LifeOS.Api.Models;`) e uguale alla cartella.
- Una classe pubblica per file, con nome file = nome classe.
- `Nullable` attivo: i campi facoltativi hanno `?`; i campi obbligatori di tipo stringa sono `required`.
- Chiave primaria: `int Id`. Chiavi esterne: `<Entità>Id` + proprietà di navigazione.
- Costruttori primari dove semplificano (es. `LifeOSDbContext(DbContextOptions<...> options)`).
- Collezioni inizializzate con `= [];`.

**Database / EF Core**
- **Enum salvati come stringhe** (`HasConversion<string>()`): il DB resta leggibile e aggiungere valori non rompe i dati esistenti.
- `DateOnly` per date senza orario (cicli, libri, amici); `DateTime` per momenti precisi (pasti, sessioni).
- Lunghezze massime, indici e relazioni si configurano in `OnModelCreating`, non con attributi sui modelli.
- Ogni modifica ai modelli richiede una migrazione con un nome descrittivo (es. `AddWorkoutTable`).

**Versioni pacchetti**: sempre una versione esatta (es. `9.0.22`), mai jolly come `9.*`, così la compilazione è ripetibile.

---

## 8. Decisioni prese (e perché)

> **Perché questa sezione:** spiega le scelte non ovvie, così nessuno le cambia per errore senza conoscerne il motivo.

| Data | Decisione | Motivo |
|---|---|---|
| 2026-11-01 | SQLite come database | App personale, un solo utente: nessun server da installare, il DB è un unico file facile da salvare. |
| 2026-11-01 | Enum come stringhe nel DB | Leggibilità e robustezza se si aggiungono o riordinano valori. |
| 2026-11-01 | `GoodreadsId` unico su `Book` | Permette di reimportare il CSV di Goodreads aggiornando i libri invece di duplicarli. |
| 2026-11-01 | EF Core fissato a 9.0.22 | Il progetto è su .NET 9; EF Core 10 richiede .NET 10. |
| 2026-11-01 | Claude Haiku come modello | Veloce ed economico, adatto a molte chiamate brevi tra agenti. |
| 2026-11-02 | Enum come parole anche nel JSON dell'API (`JsonStringEnumConverter` in `Program.cs`) | Coerenza con il DB e leggibilità (`"Read"` invece di `2`). Nel frontend lo stato è quindi una `string`. |
| 2026-11-02 | Endpoint in file dedicati (`Endpoints/<Area>Endpoints.cs`) invece che tutti in `Program.cs` | `Program.cs` resta corto e leggibile man mano che le aree crescono. |
| 2026-11-03 | Import Goodreads con la libreria CsvHelper invece di un lettore CSV scritto a mano | Il CSV di Goodreads ha campi tra virgolette con virgole e a capo (es. recensioni): un `Split(',')` li romperebbe. |
| 2026-11-03 | `Book.MyRating` come `decimal` e non `int` | Nel CSV di Goodreads i voti hanno mezze stelle e decimali (3.5, 2.25): con un intero si perdevano. Attenzione: SQLite salva i `decimal` come testo, quindi `Sum`/`Average` sui voti non si possono fare nella query EF (vanno calcolati in memoria dopo `ToListAsync()`). |
| 2026-11-03 | La cronologia della chat sta nel browser e viene rimandata all'API a ogni messaggio (`POST /api/agents/books` con la lista dei messaggi); l'API ne tiene solo gli ultimi 20 e ne limita la lunghezza (4000 caratteri) | L'API resta senza stato (niente tabella o sessioni da gestire) e l'agente "ricorda" comunque la conversazione. Limite: ricaricando la pagina la conversazione si perde. Il tetto di 20 messaggi contiene i costi. |
| 2026-11-03 | Risposte dell'agente mostrate con Markdig e `DisableHtml()` | Claude scrive in Markdown; l'HTML grezzo viene ignorato così nessun testo (es. un titolo di libro) può inserire codice nella pagina. |
| 2026-11-03 | `.DisableAntiforgery()` su `POST /api/books/import` | .NET richiede un token antiforgery per i moduli con file; qui non servono perché l'API non usa cookie di login e accetta solo il frontend (CORS). Da rivedere se si aggiunge l'autenticazione. |
| 2026-11-03 | Challenge come classe astratta `Challenge` con sottoclassi, in **una sola tabella** (TPH, colonna `Kind`) | Pochi campi specifici per tipo; un'unica query elenca tutte le challenge. Ogni sottoclasse calcola da sola il suo stato (`Evaluate`): i conti li fa il codice, non l'IA. Se i tipi crescono molto si può passare a tabelle separate con una migrazione. |
| 2026-11-03 | Migrazione `ChallengeHierarchy` **modificata a mano** (eccezione alla regola §3 "non modificare le migrazioni") | EF generava un `DropTable` di `ReadingChallenges` prima di tutto, perdendo gli obiettivi salvati. Aggiunto un `Sql(INSERT … SELECT …)` e spostato il `DropTable` in fondo. Regola: **controllare sempre** le migrazioni che EF segnala con "may result in the loss of data". |
| 2026-11-02 | Indirizzo dell'API in `LifeOS.Web/wwwroot/appsettings.json`, non nel codice | Si cambia (es. in produzione) senza toccare il C#. Non metterci segreti: il file è pubblico, scaricato dal browser. |

---

## 9. Problemi noti, bug e soluzioni

> **Perché questa sezione:** raccoglie ciò che non funziona ancora o che potrebbe dare problemi, con la soluzione prevista. Prima di segnalare un "bug nuovo", controlla qui.

### Aperti

| # | Problema | Effetto | Soluzione proposta |
|---|---|---|---|
| 15 | **Endpoint `GET /api/ping-ai` e `GET /api/agents/books`** (`Endpoints/AiEndpoints.cs`) senza autenticazione: chiunque raggiunga l'API può chiamarli e far spendere token. Ora `/api/agents/books` è un `POST` usato dalla pagina `/chat`. | Costo piccolo in locale, ma da chiudere prima di esporre l'API su internet. | Eliminare `/api/ping-ai` (inutile ora che l'agente funziona); sostituire `/api/agents/books` con l'orchestratore; aggiungere autenticazione. |
| 16 | **Scadenza della chiave API Anthropic** circa 2027-02-01. | Dopo quella data le chiamate IA falliscono (errore 401). | Creare una nuova chiave e rieseguire `dotnet user-secrets set "Anthropic:ApiKey" ... --project LifeOS.Api`. |
| 6 | **`UseHttpsRedirection` con profilo `http`**. | Avviso nei log "Failed to determine the https port"; nessun effetto pratico. | Avviare con `--launch-profile https` oppure attivare il redirect solo fuori da Development. |
| 8 | **Modelli condivisi assenti**: i modelli stanno solo in `LifeOS.Api`. | Il frontend ridefinisce le stesse classi (già successo con `Web/Models/BookItem`, copia semplificata di `Book`: se cambia `Book`, aggiornarla a mano). | Creare un progetto `LifeOS.Shared` per i DTO usati da entrambi. |
| 9 | **Modelli mancanti** per alcune funzioni: allenamenti pianificati, sedute dallo psicologo (oggi previste solo da calendario). (Le challenge di lettura ci sono dal 2026-11-03: `Challenge` e sottoclassi.) | Gli agenti Sport e Sociale non hanno dove salvare questi dati. | Valutare `Workout` ed eventuale `TherapySession`. |
| 10 | **Integrazione Google Calendar assente**. | Agenti Sport, Cibo e Sociale non possono leggere gli impegni. | Pacchetto `Google.Apis.Calendar.v3` + OAuth, servizio in `Services/`. |
| 11 | **Dati sensibili** (ciclo mestruale, salute, terapia) inviati all'API Anthropic. | Questione di privacy. | Inviare all'IA solo i dati minimi necessari (es. "fase del ciclo: follicolare" invece dello storico completo). Non esporre l'API su internet senza autenticazione. |
| 12 | **Nessun test automatico**. | Le regressioni si scoprono solo a mano. | Aggiungere un progetto `LifeOS.Api.Tests` (xUnit) quando c'è logica da testare. |
| 20 | **Cognome degli autori impreciso finché non si reimporta il CSV**: `Books.AuthorLastFirst` è vuoto per i libri già importati, quindi la challenge alfabeto usa l'ultima parola dell'autore (saltando Jr./Sr./II…). | Cognomi composti sbagliati, es. Carlos Ruiz Zafón → Z invece di R. | Reimportare il CSV di Goodreads da `/books`: l'import ora legge "Author l-f". |
| 21 | **Riconoscimento dei libri della sfida per titolo**: un libro della lista "conta" solo se il titolo letto coincide (ignorando maiuscole, accenti, punteggiatura e sottotitolo). | Se tu o Fra leggete un'edizione con titolo diverso (es. tradotto: "Il nome della rosa" vs "The Name of the Rose") il libro non viene contato. | Aggiungere alla lista il titolo come compare su Goodreads; in futuro collegare gli elementi della lista a un ISBN o a più titoli alternativi. Caso noto nella sfida 2026: "Asimov (Fondazione / Destinazione cervello)" non coincide con nessun titolo reale. |
| 22 | **Mancano rimozione e modifica** di libri della lista, di letture di Fra e di challenge. | Un errore confermato per sbaglio si corregge solo a mano nel DB. | Aggiungere proposte `Remove...`/`Update...` con lo stesso meccanismo di conferma. |
| 13 | **Frontend non personalizzato**: titolo "LifeOS.Web", `lang="en"`, testi in inglese. | Aspetto da template. | Aggiornare `index.html` (`lang="it"`, titolo "LifeOS") e i testi. |

### Risolti

| # | Problema | Soluzione | Data |
|---|---|---|---|
| 19 | Chat: errore 400 "Messaggi non validi" dopo risposte vuote (#18) o lunghe (>4000 caratteri) dell'agente: l'API rifiutava la cronologia rimandata dal browser | In `POST /api/agents/books` il limite di 4000 caratteri e il controllo "non vuoto" valgono solo per la nuova domanda; nella cronologia le risposte vuote si scartano e quelle lunghe si accorciano a 4000. Provato con cronologia contenente un messaggio vuoto e uno da 5000 caratteri | 2026-11-03 |
| 18 | Con domande complesse (es. "prospetto di lettura per due mesi") l'agente rispondeva **a vuoto** circa 4 volte su 10: dopo aver chiamato gli strumenti il modello si fermava senza scrivere il testo (nei log, ultimo messaggio con solo ragionamento vuoto) | **Causa vera:** il limite predefinito di token in uscita del connettore Anthropic è basso (~1000); il modello lo esauriva ragionando e il testo usciva vuoto o tagliato. **Soluzione:** `AsIChatClient(model, maxTokens)` in `Program.cs` con `Anthropic:MaxOutputTokens` = 8000 in `appsettings.json`. Reti di sicurezza in `BookAgent.AskAsync`: un secondo tentativo se il testo è vuoto (non se è stata creata una proposta), poi una chiamata senza strumenti con i dati già letti, infine un messaggio di cortesia. Provato: 6 prospetti su 6 completi (9-14 s) | 2026-11-03 |
| 17 | `SetChallengeGoal` scriveva senza conferma nel codice (il "chiedi prima" era solo un'istruzione al modello) | Sostituito da `ProposeChallengeGoal`, che crea solo una `PendingAction`; la modifica avviene in `PendingActionService.ConfirmAsync` (`POST /api/pending/{id}/confirm`), chiamato dal pulsante Conferma della chat. Provato: prima della conferma la challenge non esiste, dopo sì; confermare/rifiutare due volte → 409, id inesistente → 404 | 2026-11-03 |
| 2 | Mancava il connettore Anthropic: gli agenti non potevano chiamare Haiku | Pacchetto `Microsoft.Agents.AI.Anthropic` 1.27.0; in `Program.cs` un `IChatClient` singleton creato alla prima richiesta (chiave da user-secrets `Anthropic:ApiKey`, modello da `Anthropic:Model`). Verificato: `GET /api/ping-ai` → 200 `{"answer":"Ciao"}` | 2026-11-03 |
| 3 | CORS non configurato nell'API (il browser bloccava le chiamate dal frontend) | `AddCors` con policy `Frontend` (origini `http://localhost:5222` e `https://localhost:7232`) + `app.UseCors("Frontend")` in `Program.cs`. Verificato: l'API risponde con `Access-Control-Allow-Origin`. Se cambiano le porte del Web, aggiornare l'elenco | 2026-11-02 |
| 4 | `HttpClient` del frontend puntava al frontend stesso | `BaseAddress` ora letto da `wwwroot/appsettings.json` → `ApiBaseUrl` (`http://localhost:5219`); se manca la chiave l'app si ferma con un errore chiaro | 2026-11-02 |
| 5 | Codice demo del template (endpoint `/weatherforecast`, record `WeatherForecast`, pagine `Counter`/`Weather`, `sample-data/weather.json`, richiesta nel `.http`) | Eliminato; restano solo `Home.razor` (ancora testo del template, vedi #13) e il layout | 2026-11-02 |
| 1 | Database non creato: nessuna migrazione e `dotnet-ef` non installato (errore "no such table") | Installato `dotnet-ef` 9.0.22, creata la migrazione `InitialCreate` e applicata: `lifeos.db` ora esiste con le 6 tabelle | 2026-11-02 |
| — | Versioni EF Core inserite come `9.*` | Fissate a `9.0.22` | 2026-11-01 |
| 14 | Credenziali Google salvate come `credentials.json.json` (Windows nasconde l'estensione e ne aggiunge una seconda): la regola `credentials.json` del `.gitignore` non le escludeva e sarebbero finite sul repository pubblico | Aggiunte al `.gitignore` le regole `credentials*.json`, `token*.json`, `client_secret*.json`; verificato con `git check-ignore`. Il file non è mai stato committato | 2026-11-01 |
| 7 | Nessun repository git e nessun `.gitignore` | `git init` (branch `main`), `.gitignore` standard .NET + esclusione di `*.db`, `*.db-shm`, `*.db-wal`; repository pubblico su GitHub | 2026-11-01 |

---

## 10. Istruzioni specifiche per assistenti IA

> **Perché questa sezione:** regole operative per Claude o altre IA che modificano il progetto.

- Leggi questo file per intero prima di iniziare; per dettagli verifica sempre il codice reale (il codice ha la precedenza se c'è discrepanza, e in quel caso correggi questo file).
- Rispondi all'utente **in italiano**.
- Dopo ogni modifica al codice esegui `dotnet build LifeOS.sln` e riporta onestamente eventuali errori o avvisi.
- Non aggiungere segreti in file del progetto; usa user-secrets. **Il repository GitHub è pubblico**: tutto ciò che viene committato è visibile a chiunque, quindi niente chiavi, dati personali o file `.db`.
- Prima di ogni commit esegui `git status` e verifica che non compaiano file di credenziali (`credentials*.json`, `token*.json`, `client_secret*.json`, `*.db`); se compaiono, fermati e aggiorna il `.gitignore`.
- Fai commit solo quando l'utente lo chiede; messaggi di commit brevi e in italiano.
- Non aggiornare pacchetti a una versione maggiore (es. 9 → 10) senza chiedere.
- Dopo una modifica ai modelli, ricorda di creare la migrazione (o segnala che manca).
- Non eliminare `lifeos.db` né dati dell'utente senza conferma esplicita.
- Proponi le modifiche in passi piccoli e verificabili; l'utente sta imparando, quindi spiega brevemente cosa fai e perché.
- **Aggiorna questo file** secondo la §0.

---

## 11. Registro modifiche

> **Perché questa sezione:** una cronologia breve di cosa è cambiato e quando, utile finché non c'è git (e anche dopo, come riassunto leggibile).

| Data | Modifica |
|---|---|
| 2026-11-01 | Creata la soluzione con `LifeOS.Api` e `LifeOS.Web` (template .NET 9). |
| 2026-11-01 | Aggiunti pacchetti EF Core SQLite/Design 9.0.22 e Microsoft.Agents.AI 1.27.0. |
| 2026-11-01 | Create cartelle `Agents/`, `Models/`, `Data/`, `Services/`; modelli `MealLog`, `MenstrualCycle`, `Book`, `Friend`, `Course`, `StudySession`; `LifeOSDbContext` registrato con SQLite. |
| 2026-11-01 | Riscritto `CLAUDE.md` come guida completa in italiano. Compilazione: 0 errori, 0 avvisi. |
| 2026-11-01 | Aggiunti commenti esplicativi in italiano a tutto il codice di `LifeOS.Api` e `LifeOS.Web`; aggiunta in §7 la regola sui commenti obbligatori. |
| 2026-11-01 | Inizializzato git con `.gitignore`; progetto collegato a un repository **pubblico** su GitHub. |
| 2026-11-01 | Aggiunta §6.1 sul frontend Blazor (ruolo di HTML/CSS/C#, tipi di file, regole); aggiunti in §5 i comandi git. |
| 2026-11-02 | Installato `dotnet-ef` 9.0.22; creata e applicata la migrazione `InitialCreate` (cartella `Data/Migrations/`); creato `lifeos.db`. Risolto problema #1. Build: 0 errori, 0 avvisi. |
| 2026-11-03 | Aggiunti pacchetto CsvHelper 33.0.1, `Services/GoodreadsImportService.cs` e endpoint `POST /api/books/import` (riceve il CSV, aggiunge/aggiorna i libri per `GoodreadsId`, risponde con aggiunti/aggiornati/saltati). Build: 0 errori, 0 avvisi. |
| 2026-11-03 | Provato l'import con il CSV reale: 619 libri (DB: 620 con un libro inserito a mano). Bug trovato e corretto: i voti decimali del CSV (`3.5`) erano letti come interi e finivano a 0. `MyRating` ora `decimal`, migrazione `MakeBookRatingDecimal` applicata, CSV reimportato (253 libri con voto). Un secondo import non crea duplicati. Build: 0 errori, 0 avvisi. |
| 2026-11-03 | Aggiunto in `/books` il selettore "Importa da Goodreads" (`InputFile`, limite 10 MB) che invia il CSV a `POST /api/books/import` e mostra il riepilogo; nuovo `Web/Models/ImportResultItem.cs` (copia di `ImportResult` dell'API: se cambia, aggiornarla a mano, vedi #8). Build: 0 errori, 0 avvisi. **Non ancora provato nel browser.** |
| 2026-11-03 | Commit `f59cd91` (import CSV, voto decimale, pulsante). Creata la chiave API Anthropic (scadenza 90 giorni, circa 2027-02-01) e salvata con `dotnet user-secrets` (`UserSecretsId` aggiunto a `LifeOS.Api.csproj`: non è un segreto). Aggiunto `Microsoft.Agents.AI.Anthropic` 1.27.0. Build: 0 errori, 0 avvisi. |
| 2026-11-03 | Registrato l'`IChatClient` Anthropic in `Program.cs`, aggiunto `Anthropic:Model` (`claude-haiku-5-5`) in `appsettings.json`, creato `Endpoints/AiEndpoints.cs` con `GET /api/ping-ai` (prova temporanea). Provato: Haiku risponde. Risolto #2; aperti #15 (eliminare la prova) e #16 (scadenza chiave). Build: 0 errori, 0 avvisi. |
| 2026-11-03 | Primo agente: `Agents/BookAgent.cs` (istruzioni in italiano, data di oggi, "usa sempre gli strumenti") e `Agents/BookTools.cs` con due strumenti di sola lettura su `Books` (`GetYearSummary`, `GetBooksByStatus`); endpoint `GET /api/agents/books?question=...`. Provato con domande vere: "libri letti nel 2025" → 36 libri, 8130 pagine, voto medio 3,92, numeri **verificati sul CSV**; "libri in lettura" → elenco corretto. Build: 0 errori, 0 avvisi. |
| 2026-11-03 | Reading challenge: modello `ReadingChallenge` (`Year` unico, `TargetBooks`), `DbSet` e indice in `LifeOSDbContext`, migrazione `AddReadingChallenge` applicata. Nuovi strumenti in `BookTools`: `GetChallengeProgress` (obiettivo, letti, ritmo atteso a oggi, differenza) e `SetChallengeGoal` (**primo strumento che scrive nel DB**, validato 1–1000). Istruzioni di `BookAgent` aggiornate. Provato in sola lettura: senza obiettivo l'agente lo chiede invece di inventarlo. Build: 0 errori, 0 avvisi. |
| 2026-11-03 | Interfaccia di chat: nuova pagina `Pages/Chat.razor` (`/chat`) con `Chat.razor.css`, link "Chat" nel menu, modelli `ChatMessageItem` e `ChatReplyItem`, pacchetto Markdig 1.5.1. Backend: nuova cartella `Dtos/` con `ChatMessageDto`; `BookAgent.AskAsync` ora riceve la conversazione; `/api/agents/books` passa da `GET` a `POST` con validazione (ruoli, lunghezza, ultimi 20 messaggi). Provato via curl: la memoria funziona ("E nel 2024?" capita) e i dati non validi danno 400. Build: 0 errori, 0 avvisi. **La pagina non è ancora stata provata nel browser.** |
| 2026-11-03 | Scritta la §12 (specifica dell'agente Libri: cosa deve saper fare, principi, fasi, punti aperti). **Solo documentazione: nessuna modifica al codice.** Aperto #17 (`SetChallengeGoal` non rispetta ancora la regola "proponi e conferma"). |
| 2026-11-03 | Ampliata la §12 con le note sulle challenge (numero 60 nel 2026, alfabeto, competitiva con Fra), gerarchia `Challenge` con sottoclassi, confronto con Fra, prospetti a meno pagine possibili; aggiunta §12.7 (Home e data visualization). **Solo documentazione.** |
| 2026-11-03 | Fase 1a: meccanismo proponi/conferma. Nuovi `Models/PendingAction.cs`, `Services/PendingActionService.cs`, `Endpoints/PendingEndpoints.cs` (`POST /api/pending/{id}/confirm` e `/reject`), `Dtos/ProposalDto.cs` (+`AgentReply`), migrazione `AddPendingActions` applicata. `SetChallengeGoal` → `ProposeChallengeGoal` (non scrive); `BookAgent.AskAsync` ora restituisce `AgentReply` e `/api/agents/books` risponde `{answer, proposals}`. Frontend: `ProposalItem`, pulsanti Conferma/Rifiuta in `Chat.razor`. Provato via API (dati di prova rimossi dal DB). Risolto #17. Build: 0 errori, 0 avvisi. |
| 2026-11-03 | Corretto il bug delle risposte vuote dell'agente Libri (#18, vedi "Risolti"): nuovi tentativi in `BookAgent.AskAsync`. Build: 0 errori, 0 avvisi. |
| 2026-11-04 | Sfida con Fra completata con le regole dell'utente: punti 0,5–10 (5 di default); autori ricavati dai titoli (ortografia di Goodreads dove il libro è in libreria; vuoti per Delitto e castigo, Il massacro di Dio, L'energetismo segreto della vita); voce "Un libro qualsiasi di Isaac Asimov" (`AnyBookByAuthor`); aggiunti "La vita istruzioni per l'uso" (5) e "M. Il figlio del secolo" (8), ignorato "L'invenzione di Morel"; Ben-Hur con autore Lew Wallace (la versione per ragazzi letta dall'utente non vale). `BookMatching` ora accetta titoli che iniziano/finiscono con quello della lista se l'autore coincide. Date facoltative per i libri di Fra (`ReaderBook.DateAdded`). Registrati i 9 libri di Fra senza data. Migrazione `ListItemAnyAuthorAndReaderDateAdded` applicata. Verifica: Eli 26, Fra 39 (il foglio dell'utente dice 37,5 ma la somma dei suoi 9 libri è 39). Lista: 58 libri, 273 punti. Build: 0 errori, 0 avvisi. |
| 2026-11-04 | `ChallengeListItem.Points` da `int` a `decimal` (0,5–100: i punteggi reali sono 1,5, 6,5…) e `Author` facoltativo (la lista reale ha solo titoli: si confronta per titolo). Migrazione `DecimalChallengePoints` applicata (tabella vuota, nessun dato perso). Caricata con uno script (su richiesta dell'utente, non dall'agente) la **Sfida 2026 contro Fra**: 56 libri, 260 punti totali; 3 libri senza punteggio esclusi. Verifica: il codice riconosce da solo i 7 libri dell'utente (26 punti, come nel suo foglio) più Ben-Hur (letto il 2026-01-29, da chiarire). Letture di Fra ancora da inserire (servono le date). Build: 0 errori, 0 avvisi. |
| 2026-11-04 | Fase 5a, sfida competitiva: nuovi modelli `CompetitiveChallenge`, `ChallengeListItem`, `Reader`, `ReaderBook`, `IReadingRecord`, `ChallengeContext`, `BookMatching`; `Challenge.Evaluate` ora riceve un `ChallengeContext` (tuoi libri + libri delle avversarie + oggi). Nuovi `PendingActionType`: CreateCompetitiveChallenge, AddChallengeItem, AddReaderBook. Strumenti: `GetReaderYearSummary`, `GetCatchUpOptions`, `ProposeCompetitiveChallenge`, `ProposeChallengeItem`, `ProposeReaderBook`. `GoodreadsImportService` diviso in lettura righe + `ImportAsync`/`ImportForReaderAsync`; nuovo `Endpoints/ReaderEndpoints.cs`; in `/books` import del CSV di un'altra persona. Migrazione `CompetitiveChallenge` applicata. Provato via API (sfida 2025 contro "Prova": 3 libri in lista, 2 letture, punteggio 8–15, piano di recupero corretto); dati di prova cancellati. Aperti #21, #22. Build: 0 errori, 0 avvisi. |
| 2026-11-03 | Fase 1b: nuovi `Models/Challenge.cs` (astratta), `CountChallenge.cs`, `AlphabetChallenge.cs`, `ChallengeProgress.cs`; eliminato `Models/ReadingChallenge.cs`. `Book.AuthorLastFirst` letto dalla colonna "Author l-f" nell'import. Nuovo `PendingActionType.CreateAlphabetChallenge`. Strumenti: `GetChallengeProgress` (tutte le challenge dell'anno), `GetBooksForMissingLetters`, `ProposeAlphabetChallenge`. Migrazione `ChallengeHierarchy` (modificata a mano, §8) applicata: 60/2026 conservato. Provato via API: proposta → conferma → creata la challenge "Alfabeto 2026" (15/26 lettere); consigli per le lettere mancanti corretti. Aperto #20. Build: 0 errori, 0 avvisi. |
| 2026-11-03 | Trovata la causa vera delle risposte vuote (#18): limite di token troppo basso. Aggiunto `Anthropic:MaxOutputTokens` (8000) e una chiamata di riserva senza strumenti in `BookAgent`. Build: 0 errori, 0 avvisi. |
| 2026-11-03 | Corretto l'errore 400 della chat con cronologia vuota/lunga (#19): validazione in `AiEndpoints.cs` resa tollerante sulla cronologia. Build: 0 errori, 0 avvisi. |
| 2026-11-02 | Risolti problemi #3 (CORS) e #4 (`HttpClient`); creati endpoint `GET`/`POST /api/books` (`Endpoints/BookEndpoints.cs`), pagina `Books.razor` (`/books`) con `Models/BookItem.cs`, link "Libri" nel menu; rimosso il codice demo (#5). Verificato: API avviata, `GET /api/books` → 200 con CORS corretto. Build: 0 errori, 0 avvisi. |

---

## 12. Specifica degli agenti (cosa vogliamo che facciano)

> **Perché questa sezione:** raccoglie le idee e le decisioni di *prodotto* prima di scrivere il codice, così non vanno perse. È una **specifica, non lo stato dei lavori**: ciò che è scritto qui non è (ancora) implementato, salvo dove indicato. Per lo stato reale vedi §1, §9 e §11.
> Ogni agente nuovo va descritto con lo stesso schema: domande che deve saper gestire, autonomia, fonti, fasi, punti aperti.

### 12.1 Principi comuni a tutti gli agenti

1. **Giudizio all'IA, conti al codice.** Claude decide, consiglia, scrive; i calcoli (punteggi, conteggi, lettere coperte, ritmo di lettura, confronti) li fanno strumenti C# deterministici. L'agente chiama lo strumento e commenta il risultato: i modelli linguistici sbagliano i conti.
2. **Proponi e conferma.** L'agente **non scrive mai** nel database di sua iniziativa. Uno strumento di scrittura crea solo una **proposta in sospeso** ("aggiungo la challenge X?") e la modifica avviene solo quando l'utente preme **Conferma** nell'interfaccia. Il permesso è quindi una regola del codice e non un'istruzione al modello (che potrebbe non rispettarla). Eccezioni: nessuna senza decisione esplicita dell'utente.
3. **Proattivo ma non invadente.** I suggerimenti spontanei partono da un processo pianificato (un agente non si sveglia da solo), con: massimo **un suggerimento a settimana**, solo se c'è qualcosa di notevole (per esempio ritardo oltre una soglia), e un'impostazione per spegnerlo. I suggerimenti si salvano e si mostrano in una sezione dell'app; non interrompono.
4. **Strumenti minimi.** Ogni agente ha solo gli strumenti che gli servono (§6). Preferire strumenti di sola lettura; quelli che scrivono toccano un campo preciso, con controlli (vedi regola 2).
5. **Dati sensibili.** Vale il problema #11: all'IA solo i dati minimi necessari.

### 12.2 Agente Libri — domande che deve saper gestire

*Consigli e scelte*
- "Che libro leggo dopo?" · "Consigliami qualcosa di breve per questo weekend."
- "In base ai libri che mi sono piaciuti, cosa mi consigli?"
- "Consigliami qualcosa di simile a Lorenz, Sacks, Feynman, Peter Smith…"
- "Dimmi 15 libri non famosi e belli, anche in base ai miei gusti."
- "Consigliami altre challenge."

*Challenge e pianificazione*
- "Sono in pari con la challenge?"
- "Fammi un prospetto del prossimo mese per andare in pari con la challenge del numero di libri, integrando libri che mi facciano avanzare anche nelle altre challenge."
- "Quali libri brevi e veloci posso leggere per vincere la challenge X, abbinandoli a libri corposi per vincere le altre?"
- "Fammi un prospetto di lettura per il 2027 e consigliami il nuovo numero di libri per la challenge."
- "Aggiungi questa challenge: …" (con conferma, regola 2). Tipi previsti:
  - **a numero** (es. N libri nell'anno; fatto: `CountChallenge`);
  - **a punti**: lista di libri, ognuno con un punteggio;
  - **competitiva**: a punti contro un'altra persona, di cui l'utente ha le statistiche di lettura;
  - **alfabeto**: un autore per ogni lettera.
- **Dettagli aggiunti il 2026-11-03** (volontà dell'utente):
  - Challenge concrete: (a) **numero di libri** (per il 2026: **60**); (b) **alfabeto** (un autore con iniziale diversa per ogni lettera); (c) **competitiva con "Fra"** (un altro utente): ci si sfida a leggere **più libri possibile da una lista fissata**; dell'utente servono le **statistiche** di Fra.
  - **Modello dati (idea):** una classe base `Challenge` con una sottoclasse per tipo (`CountChallenge`, `AlphabetChallenge`, `CompetitiveChallenge`…), con metodo astratto `ObiettivoRaggiunto()` (in codice: `IsGoalReached()`); a livello di DB, ereditarietà EF Core (TPH, colonna "tipo"). Lo stato di ogni challenge lo calcola il codice (regola 12.1.1).
  - **Inserimento libero via chat:** l'utente può creare/modificare challenge in linguaggio naturale; l'agente le **propone** e le salva solo dopo conferma (regola 12.1.2).
  - **Confronto con Fra:** statistiche di Fra vs. dell'utente, anche con **visualizzazioni grafiche**; domande tipo "quanto sono indietro rispetto a lui?" e "fammi un prospetto per recuperare".
  - **Prospetto ottimizzato:** "libri da leggere il prossimo mese per recuperare **tutte** le challenge leggendo **meno pagine possibili**" (calcolo di ottimizzazione fatto dal codice, l'agente lo commenta).
  - **Libri di Fra:** poter **aggiungere libri alla lista/letture di Fra** dalla chat (sempre con conferma). Serve un modo per rappresentare le letture di Fra (es. `Reader`/`Participant` + sue letture).

### 12.3 Autonomia, fonti e tono

- **Autonomia:** risponde e **propone**; scrive solo dopo conferma (12.1 punto 2). Nessuna azione irreversibile.
- **Proattività:** sì, con i limiti di 12.1 punto 3 (prospetti, avvisi di ritardo, riepilogo mensile e di fine anno).
- **Fonti interne:** `Books` (anche i circa 337 libri "da leggere"), voti, date, challenge.
- **Fonti esterne:** deve poter fare ricerche su internet, basandosi soprattutto su blogger e youtuber indicati dall'utente (tra cui **Matteo Fumagalli**). Limite: l'agente legge testo e non guarda i video, quindi per YouTube servono trascrizioni o pagine di accompagnamento; **da verificare** cosa pubblica ogni fonte in forma leggibile prima di promettere il suo uso.
- **Metadati dei libri** (genere, trama, temi): il CSV di Goodreads non li contiene; valutare una fonte gratuita come Open Library o Google Books, più economica e precisa di una ricerca web a ogni richiesta.

### 12.4 Funzioni aggiuntive concordate (tutte volute dall'utente)

1. **Prima la pila esistente:** prima di consigliare libri nuovi, proporre dai "da leggere" già posseduti.
2. **Profilo dei gusti** ricavato dai voti (autori e temi amati o bocciati), usato in tutti i consigli.
3. **Libri abbandonati:** un modo per segnarli, per non riproporre cose simili.
4. **Riepilogo mensile e di fine anno** ("il tuo anno di lettura").
5. **Varietà:** avvisare se si leggono sempre lo stesso genere, gli stessi autori o paesi (utile per le challenge tipo alfabeto).
6. **Collegamento col calendario** (futuro, con l'agente Sociale/orchestratore): proporre il libro breve per il weekend libero.

### 12.5 Fasi di sviluppo (ognuna utilizzabile da sola)

| Fase | Contenuto | Stato |
|---|---|---|
| 1a | Meccanismo **proponi/conferma** con pulsanti nella chat (`PendingAction`, `PendingActionService`, `/api/pending`) | **fatto** (2026-11-03; pulsanti nel browser non ancora provati a mano) |
| 1b | Challenge multiple: classe base `Challenge` + `CountChallenge` e `AlphabetChallenge` in tabella unica (TPH), migrazione dei dati, strumenti `GetChallengeProgress` (tutte le challenge dell'anno), `GetBooksForMissingLetters`, `ProposeAlphabetChallenge` | **fatto** (2026-11-03). La `CompetitiveChallenge` è rimandata alla fase 5 perché richiede i dati di Fra |
| 2 | Consigli dai libri "da leggere" + profilo dei gusti + libri abbandonati | da fare |
| 3 | Fonti esterne: metadati (Open Library/Google Books), poi ricerche web su siti scelti | da fare |
| 4 | Proattività: processo settimanale, sezione "suggerimenti" nell'app, impostazione per spegnerla | da fare |
| 5a | Sfida competitiva: `CompetitiveChallenge`, `Reader`/`ReaderBook`, lista con punti; dalla chat (con conferma) si crea la sfida, si aggiungono libri alla lista e letture di Fra; import CSV di Fra in `/books`; strumenti `GetReaderYearSummary`, `GetCatchUpOptions` (piano "goloso" per superarla con meno pagine) | **fatto** (2026-11-04, provato via API con dati di prova poi cancellati) |
| 5b | Grafici: statistiche tue e di Fra a confronto (pagina dedicata) | da fare |
| V | **Data visualization e Home** (§12.7): percorso separato, indipendente dall'agente Libri | da fare |
| — | Già fatto prima della specifica: agente con 4 strumenti (riepilogo annuale, libri per stato, avanzamento challenge a numero, salva obiettivo), pagina `/chat` con memoria della conversazione | fatto (§11) |

### 12.6 Punti aperti (da decidere prima della fase 1)

- **Modello dati delle challenge:** una tabella `Challenge` con tipo e regole (in JSON o campi) più una tabella di collegamento con i libri/punteggi? Come si rappresenta l'altra persona della sfida competitiva e le sue statistiche (inserite a mano? CSV suo?).
- **Pulsante Conferma:** come si salvano le proposte in sospeso (tabella `PendingAction`?) e come la chat le mostra.
- ~~**Un autore per lettera**~~ → deciso il 2026-11-03: iniziale del **cognome** (da "Author l-f" di Goodreads), **26 lettere A-Z**, accenti ignorati.
- **Costi:** le ricerche web e i prospetti lunghi consumano molti token; fissare un limite mensile in console Anthropic e valutare un modello più capace solo dove serve.
- ~~**Dati di Fra**~~ → deciso il 2026-11-04: **chat + CSV** di Goodreads; punteggio **per libro** (1–100, deciso quando lo si aggiunge); durata **anno solare**; la lista vive in `ChallengeListItems`.
- ~~**Ereditarietà EF**~~ → deciso il 2026-11-03: **tabella unica (TPH)**, perché le challenge hanno pochi campi specifici; si può passare a tabelle separate con una migrazione se diventasse scomoda.

### 12.7 Data visualization e Home "recap della vita" (discorso separato)

- **Obiettivo:** una **Home** che dà un recap della vita: sezioni per libri, sport, cibo, sociale, studio, con **grafici e tabelle**. Da sviluppare prima o dopo l'agente Libri: le due cose sono indipendenti.
- **Libri (primo candidato):** libri letti per mese/anno, pagine, voti, avanzamento challenge, confronto con Fra.
- **Altri ambiti:** si aggiungono quando esistono i dati (cibo, ciclo, studio…); attenzione ai dati sensibili (#11) solo se passano dall'IA: i grafici sono calcolati dal codice e non richiedono l'IA.
- **Da decidere:** libreria grafici per Blazor WebAssembly (nessun JavaScript a mano, vedi §6.1: valutare librerie .NET come ApexCharts.Blazor o MudBlazor, oppure SVG scritto in Razor); endpoint API dedicati di sola lettura (`/api/stats/...`).
