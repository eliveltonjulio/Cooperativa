using Cooperativa.Models;
using Xunit;

namespace Cooperativa.Tests;

public class CalculadoraFolhaPagamentoTests
{
    private static readonly DateTime InicioCompetencia = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FimCompetenciaExclusivo = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Calcular_RemuneracaoPorHoraCompoeExtrasNoturnasAdicionaisEDescontos()
    {
        var (contratoId, funcaoId, cooperadoId) = CriarIds();
        var segunda = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);

        var resultado = CalculadoraFolhaPagamento.Calcular(
            InicioCompetencia,
            FimCompetenciaExclusivo,
            new[] { CriarRegistro(cooperadoId, segunda, 8, 18), CriarRegistro(cooperadoId, segunda, 22, 23) },
            new[] { CriarAlocacao(cooperadoId, contratoId, funcaoId, InicioCompetencia) },
            new[] { CriarJornada(contratoId, funcaoId, 8, 17) },
            new[] { CriarRemuneracao(contratoId, funcaoId, "HORA", 100m) },
            new[]
            {
                CriarAdicionalVinculado(contratoId, funcaoId, "PERCENTUAL_HORA", 20m),
                CriarAdicionalVinculado(contratoId, funcaoId, "VALOR_FIXO", 50m)
            },
            new[] { CriarRegraHoraExtra(contratoId, 50m, 100m) },
            new[]
            {
                CriarDespesa("PERCENTUAL_REMUNERACAO", 10m, teto: null),
                CriarDespesa("VALOR_FIXO", 30m, teto: null)
            },
            new ConfiguracaoHoraNoturna { HoraInicio = new TimeSpan(22, 0, 0), HoraFim = new TimeSpan(5, 0, 0), HoraReduzida = true });

        Assert.NotNull(resultado);
        // 8h-18h: 9h normais + 1h extra; 22h-23h: 1h noturna (1,14h com fator reduzido)
        Assert.Equal(9m, resultado!.HorasNormais);
        Assert.Equal(1m, resultado.HorasExtras);
        Assert.Equal(1.14m, resultado.HorasNoturnas);
        // Dois registros no mesmo dia => 1 dia trabalhado
        Assert.Equal(1, resultado.DiasTrabalhados);
        // Base: 9h x 100 = 900
        Assert.Equal(900m, resultado.ValorRemuneracaoBase);
        // HE: 1h comum x 100 x 1,5 = 150
        Assert.Equal(150m, resultado.ValorHorasExtras);
        // Noturno: 1,14h x 100 x 1,2 = 136,80
        Assert.Equal(136.80m, resultado.ValorAdicionalNoturno);
        // Adicionais: 20% x 100 x 9h = 180 + fixo 50 = 230
        Assert.Equal(230m, resultado.ValorAdicionais);
        // Bruto = 900 + 150 + 136,80 + 230 = 1.416,80
        Assert.Equal(1416.80m, resultado.TotalBruto);
        // Descontos: 10% do bruto (141,68) + fixo 30 = 171,68
        Assert.Equal(171.68m, resultado.TotalDescontos);
        Assert.Equal(1245.12m, resultado.Liquido);

        // Detalhamento por empresa/função
        var detalhe = resultado.Detalhe;
        Assert.Single(detalhe.Grupos);
        var grupo = detalhe.Grupos[0];
        Assert.Equal(9m, grupo.HorasNormais);
        Assert.Equal(1m, grupo.HorasExtrasComum);
        Assert.Equal(0m, grupo.HorasExtrasEspeciais);
        Assert.Equal(1.14m, grupo.HorasNoturnas);
        Assert.Equal(100m, grupo.ValorHora);
        Assert.Equal(50m, grupo.PercentualHeComum);
        Assert.Equal(900m, grupo.ValorHorasNormais);
        Assert.Equal(150m, grupo.ValorHorasExtrasComum);
        Assert.Equal(0m, grupo.ValorHorasExtrasEspeciais);
        Assert.Equal(136.80m, grupo.ValorAdicionalNoturno);
        Assert.Equal(2, grupo.Adicionais.Count);
        Assert.Equal(180m, grupo.Adicionais[0].Valor);
        Assert.Equal(50m, grupo.Adicionais[1].Valor);
        Assert.Equal(1416.80m, grupo.TotalGrupo);
        // Descontos detalhados
        Assert.Equal(2, detalhe.Descontos.Count);
        Assert.Equal(141.68m, detalhe.Descontos[0].Valor);
        Assert.Equal(30m, detalhe.Descontos[1].Valor);
        Assert.Equal(1416.80m, detalhe.TotalBruto);
        Assert.Equal(171.68m, detalhe.TotalDescontos);
        Assert.Equal(1245.12m, detalhe.Liquido);
    }

    [Fact]
    public void Calcular_RemuneracaoPorDiaUsaDiasComRegistro()
    {
        var (contratoId, funcaoId, cooperadoId) = CriarIds();
        var segunda = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);
        var terca = new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc);

        var resultado = CalculadoraFolhaPagamento.Calcular(
            InicioCompetencia,
            FimCompetenciaExclusivo,
            new[] { CriarRegistro(cooperadoId, segunda, 8, 12), CriarRegistro(cooperadoId, terca, 8, 12) },
            new[] { CriarAlocacao(cooperadoId, contratoId, funcaoId, InicioCompetencia) },
            new[] { CriarJornada(contratoId, funcaoId, 8, 17) },
            new[] { CriarRemuneracao(contratoId, funcaoId, "DIA", 80m) },
            new List<ContratoFuncaoAdicional>(),
            new List<RegraHoraExtra>(),
            new[] { CriarDespesa("VALOR_FIXO", 20m, teto: null) },
            null);

        Assert.NotNull(resultado);
        // Base: 2 dias com registro x 80 = 160
        Assert.Equal(160m, resultado!.ValorRemuneracaoBase);
        Assert.Equal(2, resultado.DiasTrabalhados);
        Assert.Equal(20m, resultado.TotalDescontos);
        Assert.Equal(140m, resultado.Liquido);
    }

    [Fact]
    public void Calcular_RemuneracaoMensalAplicaValorIntegralEUsaValorHoraNasExtras()
    {
        var (contratoId, funcaoId, cooperadoId) = CriarIds();
        var segunda = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);

        var resultado = CalculadoraFolhaPagamento.Calcular(
            InicioCompetencia,
            FimCompetenciaExclusivo,
            new[] { CriarRegistro(cooperadoId, segunda, 8, 18) },
            new[] { CriarAlocacao(cooperadoId, contratoId, funcaoId, InicioCompetencia) },
            new[] { CriarJornada(contratoId, funcaoId, 8, 17) },
            new[] { CriarRemuneracao(contratoId, funcaoId, "MES", 2200m) },
            new List<ContratoFuncaoAdicional>(),
            new List<RegraHoraExtra>(),
            new List<DespesaFolha>(),
            null);

        Assert.NotNull(resultado);
        // Base integral da competência
        Assert.Equal(2200m, resultado!.ValorRemuneracaoBase);
        // Valor-hora = 2200 / 220 = 10 => HE 1h x 10 x 1,5 = 15
        Assert.Equal(15m, resultado.ValorHorasExtras);
        Assert.Equal(2215m, resultado.TotalBruto);
        Assert.Equal(2215m, resultado.Liquido);
    }

    [Fact]
    public void Calcular_HoraExtraEmFeriadoUsaPercentualEspecial()
    {
        var (contratoId, funcaoId, cooperadoId) = CriarIds();
        // 07/09/2026 é segunda-feira e feriado nacional (Independência do Brasil)
        var feriado = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);

        var resultado = CalculadoraFolhaPagamento.Calcular(
            InicioCompetencia,
            FimCompetenciaExclusivo,
            new[] { CriarRegistro(cooperadoId, feriado, 8, 12) },
            new[] { CriarAlocacao(cooperadoId, contratoId, funcaoId, InicioCompetencia) },
            new[] { CriarJornada(contratoId, funcaoId, 8, 17) },
            new[] { CriarRemuneracao(contratoId, funcaoId, "HORA", 100m) },
            new List<ContratoFuncaoAdicional>(),
            new[] { CriarRegraHoraExtra(contratoId, 50m, 100m) },
            new List<DespesaFolha>(),
            null,
            new[] { new Feriado { Data = feriado, Descricao = "Independência", Tipo = "NACIONAL" } });

        Assert.NotNull(resultado);
        // 4h trabalhadas em feriado: todas como HE especial x 100% => 4 x 100 x 2 = 800
        Assert.Equal(0m, resultado!.ValorRemuneracaoBase);
        Assert.Equal(4m, resultado.HorasExtras);
        Assert.Equal(800m, resultado.ValorHorasExtras);
        Assert.Equal(800m, resultado.TotalBruto);
    }

    [Fact]
    public void Calcular_TetoRetencaoLimitaODesconto()
    {
        var (contratoId, funcaoId, cooperadoId) = CriarIds();
        var segunda = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);

        var resultado = CalculadoraFolhaPagamento.Calcular(
            InicioCompetencia,
            FimCompetenciaExclusivo,
            new[] { CriarRegistro(cooperadoId, segunda, 8, 12) },
            new[] { CriarAlocacao(cooperadoId, contratoId, funcaoId, InicioCompetencia) },
            new[] { CriarJornada(contratoId, funcaoId, 8, 17) },
            new[] { CriarRemuneracao(contratoId, funcaoId, "MES", 1000m) },
            new List<ContratoFuncaoAdicional>(),
            new List<RegraHoraExtra>(),
            new[] { CriarDespesa("PERCENTUAL_REMUNERACAO", 50m, teto: 100m) },
            null);

        Assert.NotNull(resultado);
        // 50% de 1000 = 500, porém limitado ao teto de 100
        Assert.Equal(100m, resultado!.TotalDescontos);
        Assert.Equal(900m, resultado.Liquido);
    }

    [Fact]
    public void Calcular_SemAlocacaoComContratoRetornaNulo()
    {
        var (_, _, cooperadoId) = CriarIds();

        var resultado = CalculadoraFolhaPagamento.Calcular(
            InicioCompetencia,
            FimCompetenciaExclusivo,
            new List<RegistroPonto>(),
            new List<Alocacao>(),
            new List<JornadaContratual>(),
            new List<Remuneracao>(),
            new List<ContratoFuncaoAdicional>(),
            new List<RegraHoraExtra>(),
            new List<DespesaFolha>(),
            null);

        Assert.Null(resultado);
    }

    [Fact]
    public void Detalhe_SerializaEDesserializaCorretamente()
    {
        var (contratoId, funcaoId, cooperadoId) = CriarIds();
        var segunda = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);

        var resultado = CalculadoraFolhaPagamento.Calcular(
            InicioCompetencia,
            FimCompetenciaExclusivo,
            new[] { CriarRegistro(cooperadoId, segunda, 8, 12) },
            new[] { CriarAlocacao(cooperadoId, contratoId, funcaoId, InicioCompetencia) },
            new[] { CriarJornada(contratoId, funcaoId, 8, 17) },
            new[] { CriarRemuneracao(contratoId, funcaoId, "MES", 1000m) },
            new[] { CriarAdicionalVinculado(contratoId, funcaoId, "VALOR_FIXO", 75m) },
            new List<RegraHoraExtra>(),
            new[] { CriarDespesa("PERCENTUAL_REMUNERACAO", 50m, teto: 100m) },
            null);

        Assert.NotNull(resultado);

        // Simula o ciclo persistido: FolhaPagamento.Detalhes (JSON) -> tela de detalhes
        var json = System.Text.Json.JsonSerializer.Serialize(resultado!.Detalhe);
        var desserializado = System.Text.Json.JsonSerializer.Deserialize<DetalheFolha>(json);

        Assert.NotNull(desserializado);
        Assert.Single(desserializado!.Grupos);
        Assert.Equal(resultado.Detalhe.Grupos[0].ValorHorasNormais, desserializado.Grupos[0].ValorHorasNormais);
        Assert.Equal(resultado.Detalhe.Grupos[0].Adicionais[0].Valor, desserializado.Grupos[0].Adicionais[0].Valor);
        Assert.Equal(resultado.Detalhe.Descontos[0].Valor, desserializado.Descontos[0].Valor);
        Assert.Equal(resultado.Detalhe.TotalBruto, desserializado.TotalBruto);
        Assert.Equal(resultado.Detalhe.Liquido, desserializado.Liquido);
    }

    [Fact]
    public void Calcular_DeveConsiderarRemuneracaoSemDataFimComoVigente()
    {
        var (contratoId, funcaoId, cooperadoId) = CriarIds();
        var segunda = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);

        // Remuneração sem data fim: deve ser considerada vigente na competência.
        var remuneracaoSemDataFim = CriarRemuneracao(contratoId, funcaoId, "HORA", 100m);
        remuneracaoSemDataFim.DataFim = null;

        var resultado = CalculadoraFolhaPagamento.Calcular(
            InicioCompetencia,
            FimCompetenciaExclusivo,
            new[] { CriarRegistro(cooperadoId, segunda, 8, 10) },
            new[] { CriarAlocacao(cooperadoId, contratoId, funcaoId, InicioCompetencia) },
            new[] { CriarJornada(contratoId, funcaoId, 8, 17) },
            new[] { remuneracaoSemDataFim },
            Array.Empty<ContratoFuncaoAdicional>(),
            new[] { CriarRegraHoraExtra(contratoId, 50m, 100m) },
            Array.Empty<DespesaFolha>(),
            new ConfiguracaoHoraNoturna { HoraInicio = new TimeSpan(22, 0, 0), HoraFim = new TimeSpan(5, 0, 0), HoraReduzida = true });

        Assert.NotNull(resultado);
        Assert.Single(resultado!.Detalhe.Grupos);
        // 2h trabalhadas x 100 = 200 de base, sem extras
        Assert.Equal(200m, resultado.ValorRemuneracaoBase);
        Assert.Equal(200m, resultado.TotalBruto);
    }

    private static (Guid ContratoId, Guid FuncaoId, Guid CooperadoId) CriarIds()
        => (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    private static Alocacao CriarAlocacao(Guid cooperadoId, Guid contratoId, Guid funcaoId, DateTime data) => new()
    {
        CooperadoId = cooperadoId,
        FuncaoId = funcaoId,
        DataInicio = data,
        ContratoId = contratoId
    };

    private static JornadaContratual CriarJornada(Guid contratoId, Guid funcaoId, int horaInicio, int horaFim) => new()
    {
        ContratoId = contratoId,
        FuncaoId = funcaoId,
        HoraInicio = new TimeSpan(horaInicio, 0, 0),
        HoraFim = new TimeSpan(horaFim, 0, 0),
        DiasSemana = DiaSemanaFlags.SegundaASexta
    };

    private static RegistroPonto CriarRegistro(Guid cooperadoId, DateTime data, int horaEntrada, int horaSaida) => new()
    {
        CooperadoId = cooperadoId,
        Data = data,
        Entrada = data.AddHours(horaEntrada),
        Saida = horaSaida > horaEntrada ? data.AddHours(horaSaida) : data.AddDays(1).AddHours(horaSaida)
    };

    private static Remuneracao CriarRemuneracao(Guid contratoId, Guid funcaoId, string tipo, decimal valor) => new()
    {
        ContratoId = contratoId,
        FuncaoId = funcaoId,
        TipoRemuneracao = tipo,
        Valor = valor,
        DataInicio = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
        DataFim = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc)
    };

    private static ContratoFuncaoAdicional CriarAdicionalVinculado(Guid contratoId, Guid funcaoId, string tipo, decimal valor) => new()
    {
        ContratoId = contratoId,
        FuncaoId = funcaoId,
        Adicional = new TipoAdicional { Nome = "Adicional teste", TipoCalculo = tipo, Valor = valor }
    };

    private static RegraHoraExtra CriarRegraHoraExtra(Guid? contratoId, decimal percentualComum, decimal percentualEspecial) => new()
    {
        ContratoId = contratoId,
        PercentualHeComum = percentualComum,
        PercentualHeDomingoFeriado = percentualEspecial
    };

    private static DespesaFolha CriarDespesa(string tipo, decimal valor, decimal? teto) => new()
    {
        Nome = "Despesa teste",
        TipoCalculo = tipo,
        Valor = valor,
        TetoRetencao = teto,
        Ativo = true
    };
}
