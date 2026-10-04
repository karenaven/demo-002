using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Servicios;
using static RodajeIA.Tests.Guiones.GuionesDeEjemplo;

namespace RodajeIA.Tests.Generacion;

public sealed class VariantesTests : IDisposable
{
    private readonly EscenarioGeneracion e = new();
    private Personaje ana = null!;
    private Personaje jose = null!;
    private int escena2;

    public void Dispose() => e.Dispose();

    private static CancellationToken Cancelar => TestContext.Current.CancellationToken;

    private async Task PrepararAsync()
    {
        ana = await e.CrearHojaAsync("Ana", vestuario: "chaqueta negra");
        jose = await e.CrearHojaAsync("José Daniel");
        await e.CargarGuionAsync(Unir(Escena(1, contenidoPorClip: [["Ana corre."]]), Escena(2, contenidoPorClip: [["Ana y José Daniel cenan."]])));
        escena2 = (await e.EscenaAsync(2)).Id;
    }

    private async Task<VariantePersonaje> CrearAsync(Personaje personaje, DatosVariante datos)
    {
        var resultado = await e.Variantes.CrearAsync(escena2, personaje.Id, datos);
        Assert.True(resultado.Ok, string.Join("; ", resultado.Errores));
        return resultado.Valor!;
    }

    private async Task<VariantePersonaje?> GuardadaAsync(int id)
    {
        await using var db = e.Db.CreateDbContext();
        return await db.Variantes.AsNoTracking().SingleOrDefaultAsync(v => v.Id == id, Cancelar);
    }

    [Fact]
    public async Task CrearVariante_GuardaSoloLosCamposQueCambian()
    {
        await PrepararAsync();

        var variante = await CrearAsync(ana, new DatosVariante { Vestuario = " abrigo gris ", Peinado = "trenza larga", Edad = "" });

        var guardada = await GuardadaAsync(variante.Id);
        Assert.Equal("abrigo gris", guardada!.Vestuario);
        Assert.Null(guardada.Peinado);
        Assert.Null(guardada.Edad);
        Assert.Null(guardada.Heridas);
    }

    // AC-09b
    [Fact]
    public async Task ListarVariantes_MuestraLasDeLaEscena()
    {
        await PrepararAsync();
        await CrearAsync(ana, new DatosVariante { Vestuario = "abrigo gris" });
        await CrearAsync(jose, new DatosVariante { Peinado = "rapado" });

        var variantes = await e.Variantes.ListarAsync(escena2);

        Assert.Equal(["Ana", "José Daniel"], variantes.Select(v => v.Personaje!.Nombre));
    }

    // AC-09c
    [Fact]
    public async Task EditarVariante_GuardaElCambio()
    {
        await PrepararAsync();
        var variante = await CrearAsync(ana, new DatosVariante { Vestuario = "vestido rojo" });

        var resultado = await e.Variantes.EditarAsync(variante.Id, new DatosVariante { Vestuario = "abrigo gris" });

        Assert.True(resultado.Ok);
        Assert.Equal("abrigo gris", (await GuardadaAsync(variante.Id))!.Vestuario);
    }

    // AC-09d
    [Fact]
    public async Task EliminarVariante_NoTocaLaHojaBase()
    {
        await PrepararAsync();
        var variante = await CrearAsync(ana, new DatosVariante { Vestuario = "abrigo gris" });

        Assert.True((await e.Variantes.EliminarAsync(variante.Id)).Ok);

        Assert.Null(await GuardadaAsync(variante.Id));
        await using var db = e.Db.CreateDbContext();
        Assert.Equal("chaqueta negra", (await db.Personajes.SingleAsync(p => p.Id == ana.Id, Cancelar)).Vestuario);
    }

    // AC-09e
    [Fact]
    public async Task SegundaVarianteDelMismoPersonaje_NoSeGuarda()
    {
        await PrepararAsync();
        await CrearAsync(ana, new DatosVariante { Vestuario = "abrigo gris" });

        var resultado = await e.Variantes.CrearAsync(escena2, ana.Id, new DatosVariante { Peinado = "moño" });

        Assert.False(resultado.Ok);
        Assert.Contains("ya tiene una variante", Assert.Single(resultado.Errores));
        Assert.Single(await e.Variantes.ListarAsync(escena2));
    }

    // AC-09f
    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("chaqueta negra")]
    public async Task VarianteSinCambios_NoSeGuarda(string? vestuario)
    {
        await PrepararAsync();

        var resultado = await e.Variantes.CrearAsync(escena2, ana.Id, new DatosVariante { Vestuario = vestuario });

        Assert.False(resultado.Ok);
        Assert.Equal(VariantesService.SinCambios, Assert.Single(resultado.Errores));
        Assert.Empty(await e.Variantes.ListarAsync(escena2));
    }

    // AC-09g
    [Fact]
    public async Task EditarDejandoLaVarianteSinCambios_NoSeGuarda()
    {
        await PrepararAsync();
        var variante = await CrearAsync(ana, new DatosVariante { Vestuario = "abrigo gris" });

        var resultado = await e.Variantes.EditarAsync(variante.Id, new DatosVariante { Vestuario = "" });

        Assert.False(resultado.Ok);
        Assert.Equal(VariantesService.SinCambios, Assert.Single(resultado.Errores));
        Assert.Equal("abrigo gris", (await GuardadaAsync(variante.Id))!.Vestuario);
    }

    [Fact]
    public async Task PersonajeAusenteEnLaEscena_NoAdmiteVariante()
    {
        await PrepararAsync();
        var escena1 = (await e.EscenaAsync(1)).Id;

        var resultado = await e.Variantes.CrearAsync(escena1, jose.Id, new DatosVariante { Vestuario = "abrigo gris" });

        Assert.False(resultado.Ok);
    }

    // AC-09i, AC-09l
    [Theory]
    [InlineData(EstadoGeneracion.Generada, "ya está generada")]
    [InlineData(EstadoGeneracion.Generando, "se está generando")]
    public async Task EscenaGenerandoOGenerada_NoAdmiteCrearVariantes(EstadoGeneracion estado, string motivo)
    {
        await PrepararAsync();
        await e.CambiarEstadoAsync(2, estado);

        var resultado = await e.Variantes.CrearAsync(escena2, ana.Id, new DatosVariante { Vestuario = "abrigo gris" });

        Assert.False(resultado.Ok);
        Assert.Contains(motivo, Assert.Single(resultado.Errores));
        Assert.Empty(await e.Variantes.ListarAsync(escena2));
    }

    // AC-09j
    [Fact]
    public async Task EscenaGenerada_NoAdmiteEditarVariantes()
    {
        await PrepararAsync();
        var variante = await CrearAsync(ana, new DatosVariante { Vestuario = "abrigo gris" });
        await e.CambiarEstadoAsync(2, EstadoGeneracion.Generada);

        var resultado = await e.Variantes.EditarAsync(variante.Id, new DatosVariante { Vestuario = "vestido rojo" });

        Assert.False(resultado.Ok);
        Assert.Equal("abrigo gris", (await GuardadaAsync(variante.Id))!.Vestuario);
    }

    // AC-09k
    [Fact]
    public async Task EscenaGenerada_NoAdmiteEliminarVariantes()
    {
        await PrepararAsync();
        var variante = await CrearAsync(ana, new DatosVariante { Vestuario = "abrigo gris" });
        await e.CambiarEstadoAsync(2, EstadoGeneracion.Generada);

        Assert.False((await e.Variantes.EliminarAsync(variante.Id)).Ok);

        Assert.NotNull(await GuardadaAsync(variante.Id));
    }

    [Fact]
    public async Task EscenaEnError_AdmiteCambiarVariantesAntesDeReintentar()
    {
        await PrepararAsync();
        await e.CambiarEstadoAsync(2, EstadoGeneracion.Error);

        await CrearAsync(ana, new DatosVariante { Vestuario = "abrigo gris" });
    }

    // AC-09m
    [Fact]
    public async Task VarianteEliminadaAntesDeGenerar_ElPromptUsaLaHojaBase()
    {
        await PrepararAsync();
        var variante = await CrearAsync(ana, new DatosVariante { Vestuario = "abrigo gris" });
        Assert.True((await e.Variantes.EliminarAsync(variante.Id)).Ok);

        await e.GenerarEpisodioAsync();

        var prompt = await e.PromptAsync(2);
        Assert.Contains("chaqueta negra", prompt);
        Assert.DoesNotContain("abrigo gris", prompt);
    }

    // AC-01h (en la base de datos; la pantalla para eliminar hojas es de RF-01h)
    [Fact]
    public async Task BorrarLaHoja_BorraSusVariantesAunqueLaEscenaEsteGenerada()
    {
        await PrepararAsync();
        var variante = await CrearAsync(ana, new DatosVariante { Vestuario = "abrigo gris" });
        await e.CambiarEstadoAsync(2, EstadoGeneracion.Generada);

        await using (var db = e.Db.CreateDbContext())
        {
            await db.Personajes.Where(p => p.Id == ana.Id).ExecuteDeleteAsync(Cancelar);
        }

        Assert.Null(await GuardadaAsync(variante.Id));
    }

    // AC-02c: recargar el guion borra los bloques, tomas y variantes del anterior.
    [Fact]
    public async Task RecargarElGuion_BorraBloquesYVariantesDelAnterior()
    {
        await PrepararAsync();
        await CrearAsync(ana, new DatosVariante { Vestuario = "abrigo gris" });
        await e.GenerarEpisodioAsync();

        await e.CargarGuionAsync(Unir(Escena(1)));

        await using var db = e.Db.CreateDbContext();
        Assert.Equal(0, await db.Variantes.CountAsync(Cancelar));
        Assert.Equal(0, await db.Bloques.CountAsync(Cancelar));
        Assert.Equal(0, await db.Tomas.CountAsync(Cancelar));
    }
}
