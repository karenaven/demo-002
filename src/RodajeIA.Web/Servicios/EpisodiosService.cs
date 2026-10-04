using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;

namespace RodajeIA.Web.Servicios;

public class DatosEpisodio
{
    [Required(ErrorMessage = "Falta el número.")]
    [Range(1, int.MaxValue, ErrorMessage = "El número debe ser 1 o mayor.")]
    public int? Numero { get; set; }

    [Required(ErrorMessage = "Falta el título.")]
    public string? Titulo { get; set; }
}

public class EpisodiosService(IDbContextFactory<RodajeDbContext> contextos)
{
    public async Task<Resultado<Episodio>> CrearAsync(int serieId, DatosEpisodio datos)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(datos, new ValidationContext(datos), errores, validateAllProperties: true))
        {
            return Resultado<Episodio>.Fallo(errores.Select(e => e.ErrorMessage!).ToList());
        }

        await using var db = await contextos.CreateDbContextAsync();
        if (!await db.Series.AnyAsync(s => s.Id == serieId))
        {
            return Resultado<Episodio>.Fallo("La serie no existe.");
        }

        if (await db.Episodios.AnyAsync(e => e.SerieId == serieId && e.Numero == datos.Numero))
        {
            return Resultado<Episodio>.Fallo($"Ya existe el episodio número {datos.Numero} en esta serie.");
        }

        var episodio = new Episodio { SerieId = serieId, Numero = datos.Numero!.Value, Titulo = datos.Titulo!.Trim() };
        db.Episodios.Add(episodio);
        await db.SaveChangesAsync();
        return Resultado<Episodio>.Exito(episodio);
    }

    public async Task<List<Episodio>> ListarAsync(int serieId)
    {
        await using var db = await contextos.CreateDbContextAsync();
        return await db.Episodios.AsNoTracking().Where(e => e.SerieId == serieId).OrderBy(e => e.Numero).ToListAsync();
    }

    /// <summary>Cambia el título del episodio; el número no se puede cambiar (RF-11c).</summary>
    public async Task<Resultado<Episodio>> EditarTituloAsync(int id, string? titulo)
    {
        if (string.IsNullOrWhiteSpace(titulo))
        {
            return Resultado<Episodio>.Fallo("Falta el título.");
        }

        await using var db = await contextos.CreateDbContextAsync();
        var episodio = await db.Episodios.SingleOrDefaultAsync(e => e.Id == id);
        if (episodio is null)
        {
            return Resultado<Episodio>.Fallo("El episodio no existe.");
        }

        episodio.Titulo = titulo.Trim();
        await db.SaveChangesAsync();
        return Resultado<Episodio>.Exito(episodio);
    }

    /// <summary>Elimina el episodio con su guion, escenas, clips, bloques y variantes (RF-11d).</summary>
    /// <returns>Si el episodio existía.</returns>
    public async Task<bool> EliminarAsync(int id)
    {
        await using var db = await contextos.CreateDbContextAsync();
        return await db.Episodios.Where(e => e.Id == id).ExecuteDeleteAsync() > 0;
    }

    public async Task<Episodio?> ObtenerAsync(int id)
    {
        await using var db = await contextos.CreateDbContextAsync();
        return await db.Episodios.AsNoTracking().Include(e => e.Serie).SingleOrDefaultAsync(e => e.Id == id);
    }

    /// <summary>Escenas del episodio en orden, con sus clips, diálogos y personajes detectados.</summary>
    public async Task<List<Escena>> ListarEscenasAsync(int episodioId)
    {
        await using var db = await contextos.CreateDbContextAsync();
        return await db.Escenas.AsNoTracking()
            .Where(e => e.EpisodioId == episodioId)
            .Include(e => e.Personajes)
            .Include(e => e.Clips.OrderBy(c => c.Numero)).ThenInclude(c => c.Dialogos.OrderBy(d => d.Orden))
            .Include(e => e.Clips).ThenInclude(c => c.Personajes)
            .AsSplitQuery()
            .OrderBy(e => e.Numero)
            .ToListAsync();
    }
}
