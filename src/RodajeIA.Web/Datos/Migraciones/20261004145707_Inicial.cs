using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RodajeIA.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Series",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Series", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Episodios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SerieId = table.Column<int>(type: "INTEGER", nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", nullable: false),
                    Guion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Episodios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Episodios_Series_SerieId",
                        column: x => x.SerieId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Personajes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SerieId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Edad = table.Column<string>(type: "TEXT", nullable: false),
                    DescripcionFisica = table.Column<string>(type: "TEXT", nullable: false),
                    Peinado = table.Column<string>(type: "TEXT", nullable: false),
                    Vestuario = table.Column<string>(type: "TEXT", nullable: false),
                    Heridas = table.Column<string>(type: "TEXT", nullable: true),
                    Personalidad = table.Column<string>(type: "TEXT", nullable: false),
                    Rol = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personajes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Personajes_Series_SerieId",
                        column: x => x.SerieId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Escenas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EpisodioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    IntExt = table.Column<string>(type: "TEXT", nullable: false),
                    Lugar = table.Column<string>(type: "TEXT", nullable: false),
                    MomentoDelDia = table.Column<string>(type: "TEXT", nullable: false),
                    Locacion = table.Column<string>(type: "TEXT", nullable: false),
                    Iluminacion = table.Column<string>(type: "TEXT", nullable: false),
                    PuestaEnEscena = table.Column<string>(type: "TEXT", nullable: false),
                    Audio = table.Column<string>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Escenas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Escenas_Episodios_EpisodioId",
                        column: x => x.EpisodioId,
                        principalTable: "Episodios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Clips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EscenaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    Accion = table.Column<string>(type: "TEXT", nullable: false),
                    TextoEnPantalla = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clips_Escenas_EscenaId",
                        column: x => x.EscenaId,
                        principalTable: "Escenas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EscenaPersonaje",
                columns: table => new
                {
                    EscenaId = table.Column<int>(type: "INTEGER", nullable: false),
                    PersonajesId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscenaPersonaje", x => new { x.EscenaId, x.PersonajesId });
                    table.ForeignKey(
                        name: "FK_EscenaPersonaje_Escenas_EscenaId",
                        column: x => x.EscenaId,
                        principalTable: "Escenas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EscenaPersonaje_Personajes_PersonajesId",
                        column: x => x.PersonajesId,
                        principalTable: "Personajes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClipPersonaje",
                columns: table => new
                {
                    ClipId = table.Column<int>(type: "INTEGER", nullable: false),
                    PersonajesId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClipPersonaje", x => new { x.ClipId, x.PersonajesId });
                    table.ForeignKey(
                        name: "FK_ClipPersonaje_Clips_ClipId",
                        column: x => x.ClipId,
                        principalTable: "Clips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClipPersonaje_Personajes_PersonajesId",
                        column: x => x.PersonajesId,
                        principalTable: "Personajes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LineasDialogo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClipId = table.Column<int>(type: "INTEGER", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    Personaje = table.Column<string>(type: "TEXT", nullable: false),
                    Acotacion = table.Column<string>(type: "TEXT", nullable: true),
                    Texto = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineasDialogo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineasDialogo_Clips_ClipId",
                        column: x => x.ClipId,
                        principalTable: "Clips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClipPersonaje_PersonajesId",
                table: "ClipPersonaje",
                column: "PersonajesId");

            migrationBuilder.CreateIndex(
                name: "IX_Clips_EscenaId_Numero",
                table: "Clips",
                columns: new[] { "EscenaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Episodios_SerieId_Numero",
                table: "Episodios",
                columns: new[] { "SerieId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EscenaPersonaje_PersonajesId",
                table: "EscenaPersonaje",
                column: "PersonajesId");

            migrationBuilder.CreateIndex(
                name: "IX_Escenas_EpisodioId_Numero",
                table: "Escenas",
                columns: new[] { "EpisodioId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LineasDialogo_ClipId_Orden",
                table: "LineasDialogo",
                columns: new[] { "ClipId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Personajes_SerieId",
                table: "Personajes",
                column: "SerieId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClipPersonaje");

            migrationBuilder.DropTable(
                name: "EscenaPersonaje");

            migrationBuilder.DropTable(
                name: "LineasDialogo");

            migrationBuilder.DropTable(
                name: "Personajes");

            migrationBuilder.DropTable(
                name: "Clips");

            migrationBuilder.DropTable(
                name: "Escenas");

            migrationBuilder.DropTable(
                name: "Episodios");

            migrationBuilder.DropTable(
                name: "Series");
        }
    }
}
