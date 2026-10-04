using RodajeIA.Web.Generacion;

namespace RodajeIA.Tests.Generacion;

public class SchemaYPlantillaTests
{
    private static readonly ConfiguracionGeneracion Config = EscenarioGeneracion.Config;

    // RN-02: el schema de Gemini se genera desde el vocabulario, con cada campo técnico como enum.
    [Theory]
    [InlineData(Vocabulario.Plano)]
    [InlineData(Vocabulario.Optica)]
    [InlineData(Vocabulario.Iluminacion)]
    [InlineData(Vocabulario.Angulo)]
    [InlineData(Vocabulario.Movimiento)]
    public void Schema_CadaCampoTecnicoEsUnEnumConLosValoresDelVocabulario(string campo)
    {
        var toma = Config.Schema.Properties!["bloques"].Items!.Properties!["tomas"].Items!;

        Assert.Equal(Config.Vocabulario[campo].Valores, toma.Properties![campo].Enum);
    }

    [Fact]
    public void Schema_AnguloYMovimientoSonOpcionales()
    {
        var toma = Config.Schema.Properties!["bloques"].Items!.Properties!["tomas"].Items!;

        Assert.Equal(["plano", "optica", "iluminacion", "accion", "dialogos"], toma.Required);
    }

    [Fact]
    public void Vocabulario_TieneLos29ValoresDelAnexoE()
    {
        var total = new[] { Vocabulario.Plano, Vocabulario.Optica, Vocabulario.Iluminacion, Vocabulario.Angulo, Vocabulario.Movimiento }
            .Sum(c => Config.Vocabulario[c].Valores.Count);

        Assert.Equal(29, total);
    }

    [Fact]
    public void Plantilla_OmiteElOpcionalSiSuVariableEstaVacia()
    {
        var plantilla = "{{a}}[, {{b}}][ ({{c}})].";

        Assert.Equal("A, B.", Plantilla.Rellenar(plantilla, new Dictionary<string, string?> { ["a"] = "A", ["b"] = "B", ["c"] = null }));
        Assert.Equal("A (C).", Plantilla.Rellenar(plantilla, new Dictionary<string, string?> { ["a"] = "A", ["b"] = "", ["c"] = "C" }));
    }

    [Fact]
    public void Plantilla_InsertaLosValoresLiterales()
    {
        var valor = "texto con [corchetes] y {{llaves}}";

        Assert.Equal($"<{valor}>", Plantilla.Rellenar("<{{a}}>", new Dictionary<string, string?> { ["a"] = valor }));
    }
}
