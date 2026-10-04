using RodajeIA.Tests.Servicios;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Servicios;
using static RodajeIA.Tests.Guiones.GuionesDeEjemplo;

namespace RodajeIA.Tests.Generacion;

public sealed class PromptFinalTests : IDisposable
{
    private readonly EscenarioGeneracion e = new();

    public void Dispose() => e.Dispose();

    /// <summary>La línea del prompt con la descripción del personaje.</summary>
    private static string Descripcion(string prompt, string nombre) =>
        Assert.Single(prompt.Split('\n'), linea => linea.StartsWith($"{nombre}, ", StringComparison.Ordinal));

    // AC-08a
    [Fact]
    public async Task EscenaDeEjemplo_IncluyeTodasLasSeccionesSinCamposVacios()
    {
        await e.CrearHojaAsync("Ana");
        await e.CrearHojaAsync("José Daniel");
        await e.CargarGuionAsync(Unir(EscenaValida));
        await e.GenerarEpisodioAsync();

        var prompt = await e.PromptAsync(1);

        Assert.StartsWith(EscenarioGeneracion.Config.Estilo, prompt);
        Assert.Contains($"Locación: {Locacion}", prompt);
        Assert.Contains($"Iluminación base: {Iluminacion}", prompt);
        Assert.Contains("Personajes:", prompt);
        Descripcion(prompt, "Ana");
        Descripcion(prompt, "José Daniel");
        Assert.Contains($"Puesta en escena:\n{PuestaEnEscena}", prompt);
        Assert.Contains($"Audio:\n{Audio}", prompt);
        Assert.Contains("Bloque 1:", prompt);
        Assert.Contains("Bloque 2:", prompt);
        Assert.DoesNotContain("{{", prompt);
        Assert.DoesNotContain(": .", prompt);
    }

    // Anexo D: plantilla de la toma y cierre del bloque.
    [Fact]
    public async Task Bloque_SigueLaPlantillaDeTomaYCierre()
    {
        await e.CargarGuionAsync(Unir(EscenaValida));
        await e.GenerarEpisodioAsync();

        var prompt = await e.PromptAsync(1);

        Assert.Contains(
            "Bloque 1:\nplano medio, óptica 50mm. Ana sonríe. ANA: \"Mentiroso.\" JOSÉ DANIEL (sonriendo): \"Bueno... pasó parecido.\" "
            + "Iluminación: continuidad con la luz base. Sin subtítulos ni texto en pantalla.",
            prompt);
    }

    [Fact]
    public async Task VariasTomas_SeUnenConCorteAYLosOpcionalesSeInsertan()
    {
        await e.CargarGuionAsync(Unir(Escena(1)));
        e.Gemini.Responder = (_, _) => GeminiFalso.Json(new[]
        {
            new
            {
                clip = 1,
                tomas = new object[]
                {
                    new { plano = "plano general", optica = "35mm", iluminacion = "luz suave de relleno", movimiento = "paneo", angulo = "picado", accion = "Ana entra.", dialogos = Array.Empty<string>() },
                    new { plano = "primer plano", optica = "85mm", iluminacion = "contraluz o luz de borde", accion = "Ana sonríe.", dialogos = Array.Empty<string>() },
                },
            },
        });
        await e.GenerarEpisodioAsync();

        var prompt = await e.PromptAsync(1);

        Assert.Contains(
            "plano general, paneo, picado, óptica 35mm. Ana entra. Iluminación: luz suave de relleno. "
            + "Corte a primer plano, óptica 85mm. Ana sonríe. Iluminación: contraluz o luz de borde.",
            prompt);
    }

    // AC-08b
    [Fact]
    public async Task EscenaDe3Clips_TieneLosBloquesEnOrdenConLasTomasDeSuClip()
    {
        await e.CargarGuionAsync(Unir(Escena(1, clips: 3)));
        e.Gemini.Responder = (instruccion, _) => GeminiFalso.Json(GeminiFalso.Clips(instruccion).Select(c => new
        {
            clip = c.Numero,
            tomas = new[] { new { plano = "plano medio", optica = "50mm", iluminacion = "continuidad con la luz base", accion = $"Acción del clip {c.Numero}.", dialogos = c.Lineas } },
        }));
        await e.GenerarEpisodioAsync();

        var prompt = await e.PromptAsync(1);

        var posiciones = new[] { "Bloque 1:", "Bloque 2:", "Bloque 3:" }.Select(b => prompt.IndexOf(b, StringComparison.Ordinal)).ToList();
        Assert.All(posiciones, p => Assert.True(p >= 0));
        Assert.Equal(posiciones.Order(), posiciones);
        var bloques = prompt.Split("\n\n").Where(s => s.StartsWith("Bloque ", StringComparison.Ordinal)).ToList();
        Assert.Equal(3, bloques.Count);
        for (var i = 0; i < 3; i++)
        {
            Assert.Contains($"Acción del clip {i + 1}.", bloques[i]);
        }
    }

    // AC-08c
    [Fact]
    public async Task HojaSinHeridas_NoMencionaHeridas()
    {
        await e.CrearHojaAsync("Ana", heridas: null);
        await e.CargarGuionAsync(Unir(EscenaValida));
        await e.GenerarEpisodioAsync();

        Assert.DoesNotContain("Heridas/marcas", await e.PromptAsync(1));
    }

    [Fact]
    public async Task HojaConHeridas_LasIncluye()
    {
        await e.CrearHojaAsync("Ana", heridas: "cicatriz en la ceja");
        await e.CargarGuionAsync(Unir(EscenaValida));
        await e.GenerarEpisodioAsync();

        Assert.Contains("Heridas/marcas: cicatriz en la ceja.", Descripcion(await e.PromptAsync(1), "Ana"));
    }

    // AC-08e
    [Fact]
    public async Task MismoPersonajeSinVariantes_DescripcionIdenticaEnDosEscenas()
    {
        await e.CrearHojaAsync("Ana");
        await e.CargarGuionAsync(Unir(Escena(1, contenidoPorClip: [["Ana corre."]]), Escena(2, contenidoPorClip: [["Ana descansa."]])));
        await e.GenerarEpisodioAsync();

        Assert.Equal(Descripcion(await e.PromptAsync(1), "Ana"), Descripcion(await e.PromptAsync(2), "Ana"));
    }

    // AC-08f
    [Fact]
    public async Task Dialogo_SeInsertaLiteralConMarcadorYAcotacion()
    {
        await e.CargarGuionAsync(Unir(EscenaValida));
        await e.GenerarEpisodioAsync();

        var prompt = await e.PromptAsync(1);

        Assert.Contains("ANA: \"Mentiroso.\"", prompt);
        Assert.Contains("JOSÉ DANIEL (sonriendo): \"Bueno... pasó parecido.\"", prompt);
    }

    // AC-08g
    [Fact]
    public async Task TextoEnPantalla_SeInsertaLiteralEnLugarDelCierreSinTexto()
    {
        await e.CargarGuionAsync(Unir(Escena(1, contenidoPorClip: [["Ana escribe.", "TEXTO EN PANTALLA: \"Hermano, ¿puedes salir conmigo?\""]])));
        await e.GenerarEpisodioAsync();

        var prompt = await e.PromptAsync(1);

        Assert.Contains("\"Hermano, ¿puedes salir conmigo?\"", prompt);
        Assert.DoesNotContain("Sin subtítulos ni texto en pantalla.", prompt);
    }

    // AC-08h
    [Fact]
    public async Task SinPersonajesConHoja_OmiteLaSeccionDePersonajes()
    {
        await e.CrearHojaAsync("Ana");
        await e.CargarGuionAsync(Unir(Escena(1, clips: 2)));
        await e.GenerarEpisodioAsync();

        var prompt = await e.PromptAsync(1);

        Assert.DoesNotContain("Personajes:", prompt);
        Assert.StartsWith(EscenarioGeneracion.Config.Estilo, prompt);
        Assert.Contains("Locación: ", prompt);
        Assert.Contains("Puesta en escena:", prompt);
        Assert.Contains("Audio:", prompt);
        Assert.Contains("Bloque 1:", prompt);
        Assert.Contains("Bloque 2:", prompt);
    }

    // AC-09a
    [Fact]
    public async Task VarianteEnLaEscena2_SoloCambiaEsaEscena()
    {
        var ana = await e.CrearHojaAsync("Ana", heridas: null);
        await e.CargarGuionAsync(Unir(Escena(1, contenidoPorClip: [["Ana corre."]]), Escena(2, contenidoPorClip: [["Ana descansa."]])));
        var variante = new DatosVariante { Edad = "60 años", Peinado = "moño gris", Vestuario = "abrigo gris", Heridas = "brazo vendado" };
        Assert.True((await e.Variantes.CrearAsync((await e.EscenaAsync(2)).Id, ana.Id, variante)).Ok);
        await e.GenerarEpisodioAsync();

        var escena1 = Descripcion(await e.PromptAsync(1), "Ana");
        var escena2 = Descripcion(await e.PromptAsync(2), "Ana");

        var hoja = SeriesYPersonajesTests.HojaCompleta("Ana");
        Assert.Equal(
            $"Ana, 60 años. {hoja.DescripcionFisica} Peinado: moño gris. Vestuario: abrigo gris. Heridas/marcas: brazo vendado. "
            + $"Personalidad: {hoja.Personalidad}. Rol: {hoja.Rol}.",
            escena2);
        Assert.Equal(
            $"Ana, {hoja.Edad}. {hoja.DescripcionFisica} Peinado: {hoja.Peinado}. Vestuario: chaqueta negra. "
            + $"Personalidad: {hoja.Personalidad}. Rol: {hoja.Rol}.",
            escena1);
    }

    [Fact]
    public async Task EscenaNoGenerada_NoTienePrompt()
    {
        await e.CargarGuionAsync(Unir(Escena(1)));

        Assert.Null(await e.Prompts.ArmarAsync((await e.EscenaAsync(1)).Id));
    }

    // RNF-04 (medición orientativa: el armado es en memoria y no llama a la IA)
    [Fact]
    public async Task EscenaDe20Clips_SeArmaEnMenosDe1Segundo()
    {
        await e.CrearHojaAsync("Ana");
        await e.CargarGuionAsync(GuionMaximo());
        await e.GenerarEpisodioAsync();
        var escenaId = (await e.EscenaAsync(1)).Id;

        var reloj = System.Diagnostics.Stopwatch.StartNew();
        await e.Prompts.ArmarAsync(escenaId);

        Assert.True(reloj.Elapsed < TimeSpan.FromSeconds(1), $"Tardó {reloj.Elapsed}.");
        Assert.Equal(EstadoGeneracion.Generada, (await e.EscenaAsync(10)).Estado);
    }
}
