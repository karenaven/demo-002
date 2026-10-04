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
}

public class PersonajesService(IDbContextFactory<RodajeDbContext> contextos)
{
    public async Task<Resultado<Personaje>> CrearAsync(int serieId, DatosPersonaje datos)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(datos, new ValidationContext(datos), errores, validateAllProperties: true))
        {
            return Resultado<Personaje>.Fallo(errores.Select(e => e.ErrorMessage!).ToList());
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
            Heridas = string.IsNullOrWhiteSpace(datos.Heridas) ? null : datos.Heridas.Trim(),
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
}
