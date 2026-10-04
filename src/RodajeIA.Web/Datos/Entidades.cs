using RodajeIA.Web.Guiones;

namespace RodajeIA.Web.Datos;

public class Serie
{
    public int Id { get; set; }

    public required string Nombre { get; set; }

    public List<Personaje> Personajes { get; set; } = [];

    public List<Episodio> Episodios { get; set; } = [];
}

/// <summary>Hoja base de un personaje (Anexo B). Todos los campos son obligatorios salvo <see cref="Heridas"/>.</summary>
public class Personaje
{
    public int Id { get; set; }

    public int SerieId { get; set; }

    public Serie? Serie { get; set; }

    public required string Nombre { get; set; }

    public required string Edad { get; set; }

    /// <summary>Descripción física, incluida la voz.</summary>
    public required string DescripcionFisica { get; set; }

    public required string Peinado { get; set; }

    public required string Vestuario { get; set; }

    public string? Heridas { get; set; }

    public required string Personalidad { get; set; }

    public required string Rol { get; set; }
}

public class Episodio
{
    public int Id { get; set; }

    public int SerieId { get; set; }

    public Serie? Serie { get; set; }

    /// <summary>Único dentro de la serie; no se puede cambiar después de crearlo.</summary>
    public int Numero { get; set; }

    public required string Titulo { get; set; }

    /// <summary>Guion en texto plano, tal como se cargó. Solo se guarda si es válido.</summary>
    public string? Guion { get; set; }

    public List<Escena> Escenas { get; set; } = [];
}

public enum EstadoGeneracion
{
    Pendiente,
    Generando,
    Generada,
    Error,
}

public class Escena
{
    public int Id { get; set; }

    public int EpisodioId { get; set; }

    public Episodio? Episodio { get; set; }

    public int Numero { get; set; }

    public IntExt IntExt { get; set; }

    public required string Lugar { get; set; }

    public required string MomentoDelDia { get; set; }

    public required string Locacion { get; set; }

    public required string Iluminacion { get; set; }

    public required string PuestaEnEscena { get; set; }

    public required string Audio { get; set; }

    public EstadoGeneracion Estado { get; set; } = EstadoGeneracion.Pendiente;

    /// <summary>Motivo del último intento fallido cuando la escena queda en "error" (RF-10b).</summary>
    public string? MotivoError { get; set; }

    /// <summary>Personajes con hoja detectados al cargar el guion (RF-05b).</summary>
    public List<Personaje> Personajes { get; set; } = [];

    public List<Clip> Clips { get; set; } = [];

    public List<VariantePersonaje> Variantes { get; set; } = [];
}

/// <summary>
/// Variante de un personaje para una escena (RF-09a): reemplaza solo los campos no nulos de su hoja base.
/// Como máximo una por personaje y escena.
/// </summary>
public class VariantePersonaje
{
    public int Id { get; set; }

    public int EscenaId { get; set; }

    public Escena? Escena { get; set; }

    public int PersonajeId { get; set; }

    public Personaje? Personaje { get; set; }

    public string? Edad { get; set; }

    public string? Peinado { get; set; }

    public string? Vestuario { get; set; }

    public string? Heridas { get; set; }
}

public class Clip
{
    public int Id { get; set; }

    public int EscenaId { get; set; }

    public Escena? Escena { get; set; }

    public int Numero { get; set; }

    /// <summary>Líneas de acción del clip, separadas por salto de línea.</summary>
    public required string Accion { get; set; }

    public string? TextoEnPantalla { get; set; }

    public List<LineaDialogo> Dialogos { get; set; } = [];

    /// <summary>Personajes con hoja detectados al cargar el guion (RF-05a).</summary>
    public List<Personaje> Personajes { get; set; } = [];

    /// <summary>Bloque generado por la IA para este clip; nulo mientras la escena no está generada.</summary>
    public Bloque? Bloque { get; set; }
}

public class LineaDialogo
{
    public int Id { get; set; }

    public int ClipId { get; set; }

    public Clip? Clip { get; set; }

    /// <summary>Posición de la línea dentro del clip, desde 1.</summary>
    public int Orden { get; set; }

    /// <summary>Marcador del diálogo tal como está en el guion, en mayúsculas (por ejemplo "JOSÉ DANIEL").</summary>
    public required string Personaje { get; set; }

    public string? Acotacion { get; set; }

    public required string Texto { get; set; }
}

/// <summary>Lista ordenada de tomas generadas por la IA para un clip.</summary>
public class Bloque
{
    public int Id { get; set; }

    public int ClipId { get; set; }

    public Clip? Clip { get; set; }

    public List<Toma> Tomas { get; set; } = [];
}

/// <summary>Toma de un bloque. Los campos técnicos son valores del vocabulario (RN-02); la acción es el único texto libre de la IA.</summary>
public class Toma
{
    public int Id { get; set; }

    public int BloqueId { get; set; }

    public Bloque? Bloque { get; set; }

    /// <summary>Posición de la toma dentro del bloque, desde 1.</summary>
    public int Orden { get; set; }

    public required string Plano { get; set; }

    public required string Optica { get; set; }

    public required string Iluminacion { get; set; }

    public string? Angulo { get; set; }

    public string? Movimiento { get; set; }

    public required string Accion { get; set; }

    /// <summary>Líneas de diálogo que se dicen en esta toma (RN-04), en el orden que indicó la IA.</summary>
    public List<TomaDialogo> Dialogos { get; set; } = [];
}

public class TomaDialogo
{
    public int Id { get; set; }

    public int TomaId { get; set; }

    public Toma? Toma { get; set; }

    public int LineaDialogoId { get; set; }

    public LineaDialogo? LineaDialogo { get; set; }

    /// <summary>Posición de la línea dentro de la toma, desde 1.</summary>
    public int Orden { get; set; }
}
