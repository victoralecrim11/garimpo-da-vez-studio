using GarimpoDaVezStudio.Models;

namespace GarimpoDaVezStudio.Services;

/// <summary>
/// Representa um resultado da busca lateral, preservando a diferença entre
/// cadastro salvo no SQLite e sugestão online ainda não importada.
/// </summary>
public sealed class SidebarSearchItem
{
    public Product? LocalProduct { get; init; }
    public ProductNameSuggestion? OfficialSuggestion { get; init; }
    public GarimpoDaVezStudio.Data.CatalogEntry? CatalogReference { get; init; }

    public static bool IsHistoricalSpanish(GarimpoDaVezStudio.Data.CatalogEntry? entry) =>
        entry is not null &&
        (entry.Category.StartsWith("MeLi 2019 /", StringComparison.OrdinalIgnoreCase) ||
         entry.SourceFile.StartsWith("meli_2019_es_", StringComparison.OrdinalIgnoreCase));

    public string OriginLabel => LocalProduct is not null ? "SALVO NO STUDIO" :
        CatalogReference is not null ? IsHistoricalSpanish(CatalogReference) ? "BASE HISTÓRICA · ES (2019)" :
            CatalogReference.Kind switch {
                "listing" => "ANÚNCIO INDEXADO · BR", "catalog" => "CATÁLOGO INDEXADO · BR",
                _ => "REFERÊNCIA IMPORTADA · ORIGEM A VERIFICAR"
            } :
        OfficialSuggestion?.Kind switch {
            "listing" => "ANÚNCIO OFICIAL · BR", "catalog" => "CATÁLOGO OFICIAL · BR",
            _ => "SUGESTÃO"
        };

    public string ProductTitle => LocalProduct?.Name ??
        (CatalogReference is not null ? ProductTitleFormatter.Suggest(CatalogReference.Name) :
         OfficialSuggestion?.SuggestedName) ?? "";

    public string FullDetails => (CatalogReference?.Name ?? OfficialSuggestion?.Name ?? LocalProduct?.Name ?? "") +
        (CatalogReference is not null ? "\n" + CatalogReference.Category : "");

    public override string ToString() =>
        LocalProduct is not null ? "Salvo · " + LocalProduct.Name :
        CatalogReference is not null ? CatalogReference.Kind switch {
            "listing" => "Anúncio indexado · " + ProductTitle,
            "catalog" => "Catálogo indexado · " + ProductTitle,
            _ => "Base offline · " + ProductTitle
        } : OfficialSuggestion?.Display ?? "";
}
