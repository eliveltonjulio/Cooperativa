using System.ComponentModel.DataAnnotations;

namespace Cooperativa.Models;

public class Feriado
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "A data é obrigatória.")]
    public DateTime Data { get; set; }

    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [StringLength(100, ErrorMessage = "A descrição deve ter até 100 caracteres.")]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "O tipo é obrigatório.")]
    [RegularExpression("^(NACIONAL|ESTADUAL|MUNICIPAL)$", ErrorMessage = "Informe um tipo válido.")]
    public string Tipo { get; set; } = "NACIONAL";
}
