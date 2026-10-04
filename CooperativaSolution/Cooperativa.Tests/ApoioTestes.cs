using Cooperativa.Data;
using Cooperativa.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cooperativa.Tests;

/// <summary>
/// Fábricas compartilhadas dos testes: contexto EF em memória, preparação de controllers
/// e cadastros básicos (função, contrato e remuneração).
/// </summary>
internal static class ApoioTestes
{
    // TryValidateModel resolve o IObjectValidator a partir de HttpContext.RequestServices;
    // sem este provider o MVC lança NullReferenceException fora do pipeline de requisição.
    private static readonly Lazy<ServiceProvider> ServicosMvc = new(() =>
        new ServiceCollection()
            .AddLogging()
            .AddMvc()
            .Services
            .BuildServiceProvider());

    public static CooperativaDbContext NovoContexto()
    {
        var options = new DbContextOptionsBuilder<CooperativaDbContext>()
            .UseInMemoryDatabase($"cooperativa-{Guid.NewGuid()}")
            .Options;

        return new CooperativaDbContext(options);
    }

    public static T PrepararController<T>(T controller) where T : Controller
    {
        var httpContext = new DefaultHttpContext { RequestServices = ServicosMvc.Value };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
            // O MVC constrói o UrlHelper (usado por RedirectToAction) a partir do RouteData.
            RouteData = new RouteData()
        };
        controller.TempData = new TempDataDictionary(httpContext, new ProvedorTempDataVazio());
        return controller;
    }

    public static async Task<Funcao> CriarFuncaoAsync(CooperativaDbContext context, string nome)
    {
        var funcao = new Funcao
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Descricao = $"Descrição da função {nome}."
        };

        context.Funcoes.Add(funcao);
        await context.SaveChangesAsync();
        return funcao;
    }

    public static async Task<Contrato> CriarContratoAsync(CooperativaDbContext context, string empresaNome)
    {
        var contrato = new Contrato
        {
            Id = Guid.NewGuid(),
            EmpresaNome = empresaNome,
            Cnpj = "12.345.678/0001-99",
            DataInicio = new DateTime(2026, 1, 1),
            ValorContrato = 10000m,
            Logradouro = "Rua das Flores, 123",
            Bairro = "Centro",
            Cidade = "São Paulo",
            Uf = "SP",
            Cep = "01001-000"
        };

        context.Contratos.Add(contrato);
        await context.SaveChangesAsync();
        return contrato;
    }

    public static async Task<Remuneracao> CriarRemuneracaoAsync(
        CooperativaDbContext context,
        Contrato contrato,
        Funcao funcao,
        decimal valor = 1000m)
    {
        var remuneracao = new Remuneracao
        {
            Id = Guid.NewGuid(),
            ContratoId = contrato.Id,
            FuncaoId = funcao.Id,
            Valor = valor,
            DataInicio = new DateTime(2026, 1, 1),
            TipoRemuneracao = "DIA"
        };

        context.Remuneracoes.Add(remuneracao);
        await context.SaveChangesAsync();
        return remuneracao;
    }

    private sealed class ProvedorTempDataVazio : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}