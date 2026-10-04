using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;

namespace RodajeIA.Web.Generacion;

/// <summary>Tiempos de la generación (RNF-06, RNF-07). Los tests los reducen a cero.</summary>
public sealed record OpcionesGeneracion
{
    /// <summary>Tiempo máximo de espera por llamada a Gemini (RNF-06).</summary>
    public TimeSpan TiempoMaximoPorLlamada { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>Espera antes de cada reintento: 2 reintentos, 3 intentos en total (RNF-07).</summary>
    public IReadOnlyList<TimeSpan> EsperasAntesDeReintentar { get; init; } = [TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30)];
}

/// <summary>Avisa a las pantallas abiertas que cambió el estado de generación de las escenas de un episodio.</summary>
public sealed class AvisosGeneracion
{
    public event Action<int>? EpisodioCambio;

    public void Avisar(int episodioId) => EpisodioCambio?.Invoke(episodioId);
}

/// <summary>
/// Genera con Gemini los bloques de una escena en estado "generando" (RF-06a), con reintentos ante error, timeout o
/// respuesta inválida (RF-06b, RNF-07). Guarda los bloques en cuanto se generan (RF-10a) o deja la escena en "error" (RF-10b).
/// </summary>
public sealed class GeneradorEscenas(
    IDbContextFactory<RodajeDbContext> contextos,
    IClienteGemini gemini,
    ConfiguracionGeneracion config,
    OpcionesGeneracion opciones,
    AvisosGeneracion avisos,
    ILogger<GeneradorEscenas> logger)
{
    public async Task GenerarAsync(int escenaId, CancellationToken cancelacion = default)
    {
        await using var db = await contextos.CreateDbContextAsync(cancelacion);
        var escena = await db.Escenas
            .Include(e => e.Personajes)
            .Include(e => e.Variantes)
            .Include(e => e.Clips).ThenInclude(c => c.Dialogos)
            .Include(e => e.Clips).ThenInclude(c => c.Bloque)
            .AsSplitQuery()
            .SingleOrDefaultAsync(e => e.Id == escenaId, cancelacion);

        // Solo se generan escenas reservadas para generar; si el guion se reemplazó mientras tanto, no hay nada que hacer.
        if (escena is not { Estado: EstadoGeneracion.Generando })
        {
            return;
        }

        var instruccion = ArmadorPrompt.ArmarInstruccion(config, escena);
        string? motivo = null;
        for (var intento = 0; intento <= opciones.EsperasAntesDeReintentar.Count; intento++)
        {
            if (intento > 0)
            {
                await Task.Delay(opciones.EsperasAntesDeReintentar[intento - 1], cancelacion);
            }

            RespuestaGeneracion? respuesta;
            try
            {
                respuesta = await LlamarAsync(instruccion, cancelacion);
            }
            catch (GeminiNoConfiguradoException ex)
            {
                motivo = ex.Message;
                break;
            }
            catch (Exception ex) when (!cancelacion.IsCancellationRequested)
            {
                motivo = ex is OperationCanceledException
                    ? $"Gemini no respondió en {opciones.TiempoMaximoPorLlamada.TotalSeconds:0} s."
                    : $"Error al llamar a Gemini: {ex.Message}";
                logger.LogWarning(ex, "Intento {Intento} de la escena {EscenaId} falló", intento + 1, escenaId);
                continue;
            }

            var errores = ValidadorRespuesta.Validar(respuesta, escena, config.Vocabulario);
            if (errores.Count == 0)
            {
                foreach (var (clip, bloque) in ValidadorRespuesta.ABloques(respuesta!, escena))
                {
                    clip.Bloque = bloque;
                }

                await TerminarAsync(db, escena, EstadoGeneracion.Generada, null);
                return;
            }

            motivo = "Respuesta inválida de Gemini: " + string.Join(" ", errores);
            logger.LogWarning("Intento {Intento} de la escena {EscenaId} con respuesta inválida: {Motivo}", intento + 1, escenaId, motivo);
        }

        await TerminarAsync(db, escena, EstadoGeneracion.Error, motivo);
    }

    /// <summary>Deja en "error" una escena que sigue en "generando" (RF-10b).</summary>
    public async Task MarcarErrorAsync(int escenaId, string motivo)
    {
        await using var db = await contextos.CreateDbContextAsync();
        var escena = await db.Escenas.SingleOrDefaultAsync(e => e.Id == escenaId && e.Estado == EstadoGeneracion.Generando);
        if (escena is not null)
        {
            await TerminarAsync(db, escena, EstadoGeneracion.Error, motivo);
        }
    }

    private async Task<RespuestaGeneracion?> LlamarAsync(string instruccion, CancellationToken cancelacion)
    {
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancelacion);
        limite.CancelAfter(opciones.TiempoMaximoPorLlamada);
        var json = await gemini.GenerarAsync(instruccion, config.Schema, limite.Token);
        return RespuestaGeneracion.Parsear(json);
    }

    private async Task TerminarAsync(RodajeDbContext db, Escena escena, EstadoGeneracion estado, string? motivo)
    {
        escena.Estado = estado;
        escena.MotivoError = motivo;
        await db.SaveChangesAsync(CancellationToken.None);
        avisos.Avisar(escena.EpisodioId);
    }
}
