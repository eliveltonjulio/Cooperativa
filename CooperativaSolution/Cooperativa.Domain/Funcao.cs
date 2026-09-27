using System.ComponentModel.DataAnnotations;

namespace Cooperativa.Models;

public class Funcao
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(120, ErrorMessage = "O nome deve ter até 120 caracteres.")]
    [Display(Name = "Nome")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [StringLength(120, ErrorMessage = "A descrição deve ter até 120 caracteres.")]
    [Display(Name = "Descrição")]
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// CBO - Classificação Brasileira de Ocupações, no formato 0000-00 (ex.: 4110-10).
    /// Campo opcional: validado apenas quando informado.
    /// </summary>
    [StringLength(10, ErrorMessage = "O CBO deve ter até 10 caracteres.")]
    [RegularExpression(@"^\d{4}-?\d{2}$", ErrorMessage = "Informe o CBO no formato 0000-00 (6 dígitos, ex.: 4110-10).")]
    [Display(Name = "CBO (Classificação Brasileira de Ocupações)")]
    public string Cbo { get; set; } = string.Empty;

    public ICollection<Alocacao> Alocacoes { get; set; } = new List<Alocacao>();
}
