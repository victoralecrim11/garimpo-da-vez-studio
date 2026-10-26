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

    public override string ToString()
    {
        if (LocalProduct is not null) return "Salvo · " + LocalProduct.Name;
        return CatalogReference is not null
            ? CatalogReference.Kind switch {
                "listing" => "Anúncio indexado · " + ProductTitleFormatter.Suggest(CatalogReference.Name),
                "catalog" => "Catálogo indexado · " + ProductTitleFormatter.Suggest(CatalogReference.Name),
                _ => "Base offline · " + CatalogReference.Name
              }
            : OfficialSuggestion?.Display ?? "";
    }
}
