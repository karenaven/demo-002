using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Components;
using RodajeIA.Web.Datos;
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

var app = builder.Build();

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
