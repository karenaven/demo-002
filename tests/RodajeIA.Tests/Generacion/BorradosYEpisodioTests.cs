using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Servicios;
using static RodajeIA.Tests.Guiones.GuionesDeEjemplo;

namespace RodajeIA.Tests.Generacion;

public sealed class BorradosYEpisodioTests : IDisposable
{
    private readonly EscenarioGeneracion e = new();

    public void Dispose() => e.Dispose();

    private static CancellationToken Cancelar => TestContext.Current.CancellationToken;

    /// <summary>Episodio con guion, escenas generadas y una variante de Ana en la escena 2.</summary>
    private async Task PrepararGeneradoAsync()
    {
        var ana = await e.CrearHojaAsync("Ana");
        await e.CargarGuionAsync(Unir(Escena(1, contenidoPorClip: [["Ana corre.", "ANA: \"Hola.\""]]), Escena(2, contenidoPorClip: [["Ana cena."]])));
        Assert.True((await e.Variantes.CrearAsync((await e.EscenaAsync(2)).Id, ana.Id, new DatosVariante { Vestuario = "abrigo gris" })).Ok);
        await e.GenerarEpisodioAsync();
    }

    private async Task<Dictionary<string, int>> ContarFilasAsync()
    {
        await using var db = e.Db.CreateDbContext();
        return new()
        {
            ["series"] = await db.Series.CountAsync(Cancelar),
            ["hojas"] = await db.Personajes.CountAsync(Cancelar),
            ["episodios"] = await db.Episodios.CountAsync(Cancelar),
            ["escenas"] = await db.Escenas.CountAsync(Cancelar),
            ["clips"] = await db.Clips.CountAsync(Cancelar),
            ["dialogos"] = await db.LineasDialogo.CountAsync(Cancelar),
            ["bloques"] = await db.Bloques.CountAsync(Cancelar),
            ["tomas"] = await db.Tomas.CountAsync(Cancelar),
            ["variantes"] = await db.Variantes.CountAsync(Cancelar),
        };
    }

    // AC-01d
    [Fact]
    public async Task EliminarSerie_BorraTodoLoQueDependeDeElla()
    {
        await PrepararGeneradoAsync();
        Assert.All(await ContarFilasAsync(), fila => Assert.True(fila.Value > 0, fila.Key));

        Assert.True(await new SeriesService(e.Db).EliminarAsync(e.Serie.Id));

        Assert.All(await ContarFilasAsync(), fila => Assert.Equal(0, fila.Value));
    }

    // AC-11d
    [Fact]
    public async Task EliminarEpisodio_BorraSuGuionEscenasBloquesYVariantes()
    {
        await PrepararGeneradoAsync();

        Assert.True(await new EpisodiosService(e.Db).EliminarAsync(e.Episodio.Id));

        var filas = await ContarFilasAsync();
        Assert.Equal(1, filas["series"]);
        Assert.Equal(1, filas["hojas"]);
        Assert.All(filas.Where(f => f.Key is not ("series" or "hojas")), fila => Assert.Equal(0, fila.Value));
    }

    [Fact]
    public async Task EliminarEpisodio_NoTocaLosOtrosEpisodiosDeLaSerie()
    {
        await PrepararGeneradoAsync();
        var episodios = new EpisodiosService(e.Db);
        var otro = (await episodios.CrearAsync(e.Serie.Id, new DatosEpisodio { Numero = 2, Titulo = "La cena" })).Valor!;

        await episodios.EliminarAsync(e.Episodio.Id);

        Assert.Equal([otro.Id], (await episodios.ListarAsync(e.Serie.Id)).Select(ep => ep.Id));
    }

    // AC-11e
    [Fact]
    public async Task AbrirEpisodio_MuestraLasEscenasEnOrdenConPromptSoloLasGeneradas()
    {
        await e.CargarGuionAsync(Unir(Escena(1), Escena(2), Escena(3)));
        e.Gemini.Responder = (instruccion, _) => instruccion.Contains("ESCENA 3 —")
            ? throw new HttpRequestException("503")
            : GeminiFalso.RespuestaValida(instruccion);
        await e.GenerarEpisodioAsync();

        var escenas = await new EpisodiosService(e.Db).ListarEscenasAsync(e.Episodio.Id);
        var prompts = await e.Prompts.ArmarEpisodioAsync(e.Episodio.Id);

        Assert.Equal([1, 2, 3], escenas.Select(s => s.Numero));
        Assert.Equal([EstadoGeneracion.Generada, EstadoGeneracion.Generada, EstadoGeneracion.Error], escenas.Select(s => s.Estado));
        Assert.Equal(escenas.Take(2).Select(s => s.Id).Order(), prompts.Keys.Order());
    }
}
