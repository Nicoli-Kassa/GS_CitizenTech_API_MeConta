using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeConta.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class PortaRetrato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvisosQueroContar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdosoId = table.Column<int>(type: "int", nullable: false),
                    EnvioId = table.Column<int>(type: "int", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VistoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VistoPorMembroId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvisosQueroContar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvisosQueroContar_Envios_EnvioId",
                        column: x => x.EnvioId,
                        principalTable: "Envios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AvisosQueroContar_Idosos_IdosoId",
                        column: x => x.IdosoId,
                        principalTable: "Idosos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AvisosQueroContar_Membros_VistoPorMembroId",
                        column: x => x.VistoPorMembroId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PortaRetratos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdosoId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nchar(8)", fixedLength: true, maxLength: 8, nullable: false),
                    ChaveHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    CriadoPorMembroId = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Online = table.Column<bool>(type: "bit", nullable: false),
                    UltimoContatoEm = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortaRetratos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortaRetratos_Idosos_IdosoId",
                        column: x => x.IdosoId,
                        principalTable: "Idosos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PortaRetratos_Membros_CriadoPorMembroId",
                        column: x => x.CriadoPorMembroId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvisosQueroContar_EnvioId",
                table: "AvisosQueroContar",
                column: "EnvioId",
                unique: true,
                filter: "[EnvioId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AvisosQueroContar_IdosoId_CriadoEm",
                table: "AvisosQueroContar",
                columns: new[] { "IdosoId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_AvisosQueroContar_VistoPorMembroId",
                table: "AvisosQueroContar",
                column: "VistoPorMembroId");

            migrationBuilder.CreateIndex(
                name: "IX_PortaRetratos_ChaveHash",
                table: "PortaRetratos",
                column: "ChaveHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PortaRetratos_Codigo",
                table: "PortaRetratos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PortaRetratos_CriadoPorMembroId",
                table: "PortaRetratos",
                column: "CriadoPorMembroId");

            migrationBuilder.CreateIndex(
                name: "IX_PortaRetratos_IdosoId",
                table: "PortaRetratos",
                column: "IdosoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvisosQueroContar");

            migrationBuilder.DropTable(
                name: "PortaRetratos");
        }
    }
}
