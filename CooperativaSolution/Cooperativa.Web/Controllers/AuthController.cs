using Cooperativa.Models;
using Cooperativa.Web.Models;
using Cooperativa.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cooperativa.Web.Controllers;

public class AuthController : Controller
{
    private readonly UsuarioService _usuarioService;

    public AuthController(UsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var usuario = await _usuarioService.Autenticar(model.Login, model.Senha);

        if (usuario == null)
        {
            ModelState.AddModelError(string.Empty, "Login ou senha inválidos, ou conta inativa.");
            return View(model);
        }

        var perfilNormalizado = usuario.Perfil.Trim();
        var ehAdmin = perfilNormalizado.Equals("Administrador", StringComparison.OrdinalIgnoreCase)
                   || perfilNormalizado.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        if (ehAdmin)
        {
            // Exige MFA para administradores
            var codigoMfa = await _usuarioService.GerarMfaAsync(usuario);
            TempData["MfaCodigoSimulado"] = codigoMfa; // Para testes e desenvolvimento
            TempData["MensagemInfo"] = $"Código de verificação MFA enviado para o e-mail {MascararEmail(usuario.Email)}.";

            return RedirectToAction(nameof(VerificarMfa), new { login = usuario.Login });
        }

        await RealizarLoginAsync(usuario);
        TempData["MensagemSucesso"] = $"Bem-vindo, {usuario.Nome}!";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult VerificarMfa(string login)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return RedirectToAction(nameof(Login));
        }

        var model = new MfaViewModel
        {
            Login = login,
            EmailMascarado = TempData["EmailMascarado"] as string
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerificarMfa(MfaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var usuario = await _usuarioService.ValidarMfaAsync(model.Login, model.Codigo);
        if (usuario == null)
        {
            ModelState.AddModelError(nameof(model.Codigo), "Código MFA inválido ou expirado.");
            return View(model);
        }

        await RealizarLoginAsync(usuario);
        TempData["MensagemSucesso"] = $"Autenticação em 2 etapas concluída. Bem-vindo, {usuario.Nome}!";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult EsqueciSenha()
    {
        return View(new EsqueciSenhaViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EsqueciSenha(EsqueciSenhaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var token = await _usuarioService.SolicitarRedefinicaoSenhaAsync(model.LoginOuEmail);
        if (token != null)
        {
            ViewBag.Token = token;
            ViewBag.LinkRedefinicao = Url.Action(nameof(RedefinirSenha), "Auth", new { token }, Request.Scheme);
            TempData["MensagemSucesso"] = "As instruções para redefinição de senha foram geradas com sucesso.";
            return View("EsqueciSenhaConfirmacao");
        }

        ModelState.AddModelError(string.Empty, "Usuário ou e-mail não encontrado no sistema.");
        return View(model);
    }

    [HttpGet]
    public IActionResult RedefinirSenha(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["MensagemErro"] = "Token de redefinição de senha inválido ou ausente.";
            return RedirectToAction(nameof(Login));
        }

        return View(new RedefinirSenhaViewModel { Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RedefinirSenha(RedefinirSenhaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var sucesso = await _usuarioService.RedefinirSenhaAsync(model.Token, model.NovaSenha);
        if (sucesso)
        {
            TempData["MensagemSucesso"] = "Senha alterada com sucesso! Faça login com a nova senha.";
            return RedirectToAction(nameof(Login));
        }

        ModelState.AddModelError(string.Empty, "Token expirado ou inválido. Solicite uma nova redefinição.");
        return View(model);
    }

    [HttpPost]
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["MensagemSucesso"] = "Sessão encerrada com sucesso.";
        return RedirectToAction(nameof(Login));
    }

    private async Task RealizarLoginAsync(Usuario usuario)
    {
        var perfilFormatado = NormalizarPerfil(usuario.Perfil);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Login),
            new(ClaimTypes.Role, perfilFormatado),
            new("NomeCompleto", string.IsNullOrWhiteSpace(usuario.Nome) ? usuario.Login : usuario.Nome),
            new("Email", usuario.Email ?? string.Empty),
            new("PerfilExibicao", perfilFormatado),
            new("CooperadoId", usuario.CooperadoId?.ToString() ?? string.Empty),
            new("ContratoId", usuario.ContratoId?.ToString() ?? string.Empty),
            new("Equipe", usuario.Equipe ?? string.Empty)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
        await _usuarioService.RegistrarUltimoAcesso(usuario.Id);
    }

    private static string NormalizarPerfil(string perfil)
    {
        if (string.IsNullOrWhiteSpace(perfil)) return "Cooperado";
        if (perfil.Equals("Admin", StringComparison.OrdinalIgnoreCase) || perfil.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            return "Administrador";
        if (perfil.Equals("Coordenador", StringComparison.OrdinalIgnoreCase) || perfil.Equals("Gestor", StringComparison.OrdinalIgnoreCase))
            return "Coordenador";
        return "Cooperado";
    }

    private static string MascararEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return "e-mail cadastrado";
        var partes = email.Split('@');
        var usuario = partes[0];
        var dominio = partes[1];
        var usuarioMasc = usuario.Length <= 2 ? usuario + "***" : usuario[..2] + new string('*', usuario.Length - 2);
        return $"{usuarioMasc}@{dominio}";
    }
}
