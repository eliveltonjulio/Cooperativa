using Cooperativa.Web.Services;
using Xunit;

namespace Cooperativa.Tests;

/// <summary>
/// Base de municípios usada para selecionar a cidade pela UF no contrato.
/// </summary>
public class MunicipiosServiceTests
{
    private static MunicipiosService NovoServico()
        => new(MunicipiosService.LocalizarArquivoPadrao());

    [Fact]
    public void ObterCidades_UfValida_RetornaMunicipiosOrdenados()
    {
        var cidades = NovoServico().ObterCidades("sp");

        Assert.NotEmpty(cidades);
        Assert.Contains("São Paulo", cidades);
        Assert.Equal(cidades.OrderBy(c => c, StringComparer.CurrentCulture), cidades);
    }

    [Fact]
    public void ObterCidades_UfInvalidaOuVazia_RetornaListaVazia()
    {
        var servico = NovoServico();

        Assert.Empty(servico.ObterCidades("XX"));
        Assert.Empty(servico.ObterCidades(""));
        Assert.Empty(servico.ObterCidades(null));
    }

    [Fact]
    public void BaseDeMunicipios_CobremAs27Ufs()
    {
        var servico = NovoServico();
        var ufs = new[] { "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO" };

        Assert.All(ufs, uf => Assert.NotEmpty(servico.ObterCidades(uf)));
    }

    [Fact]
    public void CidadePertenceAUf_CidadeDaPropriaUf_RetornaVerdadeiro()
    {
        var servico = NovoServico();

        Assert.True(servico.CidadePertenceAUf("Belo Horizonte", "MG"));
        Assert.True(servico.CidadePertenceAUf("  belo horizonte  ", "mg"));
    }

    [Fact]
    public void CidadePertenceAUf_CidadeDeOutraUfOuDesconhecida_RetornaFalso()
    {
        var servico = NovoServico();

        Assert.False(servico.CidadePertenceAUf("Belo Horizonte", "SP"));
        Assert.False(servico.CidadePertenceAUf("Cidade Inexistente", "MG"));
        Assert.False(servico.CidadePertenceAUf("", "MG"));
        Assert.False(servico.CidadePertenceAUf("São Paulo", "XX"));
    }
}
