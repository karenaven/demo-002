using RodajeIA.Web.Servicios;

namespace RodajeIA.Tests.Servicios;

public sealed class EpisodiosTests : IDisposable
{
    private readonly BaseDeDatosDePrueba db = new();
    private readonly SeriesService series;
    private readonly EpisodiosService episodios;

    public EpisodiosTests()
    {
        series = new SeriesService(db);
        episodios = new EpisodiosService(db);
    }

    public void Dispose() => db.Dispose();

    // AC-11a
    [Fact]
    public async Task CrearEpisodio_QuedaGuardadoEnLaSerie()
    {
        var serie = (await series.CrearAsync("Serie A")).Valor!;

        var resultado = await episodios.CrearAsync(serie.Id, new DatosEpisodio { Numero = 1, Titulo = "Piloto" });

        Assert.True(resultado.Ok);
        var guardado = Assert.Single(await episodios.ListarAsync(serie.Id));
        Assert.Equal(1, guardado.Numero);
        Assert.Equal("Piloto", guardado.Titulo);
    }

    // AC-11b
    [Fact]
    public async Task ListarEpisodios_MuestraLosDeLaSerieEnOrden()
    {
        var serie = (await series.CrearAsync("Serie A")).Valor!;
        var otra = (await series.CrearAsync("Serie B")).Valor!;
        await episodios.CrearAsync(serie.Id, new DatosEpisodio { Numero = 2, Titulo = "La cena" });
        await episodios.CrearAsync(serie.Id, new DatosEpisodio { Numero = 1, Titulo = "Piloto" });
        await episodios.CrearAsync(otra.Id, new DatosEpisodio { Numero = 1, Titulo = "Otro" });

        var listado = await episodios.ListarAsync(serie.Id);

        Assert.Equal(["Piloto", "La cena"], listado.Select(e => e.Titulo));
    }

    // AC-11f
    [Fact]
    public async Task NumeroRepetidoEnLaSerie_NoSeGuardaEIndicaQueExiste()
    {
        var serie = (await series.CrearAsync("Serie A")).Valor!;
        await episodios.CrearAsync(serie.Id, new DatosEpisodio { Numero = 1, Titulo = "Piloto" });

        var resultado = await episodios.CrearAsync(serie.Id, new DatosEpisodio { Numero = 1, Titulo = "Otro" });

        Assert.False(resultado.Ok);
        Assert.Contains("ya existe", Assert.Single(resultado.Errores), StringComparison.OrdinalIgnoreCase);
        Assert.Single(await episodios.ListarAsync(serie.Id));
    }

    [Fact]
    public async Task MismoNumeroEnOtraSerie_SePermite()
    {
        var serieA = (await series.CrearAsync("Serie A")).Valor!;
        var serieB = (await series.CrearAsync("Serie B")).Valor!;
        await episodios.CrearAsync(serieA.Id, new DatosEpisodio { Numero = 1, Titulo = "Piloto" });

        var resultado = await episodios.CrearAsync(serieB.Id, new DatosEpisodio { Numero = 1, Titulo = "Piloto" });

        Assert.True(resultado.Ok);
    }

    [Fact]
    public async Task EpisodioSinNumeroNiTitulo_NoSeGuarda()
    {
        var serie = (await series.CrearAsync("Serie A")).Valor!;

        var resultado = await episodios.CrearAsync(serie.Id, new DatosEpisodio());

        Assert.Equal(2, resultado.Errores.Count);
        Assert.Empty(await episodios.ListarAsync(serie.Id));
    }
}
