using RodajeIA.Web.Datos;
using RodajeIA.Web.Servicios;
using static RodajeIA.Tests.Guiones.GuionesDeEjemplo;

namespace RodajeIA.Tests.Generacion;

public sealed class TomasTests : IDisposable
{
    private readonly EscenarioGeneracion e = new();
    private readonly TomasService tomas;

    public TomasTests() => tomas = new TomasService(e.Db, EscenarioGeneracion.Config);

    public void Dispose() => e.Dispose();

    /// <summary>Genera la escena de ejemplo con una toma por clip en plano medio y la acción "Ana sonríe."; solo la del clip 1 va en picado.</summary>
    private async Task<Toma> GenerarAsync()
    {
        await e.CargarGuionAsync(Unir(EscenaValida));
        e.Gemini.Responder = (instruccion, _) => GeminiFalso.Json(GeminiFalso.Clips(instruccion).Select(c => new
        {
            clip = c.Numero,
            tomas = new[] { new { plano = "plano medio", optica = "50mm", iluminacion = "continuidad con la luz base", angulo = c.Numero == 1 ? "picado" : null, accion = "Ana sonríe.", dialogos = c.Lineas } },
        }));
        await e.GenerarEpisodioAsync();
        return await PrimeraTomaAsync();
    }

    private async Task<Toma> PrimeraTomaAsync() =>
        (await tomas.ListarBloquesAsync((await e.EscenaAsync(1)).Id))[0].Bloque!.Tomas[0];

    private static DatosToma Con(Toma toma, Action<DatosToma> cambio)
    {
        var datos = DatosToma.De(toma);
        cambio(datos);
        return datos;
    }

    // AC-07a
    [Fact]
    public async Task Revision_MuestraTodosLosBloquesConSusTomasYDialogos()
    {
        await e.CargarGuionAsync(Unir(EscenaValida));
        e.Gemini.Responder = (_, _) => GeminiFalso.Json(new object[]
        {
            new
            {
                clip = 1,
                tomas = new[]
                {
                    new { plano = "plano medio", optica = "50mm", iluminacion = "luz suave de relleno", accion = "Ana habla.", dialogos = new[] { "c1-l1" } },
                    new { plano = "primer plano", optica = "85mm", iluminacion = "luz suave de relleno", accion = "José Daniel responde.", dialogos = new[] { "c1-l2" } },
                },
            },
            new { clip = 2, tomas = new[] { new { plano = "plano general", optica = "35mm", iluminacion = "luz suave de relleno", accion = "Llega el mesero.", dialogos = Array.Empty<string>() } } },
        });
        await e.GenerarEpisodioAsync();

        var clips = await tomas.ListarBloquesAsync((await e.EscenaAsync(1)).Id);

        Assert.Equal([1, 2], clips.Select(c => c.Numero));
        Assert.Equal(["Ana habla.", "José Daniel responde."], clips[0].Bloque!.Tomas.Select(t => t.Accion));
        Assert.Equal(["Llega el mesero."], clips[1].Bloque!.Tomas.Select(t => t.Accion));
        Assert.Equal("Mentiroso.", Assert.Single(clips[0].Bloque!.Tomas[0].Dialogos).LineaDialogo!.Texto);
    }

    // AC-07b
    [Fact]
    public async Task EditarPlano_ElPromptUsaElNuevo()
    {
        var toma = await GenerarAsync();

        var resultado = await tomas.EditarAsync(toma.Id, Con(toma, d => d.Plano = "primer plano"));

        Assert.True(resultado.Ok);
        var prompt = await e.PromptAsync(1);
        Assert.Contains("Bloque 1:\nprimer plano", prompt);
        Assert.DoesNotContain("Bloque 1:\nplano medio", prompt);
    }

    // AC-07c
    [Fact]
    public async Task EditarAccion_ElPromptUsaLaNueva()
    {
        var toma = await GenerarAsync();

        await tomas.EditarAsync(toma.Id, Con(toma, d => d.Accion = "Ana mira por la ventana."));

        var bloque1 = Assert.Single((await e.PromptAsync(1)).Split("\n\n"), s => s.StartsWith("Bloque 1:", StringComparison.Ordinal));
        Assert.Contains("Ana mira por la ventana.", bloque1);
        Assert.DoesNotContain("Ana sonríe.", bloque1);
    }

    // AC-07d
    [Theory]
    [InlineData("plano americano")]
    [InlineData("PLANO MEDIO")]
    public async Task ValorFueraDelVocabulario_NoSeAceptaYLaTomaSeConserva(string plano)
    {
        var toma = await GenerarAsync();

        var resultado = await tomas.EditarAsync(toma.Id, Con(toma, d => d.Plano = plano));

        Assert.False(resultado.Ok);
        Assert.Contains("no es un valor permitido", Assert.Single(resultado.Errores));
        Assert.Equal("plano medio", (await PrimeraTomaAsync()).Plano);
    }

    [Fact]
    public async Task CampoObligatorioOAccionVacios_NoSeGuardan()
    {
        var toma = await GenerarAsync();

        var resultado = await tomas.EditarAsync(toma.Id, Con(toma, d =>
        {
            d.Optica = null;
            d.Accion = "  ";
        }));

        Assert.False(resultado.Ok);
        Assert.Equal(["Falta la óptica.", "Falta la acción."], resultado.Errores);
        var guardada = await PrimeraTomaAsync();
        Assert.Equal("50mm", guardada.Optica);
        Assert.Equal("Ana sonríe.", guardada.Accion);
    }

    // AC-07e: la edición queda en la base, así que sobrevive a un reinicio.
    [Fact]
    public async Task TomaEditada_SeLeeDeLaBaseConElValorNuevo()
    {
        var toma = await GenerarAsync();

        await tomas.EditarAsync(toma.Id, Con(toma, d => d.Plano = "primer plano"));

        var deOtroServicio = await new TomasService(e.Db, EscenarioGeneracion.Config).ListarBloquesAsync((await e.EscenaAsync(1)).Id);
        Assert.Equal("primer plano", deOtroServicio[0].Bloque!.Tomas[0].Plano);
    }

    // AC-07f
    [Fact]
    public async Task VaciarElAngulo_QuedaSinAnguloYElPromptNoLoMenciona()
    {
        var toma = await GenerarAsync();
        Assert.Contains("picado", await e.PromptAsync(1));

        var resultado = await tomas.EditarAsync(toma.Id, Con(toma, d => d.Angulo = ""));

        Assert.True(resultado.Ok);
        Assert.Null((await PrimeraTomaAsync()).Angulo);
        Assert.DoesNotContain("picado", await e.PromptAsync(1));
    }

    [Fact]
    public async Task EditarToma_NoCambiaSusDialogos()
    {
        var toma = await GenerarAsync();

        await tomas.EditarAsync(toma.Id, Con(toma, d => d.Accion = "Ana mira por la ventana."));

        Assert.Contains("ANA: \"Mentiroso.\" JOSÉ DANIEL (sonriendo): \"Bueno... pasó parecido.\"", await e.PromptAsync(1));
    }
}
