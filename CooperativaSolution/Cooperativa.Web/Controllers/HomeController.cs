using System.Diagnostics;
using Cooperativa.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Cooperativa.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IConfiguration _configuracao;

    public HomeController(ILogger<HomeController> logger, IConfiguration configuracao)
    {
        _logger = logger;
        _configuracao = configuracao;
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

        // Descreve a situação da connection string para exibir na página E nos
        // logs — assim qualquer trecho de log colado em suporte já traz o motivo.
        var motivo = falhaBanco ? DescreverSituacaoConnectionStrings(_configuracao) : null;

        // Registro auxiliar para diagnóstico nos logs da plataforma.
        _logger.LogWarning(
            "Página /Home/Error exibida (exceção disponível: {TemExcecao}; tipo: {Tipo}; falha de banco: {FalhaBanco}). {Situacao}",
            excecao is not null,
            excecao?.GetType().FullName ?? "-",
            falhaBanco,
            motivo ?? "-");

        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            FalhaBanco = falhaBanco,
            MotivoConnectionStrings = motivo
        });
    }

    // Describe, sem expor host/senha, qual é a situação da variável
    // ConnectionStrings__DefaultConnection — exibida na página de erro para
    // dispensar a consulta manual aos logs na etapa de diagnóstico.
    private static string DescreverSituacaoConnectionStrings(IConfiguration configuracao)
    {
        var efetiva = configuracao.GetConnectionString("DefaultConnection") ?? string.Empty;
        var efetivaLocal = efetiva.Contains("Host=localhost", StringComparison.OrdinalIgnoreCase);

        // Com a connection string efetiva externa, o problema não é de configuração.
        if (!efetivaLocal)
        {
            return "Situação: a connection string está configurada com um servidor externo — a falha é de acesso a ele (rede, credenciais ou SSL). Confira o host efetivo e o stack trace nos logs.";
        }

        var valorEnv = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings:DefaultConnection");

        if (valorEnv is null)
        {
            return "Situação: a variável ConnectionStrings__DefaultConnection NÃO foi encontrada neste processo; o sistema está usando o padrão do appsettings.json (Host=localhost).";
        }

        string? hostDaVariavel = null;
        try
        {
            hostDaVariavel = new NpgsqlConnectionStringBuilder(valorEnv).Host;
        }
        catch
        {
            return "Situação: a variável ConnectionStrings__DefaultConnection existe, mas o valor não pôde ser interpretado como connection string.";
        }

        if (hostDaVariavel is not null && hostDaVariavel.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return "Situação: a variável ConnectionStrings__DefaultConnection existe, mas o VALOR contém Host=localhost — corrija o valor.";
        }

        return "Situação: a variável ConnectionStrings__DefaultConnection existe com um servidor externo, mas NÃO foi utilizada neste processo — verifique se está marcada para o ambiente Production e faça um novo deploy após salvar.";
    }
}