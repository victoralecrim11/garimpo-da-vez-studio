using System.Diagnostics;
using GarimpoDaVezStudio.Data;
using GarimpoDaVezStudio.Models;
using GarimpoDaVezStudio.Services;

namespace GarimpoDaVezStudio.UI;

public sealed class MainForm : Form
{
    private static readonly Color Background = Color.FromArgb(15, 23, 39);
    private static readonly Color Surface = Color.FromArgb(24, 34, 52);
    private static readonly Color Card = Color.FromArgb(31, 45, 66);
    private static readonly Color Foreground = Color.FromArgb(235, 242, 250);
    private static readonly Color Muted = Color.FromArgb(160, 176, 195);
    private static readonly Color Accent = Color.FromArgb(40, 160, 230);

    private readonly StudioRepository _repository;
    private Product? _current;
    private bool _reloadingProducts;

    private readonly ListBox _products = new();
    private readonly TextBox _search = new();
    private readonly TabControl _tabs = new();
    private readonly Label _footer = new();

    private readonly TextBox _name = TextInput();
    private readonly TextBox _source = TextInput();
    private readonly TextBox _externalId = TextInput();
    private readonly TextBox _category = TextInput();
    private readonly ComboBox _nicheOptions = new();
    private readonly ComboBox _productCategory = new();
    private readonly ComboBox _productSubcategory = new();
    private bool _suspendProductCategoryEvents;
    private readonly TextBox _description = TextInput(multiline: true);
    private readonly TextBox _affiliateUrl = TextInput();
    private readonly TextBox _listingUrl = TextInput();
    private readonly TextBox _mlRoot = TextInput();
    private readonly TextBox _mlQuery = TextInput();
    private readonly ComboBox _mlCategories = new();
    private readonly ComboBox _mlChildren = new();
    private readonly DataGridView _mlResults = new();
    private readonly PictureBox _mlPreview = Picture();
    private readonly TextBox _mlManualUrl = TextInput();
    private int _mlPreviewGeneration;
    private static readonly HttpClient ThumbnailClient = new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(12)
    };
    private readonly CheckBox _mlSyncQueue = new();
    private readonly Label _mlStatus = new();
    private bool _mlLoadingCategories;
    private bool _mlBusy;
    private readonly TextBox _originalPath = TextInput();
    private readonly PictureBox _originalPreview = Picture();
    private readonly PictureBox _studioPreview = Picture();

    private readonly ComboBox _style = SelectBox(new[] {
        "Automático", "Minimalista", "Sofisticado", "Aconchegante", "Moderno", "Rústico", "Urbano" });
    private readonly ComboBox _scene = SelectBox(new[] {
        "Adequado ao produto", "Ambiente decorado", "Fotografia de estúdio",
        "Situação de uso real", "Composição editorial" });
    private readonly RichTextBox _prompt = new();
    private readonly Label _studioStatus = new();

    private readonly ListBox _versions = new();
    private readonly PictureBox _versionPreview = Picture();
    private readonly Label _versionStatus = new();

    private readonly TextBox _pinTitle = TextInput();
    private readonly TextBox _pinDescription = TextInput(multiline: true);
    private readonly TextBox _pinBoard = TextInput();
    private readonly TextBox _pinUrl = TextInput();
    private readonly Label _pinStatus = new();
    private readonly ListBox _logs = new();

    public MainForm(StudioRepository repository)
    {
        _repository = repository;
        Text = "Garimpo da Vez Studio · Produtos e Pins";
        Width = 1440;
        Height = 900;
        MinimumSize = new Size(1250, 760);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Background;
        ForeColor = Foreground;
        Font = new Font("Segoe UI", 10f);
        BuildInterface();
        ReloadProducts();
    }

    private void BuildInterface()
    {
        var shell = new TableLayoutPanel {
            Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1,
            BackColor = Background
        };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        Controls.Add(shell);
        var header = new Panel { Dock = DockStyle.Fill, BackColor = Background };
        shell.Controls.Add(header, 0, 0);
        header.Controls.Add(new Label {
            Text = "AFFILIATE  /  STUDIO", ForeColor = Foreground,
            Font = new Font("Segoe UI Semibold", 20f, FontStyle.Bold),
            Left = 22, Top = 11, AutoSize = true
        });
        header.Controls.Add(new Label {
            Text = "Garimpo  ·  Imagens IA  ·  Rascunhos Pinterest",
            ForeColor = Muted, Left = 25, Top = 55, AutoSize = true
        });
        var export = ButtonOf("Exportar Excel (.xlsx)", ExportExcel, true);
        export.Width = 200;
        export.Dock = DockStyle.Right;
        export.Margin = new Padding(0, 17, 20, 17);
        var exportWrap = new Panel { Dock = DockStyle.Right, Width = 220, Padding = new Padding(0, 20, 20, 20) };
        exportWrap.Controls.Add(export);
        header.Controls.Add(exportWrap);

        var footer = new Panel { Dock = DockStyle.Fill, BackColor = Surface };
        _footer.ForeColor = Muted;
        _footer.Dock = DockStyle.Fill;
        _footer.Padding = new Padding(15, 10, 10, 0);
        _footer.Text = "Pronto. Dados salvos localmente.";
        footer.Controls.Add(_footer);
        shell.Controls.Add(footer, 0, 2);

        var main = new TableLayoutPanel {
            Dock = DockStyle.Fill, BackColor = Background,
            ColumnCount = 2, Padding = new Padding(12, 0, 12, 12)
        };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.Controls.Add(main, 0, 1);

        var sidebar = new TableLayoutPanel {
            Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(12),
            ColumnCount = 1, RowCount = 4
        };
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        main.Controls.Add(sidebar, 0, 0);

        var title = new Label {
            Text = "PRODUTOS", ForeColor = Foreground,
            Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
        };
        sidebar.Controls.Add(title, 0, 0);

        _search.Dock = DockStyle.Fill;
        _search.PlaceholderText = "Buscar produto ou nicho...";
        StyleInput(_search);
        _search.TextChanged += (_, _) => ReloadProducts(_current?.Id);
        sidebar.Controls.Add(_search, 0, 1);

        _products.Dock = DockStyle.Fill;
        _products.BackColor = Surface;
        _products.ForeColor = Foreground;
        _products.BorderStyle = BorderStyle.None;
        _products.Font = new Font("Segoe UI", 11f);
        _products.ItemHeight = 37;
        _products.DisplayMember = nameof(Product.Name);
        _products.SelectedIndexChanged += (_, _) =>
        {
            if (!_reloadingProducts && _products.SelectedItem is Product p) LoadProduct(p);
        };
        sidebar.Controls.Add(_products, 0, 2);

        var actions = new FlowLayoutPanel {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false, BackColor = Surface
        };
        actions.Controls.Add(ButtonOf("+ Novo", NewProduct, true, 115));
        actions.Controls.Add(ButtonOf("Salvar", () => SaveCurrent(), false, 115));
        sidebar.Controls.Add(actions, 0, 3);

        _tabs.Dock = DockStyle.Fill;
        _tabs.Font = new Font("Segoe UI Semibold", 10f);
        _tabs.Padding = new Point(22, 11);
        main.Controls.Add(_tabs, 1, 0);
        _tabs.TabPages.Add(BuildProductTab());
        _tabs.TabPages.Add(BuildMarketplaceTab());
        _tabs.TabPages.Add(BuildStudioTab());
        _tabs.TabPages.Add(BuildVersionsTab());
        _tabs.TabPages.Add(BuildPinTab());
        _tabs.TabPages.Add(BuildHistoryTab());
    }

    private TabPage BuildProductTab()
    {
        var page = Page("01 · Produto");
        var cols = TwoColumns(56);
        page.Controls.Add(cols);
        var form = Stack();
        cols.Controls.Add(form, 0, 0);
        form.Controls.Add(Section("DADOS DO PRODUTO"));
        form.Controls.Add(Field("Nome do produto *", _name));
        form.Controls.Add(Field("Fonte", _source));
        form.Controls.Add(Field("ID externo / SKU (opcional)", _externalId));
        SetupProductCategoryFields();
        form.Controls.Add(Field("Categoria oficial do Mercado Livre", _productCategory));
        form.Controls.Add(Field("Subcategoria oficial (opcional)", _productSubcategory));
        form.Controls.Add(ButtonOf("Carregar categorias oficiais", async () => await LoadMlCategoriesAsync(), false, 260));
        form.Controls.Add(Field("Nicho editorial (sugestões editáveis)", _nicheOptions));
        form.Controls.Add(Field("Descrição real do produto", _description, 132));
        form.Controls.Add(Field("Link oficial do afiliado", _affiliateUrl));
        _listingUrl.ReadOnly = true;
        form.Controls.Add(Field("URL do anúncio original (não comissionada)", _listingUrl));
        var buttons = ButtonRow(468);
        buttons.Controls.Add(ButtonOf("Salvar produto", () => SaveCurrent(), true));
        buttons.Controls.Add(ButtonOf("Selecionar foto", ImportOriginal));
        form.Controls.Add(buttons);
        var right = Stack();
        cols.Controls.Add(right, 1, 0);
        right.Controls.Add(Section("IMAGEM ORIGINAL"));
        _originalPreview.Size = new Size(350, 420);
        _originalPreview.Cursor = Cursors.Hand;
        _originalPreview.Click += (_, _) => ImportOriginal();
        right.Controls.Add(_originalPreview);
        _originalPath.ReadOnly = true;
        _originalPath.Cursor = Cursors.Hand;
        _originalPath.Click += (_, _) => ImportOriginal();
        right.Controls.Add(Field("Arquivo salvo (clique para escolher outra imagem)", _originalPath));
        right.Controls.Add(Note("O ID interno é criado automaticamente. O link de afiliado deve vir da ferramenta oficial da plataforma."));
        return page;
    }


    private TabPage BuildMarketplaceTab()
    {
        var page = Page("02 · Garimpo Mercado Livre");
        var panel = Stack();
        panel.Dock = DockStyle.Fill;
        panel.Controls.Add(Section("CATÁLOGO OFICIAL · MERCADO LIVRE"));
        _mlRoot.Text = MercadoLivreBridgeService.SavedRepository;
        panel.Controls.Add(Field("Caminho para a pasta do projeto Python garimpo-da-vez", _mlRoot));
        var setup = ButtonRow(810);
        setup.Controls.Add(ButtonOf("Selecionar pasta Python", ChoosePythonRoot, false, 205));
        setup.Controls.Add(ButtonOf("Carregar categorias", async () => await LoadMlCategoriesAsync(), true, 205));
        panel.Controls.Add(setup);

        _mlQuery.PlaceholderText = "Ex.: luminária LED, organizador, notebook gamer...";
        panel.Controls.Add(Field("Palavras-chave", _mlQuery));
        foreach (var combo in new[] { _mlCategories, _mlChildren })
        {
            combo.Width = 445;
            combo.Height = 37;
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.BackColor = Card;
            combo.ForeColor = Foreground;
        }
        _mlCategories.Items.Add(new MercadoLivreCategory { Name = "Todas as categorias" });
        _mlCategories.SelectedIndex = 0;
        _mlChildren.Items.Add(new MercadoLivreCategory { Name = "Todas as subcategorias" });
        _mlChildren.SelectedIndex = 0;
        _mlCategories.SelectedIndexChanged += async (_, _) => await LoadMlChildrenAsync();
        panel.Controls.Add(Field("Categoria oficial", _mlCategories));
        panel.Controls.Add(Field("Subcategoria (opcional)", _mlChildren));

        var searchRow = ButtonRow(810);
        searchRow.Controls.Add(ButtonOf("Pesquisar anúncios", async () => await SearchMlAsync(), true, 210));
        searchRow.Controls.Add(ButtonOf("Abrir anúncio selecionado", OpenSelectedMlListing, false, 225));
        panel.Controls.Add(searchRow);

        _mlResults.Width = 930;
        _mlResults.Height = 275;
        _mlResults.BackgroundColor = Card;
        _mlResults.ForeColor = Color.Black;
        _mlResults.ReadOnly = true;
        _mlResults.AllowUserToAddRows = false;
        _mlResults.AllowUserToDeleteRows = false;
        _mlResults.MultiSelect = false;
        _mlResults.RowHeadersVisible = false;
        _mlResults.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _mlResults.AutoGenerateColumns = false;
        _mlResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "Id", Width = 125 });
        _mlResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Produto / anúncio", DataPropertyName = "Title", Width = 420 });
        _mlResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Categoria", DataPropertyName = "CategoryId", Width = 125 });
        _mlResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Preço (informativo)", DataPropertyName = "Price", Width = 135, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _mlResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Moeda", DataPropertyName = "Currency", Width = 80 });
        _mlResults.Width = 655;
        _mlResults.Height = 260;
        _mlResults.SelectionChanged += async (_, _) => await ShowMlPreviewAsync();
        var gridWithPreview = new FlowLayoutPanel {
            Width = 1000, Height = 280, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Surface
        };
        gridWithPreview.Controls.Add(_mlResults);
        _mlPreview.Width = 280;
        _mlPreview.Height = 260;
        _mlPreview.SizeMode = PictureBoxSizeMode.Zoom;
        gridWithPreview.Controls.Add(_mlPreview);
        panel.Controls.Add(gridWithPreview);
        panel.Controls.Add(Field("Busca bloqueada (401/403)? Cole a URL REAL do anúncio", _mlManualUrl));
        panel.Controls.Add(ButtonOf("Importar diretamente pela URL", async () => await ImportMlUrlAsync(), false, 300));

        _mlSyncQueue.Text = "Também registrar na fila Python (pins.json / Excel), sem aprovar imagem ou link";
        _mlSyncQueue.Checked = true;
        _mlSyncQueue.Width = 790;
        _mlSyncQueue.Height = 32;
        _mlSyncQueue.ForeColor = Foreground;
        panel.Controls.Add(_mlSyncQueue);
        panel.Controls.Add(ButtonOf("Importar anúncio selecionado", async () => await ImportMlSelectedAsync(), true, 280));
        _mlStatus.ForeColor = Muted;
        _mlStatus.Width = 900;
        _mlStatus.Height = 55;
        _mlStatus.Text = "A API pode negar busca com HTTP 401/403. Nenhum link de afiliado é criado automaticamente.";
        panel.Controls.Add(_mlStatus);
        page.Controls.Add(panel);
        return page;
    }


    private void SetupProductCategoryFields()
    {
        foreach (var combo in new[] { _productCategory, _productSubcategory })
        {
            combo.Width = 445;
            combo.Height = 37;
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.BackColor = Card;
            combo.ForeColor = Foreground;
        }
        _productCategory.Items.Add(new MercadoLivreCategory { Name = "Carregar categorias oficiais para selecionar" });
        _productCategory.SelectedIndex = 0;
        _productSubcategory.Items.Add(new MercadoLivreCategory { Name = "Selecione primeiro uma categoria" });
        _productSubcategory.SelectedIndex = 0;
        _productCategory.SelectedIndexChanged += async (_, _) =>
        {
            if (_suspendProductCategoryEvents) return;
            var selected = _productCategory.SelectedItem as MercadoLivreCategory;
            if (selected is { Id.Length: > 0 })
            {
                _category.Text = selected.Name;
                FillNicheSuggestions(_nicheOptions.Text);
            }
            await LoadProductSubcategoriesAsync(keepSavedSelection: false);
        };
        _productSubcategory.SelectedIndexChanged += (_, _) =>
        {
            if (_suspendProductCategoryEvents) return;
            var sub = _productSubcategory.SelectedItem as MercadoLivreCategory;
            if (sub is { Id.Length: > 0 })
            {
                _category.Text = sub.Name;
                FillNicheSuggestions(_nicheOptions.Text);
            }
        };
        _nicheOptions.Width = 445;
        _nicheOptions.Height = 37;
        _nicheOptions.DropDownStyle = ComboBoxStyle.DropDown;
        _nicheOptions.BackColor = Card;
        _nicheOptions.ForeColor = Foreground;
        FillNicheSuggestions("");
    }

    private void FillNicheSuggestions(string preservedText)
    {
        var suggestions = NicheCatalog.GetSuggestions(_category.Text);
        _nicheOptions.BeginUpdate();
        _nicheOptions.Items.Clear();
        _nicheOptions.Items.AddRange(suggestions.Cast<object>().ToArray());
        _nicheOptions.Text = preservedText;
        _nicheOptions.EndUpdate();
    }

    private void SelectSavedProductCategory(Product product)
    {
        _suspendProductCategoryEvents = true;
        try
        {
            var index = _productCategory.Items.Cast<MercadoLivreCategory>().ToList().FindIndex(
                x => !string.IsNullOrWhiteSpace(x.Id) && x.Id == product.CategoryId);
            _productCategory.SelectedIndex = index >= 0 ? index : 0;
            _productSubcategory.Items.Clear();
            _productSubcategory.Items.Add(new MercadoLivreCategory { Name = "Carregue a categoria para consultar subcategorias" });
            _productSubcategory.SelectedIndex = 0;
        }
        finally { _suspendProductCategoryEvents = false; }
        if (_productCategory.SelectedItem is MercadoLivreCategory { Id.Length: > 0 })
            _ = LoadProductSubcategoriesAsync(keepSavedSelection: true);
    }

    private void FillProductCategories(IReadOnlyList<MercadoLivreCategory> categories)
    {
        _suspendProductCategoryEvents = true;
        try
        {
            _productCategory.Items.Clear();
            _productCategory.Items.Add(new MercadoLivreCategory { Name = "Selecione uma categoria" });
            foreach (var category in categories) _productCategory.Items.Add(category);
            var saved = _current?.CategoryId ?? "";
            int index = _productCategory.Items.Cast<MercadoLivreCategory>().ToList()
                .FindIndex(c => c.Id == saved && saved.Length > 0);
            _productCategory.SelectedIndex = index >= 0 ? index : 0;
        }
        finally { _suspendProductCategoryEvents = false; }
        if (_productCategory.SelectedItem is MercadoLivreCategory { Id.Length: > 0 })
            _ = LoadProductSubcategoriesAsync(keepSavedSelection: true);
    }

    private async Task LoadProductSubcategoriesAsync(bool keepSavedSelection)
    {
        var category = _productCategory.SelectedItem as MercadoLivreCategory;
        var wantedSubcategory = keepSavedSelection ? _current?.SubcategoryId ?? "" : "";
        _suspendProductCategoryEvents = true;
        _productSubcategory.Items.Clear();
        _productSubcategory.Items.Add(new MercadoLivreCategory { Name = "Sem subcategoria" });
        _productSubcategory.SelectedIndex = 0;
        _suspendProductCategoryEvents = false;
        if (category is null || string.IsNullOrEmpty(category.Id)) return;
        try
        {
            var root = MercadoLivreBridgeService.SavedRepository;
            var response = await MercadoLivreBridgeService.ChildrenAsync(root, category.Id);
            // Se o usuário mudou de categoria durante a consulta, descarta resultados antigos.
            if (!string.Equals((_productCategory.SelectedItem as MercadoLivreCategory)?.Id,
                category.Id, StringComparison.Ordinal)) return;
            _suspendProductCategoryEvents = true;
            foreach (var child in response.Categories) _productSubcategory.Items.Add(child);
            var index = _productSubcategory.Items.Cast<MercadoLivreCategory>().ToList()
                .FindIndex(x => x.Id == wantedSubcategory && wantedSubcategory.Length > 0);
            _productSubcategory.SelectedIndex = index >= 0 ? index : 0;
        }
        catch (Exception ex)
        {
            _footer.Text = "Subcategorias indisponíveis: " + ex.Message;
        }
        finally { _suspendProductCategoryEvents = false; }
    }

    private async Task ShowMlPreviewAsync()
    {
        int generation = ++_mlPreviewGeneration;
        var urlString = SelectedMlListing()?.Thumbnail;
        _mlPreview.Image?.Dispose();
        _mlPreview.Image = null;
        if (!Uri.TryCreate(urlString, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps
            || url.UserInfo.Length > 0 || !url.IsDefaultPort) return;
        var host = url.IdnHost.ToLowerInvariant();
        if (host != "mlstatic.com" && !host.EndsWith(".mlstatic.com", StringComparison.Ordinal)) return;
        try
        {
            using var response = await ThumbnailClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode ||
                response.Content.Headers.ContentLength > 4_000_000) return;
            var mime = response.Content.Headers.ContentType?.MediaType ?? "";
            if (!mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return;
            await using var stream = await response.Content.ReadAsStreamAsync();
            using var bytes = new MemoryStream();
            var buffer = new byte[32768];
            int read;
            while ((read = await stream.ReadAsync(buffer)) > 0)
            {
                if (bytes.Length + read > 4_000_000) return;
                bytes.Write(buffer, 0, read);
            }
            bytes.Position = 0;
            using var picture = Image.FromStream(bytes);
            var copy = new Bitmap(picture);
            if (generation != _mlPreviewGeneration || _mlPreview.IsDisposed)
            {
                copy.Dispose();
                return;
            }
            _mlPreview.Image = copy;
        }
        catch (Exception)
        {
            // Prévia opcional: ausência de foto não altera nem oculta o anúncio oficial.
        }
    }

    private void ChoosePythonRoot()
    {
        using var picker = new FolderBrowserDialog { Description = "Selecione o repositório garimpo-da-vez (pasta com automation)" };
        if (Directory.Exists(_mlRoot.Text)) picker.SelectedPath = _mlRoot.Text;
        if (picker.ShowDialog() != DialogResult.OK) return;
        try
        {
            MercadoLivreBridgeService.SaveRepository(picker.SelectedPath);
            _mlRoot.Text = picker.SelectedPath;
            _mlStatus.Text = "Repositório Python configurado. Clique em Carregar categorias.";
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void BeginMlOperation()
    {
        if (_mlBusy) throw new InvalidOperationException("Aguarde a consulta em andamento.");
        _mlBusy = true;
        _mlStatus.Text = "Consultando a API oficial por meio do Python...";
    }

    private void EndMlOperation() => _mlBusy = false;

    private async Task LoadMlCategoriesAsync()
    {
        if (_mlBusy) return;
        try
        {
            BeginMlOperation();
            MercadoLivreBridgeService.SaveRepository(_mlRoot.Text);
            var result = await MercadoLivreBridgeService.CategoriesAsync(_mlRoot.Text);
            _mlLoadingCategories = true;
            _mlCategories.Items.Clear();
            _mlCategories.Items.Add(new MercadoLivreCategory { Name = "Todas as categorias" });
            foreach (var category in result.Categories) _mlCategories.Items.Add(category);
            _mlCategories.SelectedIndex = 0;
            _mlLoadingCategories = false;
            FillProductCategories(result.Categories);
            _mlStatus.Text = result.Categories.Count + " categorias carregadas (" + result.Origin + ").";
        }
        catch (Exception ex) { _mlStatus.Text = ex.Message; ShowError(ex); }
        finally { _mlLoadingCategories = false; EndMlOperation(); }
    }

    private async Task LoadMlChildrenAsync()
    {
        if (_mlLoadingCategories || _mlBusy) return;
        var selected = _mlCategories.SelectedItem as MercadoLivreCategory;
        _mlChildren.Items.Clear();
        _mlChildren.Items.Add(new MercadoLivreCategory { Name = "Todas as subcategorias" });
        _mlChildren.SelectedIndex = 0;
        if (selected is null || string.IsNullOrWhiteSpace(selected.Id)) return;
        try
        {
            BeginMlOperation();
            var result = await MercadoLivreBridgeService.ChildrenAsync(_mlRoot.Text, selected.Id);
            foreach (var category in result.Categories) _mlChildren.Items.Add(category);
            _mlStatus.Text = result.Categories.Count + " subcategorias carregadas.";
        }
        catch (Exception ex) { _mlStatus.Text = "Subcategorias indisponíveis: " + ex.Message; }
        finally { EndMlOperation(); }
    }

    private async Task SearchMlAsync()
    {
        if (_mlBusy) return;
        var query = _mlQuery.Text.Trim();
        if (query.Length is < 2 or > 100)
        {
            MessageBox.Show("Informe uma busca de 2 a 100 caracteres.");
            return;
        }
        try
        {
            BeginMlOperation();
            MercadoLivreBridgeService.SaveRepository(_mlRoot.Text);
            var category = (_mlChildren.SelectedItem as MercadoLivreCategory)?.Id;
            if (string.IsNullOrWhiteSpace(category))
                category = (_mlCategories.SelectedItem as MercadoLivreCategory)?.Id;
            var result = await MercadoLivreBridgeService.SearchAsync(_mlRoot.Text, query, category ?? "");
            _mlResults.DataSource = null;
            _mlResults.DataSource = result.Items;
            _mlStatus.Text = result.Items.Count + " anúncio(s) real(is) retornado(s); preço sujeito a mudanças.";
        }
        catch (Exception ex)
        {
            _mlResults.DataSource = null;
            FileStudioService.ShowImage(_mlPreview, null);
            _mlStatus.Text = ex.Message + " Use a URL oficial do anúncio no campo abaixo se a busca estiver bloqueada.";
            ShowError(ex);
        }
        finally { EndMlOperation(); }
    }

    private MercadoLivreListing? SelectedMlListing() =>
        _mlResults.CurrentRow?.DataBoundItem as MercadoLivreListing;

    private static bool ValidMercadoLivreListing(MercadoLivreListing listing)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(listing.Id, @"^MLB\d{6,18}$"))
            return false;
        if (!Uri.TryCreate(listing.Permalink, UriKind.Absolute, out var url))
            return false;
        var domain = url.IdnHost.ToLowerInvariant();
        return url.Scheme == Uri.UriSchemeHttps && url.UserInfo.Length == 0 &&
            (domain == "mercadolivre.com.br" || domain.EndsWith(".mercadolivre.com.br", StringComparison.Ordinal));
    }

    private void OpenSelectedMlListing()
    {
        var selected = SelectedMlListing();
        if (selected is null || !ValidMercadoLivreListing(selected))
        {
            MessageBox.Show("Selecione um anúncio válido na tabela.");
            return;
        }
        Process.Start(new ProcessStartInfo { FileName = selected.Permalink, UseShellExecute = true });
    }


    private async Task ImportMlSelectedAsync()
    {
        if (_mlBusy) return;
        var selected = SelectedMlListing();
        if (selected is null || !ValidMercadoLivreListing(selected))
        {
            MessageBox.Show("Selecione um anúncio oficial válido na tabela.");
            return;
        }
        await ImportMlListingAsync(selected, alreadyInPythonQueue: false);
    }

    private async Task ImportMlUrlAsync()
    {
        if (_mlBusy) return;
        var source = _mlManualUrl.Text.Trim();
        if (!ValidOfficialProductUrl(source))
        {
            MessageBox.Show("Cole a URL HTTPS real do anúncio (mercadolivre.com.br ou meli.la).");
            return;
        }
        try
        {
            BeginMlOperation();
            MercadoLivreBridgeService.SaveRepository(_mlRoot.Text);
            var response = await MercadoLivreBridgeService.IngestUrlAsync(_mlRoot.Text, source);
            var selected = response.Items.FirstOrDefault();
            if (selected is null || string.IsNullOrWhiteSpace(selected.Title))
                throw new InvalidOperationException("O Python não retornou dados suficientes para cadastrar; revise a URL.");
            EndMlOperation();
            _mlResults.DataSource = null;
            _mlResults.DataSource = new List<MercadoLivreListing> { selected };
            await ImportMlListingAsync(selected, alreadyInPythonQueue: true);
        }
        catch (Exception ex)
        {
            _mlStatus.Text = "Falha na importação por URL: " + ex.Message;
            ShowError(ex);
        }
        finally { EndMlOperation(); }
    }

    private static bool ValidOfficialProductUrl(string? source)
    {
        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || uri.UserInfo.Length > 0 || !uri.IsDefaultPort)
            return false;
        var host = uri.IdnHost.ToLowerInvariant();
        return host is "meli.la" or "mercadolivre.com.br" or "mercadolibre.com.br" ||
            host.EndsWith(".mercadolivre.com.br", StringComparison.Ordinal) ||
            host.EndsWith(".mercadolibre.com.br", StringComparison.Ordinal);
    }

    private async Task ImportMlListingAsync(MercadoLivreListing selected, bool alreadyInPythonQueue)
    {
        if (_mlBusy) return;
        if (!ValidOfficialProductUrl(selected.Permalink))
            throw new InvalidOperationException("URL oficial de anúncio inválida.");
        try
        {
            BeginMlOperation();
            var existing = (!string.IsNullOrWhiteSpace(selected.Id)
                ? _repository.FindMercadoLivreItem(selected.Id) : null)
                ?? _repository.GetProducts().FirstOrDefault(
                    p => p.Source == "Mercado Livre" && p.ListingUrl == selected.Permalink);
            var rootCategory = _mlCategories.SelectedItem as MercadoLivreCategory;
            var childCategory = _mlChildren.SelectedItem as MercadoLivreCategory;
            var product = existing ?? new Product
            {
                Source = "Mercado Livre",
                ExternalId = selected.Id,
                Name = selected.Title,
                Category = childCategory?.Id == selected.CategoryId ? childCategory.Name
                    : rootCategory is { Id.Length: > 0 } ? rootCategory.Name : selected.CategoryId,
                CategoryId = rootCategory?.Id ?? "",
                SubcategoryId = selected.CategoryId,
                ListingUrl = selected.Permalink
            };
            if (existing is null)
            {
                _repository.SaveProduct(product);
                _repository.AddLog(product.Id, "Mercado Livre", "Anúncio importado; imagem e link de afiliado pendentes de revisão.");
            }
            var sync = alreadyInPythonQueue ? " Fila Python atualizada por URL." : "";
            if (!alreadyInPythonQueue && _mlSyncQueue.Checked)
            {
                try
                {
                    await MercadoLivreBridgeService.IngestAsync(_mlRoot.Text, selected);
                    _repository.AddLog(product.Id, "Fila Python", "Importado pela ponte; verificar sincronização Excel.");
                    sync = " Fila Python atualizada; confira a planilha Excel.";
                }
                catch (Exception ex)
                {
                    sync = " Importado no SQLite, mas fila Python falhou: " + ex.Message;
                }
            }
            _search.Clear();
            ReloadProducts(product.Id);
            _tabs.SelectedIndex = 0;
            _footer.Text = (existing is null ? "Anúncio cadastrado." : "Anúncio já cadastrado; edição preservada.") + sync;
            MessageBox.Show(_footer.Text + "\nA imagem original precisa ser escolhida de arquivo autorizado.",
                "Garimpo Mercado Livre");
        }
        catch (Exception ex)
        {
            _mlStatus.Text = ex.Message;
            ShowError(ex);
        }
        finally { EndMlOperation(); }
    }

    private TabPage BuildStudioTab()
    {
        var page = Page("03 · Estúdio IA");
        var cols = TwoColumns(45);
        page.Controls.Add(cols);
        var left = Stack();
        cols.Controls.Add(left, 0, 0);
        left.Controls.Add(Section("FOTO DE REFERÊNCIA"));
        _studioPreview.Size = new Size(380, 445);
        left.Controls.Add(_studioPreview);
        _tabs.SelectedIndexChanged += (_, _) =>
        {
            if (_tabs.SelectedTab == page)
                FileStudioService.ShowImage(_studioPreview, _current?.OriginalImagePath);
        };
        left.Controls.Add(Note("O estúdio prepara seu material, mas não envia prompts nem extrai imagens automaticamente do ChatGPT web."));

        var right = Stack();
        cols.Controls.Add(right, 1, 0);
        right.Controls.Add(Section("MONTAGEM DE PROMPT"));
        right.Controls.Add(Field("Estilo visual", _style));
        right.Controls.Add(Field("Tipo de cenário", _scene));
        var row = ButtonRow(490);
        row.Controls.Add(ButtonOf("Gerar prompt", GeneratePrompt, true));
        row.Controls.Add(ButtonOf("Copiar", CopyPrompt));
        right.Controls.Add(row);
        _prompt.Height = 335;
        _prompt.Width = 475;
        _prompt.BorderStyle = BorderStyle.FixedSingle;
        _prompt.BackColor = Card;
        _prompt.ForeColor = Foreground;
        _prompt.Font = new Font("Consolas", 9.5f);
        right.Controls.Add(_prompt);
        var row2 = ButtonRow(490);
        row2.Controls.Add(ButtonOf("Ativar aba do ChatGPT", async () => await ActivateChatGpt(), true, 215));
        row2.Controls.Add(ButtonOf("Ver pasta do produto", OpenProductFolder, false, 190));
        right.Controls.Add(row2);
        _studioStatus.Width = 480;
        _studioStatus.Height = 58;
        _studioStatus.ForeColor = Muted;
        _studioStatus.Text = "Sem API. Use o ChatGPT manualmente com a foto e o prompt preparado.";
        right.Controls.Add(_studioStatus);
        return page;
    }

    private TabPage BuildVersionsTab()
    {
        var page = Page("04 · Versões");
        var cols = TwoColumns(45);
        page.Controls.Add(cols);
        var left = Stack();
        cols.Controls.Add(left, 0, 0);
        left.Controls.Add(Section("VERSÕES IMPORTADAS"));
        _versions.Width = 395;
        _versions.Height = 335;
        _versions.Font = new Font("Segoe UI", 12f);
        _versions.BorderStyle = BorderStyle.None;
        _versions.BackColor = Card;
        _versions.ForeColor = Foreground;
        _versions.SelectedIndexChanged += (_, _) =>
        {
            if (_versions.SelectedItem is ImageVersion v)
            {
                FileStudioService.ShowImage(_versionPreview, v.FilePath);
                _versionStatus.Text = $"Versão {v.Version} · {v.Style} · {(v.IsApproved ? "APROVADA" : "Em revisão")}";
            }
        };
        left.Controls.Add(_versions);
        left.Controls.Add(ButtonOf("Importar imagem editada", ImportGenerated, true, 285));
        left.Controls.Add(ButtonOf("Aprovar versão selecionada", ApproveVersion, false, 285));
        left.Controls.Add(Note("Primeiro gere/salve a imagem no ChatGPT; depois importe o arquivo para vinculá-lo ao produto."));
        var right = Stack();
        cols.Controls.Add(right, 1, 0);
        right.Controls.Add(Section("PRÉ-VISUALIZAÇÃO"));
        _versionPreview.Size = new Size(390, 485);
        right.Controls.Add(_versionPreview);
        _versionStatus.Width = 450;
        _versionStatus.Height = 45;
        _versionStatus.ForeColor = Muted;
        right.Controls.Add(_versionStatus);
        return page;
    }

    private TabPage BuildPinTab()
    {
        var page = Page("05 · Rascunho Pin");
        var cols = TwoColumns(58);
        page.Controls.Add(cols);
        var left = Stack();
        cols.Controls.Add(left, 0, 0);
        left.Controls.Add(Section("PREPARAR PARA PINTEREST"));
        left.Controls.Add(Field("Título do Pin", _pinTitle));
        left.Controls.Add(Field("Descrição e transparência de afiliação", _pinDescription, 200));
        left.Controls.Add(Field("Pasta / board do Pinterest", _pinBoard));
        left.Controls.Add(Field("Link de afiliado", _pinUrl));
        var row = ButtonRow(520);
        row.Controls.Add(ButtonOf("Salvar rascunho", () => SaveDraft(), true));
        row.Controls.Add(ButtonOf("Marcar publicação manual", MarkPublished, false, 220));
        left.Controls.Add(row);
        var right = Stack();
        cols.Controls.Add(right, 1, 0);
        right.Controls.Add(Section("STATUS DA PUBLICAÇÃO"));
        _pinStatus.ForeColor = Accent;
        _pinStatus.Font = new Font("Segoe UI Semibold", 15f);
        _pinStatus.Height = 55;
        _pinStatus.Width = 350;
        right.Controls.Add(_pinStatus);
        right.Controls.Add(Note("Este MVP não publica Pins nem agenda postagens via interface do Pinterest. O status ‘Publicado manualmente’ é uma declaração sua, não confirmação da plataforma."));
        right.Controls.Add(ButtonOf("Abrir pasta com imagens", OpenProductFolder, false, 280));
        return page;
    }

    private TabPage BuildHistoryTab()
    {
        var page = Page("06 · Histórico");
        var container = Stack();
        container.Dock = DockStyle.Fill;
        container.Controls.Add(Section("REGISTRO DE ATIVIDADES DO PRODUTO"));
        _logs.BackColor = Card;
        _logs.ForeColor = Foreground;
        _logs.BorderStyle = BorderStyle.None;
        _logs.Width = 820;
        _logs.Height = 555;
        _logs.Font = new Font("Consolas", 10f);
        container.Controls.Add(_logs);
        container.Controls.Add(Note("Histórico local. O SQLite fica na pasta de dados do usuário Windows."));
        page.Controls.Add(container);
        return page;
    }

    private void ReloadProducts(string? preferredId = null)
    {
        var query = _search.Text.Trim();
        var products = _repository.GetProducts().Where(p =>
            p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            p.Niche.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            p.Category.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        _reloadingProducts = true;
        _products.BeginUpdate();
        _products.DataSource = null;
        _products.DataSource = products;
        _products.DisplayMember = nameof(Product.Name);
        if (products.Count > 0)
        {
            var index = products.FindIndex(p => p.Id == preferredId);
            _products.SelectedIndex = index >= 0 ? index : 0;
        }
        else if (_current is null)
        {
            NewProduct();
        }
        _products.EndUpdate();
        _reloadingProducts = false;
        if (_products.SelectedItem is Product selected &&
            (_current is null || selected.Id != _current.Id))
            LoadProduct(selected);
    }

    private void LoadProduct(Product p)
    {
        _current = p;
        _name.Text = p.Name;
        _source.Text = p.Source;
        _externalId.Text = p.ExternalId;
        _category.Text = p.Category;
        SelectSavedProductCategory(p);
        FillNicheSuggestions(p.Niche);
        _description.Text = p.Description;
        _affiliateUrl.Text = p.AffiliateUrl;
        _listingUrl.Text = p.ListingUrl;
        _originalPath.Text = p.OriginalImagePath;
        FileStudioService.ShowImage(_originalPreview, p.OriginalImagePath);
        FileStudioService.ShowImage(_studioPreview, p.OriginalImagePath);
        _prompt.Clear();
        _studioStatus.Text = "Clique em 'Gerar prompt' para preparar o material.";
        RefreshVersions();
        var draft = _repository.GetPinDraft(p.Id);
        _pinTitle.Text = draft?.Title ?? p.Name;
        _pinDescription.Text = draft?.Description ?? "";
        _pinBoard.Text = draft?.Board ?? "";
        _pinUrl.Text = draft?.AffiliateUrl ?? p.AffiliateUrl;
        _pinStatus.Text = draft?.Status ?? "Sem rascunho";
        RefreshLogs();
        _footer.Text = $"Produto atual: {p.Name}";
    }

    private void NewProduct()
    {
        var p = new Product();
        _current = p;
        _products.ClearSelected();
        LoadProductFieldsForNew(p);
        _tabs.SelectedIndex = 0;
        _footer.Text = "Novo produto · preencha o nome e clique em salvar.";
    }

    private void LoadProductFieldsForNew(Product p)
    {
        _name.Clear(); _source.Text = p.Source; _externalId.Clear();
        _category.Clear();
        _suspendProductCategoryEvents = true;
        _productCategory.SelectedIndex = _productCategory.Items.Count > 0 ? 0 : -1;
        _productSubcategory.Items.Clear();
        _productSubcategory.Items.Add(new MercadoLivreCategory { Name = "Selecione primeiro a categoria" });
        _productSubcategory.SelectedIndex = 0;
        _suspendProductCategoryEvents = false;
        FillNicheSuggestions(""); _description.Clear();
        _affiliateUrl.Clear(); _listingUrl.Clear(); _originalPath.Clear();
        FileStudioService.ShowImage(_originalPreview, null);
        FileStudioService.ShowImage(_studioPreview, null);
        _prompt.Clear();
        _versions.DataSource = null;
        FileStudioService.ShowImage(_versionPreview, null);
        _versionStatus.Text = "";
        _pinTitle.Clear(); _pinDescription.Clear(); _pinBoard.Clear();
        _pinUrl.Clear(); _pinStatus.Text = "Sem rascunho";
        _logs.DataSource = null;
    }

    private bool SaveCurrent()
    {
        if (string.IsNullOrWhiteSpace(_name.Text))
        {
            MessageBox.Show("Informe o nome do produto.", "Validação");
            _tabs.SelectedIndex = 0;
            return false;
        }
        if (!IsValidHttpUrl(_affiliateUrl.Text))
        {
            MessageBox.Show("O link de afiliado deve ser um URL HTTP/HTTPS válido.", "Validação");
            return false;
        }
        _current ??= new Product();
        _current.Name = _name.Text.Trim();
        _current.Source = _source.Text.Trim();
        _current.ExternalId = _externalId.Text.Trim();
        var official = _productCategory.SelectedItem as MercadoLivreCategory;
        var officialChild = _productSubcategory.SelectedItem as MercadoLivreCategory;
        if (official is not null && !string.IsNullOrWhiteSpace(official.Id))
        {
            _current.CategoryId = official.Id;
            _current.SubcategoryId = officialChild?.Id ?? "";
            _current.Category = !string.IsNullOrWhiteSpace(officialChild?.Id)
                ? officialChild.Name : official.Name;
        }
        else _current.Category = _category.Text.Trim();
        _current.Niche = _nicheOptions.Text.Trim();
        _current.Description = _description.Text.Trim();
        _current.AffiliateUrl = _affiliateUrl.Text.Trim();
        _repository.SaveProduct(_current);
        _repository.AddLog(_current.Id, "Produto", "Cadastro salvo ou atualizado");
        ReloadProducts(_current.Id);
        _footer.Text = "Produto salvo no SQLite.";
        return true;
    }

    private void ImportOriginal()
    {
        if (!SaveCurrent() || _current is null) return;
        using var picker = OpenImageDialog();
        if (picker.ShowDialog() != DialogResult.OK) return;
        try
        {
            _current.OriginalImagePath = FileStudioService.CopyOriginal(_current, picker.FileName);
            _repository.SaveProduct(_current);
            _repository.AddLog(_current.Id, "Imagem original", "Imagem de referência importada");
            _originalPath.Text = _current.OriginalImagePath;
            FileStudioService.ShowImage(_originalPreview, _current.OriginalImagePath);
            _footer.Text = "Imagem original importada.";
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void GeneratePrompt()
    {
        if (_current is null || !SaveCurrent()) return;
        if (string.IsNullOrWhiteSpace(_current.OriginalImagePath))
        {
            MessageBox.Show("Importe antes uma imagem de referência para o produto.");
            _tabs.SelectedIndex = 0;
            return;
        }
        _prompt.Text = PromptService.Build(_current, _style.Text, _scene.Text);
        _studioStatus.Text = "Prompt pronto. Copie, abra a aba do ChatGPT e envie junto com a foto original.";
        _repository.AddLog(_current.Id, "Prompt IA", "Prompt montado localmente");
        RefreshLogs();
    }

    private void CopyPrompt()
    {
        if (string.IsNullOrWhiteSpace(_prompt.Text)) GeneratePrompt();
        if (string.IsNullOrWhiteSpace(_prompt.Text)) return;
        Clipboard.SetText(_prompt.Text);
        _footer.Text = "Prompt copiado para a área de transferência.";
    }

    private async Task ActivateChatGpt()
    {
        bool found = false;
        try
        {
            found = await EdgeTabService.ActivateExistingChatGptTabAsync();
        }
        catch (HttpRequestException) { /* CDP indisponível: tentar janela já existente. */ }
        catch (TaskCanceledException) { /* Porta fechada ou timeout. */ }

        if (!found) found = EdgeWindowService.ActivateVisibleChatGptWindow();
        if (!found)
        {
            MessageBox.Show("Não foi possível localizar uma aba ChatGPT ativa no Edge.\n\n" +
                "Opção 1: deixe a aba ChatGPT selecionada no Edge e clique novamente.\n" +
                "Opção 2: use a janela do Edge com CDP na porta 9333 para localizar também abas inativas.\n\n" +
                "O programa não abre outra janela automaticamente.",
                "ChatGPT não encontrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
            _footer.Text = "Janela existente do ChatGPT ativada. Anexo e envio continuam manuais.";
    }

    private void ImportGenerated()
    {
        if (!SaveCurrent() || _current is null) return;
        using var picker = OpenImageDialog();
        if (picker.ShowDialog() != DialogResult.OK) return;
        try
        {
            var versionNumber = _repository.NextImageVersion(_current.Id);
            var savedPath = FileStudioService.CopyGenerated(_current, picker.FileName, versionNumber);
            var image = new ImageVersion {
                ProductId = _current.Id, Version = versionNumber,
                FilePath = savedPath, Prompt = _prompt.Text, Style = _style.Text,
                CreatedAt = AppPaths.UtcNow()
            };
            _repository.SaveImage(image);
            _repository.AddLog(_current.Id, "Imagem IA", $"Versão {versionNumber} importada");
            RefreshVersions(image.Id);
            RefreshLogs();
            _footer.Text = $"Imagem v{versionNumber:000} importada para revisão.";
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void RefreshVersions(string? preferredId = null)
    {
        if (_current is null) return;
        var items = _repository.GetImages(_current.Id);
        _versions.DataSource = null;
        _versions.DataSource = items;
        if (items.Count > 0)
        {
            int index = items.FindIndex(v => v.Id == preferredId);
            _versions.SelectedIndex = index >= 0 ? index : 0;
        }
        else
        {
            _versionStatus.Text = "Nenhuma versão importada.";
            FileStudioService.ShowImage(_versionPreview, null);
        }
    }

    private void ApproveVersion()
    {
        if (_current is null || _versions.SelectedItem is not ImageVersion selected) return;
        try
        {
            _repository.ApproveImage(_current.Id, selected.Id);
            _repository.AddLog(_current.Id, "Aprovação", $"Versão v{selected.Version:000} aprovada");
            RefreshVersions(selected.Id);
            RefreshLogs();
            _footer.Text = "Imagem aprovada para preparar o Pin.";
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void SaveDraft(string status = "Rascunho")
    {
        if (!SaveCurrent() || _current is null) return;
        if (!IsValidHttpUrl(_pinUrl.Text))
        {
            MessageBox.Show("Corrija o link do Pin (HTTP/HTTPS) antes de salvar.");
            return;
        }
        var approved = _repository.GetImages(_current.Id).FirstOrDefault(i => i.IsApproved);
        var draft = _repository.GetPinDraft(_current.Id) ?? new PinDraft { ProductId = _current.Id };
        draft.Title = _pinTitle.Text.Trim();
        draft.Description = _pinDescription.Text.Trim();
        draft.Board = _pinBoard.Text.Trim();
        draft.AffiliateUrl = _pinUrl.Text.Trim();
        draft.ImageVersionId = approved?.Id ?? "";
        draft.Status = status;
        _repository.SavePin(draft);
        _repository.AddLog(_current.Id, "Pin", "Rascunho salvo: " + status);
        _pinStatus.Text = status;
        RefreshLogs();
        _footer.Text = "Rascunho salvo localmente. Nenhum Pin foi publicado.";
    }

    private void MarkPublished()
    {
        if (_current is null) return;
        var approved = _repository.GetImages(_current.Id).FirstOrDefault(i => i.IsApproved);
        if (approved is null || string.IsNullOrWhiteSpace(_pinTitle.Text) ||
            string.IsNullOrWhiteSpace(_pinUrl.Text))
        {
            MessageBox.Show("Antes: aprove uma imagem, preencha o título e o link.");
            return;
        }
        if (MessageBox.Show("Você conferiu que publicou o Pin MANUALMENTE no Pinterest?\n" +
            "Este aplicativo não faz a publicação nem confirma o envio.", "Confirmação manual",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        SaveDraft("Publicado manualmente (informado pelo usuário)");
    }

    private void RefreshLogs()
    {
        if (_current is null) return;
        _logs.DataSource = null;
        _logs.DataSource = _repository.GetLogs(_current.Id);
    }

    private void OpenProductFolder()
    {
        if (_current is null) return;
        var folder = AppPaths.ProductDirectory(_current.Id);
        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
    }

    private void ExportExcel()
    {
        using var dialog = new SaveFileDialog {
            Filter = "Planilha Excel (*.xlsx)|*.xlsx",
            FileName = "AffiliateStudio_Produtos_" + DateTime.Now.ToString("yyyyMMdd") + ".xlsx"
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        try
        {
            ExcelExportService.Export(_repository, dialog.FileName);
            _footer.Text = "Arquivo Excel exportado: " + dialog.FileName;
            MessageBox.Show("Planilha .xlsx criada com os produtos cadastrados.");
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private static bool IsValidHttpUrl(string input) => string.IsNullOrWhiteSpace(input) ||
        (Uri.TryCreate(input.Trim(), UriKind.Absolute, out var url) &&
         (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps));

    private static OpenFileDialog OpenImageDialog() => new() {
        Filter = "Imagens (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
        Title = "Selecione a imagem do produto"
    };

    private static void ShowError(Exception ex) =>
        MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private static TextBox TextInput(bool multiline = false) => new() {
        Width = 420, Height = multiline ? 102 : 37, Multiline = multiline,
        ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
        BackColor = Card, ForeColor = Foreground,
        BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10f)
    };

    private static ComboBox SelectBox(string[] items)
    {
        var box = new ComboBox {
            Width = 440, Height = 38, DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Card, ForeColor = Foreground, Font = new Font("Segoe UI", 10f)
        };
        box.Items.AddRange(items);
        box.SelectedIndex = 0;
        return box;
    }

    private static PictureBox Picture() => new() {
        Width = 380, Height = 430, SizeMode = PictureBoxSizeMode.Zoom,
        BackColor = Card, BorderStyle = BorderStyle.FixedSingle
    };

    private static TabPage Page(string title) => new(title) {
        BackColor = Surface, ForeColor = Foreground,
        Padding = new Padding(15), AutoScroll = true
    };

    private static TableLayoutPanel TwoColumns(int leftPercent)
    {
        var cols = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, leftPercent));
        cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100 - leftPercent));
        return cols;
    }

    private static FlowLayoutPanel Stack() => new() {
        Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
        WrapContents = false, AutoScroll = true, BackColor = Surface,
        Padding = new Padding(14, 15, 10, 14)
    };

    private static Panel Field(string caption, Control field, int height = 78)
    {
        var panel = new Panel { Width = 470, Height = height, BackColor = Surface };
        panel.Controls.Add(new Label {
            Text = caption, Left = 0, Top = 1, Width = 450, Height = 23,
            ForeColor = Muted, Font = new Font("Segoe UI", 9.5f)
        });
        field.Left = 0; field.Top = 27;
        field.Width = 445;
        if (field is TextBox tb && tb.Multiline) field.Height = height - 34;
        panel.Controls.Add(field);
        return panel;
    }

    private static Label Section(string label) => new() {
        Text = label, Height = 40, Width = 470, ForeColor = Foreground,
        Font = new Font("Segoe UI Semibold", 15f, FontStyle.Bold)
    };

    private static Label Note(string value) => new() {
        Text = value, ForeColor = Muted, Height = 92, Width = 435,
        Font = new Font("Segoe UI", 9.5f)
    };

    private static FlowLayoutPanel ButtonRow(int width) => new() {
        Width = width, Height = 56, BackColor = Surface,
        FlowDirection = FlowDirection.LeftToRight, WrapContents = false
    };

    private static Button ButtonOf(string title, Action action, bool primary = false, int width = 180)
    {
        var b = new Button {
            Text = title, Width = width, Height = 39, Margin = new Padding(0, 2, 11, 5),
            FlatStyle = FlatStyle.Flat, BackColor = primary ? Accent : Card,
            ForeColor = Foreground, Font = new Font("Segoe UI Semibold", 9.5f),
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderSize = 0;
        b.Click += (_, _) => action();
        return b;
    }

    private static void StyleInput(TextBox input)
    {
        input.BackColor = Card;
        input.ForeColor = Foreground;
        input.BorderStyle = BorderStyle.FixedSingle;
    }
}
