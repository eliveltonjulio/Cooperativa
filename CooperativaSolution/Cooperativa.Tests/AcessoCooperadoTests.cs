using Cooperativa.Web.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using Xunit;

namespace Cooperativa.Tests;

/// <summary>
/// Restrição de acesso do perfil Cooperado: consulta apenas o próprio cadastro e o
/// próprio ponto; a página principal (hub) e as ações de conta (alterar senha/sair)
/// ficam liberadas; qualquer outra página é negada com redirecionamento amigável.
/// </summary>
public class AcessoCooperadoTests
{
    private const string MensagemRestrita = "Seu perfil de acesso é restrito ao próprio cadastro e ao próprio ponto.";
    private const string MensagemCadastroAlheio = "Você só pode visualizar o seu próprio cadastro.";
    private static readonly Guid MeuCooperadoId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

    #region Permitido ao cooperado

    [Fact]
    public async Task Cooperado_PodeAcessarPaginaPrincipal()
    {
        var resultado = await ExecutarAsync("Home", "Index", Cooperado());
        AssertPermitido(resultado);
    }

    [Fact]
    public async Task Cooperado_PodeAlterarPropriaSenha()
    {
        var resultado = await ExecutarAsync("Auth", "AlterarSenha", Cooperado());
        AssertPermitido(resultado);
    }

    [Fact]
    public async Task Cooperado_PodeEncerrarSessao()
    {
        var resultado = await ExecutarAsync("Auth", "Logout", Cooperado());
        AssertPermitido(resultado);
    }

    [Fact]
    public async Task Cooperado_PodeConsultarProprioCadastro()
    {
        var resultado = await ExecutarAsync(
            "Cooperados", "Details", Cooperado(MeuCooperadoId), MeuCooperadoId.ToString());
        AssertPermitido(resultado);
    }

    [Fact]
    public async Task Cooperado_PodeConsultarSeuPonto()
    {
        var resultado = await ExecutarAsync("Ponto", "Index", Cooperado(MeuCooperadoId));
        AssertPermitido(resultado);
    }

    [Fact]
    public async Task Cooperado_PodeAbrirUmRegistroDePonto()
    {
        // A posse do registro (ser o ponto do próprio cooperado) é conferida na action.
        var resultado = await ExecutarAsync(
            "Ponto", "Details", Cooperado(MeuCooperadoId), Guid.NewGuid().ToString());
        AssertPermitido(resultado);
    }

    [Fact]
    public async Task Cooperado_PodeAcessarAcaoComAllowAnonymous()
    {
        var resultado = await ExecutarAsync("Auth", "Login", Cooperado(), allowAnonymous: true);
        AssertPermitido(resultado);
    }

    #endregion

    #region Negado ao cooperado

    [Fact]
    public async Task Cooperado_NaoPodeListarCooperados()
    {
        var resultado = await ExecutarAsync("Cooperados", "Index", Cooperado(MeuCooperadoId));
        var redirect = AssertNegado(resultado);
        Assert.Equal("Home", redirect.ControllerName);
        Assert.Equal(MensagemRestrita, resultado.Controller.TempData["MensagemErro"]);
    }

    [Fact]
    public async Task Cooperado_NaoPodeConsultarCadastroDeOutroCooperado()
    {
        var outroId = Guid.NewGuid();
        var resultado = await ExecutarAsync(
            "Cooperados", "Details", Cooperado(MeuCooperadoId), outroId.ToString());

        var redirect = AssertNegado(resultado);
        Assert.Equal("Cooperados", redirect.ControllerName);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal(MeuCooperadoId.ToString(), redirect.RouteValues!["id"]?.ToString());
        Assert.Equal(MensagemCadastroAlheio, resultado.Controller.TempData["MensagemErro"]);
    }

    [Fact]
    public async Task CooperadoSemClaim_NaoPodeAbrirNenhumCadastro()
    {
        var resultado = await ExecutarAsync(
            "Cooperados", "Details", Cooperado(), Guid.NewGuid().ToString());

        var redirect = AssertNegado(resultado);
        Assert.Equal("Home", redirect.ControllerName);
        Assert.Equal(MensagemRestrita, resultado.Controller.TempData["MensagemErro"]);
    }

    [Fact]
    public async Task Cooperado_NaoPodeAcessarContratos()
    {
        var resultado = await ExecutarAsync("Contratos", "Index", Cooperado(MeuCooperadoId));
        AssertNegado(resultado);
        Assert.Equal(MensagemRestrita, resultado.Controller.TempData["MensagemErro"]);
    }

    [Fact]
    public async Task Cooperado_NaoPodeCadastrarPonto()
    {
        var resultado = await ExecutarAsync("Ponto", "Create", Cooperado(MeuCooperadoId));
        AssertNegado(resultado);
    }

    [Fact]
    public async Task Cooperado_NaoPodeAcessarGestao()
    {
        var resultado = await ExecutarAsync("Home", "Gestao", Cooperado(MeuCooperadoId));
        AssertNegado(resultado);
    }

    [Fact]
    public async Task Cooperado_NaoPodeAcessarFolha()
    {
        var resultado = await ExecutarAsync("Folha", "Index", Cooperado(MeuCooperadoId));
        AssertNegado(resultado);
    }

    [Fact]
    public async Task Cooperado_NaoPodeAcessarUsuarios()
    {
        var resultado = await ExecutarAsync("Usuarios", "Index", Cooperado(MeuCooperadoId));
        AssertNegado(resultado);
    }

    [Fact]
    public async Task Cooperado_NaoPodeAcessarApuracoesDePonto()
    {
        var resultado = await ExecutarAsync("ApuracoesPontoMensais", "Index", Cooperado(MeuCooperadoId));
        AssertNegado(resultado);
    }

    #endregion

    #region Outros perfis

    [Fact]
    public async Task Administrador_AcessaQualquerPagina()
    {
        var resultado = await ExecutarAsync("Cooperados", "Index", Admin());
        AssertPermitido(resultado);

        resultado = await ExecutarAsync("Folha", "Index", Admin());
        AssertPermitido(resultado);
    }

    [Fact]
    public async Task Anonimo_NaoEInterceptadoPeloFiltro()
    {
        // Exigir login é papel do AuthorizeFilter global; este filtro age apenas
        // sobre sessões autenticadas com perfil cooperado.
        var resultado = await ExecutarAsync("Cooperados", "Index", Anonimo());
        AssertPermitido(resultado);
    }

    [Fact]
    public async Task PerfilOperacional_TambemERestritoComoCooperado()
    {
        var operacional = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Role, "Operacional") }, "Teste"));

        var resultado = await ExecutarAsync("Contratos", "Index", operacional);
        AssertNegado(resultado);
    }

    #endregion

    #region Apoio

    private sealed class ControladorFalso : Controller { }

    private static void AssertPermitido((bool Executou, IActionResult? Resultado, Controller Controller) resultado)
    {
        Assert.True(resultado.Executou, "A action deveria ter sido executada (acesso permitido).");
        Assert.Null(resultado.Resultado);
    }

    private static RedirectToActionResult AssertNegado((bool Executou, IActionResult? Resultado, Controller Controller) resultado)
    {
        Assert.False(resultado.Executou, "A action restrita não deveria ter sido executada.");
        return Assert.IsType<RedirectToActionResult>(resultado.Resultado);
    }

    private static ClaimsPrincipal Cooperado(Guid? cooperadoId = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, "Cooperado") };
        if (cooperadoId.HasValue)
        {
            claims.Add(new Claim("CooperadoId", cooperadoId.Value.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Teste"));
    }

    private static ClaimsPrincipal Admin() =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Administrador") }, "Teste"));

    private static ClaimsPrincipal Anonimo() => new(new ClaimsIdentity());

    private static async Task<(bool Executou, IActionResult? Resultado, Controller Controller)> ExecutarAsync(
        string controllerRota,
        string acao,
        ClaimsPrincipal usuario,
        string? idRota = null,
        bool allowAnonymous = false)
    {
        var controller = new ControladorFalso();
        ApoioTestes.PrepararController(controller);
        controller.ControllerContext.HttpContext.User = usuario;

        var routeData = new RouteData();
        routeData.Values["controller"] = controllerRota;
        routeData.Values["action"] = acao;
        if (idRota != null)
        {
            routeData.Values["id"] = idRota;
        }

        var actionContext = new ActionExecutingContext(
            new ActionContext(controller.ControllerContext.HttpContext, routeData, new ActionDescriptor()),
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller);

        if (allowAnonymous)
        {
            // [AllowAnonymous] chega aos filtros como metadata do endpoint (não como IFilterMetadata).
            actionContext.ActionDescriptor.EndpointMetadata = new List<object> { new AllowAnonymousAttribute() };
        }

        var chegouNaAction = false;
        await new RestricaoAcessoCooperadoAttribute().OnActionExecutionAsync(actionContext, () =>
        {
            chegouNaAction = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), controller));
        });

        return (chegouNaAction, actionContext.Result, controller);
    }

    #endregion
}
