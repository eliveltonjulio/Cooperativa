using System.ComponentModel.DataAnnotations;
using Cooperativa.Models;
using Xunit;

namespace Cooperativa.Tests;

public class DomainModelValidationTests
{
    [Fact]
    public void Funcao_DeveExigirNomeEDescricao()
    {
        var funcao = new Funcao
        {
            Nome = string.Empty,
            Descricao = string.Empty
        };

        var validationContext = new ValidationContext(funcao);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(funcao, validationContext, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("nome"));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("descrição"));
    }

    [Fact]
    public void Funcao_NaoDeveMaisTerValorBase()
    {
        Assert.Null(typeof(Funcao).GetProperty("ValorBase"));
    }

    [Theory]
    [InlineData("4110-10")]
    [InlineData("411010")]
    [InlineData("2124-05")]
    [InlineData("")]
    public void Funcao_DeveAceitarCboValidoOuVazio(string cbo)
    {
        var funcao = new Funcao
        {
            Nome = "Auxiliar de Limpeza",
            Descricao = "Executa a limpeza e conservação das instalações.",
            Cbo = cbo
        };

        var validationContext = new ValidationContext(funcao);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(funcao, validationContext, results, true);

        Assert.True(isValid);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("1234567")]
    [InlineData("4110.10")]
    public void Funcao_DeveRecusarCboEmFormatoInvalido(string cbo)
    {
        var funcao = new Funcao
        {
            Nome = "Auxiliar de Limpeza",
            Descricao = "Executa a limpeza e conservação das instalações.",
            Cbo = cbo
        };

        var validationContext = new ValidationContext(funcao);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(funcao, validationContext, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("0000-00"));
    }

    [Fact]
    public void Contrato_DeveTerEmpresaNomeECnpjObrigatorios()
    {
        var contrato = new Contrato
        {
            EmpresaNome = string.Empty,
            Cnpj = string.Empty,
            DataInicio = DateTime.Today,
            ValorContrato = 0
        };

        var validationContext = new ValidationContext(contrato);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contrato, validationContext, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("empresa"));
    }

    [Fact]
    public void Remuneracao_DeveExigirTipoRemuneracao()
    {
        var remuneracao = new Remuneracao
        {
            ContratoId = Guid.NewGuid(),
            FuncaoId = Guid.NewGuid(),
            TipoRemuneracao = string.Empty,
            Valor = 0
        };

        var validationContext = new ValidationContext(remuneracao);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(remuneracao, validationContext, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("tipo"));
    }

    [Fact]
    public void Remuneracao_TipoRemuneracaoPadraoDeveSerDia()
    {
        var remuneracao = new Remuneracao();

        Assert.Equal("DIA", remuneracao.TipoRemuneracao);
    }

    [Fact]
    public void Remuneracao_DeveSerValidaComContratoEFuncao()
    {
        var remuneracao = new Remuneracao
        {
            ContratoId = Guid.NewGuid(),
            FuncaoId = Guid.NewGuid(),
            TipoRemuneracao = "DIA",
            Valor = 100
        };

        var validationContext = new ValidationContext(remuneracao);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(remuneracao, validationContext, results, true);

        Assert.True(isValid);
    }

    [Fact]
    public void Usuario_DeveExigirNomeEmailCelularLoginESenha()
    {
        var usuario = new Usuario
        {
            Nome = string.Empty,
            Email = string.Empty,
            Celular = string.Empty,
            Login = string.Empty,
            Senha = string.Empty,
            Perfil = string.Empty
        };

        var validationContext = new ValidationContext(usuario);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(usuario, validationContext, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("nome completo", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("e-mail", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("celular", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("login", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("senha", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Usuario_CelularDeveSeguirFormato11Digitos()
    {
        var usuarioInvalido = new Usuario
        {
            Nome = "Maria Silva",
            Email = "maria@exemplo.com",
            Celular = "123", // Inválido
            Login = "msilva",
            Senha = "senha123Safe",
            Perfil = "Coordenador"
        };

        var validationContext = new ValidationContext(usuarioInvalido);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(usuarioInvalido, validationContext, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("11988882222", StringComparison.OrdinalIgnoreCase));

        // Celular correto (11 dígitos com DDD)
        usuarioInvalido.Celular = "11988882222";
        results.Clear();
        isValid = Validator.TryValidateObject(usuarioInvalido, validationContext, results, true);
        Assert.True(isValid);
    }

    [Fact]
    public void Usuario_DeveSerValidoComTodosOsCamposFuncionais()
    {
        var usuario = new Usuario
        {
            Nome = "Carlos Coordenador",
            Email = "carlos@cooperativa.com",
            Celular = "11988882222",
            Login = "ccoordenador",
            Senha = "senhaForte123",
            Perfil = "Coordenador",
            Cargo = "Coordenador de TI",
            Equipe = "Equipe Alpha",
            DataIngresso = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Ativo = true
        };

        var validationContext = new ValidationContext(usuario);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(usuario, validationContext, results, true);

        Assert.True(isValid);
    }

    [Fact]
    public void Usuario_AutenticacaoComEmailOuLogin_VerificaCampos()
    {
        var usuario = new Usuario
        {
            Nome = "Ana Cooperada",
            Email = "ana.cooperada@cooperativa.com",
            Celular = "11977778888",
            Login = "acooperada",
            Senha = BCrypt.Net.BCrypt.HashPassword("senhaSegura123"),
            Perfil = "Cooperado",
            Ativo = true
        };

        // Verifica que tanto o e-mail quanto o login podem ser comparados para autenticação
        Assert.Equal("ana.cooperada@cooperativa.com", usuario.Email, ignoreCase: true);
        Assert.Equal("acooperada", usuario.Login, ignoreCase: true);

        // Verifica que a senha confere via BCrypt
        Assert.True(BCrypt.Net.BCrypt.Verify("senhaSegura123", usuario.Senha));
        Assert.False(BCrypt.Net.BCrypt.Verify("senhaIncorreta", usuario.Senha));
    }
}
