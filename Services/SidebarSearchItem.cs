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

    public override string ToString()
    {
        if (LocalProduct is not null) return "Salvo · " + LocalProduct.Name;
        return OfficialSuggestion?.Display ?? "";
    }
}
