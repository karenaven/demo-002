using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;

namespace RodajeIA.Web.Servicios;

/// <summary>Campos de la hoja base que una variante puede reemplazar (RF-09a). Vacío significa "igual que la hoja base".</summary>
public class DatosVariante
{
    public string? Edad { get; set; }

    public string? Peinado { get; set; }

    public string? Vestuario { get; set; }

    public string? Heridas { get; set; }

    public static DatosVariante De(VariantePersonaje variante) => new()
    {
        Edad = variante.Edad,
        Peinado = variante.Peinado,
        Vestuario = variante.Vestuario,
        Heridas = variante.Heridas,
    };
}

/// <summary>Variantes de personaje por escena (RF-09).</summary>
public class VariantesService(IDbContextFactory<RodajeDbContext> contextos)
{
    public const string SinCambios = "La variante debe cambiar al menos un campo de la hoja base: edad, peinado, vestuario o heridas/marcas.";

    public async Task<Resultado<VariantePersonaje>> CrearAsync(int escenaId, int personajeId, DatosVariante datos)
    {
        await using var db = await contextos.CreateDbContextAsync();
        var escena = await db.Escenas.Include(e => e.Personajes).Include(e => e.Variantes).SingleOrDefaultAsync(e => e.Id == escenaId);
        if (escena is null)
        {
            return Resultado<VariantePersonaje>.Fallo("La escena no existe.");
        }

        if (ErrorPorEstado(escena) is { } error)
        {
            return Resultado<VariantePersonaje>.Fallo(error);
        }

        var personaje = escena.Personajes.SingleOrDefault(p => p.Id == personajeId);
        if (personaje is null)
        {
            return Resultado<VariantePersonaje>.Fallo("El personaje no está presente en esta escena.");
        }

        if (escena.Variantes.Any(v => v.PersonajeId == personajeId))
        {
            return Resultado<VariantePersonaje>.Fallo($"{personaje.Nombre} ya tiene una variante en esta escena.");
        }

        var variante = new VariantePersonaje { EscenaId = escenaId, PersonajeId = personajeId };
        if (!Aplicar(variante, personaje, datos))
        {
            return Resultado<VariantePersonaje>.Fallo(SinCambios);
        }

        db.Variantes.Add(variante);
        await db.SaveChangesAsync();
        return Resultado<VariantePersonaje>.Exito(variante);
    }

    public async Task<List<VariantePersonaje>> ListarAsync(int escenaId)
    {
        await using var db = await contextos.CreateDbContextAsync();
        return await db.Variantes.AsNoTracking()
            .Where(v => v.EscenaId == escenaId)
            .Include(v => v.Personaje)
            .OrderBy(v => v.Personaje!.Nombre)
            .ToListAsync();
    }

    public async Task<Resultado<VariantePersonaje>> EditarAsync(int varianteId, DatosVariante datos)
    {
        await using var db = await contextos.CreateDbContextAsync();
        var variante = await db.Variantes.Include(v => v.Escena).Include(v => v.Personaje).SingleOrDefaultAsync(v => v.Id == varianteId);
        if (variante is null)
        {
            return Resultado<VariantePersonaje>.Fallo("La variante no existe.");
        }

        if (ErrorPorEstado(variante.Escena!) is { } error)
        {
            return Resultado<VariantePersonaje>.Fallo(error);
        }

        if (!Aplicar(variante, variante.Personaje!, datos))
        {
            return Resultado<VariantePersonaje>.Fallo(SinCambios);
        }

        await db.SaveChangesAsync();
        return Resultado<VariantePersonaje>.Exito(variante);
    }

    /// <summary>Elimina la variante sin tocar la hoja base (RF-09d).</summary>
    public async Task<Resultado<bool>> EliminarAsync(int varianteId)
    {
        await using var db = await contextos.CreateDbContextAsync();
        var variante = await db.Variantes.Include(v => v.Escena).SingleOrDefaultAsync(v => v.Id == varianteId);
        if (variante is null)
        {
            return Resultado<bool>.Fallo("La variante no existe.");
        }

        if (ErrorPorEstado(variante.Escena!) is { } error)
        {
            return Resultado<bool>.Fallo(error);
        }

        db.Variantes.Remove(variante);
        await db.SaveChangesAsync();
        return Resultado<bool>.Exito(true);
    }

    /// <summary>Las variantes se congelan cuando la escena empieza a generarse (RF-09e).</summary>
    public static string? ErrorPorEstado(Escena escena) => escena.Estado switch
    {
        EstadoGeneracion.Generando => "La escena se está generando: no se pueden cambiar sus variantes.",
        EstadoGeneracion.Generada => "La escena ya está generada: no se pueden cambiar sus variantes.",
        _ => null,
    };

    /// <summary>
    /// Copia en la variante solo los campos que cambian respecto de la hoja base. No modifica la variante si no cambia ninguno.
    /// </summary>
    /// <returns>Si la variante cambia al menos un campo.</returns>
    private static bool Aplicar(VariantePersonaje variante, Personaje hoja, DatosVariante datos)
    {
        var edad = Cambio(datos.Edad, hoja.Edad);
        var peinado = Cambio(datos.Peinado, hoja.Peinado);
        var vestuario = Cambio(datos.Vestuario, hoja.Vestuario);
        var heridas = Cambio(datos.Heridas, hoja.Heridas);
        if (edad is null && peinado is null && vestuario is null && heridas is null)
        {
            return false;
        }

        variante.Edad = edad;
        variante.Peinado = peinado;
        variante.Vestuario = vestuario;
        variante.Heridas = heridas;
        return true;
    }

    private static string? Cambio(string? valor, string? baseActual)
    {
        var limpio = valor?.Trim();
        return string.IsNullOrEmpty(limpio) || limpio == baseActual ? null : limpio;
    }
}
