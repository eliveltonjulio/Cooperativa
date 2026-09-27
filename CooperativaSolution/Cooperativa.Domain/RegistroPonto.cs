using System.ComponentModel.DataAnnotations;

namespace Cooperativa.Models;

public class RegistroPonto
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Selecione um cooperado.")]
    public Guid CooperadoId { get; set; }

    public Cooperado? Cooperado { get; set; }

    [Required(ErrorMessage = "A data é obrigatória.")]
    [Display(Name = "Data")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime Data { get; set; }

    [Display(Name = "Entrada")]
    [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? Entrada { get; set; }

    [Display(Name = "Saída")]
    [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? Saida { get; set; }

    [Display(Name = "Início do intervalo")]
    [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? InicioIntervalo { get; set; }

    [Display(Name = "Fim do intervalo")]
    [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? FimIntervalo { get; set; }

    /// <summary>Código da ocorrência do catálogo <see cref="OcorrenciasPonto"/>; nulo indica dia trabalhado comum.</summary>
    [StringLength(20, ErrorMessage = "A ocorrência deve ter até 20 caracteres.")]
    [Display(Name = "Ocorrência")]
    public string? Ocorrencia { get; set; }

    [StringLength(250, ErrorMessage = "A observação deve ter até 250 caracteres.")]
    public string? Observacao { get; set; }
}
