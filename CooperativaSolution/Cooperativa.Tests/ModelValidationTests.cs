using System.ComponentModel.DataAnnotations;
using Cooperativa.Models;
using Xunit;

namespace Cooperativa.Tests;

public class ModelValidationTests
{
    [Fact]
    public void Cooperado_DeveTerNomeCpfEEmailParaSerValido()
    {
        var cooperado = new Cooperado
        {
            Nome = string.Empty,
            CPF = string.Empty,
            Email = string.Empty
        };

        var validationContext = new ValidationContext(cooperado);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(cooperado, validationContext, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("nome"));
    }

    [Fact]
    public void Remuneracao_DataFimDeveSerOpcional()
    {
        Assert.Equal(typeof(DateTime?), typeof(Remuneracao).GetProperty(nameof(Remuneracao.DataFim))!.PropertyType);

        var remuneracao = new Remuneracao
        {
            ContratoId = Guid.NewGuid(),
            FuncaoId = Guid.NewGuid(),
            DataInicio = DateTime.Today,
            TipoRemuneracao = "DIA",
            Valor = 100
        };

        var validationContext = new ValidationContext(remuneracao);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(remuneracao, validationContext, results, true);

        Assert.Null(remuneracao.DataFim);
        Assert.True(isValid);
    }

    [Fact]
    public void Alocacao_DataFimDeveSerOpcional()
    {
        Assert.Equal(typeof(DateTime?), typeof(Alocacao).GetProperty(nameof(Alocacao.DataFim))!.PropertyType);

        var alocacao = new Alocacao
        {
            CooperadoId = Guid.NewGuid(),
            ContratoId = Guid.NewGuid(),
            FuncaoId = Guid.NewGuid(),
            DataInicio = DateTime.Today
        };

        var validationContext = new ValidationContext(alocacao);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(alocacao, validationContext, results, true);

        Assert.Null(alocacao.DataFim);
        Assert.True(isValid);
    }

    [Fact]
    public void Contrato_DataFimDeveSerOpcional()
    {
        Assert.Equal(typeof(DateTime?), typeof(Contrato).GetProperty(nameof(Contrato.DataFim))!.PropertyType);

        var contrato = new Contrato
        {
            EmpresaNome = "Empresa Alpha",
            Cnpj = "12.345.678/0001-99",
            DataInicio = DateTime.Today,
            ValorContrato = 1000,
            Logradouro = "Rua das Flores, 123",
            Bairro = "Centro",
            Cidade = "Belo Horizonte",
            Uf = "MG",
            Cep = "30110-000"
        };

        var validationContext = new ValidationContext(contrato);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contrato, validationContext, results, true);

        Assert.Null(contrato.DataFim);
        Assert.True(isValid);
    }

    [Fact]
    public void Contrato_DeveExigirNomeCnpjEEndereco()
    {
        var contrato = new Contrato
        {
            EmpresaNome = string.Empty,
            Cnpj = "123",
            DataInicio = DateTime.Today,
            ValorContrato = 0,
            Logradouro = string.Empty,
            Bairro = string.Empty,
            Cidade = string.Empty,
            Uf = string.Empty,
            Cep = string.Empty
        };

        var validationContext = new ValidationContext(contrato);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contrato, validationContext, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("empresa", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("00.000.000/0000-00", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("logradouro", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("bairro", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("cidade", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("UF", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("CEP", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Contrato_ContatosDevemSerOpcionais()
    {
        var contrato = new Contrato
        {
            EmpresaNome = "Empresa Alpha",
            Cnpj = "12.345.678/0001-99",
            DataInicio = DateTime.Today,
            ValorContrato = 1000,
            Logradouro = "Rua das Flores, 123",
            Bairro = "Centro",
            Cidade = "Belo Horizonte",
            Uf = "MG",
            Cep = "30110-000",
            Contato1Nome = string.Empty,
            Contato1Telefone = string.Empty,
            Contato2Nome = string.Empty,
            Contato2Telefone = string.Empty
        };

        var validationContext = new ValidationContext(contrato);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contrato, validationContext, results, true);

        Assert.True(isValid);
    }

    [Fact]
    public void Contrato_DeveAceitarCnpjAlfanumerico()
    {
        var contrato = new Contrato
        {
            EmpresaNome = "Empresa Alpha",
            Cnpj = "12.ABC.345/01DE-35",
            DataInicio = DateTime.Today,
            ValorContrato = 0,
            Logradouro = "Rua das Flores, 123",
            Bairro = "Centro",
            Cidade = "Belo Horizonte",
            Uf = "MG",
            Cep = "30110-000"
        };

        var validationContext = new ValidationContext(contrato);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contrato, validationContext, results, true);

        Assert.True(isValid);
    }

    [Fact]
    public void Contrato_DeveInvalidarCnpjComDigitosVerificadoresNaoNumericos()
    {
        var contrato = new Contrato
        {
            EmpresaNome = "Empresa Alpha",
            Cnpj = "12.ABC.345/01DE-AB",
            DataInicio = DateTime.Today,
            ValorContrato = 0,
            Logradouro = "Rua das Flores, 123",
            Bairro = "Centro",
            Cidade = "Belo Horizonte",
            Uf = "MG",
            Cep = "30110-000"
        };

        var validationContext = new ValidationContext(contrato);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contrato, validationContext, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("00.000.000/0000-00", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Contrato_DeveValidarFormatoDeTelefoneSomenteQuandoInformado()
    {
        var contrato = new Contrato
        {
            EmpresaNome = "Empresa Alpha",
            Cnpj = "12.345.678/0001-99",
            DataInicio = DateTime.Today,
            ValorContrato = 0,
            Logradouro = "Rua das Flores, 123",
            Bairro = "Centro",
            Cidade = "Belo Horizonte",
            Uf = "MG",
            Cep = "30110-000",
            Contato1Nome = "João Silva",
            Contato1Telefone = "123"
        };

        var validationContext = new ValidationContext(contrato);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contrato, validationContext, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("(00) 00000-0000", StringComparison.OrdinalIgnoreCase));

        // Com o telefone em branco (contato opcional) o contrato volta a ser válido.
        contrato.Contato1Telefone = string.Empty;
        results.Clear();
        isValid = Validator.TryValidateObject(contrato, validationContext, results, true);
        Assert.True(isValid);
    }

    [Fact]
    public void Contrato_DeveSerValidoComDadosDigitadosFormatados()
    {
        var contrato = new Contrato
        {
            EmpresaNome = "Empresa Alpha",
            Cnpj = "12.345.678/0001-99",
            DataInicio = DateTime.Today,
            ValorContrato = 1000,
            Logradouro = "Rua das Flores, 123",
            Bairro = "Centro",
            Cidade = "Belo Horizonte",
            Uf = "MG",
            Cep = "30110-000",
            Contato1Nome = "João Silva",
            Contato1Telefone = "(31) 98888-3333",
            Contato2Nome = "Maria Souza",
            Contato2Telefone = "(31) 3333-4444"
        };

        var validationContext = new ValidationContext(contrato);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contrato, validationContext, results, true);

        Assert.True(isValid);
    }

    [Fact]
    public void Cooperado_ValidacoesDeFormato_TelefoneEmailCep()
    {
        var cooperadoInvalido = new Cooperado
        {
            Nome = "José Carlos",
            DataNascimento = new DateTime(1990, 5, 20),
            CPF = "123.456.789-00",
            NomeMae = "Maria da Silva",
            Sexo = "Masculino",
            Email = "email_invalido", // Formato inválido
            Telefone = "123", // Formato inválido (deve ser (31) 98888-3333)
            Logradouro = "Rua das Flores, 123",
            Bairro = "Centro",
            Cidade = "Belo Horizonte",
            CEP = "invalido", // Formato inválido
            DataAdmissao = new DateTime(2026, 1, 1)
        };

        var context = new ValidationContext(cooperadoInvalido);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(cooperadoInvalido, context, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("e-mail", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("(00) 00000-0000", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("35.000-000", StringComparison.OrdinalIgnoreCase));

        // Ajustando para os formatos corretos
        cooperadoInvalido.Email = "jose.carlos@email.com";
        cooperadoInvalido.Telefone = "(31) 98888-3333";
        cooperadoInvalido.CEP = "35.000-000";

        results.Clear();
        isValid = Validator.TryValidateObject(cooperadoInvalido, context, results, true);
        Assert.True(isValid);
    }

    [Fact]
    public void Cooperado_NumeroDependentesEUfDevemSerOpcionais()
    {
        var cooperado = new Cooperado
        {
            Nome = "José Carlos",
            DataNascimento = new DateTime(1990, 5, 20),
            CPF = "123.456.789-00",
            NomeMae = "Maria da Silva",
            Sexo = "Masculino",
            Email = "jose.carlos@email.com",
            Telefone = "(31) 98888-3333",
            Logradouro = "Rua das Flores, 123",
            Bairro = "Centro",
            Cidade = "Belo Horizonte",
            CEP = "35.000-000",
            DataAdmissao = new DateTime(2026, 1, 1)
        };

        Assert.Null(cooperado.NumeroDependentes);
        Assert.Equal(typeof(int?), typeof(Cooperado).GetProperty(nameof(Cooperado.NumeroDependentes))!.PropertyType);
        Assert.Equal(string.Empty, cooperado.Uf);

        var context = new ValidationContext(cooperado);
        var results = new List<ValidationResult>();
        Assert.True(Validator.TryValidateObject(cooperado, context, results, true));

        // UF informada em maiúsculas/minúsculas é aceita...
        cooperado.Uf = "mg";
        results.Clear();
        Assert.True(Validator.TryValidateObject(cooperado, context, results, true));

        // ...e UF com formato inválido é recusada.
        cooperado.Uf = "MGG";
        results.Clear();
        Assert.False(Validator.TryValidateObject(cooperado, context, results, true));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("UF", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Cooperado_DeveAceitarNumeroDependentesInformado()
    {
        var cooperado = new Cooperado
        {
            Nome = "José Carlos",
            DataNascimento = new DateTime(1990, 5, 20),
            CPF = "123.456.789-00",
            NomeMae = "Maria da Silva",
            Sexo = "Masculino",
            Email = "jose.carlos@email.com",
            Telefone = "(31) 98888-3333",
            Logradouro = "Rua das Flores, 123",
            Bairro = "Centro",
            Cidade = "Belo Horizonte",
            Uf = "MG",
            CEP = "35.000-000",
            NumeroDependentes = 2,
            DataAdmissao = new DateTime(2026, 1, 1)
        };

        var context = new ValidationContext(cooperado);
        var results = new List<ValidationResult>();
        Assert.True(Validator.TryValidateObject(cooperado, context, results, true));

        // Fora da faixa 0..50 a validação recusa.
        cooperado.NumeroDependentes = 51;
        results.Clear();
        Assert.False(Validator.TryValidateObject(cooperado, context, results, true));
    }
}
