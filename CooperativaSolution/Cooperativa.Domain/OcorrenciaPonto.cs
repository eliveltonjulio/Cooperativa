namespace Cooperativa.Models;

/// <summary>
/// Informações de uma ocorrência que pode ser cadastrada no ponto do cooperado.
/// </summary>
public sealed class OcorrenciaPontoInfo
{
    public OcorrenciaPontoInfo(string codigo, string descricao, string tipoImpactoFolha)
    {
        Codigo = codigo;
        Descricao = descricao;
        TipoImpactoFolha = tipoImpactoFolha;
    }

    /// <summary>Código da ocorrência armazenado no registro de ponto.</summary>
    public string Codigo { get; }

    /// <summary>Descrição amigável exibida nas telas.</summary>
    public string Descricao { get; }

    /// <summary>Tipo de impacto esperado na folha de pagamento.</summary>
    public string TipoImpactoFolha { get; }
}

/// <summary>
/// Catálogo fixo das ocorrências que podem ser registradas no ponto do cooperado.
/// </summary>
public static class OcorrenciasPonto
{
    public const string CodigoFolga = "FOLGA";
    public const string CodigoFeriado = "FERIADO";
    public const string CodigoAtestado = "ATESTADO";
    public const string CodigoFalta = "FALTA";
    public const string CodigoAtraso = "ATRASO";
    public const string CodigoHoraExtraAprovada = "HE_APROV";
    public const string CodigoAbatimentoBancoFolga = "BANCO_FOLGA";

    public static readonly OcorrenciaPontoInfo Folga = new(
        CodigoFolga,
        "Folga Semanal / DSR",
        "Neutro (Horas Não Devidas)");

    public static readonly OcorrenciaPontoInfo Feriado = new(
        CodigoFeriado,
        "Feriado",
        "Neutro / Abonado");

    public static readonly OcorrenciaPontoInfo Atestado = new(
        CodigoAtestado,
        "Licença Médica com Atestado",
        "Abonado (Paga Horas Normais)");

    public static readonly OcorrenciaPontoInfo Falta = new(
        CodigoFalta,
        "Falta Injustificada",
        "Desconto (Abate Horas)");

    public static readonly OcorrenciaPontoInfo Atraso = new(
        CodigoAtraso,
        "Atraso / Saída Antecipada",
        "Desconto (Abate Minutos/Horas)");

    public static readonly OcorrenciaPontoInfo HoraExtraAprovada = new(
        CodigoHoraExtraAprovada,
        "Hora Extra Autorizada",
        "Adicional (Acresce Horas Extras)");

    public static readonly OcorrenciaPontoInfo AbatimentoBancoFolga = new(
        CodigoAbatimentoBancoFolga,
        "Abatimento de Banco de Horas",
        "Abonado (Consome Saldo Banco)");

    /// <summary>Todas as ocorrências na ordem de exibição nas telas.</summary>
    public static readonly OcorrenciaPontoInfo[] Todas =
    {
        Folga,
        Feriado,
        Atestado,
        Falta,
        Atraso,
        HoraExtraAprovada,
        AbatimentoBancoFolga,
    };

    /// <summary>Localiza uma ocorrência pelo código (sem diferenciar maiúsculas/minúsculas) ou retorna nulo.</summary>
    public static OcorrenciaPontoInfo? ObterPorCodigo(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return null;
        }

        var termo = codigo.Trim();
        return Todas.FirstOrDefault(o => string.Equals(o.Codigo, termo, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Indica se o código informado pertence ao catálogo.</summary>
    public static bool Existe(string? codigo) => ObterPorCodigo(codigo) != null;
}