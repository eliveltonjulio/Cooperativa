using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cooperativa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracaoHoraNoturna",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hora_inicio = table.Column<TimeSpan>(type: "time without time zone", nullable: false),
                    hora_fim = table.Column<TimeSpan>(type: "time without time zone", nullable: false),
                    hora_reduzida = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracaoHoraNoturna", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Contratos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaNome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Cnpj = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: false),
                    DataInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataFim = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValorContrato = table.Column<decimal>(type: "numeric", nullable: false),
                    Logradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Bairro = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Cidade = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Cep = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Contato1Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Contato1Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Contato2Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Contato2Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contratos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DespesaFolha",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tipo_calculo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    teto_retencao = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DespesaFolha", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "feriado",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<DateTime>(type: "date", nullable: false),
                    descricao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "NACIONAL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feriado", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Funcoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Cbo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Funcoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TipoAdicional",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tipo_calculo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoAdicional", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Login = table.Column<string>(type: "text", nullable: false),
                    SenhaHash = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Celular = table.Column<string>(type: "text", nullable: false),
                    Perfil = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CooperadoId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContratoId = table.Column<Guid>(type: "uuid", nullable: true),
                    Cargo = table.Column<string>(type: "text", nullable: true),
                    Equipe = table.Column<string>(type: "text", nullable: true),
                    DataIngresso = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UltimoAcesso = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MfaCodigoTemp = table.Column<string>(type: "text", nullable: true),
                    MfaCodigoExpiracao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TokenRedefinicaoSenha = table.Column<string>(type: "text", nullable: true),
                    TokenRedefinicaoExpiracao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "regra_hora_extra",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contrato_id = table.Column<Guid>(type: "uuid", nullable: true),
                    percentual_he_comum = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 50.00m),
                    percentual_he_domingo_feriado = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 100.00m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regra_hora_extra", x => x.id);
                    table.ForeignKey(
                        name: "FK_regra_hora_extra_Contratos_contrato_id",
                        column: x => x.contrato_id,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Cooperados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DataNascimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CPF = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    NomePai = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NomeMae = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Naturalidade = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Escolaridade = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Nacionalidade = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Sexo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NumeroIdentidade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OrgaoEmissorIdentidade = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CertificadoReservista = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NumeroPis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TituloEleitor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ZonaEleitoral = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SecaoEleitoral = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    NumeroDependentes = table.Column<int>(type: "integer", nullable: true),
                    EstadoCivil = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NomeConjuge = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RegimeCasamento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Banco = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Agencia = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContaCorrente = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ChavePix = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Logradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Bairro = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Cidade = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    CEP = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DataAdmissao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeclaraImpostoRenda = table.Column<bool>(type: "boolean", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    Equipe = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FuncaoId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContratoId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cooperados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cooperados_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Cooperados_Funcoes_FuncaoId",
                        column: x => x.FuncaoId,
                        principalTable: "Funcoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "JornadaContratual",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contrato_id = table.Column<Guid>(type: "uuid", nullable: false),
                    funcao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hora_inicio = table.Column<TimeSpan>(type: "time without time zone", nullable: false),
                    hora_fim = table.Column<TimeSpan>(type: "time without time zone", nullable: false),
                    hora_inicio_intervalo = table.Column<TimeSpan>(type: "time without time zone", nullable: true),
                    hora_fim_intervalo = table.Column<TimeSpan>(type: "time without time zone", nullable: true),
                    dias_semana = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JornadaContratual", x => x.id);
                    table.ForeignKey(
                        name: "FK_JornadaContratual_Contratos_contrato_id",
                        column: x => x.contrato_id,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JornadaContratual_Funcoes_funcao_id",
                        column: x => x.funcao_id,
                        principalTable: "Funcoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Remuneracoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContratoId = table.Column<Guid>(type: "uuid", nullable: false),
                    FuncaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "date", nullable: false),
                    DataFim = table.Column<DateTime>(type: "date", nullable: true),
                    TipoRemuneracao = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Remuneracoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Remuneracoes_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Remuneracoes_Funcoes_FuncaoId",
                        column: x => x.FuncaoId,
                        principalTable: "Funcoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContratoFuncaoAdicional",
                columns: table => new
                {
                    contrato_id = table.Column<Guid>(type: "uuid", nullable: false),
                    funcao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    adicional_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContratoFuncaoAdicional", x => new { x.contrato_id, x.funcao_id, x.adicional_id });
                    table.ForeignKey(
                        name: "FK_ContratoFuncaoAdicional_Contratos_contrato_id",
                        column: x => x.contrato_id,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContratoFuncaoAdicional_Funcoes_funcao_id",
                        column: x => x.funcao_id,
                        principalTable: "Funcoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContratoFuncaoAdicional_TipoAdicional_adicional_id",
                        column: x => x.adicional_id,
                        principalTable: "TipoAdicional",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alocacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CooperadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContratoId = table.Column<Guid>(type: "uuid", nullable: false),
                    FuncaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataFim = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alocacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alocacoes_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Alocacoes_Cooperados_CooperadoId",
                        column: x => x.CooperadoId,
                        principalTable: "Cooperados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Alocacoes_Funcoes_FuncaoId",
                        column: x => x.FuncaoId,
                        principalTable: "Funcoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApuracaoPontoMensal",
                columns: table => new
                {
                    cooperado_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mes_ano = table.Column<DateTime>(type: "date", nullable: false),
                    qtd_horas_normais = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    qtd_horas_extras = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    qtd_horas_noturnas = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApuracaoPontoMensal", x => new { x.cooperado_id, x.mes_ano });
                    table.ForeignKey(
                        name: "FK_ApuracaoPontoMensal_Cooperados_cooperado_id",
                        column: x => x.cooperado_id,
                        principalTable: "Cooperados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FolhasPagamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CooperadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Competencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Salario = table.Column<decimal>(type: "numeric", nullable: false),
                    Adicional = table.Column<decimal>(type: "numeric", nullable: false),
                    Descontos = table.Column<decimal>(type: "numeric", nullable: false),
                    Detalhes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FolhasPagamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FolhasPagamento_Cooperados_CooperadoId",
                        column: x => x.CooperadoId,
                        principalTable: "Cooperados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosPonto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CooperadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Data = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Entrada = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Saida = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InicioIntervalo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FimIntervalo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ocorrencia = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Observacao = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosPonto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosPonto_Cooperados_CooperadoId",
                        column: x => x.CooperadoId,
                        principalTable: "Cooperados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosSistema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Celular = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Login = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Senha = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Perfil = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CooperadoId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContratoId = table.Column<Guid>(type: "uuid", nullable: true),
                    Cargo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Equipe = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DataIngresso = table.Column<DateTime>(type: "date", nullable: true),
                    UltimoAcesso = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MfaCodigoTemp = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    MfaCodigoExpiracao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TokenRedefinicaoSenha = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TokenRedefinicaoExpiracao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosSistema", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuariosSistema_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UsuariosSistema_Cooperados_CooperadoId",
                        column: x => x.CooperadoId,
                        principalTable: "Cooperados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alocacoes_ContratoId",
                table: "Alocacoes",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_Alocacoes_CooperadoId",
                table: "Alocacoes",
                column: "CooperadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Alocacoes_FuncaoId",
                table: "Alocacoes",
                column: "FuncaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ContratoFuncaoAdicional_adicional_id",
                table: "ContratoFuncaoAdicional",
                column: "adicional_id");

            migrationBuilder.CreateIndex(
                name: "IX_ContratoFuncaoAdicional_funcao_id",
                table: "ContratoFuncaoAdicional",
                column: "funcao_id");

            migrationBuilder.CreateIndex(
                name: "IX_Cooperados_ContratoId",
                table: "Cooperados",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_Cooperados_CPF",
                table: "Cooperados",
                column: "CPF",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cooperados_FuncaoId",
                table: "Cooperados",
                column: "FuncaoId");

            migrationBuilder.CreateIndex(
                name: "IX_feriado_data",
                table: "feriado",
                column: "data",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FolhasPagamento_CooperadoId",
                table: "FolhasPagamento",
                column: "CooperadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Funcoes_Nome",
                table: "Funcoes",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JornadaContratual_contrato_id_funcao_id",
                table: "JornadaContratual",
                columns: new[] { "contrato_id", "funcao_id" });

            migrationBuilder.CreateIndex(
                name: "IX_JornadaContratual_funcao_id",
                table: "JornadaContratual",
                column: "funcao_id");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosPonto_CooperadoId",
                table: "RegistrosPonto",
                column: "CooperadoId");

            migrationBuilder.CreateIndex(
                name: "IX_regra_hora_extra_contrato_id",
                table: "regra_hora_extra",
                column: "contrato_id");

            migrationBuilder.CreateIndex(
                name: "IX_Remuneracoes_ContratoId_FuncaoId",
                table: "Remuneracoes",
                columns: new[] { "ContratoId", "FuncaoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Remuneracoes_FuncaoId",
                table: "Remuneracoes",
                column: "FuncaoId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosSistema_ContratoId",
                table: "UsuariosSistema",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosSistema_CooperadoId",
                table: "UsuariosSistema",
                column: "CooperadoId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosSistema_Login",
                table: "UsuariosSistema",
                column: "Login",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alocacoes");

            migrationBuilder.DropTable(
                name: "ApuracaoPontoMensal");

            migrationBuilder.DropTable(
                name: "ConfiguracaoHoraNoturna");

            migrationBuilder.DropTable(
                name: "ContratoFuncaoAdicional");

            migrationBuilder.DropTable(
                name: "DespesaFolha");

            migrationBuilder.DropTable(
                name: "feriado");

            migrationBuilder.DropTable(
                name: "FolhasPagamento");

            migrationBuilder.DropTable(
                name: "JornadaContratual");

            migrationBuilder.DropTable(
                name: "RegistrosPonto");

            migrationBuilder.DropTable(
                name: "regra_hora_extra");

            migrationBuilder.DropTable(
                name: "Remuneracoes");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "UsuariosSistema");

            migrationBuilder.DropTable(
                name: "TipoAdicional");

            migrationBuilder.DropTable(
                name: "Cooperados");

            migrationBuilder.DropTable(
                name: "Contratos");

            migrationBuilder.DropTable(
                name: "Funcoes");
        }
    }
}
