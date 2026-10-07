using Cooperativa.Web.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cooperativa.Web.Filters;

/// <summary>
/// Valida o token antiforgery como o [ValidateAntiForgeryToken] padrão, mas em
/// vez de retornar HTTP 400 vazio em falha, devolve a própria view de login com
/// uma mensagem clara. É o cenário típico de contêineres com chaves do Data
/// Protection efêmeras: a cada novo container os cookies antiforgery antigos
/// ficam indecidáveis e o envio do formulário falharia com 400 silencioso.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ValidacaoAntiforgeryAmigavelAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        if (await antiforgery.IsRequestValidAsync(context.HttpContext))
        {
            await next();
            return;
        }

        // Recarrega o formulário com aviso. A renderização emite um novo par de
        // cookie/token (da instância atual), então o novo envio funciona.
        context.ActionArguments.TryGetValue("model", out var argumento);
        var original = argumento as LoginViewModel;

        var controller = (Controller)context.Controller;
        controller.ModelState.AddModelError(
            string.Empty,
            "Sua sessão ficou desatualizada. Recarregue a página e envie novamente — se persistir, limpe os cookies deste site.");

        context.Result = controller.View(
            "Login",
            new LoginViewModel { Login = original?.Login ?? string.Empty });
    }
}
