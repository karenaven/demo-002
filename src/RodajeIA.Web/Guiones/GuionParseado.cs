namespace RodajeIA.Web.Guiones;

public enum IntExt
{
    Int,
    Ext,
}

public sealed record DialogoParseado(string Personaje, string? Acotacion, string Texto);

public sealed record ClipParseado(
    int Numero,
    string Accion,
    IReadOnlyList<DialogoParseado> Dialogos,
    string? TextoEnPantalla);

public sealed record EscenaParseada(
    int Numero,
    IntExt IntExt,
    string Lugar,
    string MomentoDelDia,
    string Locacion,
    string Iluminacion,
    string PuestaEnEscena,
    string Audio,
    IReadOnlyList<ClipParseado> Clips);

public sealed record ErrorGuion(int Linea, string Motivo);

public sealed class ResultadoParseo
{
    private ResultadoParseo(IReadOnlyList<EscenaParseada> escenas, IReadOnlyList<ErrorGuion> errores)
    {
        Escenas = escenas;
        Errores = errores;
    }

    /// <summary>Escenas del guion. Vacía si el guion es inválido: nunca se devuelve un guion parcial.</summary>
    public IReadOnlyList<EscenaParseada> Escenas { get; }

    public IReadOnlyList<ErrorGuion> Errores { get; }

    public bool EsValido => Errores.Count == 0;

    public static ResultadoParseo Valido(IReadOnlyList<EscenaParseada> escenas) => new(escenas, []);

    public static ResultadoParseo Invalido(IReadOnlyList<ErrorGuion> errores) => new([], errores);
}
