namespace GarimpoDaVezStudio.Models;

public sealed class Product
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Source { get; set; } = "Mercado Livre";
    public string ExternalId { get; set; } = "";
    public string ListingUrl { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string CategoryId { get; set; } = "";
    public string SubcategoryId { get; set; } = "";
    public string Niche { get; set; } = "";
    public string Description { get; set; } = "";
    public string AffiliateUrl { get; set; } = "";
    public string OriginalImagePath { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
    public override string ToString() => Name;
}

public sealed class ImageVersion
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProductId { get; set; } = "";
    public int Version { get; set; }
    public string FilePath { get; set; } = "";
    public string Prompt { get; set; } = "";
    public string Style { get; set; } = "";
    public bool IsApproved { get; set; }
    public string CreatedAt { get; set; } = "";
    public override string ToString() => $"v{Version:000} {(IsApproved ? "★ APROVADA" : "· revisão")}";
}

public sealed class PinDraft
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProductId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Board { get; set; } = "";
    public string AffiliateUrl { get; set; } = "";
    public string ImageVersionId { get; set; } = "";
    public string Status { get; set; } = "Rascunho";
    public string UpdatedAt { get; set; } = "";
}

public sealed class StudioLog
{
    public string OccurredAt { get; set; } = "";
    public string Action { get; set; } = "";
    public string Message { get; set; } = "";
    public override string ToString() => $"{OccurredAt}  |  {Action}  |  {Message}";
}
