using RodajeIA.Web.Guiones;
using static RodajeIA.Tests.Guiones.GuionesDeEjemplo;

namespace RodajeIA.Tests.Guiones;

public class DetectorPersonajesTests
{
    private static PersonajesDetectados Detectar(string[] escena, params string[] nombresConHoja)
    {
        var resultado = GuionParser.Parsear(Unir(escena));
        Assert.True(resultado.EsValido, string.Join("; ", resultado.Errores));
        return DetectorPersonajes.Detectar(Assert.Single(resultado.Escenas), nombresConHoja);
    }

    [Fact]
    public void EscenaDeEjemplo_DetectaAnaYJoseDaniel()
    {
        var detectados = Detectar(EscenaValida, "Ana", "José Daniel");

        Assert.Equal(["Ana", "José Daniel"], detectados.EnEscena);
        Assert.Equal(["Ana", "José Daniel"], detectados.PorClip[0]);
        Assert.Equal(["Ana", "José Daniel"], detectados.PorClip[1]);
    }

    // AC-05a
    [Fact]
    public void NombreEnLaAccion_DetectaEnClipYEscena()
    {
        var detectados = Detectar(Escena(1, contenidoPorClip: [["Ana se sienta."]]), "Ana");

        Assert.Equal(["Ana"], detectados.PorClip[0]);
        Assert.Equal(["Ana"], detectados.EnEscena);
    }

    // AC-05b
    [Fact]
    public void MarcadorDeDialogo_DetectaEnClipYEscena()
    {
        var detectados = Detectar(Escena(1, contenidoPorClip: [["Alguien toca la puerta.", "ANA: \"Claro.\""]]), "Ana");

        Assert.Equal(["Ana"], detectados.PorClip[0]);
        Assert.Equal(["Ana"], detectados.EnEscena);
    }

    [Fact]
    public void MarcadorDeDialogoConTildeYAcotacion_Detecta()
    {
        var detectados = Detectar(
            Escena(1, contenidoPorClip: [["Alguien toca la puerta.", "JOSÉ DANIEL (gritando): \"¡Voy!\""]]), "José Daniel");

        Assert.Equal(["José Daniel"], detectados.PorClip[0]);
    }

    // AC-05c
    [Fact]
    public void PersonajeSinHojaEnLaAccion_NoSeDetecta()
    {
        var detectados = Detectar(Escena(1, contenidoPorClip: [["Mesero trae la cuenta."]]), "Ana");

        Assert.Empty(detectados.PorClip[0]);
        Assert.Empty(detectados.EnEscena);
    }

    // AC-05d (parte de detección)
    [Fact]
    public void DialogoDePersonajeSinHoja_NoSeDetecta()
    {
        var detectados = Detectar(Escena(1, contenidoPorClip: [["MESERO: \"¿Algo más?\""]]), "Ana");

        Assert.Empty(detectados.PorClip[0]);
        Assert.Empty(detectados.EnEscena);
    }

    // AC-05e
    [Fact]
    public void NombreComoParteDeOtraPalabra_NoSeDetecta()
    {
        var detectados = Detectar(Escena(1, contenidoPorClip: [["Anabel se sienta."]]), "Ana");

        Assert.Empty(detectados.PorClip[0]);
    }

    // AC-05f
    [Fact]
    public void NombreSinTilde_NoSeDetecta()
    {
        var detectados = Detectar(Escena(1, contenidoPorClip: [["Jose Daniel se sienta."]]), "José Daniel");

        Assert.Empty(detectados.PorClip[0]);
    }

    [Fact]
    public void NombreEnMinuscula_NoSeDetecta()
    {
        var detectados = Detectar(Escena(1, contenidoPorClip: [["La ana del cuento."]]), "Ana");

        Assert.Empty(detectados.PorClip[0]);
    }

    [Fact]
    public void NombreSeguidoDePuntuacion_SeDetecta()
    {
        var detectados = Detectar(Escena(1, contenidoPorClip: [["Todos miran a Ana."]]), "Ana");

        Assert.Equal(["Ana"], detectados.PorClip[0]);
    }

    // AC-05g
    [Fact]
    public void NombreSoloEnPuestaEnEscena_DetectaEnEscenaYNoEnClips()
    {
        var detectados = Detectar(
            Escena(1, clips: 2, puestaEnEscena: "Ana espera junto a la ventana."), "Ana");

        Assert.Equal(["Ana"], detectados.EnEscena);
        Assert.All(detectados.PorClip, Assert.Empty);
    }

    [Fact]
    public void TextoEnPantallaYTextoDeDialogo_NoCuentanComoMencion()
    {
        var detectados = Detectar(
            Escena(1, contenidoPorClip: [["Un mensaje llega.", "TEXTO EN PANTALLA: \"Ana, llámame\"", "MESERO: \"¿Usted es Ana?\""]]),
            "Ana");

        Assert.Empty(detectados.PorClip[0]);
    }

    [Fact]
    public void SoloDetectaEnLosClipsDondeAparece()
    {
        var detectados = Detectar(
            Escena(1, contenidoPorClip: [["Ana entra."], ["José Daniel entra."]]), "Ana", "José Daniel");

        Assert.Equal(["Ana"], detectados.PorClip[0]);
        Assert.Equal(["José Daniel"], detectados.PorClip[1]);
        Assert.Equal(["Ana", "José Daniel"], detectados.EnEscena);
    }
}
