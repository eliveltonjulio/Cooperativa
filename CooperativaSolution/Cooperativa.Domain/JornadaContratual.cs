using System.ComponentModel.DataAnnotations;

namespace Cooperativa.Models;

/// <summary>Bitmask dos dias da semana usados em JornadaContratual.DiasSemana.</summary>
public static class DiaSemanaFlags
{
    public const int Domingo = 1;
    public const int Segunda = 2;
    public const int Terca = 4;
    public const int Quarta = 8;
    public const int Quinta = 16;
    public const int Sexta = 32;
    public const int Sabado = 64;

    public const int SegundaASexta = Segunda | Terca | Quarta | Quinta | Sexta;

    public static readonly (int Valor, string Nome)[] Todos =
    {
        (Domingo, "Domingo"),
        (Segunda, "Segunda-feira"),
        (Terca, "Terça-feira"),
        (Quarta, "Quarta-feira"),
        (Quinta, "Quinta-feira"),
        (Sexta, "Sexta-feira"),
        (Sabado, "Sábado"),
    };
}

public class JornadaContratual
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Selecione um contrato.")]
    public Guid ContratoId { get; set; }
    public Contrato? Contrato { get; set; }

    [Required(ErrorMessage = "Selecione uma função.")]
    public Guid FuncaoId { get; set; }
    public Funcao? Funcao { get; set; }

    [Required(ErrorMessage = "O horário de início é obrigatório.")]
    [Display(Name = "Início da jornada")]
    public TimeSpan HoraInicio { get; set; } = new TimeSpan(8, 0, 0);

    [Required(ErrorMessage = "O horário de fim é obrigatório.")]
    [Display(Name = "Fim da jornada")]
    public TimeSpan HoraFim { get; set; } = new TimeSpan(17, 0, 0);

    [Display(Name = "Início do intervalo")]
    public TimeSpan? HoraInicioIntervalo { get; set; }

    [Display(Name = "Fim do intervalo")]
    public TimeSpan? HoraFimIntervalo { get; set; }

    [Range(0, 127, ErrorMessage = "Selecione ao menos um dia da semana.")]
    [Display(Name = "Dias da semana")]
    public int DiasSemana { get; set; } = DiaSemanaFlags.SegundaASexta;
}
