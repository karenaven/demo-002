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

    /// <summary>Personajes con hoja detectados al cargar el guion (RF-05b).</summary>
    public List<Personaje> Personajes { get; set; } = [];

    public List<Clip> Clips { get; set; } = [];
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
