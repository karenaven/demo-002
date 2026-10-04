using System.Text.Json;

namespace RodajeIA.Web.Generacion;

/// <summary>Una categoría del vocabulario técnico cerrado (Anexo E).</summary>
public sealed record CategoriaVocabulario(bool Obligatorio, IReadOnlyList<string> Valores)
{
    public bool Admite(string? valor) => valor is not null && Valores.Contains(valor, StringComparer.Ordinal);
}

/// <summary>
/// Vocabulario técnico cerrado (RN-02), leído de <c>config/vocabulario.json</c>.
/// Es la única fuente de valores para plano, óptica, iluminación, ángulo y movimiento.
/// </summary>
public sealed class Vocabulario
{
    public const string Plano = "plano";
    public const string Optica = "optica";
    public const string Iluminacion = "iluminacion";
    public const string Angulo = "angulo";
    public const string Movimiento = "movimiento";

    private static readonly string[] Categorias = [Plano, Optica, Iluminacion, Angulo, Movimiento];

    private readonly IReadOnlyDictionary<string, CategoriaVocabulario> categorias;

    private Vocabulario(string version, IReadOnlyDictionary<string, CategoriaVocabulario> categorias)
    {
        Version = version;
        this.categorias = categorias;
    }

    public string Version { get; }

    public CategoriaVocabulario this[string categoria] => categorias[categoria];

    public static Vocabulario Cargar(string ruta) => Parsear(File.ReadAllText(ruta));

    public static Vocabulario Parsear(string json)
    {
        var archivo = JsonSerializer.Deserialize<ArchivoVocabulario>(json, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("El vocabulario está vacío.");

        var categorias = new Dictionary<string, CategoriaVocabulario>(StringComparer.Ordinal);
        foreach (var nombre in Categorias)
        {
            if (archivo.Categorias?.GetValueOrDefault(nombre) is not { Valores.Count: > 0 } categoria)
            {
                throw new InvalidOperationException($"El vocabulario no tiene valores para la categoría \"{nombre}\".");
            }

            categorias[nombre] = new CategoriaVocabulario(categoria.Obligatorio, categoria.Valores);
        }

        return new Vocabulario(archivo.Version ?? "", categorias);
    }

    private sealed record ArchivoVocabulario(string? Version, Dictionary<string, ArchivoCategoria>? Categorias);

    private sealed record ArchivoCategoria(bool Obligatorio, List<string> Valores);
}
