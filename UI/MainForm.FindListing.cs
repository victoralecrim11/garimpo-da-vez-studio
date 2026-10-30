using System.Diagnostics;
using GarimpoDaVezStudio.Services;

namespace GarimpoDaVezStudio.UI;

public sealed partial class MainForm
{
    private bool _listingFinderRunning;

    private async Task FindBrazilianListingAsync()
    {
        var term = _name.Text.Trim();
        if (term.Length < 3)
        {
            _productNameStatus.Text = "Digite ao menos 3 caracteres do produto para procurar anúncios.";
            return;
        }
        if (_listingFinderRunning) return;
        var root = MercadoLivreBridgeService.SavedRepository;
        if (!MercadoLivreBridgeService.ValidRepository(root))
        {
            OfferBrazilianSearchInBrowser(term,
                "A integração Python ainda não está configurada.");
            return;
        }

        try
        {
            _listingFinderRunning = true;
            _productNameStatus.Text = "Pesquisando anúncios MLB individuais no Mercado Livre Brasil...";
            // Não se usa categoria selecionada: pode ter sido apenas predita por
            // título de catálogo e excluir anúncios reais de outra subcategoria.
            var result = await MercadoLivreBridgeService.SearchAsync(root, term, "");
            if (IsDisposed) return;
            var matches = new List<MercadoLivreListing>();
            foreach (var item in result.Items)
            {
                if (!OfficialListingMatcher.TryGetVerifiedPermalink(item, out _)) continue;
                if (matches.Any(x => x.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase)))
                    continue;
                matches.Add(item);
            }
            if (matches.Count == 0)
            {
                OfferBrazilianSearchInBrowser(term,
                    "A consulta não retornou anúncios com URL e ID MLB correspondentes.");
                return;
            }
            CaptureCatalogListings(matches);
            var selected = ChooseBrazilianListing(matches);
            if (selected is null) {
                _productNameStatus.Text = "Nenhum anúncio vinculado. Seu cadastro permanece inalterado.";
                return;
            }
            if (!OfficialListingMatcher.TryGetVerifiedPermalink(selected, out var verifiedUrl))
                return;
            var currentId = _externalId.Text.Trim();
            var changesId = currentId.Length > 0 &&
                !currentId.Equals(selected.Id, StringComparison.OrdinalIgnoreCase);
            var warning = changesId
                ? "\n\nO cadastro tinha outro ID. Ao vincular este anúncio, o link anterior de afiliado será limpo. Revise também a foto."
                : "";
            if (MessageBox.Show(this,
                $"Vincular este anúncio ao cadastro?\n\n{selected.Title}\nID: {selected.Id}\nURL: {verifiedUrl}" +
                warning + "\n\nO produto será salvo somente quando você clicar em Salvar.",
                "Confirmar anúncio brasileiro", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            if (changesId)
                _affiliateUrl.Clear(); // Nunca mantém comissão de outro anúncio.
            _externalId.Text = selected.Id;
            _listingUrl.Text = verifiedUrl;
            _source.Text = "Mercado Livre";
            _productNameStatus.Text =
                "Anúncio brasileiro individual selecionado, com ID e URL correspondente. " +
                "Confira modelo e foto, gere o link de afiliado oficial e salve manualmente.";
            _footer.Text = "Anúncio encontrado: " + selected.Id;
            if (string.IsNullOrWhiteSpace(_originalPath.Text) &&
                !string.IsNullOrWhiteSpace(selected.Thumbnail))
                _ = ShowProductSuggestionPreviewAsync(selected.Thumbnail);
            // O título editado pelo usuário não é trocado automaticamente.
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
                OfferBrazilianSearchInBrowser(term,
                    "Busca oficial indisponível: " + ex.Message);
        }
        finally { _listingFinderRunning = false; }
    }

    private MercadoLivreListing? ChooseBrazilianListing(List<MercadoLivreListing> items)
    {
        using var dialog = new Form {
            Text = "Escolher anúncio individual do Mercado Livre Brasil",
            Width = 940, Height = 545, MinimumSize = new Size(680, 420),
            StartPosition = FormStartPosition.CenterParent, BackColor = Surface,
            ForeColor = Foreground, Font = new Font("Segoe UI", 10f)
        };
        var layout = new TableLayoutPanel {
            Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1,
            Padding = new Padding(14)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
        var message = new Label {
            Text = "Selecione o anúncio que corresponde ao modelo e à foto. " +
                "Resultados semelhantes não são automaticamente o mesmo produto. " +
                "As URLs abaixo são públicas, não são links de afiliado.",
            Dock = DockStyle.Fill, ForeColor = Muted
        };
        layout.Controls.Add(message, 0, 0);
        var grid = new DataGridView {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
            AllowUserToDeleteRows = false, MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false, BackgroundColor = Card,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BorderStyle = BorderStyle.None
        };
        grid.Columns.Add("Title", "Anúncio / modelo");
        grid.Columns.Add("Id", "ID do anúncio");
        grid.Columns.Add("Price", "Preço consultado");
        grid.Columns[0].FillWeight = 65;
        grid.Columns[1].FillWeight = 19;
        grid.Columns[2].FillWeight = 16;
        foreach (var entry in items)
        {
            int index = grid.Rows.Add(entry.Title, entry.Id,
                entry.Price.HasValue ? entry.Price.Value.ToString("N2") +
                    " " + entry.Currency : "Não informado");
            grid.Rows[index].Tag = entry;
        }
        grid.ClearSelection();
        layout.Controls.Add(grid, 0, 1);
        var actions = new FlowLayoutPanel {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        var choose = ButtonOf("Usar este anúncio", () => dialog.DialogResult = DialogResult.OK,
            true, 190);
        var visit = ButtonOf("Abrir anúncio", () => {
            if (grid.SelectedRows.Count != 1 ||
                grid.SelectedRows[0].Tag is not MercadoLivreListing row ||
                !OfficialListingMatcher.TryGetVerifiedPermalink(row, out var url))
                return;
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }, false, 170);
        var cancel = ButtonOf("Cancelar", () => dialog.DialogResult = DialogResult.Cancel,
            false, 120);
        choose.Enabled = false;
        visit.Enabled = false;
        grid.SelectionChanged += (_, _) => {
            bool selected = grid.SelectedRows.Count == 1;
            choose.Enabled = selected;
            visit.Enabled = selected;
        };
        actions.Controls.Add(choose);
        actions.Controls.Add(visit);
        actions.Controls.Add(cancel);
        layout.Controls.Add(actions, 0, 2);
        dialog.Controls.Add(layout);
        return dialog.ShowDialog(this) == DialogResult.OK &&
            grid.SelectedRows.Count == 1
            ? grid.SelectedRows[0].Tag as MercadoLivreListing : null;
    }

    private void OfferBrazilianSearchInBrowser(string term, string reason)
    {
        _productNameStatus.Text = reason + " Pesquise no site brasileiro e cole a URL do anúncio na aba Produto.";
        if (MessageBox.Show(this,
            reason + "\n\nAbrir a busca por este nome no Mercado Livre Brasil? " +
            "Depois selecione o anúncio exato e copie sua URL pública.",
            "Buscar anúncio no navegador", MessageBoxButtons.YesNo,
            MessageBoxIcon.Information) != DialogResult.Yes)
            return;
        var searchUrl = "https://lista.mercadolivre.com.br/" +
            Uri.EscapeDataString(term.Replace(' ', '-'));
        Process.Start(new ProcessStartInfo(searchUrl) { UseShellExecute = true });
    }
}
