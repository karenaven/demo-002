using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;

namespace RodajeIA.Web.Servicios;

/// <summary>Datos de una hoja de personaje (Anexo B). Todos son obligatorios salvo heridas/marcas (RF-01e).</summary>
public class DatosPersonaje
{
    [Required(ErrorMessage = "Falta el nombre.")]
    public string? Nombre { get; set; }

    [Required(ErrorMessage = "Falta la edad.")]
    public string? Edad { get; set; }

    [Required(ErrorMessage = "Falta la descripción física.")]
    public string? DescripcionFisica { get; set; }

    [Required(ErrorMessage = "Falta el peinado.")]
    public string? Peinado { get; set; }

    [Required(ErrorMessage = "Falta el vestuario.")]
    public string? Vestuario { get; set; }

    public string? Heridas { get; set; }

    [Required(ErrorMessage = "Falta la personalidad.")]
    public string? Personalidad { get; set; }

    [Required(ErrorMessage = "Falta el rol.")]
    public string? Rol { get; set; }

    public static DatosPersonaje De(Personaje hoja) => new()
    {
        Nombre = hoja.Nombre,
        Edad = hoja.Edad,
        DescripcionFisica = hoja.DescripcionFisica,
        Peinado = hoja.Peinado,
        Vestuario = hoja.Vestuario,
        Heridas = hoja.Heridas,
        Personalidad = hoja.Personalidad,
        Rol = hoja.Rol,
    };
}

public class PersonajesService(IDbContextFactory<RodajeDbContext> contextos)
{
    public async Task<Resultado<Personaje>> CrearAsync(int serieId, DatosPersonaje datos)
    {
        if (Validar(datos) is { Count: > 0 } errores)
        {
            return Resultado<Personaje>.Fallo(errores);
        }

        await using var db = await contextos.CreateDbContextAsync();
        if (!await db.Series.AnyAsync(s => s.Id == serieId))
        {
            return Resultado<Personaje>.Fallo("La serie no existe.");
        }

        var personaje = new Personaje
        {
            SerieId = serieId,
            Nombre = datos.Nombre!.Trim(),
            Edad = datos.Edad!.Trim(),
            DescripcionFisica = datos.DescripcionFisica!.Trim(),
            Peinado = datos.Peinado!.Trim(),
            Vestuario = datos.Vestuario!.Trim(),
            Heridas = Opcional(datos.Heridas),
            Personalidad = datos.Personalidad!.Trim(),
            Rol = datos.Rol!.Trim(),
        };
        db.Personajes.Add(personaje);
        await db.SaveChangesAsync();
        return Resultado<Personaje>.Exito(personaje);
    }

    public async Task<List<Personaje>> ListarAsync(int serieId)
    {
        await using var db = await contextos.CreateDbContextAsync();
        return await db.Personajes.AsNoTracking().Where(p => p.SerieId == serieId).OrderBy(p => p.Nombre).ToListAsync();
    }

    /// <summary>
    /// Edita la hoja sin dejar vacío ningún campo obligatorio (RF-01g); si falla, la hoja guardada no cambia.
    /// El prompt de las escenas ya generadas toma la hoja nueva; la detección de personajes no se vuelve a correr.
    /// </summary>
    public async Task<Resultado<Personaje>> EditarAsync(int id, DatosPersonaje datos)
    {
        if (Validar(datos) is { Count: > 0 } errores)
        {
            return Resultado<Personaje>.Fallo(errores);
        }

        await using var db = await contextos.CreateDbContextAsync();
        var personaje = await db.Personajes.SingleOrDefaultAsync(p => p.Id == id);
        if (personaje is null)
        {
            return Resultado<Personaje>.Fallo("La hoja de personaje no existe.");
        }

        Aplicar(personaje, datos);
        await db.SaveChangesAsync();
        return Resultado<Personaje>.Exito(personaje);
    }

    /// <summary>Elimina la hoja con todas sus variantes, en cualquier estado de la escena (RF-01h).</summary>
    /// <returns>Si la hoja existía.</returns>
    public async Task<bool> EliminarAsync(int id)
    {
        await using var db = await contextos.CreateDbContextAsync();
        return await db.Personajes.Where(p => p.Id == id).ExecuteDeleteAsync() > 0;
    }

    private static List<string> Validar(DatosPersonaje datos)
    {
        var errores = new List<ValidationResult>();
        Validator.TryValidateObject(datos, new ValidationContext(datos), errores, validateAllProperties: true);
        return errores.Select(e => e.ErrorMessage!).ToList();
    }

    private static void Aplicar(Personaje personaje, DatosPersonaje datos)
    {
        personaje.Nombre = datos.Nombre!.Trim();
        personaje.Edad = datos.Edad!.Trim();
        personaje.DescripcionFisica = datos.DescripcionFisica!.Trim();
        personaje.Peinado = datos.Peinado!.Trim();
        personaje.Vestuario = datos.Vestuario!.Trim();
        personaje.Heridas = Opcional(datos.Heridas);
        personaje.Personalidad = datos.Personalidad!.Trim();
        personaje.Rol = datos.Rol!.Trim();
    }

    private static string? Opcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
