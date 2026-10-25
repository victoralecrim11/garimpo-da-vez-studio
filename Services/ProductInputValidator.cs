using System.Text.RegularExpressions;

namespace GarimpoDaVezStudio.Services;

/// <summary>Validação de entradas da interface; nunca confunde URL do anúncio com link comissionado.</summary>
public static class ProductInputValidator
{
    private static readonly Regex ExternalIdPattern = new(
        @"\A[A-Za-z0-9][A-Za-z0-9_.-]{0,63}\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool IsValidExternalId(string? value) =>
        string.IsNullOrWhiteSpace(value) || ExternalIdPattern.IsMatch(value.Trim());

    public static bool IsValidClientId(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length is >= 5 and <= 30 && value.All(c => c is >= '0' and <= '9');

    public static bool IsValidHttpsUrl(string? value, bool required = false)
    {
        if (string.IsNullOrWhiteSpace(value)) return !required;
        if (value.Length > 2048 || value != value.Trim() ||
            !Uri.TryCreate(value, UriKind.Absolute, out var url))
            return false;
        return url.Scheme == Uri.UriSchemeHttps && url.UserInfo.Length == 0 &&
               url.IsDefaultPort && url.Host.Length > 0 && url.HostNameType == UriHostNameType.Dns;
    }

    private static bool HostMatches(Uri url, string hostname) =>
        url.IdnHost.Equals(hostname, StringComparison.OrdinalIgnoreCase) ||
        url.IdnHost.EndsWith("." + hostname, StringComparison.OrdinalIgnoreCase);

    public static bool IsMercadoLivreListing(string? value, bool required = false) =>
        (string.IsNullOrWhiteSpace(value) && !required) ||
        (IsValidHttpsUrl(value, true) && Uri.TryCreate(value, UriKind.Absolute, out var url) &&
         (HostMatches(url, "mercadolivre.com.br") || HostMatches(url, "mercadolibre.com.br")));

    public static bool IsMercadoLivreAffiliate(string? value, bool required = false) =>
        (string.IsNullOrWhiteSpace(value) && !required) ||
        (IsValidHttpsUrl(value, true) && Uri.TryCreate(value, UriKind.Absolute, out var url) &&
         (url.IdnHost.Equals("meli.la", StringComparison.OrdinalIgnoreCase) ||
          HostMatches(url, "mercadolivre.com.br") || HostMatches(url, "mercadolibre.com.br")));

    public static bool HasValidContent(string? value, int maxLength, bool required = false) =>
        value is not null && value.Trim().Length <= maxLength && (!required || value.Trim().Length > 0);
}
