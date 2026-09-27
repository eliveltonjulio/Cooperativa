using System.ComponentModel.DataAnnotations;

namespace Cooperativa.Models;

public class FolhaPagamento
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Selecione um cooperado.")]
    public Guid CooperadoId { get; set; }

    public Cooperado? Cooperado { get; set; }

    [Required(ErrorMessage = "A competência é obrigatória.")]
    public DateTime Competencia { get; set; }

    [Range(0, 100000000, ErrorMessage = "O salário deve ser positivo.")]
    public decimal Salario { get; set; }

    [Range(0, 100000000, ErrorMessage = "O adicional deve ser positivo.")]
    public decimal Adicional { get; set; }

    [Range(0, 100000000, ErrorMessage = "Os descontos devem ser positivos.")]
    public decimal Descontos { get; set; }

    /// <summary>JSON com o detalhamento do cálculo (horas, adicionais e descontos); nulo em folhas manuais.</summary>
    public string? Detalhes { get; set; }

    public decimal Liquido => Salario + Adicional - Descontos;
}
