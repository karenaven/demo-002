using System.Text;
using System.Text.RegularExpressions;

namespace RodajeIA.Web.Guiones;

/// <summary>
/// Valida y divide un guion según la convención del Anexo A del PRD (RF-03, RF-04).
/// Es determinístico: no usa IA. Un guion con cualquier error se rechaza completo.
/// </summary>
public static partial class GuionParser
{
    public const int MaximoEscenas = 10;
    public const int MaximoClipsPorEscena = 20;

    private const string FormatoEncabezado = "ESCENA N — INT./EXT. LUGAR — MOMENTO DEL DÍA";

    private static readonly string[] CamposEscena = ["LOCACIÓN:", "ILUMINACIÓN:", "PUESTA EN ESCENA:", "AUDIO:"];

    private const string MarcadorTextoEnPantalla = "TEXTO EN PANTALLA:";

    [GeneratedRegex(@"^ESCENA(\s|$)")]
    private static partial Regex InicioEncabezadoEscena();

    [GeneratedRegex(@"^ESCENA\s+(?<numero>\d+)")]
    private static partial Regex NumeroEscena();

    // El "medio" es greedy para que el momento del día sea lo que sigue al último separador.
    [GeneratedRegex(@"^ESCENA\s+\d+\s+[—-]\s+(?<medio>.+)\s+[—-]\s+(?<momento>\S.*)$")]
    private static partial Regex EncabezadoEscena();

    [GeneratedRegex(@"^(?<intExt>INT|EXT)\.\s+(?<lugar>\S.*)$")]
    private static partial Regex IntExtYLugar();

    [GeneratedRegex(@"^CLIP(\s|$)")]
    private static partial Regex InicioClip();

    [GeneratedRegex(@"^CLIP\s+(?<numero>\d+)$")]
    private static partial Regex Clip();

    [GeneratedRegex(@"^(?<personaje>[A-ZÁÉÍÓÚÜÑ]+(?: [A-ZÁÉÍÓÚÜÑ]+)*)\s*(?:\((?<acotacion>[^()]*)\))?\s*:\s*(?<resto>.*)$")]
    private static partial Regex Dialogo();

    [GeneratedRegex("^\"(?<texto>.+)\"$")]
    private static partial Regex EntreComillas();

    private sealed record Linea(int Numero, string Texto);

    public static ResultadoParseo Parsear(string? guion)
    {
        var lineas = (guion ?? string.Empty)
            .Normalize(NormalizationForm.FormC)
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select((texto, indice) => new Linea(indice + 1, texto.Trim()))
            .Where(linea => linea.Texto.Length > 0)
            .ToList();

        if (lineas.Count == 0)
        {
            return ResultadoParseo.Invalido([new ErrorGuion(1, "El guion está vacío.")]);
        }

        var indicesEncabezado = lineas
            .Select((linea, indice) => (linea, indice))
            .Where(x => InicioEncabezadoEscena().IsMatch(x.linea.Texto))
            .Select(x => x.indice)
            .ToList();

        if (indicesEncabezado.Count == 0)
        {
            return ResultadoParseo.Invalido(
                [new ErrorGuion(1, $"El guion no tiene ningún encabezado de escena con el formato '{FormatoEncabezado}'.")]);
        }

        var errores = new List<ErrorGuion>();

        foreach (var linea in lineas.Take(indicesEncabezado[0]))
        {
            errores.Add(new ErrorGuion(linea.Numero, "Texto antes del primer encabezado ESCENA."));
        }

        var escenas = new List<EscenaParseada>();
        int? numeroAnterior = null;

        for (var i = 0; i < indicesEncabezado.Count; i++)
        {
            var inicio = indicesEncabezado[i];
            var fin = i + 1 < indicesEncabezado.Count ? indicesEncabezado[i + 1] : lineas.Count;
            var encabezado = lineas[inicio];
            var cuerpo = lineas.GetRange(inicio + 1, fin - inicio - 1);

            if (i == MaximoEscenas)
            {
                errores.Add(new ErrorGuion(encabezado.Numero, $"Se excede el límite de {MaximoEscenas} escenas por episodio."));
            }

            var numero = ValidarNumeracion(
                NumeroEscena().Match(encabezado.Texto), numeroAnterior, encabezado.Numero, "escena", "ESCENA", errores);
            numeroAnterior = numero ?? numeroAnterior + 1 ?? 1;

            var escena = ParsearEscena(encabezado, cuerpo, numero, errores);
            if (escena is not null)
            {
                escenas.Add(escena);
            }
        }

        return errores.Count == 0
            ? ResultadoParseo.Valido(escenas)
            : ResultadoParseo.Invalido(errores.OrderBy(e => e.Linea).ToList());
    }

    /// <summary>
    /// Valida que la numeración empiece en 1 y no se saltee ni repita. Devuelve el número leído, o null si no se pudo leer.
    /// </summary>
    private static int? ValidarNumeracion(
        Match coincidencia, int? numeroAnterior, int numeroLinea, string nombre, string marcador, List<ErrorGuion> errores)
    {
        if (!coincidencia.Success || !int.TryParse(coincidencia.Groups["numero"].Value, out var numero))
        {
            return null;
        }

        var esperado = (numeroAnterior ?? 0) + 1;
        if (numero != esperado)
        {
            var motivo = numeroAnterior is null
                ? $"La numeración de {nombre}s debe empezar en 1: se esperaba {marcador} 1 y se encontró {marcador} {numero}."
                : $"Numeración de {nombre}s salteada o repetida: se esperaba {marcador} {esperado} y se encontró {marcador} {numero}.";
            errores.Add(new ErrorGuion(numeroLinea, motivo));
        }

        return numero;
    }

    private static EscenaParseada? ParsearEscena(Linea encabezado, List<Linea> cuerpo, int? numero, List<ErrorGuion> errores)
    {
        var cantidadErroresInicial = errores.Count;

        var (intExt, lugar, momento) = ParsearEncabezado(encabezado, errores);

        var valoresCampos = new string?[CamposEscena.Length];
        var presentes = new bool[CamposEscena.Length];
        var apariciones = new List<(int Campo, int Linea)>();
        var clips = new List<(Linea Marcador, List<Linea> Contenido)>();

        foreach (var linea in cuerpo)
        {
            var campo = IndiceCampoEscena(linea.Texto);
            if (campo >= 0)
            {
                var marcador = CamposEscena[campo];
                if (clips.Count > 0)
                {
                    errores.Add(new ErrorGuion(linea.Numero, $"El campo {marcador} debe ir antes del primer CLIP."));
                }
                else if (presentes[campo])
                {
                    errores.Add(new ErrorGuion(linea.Numero, $"El campo {marcador} está repetido."));
                }
                else
                {
                    presentes[campo] = true;
                    apariciones.Add((campo, linea.Numero));
                    var valor = linea.Texto[marcador.Length..].Trim();
                    if (valor.Length == 0)
                    {
                        errores.Add(new ErrorGuion(linea.Numero, $"El campo {marcador} no tiene valor."));
                    }
                    else
                    {
                        valoresCampos[campo] = valor;
                    }
                }
            }
            else if (InicioClip().IsMatch(linea.Texto))
            {
                clips.Add((linea, []));
            }
            else if (clips.Count == 0)
            {
                errores.Add(new ErrorGuion(
                    linea.Numero,
                    "Línea inesperada entre el encabezado de la escena y el primer CLIP: solo se permiten LOCACIÓN:, ILUMINACIÓN:, PUESTA EN ESCENA: y AUDIO:."));
            }
            else
            {
                clips[^1].Contenido.Add(linea);
            }
        }

        // Un campo está fuera de orden si después de él aparece un campo que debería ir antes.
        foreach (var (campo, numeroLinea) in apariciones)
        {
            if (apariciones.Any(otra => otra.Linea > numeroLinea && otra.Campo < campo))
            {
                errores.Add(new ErrorGuion(
                    numeroLinea,
                    $"El campo {CamposEscena[campo]} está fuera de orden: el orden es LOCACIÓN:, ILUMINACIÓN:, PUESTA EN ESCENA:, AUDIO:."));
            }
        }

        for (var campo = 0; campo < CamposEscena.Length; campo++)
        {
            if (!presentes[campo])
            {
                errores.Add(new ErrorGuion(encabezado.Numero, $"Falta el campo {CamposEscena[campo]} en la escena."));
            }
        }

        if (clips.Count == 0)
        {
            errores.Add(new ErrorGuion(encabezado.Numero, "La escena no tiene ningún CLIP."));
        }

        var clipsParseados = new List<ClipParseado>();
        int? numeroClipAnterior = null;
        for (var i = 0; i < clips.Count; i++)
        {
            var (marcador, contenido) = clips[i];

            if (i == MaximoClipsPorEscena)
            {
                errores.Add(new ErrorGuion(marcador.Numero, $"Se excede el límite de {MaximoClipsPorEscena} clips por escena."));
            }

            var coincidencia = Clip().Match(marcador.Texto);
            if (!coincidencia.Success)
            {
                errores.Add(new ErrorGuion(marcador.Numero, "Marcador de clip inválido: se espera 'CLIP N'."));
            }

            var numeroClip = ValidarNumeracion(coincidencia, numeroClipAnterior, marcador.Numero, "clip", "CLIP", errores);
            numeroClipAnterior = numeroClip ?? numeroClipAnterior + 1 ?? 1;

            var clip = ParsearClip(marcador, contenido, numeroClip, errores);
            if (clip is not null)
            {
                clipsParseados.Add(clip);
            }
        }

        if (errores.Count > cantidadErroresInicial || numero is null || intExt is null)
        {
            return null;
        }

        return new EscenaParseada(
            numero.Value,
            intExt.Value,
            lugar!,
            momento!,
            valoresCampos[0]!,
            valoresCampos[1]!,
            valoresCampos[2]!,
            valoresCampos[3]!,
            clipsParseados);
    }

    private static (IntExt? IntExt, string? Lugar, string? Momento) ParsearEncabezado(Linea encabezado, List<ErrorGuion> errores)
    {
        var coincidencia = EncabezadoEscena().Match(encabezado.Texto);
        if (!coincidencia.Success)
        {
            errores.Add(new ErrorGuion(
                encabezado.Numero, $"Encabezado de escena con formato inválido: se espera '{FormatoEncabezado}'."));
            return (null, null, null);
        }

        var medio = IntExtYLugar().Match(coincidencia.Groups["medio"].Value.Trim());
        if (!medio.Success)
        {
            errores.Add(new ErrorGuion(
                encabezado.Numero, "En el encabezado de escena, INT/EXT debe escribirse 'INT.' o 'EXT.' (con punto), seguido del lugar."));
            return (null, null, null);
        }

        var intExt = medio.Groups["intExt"].Value == "INT" ? IntExt.Int : IntExt.Ext;
        return (intExt, medio.Groups["lugar"].Value.Trim(), coincidencia.Groups["momento"].Value.Trim());
    }

    private static int IndiceCampoEscena(string texto) =>
        Array.FindIndex(CamposEscena, marcador => texto.StartsWith(marcador, StringComparison.Ordinal));

    private static ClipParseado? ParsearClip(Linea marcador, List<Linea> contenido, int? numero, List<ErrorGuion> errores)
    {
        var cantidadErroresInicial = errores.Count;

        if (contenido.Count == 0)
        {
            errores.Add(new ErrorGuion(marcador.Numero, "El clip no tiene contenido."));
        }

        var accion = new List<string>();
        var dialogos = new List<DialogoParseado>();
        string? textoEnPantalla = null;
        var tieneTextoEnPantalla = false;

        foreach (var linea in contenido)
        {
            if (linea.Texto.StartsWith(MarcadorTextoEnPantalla, StringComparison.Ordinal))
            {
                if (tieneTextoEnPantalla)
                {
                    errores.Add(new ErrorGuion(linea.Numero, "El clip tiene más de un TEXTO EN PANTALLA: se permite uno como máximo."));
                    continue;
                }

                tieneTextoEnPantalla = true;
                var texto = EntreComillas().Match(linea.Texto[MarcadorTextoEnPantalla.Length..].Trim());
                if (texto.Success)
                {
                    textoEnPantalla = texto.Groups["texto"].Value;
                }
                else
                {
                    errores.Add(new ErrorGuion(linea.Numero, "El texto de TEXTO EN PANTALLA: debe ir entre comillas."));
                }

                continue;
            }

            var dialogo = Dialogo().Match(linea.Texto);
            if (dialogo.Success)
            {
                var personaje = dialogo.Groups["personaje"].Value;
                var texto = EntreComillas().Match(dialogo.Groups["resto"].Value.Trim());
                if (texto.Success)
                {
                    var acotacion = dialogo.Groups["acotacion"].Value.Trim();
                    dialogos.Add(new DialogoParseado(personaje, acotacion.Length > 0 ? acotacion : null, texto.Groups["texto"].Value));
                }
                else
                {
                    errores.Add(new ErrorGuion(linea.Numero, $"El diálogo de {personaje} debe ir entre comillas."));
                }

                continue;
            }

            accion.Add(linea.Texto);
        }

        if (errores.Count > cantidadErroresInicial || numero is null)
        {
            return null;
        }

        return new ClipParseado(numero.Value, string.Join("\n", accion), dialogos, textoEnPantalla);
    }
}
