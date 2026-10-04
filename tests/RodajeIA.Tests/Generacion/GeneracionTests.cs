using RodajeIA.Web.Datos;
using RodajeIA.Web.Generacion;
using RodajeIA.Web.Servicios;
using static RodajeIA.Tests.Guiones.GuionesDeEjemplo;

namespace RodajeIA.Tests.Generacion;

public sealed class GeneracionTests : IDisposable
{
    private readonly EscenarioGeneracion e = new();

    public void Dispose() => e.Dispose();

    private static string FallaSiempre(string instruccion, int llamada) => throw new HttpRequestException("503 Service Unavailable");

    // AC-06a
    [Fact]
    public async Task EscenaDe3Clips_RecibeUnBloquePorClipConValoresDelVocabulario()
    {
        await e.CargarGuionAsync(Unir(Escena(1, clips: 3)));
        e.Gemini.Responder = (instruccion, _) => GeminiFalso.Json(GeminiFalso.Clips(instruccion).Select(c => new
        {
            clip = c.Numero,
            tomas = new[] { new { plano = "primer plano", optica = "85mm", iluminacion = "contraluz o luz de borde", angulo = "picado", movimiento = "paneo", accion = "Ana mira.", dialogos = c.Lineas } },
        }));

        await e.GenerarEpisodioAsync();

        var escena = await e.EscenaAsync(1);
        Assert.Equal(EstadoGeneracion.Generada, escena.Estado);
        Assert.Equal(3, escena.Clips.Count(c => c.Bloque is not null));
        var vocabulario = EscenarioGeneracion.Config.Vocabulario;
        Assert.All(escena.Clips.SelectMany(c => c.Bloque!.Tomas), toma =>
        {
            Assert.True(vocabulario[Vocabulario.Plano].Admite(toma.Plano));
            Assert.True(vocabulario[Vocabulario.Optica].Admite(toma.Optica));
            Assert.True(vocabulario[Vocabulario.Iluminacion].Admite(toma.Iluminacion));
            Assert.True(vocabulario[Vocabulario.Angulo].Admite(toma.Angulo));
            Assert.True(vocabulario[Vocabulario.Movimiento].Admite(toma.Movimiento));
        });
    }

    // AC-06b a AC-06i: una respuesta inválida se reintenta.
    [Fact]
    public async Task RespuestaInvalida_SeReintentaYQuedaGenerada()
    {
        await e.CargarGuionAsync(Unir(EscenaValida));
        e.Gemini.Responder = (instruccion, llamada) => llamada == 1
            ? GeminiFalso.Json(new[] { new { clip = 1, tomas = Array.Empty<object>() } })
            : GeminiFalso.RespuestaValida(instruccion);

        await e.GenerarEpisodioAsync();

        Assert.Equal(2, e.Gemini.Instrucciones.Count);
        Assert.Equal(EstadoGeneracion.Generada, (await e.EscenaAsync(1)).Estado);
    }

    // AC-06f
    [Fact]
    public async Task Generar_LasEscenasPendientesTerminanGeneradas()
    {
        await e.CargarGuionAsync(Unir(Escena(1), Escena(2)));

        await e.GenerarEpisodioAsync();

        Assert.Equal(EstadoGeneracion.Generada, (await e.EscenaAsync(1)).Estado);
        Assert.Equal(EstadoGeneracion.Generada, (await e.EscenaAsync(2)).Estado);
    }

    // AC-06j
    [Fact]
    public async Task CargarGuion_DejaLasEscenasPendientesSinLlamarAGemini()
    {
        await e.CargarGuionAsync(Unir(Escena(1), Escena(2)));

        Assert.Equal(EstadoGeneracion.Pendiente, (await e.EscenaAsync(1)).Estado);
        Assert.Equal(EstadoGeneracion.Pendiente, (await e.EscenaAsync(2)).Estado);
        Assert.Empty(e.Gemini.Instrucciones);
    }

    // AC-06k
    [Fact]
    public async Task Generar_SoloLlamaPorLasEscenasPendientes()
    {
        await e.CargarGuionAsync(Unir(Escena(1), Escena(2)));
        var escena1 = await e.EscenaAsync(1);
        await e.CambiarEstadoAsync(1, EstadoGeneracion.Generando);
        await e.Generador.GenerarAsync(escena1.Id, TestContext.Current.CancellationToken);
        var bloquesEscena1 = BloquesDe(await e.EscenaAsync(1));
        e.Gemini.Instrucciones.Clear();

        await e.GenerarEpisodioAsync();

        var instruccion = Assert.Single(e.Gemini.Instrucciones);
        Assert.Contains("ESCENA 2 —", instruccion);
        Assert.Equal(bloquesEscena1, BloquesDe(await e.EscenaAsync(1)));
        Assert.Equal(EstadoGeneracion.Generada, (await e.EscenaAsync(2)).Estado);
    }

    private static List<int> BloquesDe(Escena escena) => escena.Clips.Select(c => c.Bloque!.Id).ToList();

    // AC-10a
    [Fact]
    public async Task GeminiFallaEnLos3Intentos_EscenaQuedaEnErrorTras3Intentos()
    {
        await e.CargarGuionAsync(Unir(Escena(1)));
        e.Gemini.Responder = FallaSiempre;

        await e.GenerarEpisodioAsync();

        Assert.Equal(3, e.Gemini.Instrucciones.Count);
        var escena = await e.EscenaAsync(1);
        Assert.Equal(EstadoGeneracion.Error, escena.Estado);
        Assert.Contains("503", escena.MotivoError);
    }

    // AC-10b
    [Fact]
    public async Task UnaEscenaFalla_LasAnterioresConservanSusBloques()
    {
        await e.CargarGuionAsync(Unir(Escena(1), Escena(2), Escena(3)));
        e.Gemini.Responder = (instruccion, _) => instruccion.Contains("ESCENA 3 —")
            ? FallaSiempre(instruccion, 0)
            : GeminiFalso.RespuestaValida(instruccion);

        await e.GenerarEpisodioAsync();

        foreach (var numero in new[] { 1, 2 })
        {
            var escena = await e.EscenaAsync(numero);
            Assert.Equal(EstadoGeneracion.Generada, escena.Estado);
            Assert.All(escena.Clips, c => Assert.NotNull(c.Bloque));
        }

        Assert.Equal(EstadoGeneracion.Error, (await e.EscenaAsync(3)).Estado);
    }

    // AC-10c
    [Fact]
    public async Task FallaElPrimerIntentoYRespondeElSegundo_EscenaQuedaGenerada()
    {
        await e.CargarGuionAsync(Unir(Escena(1)));
        e.Gemini.Responder = (instruccion, llamada) => llamada == 1 ? FallaSiempre(instruccion, llamada) : GeminiFalso.RespuestaValida(instruccion);

        await e.GenerarEpisodioAsync();

        var escena = await e.EscenaAsync(1);
        Assert.Equal(EstadoGeneracion.Generada, escena.Estado);
        Assert.All(escena.Clips, c => Assert.NotNull(c.Bloque));
    }

    // AC-10d
    [Fact]
    public async Task ReintentarEscenaEnError_QuedaGenerada()
    {
        await e.CargarGuionAsync(Unir(Escena(1)));
        e.Gemini.Responder = FallaSiempre;
        await e.GenerarEpisodioAsync();
        e.Gemini.Responder = (instruccion, _) => GeminiFalso.RespuestaValida(instruccion);
        var escenaId = (await e.EscenaAsync(1)).Id;

        Assert.True(await e.Cola.ReintentarAsync(escenaId));
        await e.Generador.GenerarAsync(escenaId, TestContext.Current.CancellationToken);

        var escena = await e.EscenaAsync(1);
        Assert.Equal(EstadoGeneracion.Generada, escena.Estado);
        Assert.Null(escena.MotivoError);
        Assert.All(escena.Clips, c => Assert.NotNull(c.Bloque));
    }

    [Theory]
    [InlineData(EstadoGeneracion.Pendiente)]
    [InlineData(EstadoGeneracion.Generada)]
    public async Task Reintentar_SoloAplicaAEscenasEnError(EstadoGeneracion estado)
    {
        await e.CargarGuionAsync(Unir(Escena(1)));
        await e.CambiarEstadoAsync(1, estado);

        Assert.False(await e.Cola.ReintentarAsync((await e.EscenaAsync(1)).Id));
        Assert.Equal(estado, (await e.EscenaAsync(1)).Estado);
    }

    // AC-10e
    [Fact]
    public async Task EscenaEncoladaSinRespuesta_SeMuestraGenerando()
    {
        await e.CargarGuionAsync(Unir(Escena(1)));
        var avisos = new List<int>();
        e.Avisos.EpisodioCambio += avisos.Add;

        await e.Cola.GenerarEpisodioAsync(e.Episodio.Id);

        Assert.Equal(EstadoGeneracion.Generando, (await e.EscenaAsync(1)).Estado);
        Assert.Equal([e.Episodio.Id], avisos);
    }

    // AC-10f
    [Fact]
    public async Task AlIniciar_LasEscenasGenerandoPasanAError()
    {
        await e.CargarGuionAsync(Unir(Escena(1), Escena(2)));
        await e.CambiarEstadoAsync(1, EstadoGeneracion.Generando);

        await e.Cola.MarcarInterrumpidasAsync();

        Assert.Equal(EstadoGeneracion.Error, (await e.EscenaAsync(1)).Estado);
        Assert.Equal(EstadoGeneracion.Pendiente, (await e.EscenaAsync(2)).Estado);
    }

    // RNF-06
    [Fact]
    public async Task GeminiNoResponde_SeCortaPorTiempoYSeReintenta()
    {
        using var lento = new EscenarioGeneracion(tiempoMaximoPorLlamada: TimeSpan.FromMilliseconds(50));
        await lento.CargarGuionAsync(Unir(Escena(1)));
        lento.Gemini.Colgarse = true;

        await lento.GenerarEpisodioAsync();

        Assert.Equal(3, lento.Gemini.Instrucciones.Count);
        var escena = await lento.EscenaAsync(1);
        Assert.Equal(EstadoGeneracion.Error, escena.Estado);
        Assert.Contains("no respondió", escena.MotivoError);
    }

    [Fact]
    public async Task SinApiKey_NoSeReintentaYSeExplicaComoConfigurarla()
    {
        await e.CargarGuionAsync(Unir(Escena(1)));
        e.Gemini.Responder = (_, _) => throw new GeminiNoConfiguradoException("Falta la API key de Gemini.");

        await e.GenerarEpisodioAsync();

        Assert.Single(e.Gemini.Instrucciones);
        Assert.Equal("Falta la API key de Gemini.", (await e.EscenaAsync(1)).MotivoError);
    }

    // AC-09h
    [Fact]
    public async Task InstruccionAGemini_UsaLaHojaEfectivaConLaVariante()
    {
        var ana = await e.CrearHojaAsync("Ana", vestuario: "chaqueta negra");
        await e.CargarGuionAsync(Unir(Escena(1), Escena(2, contenidoPorClip: [["Ana entra."]])));
        var escena2 = await e.EscenaAsync(2);
        Assert.True((await e.Variantes.CrearAsync(escena2.Id, ana.Id, new DatosVariante { Vestuario = "abrigo gris" })).Ok);
        await e.CambiarEstadoAsync(1, EstadoGeneracion.Generada);

        await e.GenerarEpisodioAsync();

        var instruccion = Assert.Single(e.Gemini.Instrucciones);
        Assert.Contains("abrigo gris", instruccion);
        Assert.DoesNotContain("chaqueta negra", instruccion);
    }

    // RN-04: la IA recibe cada línea con su id para referenciarla.
    [Fact]
    public async Task InstruccionAGemini_TraeLaEscenaConUnIdPorLineaDeDialogo()
    {
        await e.CargarGuionAsync(Unir(EscenaValida));

        await e.GenerarEpisodioAsync();

        var instruccion = Assert.Single(e.Gemini.Instrucciones);
        Assert.Contains($"Locación: {Locacion}", instruccion);
        Assert.Contains("Diálogo [id c1-l1]: ANA: \"Mentiroso.\"", instruccion);
        Assert.Contains("Diálogo [id c1-l2]: JOSÉ DANIEL (sonriendo): \"Bueno... pasó parecido.\"", instruccion);
        Assert.DoesNotContain("{{", instruccion);
    }
}
