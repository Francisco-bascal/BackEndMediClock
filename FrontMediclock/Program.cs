using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FrontEndMediClock;
using FrontEndMediClock.Servicios;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// URL base configurable: wwwroot/appsettings.json (MediClock:BaseUrl).
// Si es "/" o vacia, se usa el origen real del app (builder.HostEnvironment.BaseAddress),
// de modo que el front hable con el backend que lo sirve (hosteado o local),
// sin depender de una URL absoluta. Un valor absoluto (ej. dev) se respeta.
var configuredBaseUrl = builder.Configuration["MediClock:BaseUrl"];
var baseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl) || configuredBaseUrl == "/"
    ? builder.HostEnvironment.BaseAddress
    : configuredBaseUrl;
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") });
builder.Services.AddScoped<ApiMediClock>();
builder.Services.AddScoped<EstadoDispositivo>();
builder.Services.AddScoped<PreferenciasLocales>();

await builder.Build().RunAsync();
