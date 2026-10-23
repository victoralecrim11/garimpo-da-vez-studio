using GarimpoDaVezStudio.Models;

namespace GarimpoDaVezStudio.Services;

public static class PromptService
{
    private static readonly string DefaultTemplatePath = Path.Combine(
        AppContext.BaseDirectory, "prompts", "pinterest-template.txt");

    public static string Build(Product product, string style, string scene)
    {
        var userTemplate = Path.Combine(AppPaths.Root, "prompts", "pinterest-template.txt");
        var templatePath = File.Exists(userTemplate) ? userTemplate : DefaultTemplatePath;
        if (!File.Exists(templatePath))
            throw new FileNotFoundException("Template de prompt não encontrado", templatePath);

        var template = File.ReadAllText(templatePath);
        var description = string.IsNullOrWhiteSpace(product.Description)
            ? product.Name : product.Description;
        return template
            .Replace("{{NOME}}", product.Name)
            .Replace("{{DESCRICAO}}", description)
            .Replace("{{CATEGORIA}}", product.Category)
            .Replace("{{NICHO}}", product.Niche)
            .Replace("{{ESTILO}}", style)
            .Replace("{{CENARIO}}", scene);
    }
}
