using System.ComponentModel.DataAnnotations;

namespace Cooperativa.Models;

public class Alocacao
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Selecione um cooperado.")]
    public Guid CooperadoId { get; set; }
    public Cooperado? Cooperado { get; set; }

    [Required(ErrorMessage = "Selecione um contrato.")]
    public Guid ContratoId { get; set; }
    public Contrato? Contrato { get; set; }

    [Required(ErrorMessage = "Selecione uma função.")]
    public Guid FuncaoId { get; set; }
    public Funcao? Funcao { get; set; }

    [Required(ErrorMessage = "A data de início é obrigatória.")]
    [Display(Name = "Data de início")]
    [DataType(DataType.Date)]
    public DateTime DataInicio { get; set; }

    /// <summary>Data final da alocação. Opcional: sem data fim a alocação fica em aberto.</summary>
    [Display(Name = "Data fim (opcional)")]
    [DataType(DataType.Date)]
    public DateTime? DataFim { get; set; }
}
