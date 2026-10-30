using System.Text.RegularExpressions;

namespace GarimpoDaVezStudio.Services;

/// <summary>Garante que uma URL pública pertence ao mesmo anúncio individual MLB.
/// Produtos de catálogo MLBU não se tornam anúncios automaticamente.</summary>
public static class OfficialListingMatcher
{
    private static readonly Regex ListingId = new(
        @"\AMLB\d{6,18}\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant |
        RegexOptions.Compiled);
    private static readonly Regex PathId = new(
        @"(?:^|[/_-])MLB-?(\d{6,18})(?=$|[/_?\-])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool TryGetVerifiedPermalink(MercadoLivreListing? item, out string url)
    {
        url = "";
        if (item is null || !ListingId.IsMatch(item.Id) ||
            !ProductInputValidator.IsMercadoLivreListing(item.Permalink, true) ||
            !Uri.TryCreate(item.Permalink, UriKind.Absolute, out var parsed))
            return false;
        var match = PathId.Match(parsed.AbsolutePath);
        if (!match.Success || !string.Equals("MLB" + match.Groups[1].Value, item.Id,
                StringComparison.OrdinalIgnoreCase))
            return false;
        url = item.Permalink;
        return true;
    }
}
