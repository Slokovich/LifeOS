# LifeOS — Guida al progetto

> **A cosa serve questo file.** È il punto di partenza per chiunque lavori su LifeOS: persone (anche non esperte di programmazione) e assistenti IA (Claude Code, Copilot, ecc.). Spiega cos'è il progetto, com'è organizzato, come avviarlo, quali scelte sono state fatte e quali problemi sono noti.
> Claude Code lo legge automaticamente all'inizio di ogni sessione, quindi tutto ciò che è scritto qui diventa "contesto" per l'IA.

**Ultimo aggiornamento:** 2026-11-01
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
| Modello IA | Anthropic API, Claude Haiku | — | **connettore non ancora aggiunto** (vedi §9) |
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
│   ├── Models/                ← classi che rappresentano le tabelle del DB
│   ├── Data/                  ← LifeOSDbContext (+ in futuro Migrations/)
│   └── Services/              ← (vuota) integrazioni esterne: Google Calendar, import Goodreads, client Anthropic
│
└── LifeOS.Web/                ← FRONTEND (Blazor WebAssembly, ancora template di default)
    ├── Program.cs             ← avvio dell'app nel browser, registra HttpClient
    ├── App.razor              ← router: decide quale pagina mostrare per ogni URL
    ├── _Imports.razor         ← "using" comuni a tutte le pagine
    ├── Layout/                ← struttura della pagina (menu laterale, layout)
    ├── Pages/                 ← pagine: Home, Counter, Weather (demo del template)
    └── wwwroot/               ← file statici: index.html, CSS, icone, Bootstrap, dati demo
```

`bin/` e `obj/` (in entrambi i progetti) contengono i file generati dalla compilazione: **non vanno modificati né salvati in git**.

### Dove mettere il codice nuovo
- Nuovo agente → `LifeOS.Api/Agents/<Nome>Agent.cs`
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
| `Book` | `Books` | Libri | `GoodreadsId?`, `Title`, `Author`, `Isbn13?`, `Pages?`, `MyRating` (0–5), `Status` (ToRead/Reading/Read), `DateAdded?`, `DateRead?` | `GoodreadsId` unico → evita duplicati reimportando il CSV |
| `Friend` | `Friends` | Sociale | `Name`, `LastSeen?`, `DesiredFrequencyDays?`, `Interests?`, `Notes?` | |
| `Course` | `Courses` | Apprendimento | `Title`, `Topic`, `Platform`, `TotalHours`, `IsCompleted` | ha molte `StudySession` |
| `StudySession` | `StudySessions` | Apprendimento | `CourseId`, `ScheduledStart`, `PlannedMinutes`, `ActualMinutes?`, `IsCompleted`, `Notes?` | eliminando un corso si eliminano le sue sessioni (cascade) |

`?` = campo facoltativo (può essere vuoto).

### Migrazioni
Una **migrazione** è uno script generato automaticamente che crea o modifica le tabelle nel DB per farle corrispondere ai modelli. **Al momento non esiste nessuna migrazione, quindi il database non è ancora stato creato** (vedi §9).

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
# 1. Installare lo strumento EF Core (una sola volta; attualmente NON installato)
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

---

## 9. Problemi noti, bug e soluzioni

> **Perché questa sezione:** raccoglie ciò che non funziona ancora o che potrebbe dare problemi, con la soluzione prevista. Prima di segnalare un "bug nuovo", controlla qui.

### Aperti

| # | Problema | Effetto | Soluzione proposta |
|---|---|---|---|
| 1 | **Database non creato**: nessuna migrazione e `dotnet-ef` non installato. | L'API si avvia, ma qualunque accesso al DB fallisce ("no such table"). | Installare `dotnet-ef` 9.0.22 e creare la migrazione `InitialCreate` (vedi §5). |
| 2 | **Manca il connettore Anthropic**: `Microsoft.Agents.AI` non include un client per Claude. | Gli agenti non possono ancora chiamare Haiku. | Aggiungere un pacchetto client Anthropic compatibile con `Microsoft.Extensions.AI` (`IChatClient`) e registrarlo in `Program.cs`; chiave via user-secrets. |
| 3 | **CORS non configurato** nell'API. | Il browser bloccherà le chiamate da `localhost:5222` (Web) a `localhost:5219` (API). | In `Program.cs`: `AddCors` con origine `http://localhost:5222` / `https://localhost:7232` e `app.UseCors(...)`. |
| 4 | **HttpClient del frontend punta a sé stesso** (`BaseAddress = HostEnvironment.BaseAddress`). | Le chiamate dal frontend vanno al server del frontend, non all'API. | Impostare `BaseAddress` all'URL dell'API, letto da `wwwroot/appsettings.json`. |
| 5 | **Codice demo del template** ancora presente: endpoint `/weatherforecast` e record `WeatherForecast` in `Program.cs`, `LifeOS.Api.http`, pagine `Counter`/`Weather`, `sample-data/weather.json`. | Confusione, codice inutile. | Rimuoverlo quando si creano i primi endpoint/pagine reali. |
| 6 | **`UseHttpsRedirection` con profilo `http`**. | Avviso nei log "Failed to determine the https port"; nessun effetto pratico. | Avviare con `--launch-profile https` oppure attivare il redirect solo fuori da Development. |
| 8 | **Modelli condivisi assenti**: i modelli stanno solo in `LifeOS.Api`. | Il frontend dovrà ridefinire le stesse classi. | Creare un progetto `LifeOS.Shared` per i DTO usati da entrambi. |
| 9 | **Modelli mancanti** per alcune funzioni: obiettivo annuale della reading challenge, allenamenti pianificati, sedute dallo psicologo (oggi previste solo da calendario). | Gli agenti non hanno dove salvare questi dati. | Valutare `ReadingChallenge` (anno, obiettivo), `Workout`, eventuale `TherapySession`. |
| 10 | **Integrazione Google Calendar assente**. | Agenti Sport, Cibo e Sociale non possono leggere gli impegni. | Pacchetto `Google.Apis.Calendar.v3` + OAuth, servizio in `Services/`. |
| 11 | **Dati sensibili** (ciclo mestruale, salute, terapia) inviati all'API Anthropic. | Questione di privacy. | Inviare all'IA solo i dati minimi necessari (es. "fase del ciclo: follicolare" invece dello storico completo). Non esporre l'API su internet senza autenticazione. |
| 12 | **Nessun test automatico**. | Le regressioni si scoprono solo a mano. | Aggiungere un progetto `LifeOS.Api.Tests` (xUnit) quando c'è logica da testare. |
| 13 | **Frontend non personalizzato**: titolo "LifeOS.Web", `lang="en"`, testi in inglese. | Aspetto da template. | Aggiornare `index.html` (`lang="it"`, titolo "LifeOS") e i testi. |

### Risolti

| # | Problema | Soluzione | Data |
|---|---|---|---|
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
