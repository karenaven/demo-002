using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Servicios;
using static RodajeIA.Tests.Guiones.GuionesDeEjemplo;

namespace RodajeIA.Tests.Servicios;

public sealed class CargaGuionTests : IDisposable
{
    private readonly BaseDeDatosDePrueba db = new();
    private readonly EpisodiosService episodios;
    private readonly CargaGuionService carga;
    private readonly Serie serie;
    private readonly Episodio episodio;

    public CargaGuionTests()
    {
        episodios = new EpisodiosService(db);
        carga = new CargaGuionService(db);
        serie = new SeriesService(db).CrearAsync("Serie A").GetAwaiter().GetResult().Valor!;
        episodio = episodios.CrearAsync(serie.Id, new DatosEpisodio { Numero = 1, Titulo = "Piloto" }).GetAwaiter().GetResult().Valor!;
    }

    private static CancellationToken Cancelar => TestContext.Current.CancellationToken;

    public void Dispose() => db.Dispose();

    private async Task CrearHojas(params string[] nombres)
    {
        var personajes = new PersonajesService(db);
        foreach (var nombre in nombres)
        {
            Assert.True((await personajes.CrearAsync(serie.Id, SeriesYPersonajesTests.HojaCompleta(nombre))).Ok);
        }
    }

    private async Task<string?> GuionGuardado()
    {
        await using var contexto = db.CreateDbContext();
        return (await contexto.Episodios.SingleAsync(e => e.Id == episodio.Id, Cancelar)).Guion;
    }

    private async Task<(int Escenas, int Clips, int Dialogos)> ContarFilas()
    {
        await using var contexto = db.CreateDbContext();
        return (await contexto.Escenas.CountAsync(Cancelar), await contexto.Clips.CountAsync(Cancelar), await contexto.LineasDialogo.CountAsync(Cancelar));
    }

    // AC-02a
    [Fact]
    public async Task GuionValido_QuedaGuardadoEnElEpisodio()
    {
        var guion = Unir(EscenaValida);

        var resultado = await carga.CargarAsync(serie.Id, episodio.Id, guion);

        Assert.True(resultado.Ok);
        Assert.Equal(guion, await GuionGuardado());
        var escena = Assert.Single(await episodios.ListarEscenasAsync(episodio.Id));
        Assert.Equal("RESTAURANTE", escena.Lugar);
        Assert.Equal(Audio, escena.Audio);
        Assert.Equal([1, 2], escena.Clips.Select(c => c.Numero));
        Assert.Equal(
            [("ANA", null, "Mentiroso."), ("JOSÉ DANIEL", "sonriendo", "Bueno... pasó parecido.")],
            escena.Clips[0].Dialogos.Select(d => (d.Personaje, d.Acotacion, d.Texto)));
    }

    // AC-02b
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task SinSerieOEpisodio_NoSePermiteNiSeGuardaNada(bool conSerie, bool conEpisodio)
    {
        var resultado = await carga.CargarAsync(conSerie ? serie.Id : null, conEpisodio ? episodio.Id : null, Unir(EscenaValida));

        Assert.False(resultado.Ok);
        Assert.NotNull(resultado.Error);
        Assert.Null(await GuionGuardado());
        Assert.Equal((0, 0, 0), await ContarFilas());
    }

    [Fact]
    public async Task EpisodioDeOtraSerie_NoSePermite()
    {
        var otraSerie = (await new SeriesService(db).CrearAsync("Serie B")).Valor!;

        var resultado = await carga.CargarAsync(otraSerie.Id, episodio.Id, Unir(EscenaValida));

        Assert.False(resultado.Ok);
        Assert.Null(await GuionGuardado());
    }

    // AC-02c
    [Fact]
    public async Task GuionNuevoValido_ReemplazaAlAnteriorYBorraSusEscenas()
    {
        await CrearHojas("Ana");
        await carga.CargarAsync(serie.Id, episodio.Id, Unir(Escena(1, clips: 2), Escena(2), Escena(3)));
        await using (var contexto = db.CreateDbContext())
        {
            await contexto.Escenas.ExecuteUpdateAsync(s => s.SetProperty(e => e.Estado, EstadoGeneracion.Generada), Cancelar);
        }

        var guionNuevo = Unir(EscenaValida, Escena(2));
        var resultado = await carga.CargarAsync(serie.Id, episodio.Id, guionNuevo);

        Assert.True(resultado.Ok);
        Assert.Equal(guionNuevo, await GuionGuardado());
        var escenas = await episodios.ListarEscenasAsync(episodio.Id);
        Assert.Equal([1, 2], escenas.Select(e => e.Numero));
        Assert.Equal("RESTAURANTE", escenas[0].Lugar);
        Assert.Equal((2, 3, 2), await ContarFilas());
    }

    // AC-02d
    [Fact]
    public async Task GuionNuevoInvalido_ConservaElAnterior()
    {
        var guionAnterior = Unir(EscenaValida);
        await carga.CargarAsync(serie.Id, episodio.Id, guionAnterior);
        var escenasAntes = await episodios.ListarEscenasAsync(episodio.Id);

        var resultado = await carga.CargarAsync(serie.Id, episodio.Id, "ESCENA 1 — I/E CASA — DÍA");

        Assert.False(resultado.Ok);
        Assert.NotEmpty(resultado.ErroresGuion);
        Assert.Equal(guionAnterior, await GuionGuardado());
        var escenasDespues = await episodios.ListarEscenasAsync(episodio.Id);
        Assert.Equal(escenasAntes.Select(e => e.Id), escenasDespues.Select(e => e.Id));
        Assert.Equal((1, 2, 2), await ContarFilas());
    }

    // AC-03f
    [Fact]
    public async Task GuionInvalidoEnEpisodioSinGuion_NoGuardaNada()
    {
        var resultado = await carga.CargarAsync(serie.Id, episodio.Id, Unir(Escena(1), Escena(3)));

        Assert.False(resultado.Ok);
        Assert.Single(resultado.ErroresGuion);
        Assert.Null(await GuionGuardado());
        Assert.Equal((0, 0, 0), await ContarFilas());
    }

    // AC-06j
    [Fact]
    public async Task GuionValido_DejaLasEscenasPendientes()
    {
        await carga.CargarAsync(serie.Id, episodio.Id, Unir(Escena(1), Escena(2)));

        var escenas = await episodios.ListarEscenasAsync(episodio.Id);

        Assert.Equal(2, escenas.Count);
        Assert.All(escenas, e => Assert.Equal(EstadoGeneracion.Pendiente, e.Estado));
    }

    [Fact]
    public async Task GuionValido_GuardaLosPersonajesDetectadosEnEscenaYClips()
    {
        await CrearHojas("Ana", "José Daniel", "Lucía");

        await carga.CargarAsync(serie.Id, episodio.Id, Unir(EscenaValida));

        var escena = Assert.Single(await episodios.ListarEscenasAsync(episodio.Id));
        Assert.Equal(["Ana", "José Daniel"], escena.Personajes.Select(p => p.Nombre).Order());
        Assert.All(escena.Clips, c => Assert.Equal(["Ana", "José Daniel"], c.Personajes.Select(p => p.Nombre).Order()));
    }

    // AC-05d
    [Fact]
    public async Task DialogoDePersonajeSinHoja_EsValidoYNoSeDetecta()
    {
        await CrearHojas("Ana");

        var resultado = await carga.CargarAsync(
            serie.Id, episodio.Id, Unir(Escena(1, contenidoPorClip: [["Ana espera.", "MESERO: \"¿Algo más?\""]])));

        Assert.True(resultado.Ok);
        var escena = Assert.Single(await episodios.ListarEscenasAsync(episodio.Id));
        var clip = Assert.Single(escena.Clips);
        Assert.Equal("MESERO", Assert.Single(clip.Dialogos).Personaje);
        Assert.Equal(["Ana"], clip.Personajes.Select(p => p.Nombre));
        Assert.Equal(["Ana"], escena.Personajes.Select(p => p.Nombre));
    }
}
