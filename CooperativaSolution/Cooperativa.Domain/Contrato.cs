using System.ComponentModel.DataAnnotations;

namespace Cooperativa.Models;

public class Contrato
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "O nome da empresa é obrigatório.")]
    [StringLength(120, ErrorMessage = "O nome da empresa deve ter até 120 caracteres.")]
    [Display(Name = "Nome da empresa")]
    public string EmpresaNome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O CNPJ é obrigatório.")]
    [StringLength(18, ErrorMessage = "O CNPJ deve ter até 18 caracteres.")]
    [RegularExpression(@"^[A-Za-z0-9]{2}\.[A-Za-z0-9]{3}\.[A-Za-z0-9]{3}/[A-Za-z0-9]{4}-\d{2}$",
        ErrorMessage = "Informe o CNPJ no formato 00.000.000/0000-00. Os 12 primeiros caracteres aceitam letras e números; os 2 dígitos verificadores devem ser números.")]
    [Display(Name = "CNPJ")]
    public string Cnpj { get; set; } = string.Empty;

    [Required(ErrorMessage = "A data de início é obrigatória.")]
    [Display(Name = "Data de início")]
    [DataType(DataType.Date)]
    public DateTime DataInicio { get; set; }

    /// <summary>Data final da vigência do contrato. Opcional: sem data fim o contrato fica em aberto.</summary>
    [Display(Name = "Data de fim (opcional)")]
    [DataType(DataType.Date)]
    public DateTime? DataFim { get; set; }

    [Range(0, 100000000, ErrorMessage = "O valor do contrato deve ser positivo.")]
    [Display(Name = "Valor do contrato")]
    public decimal ValorContrato { get; set; }

    [Required(ErrorMessage = "O logradouro é obrigatório.")]
    [StringLength(200, ErrorMessage = "O logradouro deve ter até 200 caracteres.")]
    [Display(Name = "Logradouro")]
    public string Logradouro { get; set; } = string.Empty;

    [Required(ErrorMessage = "O bairro é obrigatório.")]
    [StringLength(80, ErrorMessage = "O bairro deve ter até 80 caracteres.")]
    [Display(Name = "Bairro")]
    public string Bairro { get; set; } = string.Empty;

    [Required(ErrorMessage = "A cidade é obrigatória.")]
    [StringLength(80, ErrorMessage = "A cidade deve ter até 80 caracteres.")]
    [Display(Name = "Cidade")]
    public string Cidade { get; set; } = string.Empty;

    [Required(ErrorMessage = "A UF é obrigatória.")]
    [StringLength(2, ErrorMessage = "A UF deve ter 2 caracteres.")]
    [Display(Name = "UF")]
    public string Uf { get; set; } = string.Empty;

    [Required(ErrorMessage = "O CEP é obrigatório.")]
    [StringLength(10, ErrorMessage = "O CEP deve ter até 10 caracteres.")]
    [RegularExpression(@"^\d{2}\.?\d{3}-\d{3}$", ErrorMessage = "Informe o CEP no formato 00000-000.")]
    [Display(Name = "CEP")]
    public string Cep { get; set; } = string.Empty;

    [StringLength(120, ErrorMessage = "O nome do contato 1 deve ter até 120 caracteres.")]
    [Display(Name = "Contato 1 - Nome (opcional)")]
    public string Contato1Nome { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "O telefone do contato 1 deve ter até 20 caracteres.")]
    [RegularExpression(@"^\(\d{2}\) \d{4,5}-\d{4}$", ErrorMessage = "Informe o telefone no formato (00) 00000-0000.")]
    [Display(Name = "Contato 1 - Telefone (opcional)")]
    public string Contato1Telefone { get; set; } = string.Empty;

    [StringLength(120, ErrorMessage = "O nome do contato 2 deve ter até 120 caracteres.")]
    [Display(Name = "Contato 2 - Nome (opcional)")]
    public string Contato2Nome { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "O telefone do contato 2 deve ter até 20 caracteres.")]
    [RegularExpression(@"^\(\d{2}\) \d{4,5}-\d{4}$", ErrorMessage = "Informe o telefone no formato (00) 00000-0000.")]
    [Display(Name = "Contato 2 - Telefone (opcional)")]
    public string Contato2Telefone { get; set; } = string.Empty;

    public ICollection<Alocacao> Alocacoes { get; set; } = new List<Alocacao>();
}
