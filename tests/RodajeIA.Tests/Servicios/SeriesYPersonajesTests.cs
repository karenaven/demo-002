using RodajeIA.Web.Servicios;

namespace RodajeIA.Tests.Servicios;

public sealed class SeriesYPersonajesTests : IDisposable
{
    private readonly BaseDeDatosDePrueba db = new();
    private readonly SeriesService series;
    private readonly PersonajesService personajes;

    public SeriesYPersonajesTests()
    {
        series = new SeriesService(db);
        personajes = new PersonajesService(db);
    }

    public void Dispose() => db.Dispose();

    internal static DatosPersonaje HojaCompleta(string nombre = "Ana") => new()
    {
        Nombre = nombre,
        Edad = "28 años",
        DescripcionFisica = "Mujer latina, piel morena clara, ojos marrones; voz cálida y grave.",
        Peinado = "trenza larga",
        Vestuario = "chaqueta negra",
        Heridas = "cicatriz pequeña en la ceja izquierda",
        Personalidad = "Irónica y leal.",
        Rol = "Protagonista",
    };

    // AC-01a
    [Fact]
    public async Task CrearSerie_QuedaGuardada()
    {
        var resultado = await series.CrearAsync("Serie A");

        Assert.True(resultado.Ok);
        var guardada = await series.ObtenerAsync(resultado.Valor!.Id);
        Assert.Equal("Serie A", guardada!.Nombre);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CrearSerieSinNombre_NoSeGuarda(string? nombre)
    {
        var resultado = await series.CrearAsync(nombre);

        Assert.False(resultado.Ok);
        Assert.Empty(await series.ListarAsync());
    }

    // AC-01b
    [Fact]
    public async Task ListarSeries_MuestraTodas()
    {
        await series.CrearAsync("Serie A");
        await series.CrearAsync("Serie B");

        var listado = await series.ListarAsync();

        Assert.Equal(["Serie A", "Serie B"], listado.Select(s => s.Nombre));
    }

    // AC-01e
    [Fact]
    public async Task CrearHojaCompleta_QuedaGuardadaConEsosValores()
    {
        var serie = (await series.CrearAsync("Serie A")).Valor!;
        var hoja = HojaCompleta();

        var resultado = await personajes.CrearAsync(serie.Id, hoja);

        Assert.True(resultado.Ok);
        var guardada = Assert.Single(await personajes.ListarAsync(serie.Id));
        Assert.Equal(serie.Id, guardada.SerieId);
        Assert.Equal(hoja.Nombre, guardada.Nombre);
        Assert.Equal(hoja.Edad, guardada.Edad);
        Assert.Equal(hoja.DescripcionFisica, guardada.DescripcionFisica);
        Assert.Equal(hoja.Peinado, guardada.Peinado);
        Assert.Equal(hoja.Vestuario, guardada.Vestuario);
        Assert.Equal(hoja.Heridas, guardada.Heridas);
        Assert.Equal(hoja.Personalidad, guardada.Personalidad);
        Assert.Equal(hoja.Rol, guardada.Rol);
    }

    // AC-01f
    [Fact]
    public async Task ListarHojas_MuestraLasDeLaSerie()
    {
        var serieA = (await series.CrearAsync("Serie A")).Valor!;
        var serieB = (await series.CrearAsync("Serie B")).Valor!;
        await personajes.CrearAsync(serieA.Id, HojaCompleta("Ana"));
        await personajes.CrearAsync(serieA.Id, HojaCompleta("José Daniel"));
        await personajes.CrearAsync(serieB.Id, HojaCompleta("Otra"));

        var listado = await personajes.ListarAsync(serieA.Id);

        Assert.Equal(["Ana", "José Daniel"], listado.Select(p => p.Nombre));
    }

    // AC-01i
    [Fact]
    public async Task CrearHojaSinPeinado_NoSeGuardaEIndicaQueFalta()
    {
        var serie = (await series.CrearAsync("Serie A")).Valor!;
        var hoja = HojaCompleta();
        hoja.Peinado = "  ";

        var resultado = await personajes.CrearAsync(serie.Id, hoja);

        Assert.False(resultado.Ok);
        Assert.Equal(["Falta el peinado."], resultado.Errores);
        Assert.Empty(await personajes.ListarAsync(serie.Id));
    }

    // AC-01j
    [Fact]
    public async Task CrearHojaSinHeridas_QuedaGuardada()
    {
        var serie = (await series.CrearAsync("Serie A")).Valor!;
        var hoja = HojaCompleta();
        hoja.Heridas = null;

        var resultado = await personajes.CrearAsync(serie.Id, hoja);

        Assert.True(resultado.Ok);
        Assert.Null(Assert.Single(await personajes.ListarAsync(serie.Id)).Heridas);
    }

    [Fact]
    public async Task CrearHojaVacia_IndicaTodosLosCamposObligatorios()
    {
        var serie = (await series.CrearAsync("Serie A")).Valor!;

        var resultado = await personajes.CrearAsync(serie.Id, new DatosPersonaje());

        Assert.Equal(7, resultado.Errores.Count);
    }

    // AC-01c
    [Fact]
    public async Task EditarNombreDeSerie_ElListadoMuestraElNuevo()
    {
        var serie = (await series.CrearAsync("Serie A")).Valor!;

        var resultado = await series.EditarAsync(serie.Id, "Serie B");

        Assert.True(resultado.Ok);
        Assert.Equal(["Serie B"], (await series.ListarAsync()).Select(s => s.Nombre));
    }

    [Fact]
    public async Task EditarSerieSinNombre_NoCambia()
    {
        var serie = (await series.CrearAsync("Serie A")).Valor!;

        Assert.False((await series.EditarAsync(serie.Id, "  ")).Ok);

        Assert.Equal("Serie A", (await series.ObtenerAsync(serie.Id))!.Nombre);
    }

    // AC-01g
    [Fact]
    public async Task EditarVestuarioDeLaHoja_QuedaGuardado()
    {
        var serie = (await series.CrearAsync("Serie A")).Valor!;
        var hoja = (await personajes.CrearAsync(serie.Id, HojaCompleta())).Valor!;
        var datos = DatosPersonaje.De(hoja);
        datos.Vestuario = "camisa blanca";

        var resultado = await personajes.EditarAsync(hoja.Id, datos);

        Assert.True(resultado.Ok);
        var guardada = Assert.Single(await personajes.ListarAsync(serie.Id));
        Assert.Equal("camisa blanca", guardada.Vestuario);
        Assert.Equal(hoja.Peinado, guardada.Peinado);
    }

    // AC-01k
    [Fact]
    public async Task EditarHojaDejandoElPeinadoVacio_NoSeGuardaEIndicaQueFalta()
    {
        var serie = (await series.CrearAsync("Serie A")).Valor!;
        var hoja = (await personajes.CrearAsync(serie.Id, HojaCompleta())).Valor!;
        var datos = DatosPersonaje.De(hoja);
        datos.Peinado = "";
        datos.Vestuario = "camisa blanca";

        var resultado = await personajes.EditarAsync(hoja.Id, datos);

        Assert.False(resultado.Ok);
        Assert.Equal(["Falta el peinado."], resultado.Errores);
        var guardada = Assert.Single(await personajes.ListarAsync(serie.Id));
        Assert.Equal("trenza larga", guardada.Peinado);
        Assert.Equal("chaqueta negra", guardada.Vestuario);
    }

    [Fact]
    public async Task EditarHoja_PermiteQuitarLasHeridas()
    {
        var serie = (await series.CrearAsync("Serie A")).Valor!;
        var hoja = (await personajes.CrearAsync(serie.Id, HojaCompleta())).Valor!;
        var datos = DatosPersonaje.De(hoja);
        datos.Heridas = " ";

        Assert.True((await personajes.EditarAsync(hoja.Id, datos)).Ok);

        Assert.Null(Assert.Single(await personajes.ListarAsync(serie.Id)).Heridas);
    }
}
