using System.ComponentModel.DataAnnotations;

namespace Cooperativa.Web.Models;

/// <summary>Dados do formulário de geração automática de folha de pagamento.</summary>
public class GerarFolhaViewModel
{
    public const string EscopoCooperado = "COOPERADO";
    public const string EscopoContrato = "CONTRATO";

    [Required(ErrorMessage = "Informe o mês/ano da competência.")]
    [Display(Name = "Competência (mês/ano)")]
    public DateTime? Competencia { get; set; }

    /// <summary>COOPERADO gera para um cooperado; CONTRATO gera para todos os cooperados alocados ao contrato.</summary>
    [Required(ErrorMessage = "Selecione o escopo de geração.")]
    [Display(Name = "Gerar folha por")]
    public string Escopo { get; set; } = EscopoCooperado;

    [Display(Name = "Cooperado")]
    public Guid? CooperadoId { get; set; }

    [Display(Name = "Contrato (empresa)")]
    public Guid? ContratoId { get; set; }
}
