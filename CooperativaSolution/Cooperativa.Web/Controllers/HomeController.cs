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
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}