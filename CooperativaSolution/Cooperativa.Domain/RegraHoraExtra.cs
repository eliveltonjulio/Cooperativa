using System.ComponentModel.DataAnnotations;

namespace Cooperativa.Models;

public class RegraHoraExtra
{
    public Guid Id { get; set; }

    public Guid? ContratoId { get; set; }

    public Contrato? Contrato { get; set; }

    [Range(0, 999.99, ErrorMessage = "O percentual de HE comum deve ser positivo.")]
    [Display(Name = "Percentual HE comum")]
    public decimal PercentualHeComum { get; set; } = 50.00m;

    [Range(0, 999.99, ErrorMessage = "O percentual de HE em domingo/feriado deve ser positivo.")]
    [Display(Name = "Percentual HE domingo/feriado")]
    public decimal PercentualHeDomingoFeriado { get; set; } = 100.00m;
}
