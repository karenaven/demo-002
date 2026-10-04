using System.Text;
using System.Text.RegularExpressions;

namespace RodajeIA.Web.Guiones;

public sealed record PersonajesDetectados(IReadOnlyList<string> EnEscena, IReadOnlyList<IReadOnlyList<string>> PorClip);

/// <summary>
/// Detecta de forma determinística (sin IA) qué personajes con hoja están presentes en una escena y en cada clip (RF-05).
/// </summary>
public static class DetectorPersonajes
{
    /// <param name="nombresConHoja">Nombres de las hojas de personaje de la serie, tal como están guardados.</param>
    /// <returns>Los nombres detectados, en el mismo orden que <paramref name="nombresConHoja"/>.</returns>
    public static PersonajesDetectados Detectar(EscenaParseada escena, IEnumerable<string> nombresConHoja)
    {
        var nombres = nombresConHoja
            .Select(nombre => nombre.Trim().Normalize(NormalizationForm.FormC))
            .Where(nombre => nombre.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var porClip = escena.Clips
            .Select(clip => (IReadOnlyList<string>)nombres.Where(nombre => EstaEnClip(nombre, clip)).ToList())
            .ToList();

        var enEscena = nombres
            .Where(nombre => porClip.Any(clip => clip.Contains(nombre)) || MencionaPalabraCompleta(escena.PuestaEnEscena, nombre))
            .ToList();

        return new PersonajesDetectados(enEscena, porClip);
    }

    private static bool EstaEnClip(string nombre, ClipParseado clip)
    {
        var marcadorDialogo = nombre.ToUpperInvariant();
        return MencionaPalabraCompleta(clip.Accion, nombre)
            || clip.Dialogos.Any(dialogo => dialogo.Personaje == marcadorDialogo);
    }

    // Coincidencia exacta (mayúsculas y tildes incluidas) y como palabra completa: "Anabel" no cuenta como "Ana".
    private static bool MencionaPalabraCompleta(string texto, string nombre) =>
        Regex.IsMatch(
            texto.Normalize(NormalizationForm.FormC),
            $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(nombre)}(?![\p{{L}}\p{{N}}])",
            RegexOptions.CultureInvariant);
}
