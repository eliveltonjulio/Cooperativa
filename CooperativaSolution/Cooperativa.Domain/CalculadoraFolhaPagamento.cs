namespace Cooperativa.Models;

/// <summary>Resultado do cálculo de folha de um cooperado em uma competência.</summary>
public sealed class ResultadoFolha
{
    public decimal HorasNormais { get; init; }
    public decimal HorasExtras { get; init; }
    public decimal HorasNoturnas { get; init; }
    public int DiasTrabalhados { get; init; }

    /// <summary>Remuneração base da competência (mapeia para FolhaPagamento.Salario).</summary>
    public decimal ValorRemuneracaoBase { get; init; }

    /// <summary>Valor pago pelas horas extras (comuns e especiais).</summary>
    public decimal ValorHorasExtras { get; init; }

    /// <summary>Adicional noturno (percentual legal sobre a hora normal).</summary>
    public decimal ValorAdicionalNoturno { get; init; }

    /// <summary>Soma dos adicionais vinculados por contrato/função.</summary>
    public decimal ValorAdicionais { get; init; }

    public decimal TotalBruto { get; init; }
    public decimal TotalDescontos { get; init; }
    public decimal Liquido { get; init; }

    /// <summary>Detalhamento por contrato/função e por despesa, exibido na tela de detalhes.</summary>
    public DetalheFolha Detalhe { get; init; } = new();
}

/// <summary>
/// Calcula a folha de pagamento de um cooperado em uma competência a partir dos dados
/// de ponto (horas normais, extras e noturnas), remuneração por contrato/função,
/// adicionais vinculados por contrato/função e despesas da folha (descontos).
/// </summary>
public static class CalculadoraFolhaPagamento
{
    /// <summary>Percentual legal do adicional noturno (CLT, art. 73).</summary>
    public const decimal PercentualAdicionalNoturno = 20m;

    /// <summary>Horas mensais padrão (44h semanais) para converter valor mensal em valor-hora.</summary>
    public const decimal HorasMensaisPadrao = 220m;

    /// <summary>Jornada diária padrão para converter valor diário em valor-hora.</summary>
    public const decimal HorasDiariasPadrao = 8m;

    /// <summary>Percentual padrão de HE comum quando não há regra cadastrada.</summary>
    public const decimal PercentualHeComumPadrao = 50m;

    /// <summary>Percentual padrão de HE em domingo/feriado quando não há regra cadastrada.</summary>
    public const decimal PercentualHeDomingoFeriadoPadrao = 100m;

    public static ResultadoFolha? Calcular(
        DateTime inicioCompetencia,
        DateTime fimCompetenciaExclusivo,
        IEnumerable<RegistroPonto> registros,
        IEnumerable<Alocacao> alocacoes,
        IEnumerable<JornadaContratual> jornadas,
        IEnumerable<Remuneracao> remuneracoes,
        IEnumerable<ContratoFuncaoAdicional> adicionaisVinculados,
        IEnumerable<RegraHoraExtra> regrasHoraExtra,
        IEnumerable<DespesaFolha> despesasFolha,
        ConfiguracaoHoraNoturna? configuracaoNoturna,
        IEnumerable<Feriado>? feriados = null)
    {
        var grupos = alocacoes
            .Where(alocacao => alocacao.ContratoId != Guid.Empty)
            .GroupBy(alocacao => (ContratoId: alocacao.ContratoId, alocacao.FuncaoId))
            .ToList();

        if (grupos.Count == 0)
        {
            return null;
        }

        var detalhe = new DetalheFolha();
        var listaRegistros = registros as IReadOnlyList<RegistroPonto> ?? registros.ToList();
        var diasTrabalhadosTotal = new HashSet<DateTime>();

        decimal valorBase = 0;
        decimal valorHorasExtras = 0;
        decimal valorAdicionalNoturno = 0;
        decimal valorAdicionais = 0;
        decimal totalHorasNormais = 0;
        decimal totalHorasExtras = 0;
        decimal totalHorasNoturnas = 0;

        foreach (var grupo in grupos)
        {
            var contratoId = grupo.Key.ContratoId;
            var funcaoId = grupo.Key.FuncaoId;
            var alocacoesGrupo = grupo.ToList();

            // Apura o ponto do grupo: registros válidos somente nos dias com alocação ativa deste grupo.
            var totais = CalculadoraApuracaoPontoMensal.Calcular(
                listaRegistros,
                alocacoesGrupo,
                jornadas.Where(j => j.ContratoId == contratoId).ToList(),
                configuracaoNoturna,
                feriados);

            totalHorasNormais += totais.HorasNormais;
            totalHorasExtras += totais.HorasExtras;
            totalHorasNoturnas += totais.HorasNoturnas;

            var diasGrupo = new HashSet<DateTime>();
            foreach (var registro in listaRegistros)
            {
                if (!registro.Entrada.HasValue || !registro.Saida.HasValue || registro.Saida <= registro.Entrada)
                {
                    continue;
                }

                var dataRegistro = registro.Data.Date;
                var alocacaoAtiva = alocacoesGrupo.Any(alocacao =>
                    alocacao.DataInicio.Date <= dataRegistro
                    && (alocacao.DataFim == null || alocacao.DataFim.Value.Date >= dataRegistro));

                if (alocacaoAtiva)
                {
                    diasGrupo.Add(dataRegistro);
                    diasTrabalhadosTotal.Add(dataRegistro);
                }
            }

            // Remuneração vigente na competência para o contrato/função (a mais recente prevalece).
            // A data fim é opcional: quando não informada, a remuneração permanece vigente.
            var remuneracao = remuneracoes
                .Where(r => r.ContratoId == contratoId
                    && r.FuncaoId == funcaoId
                    && r.DataInicio.Date < fimCompetenciaExclusivo
                    && (r.DataFim == null || r.DataFim.Value.Date >= inicioCompetencia))
                .OrderByDescending(r => r.DataInicio)
                .FirstOrDefault();

            if (remuneracao == null)
            {
                // Sem remuneração cadastrada para este contrato/função não há base de cálculo.
                continue;
            }

            var (valorHora, baseGrupo) = ObterValoresRemuneracao(remuneracao, totais.HorasNormais, diasGrupo.Count);

            // Percentuais de hora extra: regra do contrato; sem regra específica, usa a global (sem contrato).
            var regra = regrasHoraExtra.FirstOrDefault(r => r.ContratoId == contratoId)
                ?? regrasHoraExtra.FirstOrDefault(r => r.ContratoId == null);
            var percentualComum = regra?.PercentualHeComum ?? PercentualHeComumPadrao;
            var percentualEspecial = regra?.PercentualHeDomingoFeriado ?? PercentualHeDomingoFeriadoPadrao;

            // Arredonda por linha do detalhamento para que a soma das linhas bata com os totais.
            var valorHeComumGrupo = Arredondar(valorHora * totais.HorasExtrasComum * (1 + percentualComum / 100m));
            var valorHeEspecialGrupo = Arredondar(valorHora * totais.HorasExtrasEspeciais * (1 + percentualEspecial / 100m));
            var valorHeGrupo = valorHeComumGrupo + valorHeEspecialGrupo;

            var valorNoturnoGrupo = Arredondar(valorHora * totais.HorasNoturnas * (1 + PercentualAdicionalNoturno / 100m));

            var baseGrupoArredondada = Arredondar(baseGrupo);

            var itensAdicionais = new List<DetalheItemFolha>();
            foreach (var vinculo in adicionaisVinculados.Where(v => v.ContratoId == contratoId && v.FuncaoId == funcaoId))
            {
                var adicional = vinculo.Adicional;
                if (adicional == null)
                {
                    continue;
                }

                var tipoCalculoAdicional = adicional.TipoCalculo?.Trim().ToUpperInvariant() ?? string.Empty;
                var valorAdicional = tipoCalculoAdicional switch
                {
                    "PERCENTUAL_HORA" => valorHora * (adicional.Valor / 100m) * totais.HorasNormais,
                    "PERCENTUAL_BASE" => (adicional.Valor / 100m) * baseGrupo,
                    "VALOR_FIXO" => adicional.Valor,
                    _ => 0m
                };

                var descricaoAdicional = tipoCalculoAdicional switch
                {
                    "PERCENTUAL_HORA" => $"{adicional.Nome} ({adicional.Valor}% por hora)",
                    "PERCENTUAL_BASE" => $"{adicional.Nome} ({adicional.Valor}% sobre a base)",
                    "VALOR_FIXO" => $"{adicional.Nome} (valor fixo)",
                    _ => adicional.Nome
                };

                itensAdicionais.Add(new DetalheItemFolha
                {
                    Descricao = descricaoAdicional,
                    Valor = Arredondar(valorAdicional)
                });
            }

            var somaAdicionaisGrupo = itensAdicionais.Sum(item => item.Valor);

            valorBase += baseGrupoArredondada;
            valorHorasExtras += valorHeGrupo;
            valorAdicionalNoturno += valorNoturnoGrupo;
            valorAdicionais += somaAdicionaisGrupo;

            detalhe.Grupos.Add(new DetalheGrupoFolha
            {
                ContratoId = contratoId,
                FuncaoId = funcaoId,
                TipoRemuneracao = remuneracao.TipoRemuneracao ?? string.Empty,
                ValorHora = Arredondar(valorHora),
                HorasNormais = totais.HorasNormais,
                HorasExtrasComum = totais.HorasExtrasComum,
                HorasExtrasEspeciais = totais.HorasExtrasEspeciais,
                HorasNoturnas = totais.HorasNoturnas,
                DiasTrabalhados = diasGrupo.Count,
                PercentualHeComum = percentualComum,
                PercentualHeEspecial = percentualEspecial,
                ValorHorasNormais = baseGrupoArredondada,
                ValorHorasExtrasComum = valorHeComumGrupo,
                ValorHorasExtrasEspeciais = valorHeEspecialGrupo,
                ValorAdicionalNoturno = valorNoturnoGrupo,
                Adicionais = itensAdicionais,
                TotalGrupo = Arredondar(baseGrupoArredondada + valorHeGrupo + valorNoturnoGrupo + somaAdicionaisGrupo)
            });
        }

        var bruto = valorBase + valorHorasExtras + valorAdicionalNoturno + valorAdicionais;

        var descontos = 0m;
        foreach (var despesa in despesasFolha.Where(d => d.Ativo))
        {
            var tipoCalculoDespesa = despesa.TipoCalculo?.Trim().ToUpperInvariant() ?? string.Empty;
            var valorDesconto = tipoCalculoDespesa switch
            {
                "PERCENTUAL_REMUNERACAO" => bruto * (despesa.Valor / 100m),
                "VALOR_FIXO" => despesa.Valor,
                _ => 0m
            };

            var descricaoDespesa = tipoCalculoDespesa switch
            {
                "PERCENTUAL_REMUNERACAO" => $"{despesa.Nome} ({despesa.Valor}% sobre o bruto)",
                "VALOR_FIXO" => $"{despesa.Nome} (valor fixo)",
                _ => despesa.Nome
            };

            if (despesa.TetoRetencao.HasValue && valorDesconto > despesa.TetoRetencao.Value)
            {
                valorDesconto = despesa.TetoRetencao.Value;
                descricaoDespesa += " — teto de retenção aplicado";
            }

            var valorDescontoArredondado = Arredondar(valorDesconto);
            descontos += valorDescontoArredondado;

            detalhe.Descontos.Add(new DetalheItemFolha
            {
                Descricao = descricaoDespesa,
                Valor = valorDescontoArredondado
            });
        }

        detalhe.TotalBruto = Arredondar(bruto);
        detalhe.TotalDescontos = Arredondar(descontos);
        detalhe.Liquido = Arredondar(bruto - descontos);

        return new ResultadoFolha
        {
            HorasNormais = totalHorasNormais,
            HorasExtras = totalHorasExtras,
            HorasNoturnas = totalHorasNoturnas,
            DiasTrabalhados = diasTrabalhadosTotal.Count,
            ValorRemuneracaoBase = Arredondar(valorBase),
            ValorHorasExtras = Arredondar(valorHorasExtras),
            ValorAdicionalNoturno = Arredondar(valorAdicionalNoturno),
            ValorAdicionais = Arredondar(valorAdicionais),
            TotalBruto = Arredondar(bruto),
            TotalDescontos = Arredondar(descontos),
            Liquido = Arredondar(bruto - descontos),
            Detalhe = detalhe
        };
    }

    private static (decimal ValorHora, decimal ValorBase) ObterValoresRemuneracao(
        Remuneracao remuneracao, decimal horasNormais, int diasTrabalhados)
    {
        return remuneracao.TipoRemuneracao?.Trim().ToUpperInvariant() switch
        {
            // Mensal: valor integral da competência; valor-hora deriva do padrão de 220h/mês.
            "MES" => (remuneracao.Valor / HorasMensaisPadrao, remuneracao.Valor),

            // Diário: valor por dia com registro de ponto válido; valor-hora deriva de 8h/dia.
            "DIA" => (remuneracao.Valor / HorasDiariasPadrao, remuneracao.Valor * diasTrabalhados),

            // Hora (padrão): valor por hora apurada como normal.
            _ => (remuneracao.Valor, remuneracao.Valor * horasNormais)
        };
    }

    private static decimal Arredondar(decimal valor)
    {
        return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }
}
