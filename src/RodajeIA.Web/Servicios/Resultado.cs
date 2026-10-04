namespace RodajeIA.Web.Servicios;

/// <summary>Resultado de una operación que puede fallar por una validación que se le muestra al usuario.</summary>
public sealed record Resultado<T>(T? Valor, IReadOnlyList<string> Errores)
{
    public bool Ok => Errores.Count == 0;

    public static Resultado<T> Exito(T valor) => new(valor, []);

    public static Resultado<T> Fallo(params IReadOnlyList<string> errores) => new(default, errores);
}
