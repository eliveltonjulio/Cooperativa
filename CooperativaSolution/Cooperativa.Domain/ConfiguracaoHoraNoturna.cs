using System.ComponentModel.DataAnnotations;

namespace Cooperativa.Models;

/// <summary>Configuração única da cooperativa para o intervalo legal noturno e a regra de hora reduzida.</summary>
public class ConfiguracaoHoraNoturna
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "O horário de início é obrigatório.")]
    [Display(Name = "Início do horário noturno")]
    public TimeSpan HoraInicio { get; set; } = new TimeSpan(22, 0, 0);

    [Required(ErrorMessage = "O horário de fim é obrigatório.")]
    [Display(Name = "Fim do horário noturno")]
    public TimeSpan HoraFim { get; set; } = new TimeSpan(5, 0, 0);

    [Display(Name = "Adotar hora noturna reduzida (52min30s)")]
    public bool HoraReduzida { get; set; } = true;
}
