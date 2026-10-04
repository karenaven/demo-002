using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;

namespace RodajeIA.Web.Generacion;

/// <summary>
/// Inicia la generación de escenas (RF-06c, RF-10c). Cada escena pasa a "generando" antes de encolarse, así la
/// generación sigue aunque se cierre la pantalla y la escena no se puede encolar dos veces.
/// </summary>
public sealed class ColaGeneracion(IDbContextFactory<RodajeDbContext> contextos, AvisosGeneracion avisos)
{
    private readonly Channel<int> escenas = Channel.CreateUnbounded<int>(new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<int> Pendientes => escenas.Reader;

    /// <summary>Encola todas las escenas "pendiente" del episodio, en orden (RF-06c).</summary>
    /// <returns>Los ids de las escenas encoladas.</returns>
    public Task<List<int>> GenerarEpisodioAsync(int episodioId) =>
        EncolarAsync(episodioId, e => e.EpisodioId == episodioId && e.Estado == EstadoGeneracion.Pendiente);

    /// <summary>Vuelve a encolar una escena en "error" (RF-10c). No hace nada si la escena está en otro estado.</summary>
    /// <returns>Si la escena se encoló.</returns>
    public async Task<bool> ReintentarAsync(int escenaId)
    {
        await using var db = await contextos.CreateDbContextAsync();
        var episodioId = await db.Escenas.Where(e => e.Id == escenaId).Select(e => (int?)e.EpisodioId).SingleOrDefaultAsync();
        if (episodioId is null)
        {
            return false;
        }

        var encoladas = await EncolarAsync(episodioId.Value, e => e.Id == escenaId && e.Estado == EstadoGeneracion.Error);
        return encoladas.Count > 0;
    }

    /// <summary>Al iniciar la aplicación, las escenas que quedaron en "generando" pasan a "error" (RF-10e).</summary>
    public async Task MarcarInterrumpidasAsync()
    {
        await using var db = await contextos.CreateDbContextAsync();
        await db.Escenas
            .Where(e => e.Estado == EstadoGeneracion.Generando)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Estado, EstadoGeneracion.Error)
                .SetProperty(e => e.MotivoError, "La aplicación se cerró mientras se generaba la escena."));
    }

    private async Task<List<int>> EncolarAsync(int episodioId, System.Linq.Expressions.Expression<Func<Escena, bool>> filtro)
    {
        await using var db = await contextos.CreateDbContextAsync();
        var escenasAEncolar = await db.Escenas.Where(filtro).OrderBy(e => e.Numero).ToListAsync();
        foreach (var escena in escenasAEncolar)
        {
            escena.Estado = EstadoGeneracion.Generando;
            escena.MotivoError = null;
        }

        await db.SaveChangesAsync();
        foreach (var escena in escenasAEncolar)
        {
            escenas.Writer.TryWrite(escena.Id);
        }

        if (escenasAEncolar.Count > 0)
        {
            avisos.Avisar(episodioId);
        }

        return escenasAEncolar.Select(e => e.Id).ToList();
    }
}

/// <summary>Procesa la cola de generación en segundo plano, una escena a la vez.</summary>
public sealed class ProcesadorGeneracion(ColaGeneracion cola, GeneradorEscenas generador, ILogger<ProcesadorGeneracion> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken detener)
    {
        await foreach (var escenaId in cola.Pendientes.ReadAllAsync(detener))
        {
            try
            {
                await generador.GenerarAsync(escenaId, detener);
            }
            catch (OperationCanceledException) when (detener.IsCancellationRequested)
            {
                // Al cerrar la app la escena queda en "generando" y pasa a "error" en el próximo inicio (RF-10e).
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falló la generación de la escena {EscenaId}", escenaId);
                await generador.MarcarErrorAsync(escenaId, $"Error inesperado al generar: {ex.Message}");
            }
        }
    }
}
