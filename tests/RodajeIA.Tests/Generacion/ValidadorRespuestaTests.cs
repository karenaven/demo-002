using RodajeIA.Web.Datos;
using RodajeIA.Web.Generacion;

namespace RodajeIA.Tests.Generacion;

public class ValidadorRespuestaTests
{
    private static readonly Vocabulario Vocabulario = EscenarioGeneracion.Config.Vocabulario;

    /// <summary>Escena con un clip por elemento; cada número indica cuántas líneas de diálogo tiene ese clip.</summary>
    private static Escena EscenaCon(params int[] lineasPorClip) => new()
    {
        Lugar = "RESTAURANTE",
        MomentoDelDia = "NOCHE",
        Locacion = "L",
        Iluminacion = "I",
        PuestaEnEscena = "P",
        Audio = "A",
        Clips = lineasPorClip.Select((lineas, i) => new Clip
        {
            Numero = i + 1,
            Accion = $"Acción {i + 1}.",
            Dialogos = Enumerable.Range(1, lineas)
                .Select(orden => new LineaDialogo { Orden = orden, Personaje = "ANA", Texto = $"Línea {orden}." })
                .ToList(),
        }).ToList(),
    };

    private static TomaGenerada Toma(params string[] dialogos) =>
        new("plano medio", "50mm", "continuidad con la luz base", null, null, "Ana sonríe.", [.. dialogos]);

    private static RespuestaGeneracion Respuesta(params BloqueGenerado[] bloques) => new([.. bloques]);

    private static BloqueGenerado Bloque(int clip, params TomaGenerada[] tomas) => new(clip, [.. tomas]);

    // AC-06a (validación)
    [Fact]
    public void RespuestaCorrecta_EsValida()
    {
        var escena = EscenaCon(2, 0, 1);
        var respuesta = Respuesta(
            Bloque(1, Toma("c1-l1"), Toma("c1-l2") with { Angulo = "picado", Movimiento = "paneo" }),
            Bloque(2, Toma()),
            Bloque(3, Toma("c3-l1")));

        Assert.Empty(ValidadorRespuesta.Validar(respuesta, escena, Vocabulario));
    }

    // AC-06b
    [Fact]
    public void ClipConDosLineas_ReferenciaSoloUna_EsFallida()
    {
        var respuesta = Respuesta(Bloque(1, Toma("c1-l1")));

        Assert.NotEmpty(ValidadorRespuesta.Validar(respuesta, EscenaCon(2), Vocabulario));
    }

    // AC-06c
    [Fact]
    public void LineaReferenciadaEnDosTomas_EsFallida()
    {
        var respuesta = Respuesta(Bloque(1, Toma("c1-l1"), Toma("c1-l1")));

        Assert.NotEmpty(ValidadorRespuesta.Validar(respuesta, EscenaCon(1), Vocabulario));
    }

    // AC-06d
    [Theory]
    [InlineData("c2-l1")]
    [InlineData("c1-l9")]
    [InlineData("inventado")]
    public void ReferenciaAUnaLineaAjenaAlClip_EsFallida(string id)
    {
        var respuesta = Respuesta(Bloque(1, Toma("c1-l1", id)), Bloque(2, Toma("c2-l1")));

        Assert.NotEmpty(ValidadorRespuesta.Validar(respuesta, EscenaCon(1, 1), Vocabulario));
    }

    // AC-06e
    [Fact]
    public void MenosBloquesQueClips_EsFallida()
    {
        var respuesta = Respuesta(Bloque(1, Toma()), Bloque(2, Toma()));

        Assert.NotEmpty(ValidadorRespuesta.Validar(respuesta, EscenaCon(0, 0, 0), Vocabulario));
    }

    // AC-06g
    [Fact]
    public void ClipRepetidoEnLosBloques_EsFallida()
    {
        var respuesta = Respuesta(Bloque(1, Toma()), Bloque(1, Toma()), Bloque(3, Toma()));

        Assert.NotEmpty(ValidadorRespuesta.Validar(respuesta, EscenaCon(0, 0, 0), Vocabulario));
    }

    // AC-06h
    [Fact]
    public void BloqueSinTomas_EsFallida()
    {
        var respuesta = Respuesta(Bloque(1));

        Assert.NotEmpty(ValidadorRespuesta.Validar(respuesta, EscenaCon(0), Vocabulario));
    }

    // AC-06i
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TomaSinAccion_EsFallida(string? accion)
    {
        var respuesta = Respuesta(Bloque(1, Toma() with { Accion = accion }));

        Assert.NotEmpty(ValidadorRespuesta.Validar(respuesta, EscenaCon(0), Vocabulario));
    }

    // RN-02
    [Fact]
    public void ValorTecnicoFueraDelVocabulario_EsFallida()
    {
        var respuesta = Respuesta(Bloque(1, Toma() with { Plano = "plano americano" }));

        Assert.NotEmpty(ValidadorRespuesta.Validar(respuesta, EscenaCon(0), Vocabulario));
    }

    [Fact]
    public void FaltaUnCampoObligatorio_EsFallida()
    {
        var respuesta = Respuesta(Bloque(1, Toma() with { Optica = null }));

        Assert.NotEmpty(ValidadorRespuesta.Validar(respuesta, EscenaCon(0), Vocabulario));
    }

    [Theory]
    [InlineData("no es json")]
    [InlineData("{}")]
    [InlineData("")]
    public void RespuestaQueNoEsElJsonPedido_EsFallida(string json)
    {
        Assert.NotEmpty(ValidadorRespuesta.Validar(RespuestaGeneracion.Parsear(json), EscenaCon(0), Vocabulario));
    }
}
