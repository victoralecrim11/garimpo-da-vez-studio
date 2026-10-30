using GarimpoDaVezStudio.Data;
using GarimpoDaVezStudio.Services;

namespace GarimpoDaVezStudio.UI;

public sealed partial class MainForm
{
    private readonly PictureBox _catalogPhoto = Picture();
    private readonly Label _catalogPhotoStatus = new();
    private readonly System.Windows.Forms.Timer _catalogPhotoTimer = new() { Interval = 650 };
    private int _productPhotoRevision;
    private int _catalogPhotoRevision;
    private DateTime _catalogPhotoApiUnavailableUntil = DateTime.MinValue;

    private Control BuildCatalogPhotoPanel()
    {
        var container = new TableLayoutPanel {
            Dock = DockStyle.Fill, Padding = new Padding(12, 8, 8, 8),
            RowCount = 3, ColumnCount = 1, BackColor = Card
        };
        container.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        container.RowStyles.Add(new RowStyle(SizeType.Percent, 68));
        container.RowStyles.Add(new RowStyle(SizeType.Percent, 32));
        container.Controls.Add(new Label {
            Text = "PRÉVIA DO PRODUTO", Dock = DockStyle.Fill, ForeColor = Foreground,
            Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        _catalogPhoto.Dock = DockStyle.Fill;
        _catalogPhoto.SizeMode = PictureBoxSizeMode.Zoom;
        _catalogPhoto.BackColor = Surface;
        _catalogPhoto.Margin = new Padding(0, 2, 0, 8);
        container.Controls.Add(_catalogPhoto, 0, 1);
        _catalogPhotoStatus.ForeColor = Muted;
        _catalogPhotoStatus.Dock = DockStyle.Fill;
        _catalogPhotoStatus.Padding = new Padding(2, 4, 2, 2);
        _catalogPhotoStatus.Text = "Selecione um produto para ver a imagem oficial, quando disponível.";
        _catalogPhotoStatus.Font = new Font("Segoe UI", 9f);
        container.Controls.Add(_catalogPhotoStatus, 0, 2);
        _catalogPhotoTimer.Tick += async (_, _) => {
            _catalogPhotoTimer.Stop();
            await DisplaySelectedCatalogPhotoAsync();
        };
        FormClosed += (_, _) => _catalogPhotoTimer.Dispose();
        return container;
    }

    private void OnCatalogPhotoSelectionChanged()
    {
        _catalogPhotoRevision++;
        _catalogPhotoTimer.Stop();
        FileStudioService.ShowImage(_catalogPhoto, null);
        var selected = SelectedCatalogEntry();
        _catalogPhotoStatus.Text = selected is null ? "Selecione um produto." :
            SidebarSearchItem.IsHistoricalSpanish(selected)
                ? "Referência histórica (2019/ES): não há foto nem URL atual do anúncio."
                : selected.Kind == "offline"
                    ? "Referência importada: sem foto oficial confirmada."
                    : "Consultando a miniatura oficial, se estiver disponível...";
        if (selected is not null && selected.Kind is "listing" or "catalog")
            _catalogPhotoTimer.Start();
    }

    private async Task DisplaySelectedCatalogPhotoAsync()
    {
        var selected = SelectedCatalogEntry();
        if (selected is null || selected.Kind is not ("listing" or "catalog")) return;
        int revision = _catalogPhotoRevision;
        string key = selected.Key;
        string image = selected.ImageUrl;
        string listing = selected.ListingUrl;
        string extra = "";
        if (string.IsNullOrWhiteSpace(image) &&
            !string.IsNullOrWhiteSpace(selected.ExternalId) &&
            DateTime.UtcNow >= _catalogPhotoApiUnavailableUntil &&
            MercadoLivreBridgeService.ValidRepository(MercadoLivreBridgeService.SavedRepository))
        {
            try
            {
                var result = await MercadoLivreBridgeService.EnrichSuggestionAsync(
                    MercadoLivreBridgeService.SavedRepository,
                    new ProductNameSuggestion {
                        Id = selected.ExternalId, Name = selected.Name, Kind = selected.Kind,
                        CategoryId = selected.CategoryId
                    });
                var details = result.Suggestions.FirstOrDefault();
                if (details is not null) {
                    image = details.Thumbnail;
                    if (!string.IsNullOrWhiteSpace(details.Permalink)) listing = details.Permalink;
                }
                if (string.IsNullOrWhiteSpace(image))
                    extra = "A API não forneceu miniatura para este registro.";
            }
            catch
            {
                extra = "Imagem não disponível na API ou acesso negado (401/403).";
                _catalogPhotoApiUnavailableUntil = DateTime.UtcNow.AddSeconds(60);
            }
        }
        if (revision != _catalogPhotoRevision || IsDisposed || SelectedCatalogEntry()?.Key != key)
            return;
        if (string.IsNullOrWhiteSpace(image))
        {
            _catalogPhotoStatus.Text = extra.Length > 0 ? extra :
                "Sem imagem oficial disponível. Use um anúncio brasileiro verificado.";
            return;
        }
        var ok = await ShowRemoteImageAsync(_catalogPhoto, image,
            () => revision == _catalogPhotoRevision && SelectedCatalogEntry()?.Key == key);
        if (revision == _catalogPhotoRevision && !IsDisposed)
            _catalogPhotoStatus.Text = ok
                ? "Prévia da plataforma, não salva como arquivo autorizado. " +
                    (string.IsNullOrWhiteSpace(listing) ? "Sem URL individual confirmada." :
                    "URL pública do anúncio disponível no cadastro.")
                : "Não foi possível carregar a imagem oficial.";
    }

    private async Task ShowProductSuggestionPreviewAsync(string url)
    {
        int revision = ++_productPhotoRevision;
        await ShowRemoteImageAsync(_originalPreview, url,
            () => revision == _productPhotoRevision &&
                  string.IsNullOrWhiteSpace(_current?.OriginalImagePath));
    }

    private async Task<bool> ShowRemoteImageAsync(PictureBox preview, string urlString,
        Func<bool> isStillCurrent)
    {
        if (!Uri.TryCreate(urlString, UriKind.Absolute, out var url) ||
            url.Scheme != Uri.UriSchemeHttps || url.UserInfo.Length > 0 || !url.IsDefaultPort)
            return false;
        var host = url.IdnHost.ToLowerInvariant();
        if (host != "mlstatic.com" && !host.EndsWith(".mlstatic.com", StringComparison.Ordinal))
            return false;
        try
        {
            using var response = await ThumbnailClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode ||
                response.Content.Headers.ContentLength > 4_000_000) return false;
            var type = response.Content.Headers.ContentType?.MediaType ?? "";
            if (!type.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return false;
            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (bytes.Length > 4_000_000 || bytes.Length == 0) return false;
            using var stream = new MemoryStream(bytes);
            using var decoded = Image.FromStream(stream);
            var bitmap = new Bitmap(decoded);
            if (preview.IsDisposed || IsDisposed || !isStillCurrent()) {
                bitmap.Dispose();
                return false;
            }
            var previous = preview.Image;
            preview.Image = bitmap;
            previous?.Dispose();
            return true;
        }
        catch (Exception) { return false; }
    }
}
