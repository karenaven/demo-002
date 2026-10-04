using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Guiones;

namespace RodajeIA.Web.Servicios;

public sealed record ResultadoCarga(IReadOnlyList<ErrorGuion> ErroresGuion, string? Error)
{
    public bool Ok => ErroresGuion.Count == 0 && Error is null;
}

/// <summary>Carga el guion de un episodio (RF-02, RF-03, RF-04, RF-05, RF-06d).</summary>
public class CargaGuionService(IDbContextFactory<RodajeDbContext> contextos)
{
    /// <summary>
    /// Valida el guion y, si es válido, lo guarda en el episodio reemplazando el guion anterior y todo lo que depende de él.
    /// Si es inválido no guarda nada. Las escenas nuevas quedan en estado "pendiente", sin iniciar la generación.
    /// </summary>
    public async Task<ResultadoCarga> CargarAsync(int? serieId, int? episodioId, string? guion)
    {
        if (serieId is null || episodioId is null)
        {
            return new ResultadoCarga([], "Elegí una serie y un episodio antes de cargar el guion.");
        }

        await using var db = await contextos.CreateDbContextAsync();
        var episodio = await db.Episodios.SingleOrDefaultAsync(e => e.Id == episodioId && e.SerieId == serieId);
        if (episodio is null)
        {
            return new ResultadoCarga([], "El episodio no existe en la serie elegida.");
        }

        var resultado = GuionParser.Parsear(guion);
        if (!resultado.EsValido)
        {
            return new ResultadoCarga(resultado.Errores, null);
        }

        var personajes = await db.Personajes.Where(p => p.SerieId == serieId).OrderBy(p => p.Id).ToListAsync();
        var personajePorNombre = personajes
            .GroupBy(p => p.Nombre.Normalize(), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        var nombres = personajePorNombre.Keys.ToList();

        // Las escenas anteriores se borran en cascada con sus clips, diálogos y personajes detectados (RF-02b).
        // Todo va en un único SaveChanges, así que es atómico: si algo falla, el episodio conserva su guion anterior.
        db.Escenas.RemoveRange(await db.Escenas.Where(e => e.EpisodioId == episodio.Id).ToListAsync());

        episodio.Guion = guion;
        foreach (var escena in resultado.Escenas)
        {
            var detectados = DetectorPersonajes.Detectar(escena, nombres);
            episodio.Escenas.Add(new Escena
            {
                Numero = escena.Numero,
                IntExt = escena.IntExt,
                Lugar = escena.Lugar,
                MomentoDelDia = escena.MomentoDelDia,
                Locacion = escena.Locacion,
                Iluminacion = escena.Iluminacion,
                PuestaEnEscena = escena.PuestaEnEscena,
                Audio = escena.Audio,
                Estado = EstadoGeneracion.Pendiente,
                Personajes = detectados.EnEscena.SelectMany(n => personajePorNombre[n]).ToList(),
                Clips = escena.Clips.Select((clip, i) => new Clip
                {
                    Numero = clip.Numero,
                    Accion = clip.Accion,
                    TextoEnPantalla = clip.TextoEnPantalla,
                    Dialogos = clip.Dialogos.Select((d, orden) => new LineaDialogo
                    {
                        Orden = orden + 1,
                        Personaje = d.Personaje,
                        Acotacion = d.Acotacion,
                        Texto = d.Texto,
                    }).ToList(),
                    Personajes = detectados.PorClip[i].SelectMany(n => personajePorNombre[n]).ToList(),
                }).ToList(),
            });
        }

        await db.SaveChangesAsync();
        return new ResultadoCarga([], null);
    }
}
