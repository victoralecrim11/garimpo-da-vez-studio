namespace GarimpoDaVezStudio.Services;

/// <summary>
/// Nichos editoriais para conteúdo de afiliados, não taxonomia da API Mercado Livre.
/// São sugestões editáveis; não representam elegibilidade a comissão.
/// </summary>
public static class NicheCatalog
{
    public static IReadOnlyList<string> GetSuggestions(string? category)
    {
        string key = (category ?? "").ToUpperInvariant().Normalize(System.Text.NormalizationForm.FormD);
        key = new string(key.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());
        var entries = new List<string>
        {
            "Casa inteligente", "Decoração religiosa", "Organização doméstica",
            "Presentes criativos", "Achadinhos para casa", "Sustentabilidade",
            "Itens para escritório", "Produtos para viagem", "Lifestyle"
        };
        if (key.Contains("CASA") || key.Contains("DECOR") || key.Contains("MOVEIS"))
            entries.InsertRange(0, new[] { "Decoração minimalista", "Decoração de sala", "Decoração cristã", "Quadros decorativos",
                "Organização de cozinha", "Lavanderia", "Iluminação decorativa", "Móveis compactos", "Jardim e varanda" });
        if (key.Contains("ELETRON") || key.Contains("INFORMAT") || key.Contains("TECNOLOG"))
            entries.InsertRange(0, new[] { "Setup gamer", "Home office", "Acessórios para celular",
                "Periféricos", "Automação residencial", "Gadgets de produtividade" });
        if (key.Contains("MODA") || key.Contains("ROUP") || key.Contains("CALCAD"))
            entries.InsertRange(0, new[] { "Moda feminina", "Moda masculina", "Moda casual", "Acessórios de moda",
                "Moda fitness", "Moda sustentável" });
        if (key.Contains("BELEZA") || key.Contains("SAUDE") || key.Contains("CUIDADOS"))
            entries.InsertRange(0, new[] { "Skincare", "Cabelos", "Maquiagem", "Autocuidado", "Bem-estar", "Beleza masculina" });
        if (key.Contains("VEICUL"))
            entries.InsertRange(0, new[] { "Acessórios automotivos", "Cuidados com automóveis", "Organização de carro", "Motociclismo" });
        if (key.Contains("ESPORTE"))
            entries.InsertRange(0, new[] { "Treino em casa", "Corrida", "Academia", "Yoga", "Ciclismo" });
        if (key.Contains("BEBE") || key.Contains("BRINQUED"))
            entries.InsertRange(0, new[] { "Maternidade", "Quarto infantil", "Brinquedos educativos", "Puericultura" });
        if (key.Contains("PET") || key.Contains("ANIMA"))
            entries.InsertRange(0, new[] { "Cães", "Gatos", "Acessórios para pets", "Enriquecimento ambiental" });
        return entries.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
    }
}
