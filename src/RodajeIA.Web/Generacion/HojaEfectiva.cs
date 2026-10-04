using RodajeIA.Web.Datos;

namespace RodajeIA.Web.Generacion;

/// <summary>
/// Hoja efectiva de un personaje en una escena: la hoja base con los campos de su variante reemplazados (RN-05).
/// Es la que recibe la IA y la que se inserta en el prompt final.
/// </summary>
public sealed record HojaEfectiva(
    string Nombre,
    string Edad,
    string DescripcionFisica,
    string Peinado,
    string Vestuario,
    string? Heridas,
    string Personalidad,
    string Rol)
{
    public static HojaEfectiva De(Personaje hoja, VariantePersonaje? variante) => new(
        hoja.Nombre,
        variante?.Edad ?? hoja.Edad,
        hoja.DescripcionFisica,
        variante?.Peinado ?? hoja.Peinado,
        variante?.Vestuario ?? hoja.Vestuario,
        variante?.Heridas ?? hoja.Heridas,
        hoja.Personalidad,
        hoja.Rol);

    /// <summary>Hojas efectivas de los personajes con hoja presentes en la escena, en un orden estable.</summary>
    public static List<HojaEfectiva> DeEscena(Escena escena) => escena.Personajes
        .OrderBy(p => p.Id)
        .Select(p => De(p, escena.Variantes.SingleOrDefault(v => v.PersonajeId == p.Id)))
        .ToList();

    /// <summary>Descripción del personaje con la plantilla <c>porPersonaje</c> del molde.</summary>
    public string Describir(MoldePrompt molde) => Plantilla.Rellenar(
        molde[MoldePrompt.Personajes].PorPersonaje!,
        new Dictionary<string, string?>
        {
            ["personaje.nombre"] = Nombre,
            ["personaje.edad"] = Edad,
            ["personaje.descripcionFisica"] = DescripcionFisica,
            ["personaje.peinado"] = Peinado,
            ["personaje.vestuario"] = Vestuario,
            ["personaje.heridas"] = Heridas,
            ["personaje.personalidad"] = Personalidad,
            ["personaje.rol"] = Rol,
        });
}
