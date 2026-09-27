namespace Cooperativa.Models;

public sealed class TotaisApuracaoPonto
{
    public decimal HorasNormais { get; init; }
    public decimal HorasExtras { get; init; }

    /// <summary>Horas extras prestadas em feriados ou em dias sem jornada contratada (domingo/feriado).</summary>
    public decimal HorasExtrasEspeciais { get; init; }

    /// <summary>Horas extras comuns (dentro da jornada semanal), já descontadas as especiais.</summary>
    public decimal HorasExtrasComum => Math.Max(0, HorasExtras - HorasExtrasEspeciais);

    public decimal HorasNoturnas { get; init; }
}

public static class CalculadoraApuracaoPontoMensal
{
    private static readonly TimeSpan DuracaoHoraNoturnaReduzida = TimeSpan.FromMinutes(52.5);

    public static TotaisApuracaoPonto Calcular(
        IEnumerable<RegistroPonto> registros,
        IEnumerable<Alocacao> alocacoes,
        IEnumerable<JornadaContratual> jornadas,
        ConfiguracaoHoraNoturna? configuracaoNoturna,
        IEnumerable<Feriado>? feriados = null)
    {
        var jornadasPorContratoFuncao = jornadas
            .GroupBy(jornada => (jornada.ContratoId, jornada.FuncaoId))
            .ToDictionary(g => g.Key, g => g.ToList());
        var feriadosSet = feriados?
            .Select(f => DateOnly.FromDateTime(f.Data))
            .ToHashSet() ?? new HashSet<DateOnly>();
        decimal minutosNormais = 0;
        decimal minutosExtras = 0;
        decimal minutosExtrasEspeciais = 0;
        decimal minutosNoturnos = 0;

        var registrosPorJornadaEDia = new List<(RegistroPonto Registro, JornadaContratual? Jornada, DateTime Data, IReadOnlyList<(DateTime Inicio, DateTime Fim)> PeriodosTrabalhados)>();

        foreach (var registro in registros.Where(registro => registro.Entrada.HasValue && registro.Saida.HasValue && registro.Saida > registro.Entrada))
        {
            var dataRegistro = registro.Data.Date;
            var alocacao = alocacoes
                .Where(item => item.CooperadoId == registro.CooperadoId
                    && item.DataInicio.Date <= dataRegistro
                    && (item.DataFim == null || item.DataFim.Value.Date >= dataRegistro)
                    && item.ContratoId != Guid.Empty)
                .OrderByDescending(item => item.DataInicio)
                .FirstOrDefault();

            if (alocacao is null)
            {
                continue;
            }

            var contratoId = alocacao.ContratoId;
            var diaDaSemana = 1 << (int)dataRegistro.DayOfWeek;
            JornadaContratual? jornada = null;
            if (jornadasPorContratoFuncao.TryGetValue((contratoId, alocacao.FuncaoId), out var listaJornadas))
            {
                jornada = listaJornadas.FirstOrDefault(j => (j.DiasSemana & diaDaSemana) != 0)
                    ?? listaJornadas.FirstOrDefault();
            }

            var inicioTrabalho = registro.Entrada!.Value;
            var fimTrabalho = registro.Saida!.Value;
            var periodosTrabalhados = ObterPeriodosTrabalhados(registro, inicioTrabalho, fimTrabalho);
            registrosPorJornadaEDia.Add((registro, jornada, dataRegistro, periodosTrabalhados));

            if (configuracaoNoturna != null)
            {
                minutosNoturnos += periodosTrabalhados.Sum(periodo => CalcularMinutosNoturnos(periodo.Inicio, periodo.Fim, configuracaoNoturna));
            }
        }

        foreach (var grupo in registrosPorJornadaEDia.GroupBy(item => (item.Data, item.Jornada?.ContratoId, item.Jornada?.FuncaoId)))
        {
            var data = DateOnly.FromDateTime(grupo.Key.Data);
            var jornada = grupo.First().Jornada;
            var diaDaSemana = 1 << (int)grupo.Key.Data.DayOfWeek;
            var minutosTrabalhadosTotal = grupo.Sum(item => item.PeriodosTrabalhados.Sum(periodo => (decimal)(periodo.Fim - periodo.Inicio).TotalMinutes));
            var minutosNoturnosDoGrupo = configuracaoNoturna == null
                ? 0
                : grupo.Sum(item => item.PeriodosTrabalhados.Sum(periodo => CalcularMinutosNoturnos(periodo.Inicio, periodo.Fim, configuracaoNoturna)));
            var minutosDiurnosTrabalhados = Math.Max(0, minutosTrabalhadosTotal - minutosNoturnosDoGrupo);

            var ehFeriado = feriadosSet.Contains(data);
            var diaSemJornada = ehFeriado || jornada == null || (jornada.DiasSemana & diaDaSemana) == 0;
            var minutosNormaisDoRegistro = diaSemJornada
                ? 0
                : Math.Min(minutosDiurnosTrabalhados, ObterMinutosDaJornada(jornada));

            minutosNormais += minutosNormaisDoRegistro;
            minutosExtras += minutosDiurnosTrabalhados - minutosNormaisDoRegistro;
            if (diaSemJornada)
            {
                // Em feriados ou dias sem jornada contratada, todas as horas extras são especiais (domingo/feriado).
                minutosExtrasEspeciais += minutosDiurnosTrabalhados - minutosNormaisDoRegistro;
            }
        }

        var fatorHoraNoturna = configuracaoNoturna?.HoraReduzida == true
            ? (decimal)TimeSpan.TicksPerHour / DuracaoHoraNoturnaReduzida.Ticks
            : 1m;

        return new TotaisApuracaoPonto
        {
            HorasNormais = Math.Round(minutosNormais / 60, 2, MidpointRounding.AwayFromZero),
            HorasExtras = Math.Round(minutosExtras / 60, 2, MidpointRounding.AwayFromZero),
            HorasExtrasEspeciais = Math.Round(minutosExtrasEspeciais / 60, 2, MidpointRounding.AwayFromZero),
            HorasNoturnas = Math.Round((minutosNoturnos / 60) * fatorHoraNoturna, 2, MidpointRounding.AwayFromZero)
        };
    }

    private static decimal CalcularMinutosNoturnos(DateTime inicioTrabalho, DateTime fimTrabalho, ConfiguracaoHoraNoturna configuracao)
    {
        decimal minutos = 0;
        for (var data = inicioTrabalho.Date.AddDays(-1); data <= fimTrabalho.Date; data = data.AddDays(1))
        {
            var inicioNoturno = data.Add(configuracao.HoraInicio);
            var fimNoturno = data.Add(configuracao.HoraFim);
            if (fimNoturno <= inicioNoturno)
            {
                fimNoturno = fimNoturno.AddDays(1);
            }

            minutos += MinutosEmComum(inicioTrabalho, fimTrabalho, inicioNoturno, fimNoturno);
        }

        return minutos;
    }

    private static IReadOnlyList<(DateTime Inicio, DateTime Fim)> ObterPeriodosTrabalhados(RegistroPonto registro, DateTime inicioTrabalho, DateTime fimTrabalho)
    {
        if (!registro.InicioIntervalo.HasValue || !registro.FimIntervalo.HasValue
            || registro.InicioIntervalo < inicioTrabalho || registro.FimIntervalo <= registro.InicioIntervalo
            || registro.FimIntervalo > fimTrabalho)
        {
            return new[] { (inicioTrabalho, fimTrabalho) };
        }

        return new[]
        {
            (inicioTrabalho, registro.InicioIntervalo.Value),
            (registro.FimIntervalo.Value, fimTrabalho)
        };
    }

    private static decimal ObterMinutosDaJornada(JornadaContratual jornada)
    {
        var minutos = (decimal)(jornada.HoraFim - jornada.HoraInicio).TotalMinutes;
        if (minutos <= 0)
        {
            minutos += (decimal)TimeSpan.FromDays(1).TotalMinutes;
        }

        return minutos;
    }

    private static decimal MinutosEmComum(DateTime inicioPrimeiro, DateTime fimPrimeiro, DateTime inicioSegundo, DateTime fimSegundo)
    {
        var inicio = inicioPrimeiro > inicioSegundo ? inicioPrimeiro : inicioSegundo;
        var fim = fimPrimeiro < fimSegundo ? fimPrimeiro : fimSegundo;
        return fim > inicio ? (decimal)(fim - inicio).TotalMinutes : 0;
    }
}