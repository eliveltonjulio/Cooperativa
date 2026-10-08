using System.ComponentModel.DataAnnotations;

namespace Cooperativa.Web.Models;

public class MfaViewModel
{
    [Required]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o código MFA.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "O código deve conter 6 dígitos.")]
    [Display(Name = "Código de verificação (6 dígitos)")]
    public string Codigo { get; set; } = string.Empty;

    public string? EmailMascarado { get; set; }
}

public class EsqueciSenhaViewModel
{
    [Required(ErrorMessage = "Informe seu login ou e-mail cadastrado.")]
    [Display(Name = "Login ou E-mail")]
    public string LoginOuEmail { get; set; } = string.Empty;
}

public class RedefinirSenhaViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a nova senha.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nova senha")]
    public string NovaSenha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a nova senha.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não conferem.")]
    [Display(Name = "Confirmar nova senha")]
    public string ConfirmarNovaSenha { get; set; } = string.Empty;
}

public class AlterarSenhaViewModel
{
    [Required(ErrorMessage = "Informe a senha atual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha atual")]
    public string SenhaAtual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a nova senha.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nova senha")]
    public string NovaSenha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a nova senha.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não conferem.")]
    [Display(Name = "Confirmar nova senha")]
    public string ConfirmarNovaSenha { get; set; } = string.Empty;
}
