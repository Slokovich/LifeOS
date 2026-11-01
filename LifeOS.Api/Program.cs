// Program.cs — punto di avvio del backend LifeOS.Api.
// Quando esegui `dotnet run`, il programma parte da qui ed esegue le istruzioni dall'alto in basso:
//   1) prepara i servizi (database, documentazione, ...)
//   2) costruisce l'applicazione
//   3) definisce come rispondere alle richieste HTTP
//   4) avvia il server

// "using" rende disponibili classi di altri namespace senza scriverne il nome completo.
using LifeOS.Api.Data;                // contiene LifeOSDbContext (il nostro database)
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

// ─── ENDPOINT DEMO DEL TEMPLATE (da eliminare, vedi CLAUDE.md §9 punto 5) ──────────
// Elenco di descrizioni del meteo tra cui sceglierne una a caso.
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// Risponde a GET /weatherforecast con 5 previsioni inventate (una per ciascuno dei prossimi 5 giorni).
app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>   // numeri da 1 a 5 = giorni da oggi
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)), // data: oggi + index giorni
            Random.Shared.Next(-20, 55),                        // temperatura casuale tra -20 e 54 °C
            summaries[Random.Shared.Next(summaries.Length)]     // descrizione casuale
        ))
        .ToArray();
    return forecast;  // viene convertito automaticamente in JSON
})
.WithName("GetWeatherForecast");  // nome dell'endpoint nella documentazione OpenAPI

// ─── 4. AVVIO ───────────────────────────────────────────────────────────────────
// Avvia il server e resta in ascolto (http://localhost:5219) finché non premi Ctrl+C.
app.Run();

// Struttura dei dati restituiti da /weatherforecast (demo del template).
// "record" = classe compatta pensata per contenere dati.
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    // Temperatura in Fahrenheit calcolata da quella in Celsius.
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
