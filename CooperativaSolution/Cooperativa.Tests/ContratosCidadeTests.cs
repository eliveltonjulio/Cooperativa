using Cooperativa.Models;
using Cooperativa.Web.Controllers;
using Cooperativa.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cooperativa.Tests;

/// <summary>
/// Regra da inclusão/edição de contrato: a cidade deve ser escolhida na lista de
/// municípios da UF selecionada pelo usuário.
/// </summary>
public class ContratosCidadeTests
{
    private static MunicipiosService NovoServicoMunicipios()
        => new(MunicipiosService.LocalizarArquivoPadrao());

    private static Contrato NovoContrato(string cidade, string uf) => new()
    {
        EmpresaNome = "Empresa Alpha",
        Cnpj = "12.345.678/0001-99",
        DataInicio = new DateTime(2026, 1, 1),
        ValorContrato = 1000m,
        Logradouro = "Rua das Flores, 123",
        Bairro = "Centro",
        Cidade = cidade,
        Uf = uf,
        Cep = "30110-000"
    };

    [Fact]
    public void Cidades_RetornaMunicipiosDaUfSolicitada()
    {
        var controller = ApoioTestes.PrepararController(
            new ContratosController(ApoioTestes.NovoContexto(), NovoServicoMunicipios()));

        var resultado = Assert.IsType<JsonResult>(controller.Cidades("MG"));
        var cidades = Assert.IsAssignableFrom<IReadOnlyList<string>>(resultado.Value);

        Assert.Contains("Belo Horizonte", cidades);
        Assert.DoesNotContain("São Paulo", cidades);
    }

    [Fact]
    public async Task Create_CidadeNaoPertenceAUf_NaoDeveSalvar()
    {
        await using var context = ApoioTestes.NovoContexto();
        var controller = ApoioTestes.PrepararController(
            new ContratosController(context, NovoServicoMunicipios()));

        var resultado = await controller.Create(NovoContrato(cidade: "Rio de Janeiro", uf: "SP"));

        Assert.IsType<ViewResult>(resultado);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(
            controller.ModelState[nameof(Contrato.Cidade)]!.Errors,
            erro => erro.ErrorMessage.Contains("lista", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, await context.Contratos.CountAsync());
    }

    [Fact]
    public async Task Create_CidadePertenceAUf_DeveSalvar()
    {
        await using var context = ApoioTestes.NovoContexto();
        var controller = ApoioTestes.PrepararController(
            new ContratosController(context, NovoServicoMunicipios()));

        var resultado = await controller.Create(NovoContrato(cidade: "São Paulo", uf: "SP"));

        Assert.IsType<RedirectToActionResult>(resultado);
        Assert.True(controller.ModelState.IsValid);
        Assert.Equal(1, await context.Contratos.CountAsync());
    }

    [Fact]
    public async Task Edit_CidadeLegadaForaDaBase_MantidaSemAlteracao_DeveSalvar()
    {
        await using var context = ApoioTestes.NovoContexto();
        var controller = ApoioTestes.PrepararController(
            new ContratosController(context, NovoServicoMunicipios()));

        // Contrato antigo com cidade fora da base atual do IBGE.
        var contrato = NovoContrato(cidade: "Cidade Legada", uf: "SP");
        context.Contratos.Add(contrato);
        await context.SaveChangesAsync();

        var model = NovoContrato(cidade: "Cidade Legada", uf: "SP");
        model.ValorContrato = 2000m;
        var resultado = await controller.Edit(contrato.Id, model);

        Assert.IsType<RedirectToActionResult>(resultado);
        Assert.True(controller.ModelState.IsValid);

        var atualizado = await context.Contratos.SingleAsync(c => c.Id == contrato.Id);
        Assert.Equal(2000m, atualizado.ValorContrato);
    }

    [Fact]
    public async Task Edit_TrocaDeUfManterCidadeDeOutraUf_NaoDeveSalvar()
    {
        await using var context = ApoioTestes.NovoContexto();
        var controller = ApoioTestes.PrepararController(
            new ContratosController(context, NovoServicoMunicipios()));

        var contrato = NovoContrato(cidade: "Belo Horizonte", uf: "MG");
        context.Contratos.Add(contrato);
        await context.SaveChangesAsync();

        // Usuário troca a UF mas mantém a cidade anterior (de outra UF).
        var model = NovoContrato(cidade: "Belo Horizonte", uf: "SP");
        var resultado = await controller.Edit(contrato.Id, model);

        Assert.IsType<ViewResult>(resultado);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal("Belo Horizonte", (await context.Contratos.SingleAsync(c => c.Id == contrato.Id)).Cidade);
        Assert.Equal("MG", (await context.Contratos.SingleAsync(c => c.Id == contrato.Id)).Uf);
    }
}
