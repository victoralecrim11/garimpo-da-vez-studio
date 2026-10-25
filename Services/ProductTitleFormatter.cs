using System.Text.RegularExpressions;

namespace GarimpoDaVezStudio.Services;

/// <summary>
/// Sugere um nome editorial curto a partir de palavras presentes no título original.
/// Nunca consulta IA e nunca acrescenta marca, modelo ou característica inexistente.
/// O título fornecido pela API deve continuar disponível para revisão do usuário.
/// </summary>
public static class ProductTitleFormatter
{
    public static string Suggest(string? officialTitle)
    {
        if (string.IsNullOrWhiteSpace(officialTitle)) return "";
        var normalized = Regex.Replace(officialTitle, @"\s+", " ").Trim(' ', ',', ';', '|', '-', '–');
        if (normalized.Length == 0) return "";

        // Catálogos frequentemente trazem listas de termos de SEO separadas
        // por vírgulas, sem que toda essa lista seja o nome do produto.
        var parts = Regex.Split(normalized, @"\s*[,;|]\s*")
            .Where(part => part.Length > 0).ToArray();
        var candidate = normalized;
        if (parts.Length >= 3 || (parts.Length > 1 && normalized.Length > 100))
        {
            // O primeiro trecho só vira um nome se for suficientemente
            // específico: evita reduzir "Luminária, preta, LED" a "Luminária".
            var first = parts[0].Trim();
            if (first.Length >= 12 && first.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 3)
                candidate = first;
        }

        const int maxCharacters = 95;
        if (candidate.Length <= maxCharacters) return candidate;
        var before = candidate[..maxCharacters];
        var lastSpace = before.LastIndexOf(' ');
        if (lastSpace >= 48) before = before[..lastSpace];
        return before.TrimEnd(' ', ',', ';', '|', '-', '–');
    }

    public static bool WasShortened(string? officialTitle) =>
        !string.Equals(Suggest(officialTitle),
            Regex.Replace(officialTitle ?? "", @"\s+", " ").Trim(' ', ',', ';', '|', '-', '–'),
            StringComparison.Ordinal);
}
