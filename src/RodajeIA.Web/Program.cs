using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Components;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Generacion;
using RodajeIA.Web.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<RodajeDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Rodaje")));

builder.Services.AddScoped<SeriesService>();
builder.Services.AddScoped<PersonajesService>();
builder.Services.AddScoped<EpisodiosService>();
builder.Services.AddScoped<CargaGuionService>();
builder.Services.AddScoped<VariantesService>();
builder.Services.AddScoped<PromptFinalService>();

// Generación con Gemini (RF-06, RF-10): corre en segundo plano, fuera del circuito de Blazor.
builder.Services.Configure<OpcionesGemini>(builder.Configuration.GetSection("Gemini"));
builder.Services.AddSingleton(ConfiguracionGeneracion.Cargar());
builder.Services.AddSingleton(new OpcionesGeneracion());
builder.Services.AddSingleton<IClienteGemini, ClienteGemini>();
builder.Services.AddSingleton<AvisosGeneracion>();
builder.Services.AddSingleton<GeneradorEscenas>();
builder.Services.AddSingleton<ColaGeneracion>();
builder.Services.AddHostedService<ProcesadorGeneracion>();

var app = builder.Build();

await app.Services.GetRequiredService<ColaGeneracion>().MarcarInterrumpidasAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
