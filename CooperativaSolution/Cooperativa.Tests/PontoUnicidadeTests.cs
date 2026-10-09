using Cooperativa.Data;
using Cooperativa.Models;
using Cooperativa.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cooperativa.Tests;

/// <summary>
/// Regra de unicidade do ponto: um cooperado não pode ter dois registros de ponto na
/// mesma data — no cadastro (Create), na edição (Edit) e no registro rápido (Registrar).
/// </summary>
public class PontoUnicidadeTests
{
    private const string MensagemPontoRepetido = "Já existe um registro de ponto para este cooperado nesta data.";

    #region Create - um registro por cooperado por data

    [Fact]
    public async Task Ponto_Create_NaoDeveCadastrarDoisRegistrosNaMesmaData()
    {
        await using var context = NovoContexto();
        var contrato = await CriarContratoAsync(context, "Empresa 11111");
        var cooperado = await CriarCooperadoAsync(context, "Maria Souza", "123.456.789-09");
        await CriarAlocacaoAsync(context, cooperado, contrato);
        await CriarRegistroAsync(context, cooperado, new DateTime(2026, 3, 2));

        var controller = PrepararController(new PontoController(context));
        var resultado = await controller.Create(contrato.Id, NovoModel(cooperado.Id, new DateTime(2026, 3, 2)));

        Assert.IsType<ViewResult>(resultado);
        AssertComErro(controller.ModelState, MensagemPontoRepetido);
        Assert.Equal(1, await context.RegistrosPonto.CountAsync());
    }

    [Fact]
    public async Task Ponto_Create_DevePermitirRegistroEmDataDiferente()
    {
        await using var context = NovoContexto();
        var contrato = await CriarContratoAsync(context, "Empresa 22222");
        var cooperado = await CriarCooperadoAsync(context, "João Santos", "123.456.789-08");
        await CriarAlocacaoAsync(context, cooperado, contrato);
        await CriarRegistroAsync(context, cooperado, new DateTime(2026, 3, 2));

        var controller = PrepararController(new PontoController(context));
        var resultado = await controller.Create(contrato.Id, NovoModel(cooperado.Id, new DateTime(2026, 3, 3)));

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(2, await context.RegistrosPonto.CountAsync());
    }

    [Fact]
    public async Task Ponto_Create_DevePermitirMesmaDataParaOutroCooperado()
    {
        await using var context = NovoContexto();
        var contrato = await CriarContratoAsync(context, "Empresa 33333");
        var cooperado1 = await CriarCooperadoAsync(context, "Maria Souza", "123.456.789-07");
        var cooperado2 = await CriarCooperadoAsync(context, "Ana Oliveira", "123.456.789-06");
        await CriarAlocacaoAsync(context, cooperado1, contrato);
        await CriarAlocacaoAsync(context, cooperado2, contrato);
        await CriarRegistroAsync(context, cooperado1, new DateTime(2026, 3, 2));

        var controller = PrepararController(new PontoController(context));
        var resultado = await controller.Create(contrato.Id, NovoModel(cooperado2.Id, new DateTime(2026, 3, 2)));

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(2, await context.RegistrosPonto.CountAsync());
    }

    #endregion

    #region Edit - não duplica a data de outro registro do mesmo cooperado

    [Fact]
    public async Task Ponto_Edit_NaoDeveMoverRegistroParaDataJaUsadaPeloMesmoCooperado()
    {
        await using var context = NovoContexto();
        var cooperado = await CriarCooperadoAsync(context, "Carlos Lima", "123.456.789-05");
        var registro = await CriarRegistroAsync(context, cooperado, new DateTime(2026, 3, 2));
        await CriarRegistroAsync(context, cooperado, new DateTime(2026, 3, 3));

        var controller = PrepararController(new PontoController(context));
        var resultado = await controller.Edit(registro.Id, NovoModel(cooperado.Id, new DateTime(2026, 3, 3)));

        Assert.IsType<ViewResult>(resultado);
        AssertComErro(controller.ModelState, MensagemPontoRepetido);

        var naoAlterado = await context.RegistrosPonto.FindAsync(registro.Id);
        Assert.Equal(DataUtc(new DateTime(2026, 3, 2)), naoAlterado!.Data);
        Assert.Equal(2, await context.RegistrosPonto.CountAsync());
    }

    [Fact]
    public async Task Ponto_Edit_DevePermitirManterADataDoProprioRegistro()
    {
        await using var context = NovoContexto();
        var cooperado = await CriarCooperadoAsync(context, "Carlos Lima", "123.456.789-04");
        var registro = await CriarRegistroAsync(context, cooperado, new DateTime(2026, 3, 2));

        var controller = PrepararController(new PontoController(context));
        var resultado = await controller.Edit(registro.Id, NovoModel(cooperado.Id, new DateTime(2026, 3, 2)));

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(1, await context.RegistrosPonto.CountAsync());
    }

    #endregion

    #region Registrar - registro rápido

    [Fact]
    public async Task Ponto_Registrar_NaoDeveDuplicarRegistroNaMesmaData()
    {
        await using var context = NovoContexto();
        var contrato = await CriarContratoAsync(context, "Empresa 44444");
        var cooperado = await CriarCooperadoAsync(context, "Paula Reis", "123.456.789-03");
        await CriarAlocacaoAsync(context, cooperado, contrato);
        await CriarRegistroAsync(context, cooperado, new DateTime(2026, 3, 2));

        var controller = PrepararController(new PontoController(context));
        var resultado = await controller.Registrar(
            cooperado.Id,
            new DateTime(2026, 3, 2),
            TimeSpan.FromHours(8),
            TimeSpan.FromHours(17),
            inicioIntervalo: null,
            fimIntervalo: null,
            ocorrencia: null);

        Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(MensagemPontoRepetido, controller.TempData["MensagemErro"]);
        Assert.Equal(1, await context.RegistrosPonto.CountAsync());
    }

    [Fact]
    public async Task Ponto_Registrar_DevePermitirRegistroEmDataNova()
    {
        await using var context = NovoContexto();
        var contrato = await CriarContratoAsync(context, "Empresa 55555");
        var cooperado = await CriarCooperadoAsync(context, "Paula Reis", "123.456.789-02");
        await CriarAlocacaoAsync(context, cooperado, contrato);
        await CriarRegistroAsync(context, cooperado, new DateTime(2026, 3, 2));

        var controller = PrepararController(new PontoController(context));
        var resultado = await controller.Registrar(
            cooperado.Id,
            new DateTime(2026, 3, 4),
            TimeSpan.FromHours(8),
            TimeSpan.FromHours(17),
            inicioIntervalo: null,
            fimIntervalo: null,
            ocorrencia: null);

        Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Ponto registrado com sucesso.", controller.TempData["MensagemSucesso"]);
        Assert.Equal(2, await context.RegistrosPonto.CountAsync());
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

    private static RegistroPonto NovoModel(Guid cooperadoId, DateTime data) => new()
    {
        CooperadoId = cooperadoId,
        Data = data,
        Entrada = new DateTime(data.Year, data.Month, data.Day, 8, 0, 0),
        Saida = new DateTime(data.Year, data.Month, data.Day, 17, 0, 0),
        Ocorrencia = null,
        Observacao = string.Empty
    };

    private static DateTime DataUtc(DateTime data) =>
        new(data.Year, data.Month, data.Day, 0, 0, 0, DateTimeKind.Utc);

    private static Task<Contrato> CriarContratoAsync(CooperativaDbContext context, string empresaNome) =>
        ApoioTestes.CriarContratoAsync(context, empresaNome);

    private static async Task<Cooperado> CriarCooperadoAsync(
        CooperativaDbContext context,
        string nome,
        string cpf)
    {
        var cooperado = new Cooperado
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            CPF = cpf,
            DataNascimento = new DateTime(1990, 5, 10),
            NomeMae = "Maria da Silva",
            Naturalidade = "São Paulo",
            Nacionalidade = "Brasileira",
            Sexo = "Feminino",
            Email = "cooperado@email.com",
            Telefone = "(31) 98888-3333",
            Logradouro = "Rua das Flores, 100",
            Bairro = "Centro",
            Cidade = "São Paulo",
            CEP = "01001-000",
            DataAdmissao = new DateTime(2026, 1, 1),
            Ativo = true
        };

        context.Cooperados.Add(cooperado);
        await context.SaveChangesAsync();
        return cooperado;
    }

    private static async Task CriarAlocacaoAsync(
        CooperativaDbContext context,
        Cooperado cooperado,
        Contrato contrato)
    {
        var funcao = await ApoioTestes.CriarFuncaoAsync(context, $"Função {Guid.NewGuid():N}");

        context.Alocacoes.Add(new Alocacao
        {
            Id = Guid.NewGuid(),
            CooperadoId = cooperado.Id,
            ContratoId = contrato.Id,
            FuncaoId = funcao.Id,
            DataInicio = new DateTime(2026, 1, 1),
            DataFim = null
        });
        await context.SaveChangesAsync();
    }

    private static async Task<RegistroPonto> CriarRegistroAsync(
        CooperativaDbContext context,
        Cooperado cooperado,
        DateTime data)
    {
        var dataPonto = DataUtc(data);
        var registro = new RegistroPonto
        {
            Id = Guid.NewGuid(),
            CooperadoId = cooperado.Id,
            Data = dataPonto,
            Entrada = dataPonto.AddHours(8),
            Saida = dataPonto.AddHours(17),
            Observacao = string.Empty
        };

        context.RegistrosPonto.Add(registro);
        await context.SaveChangesAsync();
        return registro;
    }

    #endregion
}

