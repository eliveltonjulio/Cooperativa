using System.ComponentModel.DataAnnotations;
using Cooperativa.Models;
using Xunit;

namespace Cooperativa.Tests;

public class OcorrenciaPontoTests
{
    [Fact]
    public void Catalogo_DeveConterExatamenteAsSeteOcorrenciasEsperadas()
    {
        var codigosEsperados = new[]
        {
            "FOLGA",
            "FERIADO",
            "ATESTADO",
            "FALTA",
            "ATRASO",
            "HE_APROV",
            "BANCO_FOLGA",
        };

        Assert.Equal(7, OcorrenciasPonto.Todas.Length);
        Assert.Equal(codigosEsperados, OcorrenciasPonto.Todas.Select(o => o.Codigo).ToArray());

        // Os códigos devem caber na coluna "Ocorrencia" (varchar(20)) do banco.
        Assert.All(OcorrenciasPonto.Todas, o => Assert.True(o.Codigo.Length <= 20, $"Código {o.Codigo} excede 20 caracteres."));
    }

    [Theory]
    [InlineData("FOLGA", "Folga Semanal / DSR", "Neutro (Horas Não Devidas)")]
    [InlineData("FERIADO", "Feriado", "Neutro / Abonado")]
    [InlineData("ATESTADO", "Licença Médica com Atestado", "Abonado (Paga Horas Normais)")]
    [InlineData("FALTA", "Falta Injustificada", "Desconto (Abate Horas)")]
    [InlineData("ATRASO", "Atraso / Saída Antecipada", "Desconto (Abate Minutos/Horas)")]
    [InlineData("HE_APROV", "Hora Extra Autorizada", "Adicional (Acresce Horas Extras)")]
    [InlineData("BANCO_FOLGA", "Abatimento de Banco de Horas", "Abonado (Consome Saldo Banco)")]
    public void Catalogo_DeveMapearCodigoDescricaoEImpactoNaFolha(string codigo, string descricaoEsperada, string tipoImpactoEsperado)
    {
        var ocorrencia = OcorrenciasPonto.ObterPorCodigo(codigo);

        Assert.NotNull(ocorrencia);
        Assert.Equal(codigo, ocorrencia!.Codigo);
        Assert.Equal(descricaoEsperada, ocorrencia.Descricao);
        Assert.Equal(tipoImpactoEsperado, ocorrencia.TipoImpactoFolha);
    }

    [Fact]
    public void ObterPorCodigo_DeveIgnorarCaixaEEspacosEmBranco()
    {
        Assert.Equal("HE_APROV", OcorrenciasPonto.ObterPorCodigo("  he_aprov ")!.Codigo);
        Assert.True(OcorrenciasPonto.Existe("folga"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("OCORRENCIA_INEXISTENTE")]
    public void ObterPorCodigo_DeveRetornarNuloParaCodigoInvalido(string? codigo)
    {
        Assert.Null(OcorrenciasPonto.ObterPorCodigo(codigo));
        Assert.False(OcorrenciasPonto.Existe(codigo));
    }

    [Fact]
    public void RegistroPonto_DeveAceitarOcorrenciaDoCatalogo()
    {
        var registro = new RegistroPonto
        {
            CooperadoId = Guid.NewGuid(),
            Data = new DateTime(2026, 9, 10),
            Ocorrencia = OcorrenciasPonto.CodigoFolga
        };

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(registro, new ValidationContext(registro), results, true);

        Assert.True(isValid);
    }

    [Fact]
    public void RegistroPonto_NaoDeveAceitarOcorrenciaComMaisDeVinteCaracteres()
    {
        var registro = new RegistroPonto
        {
            CooperadoId = Guid.NewGuid(),
            Data = new DateTime(2026, 9, 10),
            Ocorrencia = new string('X', 21)
        };

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(registro, new ValidationContext(registro), results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("20 caracteres"));
    }
}