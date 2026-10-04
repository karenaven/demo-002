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

    public DbSet<VariantePersonaje> Variantes => Set<VariantePersonaje>();

    public DbSet<Bloque> Bloques => Set<Bloque>();

    public DbSet<Toma> Tomas => Set<Toma>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Todo se borra en cascada desde Serie: hojas, episodios, escenas, clips, diálogos, bloques y variantes (RF-01d, RF-11d, RF-02b).
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
            escena.HasMany(e => e.Variantes).WithOne(v => v.Escena).HasForeignKey(v => v.EscenaId).OnDelete(DeleteBehavior.Cascade);
        });

        // Borrar una hoja borra también sus variantes, en cualquier estado de la escena (RF-01h).
        modelBuilder.Entity<VariantePersonaje>(variante =>
        {
            variante.HasIndex(v => new { v.EscenaId, v.PersonajeId }).IsUnique();
            variante.HasOne(v => v.Personaje).WithMany().HasForeignKey(v => v.PersonajeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Clip>(clip =>
        {
            clip.HasIndex(c => new { c.EscenaId, c.Numero }).IsUnique();
            clip.HasMany(c => c.Dialogos).WithOne(d => d.Clip).HasForeignKey(d => d.ClipId).OnDelete(DeleteBehavior.Cascade);
            clip.HasMany(c => c.Personajes).WithMany().UsingEntity("ClipPersonaje");
            clip.HasOne(c => c.Bloque).WithOne(b => b.Clip).HasForeignKey<Bloque>(b => b.ClipId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LineaDialogo>().HasIndex(d => new { d.ClipId, d.Orden }).IsUnique();

        modelBuilder.Entity<Bloque>()
            .HasMany(b => b.Tomas).WithOne(t => t.Bloque).HasForeignKey(t => t.BloqueId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Toma>(toma =>
        {
            toma.HasIndex(t => new { t.BloqueId, t.Orden }).IsUnique();
            toma.HasMany(t => t.Dialogos).WithOne(d => d.Toma).HasForeignKey(d => d.TomaId).OnDelete(DeleteBehavior.Cascade);
        });

        // Cada línea de diálogo se referencia en una sola toma (RN-09).
        modelBuilder.Entity<TomaDialogo>(referencia =>
        {
            referencia.HasIndex(d => d.LineaDialogoId).IsUnique();
            referencia.HasOne(d => d.LineaDialogo).WithMany().HasForeignKey(d => d.LineaDialogoId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
