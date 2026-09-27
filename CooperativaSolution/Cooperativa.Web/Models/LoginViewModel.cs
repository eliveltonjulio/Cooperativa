using System.ComponentModel.DataAnnotations;

namespace Cooperativa.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Informe o e-mail ou nome de usuário.")]
    [Display(Name = "E-mail ou Usuário")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Senha { get; set; } = string.Empty;
}
