namespace Cooperativa.Models;

/// <summary>Detalhamento completo de uma folha gerada, persistido em FolhaPagamento.Detalhes (JSON).</summary>
public sealed class DetalheFolha
{
    /// <summary>Blocos de cálculo por contrato/função da alocação.</summary>
    public List<DetalheGrupoFolha> Grupos { get; set; } = new();

    /// <summary>Descontos aplicados (despesas da folha ativas).</summary>
    public List<DetalheItemFolha> Descontos { get; set; } = new();

    public decimal TotalBruto { get; set; }
    public decimal TotalDescontos { get; set; }
    public decimal Liquido { get; set; }
}

/// <summary>Bloco de cálculo de uma folha para uma combinação contrato/função.</summary>
public sealed class DetalheGrupoFolha
{
    public Guid ContratoId { get; set; }
    public Guid FuncaoId { get; set; }

    /// <summary>Tipo da remuneração vigente usada no cálculo (HORA, DIA ou MES).</summary>
    public string TipoRemuneracao { get; set; } = string.Empty;

    /// <summary>Valor da hora usado nas horas extras e no adicional noturno.</summary>
    public decimal ValorHora { get; set; }

    public decimal HorasNormais { get; set; }
    public decimal HorasExtrasComum { get; set; }
    public decimal HorasExtrasEspeciais { get; set; }
    public decimal HorasNoturnas { get; set; }
    public int DiasTrabalhados { get; set; }
    public decimal PercentualHeComum { get; set; }
    public decimal PercentualHeEspecial { get; set; }

    /// <summary>Valor pago pelas horas normais (base da remuneração).</summary>
    public decimal ValorHorasNormais { get; set; }

    /// <summary>Valor pago pelas horas extras comuns.</summary>
    public decimal ValorHorasExtrasComum { get; set; }

    /// <summary>Valor pago pelas horas extras em domingo/feriado.</summary>
    public decimal ValorHorasExtrasEspeciais { get; set; }

    /// <summary>Valor do adicional noturno.</summary>
    public decimal ValorAdicionalNoturno { get; set; }

    /// <summary>Adicionais vinculados por contrato/função aplicados neste bloco.</summary>
    public List<DetalheItemFolha> Adicionais { get; set; } = new();

    /// <summary>Total pago neste bloco (base + extras + noturno + adicionais).</summary>
    public decimal TotalGrupo { get; set; }
}

/// <summary>Linha do detalhamento (adicional ou desconto) com descrição legível e valor.</summary>
public sealed class DetalheItemFolha
{
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}
