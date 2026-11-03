// Program.cs — punto di avvio del backend LifeOS.Api.
// Quando esegui `dotnet run`, il programma parte da qui ed esegue le istruzioni dall'alto in basso:
//   1) prepara i servizi (database, documentazione, ...)
//   2) costruisce l'applicazione
//   3) definisce come rispondere alle richieste HTTP
//   4) avvia il server

// "using" rende disponibili classi di altri namespace senza scriverne il nome completo.
using System.Text.Json.Serialization; // JsonStringEnumConverter: scrive gli enum come parole nel JSON
using LifeOS.Api.Data;                // contiene LifeOSDbContext (il nostro database)
using LifeOS.Api.Endpoints;           // contiene MapBookEndpoints (gli endpoint dei libri)
using LifeOS.Api.Services;            // contiene GoodreadsImportService (import del CSV)
using Microsoft.EntityFrameworkCore;  // contiene UseSqlite e gli altri strumenti di EF Core

// ─── 1. PREPARAZIONE DEI SERVIZI ────────────────────────────────────────────────
// Il "builder" è il cantiere dell'app: carica la configurazione (appsettings.json,
// user-secrets, variabili d'ambiente) e attiva i log.
var builder = WebApplication.CreateBuilder(args);

// OpenAPI: genera automaticamente una descrizione di tutti gli endpoint dell'API,
// utile per documentarli e provarli. Info: https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Registra il database. Da ora, chi ha bisogno di LifeOSDbContext lo riceve già pronto
// ("dependency injection"). La stringa di connessione "LifeOS" è in appsettings.json
// (Data Source=lifeos.db → file SQLite nella cartella del progetto).
builder.Services.AddDbContext<LifeOSDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("LifeOS")));

// Servizio che importa il CSV di Goodreads. "Scoped" = ne viene creato uno per ogni richiesta,
// come il database che usa.
builder.Services.AddScoped<GoodreadsImportService>();

// JSON: gli enum (es. ReadingStatus) viaggiano come parole ("Read") e non come numeri (2),
// in entrambe le direzioni. È coerente con il database, dove sono salvati come testo (CLAUDE.md §8).
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// CORS: per sicurezza il browser blocca le chiamate da un sito a un altro indirizzo.
// Qui l'API dichiara di fidarsi del frontend LifeOS.Web (stessi indirizzi di launchSettings.json
// del progetto Web), così il browser permette al frontend di chiamarla.
// La policy si chiama "Frontend" e viene attivata più sotto con app.UseCors.
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins("http://localhost:5222", "https://localhost:7232") // solo questi siti
        .AllowAnyHeader()    // qualunque intestazione HTTP (es. Content-Type per il JSON)
        .AllowAnyMethod())); // GET, POST, PUT, DELETE...

// ─── 2. COSTRUZIONE DELL'APP ────────────────────────────────────────────────────
// Chiude il cantiere: da qui in poi non si possono più registrare servizi.
var app = builder.Build();

// ─── 3. PIPELINE DELLE RICHIESTE ────────────────────────────────────────────────
// Ogni richiesta HTTP attraversa, in ordine, i "middleware" aggiunti qui sotto.

// Solo in sviluppo: pubblica la descrizione OpenAPI su /openapi/v1.json.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Reindirizza le richieste http:// verso https://.
// Nota: avviando col profilo "http" compare un avviso nei log (vedi CLAUDE.md §9, punto 6).
app.UseHttpsRedirection();

// Attiva la policy CORS "Frontend" definita sopra. Va messo prima degli endpoint,
// altrimenti le loro risposte non riceverebbero le intestazioni CORS.
app.UseCors("Frontend");

// ─── ENDPOINT ───────────────────────────────────────────────────────────────────
// Libri: GET, POST e import CSV su /api/books (definiti in Endpoints/BookEndpoints.cs).
app.MapBookEndpoints();

// ─── 4. AVVIO ───────────────────────────────────────────────────────────────────
// Avvia il server e resta in ascolto (http://localhost:5219) finché non premi Ctrl+C.
app.Run();
