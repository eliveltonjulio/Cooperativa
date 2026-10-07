using System.Diagnostics;
using Cooperativa.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cooperativa.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Gestao()
    {
        return View();
    }

    public IActionResult Parametrizacao()
    {
        return View();
    }

    // Página exibida pelo UseExceptionHandler("/Home/Error") quando ocorre uma
    // exceção não tratada. O [AllowAnonymous] é necessário porque o filtro global
    // de autorização redirecionaria para o login, mascarando o erro original.
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // Detecta se a exceção foi uma falha de banco (cadeia: InvalidOperationException
        // → NpgsqlException → SocketException) para exibir orientação objetiva na página.
        var excecao = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        var falhaBanco = false;
        for (var atual = excecao; atual is not null; atual = atual.InnerException)
        {
            if (atual is Npgsql.NpgsqlException or System.Net.Sockets.SocketException)
            {
                falhaBanco = true;
                break;
            }
        }

        // Registro auxiliar para diagnóstico nos logs da plataforma.
        _logger.LogWarning(
            "Página /Home/Error exibida (exceção disponível: {TemExcecao}; tipo: {Tipo}; falha de banco: {FalhaBanco}).",
            excecao is not null,
            excecao?.GetType().FullName ?? "-",
            falhaBanco);

        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            FalhaBanco = falhaBanco
        });
    }
}