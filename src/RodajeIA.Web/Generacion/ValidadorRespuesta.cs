using RodajeIA.Web.Datos;

namespace RodajeIA.Web.Generacion;

/// <summary>
/// Valida la respuesta de Gemini para una escena (RF-06b) y la convierte en bloques. Una respuesta que no cumple
/// RN-02, RN-08, RN-09 o RN-10 se trata como fallida y se reintenta.
/// </summary>
public static class ValidadorRespuesta
{
    /// <summary>Motivos por los que la respuesta es inválida; vacía si es válida.</summary>
    public static List<string> Validar(RespuestaGeneracion? respuesta, Escena escena, Vocabulario vocabulario)
    {
        if (respuesta?.Bloques is null)
        {
            return ["La respuesta no es un JSON con la estructura pedida."];
        }

        var errores = new List<string>();
        var clips = escena.Clips.OrderBy(c => c.Numero).ToList();

        // RN-08: un bloque por clip y cada número de clip exactamente una vez.
        if (respuesta.Bloques.Count != clips.Count)
        {
            errores.Add($"Se esperaban {clips.Count} bloques (uno por clip) y llegaron {respuesta.Bloques.Count}.");
        }

        foreach (var clip in clips)
        {
            var veces = respuesta.Bloques.Count(b => b.Clip == clip.Numero);
            if (veces != 1)
            {
                errores.Add($"El clip {clip.Numero} tiene {veces} bloques; debe tener exactamente uno.");
            }
        }

        foreach (var bloque in respuesta.Bloques.Where(b => clips.All(c => c.Numero != b.Clip)))
        {
            errores.Add($"Llegó un bloque para el clip {bloque.Clip}, que no existe en la escena.");
        }

        foreach (var clip in clips)
        {
            foreach (var bloque in respuesta.Bloques.Where(b => b.Clip == clip.Numero))
            {
                errores.AddRange(ValidarBloque(bloque, clip, vocabulario));
            }
        }

        return errores;
    }

    /// <summary>Convierte una respuesta ya validada en el bloque de cada clip.</summary>
    public static Dictionary<Clip, Bloque> ABloques(RespuestaGeneracion respuesta, Escena escena)
    {
        return escena.Clips.ToDictionary(clip => clip, clip =>
        {
            var lineas = clip.Dialogos.ToDictionary(d => ArmadorPrompt.IdLinea(clip, d));
            var generado = respuesta.Bloques!.Single(b => b.Clip == clip.Numero);
            return new Bloque
            {
                Tomas = generado.Tomas!.Select((toma, i) => new Toma
                {
                    Orden = i + 1,
                    Plano = toma.Plano!,
                    Optica = toma.Optica!,
                    Iluminacion = toma.Iluminacion!,
                    Angulo = Opcional(toma.Angulo),
                    Movimiento = Opcional(toma.Movimiento),
                    Accion = toma.Accion!.Trim(),
                    Dialogos = (toma.Dialogos ?? []).Select((id, orden) => new TomaDialogo
                    {
                        Orden = orden + 1,
                        LineaDialogo = lineas[id],
                    }).ToList(),
                }).ToList(),
            };
        });
    }

    private static IEnumerable<string> ValidarBloque(BloqueGenerado bloque, Clip clip, Vocabulario vocabulario)
    {
        // RN-10: al menos una toma, y cada una con acción.
        if (bloque.Tomas is not { Count: > 0 })
        {
            yield return $"El bloque del clip {clip.Numero} no tiene ninguna toma.";
            yield break;
        }

        for (var i = 0; i < bloque.Tomas.Count; i++)
        {
            var toma = bloque.Tomas[i];
            var donde = $"La toma {i + 1} del clip {clip.Numero}";
            if (string.IsNullOrWhiteSpace(toma.Accion))
            {
                yield return $"{donde} no tiene acción.";
            }

            // RN-02: el schema ya lo exige con enums; se vuelve a verificar por si el modelo no lo respeta.
            foreach (var (categoria, valor) in new[]
            {
                (Vocabulario.Plano, toma.Plano),
                (Vocabulario.Optica, toma.Optica),
                (Vocabulario.Iluminacion, toma.Iluminacion),
                (Vocabulario.Angulo, Opcional(toma.Angulo)),
                (Vocabulario.Movimiento, Opcional(toma.Movimiento)),
            })
            {
                var permitidos = vocabulario[categoria];
                if (valor is null ? permitidos.Obligatorio : !permitidos.Admite(valor))
                {
                    yield return $"{donde} tiene un valor de {categoria} que no está en el vocabulario: \"{valor}\".";
                }
            }
        }

        // RN-09: cada línea del clip referenciada exactamente una vez, y ninguna referencia ajena al clip.
        var referencias = bloque.Tomas.SelectMany(t => t.Dialogos ?? []).ToList();
        var ids = clip.Dialogos.OrderBy(d => d.Orden).Select(d => ArmadorPrompt.IdLinea(clip, d)).ToList();
        foreach (var id in ids)
        {
            var veces = referencias.Count(r => r == id);
            if (veces != 1)
            {
                yield return $"La línea de diálogo {id} está referenciada {veces} veces en el clip {clip.Numero}; debe estarlo exactamente una.";
            }
        }

        foreach (var ajena in referencias.Where(r => !ids.Contains(r)).Distinct())
        {
            yield return $"El clip {clip.Numero} referencia la línea \"{ajena}\", que no pertenece a ese clip.";
        }
    }

    private static string? Opcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor;
}
