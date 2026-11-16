using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeConta.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class Privacidade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Transcricao",
                table: "Paginas",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MemoriasDoBairro",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaginaId = table.Column<int>(type: "int", nullable: false),
                    IdosoId = table.Column<int>(type: "int", nullable: false),
                    FamiliaId = table.Column<int>(type: "int", nullable: false),
                    CategoriaId = table.Column<int>(type: "int", nullable: false),
                    Bairro = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Apelido = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Transcricao = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IndiceRiscoCidadao = table.Column<int>(type: "int", nullable: false),
                    AutorizadaPeloIdoso = table.Column<bool>(type: "bit", nullable: false),
                    PublicadaPorMembroId = table.Column<int>(type: "int", nullable: false),
                    PublicadaEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RemovidaEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovidaPorMembroId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoriasDoBairro", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemoriasDoBairro_Membros_PublicadaPorMembroId",
                        column: x => x.PublicadaPorMembroId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemoriasDoBairro_Membros_RemovidaPorMembroId",
                        column: x => x.RemovidaPorMembroId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemoriasDoBairro_Paginas_PaginaId",
                        column: x => x.PaginaId,
                        principalTable: "Paginas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosDePrivacidade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaginaId = table.Column<int>(type: "int", nullable: false),
                    FamiliaId = table.Column<int>(type: "int", nullable: false),
                    IdosoId = table.Column<int>(type: "int", nullable: false),
                    MembroId = table.Column<int>(type: "int", nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Bairro = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    IndiceRiscoCidadao = table.Column<int>(type: "int", nullable: false),
                    AnaliseCompleta = table.Column<bool>(type: "bit", nullable: false),
                    VersaoDasRegras = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosDePrivacidade", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AchadosDePrivacidade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegistroId = table.Column<int>(type: "int", nullable: false),
                    RegraId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Rotulo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Nivel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Campo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TrechoMascarado = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchadosDePrivacidade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchadosDePrivacidade_RegistrosDePrivacidade_RegistroId",
                        column: x => x.RegistroId,
                        principalTable: "RegistrosDePrivacidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchadosDePrivacidade_RegistroId",
                table: "AchadosDePrivacidade",
                column: "RegistroId");

            migrationBuilder.CreateIndex(
                name: "IX_MemoriasDoBairro_Bairro_RemovidaEm",
                table: "MemoriasDoBairro",
                columns: new[] { "Bairro", "RemovidaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_MemoriasDoBairro_PaginaId",
                table: "MemoriasDoBairro",
                column: "PaginaId",
                unique: true,
                filter: "[RemovidaEm] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MemoriasDoBairro_PublicadaPorMembroId",
                table: "MemoriasDoBairro",
                column: "PublicadaPorMembroId");

            migrationBuilder.CreateIndex(
                name: "IX_MemoriasDoBairro_RemovidaPorMembroId",
                table: "MemoriasDoBairro",
                column: "RemovidaPorMembroId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosDePrivacidade_CriadoEm",
                table: "RegistrosDePrivacidade",
                column: "CriadoEm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchadosDePrivacidade");

            migrationBuilder.DropTable(
                name: "MemoriasDoBairro");

            migrationBuilder.DropTable(
                name: "RegistrosDePrivacidade");

            migrationBuilder.DropColumn(
                name: "Transcricao",
                table: "Paginas");
        }
    }
}
