using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MeConta.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Familias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CriadaEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Familias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Membros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Membros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Membros_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Idosos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FamiliaId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apelido = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Telefone = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: true),
                    RespondeSozinho = table.Column<bool>(type: "bit", nullable: false),
                    CadastradoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Idosos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Idosos_Familias_FamiliaId",
                        column: x => x.FamiliaId,
                        principalTable: "Familias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Convites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FamiliaId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nchar(6)", fixedLength: true, maxLength: 6, nullable: false),
                    CriadoPorMembroId = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Convites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Convites_Familias_FamiliaId",
                        column: x => x.FamiliaId,
                        principalTable: "Familias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Convites_Membros_CriadoPorMembroId",
                        column: x => x.CriadoPorMembroId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Participacoes",
                columns: table => new
                {
                    FamiliaId = table.Column<int>(type: "int", nullable: false),
                    MembroId = table.Column<int>(type: "int", nullable: false),
                    Papel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EntrouEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participacoes", x => new { x.FamiliaId, x.MembroId });
                    table.ForeignKey(
                        name: "FK_Participacoes_Familias_FamiliaId",
                        column: x => x.FamiliaId,
                        principalTable: "Familias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Participacoes_Membros_MembroId",
                        column: x => x.MembroId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Perguntas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Texto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CategoriaId = table.Column<int>(type: "int", nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FamiliaId = table.Column<int>(type: "int", nullable: true),
                    CriadaPorMembroId = table.Column<int>(type: "int", nullable: true),
                    CriadaEm = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perguntas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perguntas_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Perguntas_Familias_FamiliaId",
                        column: x => x.FamiliaId,
                        principalTable: "Familias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Perguntas_Membros_CriadaPorMembroId",
                        column: x => x.CriadaPorMembroId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Envios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdosoId = table.Column<int>(type: "int", nullable: false),
                    PerguntaId = table.Column<int>(type: "int", nullable: false),
                    CategoriaId = table.Column<int>(type: "int", nullable: false),
                    TextoPergunta = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Mensagem = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TelefoneDestino = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    EnviadoPorMembroId = table.Column<int>(type: "int", nullable: false),
                    EnviadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RespondidoEm = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Envios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Envios_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Envios_Idosos_IdosoId",
                        column: x => x.IdosoId,
                        principalTable: "Idosos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Envios_Membros_EnviadoPorMembroId",
                        column: x => x.EnviadoPorMembroId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Envios_Perguntas_PerguntaId",
                        column: x => x.PerguntaId,
                        principalTable: "Perguntas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Paginas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnvioId = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AudioCaminho = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    DuracaoSegundos = table.Column<int>(type: "int", nullable: false),
                    FotoCaminho = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    CriadaPorMembroId = table.Column<int>(type: "int", nullable: false),
                    ChaveIdempotencia = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriadaEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paginas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Paginas_Envios_EnvioId",
                        column: x => x.EnvioId,
                        principalTable: "Envios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Paginas_Membros_CriadaPorMembroId",
                        column: x => x.CriadaPorMembroId,
                        principalTable: "Membros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Categorias",
                columns: new[] { "Id", "Nome", "Slug" },
                values: new object[,]
                {
                    { 1, "Receitas", "receitas" },
                    { 2, "História da família", "historia-da-familia" },
                    { 3, "Ofícios", "oficios" },
                    { 4, "O bairro", "o-bairro" },
                    { 5, "Conselhos", "conselhos" }
                });

            migrationBuilder.InsertData(
                table: "Perguntas",
                columns: new[] { "Id", "CategoriaId", "Origem", "Texto" },
                values: new object[,]
                {
                    { 1, 1, "Catalogo", "Qual é o segredo do seu prato mais famoso?" },
                    { 2, 1, "Catalogo", "Que receita você aprendeu com a sua mãe ou a sua avó?" },
                    { 3, 1, "Catalogo", "O que não podia faltar na mesa nas festas de fim de ano?" },
                    { 4, 1, "Catalogo", "Qual doce você fazia quando alguém da família fazia aniversário?" },
                    { 5, 1, "Catalogo", "Qual comida te lembra a sua infância?" },
                    { 6, 2, "Catalogo", "Como você conheceu o amor da sua vida?" },
                    { 7, 2, "Catalogo", "Como eram os seus pais quando você era criança?" },
                    { 8, 2, "Catalogo", "De onde veio a nossa família antes de chegar aqui?" },
                    { 9, 2, "Catalogo", "Qual é a história mais engraçada que aconteceu na nossa família?" },
                    { 10, 2, "Catalogo", "Como foi o dia em que você saiu da casa dos seus pais?" },
                    { 11, 3, "Catalogo", "Qual foi o seu primeiro trabalho e o que você fazia lá?" },
                    { 12, 3, "Catalogo", "Que ofício você sabe fazer que quase ninguém mais faz?" },
                    { 13, 3, "Catalogo", "Quem te ensinou a profissão que você teve?" },
                    { 14, 3, "Catalogo", "Qual foi o dia mais marcante no seu trabalho?" },
                    { 15, 3, "Catalogo", "Que ferramenta ou máquina você usava todo dia no trabalho?" },
                    { 16, 4, "Catalogo", "Como era o bairro quando você chegou?" },
                    { 17, 4, "Catalogo", "Qual loja ou padaria do bairro já não existe mais?" },
                    { 18, 4, "Catalogo", "Como eram as festas juninas na sua rua?" },
                    { 19, 4, "Catalogo", "Onde as crianças brincavam no bairro quando você era jovem?" },
                    { 20, 4, "Catalogo", "Qual era o nome antigo de alguma rua daqui?" },
                    { 21, 5, "Catalogo", "Qual foi o melhor conselho que você já recebeu?" },
                    { 22, 5, "Catalogo", "O que você gostaria de ter sabido aos 20 anos?" },
                    { 23, 5, "Catalogo", "Que conselho você daria para quem está começando a trabalhar?" },
                    { 24, 5, "Catalogo", "Como você fazia para economizar quando as coisas estavam difíceis?" },
                    { 25, 5, "Catalogo", "O que é mais importante para manter uma família unida?" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Slug",
                table: "Categorias",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Convites_Codigo",
                table: "Convites",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Convites_CriadoPorMembroId",
                table: "Convites",
                column: "CriadoPorMembroId");

            migrationBuilder.CreateIndex(
                name: "IX_Convites_FamiliaId",
                table: "Convites",
                column: "FamiliaId");

            migrationBuilder.CreateIndex(
                name: "IX_Envios_CategoriaId",
                table: "Envios",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Envios_EnviadoPorMembroId",
                table: "Envios",
                column: "EnviadoPorMembroId");

            migrationBuilder.CreateIndex(
                name: "IX_Envios_IdosoId_PerguntaId",
                table: "Envios",
                columns: new[] { "IdosoId", "PerguntaId" });

            migrationBuilder.CreateIndex(
                name: "IX_Envios_PerguntaId",
                table: "Envios",
                column: "PerguntaId");

            migrationBuilder.CreateIndex(
                name: "IX_Idosos_FamiliaId_Telefone",
                table: "Idosos",
                columns: new[] { "FamiliaId", "Telefone" },
                unique: true,
                filter: "[Telefone] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Membros_UsuarioId",
                table: "Membros",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Paginas_ChaveIdempotencia",
                table: "Paginas",
                column: "ChaveIdempotencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Paginas_CriadaEm",
                table: "Paginas",
                column: "CriadaEm");

            migrationBuilder.CreateIndex(
                name: "IX_Paginas_CriadaPorMembroId",
                table: "Paginas",
                column: "CriadaPorMembroId");

            migrationBuilder.CreateIndex(
                name: "IX_Paginas_EnvioId",
                table: "Paginas",
                column: "EnvioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Participacoes_MembroId",
                table: "Participacoes",
                column: "MembroId");

            migrationBuilder.CreateIndex(
                name: "IX_Perguntas_CategoriaId",
                table: "Perguntas",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Perguntas_CriadaPorMembroId",
                table: "Perguntas",
                column: "CriadaPorMembroId");

            migrationBuilder.CreateIndex(
                name: "IX_Perguntas_FamiliaId",
                table: "Perguntas",
                column: "FamiliaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Convites");

            migrationBuilder.DropTable(
                name: "Paginas");

            migrationBuilder.DropTable(
                name: "Participacoes");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Envios");

            migrationBuilder.DropTable(
                name: "Idosos");

            migrationBuilder.DropTable(
                name: "Perguntas");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "Familias");

            migrationBuilder.DropTable(
                name: "Membros");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
