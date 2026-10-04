using System.Text;
using System.Text.RegularExpressions;

namespace RodajeIA.Web.Generacion;

/// <summary>
/// Rellena las plantillas de <c>config/molde-prompt.json</c>: <c>{{variable}}</c> se reemplaza literal por su valor
/// (RN-03, RN-04) y <c>[...]</c> se omite completo si alguna variable que contiene está vacía.
/// </summary>
public static partial class Plantilla
{
    public static string Rellenar(string plantilla, IReadOnlyDictionary<string, string?> valores)
    {
        var resultado = new StringBuilder();
        var posicion = 0;
        while (posicion < plantilla.Length)
        {
            var apertura = plantilla.IndexOf('[', posicion);
            if (apertura < 0)
            {
                resultado.Append(Reemplazar(plantilla[posicion..], valores));
                break;
            }

            var cierre = plantilla.IndexOf(']', apertura);
            if (cierre < 0)
            {
                throw new FormatException($"La plantilla tiene un \"[\" sin cerrar: {plantilla}");
            }

            resultado.Append(Reemplazar(plantilla[posicion..apertura], valores));
            var opcional = plantilla[(apertura + 1)..cierre];
            if (Variables().Matches(opcional).All(v => !string.IsNullOrEmpty(Valor(v.Groups[1].Value, valores))))
            {
                resultado.Append(Reemplazar(opcional, valores));
            }

            posicion = cierre + 1;
        }

        return resultado.ToString();
    }

    // Los valores se insertan después de analizar la plantilla, así que un "[" o "{{" dentro de un valor queda literal.
    private static string Reemplazar(string texto, IReadOnlyDictionary<string, string?> valores) =>
        Variables().Replace(texto, v => Valor(v.Groups[1].Value, valores) ?? "");

    private static string? Valor(string nombre, IReadOnlyDictionary<string, string?> valores) =>
        valores.TryGetValue(nombre, out var valor)
            ? valor
            : throw new KeyNotFoundException($"La plantilla usa la variable {{{{{nombre}}}}}, que no existe.");

    [GeneratedRegex(@"\{\{([^{}]+)\}\}")]
    private static partial Regex Variables();
}
