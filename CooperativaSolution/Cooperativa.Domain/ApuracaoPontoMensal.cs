using System.ComponentModel.DataAnnotations;

namespace Cooperativa.Models;

public class ApuracaoPontoMensal
{
    [Required]
    public Guid CooperadoId { get; set; }

    public Cooperado? Cooperado { get; set; }

    [Required(ErrorMessage = "O mês e ano são obrigatórios.")]
    [Display(Name = "Mês/ano")]
    public DateTime MesAno { get; set; }

    [Range(0, 10000, ErrorMessage = "A quantidade de horas normais deve ser positiva.")]
    [Display(Name = "Horas normais")]
    public decimal QtdHorasNormais { get; set; }

    [Range(0, 10000, ErrorMessage = "A quantidade de horas extras deve ser positiva.")]
    [Display(Name = "Horas extras")]
    public decimal QtdHorasExtras { get; set; }

    [Range(0, 10000, ErrorMessage = "A quantidade de horas noturnas deve ser positiva.")]
    [Display(Name = "Horas noturnas")]
    public decimal QtdHorasNoturnas { get; set; }
}