using System.Text.Json;
using System.Text.RegularExpressions;
using Google.GenAI.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RodajeIA.Tests.Servicios;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Generacion;
using RodajeIA.Web.Servicios;

namespace RodajeIA.Tests.Generacion;

/// <summary>
/// Gemini falso: por defecto responde, a partir de la instrucción, una toma por clip que dice todas sus líneas.
/// <see cref="Responder"/> permite cambiar la respuesta de cada llamada (o lanzar una excepción para simular un error).
/// </summary>
public sealed partial class GeminiFalso : IClienteGemini
{
    public List<string> Instrucciones { get; } = [];

    /// <summary>Recibe la instrucción y el número de llamada (desde 1) y devuelve el JSON.</summary>
    public Func<string, int, string?> Responder { get; set; } = (instruccion, _) => RespuestaValida(instruccion);

    /// <summary>Si es verdadero, la llamada no responde nunca: solo termina cuando se cancela (timeout).</summary>
    public bool Colgarse { get; set; }

    public async Task<string?> GenerarAsync(string instruccion, Schema schema, CancellationToken cancelacion)
    {
        Instrucciones.Add(instruccion);
        if (Colgarse)
        {
            await Task.Delay(Timeout.Infinite, cancelacion);
        }

        return Responder(instruccion, Instrucciones.Count);
    }

    /// <summary>Una toma por clip en "plano medio", con todas las líneas del clip y la acción indicada.</summary>
    public static string RespuestaValida(string instruccion, string accion = "Ana sonríe.") =>
        Json(Clips(instruccion).Select(clip => new
        {
            clip = clip.Numero,
            tomas = new[]
            {
                new
                {
                    plano = "plano medio",
                    optica = "50mm",
                    iluminacion = "continuidad con la luz base",
                    angulo = (string?)null,
                    movimiento = (string?)null,
                    accion,
                    dialogos = clip.Lineas,
                },
            },
        }));

    public static string Json(object bloques) => JsonSerializer.Serialize(new { bloques }, JsonSerializerOptions.Web);

    /// <summary>Clips y ids de línea que trae la instrucción, tal como los arma <see cref="ArmadorPrompt.ArmarInstruccion"/>.</summary>
    public static List<(int Numero, List<string> Lineas)> Clips(string instruccion) =>
        ClipEnInstruccion().Matches(instruccion)
            .Select(m => (int.Parse(m.Groups[1].Value), IdEnInstruccion().Matches(m.Value).Select(id => id.Groups[1].Value).ToList()))
            .ToList();

    [GeneratedRegex(@"^CLIP (\d+)$(?:(?!^CLIP )[\s\S])*", RegexOptions.Multiline)]
    private static partial Regex ClipEnInstruccion();

    [GeneratedRegex(@"\[id (c\d+-l\d+)\]")]
    private static partial Regex IdEnInstruccion();
}

/// <summary>Serie con episodio y todos los servicios de generación sobre una base en memoria, sin esperas entre reintentos.</summary>
public sealed class EscenarioGeneracion : IDisposable
{
    public EscenarioGeneracion(TimeSpan? tiempoMaximoPorLlamada = null)
    {
        Avisos = new AvisosGeneracion();
        Generador = new GeneradorEscenas(
            Db,
            Gemini,
            Config,
            new OpcionesGeneracion
            {
                EsperasAntesDeReintentar = [TimeSpan.Zero, TimeSpan.Zero],
                TiempoMaximoPorLlamada = tiempoMaximoPorLlamada ?? TimeSpan.FromSeconds(60),
            },
            Avisos,
            NullLogger<GeneradorEscenas>.Instance);
        Cola = new ColaGeneracion(Db, Avisos);
        Prompts = new PromptFinalService(Db, Config);
        Variantes = new VariantesService(Db);
        Serie = new SeriesService(Db).CrearAsync("Serie A").GetAwaiter().GetResult().Valor!;
        Episodio = new EpisodiosService(Db).CrearAsync(Serie.Id, new DatosEpisodio { Numero = 1, Titulo = "Piloto" }).GetAwaiter().GetResult().Valor!;
    }

    public static ConfiguracionGeneracion Config { get; } = ConfiguracionGeneracion.Cargar();

    public BaseDeDatosDePrueba Db { get; } = new();

    public GeminiFalso Gemini { get; } = new();

    public AvisosGeneracion Avisos { get; }

    public GeneradorEscenas Generador { get; }

    public ColaGeneracion Cola { get; }

    public PromptFinalService Prompts { get; }

    public VariantesService Variantes { get; }

    public Serie Serie { get; }

    public Episodio Episodio { get; }

    public void Dispose() => Db.Dispose();

    public async Task<Personaje> CrearHojaAsync(string nombre, string vestuario = "chaqueta negra", string? heridas = null)
    {
        var hoja = SeriesYPersonajesTests.HojaCompleta(nombre);
        hoja.Vestuario = vestuario;
        hoja.Heridas = heridas;
        var resultado = await new PersonajesService(Db).CrearAsync(Serie.Id, hoja);
        Assert.True(resultado.Ok);
        return resultado.Valor!;
    }

    public async Task CargarGuionAsync(string guion)
    {
        var resultado = await new CargaGuionService(Db).CargarAsync(Serie.Id, Episodio.Id, guion);
        Assert.True(resultado.Ok, string.Join("; ", resultado.ErroresGuion.Select(e => $"{e.Linea}: {e.Motivo}")));
    }

    /// <summary>Pulsa "Generar" y procesa la cola en el momento, como lo haría el procesador en segundo plano.</summary>
    public async Task GenerarEpisodioAsync()
    {
        foreach (var escenaId in await Cola.GenerarEpisodioAsync(Episodio.Id))
        {
            await Generador.GenerarAsync(escenaId, TestContext.Current.CancellationToken);
        }
    }

    public async Task<Escena> EscenaAsync(int numero)
    {
        await using var db = Db.CreateDbContext();
        return await db.Escenas.AsNoTracking()
            .Include(e => e.Clips).ThenInclude(c => c.Bloque).ThenInclude(b => b!.Tomas)
            .Include(e => e.Personajes)
            .SingleAsync(e => e.EpisodioId == Episodio.Id && e.Numero == numero, TestContext.Current.CancellationToken);
    }

    public async Task<string> PromptAsync(int numero)
    {
        var prompt = await Prompts.ArmarAsync((await EscenaAsync(numero)).Id);
        Assert.NotNull(prompt);
        return prompt;
    }

    public async Task CambiarEstadoAsync(int numero, EstadoGeneracion estado)
    {
        await using var db = Db.CreateDbContext();
        await db.Escenas
            .Where(e => e.EpisodioId == Episodio.Id && e.Numero == numero)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Estado, estado), TestContext.Current.CancellationToken);
    }
}
