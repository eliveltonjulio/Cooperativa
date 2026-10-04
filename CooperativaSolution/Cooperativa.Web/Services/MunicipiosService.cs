using System.Text.Json;

namespace Cooperativa.Web.Services;

/// <summary>
/// Consulta a base de municípios por UF (arquivo <c>wwwroot/cidades/municipios.json</c>,
/// gerado a partir da API do IBGE). Alimenta a lista de cidades exibida na inclusão e
/// edição de contratos e valida se a cidade pertence à UF informada.
/// </summary>
public class MunicipiosService
{
    private readonly string _caminhoArquivo;
    private readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<string>>> _municipios;

    public MunicipiosService(string caminhoArquivo)
    {
        _caminhoArquivo = caminhoArquivo;
        _municipios = new Lazy<IReadOnlyDictionary<string, IReadOnlyList<string>>>(CarregarMunicipios);
    }

    /// <summary>Retorna os municípios da UF (em ordem alfabética) ou lista vazia para UF inexistente.</summary>
    public IReadOnlyList<string> ObterCidades(string? uf)
    {
        var chave = (uf ?? string.Empty).Trim().ToUpperInvariant();
        if (chave.Length == 0)
        {
            return Array.Empty<string>();
        }

        return _municipios.Value.TryGetValue(chave, out var cidades) ? cidades : Array.Empty<string>();
    }

    /// <summary>Indica se a cidade informada consta na lista de municípios da UF (ignora maiúsculas/acentos de caixa e espaços nas pontas).</summary>
    public bool CidadePertenceAUf(string? cidade, string? uf)
    {
        var nome = (cidade ?? string.Empty).Trim();
        if (nome.Length == 0)
        {
            return false;
        }

        return ObterCidades(uf).Any(c => string.Equals(c, nome, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Localiza <c>municipios.json</c> a partir do diretório da aplicação, do diretório de
    /// trabalho e das pastas ascendentes. Cobre a execução via <c>dotnet run</c>, a saída de
    /// build (arquivo copiado pelo projeto Web) e os testes unitários.
    /// </summary>
    public static string LocalizarArquivoPadrao()
    {
        var caminhoRelativo = Path.Combine("wwwroot", "cidades", "municipios.json");
        var raizes = new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() };

        foreach (var raiz in raizes)
        {
            var diretorio = new DirectoryInfo(raiz);
            while (diretorio != null)
            {
                var candidato = Path.Combine(diretorio.FullName, caminhoRelativo);
                if (File.Exists(candidato))
                {
                    return candidato;
                }

                diretorio = diretorio.Parent;
            }
        }

        return Path.Combine(AppContext.BaseDirectory, caminhoRelativo);
    }

    private IReadOnlyDictionary<string, IReadOnlyList<string>> CarregarMunicipios()
    {
        if (!File.Exists(_caminhoArquivo))
        {
            throw new FileNotFoundException($"Arquivo de municípios não encontrado: {_caminhoArquivo}", _caminhoArquivo);
        }

        var json = File.ReadAllText(_caminhoArquivo);
        var dados = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (dados == null)
        {
            throw new InvalidOperationException($"Arquivo de municípios inválido: {_caminhoArquivo}");
        }

        return dados.ToDictionary(
            par => par.Key.Trim().ToUpperInvariant(),
            par => (IReadOnlyList<string>)par.Value
                .OrderBy(c => c, StringComparer.CurrentCulture)
                .ToList(),
            StringComparer.Ordinal);
    }
}
