using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RodajeIA.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class Generacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MotivoError",
                table: "Escenas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Bloques",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClipId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bloques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bloques_Clips_ClipId",
                        column: x => x.ClipId,
                        principalTable: "Clips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Variantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EscenaId = table.Column<int>(type: "INTEGER", nullable: false),
                    PersonajeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Edad = table.Column<string>(type: "TEXT", nullable: true),
                    Peinado = table.Column<string>(type: "TEXT", nullable: true),
                    Vestuario = table.Column<string>(type: "TEXT", nullable: true),
                    Heridas = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Variantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Variantes_Escenas_EscenaId",
                        column: x => x.EscenaId,
                        principalTable: "Escenas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Variantes_Personajes_PersonajeId",
                        column: x => x.PersonajeId,
                        principalTable: "Personajes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tomas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BloqueId = table.Column<int>(type: "INTEGER", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    Plano = table.Column<string>(type: "TEXT", nullable: false),
                    Optica = table.Column<string>(type: "TEXT", nullable: false),
                    Iluminacion = table.Column<string>(type: "TEXT", nullable: false),
                    Angulo = table.Column<string>(type: "TEXT", nullable: true),
                    Movimiento = table.Column<string>(type: "TEXT", nullable: true),
                    Accion = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tomas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tomas_Bloques_BloqueId",
                        column: x => x.BloqueId,
                        principalTable: "Bloques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TomaDialogo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TomaId = table.Column<int>(type: "INTEGER", nullable: false),
                    LineaDialogoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TomaDialogo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TomaDialogo_LineasDialogo_LineaDialogoId",
                        column: x => x.LineaDialogoId,
                        principalTable: "LineasDialogo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TomaDialogo_Tomas_TomaId",
                        column: x => x.TomaId,
                        principalTable: "Tomas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bloques_ClipId",
                table: "Bloques",
                column: "ClipId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TomaDialogo_LineaDialogoId",
                table: "TomaDialogo",
                column: "LineaDialogoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TomaDialogo_TomaId",
                table: "TomaDialogo",
                column: "TomaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tomas_BloqueId_Orden",
                table: "Tomas",
                columns: new[] { "BloqueId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Variantes_EscenaId_PersonajeId",
                table: "Variantes",
                columns: new[] { "EscenaId", "PersonajeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Variantes_PersonajeId",
                table: "Variantes",
                column: "PersonajeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TomaDialogo");

            migrationBuilder.DropTable(
                name: "Variantes");

            migrationBuilder.DropTable(
                name: "Tomas");

            migrationBuilder.DropTable(
                name: "Bloques");

            migrationBuilder.DropColumn(
                name: "MotivoError",
                table: "Escenas");
        }
    }
}
