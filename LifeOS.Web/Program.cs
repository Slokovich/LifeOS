// Program.cs — punto di avvio del frontend LifeOS.Web (Blazor WebAssembly).
// Questo codice gira nel BROWSER dell'utente, non sul server.

using Microsoft.AspNetCore.Components.Web;                 // HeadOutlet
using Microsoft.AspNetCore.Components.WebAssembly.Hosting; // WebAssemblyHostBuilder
using LifeOS.Web;                                          // App (il componente principale, App.razor)

// Crea il "cantiere" dell'app Blazor con le impostazioni di default.
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Inserisce il componente App dentro <div id="app"> di wwwroot/index.html:
// da lì in poi è Blazor a disegnare la pagina.
builder.RootComponents.Add<App>("#app");

// Permette alle pagine di modificare il <head> dell'HTML (es. il titolo con <PageTitle>).
builder.RootComponents.Add<HeadOutlet>("head::after");

// Registra HttpClient, usato dalle pagine per fare richieste HTTP (@inject HttpClient Http).
// BaseAddress = indirizzo dell'API (LifeOS.Api), letto da wwwroot/appsettings.json (chiave "ApiBaseUrl").
// Così nelle pagine basta scrivere Http.GetFromJsonAsync("api/books") e la richiesta va all'API.
// Se la chiave manca, l'app si ferma subito con un messaggio chiaro invece di chiamare l'indirizzo sbagliato.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("Manca 'ApiBaseUrl' in wwwroot/appsettings.json");
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// Costruisce l'app e la avvia nel browser.
await builder.Build().RunAsync();
