using RodajeIA.Web.Datos;

namespace RodajeIA.Web.Generacion;

/// <summary>
/// Arma de forma determinística, con el molde de <c>config/molde-prompt.json</c>, el prompt final de una escena (RF-08a)
/// y la instrucción que recibe Gemini (Anexo D). Las hojas, el diálogo y el texto en pantalla se insertan literales (RN-03, RN-04).
/// </summary>
public static class ArmadorPrompt
{
    /// <summary>
    /// Prompt final de una escena generada. La escena debe traer sus personajes, variantes, clips con diálogos
    /// y el bloque de cada clip con sus tomas y las líneas que referencian.
    /// </summary>
    public static string ArmarPromptFinal(ConfiguracionGeneracion config, Escena escena)
    {
        var molde = config.Molde;
        var secciones = new List<string>();
        foreach (var seccion in molde.Secciones)
        {
            var texto = seccion.Id switch
            {
                MoldePrompt.Estilo => Rellenar(seccion.Contenido!, ("estilo.texto", config.Estilo)),
                MoldePrompt.LocacionEIluminacionBase => string.Join(
                    "\n",
                    seccion.Lineas!.Select(linea => Rellenar(linea, ("escena.locacion", escena.Locacion), ("escena.iluminacion", escena.Iluminacion)))),
                MoldePrompt.Personajes => ArmarPersonajes(molde, escena),
                MoldePrompt.PuestaEnEscena => ConTitulo(seccion, Rellenar(seccion.Contenido!, ("escena.puestaEnEscena", escena.PuestaEnEscena))),
                MoldePrompt.Audio => ConTitulo(seccion, Rellenar(seccion.Contenido!, ("escena.audio", escena.Audio))),
                MoldePrompt.Bloques => string.Join(seccion.SeparadorBloques, escena.Clips.OrderBy(c => c.Numero).Select(c => ArmarBloque(seccion, c))),
                _ => throw new InvalidOperationException($"Sección desconocida en el molde: \"{seccion.Id}\"."),
            };

            // Sin personajes con hoja presentes, la sección se omite (RN-01).
            if (texto is not null)
            {
                secciones.Add(texto);
            }
        }

        return string.Join(molde.SeparadorSecciones, secciones);
    }

    /// <summary>
    /// Instrucción para Gemini: la escena completa, con un id por línea de diálogo, y las hojas efectivas de los
    /// personajes presentes (RN-05). La IA solo recibe el diálogo como contexto y lo referencia por id (RN-04).
    /// </summary>
    public static string ArmarInstruccion(ConfiguracionGeneracion config, Escena escena)
    {
        var lineaDialogo = config.Molde[MoldePrompt.Bloques].LineaDialogo!;
        var texto = new List<string>
        {
            $"ESCENA {escena.Numero} — {(escena.IntExt == Guiones.IntExt.Int ? "INT." : "EXT.")} {escena.Lugar} — {escena.MomentoDelDia}",
            $"Locación: {escena.Locacion}",
            $"Iluminación base: {escena.Iluminacion}",
            $"Puesta en escena: {escena.PuestaEnEscena}",
            $"Audio: {escena.Audio}",
        };

        foreach (var clip in escena.Clips.OrderBy(c => c.Numero))
        {
            texto.Add("");
            texto.Add($"CLIP {clip.Numero}");
            texto.Add($"Acción: {clip.Accion}");
            foreach (var linea in clip.Dialogos.OrderBy(d => d.Orden))
            {
                texto.Add($"Diálogo [id {IdLinea(clip, linea)}]: {ArmarLinea(lineaDialogo, linea)}");
            }

            if (clip.TextoEnPantalla is not null)
            {
                texto.Add($"Texto en pantalla: \"{clip.TextoEnPantalla}\"");
            }
        }

        var hojas = HojaEfectiva.DeEscena(escena);
        var personajes = hojas.Count == 0 ? "Ninguno." : string.Join("\n", hojas.Select(h => h.Describir(config.Molde)));

        // Reemplazo directo: la instrucción es texto libre y no usa la sintaxis de opcionales del molde.
        return config.Instruccion
            .Replace("{{escena}}", string.Join("\n", texto), StringComparison.Ordinal)
            .Replace("{{personajes}}", personajes, StringComparison.Ordinal);
    }

    /// <summary>Id con que la IA referencia una línea de diálogo: único dentro de la escena (por ejemplo "c1-l2").</summary>
    public static string IdLinea(Clip clip, LineaDialogo linea) => $"c{clip.Numero}-l{linea.Orden}";

    private static string? ArmarPersonajes(MoldePrompt molde, Escena escena)
    {
        var hojas = HojaEfectiva.DeEscena(escena);
        if (hojas.Count == 0)
        {
            return null;
        }

        return ConTitulo(molde[MoldePrompt.Personajes], string.Join("\n", hojas.Select(h => h.Describir(molde))));
    }

    private static string ArmarBloque(SeccionMolde seccion, Clip clip)
    {
        var bloque = clip.Bloque ?? throw new InvalidOperationException($"El clip {clip.Numero} no tiene bloque generado.");
        var tomas = bloque.Tomas.OrderBy(t => t.Orden).Select(toma => Rellenar(
            seccion.Toma!,
            ("toma.plano", toma.Plano),
            ("toma.movimiento", toma.Movimiento),
            ("toma.angulo", toma.Angulo),
            ("toma.optica", toma.Optica),
            ("toma.accion", toma.Accion),
            ("toma.dialogos", string.Join(
                seccion.SeparadorDialogos,
                toma.Dialogos.OrderBy(d => d.Orden).Select(d => ArmarLinea(seccion.LineaDialogo!, d.LineaDialogo!)))),
            ("toma.iluminacion", toma.Iluminacion)));

        var cierre = clip.TextoEnPantalla is null
            ? seccion.CierreSinTextoEnPantalla!
            : Rellenar(seccion.CierreConTextoEnPantalla!, ("clip.textoEnPantalla", clip.TextoEnPantalla));

        var titulo = Rellenar(seccion.TituloBloque!, ("bloque.numero", clip.Numero.ToString()));
        return $"{titulo}\n{string.Join(seccion.SeparadorTomas, tomas)} {cierre}";
    }

    private static string ArmarLinea(string plantilla, LineaDialogo linea) => Rellenar(
        plantilla,
        ("linea.personaje", linea.Personaje),
        ("linea.acotacion", linea.Acotacion),
        ("linea.texto", linea.Texto));

    private static string ConTitulo(SeccionMolde seccion, string contenido) =>
        seccion.Titulo is null ? contenido : $"{seccion.Titulo}\n{contenido}";

    private static string Rellenar(string plantilla, params (string Nombre, string? Valor)[] valores) =>
        Plantilla.Rellenar(plantilla, valores.ToDictionary(v => v.Nombre, v => v.Valor));
}
