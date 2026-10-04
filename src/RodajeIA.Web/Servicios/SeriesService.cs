using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;

namespace RodajeIA.Web.Servicios;

public class SeriesService(IDbContextFactory<RodajeDbContext> contextos)
{
    public async Task<Resultado<Serie>> CrearAsync(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return Resultado<Serie>.Fallo("Falta el nombre de la serie.");
        }

        await using var db = await contextos.CreateDbContextAsync();
        var serie = new Serie { Nombre = nombre.Trim() };
        db.Series.Add(serie);
        await db.SaveChangesAsync();
        return Resultado<Serie>.Exito(serie);
    }

    public async Task<Resultado<Serie>> EditarAsync(int id, string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return Resultado<Serie>.Fallo("Falta el nombre de la serie.");
        }

        await using var db = await contextos.CreateDbContextAsync();
        var serie = await db.Series.SingleOrDefaultAsync(s => s.Id == id);
        if (serie is null)
        {
            return Resultado<Serie>.Fallo("La serie no existe.");
        }

        serie.Nombre = nombre.Trim();
        await db.SaveChangesAsync();
        return Resultado<Serie>.Exito(serie);
    }

    /// <summary>Elimina la serie con sus hojas, episodios y todo lo que depende de ellos (RF-01d).</summary>
    /// <returns>Si la serie existía.</returns>
    public async Task<bool> EliminarAsync(int id)
    {
        await using var db = await contextos.CreateDbContextAsync();
        return await db.Series.Where(s => s.Id == id).ExecuteDeleteAsync() > 0;
    }

    public async Task<List<Serie>> ListarAsync()
    {
        await using var db = await contextos.CreateDbContextAsync();
        return await db.Series.AsNoTracking().OrderBy(s => s.Nombre).ThenBy(s => s.Id).ToListAsync();
    }

    public async Task<Serie?> ObtenerAsync(int id)
    {
        await using var db = await contextos.CreateDbContextAsync();
        return await db.Series.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id);
    }
}
