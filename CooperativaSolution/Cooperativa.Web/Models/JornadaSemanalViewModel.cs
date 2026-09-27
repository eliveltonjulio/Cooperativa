using System.ComponentModel.DataAnnotations;
using Cooperativa.Models;

namespace Cooperativa.Web.Models;

public class DiaJornadaInputModel
{
    public int DiaSemanaValor { get; set; }
    public string NomeDia { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public TimeSpan HoraInicio { get; set; } = new TimeSpan(8, 0, 0);
    public TimeSpan HoraFim { get; set; } = new TimeSpan(17, 0, 0);
    public TimeSpan? HoraInicioIntervalo { get; set; }
    public TimeSpan? HoraFimIntervalo { get; set; }
}

public class JornadaSemanalViewModel
{
    public Guid? IdOriginal { get; set; }

    [Required(ErrorMessage = "Selecione um contrato.")]
    [Display(Name = "Contrato (empresa)")]
    public Guid ContratoId { get; set; }

    [Required(ErrorMessage = "Selecione uma função.")]
    [Display(Name = "Função")]
    public Guid FuncaoId { get; set; }

    public List<DiaJornadaInputModel> Dias { get; set; } = new();

    public static JornadaSemanalViewModel CriarPadrao()
    {
        return new JornadaSemanalViewModel
        {
            Dias = new List<DiaJornadaInputModel>
            {
                new() { DiaSemanaValor = DiaSemanaFlags.Segunda, NomeDia = "Segunda-feira", Ativo = true, HoraInicio = new TimeSpan(8, 0, 0), HoraFim = new TimeSpan(17, 0, 0) },
                new() { DiaSemanaValor = DiaSemanaFlags.Terca, NomeDia = "Terça-feira", Ativo = true, HoraInicio = new TimeSpan(8, 0, 0), HoraFim = new TimeSpan(17, 0, 0) },
                new() { DiaSemanaValor = DiaSemanaFlags.Quarta, NomeDia = "Quarta-feira", Ativo = true, HoraInicio = new TimeSpan(8, 0, 0), HoraFim = new TimeSpan(17, 0, 0) },
                new() { DiaSemanaValor = DiaSemanaFlags.Quinta, NomeDia = "Quinta-feira", Ativo = true, HoraInicio = new TimeSpan(8, 0, 0), HoraFim = new TimeSpan(17, 0, 0) },
                new() { DiaSemanaValor = DiaSemanaFlags.Sexta, NomeDia = "Sexta-feira", Ativo = true, HoraInicio = new TimeSpan(8, 0, 0), HoraFim = new TimeSpan(17, 0, 0) },
                new() { DiaSemanaValor = DiaSemanaFlags.Sabado, NomeDia = "Sábado", Ativo = false, HoraInicio = new TimeSpan(8, 0, 0), HoraFim = new TimeSpan(12, 0, 0) },
                new() { DiaSemanaValor = DiaSemanaFlags.Domingo, NomeDia = "Domingo", Ativo = false, HoraInicio = new TimeSpan(8, 0, 0), HoraFim = new TimeSpan(12, 0, 0) }
            }
        };
    }
}
