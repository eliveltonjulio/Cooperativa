
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cooperativa.Models;

public class Remuneracao
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Selecione um contrato.")]
    public Guid ContratoId { get; set; }
    public Contrato? Contrato { get; set; }

    [Required(ErrorMessage = "Selecione uma função.")]
    public Guid FuncaoId { get; set; }
    public Funcao? Funcao { get; set; }

    [Range(0, 100000000, ErrorMessage = "O valor deve ser positivo.")]
    public decimal Valor { get; set; }

    /// <summary>
    /// Valor digitado no formulário (aceita 1.500,50 ou 1500.50). Não é persistido:
    /// é convertido para <see cref="Valor"/> pelo controller.
    /// </summary>
    [NotMapped]
    [Display(Name = "Valor")]
    public string? ValorTexto { get; set; }

    [Required(ErrorMessage = "Informe a data de início.")]
    [Display(Name = "Data de início")]
    public DateTime DataInicio { get; set; }

    /// <summary>
    /// Data final da vigência. Opcional: sem data fim a remuneração permanece vigente
    /// por prazo indeterminado.
    /// </summary>
    [Display(Name = "Data fim (opcional)")]
    [DataType(DataType.Date)]
    public DateTime? DataFim { get; set; }

    [Required(ErrorMessage = "Selecione o tipo de remuneração.")]
    [StringLength(10)]
    [Display(Name = "Tipo de remuneração")]
    public string TipoRemuneracao { get; set; } = "DIA";
}
