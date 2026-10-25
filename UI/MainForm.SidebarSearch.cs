using GarimpoDaVezStudio.Models;
using GarimpoDaVezStudio.Services;

namespace GarimpoDaVezStudio.UI;

public sealed partial class MainForm
{
    private readonly System.Windows.Forms.Timer _sidebarSearchTimer = new() { Interval = 900 };
    private readonly Label _sidebarSearchStatus = new();
    private readonly List<ProductNameSuggestion> _sidebarOfficialSuggestions = new();
    private long _sidebarSearchRevision;
    private bool _sidebarSearchRunning;
    private DateTime _sidebarUnavailableUntil = DateTime.MinValue;

    private void ConfigureSidebarSearch(TableLayoutPanel sidebar)
    {
        _sidebarSearchStatus.Text = "Produtos salvos no Studio. Digite 3 letras para buscar também no Mercado Livre.";
        _sidebarSearchStatus.ForeColor = Muted;
        _sidebarSearchStatus.BackColor = Surface;
        _sidebarSearchStatus.Dock = DockStyle.Bottom;
        _sidebarSearchStatus.Height = 75;
        _sidebarSearchStatus.Padding = new Padding(3, 8, 3, 3);
        _sidebarSearchStatus.Font = new Font("Segoe UI", 9f);

        var wrap = new Panel { Dock = DockStyle.Fill, BackColor = Surface };
        wrap.Controls.Add(_products);
        wrap.Controls.Add(_sidebarSearchStatus);
        sidebar.Controls.Add(wrap, 0, 2);

        _sidebarSearchTimer.Tick += async (_, _) =>
        {
            _sidebarSearchTimer.Stop();
            await SearchSidebarOnlineAsync();
        };
        FormClosed += (_, _) => _sidebarSearchTimer.Dispose();
    }

    private void SidebarSearchTextChanged()
    {
        _sidebarSearchTimer.Stop();
        _sidebarSearchRevision++;
        _sidebarOfficialSuggestions.Clear();
        ReloadProducts(_current?.Id);
        if (_search.Text.Trim().Length >= 3)
        {
            if (DateTime.UtcNow >= _sidebarUnavailableUntil)
                _sidebarSearchTimer.Start();
            else
                _sidebarSearchStatus.Text = "API temporariamente indisponível; confira produtos salvos ou use a URL do anúncio.";
        }
    }

    private async Task SearchSidebarOnlineAsync()
    {
        var query = _search.Text.Trim();
        if (query.Length < 3 || _sidebarSearchRunning ||
            DateTime.UtcNow < _sidebarUnavailableUntil) return;

        var root = MercadoLivreBridgeService.SavedRepository;
        if (!MercadoLivreBridgeService.ValidRepository(root))
        {
            _sidebarSearchStatus.Text =
                "Busca online não configurada. Selecione a pasta Python na aba 02 · Garimpo Mercado Livre.";
            return;
        }

        var revision = _sidebarSearchRevision;
        _sidebarSearchRunning = true;
        _sidebarSearchStatus.Text = "Pesquisando no Mercado Livre...";
        try
        {
            // Sem filtro de categoria: a lateral é uma busca global.
            // Só recebe resultados oficiais; não cria produtos no banco.
            var result = await MercadoLivreBridgeService.SuggestNamesAsync(root, query, "");
            if (revision != _sidebarSearchRevision || IsDisposed) return;
            _sidebarOfficialSuggestions.Clear();
            _sidebarOfficialSuggestions.AddRange(result.Suggestions.Take(12));
            ReloadProducts(_current?.Id);
            _sidebarSearchStatus.Text = result.Origin switch
            {
                "anuncios_oficiais" =>
                    "Salvo = SQLite; Anúncio = resultado oficial. Clique para abrir o cadastro sem salvar.",
                "catalogo_referencia" =>
                    "Referências de catálogo não são anúncios nem geram link de afiliado. Clique para revisar.",
                "indisponivel" =>
                    "Busca oficial negada ou indisponível. Use a URL do anúncio na aba Produto.",
                _ => _sidebarOfficialSuggestions.Count == 0
                    ? "Nenhum produto encontrado. Revise o termo ou importe pela URL."
                    : "Selecione um resultado para abrir o cadastro."
            };
            if (!string.IsNullOrWhiteSpace(result.Warning))
                _footer.Text = result.Warning;
            if (result.Origin == "indisponivel")
                _sidebarUnavailableUntil = DateTime.UtcNow.AddMinutes(2);
        }
        catch (Exception ex)
        {
            if (revision == _sidebarSearchRevision && !IsDisposed)
                _sidebarSearchStatus.Text = "Busca online indisponível: " + ex.Message +
                    " Use a URL do anúncio ou cadastre manualmente.";
            _sidebarUnavailableUntil = DateTime.UtcNow.AddMinutes(2);
        }
        finally
        {
            _sidebarSearchRunning = false;
            if (revision != _sidebarSearchRevision && !IsDisposed &&
                _search.Text.Trim().Length >= 3 &&
                DateTime.UtcNow >= _sidebarUnavailableUntil)
                _sidebarSearchTimer.Start();
        }
    }

    private void ChooseSidebarItem(SidebarSearchItem selected)
    {
        if (selected.LocalProduct is Product product)
        {
            LoadProduct(product);
            _tabs.SelectedIndex = 0;
            return;
        }

        if (selected.OfficialSuggestion is not ProductNameSuggestion suggestion) return;
        // Limpar o filtro lateral ANTES de abrir o cadastro evita mostrar o
        // resultado oficial como se fosse um produto salvo no SQLite.
        _search.Clear();
        ApplySelectedProductSuggestion(suggestion);
        _tabs.SelectedIndex = 0;
        _sidebarSearchStatus.Text =
            "Sugestão carregada na aba Produto. Revise os dados e clique Salvar para cadastrá-la.";
    }
}
