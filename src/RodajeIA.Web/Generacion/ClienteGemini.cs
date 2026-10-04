using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;

namespace RodajeIA.Web.Generacion;

/// <summary>Configuración de Gemini (sección "Gemini"). La API key va en user-secrets, nunca en el repositorio.</summary>
public sealed class OpcionesGemini
{
    public string? ApiKey { get; set; }

    public string Modelo { get; set; } = "gemini-3.1-flash-lite";
}

/// <summary>Falta configurar Gemini: no tiene sentido reintentar hasta que se corrija.</summary>
public sealed class GeminiNoConfiguradoException(string mensaje) : Exception(mensaje);

/// <summary>Llamada a la IA con salida estructurada. Se abstrae para poder probar la generación sin llamar al modelo.</summary>
public interface IClienteGemini
{
    /// <returns>El texto JSON de la respuesta.</returns>
    Task<string?> GenerarAsync(string instruccion, Schema schema, CancellationToken cancelacion);
}

public sealed class ClienteGemini(IOptions<OpcionesGemini> opciones) : IClienteGemini
{
    private readonly Lazy<Client> cliente = new(() => CrearCliente(opciones.Value));

    public async Task<string?> GenerarAsync(string instruccion, Schema schema, CancellationToken cancelacion)
    {
        var respuesta = await cliente.Value.Models.GenerateContentAsync(
            opciones.Value.Modelo,
            instruccion,
            new GenerateContentConfig { ResponseMimeType = "application/json", ResponseSchema = schema },
            cancelacion);
        return respuesta.Text;
    }

    private static Client CrearCliente(OpcionesGemini opciones)
    {
        if (string.IsNullOrWhiteSpace(opciones.ApiKey))
        {
            throw new GeminiNoConfiguradoException(
                "Falta la API key de Gemini. Configurala con: dotnet user-secrets set \"Gemini:ApiKey\" \"<tu-api-key>\" --project src/RodajeIA.Web");
        }

        // Los reintentos y el tiempo máximo los maneja GeneradorEscenas (RNF-06, RNF-07); el SDK no reintenta por su cuenta.
        return new Client(apiKey: opciones.ApiKey, httpOptions: new HttpOptions { RetryOptions = new HttpRetryOptions { Attempts = 1 } });
    }
}
