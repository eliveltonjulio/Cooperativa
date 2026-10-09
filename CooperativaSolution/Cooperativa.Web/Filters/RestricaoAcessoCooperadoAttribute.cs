using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Cooperativa.Web.Filters;

/// <summary>
/// Restringe o perfil "Cooperado" (sem papel administrativo) ao acesso somente a:
/// a própria página de cadastro (Cooperados/Details com o id igual ao claim CooperadoId),
/// ao próprio ponto (Ponto/Index e Ponto/Details — a posse do registro é conferida na action),
/// à página principal (Home/Index) e às ações pessoais de conta (Auth/AlterarSenha e Auth/Logout).
/// Qualquer outra página é negada com redirecionamento amigável.
/// Registrado globalmente no Program.cs, logo após o AuthorizeFilter.
/// </summary>
public sealed class RestricaoAcessoCooperadoAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Ações com [AllowAnonymous] (login, MFA, recuperação de senha, página de erro)
        // ficam isentas — igual ao comportamento do AuthorizeFilter.
        if (EhAllowAnonymous(context))
        {
            await next();
            return;
        }

        var usuario = context.HttpContext.User;
        if (usuario.Identity?.IsAuthenticated != true || !EhCooperadoRestrito(usuario))
        {
            await next();
            return;
        }

        var valores = context.RouteData.Values;
        var controller = valores["controller"]?.ToString();
        var action = valores["action"]?.ToString();

        if (Permitido(controller, action, valores, usuario))
        {
            await next();
            return;
        }

        // Curto-circuito: a action restrita nunca é executada.
        context.Result = Negar(context, controller, action, usuario);
    }

    /// <summary>Perfil cooperado puro: Cooperado/Operacional sem nenhum papel administrativo.</summary>
    internal static bool EhCooperadoRestrito(ClaimsPrincipal usuario) =>
        (usuario.IsInRole("Cooperado") || usuario.IsInRole("Operacional"))
        && !usuario.IsInRole("Administrador")
        && !usuario.IsInRole("Admin")
        && !usuario.IsInRole("Coordenador")
        && !usuario.IsInRole("Gestor");

    private static bool EhAllowAnonymous(FilterContext context) =>
        context.Filters.Any(f => f is IAllowAnonymous)
        || context.ActionDescriptor.EndpointMetadata?.OfType<IAllowAnonymous>().Any() == true
        || context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null;

    private static bool Permitido(
        string? controller,
        string? action,
        RouteValueDictionary valores,
        ClaimsPrincipal usuario)
    {
        if (string.IsNullOrWhiteSpace(controller) || string.IsNullOrWhiteSpace(action))
        {
            return false;
        }

        // Hub de navegação: os cartões já exibem apenas páginas permitidas ao cooperado.
        if (controller.Equals("Home", StringComparison.OrdinalIgnoreCase))
        {
            return action.Equals("Index", StringComparison.OrdinalIgnoreCase);
        }

        // Conta pessoal: alterar a própria senha e encerrar a sessão.
        if (controller.Equals("Auth", StringComparison.OrdinalIgnoreCase))
        {
            return action.Equals("AlterarSenha", StringComparison.OrdinalIgnoreCase)
                || action.Equals("Logout", StringComparison.OrdinalIgnoreCase);
        }

        // Próprio cadastro — id da rota precisa ser igual ao claim CooperadoId.
        if (controller.Equals("Cooperados", StringComparison.OrdinalIgnoreCase))
        {
            return action.Equals("Details", StringComparison.OrdinalIgnoreCase)
                && MesmoCooperado(valores, usuario);
        }

        // Próprio ponto: Index já filtra pelo claim; Details confere a posse do registro na action.
        if (controller.Equals("Ponto", StringComparison.OrdinalIgnoreCase))
        {
            return action.Equals("Index", StringComparison.OrdinalIgnoreCase)
                || action.Equals("Details", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool MesmoCooperado(RouteValueDictionary valores, ClaimsPrincipal usuario)
    {
        var cooperadoIdClaim = usuario.FindFirst("CooperadoId")?.Value;
        return Guid.TryParse(cooperadoIdClaim, out var meuCooperadoId)
            && valores.TryGetValue("id", out var idRota)
            && Guid.TryParse(idRota?.ToString(), out var idDaRota)
            && idDaRota == meuCooperadoId;
    }

    private static IActionResult Negar(
        ActionExecutingContext context,
        string? controller,
        string? action,
        ClaimsPrincipal usuario)
    {
        var mensagem = "Seu perfil de acesso é restrito ao próprio cadastro e ao próprio ponto.";
        var alvoController = "Home";
        var alvoAction = "Index";
        object? alvoRota = null;

        // Tentou abrir um cadastro de outro cooperado: leva ao próprio cadastro.
        if (controller?.Equals("Cooperados", StringComparison.OrdinalIgnoreCase) == true
            && action?.Equals("Details", StringComparison.OrdinalIgnoreCase) == true
            && Guid.TryParse(usuario.FindFirst("CooperadoId")?.Value, out var meuCooperadoId))
        {
            mensagem = "Você só pode visualizar o seu próprio cadastro.";
            alvoController = "Cooperados";
            alvoAction = "Details";
            alvoRota = new { id = meuCooperadoId };
        }

        if (context.Controller is Controller controllerAtual)
        {
            // Em runtime o TempData pode ainda não ter sido criado (filtro interno do MVC);
            // cria pelo factory para que a mensagem sobreviva ao redirect.
            controllerAtual.TempData ??= context.HttpContext.RequestServices
                .GetRequiredService<ITempDataDictionaryFactory>()
                .GetTempData(context.HttpContext);
            controllerAtual.TempData["MensagemErro"] = mensagem;
        }

        return new RedirectToActionResult(alvoAction, alvoController, alvoRota);
    }
}
