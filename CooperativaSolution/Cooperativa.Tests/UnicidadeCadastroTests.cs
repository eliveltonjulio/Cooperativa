using Cooperativa.Data;
using Cooperativa.Models;
using Cooperativa.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cooperativa.Tests;

/// <summary>
/// Regras de unicidade do cadastro: cada função tem um nome único e cada contrato tem,
/// no máximo, uma remuneração por função (ex.: o contrato 99999 não pode ter duas
/// remunerações para a função Analista).
/// </summary>
public class UnicidadeCadastroTests
{
    private const string MensagemFuncaoRepetida = "Já existe uma função cadastrada com este nome.";
    private const string MensagemRemuneracaoRepetida = "Já existe uma remuneração cadastrada para este contrato e função.";

    #region Funções - nome único

    [Fact]
    public async Task Funcoes_Create_NaoDeveCadastrarNomeRepetido()
    {
        await using var context = NovoContexto();
        await CriarFuncaoAsync(context, "Analista");

        var controller = PrepararController(new FuncoesController(context));
        var resultado = await controller.Create(new Funcao
        {
            Nome = "  analista  ", // maiúsculas/minúsculas e espaços não criam uma nova função
            Descricao = "Outra descrição para o mesmo nome."
        });

        Assert.IsType<ViewResult>(resultado);
        AssertComErro(controller.ModelState, MensagemFuncaoRepetida);
        Assert.Equal(1, await context.Funcoes.CountAsync());
    }

    [Fact]
    public async Task Funcoes_Create_DevePermitirNomeDiferente()
    {
        await using var context = NovoContexto();
        await CriarFuncaoAsync(context, "Analista");

        var controller = PrepararController(new FuncoesController(context));
        var resultado = await controller.Create(new Funcao
        {
            Nome = "Técnico",
            Descricao = "Executa manutenção técnica."
        });

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(2, await context.Funcoes.CountAsync());
    }

    [Fact]
    public async Task Funcoes_Edit_NaoDeveUsarNomeDeOutraFuncao()
    {
        await using var context = NovoContexto();
        await CriarFuncaoAsync(context, "Analista");
        var tecnico = await CriarFuncaoAsync(context, "Técnico");

        var controller = PrepararController(new FuncoesController(context));
        var resultado = await controller.Edit(tecnico.Id, new Funcao
        {
            Id = tecnico.Id,
            Nome = "ANALISTA",
            Descricao = tecnico.Descricao
        });

        Assert.IsType<ViewResult>(resultado);
        AssertComErro(controller.ModelState, MensagemFuncaoRepetida);
        Assert.Equal("Técnico", (await context.Funcoes.FindAsync(tecnico.Id))!.Nome);
    }

    [Fact]
    public async Task Funcoes_Edit_DevePermitirManterONomeProprio()
    {
        await using var context = NovoContexto();
        await CriarFuncaoAsync(context, "Analista");
        var tecnico = await CriarFuncaoAsync(context, "Técnico");

        var controller = PrepararController(new FuncoesController(context));
        var resultado = await controller.Edit(tecnico.Id, new Funcao
        {
            Id = tecnico.Id,
            Nome = "Técnico",
            Descricao = "Descrição atualizada.",
            Cbo = "2124-05"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redirect.ActionName);

        var atualizada = await context.Funcoes.FindAsync(tecnico.Id);
        Assert.Equal("Técnico", atualizada!.Nome);
        Assert.Equal("Descrição atualizada.", atualizada.Descricao);
    }

    #endregion

    #region Remunerações - contrato/função único

    [Fact]
    public async Task Remuneracoes_Create_NaoDeveDuplicarContratoEFuncao()
    {
        await using var context = NovoContexto();
        var contrato = await CriarContratoAsync(context, "Empresa 99999");
        var analista = await CriarFuncaoAsync(context, "Analista");
        await CriarRemuneracaoAsync(context, contrato, analista);

        var controller = PrepararController(new RemuneracoesController(context));
        var resultado = await controller.Create(new Remuneracao
        {
            ContratoId = contrato.Id,
            FuncaoId = analista.Id,
            ValorTexto = "1.800,00",
            DataInicio = new DateTime(2026, 3, 1),
            TipoRemuneracao = "DIA"
        });

        Assert.IsType<ViewResult>(resultado);
        AssertComErro(controller.ModelState, MensagemRemuneracaoRepetida);
        Assert.Equal(1, await context.Remuneracoes.CountAsync());
    }

    [Fact]
    public async Task Remuneracoes_Create_DevePermitirOutraFuncaoNoMesmoContrato()
    {
        await using var context = NovoContexto();
        var contrato = await CriarContratoAsync(context, "Empresa 99999");
        var analista = await CriarFuncaoAsync(context, "Analista");
        var tecnico = await CriarFuncaoAsync(context, "Técnico");
        await CriarRemuneracaoAsync(context, contrato, analista);

        var controller = PrepararController(new RemuneracoesController(context));
        var resultado = await controller.Create(new Remuneracao
        {
            ContratoId = contrato.Id,
            FuncaoId = tecnico.Id,
            ValorTexto = "1.500,00",
            DataInicio = new DateTime(2026, 3, 1),
            TipoRemuneracao = "DIA"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(2, await context.Remuneracoes.CountAsync());
    }

    [Fact]
    public async Task Remuneracoes_Create_DevePermitirMesmaFuncaoEmOutroContrato()
    {
        await using var context = NovoContexto();
        var contrato99999 = await CriarContratoAsync(context, "Empresa 99999");
        var outroContrato = await CriarContratoAsync(context, "Empresa 88888");
        var analista = await CriarFuncaoAsync(context, "Analista");
        await CriarRemuneracaoAsync(context, contrato99999, analista);

        var controller = PrepararController(new RemuneracoesController(context));
        var resultado = await controller.Create(new Remuneracao
        {
            ContratoId = outroContrato.Id,
            FuncaoId = analista.Id,
            ValorTexto = "1.200,00",
            DataInicio = new DateTime(2026, 3, 1),
            TipoRemuneracao = "HORA"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(2, await context.Remuneracoes.CountAsync());
    }

    [Fact]
    public async Task Remuneracoes_Edit_NaoDeveDuplicarContratoEFuncao()
    {
        await using var context = NovoContexto();
        var contrato = await CriarContratoAsync(context, "Empresa 99999");
        var analista = await CriarFuncaoAsync(context, "Analista");
        var tecnico = await CriarFuncaoAsync(context, "Técnico");
        await CriarRemuneracaoAsync(context, contrato, analista);
        var remuneracaoDoTecnico = await CriarRemuneracaoAsync(context, contrato, tecnico);

        var controller = PrepararController(new RemuneracoesController(context));
        var resultado = await controller.Edit(remuneracaoDoTecnico.Id, new Remuneracao
        {
            Id = remuneracaoDoTecnico.Id,
            ContratoId = contrato.Id,
            FuncaoId = analista.Id, // mesma função da remuneração já existente neste contrato
            ValorTexto = "2.000,00",
            DataInicio = remuneracaoDoTecnico.DataInicio,
            TipoRemuneracao = "DIA"
        });

        Assert.IsType<ViewResult>(resultado);
        AssertComErro(controller.ModelState, MensagemRemuneracaoRepetida);

        var naoAlterada = await context.Remuneracoes.FindAsync(remuneracaoDoTecnico.Id);
        Assert.Equal(tecnico.Id, naoAlterada!.FuncaoId);
        Assert.Equal(remuneracaoDoTecnico.Valor, naoAlterada.Valor);
    }

    [Fact]
    public async Task Remuneracoes_Edit_DevePermitirManterOContratoFuncaoProprio()
    {
        await using var context = NovoContexto();
        var contrato = await CriarContratoAsync(context, "Empresa 99999");
        var analista = await CriarFuncaoAsync(context, "Analista");
        var remuneracao = await CriarRemuneracaoAsync(context, contrato, analista);

        var controller = PrepararController(new RemuneracoesController(context));
        var resultado = await controller.Edit(remuneracao.Id, new Remuneracao
        {
            Id = remuneracao.Id,
            ContratoId = contrato.Id,
            FuncaoId = analista.Id,
            ValorTexto = "2.500,00",
            DataInicio = remuneracao.DataInicio,
            TipoRemuneracao = "DIA"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redirect.ActionName);

        var atualizada = await context.Remuneracoes.FindAsync(remuneracao.Id);
        Assert.Equal(2500m, atualizada!.Valor);
    }

    #endregion

    #region Apoio

    private static CooperativaDbContext NovoContexto() => ApoioTestes.NovoContexto();

    private static T PrepararController<T>(T controller) where T : Controller =>
        ApoioTestes.PrepararController(controller);

    private static void AssertComErro(ModelStateDictionary estado, string mensagem)
    {
        Assert.False(estado.IsValid);
        Assert.Contains(estado.Values.SelectMany(v => v.Errors), e => e.ErrorMessage == mensagem);
    }

    private static Task<Funcao> CriarFuncaoAsync(CooperativaDbContext context, string nome) =>
        ApoioTestes.CriarFuncaoAsync(context, nome);

    private static Task<Contrato> CriarContratoAsync(CooperativaDbContext context, string empresaNome) =>
        ApoioTestes.CriarContratoAsync(context, empresaNome);

    private static Task<Remuneracao> CriarRemuneracaoAsync(
        CooperativaDbContext context,
        Contrato contrato,
        Funcao funcao,
        decimal valor = 1000m) =>
        ApoioTestes.CriarRemuneracaoAsync(context, contrato, funcao, valor);

    #endregion
}