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
// ATTENZIONE: BaseAddress punta al sito del frontend stesso, non all'API (vedi CLAUDE.md §9 punto 4).
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Costruisce l'app e la avvia nel browser.
await builder.Build().RunAsync();
