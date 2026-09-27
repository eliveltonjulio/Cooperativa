using System.ComponentModel.DataAnnotations;

namespace Cooperativa.Models;

public class DespesaFolha
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "O nome da despesa é obrigatório.")]
    [StringLength(120, ErrorMessage = "O nome deve ter até 120 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O tipo de cálculo é obrigatório.")]
    [RegularExpression("^(PERCENTUAL_REMUNERACAO|VALOR_FIXO)$", ErrorMessage = "Informe um tipo de cálculo válido.")]
    [Display(Name = "Tipo de cálculo")]
    public string TipoCalculo { get; set; } = string.Empty;

    [Range(0, 100000000, ErrorMessage = "O valor deve ser positivo.")]
    public decimal Valor { get; set; }

    [Range(0, 100000000, ErrorMessage = "O teto de retenção deve ser positivo.")]
    [Display(Name = "Teto de retenção")]
    public decimal? TetoRetencao { get; set; }

    public bool Ativo { get; set; } = true;
}