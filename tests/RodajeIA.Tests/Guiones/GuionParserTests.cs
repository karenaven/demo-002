using System.Diagnostics;
using RodajeIA.Web.Guiones;
using static RodajeIA.Tests.Guiones.GuionesDeEjemplo;

namespace RodajeIA.Tests.Guiones;

public class GuionParserTests
{
    /// <summary>Número de línea (desde 1) de la n-ésima línea cuyo texto es exactamente <paramref name="texto"/>.</summary>
    private static int LineaDe(string guion, string texto, int ocurrencia = 1) =>
        guion.Split('\n')
            .Select((linea, indice) => (linea, numero: indice + 1))
            .Where(x => x.linea == texto)
            .ElementAt(ocurrencia - 1)
            .numero;

    private static ErrorGuion UnicoError(string guion)
    {
        var resultado = GuionParser.Parsear(guion);
        Assert.False(resultado.EsValido);
        Assert.Empty(resultado.Escenas);
        return Assert.Single(resultado.Errores);
    }

    private static string ConLineaReemplazada(string[] escena, string original, string nueva) =>
        Unir(escena.Select(linea => linea == original ? nueva : linea));

    [Fact]
    public void EscenaDeEjemplo_EsValida()
    {
        var resultado = GuionParser.Parsear(Unir(EscenaValida));

        Assert.True(resultado.EsValido);
        Assert.Empty(resultado.Errores);
        Assert.Single(resultado.Escenas);
    }

    // AC-04a
    [Fact]
    public void EscenaDeEjemplo_ExtraeEncabezadoYCampos()
    {
        var escena = Assert.Single(GuionParser.Parsear(Unir(EscenaValida)).Escenas);

        Assert.Equal(1, escena.Numero);
        Assert.Equal(IntExt.Int, escena.IntExt);
        Assert.Equal("RESTAURANTE", escena.Lugar);
        Assert.Equal("NOCHE", escena.MomentoDelDia);
        Assert.Equal(Locacion, escena.Locacion);
        Assert.Equal(Iluminacion, escena.Iluminacion);
        Assert.Equal(PuestaEnEscena, escena.PuestaEnEscena);
        Assert.Equal(Audio, escena.Audio);
    }

    // AC-04b
    [Fact]
    public void EscenaDeEjemplo_TieneDosClips()
    {
        var escena = Assert.Single(GuionParser.Parsear(Unir(EscenaValida)).Escenas);

        Assert.Equal([1, 2], escena.Clips.Select(c => c.Numero));
    }

    // AC-04c
    [Fact]
    public void EscenaDeEjemplo_SeparaAccionYDialogo()
    {
        var clip = Assert.Single(GuionParser.Parsear(Unir(EscenaValida)).Escenas).Clips[0];

        Assert.Equal(
            "Ana y José Daniel conversan en una mesa junto a la ventana. José Daniel sonríe pícaramente.\n"
            + "Ana niega con la cabeza, divertida, con una sonrisa amplia.",
            clip.Accion);
        Assert.Equal(
            [
                new DialogoParseado("ANA", null, "Mentiroso."),
                new DialogoParseado("JOSÉ DANIEL", "sonriendo", "Bueno... pasó parecido."),
            ],
            clip.Dialogos);
        Assert.Null(clip.TextoEnPantalla);
    }

    // AC-04d
    [Fact]
    public void TextoEnPantalla_SeSeparaDeAccionYDialogo()
    {
        var guion = Unir(Escena(1, contenidoPorClip: [["Ana mira el teléfono.", "TEXTO EN PANTALLA: \"Hermano, ¿puedes salir conmigo?\""]]));

        var clip = Assert.Single(GuionParser.Parsear(guion).Escenas).Clips[0];

        Assert.Equal("Hermano, ¿puedes salir conmigo?", clip.TextoEnPantalla);
        Assert.Equal("Ana mira el teléfono.", clip.Accion);
        Assert.Empty(clip.Dialogos);
    }

    // AC-04e
    [Fact]
    public void MismoGuion_ProcesadoDosVeces_DaElMismoResultado()
    {
        var guion = GuionMaximo();

        Assert.Equivalent(GuionParser.Parsear(guion).Escenas, GuionParser.Parsear(guion).Escenas, strict: true);
    }

    // AC-03o
    [Fact]
    public void EncabezadoConGuionComun_EsEquivalenteAlDeRaya()
    {
        var conRaya = Assert.Single(GuionParser.Parsear(Unir(EscenaValida)).Escenas);
        var guionComun = ConLineaReemplazada(EscenaValida, EncabezadoEscena1, "ESCENA 1 - INT. RESTAURANTE - NOCHE");

        var resultado = GuionParser.Parsear(guionComun);

        Assert.True(resultado.EsValido);
        Assert.Equivalent(conRaya, Assert.Single(resultado.Escenas), strict: true);
    }

    // AC-03p
    [Fact]
    public void LineasEnBlanco_SeIgnoran()
    {
        var sinBlancos = Assert.Single(GuionParser.Parsear(Unir(EscenaValida)).Escenas);
        var conBlancos = string.Join("\n\n   \n", EscenaValida);

        var resultado = GuionParser.Parsear(conBlancos);

        Assert.True(resultado.EsValido);
        Assert.Equivalent(sinBlancos, Assert.Single(resultado.Escenas), strict: true);
    }

    [Fact]
    public void FinesDeLineaWindows_SeAceptan()
    {
        var resultado = GuionParser.Parsear(string.Join("\r\n", EscenaValida));

        Assert.True(resultado.EsValido);
    }

    // AC-05d (parte de parseo): el diálogo de un personaje sin hoja es válido.
    [Fact]
    public void DialogoDePersonajeSecundario_EsValido()
    {
        var guion = Unir(Escena(1, contenidoPorClip: [["MESERO: \"¿Algo más?\""]]));

        var clip = Assert.Single(GuionParser.Parsear(guion).Escenas).Clips[0];

        Assert.Equal(new DialogoParseado("MESERO", null, "¿Algo más?"), Assert.Single(clip.Dialogos));
        Assert.Equal(string.Empty, clip.Accion);
    }

    [Fact]
    public void VariasEscenas_SeDividenEnOrden()
    {
        var guion = Unir(Escena(1, clips: 2), Escena(2, clips: 3), Escena(3));

        var resultado = GuionParser.Parsear(guion);

        Assert.True(resultado.EsValido);
        Assert.Equal([1, 2, 3], resultado.Escenas.Select(e => e.Numero));
        Assert.Equal([2, 3, 1], resultado.Escenas.Select(e => e.Clips.Count));
        Assert.Equal(IntExt.Ext, resultado.Escenas[0].IntExt);
    }

    [Fact]
    public void GuionDeTamanoMaximo_EsValido()
    {
        var resultado = GuionParser.Parsear(GuionMaximo());

        Assert.True(resultado.EsValido);
        Assert.Equal(10, resultado.Escenas.Count);
        Assert.All(resultado.Escenas, e => Assert.Equal(20, e.Clips.Count));
    }

    // RNF-03: p95 < 1 s sobre 100 ejecuciones con un guion de tamaño máximo.
    [Fact]
    public void GuionDeTamanoMaximo_SeProcesaEnMenosDeUnSegundo_P95()
    {
        var guion = GuionMaximo();
        GuionParser.Parsear(guion);

        var tiempos = Enumerable.Range(0, 100)
            .Select(_ =>
            {
                var cronometro = Stopwatch.StartNew();
                GuionParser.Parsear(guion);
                return cronometro.Elapsed;
            })
            .Order()
            .ToList();

        Assert.True(tiempos[94] < TimeSpan.FromSeconds(1), $"p95 = {tiempos[94].TotalMilliseconds} ms");
    }

    // AC-03a
    [Fact]
    public void FaltaAudioYEscenaSinClips_MuestraDosErroresEnLosEncabezados()
    {
        var escena1 = EscenaValida.Where(l => !l.StartsWith("AUDIO:", StringComparison.Ordinal));
        var escena2 = Escena(2).TakeWhile(l => l != "CLIP 1");
        var guion = Unir(escena1, escena2);

        var errores = GuionParser.Parsear(guion).Errores;

        Assert.Collection(
            errores,
            e =>
            {
                Assert.Equal(LineaDe(guion, EncabezadoEscena1), e.Linea);
                Assert.Contains("AUDIO:", e.Motivo);
            },
            e =>
            {
                Assert.Equal(LineaDe(guion, "ESCENA 2 — EXT. PARQUE — DÍA"), e.Linea);
                Assert.Contains("CLIP", e.Motivo);
            });
    }

    // AC-03b
    [Fact]
    public void SinEncabezadoEscena_ErrorEnLineaUno()
    {
        var error = UnicoError("Ana entra al restaurante.\nANA: \"Hola.\"");

        Assert.Equal(1, error.Linea);
        Assert.Contains("ESCENA", error.Motivo);
    }

    // AC-03c
    [Fact]
    public void OnceEscenas_ErrorEnElEncabezadoDeLaEscena11()
    {
        var guion = Unir(Enumerable.Range(1, 11).SelectMany(n => Escena(n)));

        var error = UnicoError(guion);

        Assert.Equal(LineaDe(guion, "ESCENA 11 — EXT. PARQUE — DÍA"), error.Linea);
        Assert.Contains("10 escenas", error.Motivo);
    }

    // AC-03d
    [Fact]
    public void VeintiunClips_ErrorEnElClip21()
    {
        var guion = Unir(Escena(1, clips: 21));

        var error = UnicoError(guion);

        Assert.Equal(LineaDe(guion, "CLIP 21"), error.Linea);
        Assert.Contains("20 clips", error.Motivo);
    }

    // AC-03e
    [Fact]
    public void DosTextosEnPantalla_ErrorEnElSegundo()
    {
        var guion = Unir(Escena(1, contenidoPorClip: [["TEXTO EN PANTALLA: \"Uno\"", "Ana sale.", "TEXTO EN PANTALLA: \"Dos\""]]));

        var error = UnicoError(guion);

        Assert.Equal(LineaDe(guion, "TEXTO EN PANTALLA: \"Dos\""), error.Linea);
    }

    // AC-03g
    [Fact]
    public void DialogoSinComillas_ErrorEnEsaLinea()
    {
        var guion = ConLineaReemplazada(EscenaValida, "ANA: \"Mentiroso.\"", "ANA: Mentiroso.");

        var error = UnicoError(guion);

        Assert.Equal(LineaDe(guion, "ANA: Mentiroso."), error.Linea);
        Assert.Contains("comillas", error.Motivo);
    }

    // AC-03h
    [Fact]
    public void TextoEnPantallaSinComillas_ErrorEnEsaLinea()
    {
        var guion = Unir(Escena(1, contenidoPorClip: [["Ana sale.", "TEXTO EN PANTALLA: Hola"]]));

        var error = UnicoError(guion);

        Assert.Equal(LineaDe(guion, "TEXTO EN PANTALLA: Hola"), error.Linea);
        Assert.Contains("comillas", error.Motivo);
    }

    // AC-03i
    [Fact]
    public void EscenaSalteada_ErrorEnElEncabezadoSalteado()
    {
        var guion = Unir(Escena(1), Escena(3));

        var error = UnicoError(guion);

        Assert.Equal(LineaDe(guion, "ESCENA 3 — EXT. PARQUE — DÍA"), error.Linea);
    }

    [Fact]
    public void PrimeraEscenaNoEsUno_Error()
    {
        var guion = Unir(Escena(2));

        var error = UnicoError(guion);

        Assert.Equal(1, error.Linea);
    }

    [Fact]
    public void EscenaRepetida_ErrorEnLaSegunda()
    {
        var guion = Unir(Escena(1), Escena(1), Escena(2));

        var error = UnicoError(guion);

        Assert.Equal(LineaDe(guion, "ESCENA 1 — EXT. PARQUE — DÍA", ocurrencia: 2), error.Linea);
    }

    // AC-03j
    [Fact]
    public void ClipRepetido_ErrorEnElSegundo()
    {
        var guion = Unir(Escena(1, clips: 2).Select(l => l == "CLIP 2" ? "CLIP 1" : l));

        var error = UnicoError(guion);

        Assert.Equal(LineaDe(guion, "CLIP 1", ocurrencia: 2), error.Linea);
    }

    [Fact]
    public void ClipSalteado_ErrorEnElClipSalteado()
    {
        var guion = Unir(Escena(1, clips: 2).Select(l => l == "CLIP 2" ? "CLIP 3" : l));

        var error = UnicoError(guion);

        Assert.Equal(LineaDe(guion, "CLIP 3"), error.Linea);
    }

    [Fact]
    public void MarcadorDeClipSinNumero_Error()
    {
        var guion = Unir(Escena(1).Select(l => l == "CLIP 1" ? "CLIP uno" : l));

        var resultado = GuionParser.Parsear(guion);

        Assert.Contains(resultado.Errores, e => e.Linea == LineaDe(guion, "CLIP uno"));
    }

    // AC-03k
    [Fact]
    public void TextoAntesDelPrimerEncabezado_ErrorEnEsaLinea()
    {
        var guion = Unir(["Episodio piloto"], EscenaValida);

        var error = UnicoError(guion);

        Assert.Equal(1, error.Linea);
        Assert.Contains("antes del primer encabezado", error.Motivo);
    }

    // AC-03l
    [Fact]
    public void TextoEntreAudioYClip1_ErrorEnEsaLinea()
    {
        var guion = Unir(EscenaValida.SelectMany(l => l == "CLIP 1" ? ["Ana entra.", l] : new[] { l }));

        var error = UnicoError(guion);

        Assert.Equal(LineaDe(guion, "Ana entra."), error.Linea);
    }

    // AC-03m
    [Fact]
    public void ClipSinContenido_ErrorEnElClip()
    {
        var guion = Unir(Escena(1, contenidoPorClip: [["Ana sale."], []]));

        var error = UnicoError(guion);

        Assert.Equal(LineaDe(guion, "CLIP 2"), error.Linea);
    }

    // AC-03n
    [Fact]
    public void EncabezadoConIE_ErrorEnElEncabezado()
    {
        var guion = ConLineaReemplazada(EscenaValida, EncabezadoEscena1, "ESCENA 1 — I/E RESTAURANTE — NOCHE");

        var error = UnicoError(guion);

        Assert.Equal(1, error.Linea);
        Assert.Contains("INT.", error.Motivo);
    }

    [Fact]
    public void EncabezadoConIntSinPunto_ErrorEnElEncabezado()
    {
        var guion = ConLineaReemplazada(EscenaValida, EncabezadoEscena1, "ESCENA 1 — INT RESTAURANTE — NOCHE");

        var error = UnicoError(guion);

        Assert.Equal(1, error.Linea);
    }

    [Fact]
    public void EncabezadoSinMomentoDelDia_ErrorEnElEncabezado()
    {
        var guion = ConLineaReemplazada(EscenaValida, EncabezadoEscena1, "ESCENA 1 — INT. RESTAURANTE");

        var error = UnicoError(guion);

        Assert.Equal(1, error.Linea);
    }

    // AC-03q
    [Fact]
    public void LocacionRepetida_ErrorEnLaSegunda()
    {
        var segunda = "LOCACIÓN: Otra locación.";
        var guion = Unir(EscenaValida.SelectMany(l => l.StartsWith("LOCACIÓN:", StringComparison.Ordinal) ? [l, segunda] : new[] { l }));

        var error = UnicoError(guion);

        Assert.Equal(LineaDe(guion, segunda), error.Linea);
        Assert.Contains("repetido", error.Motivo);
    }

    // AC-03r
    [Fact]
    public void IluminacionAntesDeLocacion_ErrorEnIluminacion()
    {
        var lineas = (string[])EscenaValida.Clone();
        (lineas[1], lineas[2]) = (lineas[2], lineas[1]);
        var guion = Unir(lineas);

        var error = UnicoError(guion);

        Assert.Equal(LineaDe(guion, $"ILUMINACIÓN: {Iluminacion}"), error.Linea);
        Assert.Contains("fuera de orden", error.Motivo);
    }

    // AC-03s
    [Fact]
    public void AudioSinValor_ErrorEnEsaLinea()
    {
        var guion = ConLineaReemplazada(EscenaValida, $"AUDIO: {Audio}", "AUDIO:");

        var error = UnicoError(guion);

        Assert.Equal(LineaDe(guion, "AUDIO:"), error.Linea);
        Assert.Contains("no tiene valor", error.Motivo);
    }

    // AC-03t
    [Fact]
    public void AudioDespuesDelClip1_IncluyeErrorEnEseAudio()
    {
        var audio = $"AUDIO: {Audio}";
        var guion = Unir(EscenaValida.Where(l => l != audio).SelectMany(l => l == "CLIP 2" ? [audio, l] : new[] { l }));

        var errores = GuionParser.Parsear(guion).Errores;

        Assert.Contains(errores, e => e.Linea == LineaDe(guion, audio) && e.Motivo.Contains("antes del primer CLIP"));
    }

    // AC-03u
    [Theory]
    [InlineData("")]
    [InlineData("   \n\n  ")]
    [InlineData(null)]
    public void GuionVacio_ErrorEnLineaUno(string? guion)
    {
        var resultado = GuionParser.Parsear(guion);

        var error = Assert.Single(resultado.Errores);
        Assert.Equal(1, error.Linea);
        Assert.Contains("vacío", error.Motivo);
    }

    [Fact]
    public void VariosErrores_SeReportanTodosOrdenadosPorLinea()
    {
        var guion = Unir(
            ["Texto suelto"],
            Escena(1, contenidoPorClip: [["ANA: Sin comillas."]]),
            Escena(3));

        var errores = GuionParser.Parsear(guion).Errores;

        Assert.Equal(
            [1, LineaDe(guion, "ANA: Sin comillas."), LineaDe(guion, "ESCENA 3 — EXT. PARQUE — DÍA")],
            errores.Select(e => e.Linea));
    }
}
