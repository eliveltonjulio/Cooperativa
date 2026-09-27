using Cooperativa.Models;
using Xunit;

namespace Cooperativa.Tests;

public class CalculadoraApuracaoPontoMensalTests
{
    [Fact]
    public void Calcular_SeparaHorasNormaisExtrasENoturnas()
    {
        var contratoId = Guid.NewGuid();
        var funcaoId = Guid.NewGuid();
        var cooperadoId = Guid.NewGuid();
        var data = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);
        var alocacao = CriarAlocacao(cooperadoId, contratoId, funcaoId, data);

        var totais = CalculadoraApuracaoPontoMensal.Calcular(
            new[]
            {
                CriarRegistro(cooperadoId, data, 8, 18),
                CriarRegistro(cooperadoId, data, 22, 23)
            },
            new[] { alocacao },
            new[] { CriarJornada(contratoId, funcaoId, 8, 17) },
            new ConfiguracaoHoraNoturna { HoraInicio = new TimeSpan(22, 0, 0), HoraFim = new TimeSpan(5, 0, 0), HoraReduzida = true });

        Assert.Equal(9m, totais.HorasNormais);
        Assert.Equal(1m, totais.HorasExtras);
        Assert.Equal(1.14m, totais.HorasNoturnas);
    }

    [Fact]
    public void Calcular_ContaFaixaNoturnaQueAtravessaMeiaNoite()
    {
        var contratoId = Guid.NewGuid();
        var funcaoId = Guid.NewGuid();
        var cooperadoId = Guid.NewGuid();
        var data = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);

        var totais = CalculadoraApuracaoPontoMensal.Calcular(
            new[] { CriarRegistro(cooperadoId, data, 22, 2) },
            new[] { CriarAlocacao(cooperadoId, contratoId, funcaoId, data) },
            new[] { CriarJornada(contratoId, funcaoId, 22, 2) },
            new ConfiguracaoHoraNoturna { HoraInicio = new TimeSpan(22, 0, 0), HoraFim = new TimeSpan(5, 0, 0), HoraReduzida = false });

        Assert.Equal(0m, totais.HorasNormais);
        Assert.Equal(0m, totais.HorasExtras);
        Assert.Equal(4m, totais.HorasNoturnas);
    }

    [Fact]
    public void Calcular_DescontaIntervaloRegistradoDoTotalTrabalhado()
    {
        var contratoId = Guid.NewGuid();
        var funcaoId = Guid.NewGuid();
        var cooperadoId = Guid.NewGuid();
        var data = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);
        var registro = CriarRegistro(cooperadoId, data, 8, 18);
        registro.InicioIntervalo = data.AddHours(12);
        registro.FimIntervalo = data.AddHours(13);

        var totais = CalculadoraApuracaoPontoMensal.Calcular(
            new[] { registro },
            new[] { CriarAlocacao(cooperadoId, contratoId, funcaoId, data) },
            new[] { CriarJornada(contratoId, funcaoId, 8, 17) },
            null);

        Assert.Equal(9m, totais.HorasNormais);
        Assert.Equal(0m, totais.HorasExtras);
        Assert.Equal(0m, totais.HorasNoturnas);
    }

    [Fact]
    public void Calcular_UsaDuracaoDaJornadaMesmoComHorariosDePontoDiferentes()
    {
        var contratoId = Guid.NewGuid();
        var funcaoId = Guid.NewGuid();
        var cooperadoId = Guid.NewGuid();
        var data = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);

        var totais = CalculadoraApuracaoPontoMensal.Calcular(
            new[] { CriarRegistro(cooperadoId, data, 10, 19) },
            new[] { CriarAlocacao(cooperadoId, contratoId, funcaoId, data) },
            new[] { CriarJornada(contratoId, funcaoId, 8, 17) },
            null);

        Assert.Equal(9m, totais.HorasNormais);
        Assert.Equal(0m, totais.HorasExtras);
    }

    [Fact]
    public void Calcular_ComparaOTotalDoDiaComACargaContratual()
    {
        var contratoId = Guid.NewGuid();
        var funcaoId = Guid.NewGuid();
        var cooperadoId = Guid.NewGuid();
        var data = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);

        var totais = CalculadoraApuracaoPontoMensal.Calcular(
            new[]
            {
                CriarRegistro(cooperadoId, data, 7, 12),
                CriarRegistro(cooperadoId, data, 13, 18)
            },
            new[] { CriarAlocacao(cooperadoId, contratoId, funcaoId, data) },
            new[] { CriarJornada(contratoId, funcaoId, 8, 17) },
            null);

        Assert.Equal(9m, totais.HorasNormais);
        Assert.Equal(1m, totais.HorasExtras);
    }

    [Fact]
    public void Calcular_ConsideraHorasTrabalhadasEmFeriadosComoHorasExtras()
    {
        var contratoId = Guid.NewGuid();
        var funcaoId = Guid.NewGuid();
        var cooperadoId = Guid.NewGuid();
        var dataFeriado = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc); // Segunda-feira (feriado)

        var feriado = new Feriado
        {
            Id = Guid.NewGuid(),
            Data = dataFeriado,
            Descricao = "Independência do Brasil",
            Tipo = "NACIONAL"
        };

        var totais = CalculadoraApuracaoPontoMensal.Calcular(
            new[] { CriarRegistro(cooperadoId, dataFeriado, 8, 17) },
            new[] { CriarAlocacao(cooperadoId, contratoId, funcaoId, dataFeriado) },
            new[] { CriarJornada(contratoId, funcaoId, 8, 17) },
            null,
            new[] { feriado });

        Assert.Equal(0m, totais.HorasNormais);
        Assert.Equal(9m, totais.HorasExtras);
    }

    [Fact]
    public void Calcular_MesComDiasUteisEFeriado_SeparaNormaisEExtras()
    {
        var contratoId = Guid.NewGuid();
        var funcaoId = Guid.NewGuid();
        var cooperadoId = Guid.NewGuid();
        var diaUtil = new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc); // Terça-feira (dia útil normal)
        var diaFeriado = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc); // Segunda-feira (feriado)

        var feriado = new Feriado
        {
            Id = Guid.NewGuid(),
            Data = diaFeriado,
            Descricao = "Independência do Brasil",
            Tipo = "NACIONAL"
        };

        var totais = CalculadoraApuracaoPontoMensal.Calcular(
            new[]
            {
                CriarRegistro(cooperadoId, diaFeriado, 8, 17),
                CriarRegistro(cooperadoId, diaUtil, 8, 17)
            },
            new[] { CriarAlocacao(cooperadoId, contratoId, funcaoId, diaFeriado) },
            new[] { CriarJornada(contratoId, funcaoId, 8, 17) },
            null,
            new[] { feriado });

        Assert.Equal(9m, totais.HorasNormais);
        Assert.Equal(9m, totais.HorasExtras);
    }

    [Fact]
    public void Calcular_FeriadoComHoraNoturna_CalculaHorasExtrasENoturnas()
    {
        var contratoId = Guid.NewGuid();
        var funcaoId = Guid.NewGuid();
        var cooperadoId = Guid.NewGuid();
        var dataFeriado = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);

        var feriado = new Feriado
        {
            Id = Guid.NewGuid(),
            Data = dataFeriado,
            Descricao = "Independência do Brasil",
            Tipo = "NACIONAL"
        };

        var totais = CalculadoraApuracaoPontoMensal.Calcular(
            new[]
            {
                CriarRegistro(cooperadoId, dataFeriado, 10, 14),
                CriarRegistro(cooperadoId, dataFeriado, 22, 2)
            },
            new[] { CriarAlocacao(cooperadoId, contratoId, funcaoId, dataFeriado) },
            new[] { CriarJornada(contratoId, funcaoId, 8, 17) },
            new ConfiguracaoHoraNoturna { HoraInicio = new TimeSpan(22, 0, 0), HoraFim = new TimeSpan(5, 0, 0), HoraReduzida = false },
            new[] { feriado });

        Assert.Equal(0m, totais.HorasNormais);
        Assert.Equal(4m, totais.HorasExtras);
        Assert.Equal(4m, totais.HorasNoturnas);
    }

    [Fact]
    public void Calcular_PermiteMultiplasJornadasPorDiaDaSemanaParaMesmoContratoEFuncao()
    {
        var contratoId = Guid.NewGuid();
        var funcaoId = Guid.NewGuid();
        var cooperadoId = Guid.NewGuid();

        // Segunda a Quinta: 13h às 22h (9h de jornada)
        var jornadaSegAQui = new JornadaContratual
        {
            ContratoId = contratoId,
            FuncaoId = funcaoId,
            HoraInicio = new TimeSpan(13, 0, 0),
            HoraFim = new TimeSpan(22, 0, 0),
            DiasSemana = DiaSemanaFlags.Segunda | DiaSemanaFlags.Terca | DiaSemanaFlags.Quarta | DiaSemanaFlags.Quinta
        };

        // Sexta: 13h às 17h (4h de jornada)
        var jornadaSex = new JornadaContratual
        {
            ContratoId = contratoId,
            FuncaoId = funcaoId,
            HoraInicio = new TimeSpan(13, 0, 0),
            HoraFim = new TimeSpan(17, 0, 0),
            DiasSemana = DiaSemanaFlags.Sexta
        };

        // Sábado: 14h às 18h (4h de jornada)
        var jornadaSab = new JornadaContratual
        {
            ContratoId = contratoId,
            FuncaoId = funcaoId,
            HoraInicio = new TimeSpan(14, 0, 0),
            HoraFim = new TimeSpan(18, 0, 0),
            DiasSemana = DiaSemanaFlags.Sabado
        };

        // 2026-09-08 é Terça-feira (trabalhou das 13h às 23h => 10h trabalhadas, 9h normais, 1h extra)
        var terca = new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc);
        // 2026-09-11 é Sexta-feira (trabalhou das 13h às 18h => 5h trabalhadas, 4h normais, 1h extra)
        var sexta = new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc);
        // 2026-09-12 é Sábado (trabalhou das 14h às 18h => 4h trabalhadas, 4h normais, 0h extra)
        var sabado = new DateTime(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc);

        var alocacao = CriarAlocacao(cooperadoId, contratoId, funcaoId, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

        var totais = CalculadoraApuracaoPontoMensal.Calcular(
            new[]
            {
                CriarRegistro(cooperadoId, terca, 13, 23),
                CriarRegistro(cooperadoId, sexta, 13, 18),
                CriarRegistro(cooperadoId, sabado, 14, 18)
            },
            new[] { alocacao },
            new[] { jornadaSegAQui, jornadaSex, jornadaSab },
            null);

        // Horas normais: 9 (terça) + 4 (sexta) + 4 (sábado) = 17h
        // Horas extras: 1 (terça) + 1 (sexta) + 0 (sábado) = 2h
        Assert.Equal(17m, totais.HorasNormais);
        Assert.Equal(2m, totais.HorasExtras);
    }

    [Fact]
    public void Calcular_JornadaQueViraOMesmoDiaParaDiaSeguinte()
    {
        var contratoId = Guid.NewGuid();
        var funcaoId = Guid.NewGuid();
        var cooperadoId = Guid.NewGuid();

        // Segunda-feira: 16h00 às 00h40 (dia seguinte) -> Duração = 8h40min (520 min)
        var jornadaNoturna = new JornadaContratual
        {
            ContratoId = contratoId,
            FuncaoId = funcaoId,
            HoraInicio = new TimeSpan(16, 0, 0),
            HoraFim = new TimeSpan(0, 40, 0),
            DiasSemana = DiaSemanaFlags.Segunda
        };

        // 2026-09-07 é Segunda-feira
        var segunda = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);
        var alocacao = CriarAlocacao(cooperadoId, contratoId, funcaoId, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

        var registro = new RegistroPonto
        {
            CooperadoId = cooperadoId,
            Data = segunda,
            Entrada = segunda.AddHours(16),
            Saida = segunda.AddDays(1).AddMinutes(40) // 00h40 do dia seguinte (8h40 = 520 min)
        };

        var totais = CalculadoraApuracaoPontoMensal.Calcular(
            new[] { registro },
            new[] { alocacao },
            new[] { jornadaNoturna },
            null);

        // 520 minutos / 60 = 8.67 horas normais
        Assert.Equal(8.67m, totais.HorasNormais);
        Assert.Equal(0m, totais.HorasExtras);
    }

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
}