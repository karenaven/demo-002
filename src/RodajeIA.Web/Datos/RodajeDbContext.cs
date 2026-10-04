using Microsoft.EntityFrameworkCore;

namespace RodajeIA.Web.Datos;

public class RodajeDbContext(DbContextOptions<RodajeDbContext> options) : DbContext(options)
{
    public DbSet<Serie> Series => Set<Serie>();

    public DbSet<Personaje> Personajes => Set<Personaje>();

    public DbSet<Episodio> Episodios => Set<Episodio>();

    public DbSet<Escena> Escenas => Set<Escena>();

    public DbSet<Clip> Clips => Set<Clip>();

    public DbSet<LineaDialogo> LineasDialogo => Set<LineaDialogo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Todo se borra en cascada desde Serie: hojas, episodios, escenas, clips y diálogos (RF-01d, RF-11d, RF-02b).
        modelBuilder.Entity<Serie>(serie =>
        {
            serie.HasMany(s => s.Personajes).WithOne(p => p.Serie).HasForeignKey(p => p.SerieId).OnDelete(DeleteBehavior.Cascade);
            serie.HasMany(s => s.Episodios).WithOne(e => e.Serie).HasForeignKey(e => e.SerieId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Episodio>(episodio =>
        {
            episodio.HasIndex(e => new { e.SerieId, e.Numero }).IsUnique();
            episodio.HasMany(e => e.Escenas).WithOne(e => e.Episodio).HasForeignKey(e => e.EpisodioId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Escena>(escena =>
        {
            escena.HasIndex(e => new { e.EpisodioId, e.Numero }).IsUnique();
            escena.Property(e => e.IntExt).HasConversion<string>();
            escena.Property(e => e.Estado).HasConversion<string>();
            escena.HasMany(e => e.Clips).WithOne(c => c.Escena).HasForeignKey(c => c.EscenaId).OnDelete(DeleteBehavior.Cascade);
            escena.HasMany(e => e.Personajes).WithMany().UsingEntity("EscenaPersonaje");
        });

        modelBuilder.Entity<Clip>(clip =>
        {
            clip.HasIndex(c => new { c.EscenaId, c.Numero }).IsUnique();
            clip.HasMany(c => c.Dialogos).WithOne(d => d.Clip).HasForeignKey(d => d.ClipId).OnDelete(DeleteBehavior.Cascade);
            clip.HasMany(c => c.Personajes).WithMany().UsingEntity("ClipPersonaje");
        });

        modelBuilder.Entity<LineaDialogo>().HasIndex(d => new { d.ClipId, d.Orden }).IsUnique();
    }
}
