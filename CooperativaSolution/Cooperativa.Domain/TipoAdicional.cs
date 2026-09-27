using System.ComponentModel.DataAnnotations;

namespace Cooperativa.Models;

public class TipoAdicional
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(120, ErrorMessage = "O nome deve ter até 120 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O tipo de cálculo é obrigatório.")]
    [RegularExpression("^(PERCENTUAL_HORA|VALOR_FIXO|PERCENTUAL_BASE)$", ErrorMessage = "Informe um tipo de cálculo válido.")]
    public string TipoCalculo { get; set; } = string.Empty;

    [Range(0, 100000000, ErrorMessage = "O valor deve ser positivo.")]
    public decimal Valor { get; set; }
}