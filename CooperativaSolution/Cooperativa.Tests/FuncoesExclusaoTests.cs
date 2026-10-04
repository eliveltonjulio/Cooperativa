using Cooperativa.Data;
using Cooperativa.Models;
using Cooperativa.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cooperativa.Tests;

/// <summary>
/// Exclusão de função: a remuneração referencia a função por FK com Restrict,
/// então a exclusão deve ser recusada com mensagem amigável em vez de estourar 500.
/// </summary>
public class FuncoesExclusaoTests
{
    private const string MensagemBloqueio = "Não é possível excluir a função";

    [Fact]
    public async Task Funcoes_Delete_NaoDeveExibirConfirmacaoQuandoHaRemuneracao()
    {
        await using var context = ApoioTestes.NovoContexto();
        var contrato = await ApoioTestes.CriarContratoAsync(context, "Empresa 99999");
        var analista = await ApoioTestes.CriarFuncaoAsync(context, "Analista");
        await ApoioTestes.CriarRemuneracaoAsync(context, contrato, analista);

        var controller = ApoioTestes.PrepararController(new FuncoesController(context));
        var resultado = await controller.Delete(analista.Id);

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Contains(MensagemBloqueio, controller.TempData["MensagemErro"] as string);
        Assert.True(await context.Funcoes.AnyAsync(f => f.Id == analista.Id));
    }

    [Fact]
    public async Task Funcoes_DeleteConfirmed_NaoDeveExcluirFuncaoComRemuneracao()
    {
        await using var context = ApoioTestes.NovoContexto();
        var contrato = await ApoioTestes.CriarContratoAsync(context, "Empresa 99999");
        var analista = await ApoioTestes.CriarFuncaoAsync(context, "Analista");
        await ApoioTestes.CriarRemuneracaoAsync(context, contrato, analista);

        var controller = ApoioTestes.PrepararController(new FuncoesController(context));
        var resultado = await controller.DeleteConfirmed(analista.Id);

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Contains(MensagemBloqueio, controller.TempData["MensagemErro"] as string);

        // Função e remuneração permanecem intactas.
        Assert.True(await context.Funcoes.AnyAsync(f => f.Id == analista.Id));
        Assert.Equal(1, await context.Remuneracoes.CountAsync());
    }

    [Fact]
    public async Task Funcoes_DeleteConfirmed_DeveExcluirFuncaoSemRemuneracao()
    {
        await using var context = ApoioTestes.NovoContexto();
        var tecnico = await ApoioTestes.CriarFuncaoAsync(context, "Técnico");

        var controller = ApoioTestes.PrepararController(new FuncoesController(context));
        var resultado = await controller.DeleteConfirmed(tecnico.Id);

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Função excluída com sucesso.", controller.TempData["MensagemSucesso"] as string);
        Assert.False(await context.Funcoes.AnyAsync(f => f.Id == tecnico.Id));
    }

    [Fact]
    public async Task Funcoes_DeleteConfirmed_NaoDeveExcluirFuncaoExcluida()
    {
        await using var context = ApoioTestes.NovoContexto();
        var controller = ApoioTestes.PrepararController(new FuncoesController(context));

        var resultado = await controller.DeleteConfirmed(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(resultado);
    }
}