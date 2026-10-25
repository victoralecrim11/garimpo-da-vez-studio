using GarimpoDaVezStudio.Models;
using GarimpoDaVezStudio.Services;

namespace GarimpoDaVezStudio.UI;

/// <summary>Busca de nomes no próprio formulário: registros locais, anúncios oficiais e referências de catálogo.</summary>
public sealed partial class MainForm
{
    private readonly System.Windows.Forms.Timer _productNameTimer = new() { Interval = 850 };
    private readonly ListBox _productNameSuggestions = new();
    private readonly Label _productNameStatus = new();
    private readonly List<ProductNameSuggestion> _remoteNameSuggestions = new();
    private bool _suppressProductNameLookup;
    private bool _productNameRequestRunning;
    private long _productNameRevision;
    private DateTime _productNameLookupUnavailableUntil = DateTime.MinValue;

    private void SetupProductNameLookup(FlowLayoutPanel form)
    {
        _productNameSuggestions.Width = 445;
        _productNameSuggestions.Height = 150;
        _productNameSuggestions.BackColor = Card;
        _productNameSuggestions.ForeColor = Foreground;
        _productNameSuggestions.BorderStyle = BorderStyle.FixedSingle;
        _productNameSuggestions.Font = new Font("Segoe UI", 9f);
        _productNameSuggestions.DisplayMember = nameof(ProductNameSuggestion.Display);
        _productNameSuggestions.Visible = false;

        _productNameStatus.Width = 460;
        _productNameStatus.Height = 45;
        _productNameStatus.ForeColor = Muted;
        _productNameStatus.Text = "Digite pelo menos 3 caracteres. Selecione uma sugestão ou continue preenchendo manualmente.";

        var buttons = ButtonRow(465);
        buttons.Controls.Add(ButtonOf("Buscar sugestões", async () => await SearchProductNameAsync(force: true), true, 195));
        buttons.Controls.Add(ButtonOf("Preencher pela URL", async () => await PreviewProductUrlAsync(), false, 210));
        form.Controls.Add(buttons);
        form.Controls.Add(_productNameSuggestions);
        form.Controls.Add(_productNameStatus);

        _name.TextChanged += (_, _) =>
        {
            if (_suppressProductNameLookup) return;
            _productNameTimer.Stop();
            _productNameRevision++;
            _remoteNameSuggestions.Clear();
            RenderProductSuggestions();
            if (_name.Text.Trim().Length >= 3 &&
                DateTime.UtcNow >= _productNameLookupUnavailableUntil)
                _productNameTimer.Start();
        };
        _productNameTimer.Tick += async (_, _) =>
        {
            _productNameTimer.Stop();
            await SearchProductNameAsync(force: false);
        };
        _name.KeyDown += async (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            await SearchProductNameAsync(force: true);
        };
        _productNameSuggestions.MouseClick += (_, _) => ApplySelectedProductSuggestion();
        _productNameSuggestions.KeyDown += (_, e) =>
        {
            if (e.KeyCode is not Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            ApplySelectedProductSuggestion();
        };
        FormClosed += (_, _) => _productNameTimer.Dispose();
        RenderProductSuggestions();
    }

    private void RenderProductSuggestions()
    {
        var term = _name.Text.Trim();
        var matches = term.Length < 2
            ? new List<ProductNameSuggestion>()
            : _repository.GetProducts()
                .Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Take(7)
                .Select(p => new ProductNameSuggestion {
                    Name = p.Name, Id = p.ExternalId, Kind = "local", LocalProductId = p.Id
                }).ToList();
        matches.AddRange(_remoteNameSuggestions.Where(s =>
            !matches.Any(x => x.Id.Length > 0 && x.Id == s.Id && s.Kind == "listing")));
        _productNameSuggestions.DataSource = null;
        _productNameSuggestions.DataSource = matches;
        _productNameSuggestions.SelectedIndex = -1;
        _productNameSuggestions.Visible = matches.Count > 0;
    }

    private async Task SearchProductNameAsync(bool force)
    {
        var term = _name.Text.Trim();
        if (term.Length < 3)
        {
            _productNameStatus.Text = "Informe pelo menos 3 caracteres para pesquisar.";
            return;
        }
        if (_productNameRequestRunning) return;
        if (!force && DateTime.UtcNow < _productNameLookupUnavailableUntil) return;
        var root = MercadoLivreBridgeService.SavedRepository;
        if (!MercadoLivreBridgeService.ValidRepository(root))
        {
            _productNameStatus.Text = "Selecione o repositório Python na aba Garimpo. Sugestões locais continuam disponíveis.";
            return;
        }
        var category = (_productSubcategory.SelectedItem as MercadoLivreCategory)?.Id;
        if (string.IsNullOrWhiteSpace(category))
            category = (_productCategory.SelectedItem as MercadoLivreCategory)?.Id;

        long revision = _productNameRevision;
        _productNameRequestRunning = true;
        _productNameStatus.Text = "Buscando nomes na API oficial do Mercado Livre...";
        try
        {
            var result = await MercadoLivreBridgeService.SuggestNamesAsync(root, term, category ?? "");
            if (revision != _productNameRevision) return;
            _remoteNameSuggestions.Clear();
            _remoteNameSuggestions.AddRange(result.Suggestions.Take(12));
            RenderProductSuggestions();
            _productNameStatus.Text = result.Origin switch
            {
                "anuncios_oficiais" => "Anúncios oficiais encontrados. Clique em um para preencher o cadastro.",
                "catalogo_referencia" => "Resultados de catálogo: servem para o nome, não substituem um anúncio ou link de afiliado.",
                _ => string.IsNullOrWhiteSpace(result.Warning)
                    ? "Sem sugestões remotas. Você pode digitar o nome normalmente."
                    : result.Warning
            };
            if (result.Origin == "indisponivel")
                _productNameLookupUnavailableUntil = DateTime.UtcNow.AddMinutes(2);
        }
        catch (Exception ex)
        {
            if (revision == _productNameRevision)
                _productNameStatus.Text = "Busca oficial indisponível: " + ex.Message +
                    " Utilize uma URL real ou digite os dados. Sugestões locais permanecem disponíveis.";
            _productNameLookupUnavailableUntil = DateTime.UtcNow.AddMinutes(2);
        }
        finally
        {
            _productNameRequestRunning = false;
            if (revision != _productNameRevision && _name.Text.Trim().Length >= 3
                && DateTime.UtcNow >= _productNameLookupUnavailableUntil)
                _productNameTimer.Start();
        }
    }

    private void ApplySelectedProductSuggestion()
    {
        if (_productNameSuggestions.SelectedItem is not ProductNameSuggestion suggestion) return;
        _productNameTimer.Stop();
        _productNameRevision++;
        if (suggestion.Kind == "local")
        {
            var saved = _repository.GetProducts().FirstOrDefault(p => p.Id == suggestion.LocalProductId);
            if (saved is not null)
            {
                LoadProduct(saved);
                _productNameStatus.Text = "Cadastro local carregado para edição; informações existentes preservadas.";
            }
            return;
        }

        NewProduct(); // Nunca mistura link de afiliado/imagem de produtos diferentes.
        _suppressProductNameLookup = true;
        _name.Text = suggestion.Name.Length > 180 ? suggestion.Name[..180] : suggestion.Name;
        _suppressProductNameLookup = false;
        _source.Text = "Mercado Livre";
        _pinTitle.Text = _name.Text.Length > 100 ? _name.Text[..100] : _name.Text;
        if (suggestion.Kind == "listing")
        {
            _externalId.Text = suggestion.Id;
            _listingUrl.Text = ProductInputValidator.IsMercadoLivreListing(suggestion.Permalink)
                ? suggestion.Permalink : "";
            if (!string.IsNullOrWhiteSpace(suggestion.Description))
                _description.Text = suggestion.Description;
            if (_current is not null) _current.SubcategoryId = suggestion.CategoryId;
        }
        _remoteNameSuggestions.Clear();
        _productNameSuggestions.Visible = false;
        _productNameStatus.Text = suggestion.Kind == "catalog"
            ? "Título preenchido a partir do catálogo. Falta escolher um anúncio real e gerar o link de afiliado."
            : "Nome, ID e URL preenchidos. Confira categoria, descrição e fotografia antes de salvar.";
        _ = ShowProductSuggestionPreviewAsync(suggestion.Thumbnail);
        if (suggestion.Kind == "listing")
            _ = SelectOfficialCategoryAsync(suggestion.CategoryId, _current?.Id ?? "");
    }

    private async Task SelectOfficialCategoryAsync(string categoryId, string productId)
    {
        if (string.IsNullOrWhiteSpace(categoryId)) return;
        var root = MercadoLivreBridgeService.SavedRepository;
        if (!MercadoLivreBridgeService.ValidRepository(root)) return;
        try
        {
            var result = await MercadoLivreBridgeService.CategoryPathAsync(root, categoryId);
            if (_current?.Id != productId || result.Categories.Count == 0) return;
            var first = result.Categories[0];
            var last = result.Categories[^1];
            _suspendProductCategoryEvents = true;
            try
            {
                var category = _productCategory.Items.Cast<MercadoLivreCategory>()
                    .FirstOrDefault(x => x.Id == first.Id);
                if (category is null)
                {
                    category = first;
                    _productCategory.Items.Add(category);
                }
                _productCategory.SelectedItem = category;
                _productSubcategory.Items.Clear();
                _productSubcategory.Items.Add(new MercadoLivreCategory { Name = "Sem subcategoria" });
                if (last.Id != first.Id)
                {
                    _productSubcategory.Items.Add(last);
                    _productSubcategory.SelectedIndex = 1;
                }
                else _productSubcategory.SelectedIndex = 0;
                _category.Text = last.Name;
                _current.CategoryId = first.Id;
                _current.SubcategoryId = last.Id != first.Id ? last.Id : "";
                FillNicheSuggestions(_nicheOptions.Text);
                _productNameStatus.Text += " Categoria oficial identificada: " + last.Name + ".";
            }
            finally { _suspendProductCategoryEvents = false; }
        }
        catch (Exception)
        {
            // Se a categoria estiver restrita, é possível escolher manualmente no formulário.
        }
    }

    private async Task PreviewProductUrlAsync()
    {
        var url = _listingUrl.Text.Trim();
        if (!ProductInputValidator.IsMercadoLivreListing(url, required: true))
        {
            MessageBox.Show("Cole uma URL HTTPS de anúncio do Mercado Livre no campo 'URL do anúncio original'.");
            return;
        }
        if (_productNameRequestRunning) return;
        var root = MercadoLivreBridgeService.SavedRepository;
        if (!MercadoLivreBridgeService.ValidRepository(root))
        {
            _productNameStatus.Text = "Configure o repositório Python na aba Garimpo antes de consultar a URL.";
            return;
        }
        _productNameTimer.Stop();
        _productNameRevision++;
        _productNameRequestRunning = true;
        _productNameStatus.Text = "Obtendo metadados do anúncio pela URL informada...";
        try
        {
            var result = await MercadoLivreBridgeService.PreviewByUrlAsync(root, url);
            var suggestion = result.Suggestions.FirstOrDefault();
            if (suggestion is null)
                throw new InvalidOperationException("Nenhum dado disponível para esta URL.");
            // A origem do anúncio já foi informada pelo usuário; não entra na fila Python.
            ApplySuggestionFromUrl(suggestion, url);
            _productNameStatus.Text = suggestion.Verified
                ? "Dados retornados pela API oficial. Confira atributos e link de afiliado."
                : "Dados parciais da página pública: confira todos os detalhes antes de salvar.";
        }
        catch (Exception ex)
        {
            _productNameStatus.Text = "Não foi possível obter metadados: " + ex.Message;
        }
        finally { _productNameRequestRunning = false; }
    }

    private void ApplySuggestionFromUrl(ProductNameSuggestion suggestion, string originalUrl)
    {
        var saved = !string.IsNullOrWhiteSpace(suggestion.Id)
            ? _repository.FindMercadoLivreItem(suggestion.Id) : null;
        if (saved is not null) LoadProduct(saved);
        else NewProduct();
        _suppressProductNameLookup = true;
        _name.Text = suggestion.Name.Length > 180 ? suggestion.Name[..180] : suggestion.Name;
        _suppressProductNameLookup = false;
        _source.Text = "Mercado Livre";
        _listingUrl.Text = originalUrl;
        if (!string.IsNullOrWhiteSpace(suggestion.Id)) _externalId.Text = suggestion.Id;
        if (!string.IsNullOrWhiteSpace(suggestion.Description) &&
            (_description.Text.Trim().Length == 0))
            _description.Text = suggestion.Description;
        if (_current is not null && !string.IsNullOrWhiteSpace(suggestion.CategoryId))
            _current.SubcategoryId = suggestion.CategoryId;
        _pinTitle.Text = _name.Text.Length > 100 ? _name.Text[..100] : _name.Text;
        _remoteNameSuggestions.Clear();
        _productNameSuggestions.Visible = false;
        _ = ShowProductSuggestionPreviewAsync(suggestion.Thumbnail);
        _ = SelectOfficialCategoryAsync(suggestion.CategoryId, _current?.Id ?? "");
    }

    private async Task ShowProductSuggestionPreviewAsync(string urlString)
    {
        // Apenas prévia temporária na memória. Não equivale à autorização para baixar ou divulgar.
        if (!Uri.TryCreate(urlString, UriKind.Absolute, out var url) ||
            url.Scheme != Uri.UriSchemeHttps || url.UserInfo.Length > 0 || !url.IsDefaultPort)
            return;
        var host = url.IdnHost.ToLowerInvariant();
        if (host != "mlstatic.com" && !host.EndsWith(".mlstatic.com", StringComparison.Ordinal)) return;
        try
        {
            using var response = await ThumbnailClient.GetAsync(url,
                HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > 4_000_000)
                return;
            var mime = response.Content.Headers.ContentType?.MediaType ?? "";
            if (!mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return;
            var buffer = await response.Content.ReadAsByteArrayAsync();
            if (buffer.Length > 4_000_000) return;
            using var data = new MemoryStream(buffer);
            using var image = Image.FromStream(data);
            var copy = new Bitmap(image);
            if (_originalPreview.IsDisposed) { copy.Dispose(); return; }
            var previous = _originalPreview.Image;
            _originalPreview.Image = copy;
            previous?.Dispose();
        }
        catch (Exception) { /* Prévia opcional; não altera o cadastro. */ }
    }
}
