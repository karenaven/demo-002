using System.Text;

namespace RodajeIA.Tests.Guiones;

internal static class GuionesDeEjemplo
{
    public const string EncabezadoEscena1 = "ESCENA 1 — INT. RESTAURANTE — NOCHE";

    public const string Locacion =
        "Restaurante pequeño y acogedor; pared de ladrillo visto, mesas de madera oscura, vela en portavelas de cristal.";

    public const string Iluminacion =
        "Luz cálida ambiental (aprox. 2700 K), principal suave cenital de la lámpara sobre la mesa, reflejo de la vela en los rostros.";

    public const string PuestaEnEscena =
        "Sentados frente a frente en una mesa para dos. Ana siempre a la IZQUIERDA del encuadre, José Daniel siempre a la DERECHA.";

    public const string Audio =
        "Ambiente tranquilo de restaurante, murmullo lejano y cubiertos; SIN música. Diálogo en español, acento latinoamericano neutro.";

    /// <summary>Escena válida del Anexo A del PRD.</summary>
    public static readonly string[] EscenaValida =
    [
        EncabezadoEscena1,
        $"LOCACIÓN: {Locacion}",
        $"ILUMINACIÓN: {Iluminacion}",
        $"PUESTA EN ESCENA: {PuestaEnEscena}",
        $"AUDIO: {Audio}",
        "CLIP 1",
        "Ana y José Daniel conversan en una mesa junto a la ventana. José Daniel sonríe pícaramente.",
        "ANA: \"Mentiroso.\"",
        "JOSÉ DANIEL (sonriendo): \"Bueno... pasó parecido.\"",
        "Ana niega con la cabeza, divertida, con una sonrisa amplia.",
        "CLIP 2",
        "Un mesero deja dos platos sobre la mesa. Ana y José Daniel agradecen con gestos breves.",
    ];

    public static string Unir(params IEnumerable<string>[] partes) => string.Join("\n", partes.SelectMany(p => p));

    /// <summary>Una escena mínima válida con el número y los clips indicados.</summary>
    public static string[] Escena(int numero, int clips = 1, string? puestaEnEscena = null, params string[][] contenidoPorClip)
    {
        var lineas = new List<string>
        {
            $"ESCENA {numero} — EXT. PARQUE — DÍA",
            "LOCACIÓN: Un parque con árboles.",
            "ILUMINACIÓN: Sol de mediodía.",
            $"PUESTA EN ESCENA: {puestaEnEscena ?? "Dos bancos enfrentados."}",
            "AUDIO: Pájaros y viento.",
        };

        var cantidad = Math.Max(clips, contenidoPorClip.Length);
        for (var i = 1; i <= cantidad; i++)
        {
            lineas.Add($"CLIP {i}");
            lineas.AddRange(i <= contenidoPorClip.Length ? contenidoPorClip[i - 1] : [$"Acción del clip {i}."]);
        }

        return [.. lineas];
    }

    /// <summary>Guion de tamaño máximo: 10 escenas de 20 clips (RNF-03).</summary>
    public static string GuionMaximo()
    {
        var guion = new StringBuilder();
        for (var escena = 1; escena <= 10; escena++)
        {
            guion.AppendLine(string.Join("\n", Escena(escena, contenidoPorClip:
                Enumerable.Range(1, 20)
                    .Select(clip => new[]
                    {
                        $"Ana camina hacia la mesa {clip} y se sienta.",
                        "ANA: \"Hola.\"",
                        "JOSÉ DANIEL (sonriendo): \"Bueno... pasó parecido.\"",
                        "TEXTO EN PANTALLA: \"Más tarde\"",
                    })
                    .ToArray())));
        }

        return guion.ToString();
    }
}
