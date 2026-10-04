using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Generacion;

namespace RodajeIA.Web.Servicios;

/// <summary>Campos editables de una toma (RF-07b, RF-07c). Ángulo y movimiento vacíos significan "sin valor".</summary>
public class DatosToma
{
    public string? Plano { get; set; }

    public string? Optica { get; set; }

    public string? Iluminacion { get; set; }

    public string? Angulo { get; set; }

    public string? Movimiento { get; set; }

    public string? Accion { get; set; }

    public static DatosToma De(Toma toma) => new()
    {
        Plano = toma.Plano,
        Optica = toma.Optica,
        Iluminacion = toma.Iluminacion,
        Angulo = toma.Angulo,
        Movimiento = toma.Movimiento,
        Accion = toma.Accion,
    };
}

/// <summary>Revisión y edición de las tomas generadas (RF-07). El prompt final se vuelve a armar desde lo guardado (RF-07d).</summary>
public class TomasService(IDbContextFactory<RodajeDbContext> contextos, ConfiguracionGeneracion config)
{
    /// <summary>Clips de la escena en orden, con su bloque, sus tomas y las líneas de diálogo de cada toma (RF-07a).</summary>
    public async Task<List<Clip>> ListarBloquesAsync(int escenaId)
    {
        await using var db = await contextos.CreateDbContextAsync();
        var clips = await db.Clips.AsNoTracking()
            .Where(c => c.EscenaId == escenaId)
            .Include(c => c.Bloque).ThenInclude(b => b!.Tomas).ThenInclude(t => t.Dialogos).ThenInclude(d => d.LineaDialogo)
            .AsSplitQuery()
            .OrderBy(c => c.Numero)
            .ToListAsync();

        foreach (var bloque in clips.Select(c => c.Bloque).OfType<Bloque>())
        {
            bloque.Tomas = bloque.Tomas.OrderBy(t => t.Orden).ToList();
            foreach (var toma in bloque.Tomas)
            {
                toma.Dialogos = toma.Dialogos.OrderBy(d => d.Orden).ToList();
            }
        }

        return clips;
    }

    /// <summary>
    /// Guarda la edición de una toma (RF-07e). Los campos técnicos solo aceptan valores del vocabulario (RN-02) y la
    /// acción no puede quedar vacía (RN-01, RN-10); si algo no se cumple, la toma guardada no cambia.
    /// </summary>
    public async Task<Resultado<Toma>> EditarAsync(int tomaId, DatosToma datos)
    {
        var accion = datos.Accion?.Trim();
        var valores = new (string Categoria, string Nombre, string? Valor)[]
        {
            (Vocabulario.Plano, "el plano", Valor(datos.Plano)),
            (Vocabulario.Optica, "la óptica", Valor(datos.Optica)),
            (Vocabulario.Iluminacion, "la iluminación", Valor(datos.Iluminacion)),
            (Vocabulario.Angulo, "el ángulo", Valor(datos.Angulo)),
            (Vocabulario.Movimiento, "el movimiento", Valor(datos.Movimiento)),
        };

        var errores = new List<string>();
        foreach (var (categoria, nombre, valor) in valores)
        {
            var permitidos = config.Vocabulario[categoria];
            if (valor is null)
            {
                if (permitidos.Obligatorio)
                {
                    errores.Add($"Falta {nombre}.");
                }
            }
            else if (!permitidos.Admite(valor))
            {
                errores.Add($"\"{valor}\" no es un valor permitido para {nombre}.");
            }
        }

        if (string.IsNullOrEmpty(accion))
        {
            errores.Add("Falta la acción.");
        }

        if (errores.Count > 0)
        {
            return Resultado<Toma>.Fallo(errores);
        }

        await using var db = await contextos.CreateDbContextAsync();
        var toma = await db.Tomas.SingleOrDefaultAsync(t => t.Id == tomaId);
        if (toma is null)
        {
            return Resultado<Toma>.Fallo("La toma no existe.");
        }

        toma.Plano = valores[0].Valor!;
        toma.Optica = valores[1].Valor!;
        toma.Iluminacion = valores[2].Valor!;
        toma.Angulo = valores[3].Valor;
        toma.Movimiento = valores[4].Valor;
        toma.Accion = accion!;
        await db.SaveChangesAsync();
        return Resultado<Toma>.Exito(toma);
    }

    private static string? Valor(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor;
}
