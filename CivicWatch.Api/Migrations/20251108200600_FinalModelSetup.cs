using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicWatch.Api.Migrations
{
    /// <inheritdoc />
    public partial class FinalModelSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckIntegridade_Fornecedor_FornecedorId",
                table: "CheckIntegridade");

            migrationBuilder.DropForeignKey(
                name: "FK_Contrato_Fornecedor_FornecedorId",
                table: "Contrato");

            migrationBuilder.DropForeignKey(
                name: "FK_Contrato_OrgaoPublico_OrgaoPublicoId",
                table: "Contrato");

            migrationBuilder.DropForeignKey(
                name: "FK_Despesa_Fornecedor_FornecedorId",
                table: "Despesa");

            migrationBuilder.DropForeignKey(
                name: "FK_Despesa_OrgaoPublico_OrgaoPublicoId",
                table: "Despesa");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemContrato_Contrato_ContratoId",
                table: "ItemContrato");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemDespesa_Despesa_DespesaId",
                table: "ItemDespesa");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrgaoPublico",
                table: "OrgaoPublico");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemDespesa",
                table: "ItemDespesa");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemContrato",
                table: "ItemContrato");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Fornecedor",
                table: "Fornecedor");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Despesa",
                table: "Despesa");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Contrato",
                table: "Contrato");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CheckIntegridade",
                table: "CheckIntegridade");

            migrationBuilder.RenameTable(
                name: "OrgaoPublico",
                newName: "OrgaosPublicos");

            migrationBuilder.RenameTable(
                name: "ItemDespesa",
                newName: "ItensDespesa");

            migrationBuilder.RenameTable(
                name: "ItemContrato",
                newName: "ItensContrato");

            migrationBuilder.RenameTable(
                name: "Fornecedor",
                newName: "Fornecedores");

            migrationBuilder.RenameTable(
                name: "Despesa",
                newName: "Despesas");

            migrationBuilder.RenameTable(
                name: "Contrato",
                newName: "Contratos");

            migrationBuilder.RenameTable(
                name: "CheckIntegridade",
                newName: "ChecksIntegridade");

            migrationBuilder.RenameIndex(
                name: "IX_ItemDespesa_DespesaId",
                table: "ItensDespesa",
                newName: "IX_ItensDespesa_DespesaId");

            migrationBuilder.RenameIndex(
                name: "IX_ItemContrato_ContratoId",
                table: "ItensContrato",
                newName: "IX_ItensContrato_ContratoId");

            migrationBuilder.RenameIndex(
                name: "IX_Despesa_OrgaoPublicoId",
                table: "Despesas",
                newName: "IX_Despesas_OrgaoPublicoId");

            migrationBuilder.RenameIndex(
                name: "IX_Despesa_FornecedorId",
                table: "Despesas",
                newName: "IX_Despesas_FornecedorId");

            migrationBuilder.RenameIndex(
                name: "IX_Contrato_OrgaoPublicoId",
                table: "Contratos",
                newName: "IX_Contratos_OrgaoPublicoId");

            migrationBuilder.RenameIndex(
                name: "IX_Contrato_FornecedorId",
                table: "Contratos",
                newName: "IX_Contratos_FornecedorId");

            migrationBuilder.RenameIndex(
                name: "IX_CheckIntegridade_FornecedorId",
                table: "ChecksIntegridade",
                newName: "IX_ChecksIntegridade_FornecedorId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrgaosPublicos",
                table: "OrgaosPublicos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItensDespesa",
                table: "ItensDespesa",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItensContrato",
                table: "ItensContrato",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Fornecedores",
                table: "Fornecedores",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Despesas",
                table: "Despesas",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Contratos",
                table: "Contratos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChecksIntegridade",
                table: "ChecksIntegridade",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "FontesDadosPublica",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UrlBase = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UltimaSincronizacao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FontesDadosPublica", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegrasAlerta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DescricaoLogica = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
                    TipoEntidadeAfetada = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegrasAlerta", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatusAlertas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CorHex = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusAlertas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Alertas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataGeracao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DescricaoOcorrencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegraAlertaId = table.Column<int>(type: "int", nullable: false),
                    StatusAlertaId = table.Column<int>(type: "int", nullable: false),
                    EntidadeRelacionadaId = table.Column<int>(type: "int", nullable: true),
                    TipoEntidadeRelacionada = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alertas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alertas_RegrasAlerta_RegraAlertaId",
                        column: x => x.RegraAlertaId,
                        principalTable: "RegrasAlerta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Alertas_StatusAlertas_StatusAlertaId",
                        column: x => x.StatusAlertaId,
                        principalTable: "StatusAlertas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Denuncias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataDenuncia = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Conteudo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AlertaId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Denuncias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Denuncias_Alertas_AlertaId",
                        column: x => x.AlertaId,
                        principalTable: "Alertas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Denuncias_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RespostasAlerta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataResposta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Justificativa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AlertaId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespostasAlerta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RespostasAlerta_Alertas_AlertaId",
                        column: x => x.AlertaId,
                        principalTable: "Alertas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RespostasAlerta_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_RegraAlertaId",
                table: "Alertas",
                column: "RegraAlertaId");

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_StatusAlertaId",
                table: "Alertas",
                column: "StatusAlertaId");

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_AlertaId",
                table: "Denuncias",
                column: "AlertaId");

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_UserId",
                table: "Denuncias",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RespostasAlerta_AlertaId",
                table: "RespostasAlerta",
                column: "AlertaId");

            migrationBuilder.CreateIndex(
                name: "IX_RespostasAlerta_UserId",
                table: "RespostasAlerta",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChecksIntegridade_Fornecedores_FornecedorId",
                table: "ChecksIntegridade",
                column: "FornecedorId",
                principalTable: "Fornecedores",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Contratos_Fornecedores_FornecedorId",
                table: "Contratos",
                column: "FornecedorId",
                principalTable: "Fornecedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Contratos_OrgaosPublicos_OrgaoPublicoId",
                table: "Contratos",
                column: "OrgaoPublicoId",
                principalTable: "OrgaosPublicos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Despesas_Fornecedores_FornecedorId",
                table: "Despesas",
                column: "FornecedorId",
                principalTable: "Fornecedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Despesas_OrgaosPublicos_OrgaoPublicoId",
                table: "Despesas",
                column: "OrgaoPublicoId",
                principalTable: "OrgaosPublicos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItensContrato_Contratos_ContratoId",
                table: "ItensContrato",
                column: "ContratoId",
                principalTable: "Contratos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItensDespesa_Despesas_DespesaId",
                table: "ItensDespesa",
                column: "DespesaId",
                principalTable: "Despesas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChecksIntegridade_Fornecedores_FornecedorId",
                table: "ChecksIntegridade");

            migrationBuilder.DropForeignKey(
                name: "FK_Contratos_Fornecedores_FornecedorId",
                table: "Contratos");

            migrationBuilder.DropForeignKey(
                name: "FK_Contratos_OrgaosPublicos_OrgaoPublicoId",
                table: "Contratos");

            migrationBuilder.DropForeignKey(
                name: "FK_Despesas_Fornecedores_FornecedorId",
                table: "Despesas");

            migrationBuilder.DropForeignKey(
                name: "FK_Despesas_OrgaosPublicos_OrgaoPublicoId",
                table: "Despesas");

            migrationBuilder.DropForeignKey(
                name: "FK_ItensContrato_Contratos_ContratoId",
                table: "ItensContrato");

            migrationBuilder.DropForeignKey(
                name: "FK_ItensDespesa_Despesas_DespesaId",
                table: "ItensDespesa");

            migrationBuilder.DropTable(
                name: "Denuncias");

            migrationBuilder.DropTable(
                name: "FontesDadosPublica");

            migrationBuilder.DropTable(
                name: "RespostasAlerta");

            migrationBuilder.DropTable(
                name: "Alertas");

            migrationBuilder.DropTable(
                name: "RegrasAlerta");

            migrationBuilder.DropTable(
                name: "StatusAlertas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrgaosPublicos",
                table: "OrgaosPublicos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItensDespesa",
                table: "ItensDespesa");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItensContrato",
                table: "ItensContrato");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Fornecedores",
                table: "Fornecedores");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Despesas",
                table: "Despesas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Contratos",
                table: "Contratos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChecksIntegridade",
                table: "ChecksIntegridade");

            migrationBuilder.RenameTable(
                name: "OrgaosPublicos",
                newName: "OrgaoPublico");

            migrationBuilder.RenameTable(
                name: "ItensDespesa",
                newName: "ItemDespesa");

            migrationBuilder.RenameTable(
                name: "ItensContrato",
                newName: "ItemContrato");

            migrationBuilder.RenameTable(
                name: "Fornecedores",
                newName: "Fornecedor");

            migrationBuilder.RenameTable(
                name: "Despesas",
                newName: "Despesa");

            migrationBuilder.RenameTable(
                name: "Contratos",
                newName: "Contrato");

            migrationBuilder.RenameTable(
                name: "ChecksIntegridade",
                newName: "CheckIntegridade");

            migrationBuilder.RenameIndex(
                name: "IX_ItensDespesa_DespesaId",
                table: "ItemDespesa",
                newName: "IX_ItemDespesa_DespesaId");

            migrationBuilder.RenameIndex(
                name: "IX_ItensContrato_ContratoId",
                table: "ItemContrato",
                newName: "IX_ItemContrato_ContratoId");

            migrationBuilder.RenameIndex(
                name: "IX_Despesas_OrgaoPublicoId",
                table: "Despesa",
                newName: "IX_Despesa_OrgaoPublicoId");

            migrationBuilder.RenameIndex(
                name: "IX_Despesas_FornecedorId",
                table: "Despesa",
                newName: "IX_Despesa_FornecedorId");

            migrationBuilder.RenameIndex(
                name: "IX_Contratos_OrgaoPublicoId",
                table: "Contrato",
                newName: "IX_Contrato_OrgaoPublicoId");

            migrationBuilder.RenameIndex(
                name: "IX_Contratos_FornecedorId",
                table: "Contrato",
                newName: "IX_Contrato_FornecedorId");

            migrationBuilder.RenameIndex(
                name: "IX_ChecksIntegridade_FornecedorId",
                table: "CheckIntegridade",
                newName: "IX_CheckIntegridade_FornecedorId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrgaoPublico",
                table: "OrgaoPublico",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemDespesa",
                table: "ItemDespesa",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemContrato",
                table: "ItemContrato",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Fornecedor",
                table: "Fornecedor",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Despesa",
                table: "Despesa",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Contrato",
                table: "Contrato",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CheckIntegridade",
                table: "CheckIntegridade",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckIntegridade_Fornecedor_FornecedorId",
                table: "CheckIntegridade",
                column: "FornecedorId",
                principalTable: "Fornecedor",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Contrato_Fornecedor_FornecedorId",
                table: "Contrato",
                column: "FornecedorId",
                principalTable: "Fornecedor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Contrato_OrgaoPublico_OrgaoPublicoId",
                table: "Contrato",
                column: "OrgaoPublicoId",
                principalTable: "OrgaoPublico",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Despesa_Fornecedor_FornecedorId",
                table: "Despesa",
                column: "FornecedorId",
                principalTable: "Fornecedor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Despesa_OrgaoPublico_OrgaoPublicoId",
                table: "Despesa",
                column: "OrgaoPublicoId",
                principalTable: "OrgaoPublico",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemContrato_Contrato_ContratoId",
                table: "ItemContrato",
                column: "ContratoId",
                principalTable: "Contrato",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemDespesa_Despesa_DespesaId",
                table: "ItemDespesa",
                column: "DespesaId",
                principalTable: "Despesa",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
