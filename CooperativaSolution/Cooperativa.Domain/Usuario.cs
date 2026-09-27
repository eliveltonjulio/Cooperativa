using System.ComponentModel.DataAnnotations;

namespace Cooperativa.Models;

public class Usuario
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "O nome completo é obrigatório.")]
    [StringLength(150, ErrorMessage = "O nome completo deve ter até 150 caracteres.")]
    [Display(Name = "Nome completo")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(150, ErrorMessage = "O e-mail deve ter até 150 caracteres.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "O telefone celular é obrigatório.")]
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Informe o telefone celular no formato 11988882222 (somente números com DDD).")]
    [Display(Name = "Telefone celular")]
    public string Celular { get; set; } = string.Empty;

    [Required(ErrorMessage = "O login é obrigatório.")]
    [StringLength(50, ErrorMessage = "O login deve ter até 50 caracteres.")]
    [Display(Name = "Nome de usuário")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [StringLength(255, ErrorMessage = "A senha deve ter até 255 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "O perfil é obrigatório.")]
    [StringLength(50, ErrorMessage = "O perfil deve ter até 50 caracteres.")]
    [Display(Name = "Perfil de acesso")]
    public string Perfil { get; set; } = "Cooperado";

    [Display(Name = "Status da conta")]
    public bool Ativo { get; set; } = true;

    [Display(Name = "Cooperado vinculado")]
    public Guid? CooperadoId { get; set; }
    public Cooperado? Cooperado { get; set; }

    [Display(Name = "Contrato vinculado")]
    public Guid? ContratoId { get; set; }
    public Contrato? Contrato { get; set; }

    [Display(Name = "Cargo ou função")]
    [StringLength(100, ErrorMessage = "O cargo deve ter até 100 caracteres.")]
    public string? Cargo { get; set; }

    [Display(Name = "Equipe vinculada")]
    [StringLength(100, ErrorMessage = "A equipe deve ter até 100 caracteres.")]
    public string? Equipe { get; set; }

    [Display(Name = "Data de ingresso")]
    [DataType(DataType.Date)]
    public DateTime? DataIngresso { get; set; }

    [Display(Name = "Último acesso")]
    public DateTime? UltimoAcesso { get; set; }

    public string? MfaCodigoTemp { get; set; }
    public DateTime? MfaCodigoExpiracao { get; set; }

    public string? TokenRedefinicaoSenha { get; set; }
    public DateTime? TokenRedefinicaoExpiracao { get; set; }
}
