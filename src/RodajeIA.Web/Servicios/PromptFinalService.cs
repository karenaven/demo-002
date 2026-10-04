using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Generacion;

namespace RodajeIA.Web.Servicios;

/// <summary>
/// Prompt final de las escenas generadas (RF-08a). Se arma cada vez desde lo guardado, así refleja siempre la hoja
/// efectiva y las tomas actuales.
/// </summary>
public class PromptFinalService(IDbContextFactory<RodajeDbContext> contextos, ConfiguracionGeneracion config)
{
    /// <returns>El prompt de la escena, o nulo si la escena no existe o no está generada.</returns>
    public async Task<string?> ArmarAsync(int escenaId)
    {
        var escenas = await CargarGeneradasAsync(e => e.Id == escenaId);
        return escenas.Select(e => ArmadorPrompt.ArmarPromptFinal(config, e)).SingleOrDefault();
    }

    /// <returns>El prompt de cada escena generada del episodio, por id de escena.</returns>
    public async Task<Dictionary<int, string>> ArmarEpisodioAsync(int episodioId)
    {
        var escenas = await CargarGeneradasAsync(e => e.EpisodioId == episodioId);
        return escenas.ToDictionary(e => e.Id, e => ArmadorPrompt.ArmarPromptFinal(config, e));
    }

    private async Task<List<Escena>> CargarGeneradasAsync(System.Linq.Expressions.Expression<Func<Escena, bool>> filtro)
    {
        await using var db = await contextos.CreateDbContextAsync();
        return await db.Escenas.AsNoTracking()
            .Where(filtro)
            .Where(e => e.Estado == EstadoGeneracion.Generada)
            .Include(e => e.Personajes)
            .Include(e => e.Variantes)
            .Include(e => e.Clips).ThenInclude(c => c.Dialogos)
            .Include(e => e.Clips).ThenInclude(c => c.Bloque).ThenInclude(b => b!.Tomas).ThenInclude(t => t.Dialogos).ThenInclude(d => d.LineaDialogo)
            .AsSplitQuery()
            .ToListAsync();
    }
}
