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
            """{"id":"MLBU1234","name":"Luminária de catálogo","kind":"catalog"}""");
        if (fromPython?.ToString() != "Referência de catálogo · Luminária de catálogo")
        {
            Console.Error.WriteLine("Falha: a desserialização JSON não preservou o texto exibido.");
            failures++;
        }

        if (failures > 0) return 1;
        Console.WriteLine("5 verificações de rótulo, seleção e JSON aprovadas.");
        return 0;
    }
}
