using System.Globalization;

namespace Cooperativa.Models;

/// <summary>
/// Conversão de valores monetários digitados livremente em formulários
/// (aceita formatos pt-BR como 1.500,50 e também 1500.50).
/// </summary>
public static class ValorMonetario
{
    /// <summary>
    /// Tenta converter o texto digitado em <see cref="decimal"/>. O separador decimal é
    /// identificado explicitamente para não confundir ponto de milhar com ponto decimal
    /// (ex.: "1.500,50" = 1500,50 e "1500.50" = 1500,50).
    /// </summary>
    public static bool TentarConverter(string? texto, out decimal valor)
    {
        valor = 0m;

        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        // Mantém apenas dígitos e separadores (ignora "R$", espaços, etc.).
        var limpo = new string(texto.Where(c => char.IsDigit(c) || c == ',' || c == '.').ToArray());
        if (limpo.Length == 0)
        {
            return false;
        }

        var temVirgula = limpo.Contains(',');
        var temPonto = limpo.Contains('.');
        string normalizado;

        if (temVirgula && temPonto)
        {
            // Os dois separadores estão presentes: o último é o decimal.
            normalizado = limpo.LastIndexOf(',') > limpo.LastIndexOf('.')
                ? limpo.Replace(".", string.Empty).Replace(',', '.')
                : limpo.Replace(",", string.Empty);
        }
        else if (temVirgula)
        {
            normalizado = limpo.Replace(',', '.');
        }
        else if (temPonto)
        {
            var partes = limpo.Split('.');
            var ultimaParte = partes[^1];

            // Ponto com exatamente 3 dígitos após o último grupo é separador de milhar
            // ("1.500" = 1500); com 1 ou 2 dígitos é separador decimal ("1500.5", "1500.50").
            normalizado = ultimaParte.Length == 3 && limpo.Length > 4
                ? limpo.Replace(".", string.Empty)
                : limpo;
        }
        else
        {
            normalizado = limpo;
        }

        return decimal.TryParse(normalizado, NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
    }

    /// <summary>Formata o valor para exibição/edição no padrão pt-BR (ex.: 1.234,56).</summary>
    public static string Formatar(decimal valor)
        => valor.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"));
}
