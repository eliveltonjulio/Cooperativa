using System.ComponentModel.DataAnnotations;
using Cooperativa.Models;
using Cooperativa.Web.Models;
using Cooperativa.Web.Services;
using Xunit;

namespace Cooperativa.Tests;

/// <summary>
/// Testes da alteração de senha pelo próprio usuário
/// (UsuarioService.AlterarSenhaAsync + AlterarSenhaViewModel).
/// </summary>
public class AlterarSenhaTests
{
    private static Usuario CriarUsuario(string senhaArmazenada, bool ativo = true) => new()
    {
        Id = Guid.NewGuid(),
        Nome = "Usuário Teste",
        Email = "teste@cooperativa.com",
        Celular = "11988882222",
        Login = "usuario.teste",
        Senha = senhaArmazenada,
        Perfil = "Cooperado",
        Ativo = ativo
    };

    [Fact]
    public async Task SenhaAtualCorreta_AlteraParaHashBcrypt()
    {
        using var context = ApoioTestes.NovoContexto();
        var usuario = CriarUsuario(BCrypt.Net.BCrypt.HashPassword("Senha#Antiga1"));
        context.UsuariosSistema.Add(usuario);
        await context.SaveChangesAsync();

        var service = new UsuarioService(context);
        var resultado = await service.AlterarSenhaAsync(usuario.Id, "Senha#Antiga1", "Nova#Senha2");

        Assert.Equal(AlteracaoSenhaResultado.Sucesso, resultado);
        Assert.True(BCrypt.Net.BCrypt.Verify("Nova#Senha2", usuario.Senha));
        Assert.False(BCrypt.Net.BCrypt.Verify("Senha#Antiga1", usuario.Senha));
    }

    [Fact]
    public async Task SenhaAtualIncorreta_NaoAlteraOHash()
    {
        using var context = ApoioTestes.NovoContexto();
        var hashOriginal = BCrypt.Net.BCrypt.HashPassword("Senha#Antiga1");
        var usuario = CriarUsuario(hashOriginal);
        context.UsuariosSistema.Add(usuario);
        await context.SaveChangesAsync();

        var service = new UsuarioService(context);
        var resultado = await service.AlterarSenhaAsync(usuario.Id, "SenhaErrada1", "Nova#Senha2");

        Assert.Equal(AlteracaoSenhaResultado.SenhaAtualIncorreta, resultado);
        Assert.Equal(hashOriginal, usuario.Senha);
    }

    [Fact]
    public async Task UsuarioInexistente_RetornaUsuarioNaoEncontrado()
    {
        using var context = ApoioTestes.NovoContexto();
        var service = new UsuarioService(context);

        var resultado = await service.AlterarSenhaAsync(Guid.NewGuid(), "qualquer1", "Nova#Senha2");

        Assert.Equal(AlteracaoSenhaResultado.UsuarioNaoEncontrado, resultado);
    }

    [Fact]
    public async Task UsuarioInativo_RetornaUsuarioNaoEncontrado()
    {
        using var context = ApoioTestes.NovoContexto();
        var usuario = CriarUsuario(BCrypt.Net.BCrypt.HashPassword("Senha#Antiga1"), ativo: false);
        context.UsuariosSistema.Add(usuario);
        await context.SaveChangesAsync();

        var service = new UsuarioService(context);
        var resultado = await service.AlterarSenhaAsync(usuario.Id, "Senha#Antiga1", "Nova#Senha2");

        Assert.Equal(AlteracaoSenhaResultado.UsuarioNaoEncontrado, resultado);
        Assert.True(BCrypt.Net.BCrypt.Verify("Senha#Antiga1", usuario.Senha));
    }

    [Fact]
    public async Task SenhaLegadaEmTextoPuro_ContaComoSenhaAtualValida()
    {
        using var context = ApoioTestes.NovoContexto();
        var usuario = CriarUsuario("legada123");
        context.UsuariosSistema.Add(usuario);
        await context.SaveChangesAsync();

        var service = new UsuarioService(context);
        var resultado = await service.AlterarSenhaAsync(usuario.Id, "legada123", "Nova#Senha2");

        Assert.Equal(AlteracaoSenhaResultado.Sucesso, resultado);
        // Após a alteração, a senha passa a ser armazenada com hash.
        Assert.True(BCrypt.Net.BCrypt.Verify("Nova#Senha2", usuario.Senha));
        Assert.NotEqual("Nova#Senha2", usuario.Senha);
    }

    [Fact]
    public async Task ParametrosVazios_RetornaUsuarioNaoEncontrado()
    {
        using var context = ApoioTestes.NovoContexto();
        var service = new UsuarioService(context);

        Assert.Equal(AlteracaoSenhaResultado.UsuarioNaoEncontrado,
            await service.AlterarSenhaAsync(Guid.Empty, "senha1", "outra12"));
        Assert.Equal(AlteracaoSenhaResultado.UsuarioNaoEncontrado,
            await service.AlterarSenhaAsync(Guid.NewGuid(), " ", "outra12"));
        Assert.Equal(AlteracaoSenhaResultado.UsuarioNaoEncontrado,
            await service.AlterarSenhaAsync(Guid.NewGuid(), "senha1", " "));
    }

    [Theory]
    [InlineData("SenhaAtual1", "Nova#Senha2", "Nova#Senha2", true)]
    [InlineData("SenhaAtual1", "Nova#Senha2", "Diferente1", false)]      // confirmação não confere
    [InlineData("SenhaAtual1", "abc", "abc", false)]                     // nova senha com menos de 6
    [InlineData("SenhaAtual1", "", "", false)]                           // nova senha vazia
    [InlineData("", "Nova#Senha2", "Nova#Senha2", false)]                // senha atual vazia
    public void ValidacoesDoModelo(
        string senhaAtual, string novaSenha, string confirmacao, bool esperadoValido)
    {
        var model = new AlterarSenhaViewModel
        {
            SenhaAtual = senhaAtual,
            NovaSenha = novaSenha,
            ConfirmarNovaSenha = confirmacao
        };

        var erros = new List<ValidationResult>();
        var valido = Validator.TryValidateObject(
            model, new ValidationContext(model), erros, validateAllProperties: true);

        Assert.Equal(esperadoValido, valido);
    }
}
