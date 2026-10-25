using System.Text.Json;
using GarimpoDaVezStudio.Services;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        var cases = new (ProductNameSuggestion item, string expected)[]
        {
            (new() { Kind = "local", Name = "Luminária decorativa" },
                "Meu cadastro · Luminária decorativa"),
            (new() { Kind = "listing", Name = "Luminária LED recarregável" },
                "Anúncio · Luminária LED recarregável"),
            (new() { Kind = "catalog", Name = "Luminária inteligente Wi-Fi" },
                "Referência de catálogo · Luminária inteligente Wi-Fi"),
        };

        using var suggestions = new ListBox { DisplayMember = "" };
        suggestions.Items.AddRange(cases.Select(c => (object)c.item).ToArray());

        int failures = 0;
        for (int index = 0; index < cases.Length; index++)
        {
            var expected = cases[index].expected;
            var actual = suggestions.GetItemText(suggestions.Items[index]);
            if (actual != expected || cases[index].item.ToString() != expected)
            {
                Console.Error.WriteLine($"Falha no item {index}: esperado '{expected}', exibido '{actual}'.");
                failures++;
            }
        }

        suggestions.SelectedIndex = 1;
        if (suggestions.SelectedItem is not ProductNameSuggestion selected ||
            selected.Kind != "listing" || selected.Name != "Luminária LED recarregável")
        {
            Console.Error.WriteLine("Falha: o controle não preservou o objeto da sugestão ao selecionar.");
            failures++;
        }

        var fromPython = JsonSerializer.Deserialize<ProductNameSuggestion>(
            """{"id":"MLBU1234","name":"Luminária de catálogo","kind":"catalog"}""",
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (fromPython?.ToString() != "Referência de catálogo · Luminária de catálogo")
        {
            Console.Error.WriteLine("Falha: a desserialização JSON não preservou o texto exibido.");
            failures++;
        }

        var seoTitle = "Luminária de mesa LED, luminária noturna, luminária de mesa, " +
            "mesa de cabeceira Medusa e luminária pendente Rgb Relaxing Gift, " +
            "luminária Hoist com 5 tipos de iluminação";
        var complex = new ProductNameSuggestion { Kind = "catalog", Name = seoTitle };
        if (complex.SuggestedName != "Luminária de mesa LED" ||
            complex.ToString() != "Referência de catálogo · Luminária de mesa LED" ||
            complex.Name != seoTitle || !ProductTitleFormatter.WasShortened(complex.Name))
        {
            Console.Error.WriteLine("Falha: o título SEO não foi resumido sem alterar o original.");
            failures++;
        }

        var simple = new ProductNameSuggestion { Kind = "listing", Name = "Luminária LED com base ajustável" };
        if (simple.SuggestedName != simple.Name || ProductTitleFormatter.WasShortened(simple.Name))
        {
            Console.Error.WriteLine("Falha: título normal foi alterado indevidamente.");
            failures++;
        }

        var shortComponents = "Kit de refletores LED, 400W";
        if (ProductTitleFormatter.Suggest(shortComponents) != shortComponents)
        {
            Console.Error.WriteLine("Falha: título curto com características legítimas perdeu informações.");
            failures++;
        }

        var localName = new ProductNameSuggestion { Kind = "local", Name = seoTitle };
        if (localName.SuggestedName != seoTitle || !localName.ToString().Contains(seoTitle))
        {
            Console.Error.WriteLine("Falha: cadastro local foi alterado automaticamente.");
            failures++;
        }

        var spaces = ProductTitleFormatter.Suggest("   Luminária  LED   de mesa,    luz noturna, decorativa ");
        if (spaces != "Luminária LED de mesa")
        {
            Console.Error.WriteLine("Falha: normalização de espaços e sinônimos.");
            failures++;
        }

        var longSingle = "Luminária recarregável com suporte ajustável para escritório " +
            "com controle por toque e múltiplas possibilidades de iluminação para estudar";
        if (ProductTitleFormatter.Suggest(longSingle).Length > 95 ||
            !longSingle.StartsWith(ProductTitleFormatter.Suggest(longSingle), StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Falha: nome longo sem vírgulas cortado fora do limite.");
            failures++;
        }

        if (failures > 0) return 1;
        Console.WriteLine("11 verificações de rótulo, títulos longos, seleção e JSON aprovadas.");
        return 0;
    }
}
