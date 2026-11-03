# LifeOS — Guida al progetto

> **A cosa serve questo file.** È il punto di partenza per chiunque lavori su LifeOS: persone (anche non esperte di programmazione) e assistenti IA (Claude Code, Copilot, ecc.). Spiega cos'è il progetto, com'è organizzato, come avviarlo, quali scelte sono state fatte e quali problemi sono noti.
> Claude Code lo legge automaticamente all'inizio di ogni sessione, quindi tutto ciò che è scritto qui diventa "contesto" per l'IA.

**Ultimo aggiornamento:** 2026-11-03 (sera, dopo l'aggiunta del connettore Anthropic)
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
| **Libri** | `Book` (import CSV Goodreads) | Gestisce la reading challenge annuale | da fare |
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
| Modello IA | Anthropic API, Claude Haiku | — | connettore: `Microsoft.Agents.AI.Anthropic` 1.27.0 (porta l'SDK ufficiale `Anthropic` 12.53.0 e `Microsoft.Extensions.AI` 10.10.0); registrato in `Program.cs` come `IChatClient`, modello `claude-haiku-5-5` (in `appsettings.json` → `Anthropic:Model`); provato con `/api/ping-ai` |
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
│   ├── Agents/                ← (vuota) un file per agente + orchestratore
│   ├── Endpoints/             ← un file per area con gli endpoint HTTP (oggi: BookEndpoints.cs → /api/books e /api/books/import; AiEndpoints.cs → /api/ping-ai, prova temporanea da eliminare)
│   ├── Models/                ← classi che rappresentano le tabelle del DB
│   ├── Data/                  ← LifeOSDbContext + Migrations/ (migrazioni generate da EF: non modificarle a mano)
│   └── Services/              ← integrazioni esterne (oggi: GoodreadsImportService.cs = import CSV); in futuro Google Calendar, client Anthropic
│
└── LifeOS.Web/                ← FRONTEND (Blazor WebAssembly, ancora template di default)
    ├── Program.cs             ← avvio dell'app nel browser, registra HttpClient
    ├── App.razor              ← router: decide quale pagina mostrare per ogni URL
    ├── _Imports.razor         ← "using" comuni a tutte le pagine
    ├── Layout/                ← struttura della pagina (menu laterale, layout)
    ├── Models/                ← classi dei dati come li vede il frontend (oggi: BookItem, ImportResultItem)
    ├── Pages/                 ← pagine: Home (template), Books (/books: elenco, aggiunta libri e import CSV Goodreads)
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
| `Book` | `Books` | Libri | `GoodreadsId?`, `Title`, `Author`, `Isbn13?`, `Pages?`, `MyRating` (`decimal` 0–5, anche 3.5 o 2.25), `Status` (ToRead/Reading/Read), `DateAdded?`, `DateRead?` | `GoodreadsId` unico → evita duplicati reimportando il CSV |
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
| 2026-11-03 | `.DisableAntiforgery()` su `POST /api/books/import` | .NET richiede un token antiforgery per i moduli con file; qui non servono perché l'API non usa cookie di login e accetta solo il frontend (CORS). Da rivedere se si aggiunge l'autenticazione. |
| 2026-11-02 | Indirizzo dell'API in `LifeOS.Web/wwwroot/appsettings.json`, non nel codice | Si cambia (es. in produzione) senza toccare il C#. Non metterci segreti: il file è pubblico, scaricato dal browser. |

---

## 9. Problemi noti, bug e soluzioni

> **Perché questa sezione:** raccoglie ciò che non funziona ancora o che potrebbe dare problemi, con la soluzione prevista. Prima di segnalare un "bug nuovo", controlla qui.

### Aperti

| # | Problema | Effetto | Soluzione proposta |
|---|---|---|---|
| 15 | **Endpoint di prova `GET /api/ping-ai`** (`Endpoints/AiEndpoints.cs`) ancora presente: chiunque raggiunga l'API può farlo chiamare e far spendere token. | Costo minimo ora, ma inutile una volta pronto il primo agente. | Eliminarlo (file + `app.MapAiEndpoints()` in `Program.cs`) quando il primo agente funziona. |
| 16 | **Scadenza della chiave API Anthropic** circa 2027-02-01. | Dopo quella data le chiamate IA falliscono (errore 401). | Creare una nuova chiave e rieseguire `dotnet user-secrets set "Anthropic:ApiKey" ... --project LifeOS.Api`. |
| 6 | **`UseHttpsRedirection` con profilo `http`**. | Avviso nei log "Failed to determine the https port"; nessun effetto pratico. | Avviare con `--launch-profile https` oppure attivare il redirect solo fuori da Development. |
| 8 | **Modelli condivisi assenti**: i modelli stanno solo in `LifeOS.Api`. | Il frontend ridefinisce le stesse classi (già successo con `Web/Models/BookItem`, copia semplificata di `Book`: se cambia `Book`, aggiornarla a mano). | Creare un progetto `LifeOS.Shared` per i DTO usati da entrambi. |
| 9 | **Modelli mancanti** per alcune funzioni: obiettivo annuale della reading challenge, allenamenti pianificati, sedute dallo psicologo (oggi previste solo da calendario). | Gli agenti non hanno dove salvare questi dati. | Valutare `ReadingChallenge` (anno, obiettivo), `Workout`, eventuale `TherapySession`. |
| 10 | **Integrazione Google Calendar assente**. | Agenti Sport, Cibo e Sociale non possono leggere gli impegni. | Pacchetto `Google.Apis.Calendar.v3` + OAuth, servizio in `Services/`. |
| 11 | **Dati sensibili** (ciclo mestruale, salute, terapia) inviati all'API Anthropic. | Questione di privacy. | Inviare all'IA solo i dati minimi necessari (es. "fase del ciclo: follicolare" invece dello storico completo). Non esporre l'API su internet senza autenticazione. |
| 12 | **Nessun test automatico**. | Le regressioni si scoprono solo a mano. | Aggiungere un progetto `LifeOS.Api.Tests` (xUnit) quando c'è logica da testare. |
| 13 | **Frontend non personalizzato**: titolo "LifeOS.Web", `lang="en"`, testi in inglese. | Aspetto da template. | Aggiornare `index.html` (`lang="it"`, titolo "LifeOS") e i testi. |

### Risolti

| # | Problema | Soluzione | Data |
|---|---|---|---|
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
| 2026-11-02 | Risolti problemi #3 (CORS) e #4 (`HttpClient`); creati endpoint `GET`/`POST /api/books` (`Endpoints/BookEndpoints.cs`), pagina `Books.razor` (`/books`) con `Models/BookItem.cs`, link "Libri" nel menu; rimosso il codice demo (#5). Verificato: API avviata, `GET /api/books` → 200 con CORS corretto. Build: 0 errori, 0 avvisi. |
