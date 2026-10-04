using System.Text.Json;
using Schema = Google.GenAI.Types.Schema;

namespace RodajeIA.Web.Generacion;

/// <summary>Una sección del molde del prompt final (Anexo D). Cada sección usa solo los campos que le corresponden.</summary>
public sealed record SeccionMolde(
    string Id,
    string? Titulo,
    string? Contenido,
    List<string>? Lineas,
    string? PorPersonaje,
    string? SeparadorBloques,
    string? TituloBloque,
    string? Toma,
    string? SeparadorTomas,
    string? LineaDialogo,
    string? SeparadorDialogos,
    string? CierreConTextoEnPantalla,
    string? CierreSinTextoEnPantalla);

/// <summary>Molde del prompt final, leído de <c>config/molde-prompt.json</c>.</summary>
public sealed record MoldePrompt(string SeparadorSecciones, List<SeccionMolde> Secciones)
{
    public const string Estilo = "estilo";
    public const string LocacionEIluminacionBase = "locacionEIluminacionBase";
    public const string Personajes = "personajes";
    public const string PuestaEnEscena = "puestaEnEscena";
    public const string Audio = "audio";
    public const string Bloques = "bloques";

    public SeccionMolde this[string id] =>
        Secciones.SingleOrDefault(s => s.Id == id) ?? throw new InvalidOperationException($"El molde no tiene la sección \"{id}\".");

    public static MoldePrompt Parsear(string json)
    {
        var molde = JsonSerializer.Deserialize<MoldePrompt>(json, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("El molde del prompt está vacío.");

        foreach (var id in new[] { Estilo, LocacionEIluminacionBase, Personajes, PuestaEnEscena, Audio, Bloques })
        {
            _ = molde[id];
        }

        return molde;
    }
}

/// <summary>
/// Archivos versionados de <c>config/</c> que usa la generación: vocabulario, molde del prompt, estilo global (RN-06)
/// e instrucción para Gemini. Se leen una vez al iniciar la aplicación.
/// </summary>
public sealed class ConfiguracionGeneracion
{
    public ConfiguracionGeneracion(Vocabulario vocabulario, MoldePrompt molde, string estilo, string instruccion)
    {
        Vocabulario = vocabulario;
        Molde = molde;
        Estilo = estilo;
        Instruccion = instruccion;
        Schema = SchemaRespuesta.Crear(vocabulario);
    }

    public Vocabulario Vocabulario { get; }

    public MoldePrompt Molde { get; }

    /// <summary>Texto fijo del estilo cinematográfico, global para todas las series (RN-06).</summary>
    public string Estilo { get; }

    /// <summary>Instrucción para Gemini, con los lugares <c>{{escena}}</c> y <c>{{personajes}}</c>.</summary>
    public string Instruccion { get; }

    /// <summary>Schema de salida estructurada generado desde el vocabulario (RN-02).</summary>
    public Schema Schema { get; }

    /// <summary>Carga los archivos de la carpeta <c>config</c> que se copia junto a la aplicación.</summary>
    public static ConfiguracionGeneracion Cargar() => Cargar(Path.Combine(AppContext.BaseDirectory, "config"));

    public static ConfiguracionGeneracion Cargar(string carpeta) => new(
        Vocabulario.Cargar(Path.Combine(carpeta, "vocabulario.json")),
        MoldePrompt.Parsear(File.ReadAllText(Path.Combine(carpeta, "molde-prompt.json"))),
        File.ReadAllText(Path.Combine(carpeta, "estilo.txt")).Trim(),
        File.ReadAllText(Path.Combine(carpeta, "instruccion-generacion.txt")).Trim());
}
