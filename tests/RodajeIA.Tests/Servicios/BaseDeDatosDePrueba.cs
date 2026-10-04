using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;

namespace RodajeIA.Tests.Servicios;

/// <summary>Base SQLite en memoria con las migraciones aplicadas; vive mientras dure el test.</summary>
public sealed class BaseDeDatosDePrueba : IDbContextFactory<RodajeDbContext>, IDisposable
{
    private readonly SqliteConnection conexion = new("Data Source=:memory:");
    private readonly DbContextOptions<RodajeDbContext> opciones;

    public BaseDeDatosDePrueba()
    {
        conexion.Open();
        opciones = new DbContextOptionsBuilder<RodajeDbContext>().UseSqlite(conexion).Options;
        using var db = CreateDbContext();
        db.Database.Migrate();
    }

    public RodajeDbContext CreateDbContext() => new(opciones);

    public void Dispose() => conexion.Dispose();
}
