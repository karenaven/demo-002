using System.Text.Json;
using Google.GenAI.Types;
using TipoSchema = Google.GenAI.Types.Type;

namespace RodajeIA.Web.Generacion;

/// <summary>Respuesta estructurada que se le pide a Gemini por escena (Anexo D).</summary>
public sealed record RespuestaGeneracion(List<BloqueGenerado>? Bloques)
{
    /// <summary>Deserializa el JSON devuelto por Gemini; nulo si no es un JSON con la forma esperada.</summary>
    public static RespuestaGeneracion? Parsear(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<RespuestaGeneracion>(json, JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

public sealed record BloqueGenerado(int Clip, List<TomaGenerada>? Tomas);

public sealed record TomaGenerada(
    string? Plano,
    string? Optica,
    string? Iluminacion,
    string? Angulo,
    string? Movimiento,
    string? Accion,
    List<string>? Dialogos);

/// <summary>
/// Genera el schema de salida estructurada de Gemini desde el vocabulario (RN-02): cada campo técnico es un enum
/// con los valores del vocabulario, y ángulo y movimiento son opcionales según el archivo.
/// </summary>
public static class SchemaRespuesta
{
    public static Schema Crear(Vocabulario vocabulario)
    {
        var camposTecnicos = new[] { Vocabulario.Plano, Vocabulario.Optica, Vocabulario.Iluminacion, Vocabulario.Angulo, Vocabulario.Movimiento };

        var propiedadesToma = camposTecnicos.ToDictionary(
            campo => campo,
            campo => new Schema { Type = TipoSchema.String, Enum = [.. vocabulario[campo].Valores] });
        propiedadesToma["accion"] = new Schema { Type = TipoSchema.String };
        propiedadesToma["dialogos"] = new Schema
        {
            Type = TipoSchema.Array,
            Items = new Schema { Type = TipoSchema.String },
        };

        var toma = new Schema
        {
            Type = TipoSchema.Object,
            Properties = propiedadesToma,
            PropertyOrdering = [.. camposTecnicos, "accion", "dialogos"],
            Required = [.. camposTecnicos.Where(campo => vocabulario[campo].Obligatorio), "accion", "dialogos"],
        };

        var bloque = new Schema
        {
            Type = TipoSchema.Object,
            Properties = new Dictionary<string, Schema>
            {
                ["clip"] = new() { Type = TipoSchema.Integer },
                ["tomas"] = new() { Type = TipoSchema.Array, Items = toma },
            },
            PropertyOrdering = ["clip", "tomas"],
            Required = ["clip", "tomas"],
        };

        return new Schema
        {
            Type = TipoSchema.Object,
            Properties = new Dictionary<string, Schema>
            {
                ["bloques"] = new() { Type = TipoSchema.Array, Items = bloque },
            },
            Required = ["bloques"],
        };
    }
}
