using System.ComponentModel.DataAnnotations;
using Cooperativa.Models;
using Xunit;

namespace Cooperativa.Tests;

public class ValorMonetarioTests
{
    [Theory]
    [InlineData("1500", 1500)]
    [InlineData("1200,50", 1200.50)]
    [InlineData("1200.50", 1200.50)]
    [InlineData("1.200,50", 1200.50)]
    [InlineData("1.200.000,75", 1200000.75)]
    [InlineData("1.500", 1500)]
    [InlineData("0,99", 0.99)]
    [InlineData("R$ 2.500,75", 2500.75)]
    [InlineData(" 800 ", 800)]
    public void TentarConverter_DeveAceitarFormatosPtBrEInternacional(string texto, double esperado)
    {
        var convertido = ValorMonetario.TentarConverter(texto, out var valor);

        Assert.True(convertido);
        Assert.Equal((decimal)esperado, valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("abc")]
    [InlineData("1,2,3")]
    [InlineData("R$")]
    public void TentarConverter_DeveRecusarTextoSemValorValido(string? texto)
    {
        var convertido = ValorMonetario.TentarConverter(texto, out var valor);

        Assert.False(convertido);
        Assert.Equal(0m, valor);
    }

    [Fact]
    public void Formatar_DeveUsarPadraoPtBr()
    {
        Assert.Equal("1.234,56", ValorMonetario.Formatar(1234.56m));
        Assert.Equal("800,00", ValorMonetario.Formatar(800m));
    }

    [Fact]
    public void Remuneracao_ValorTextoNaoDeveSerPersistido()
    {
        var propriedade = typeof(Remuneracao).GetProperty(nameof(Remuneracao.ValorTexto));

        Assert.NotNull(propriedade);
        Assert.Contains(
            propriedade!.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute), true),
            atributo => atributo is System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute);
    }
}
