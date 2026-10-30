namespace GarimpoDaVezStudio.UI;

public sealed partial class MainForm
{
    private Control BuildCatalogActions()
    {
        var toolbar = new TableLayoutPanel {
            Name = "CatalogActionsToolbar", AccessibleName = "Ações do catálogo",
            Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2,
            BackColor = Surface, Margin = Padding.Empty, Padding = Padding.Empty
        };
        for (var i = 0; i < 4; i++)
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        Add(0, 0, "Importar CSV/XLSX", async () => await ImportCatalogFileAsync(), true,
            "Importar produtos por planilha CSV ou Excel");
        Add(1, 0, "Reunir salvos", async () => await SyncStudioCatalogAsync(), false,
            "Incluir no catálogo produtos já salvos no Studio");
        Add(2, 0, "Exportar Excel", async () => await ExportCatalogAsync(), false,
            "Salvar planilha Excel com o catálogo atual");
        Add(3, 0, "Abrir planilha", OpenAutoCatalogExcel, false,
            "Abrir o arquivo Excel automático mais recente");

        Add(0, 1, "Atualizar anúncio", async () => await VerifyCatalogEntryAsync(), false,
            "Conferir detalhes de um anúncio oficial selecionado");
        Add(1, 1, "Usar no Studio", PromoteCatalogEntry, true,
            "Carregar o registro selecionado no cadastro de produto");
        Add(2, 1, "Confirmar link", ConfirmCatalogAffiliate, false,
            "Registrar link de afiliado fornecido pela Central oficial");
        Add(3, 1, "Mais vendidos", async () => await CollectHighlightsAsync(), false,
            "Buscar destaques da categoria oficial selecionada na aba Garimpo");
        return toolbar;

        void Add(int col, int row, string title, Action action, bool primary, string description)
        {
            var button = ButtonOf(title, action, primary, 120);
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(4, 4, 4, 4);
            button.MinimumSize = new Size(0, 36);
            button.Font = new Font("Segoe UI Semibold", 9f);
            button.FlatAppearance.MouseOverBackColor =
                primary ? Color.FromArgb(22, 139, 206) : Color.FromArgb(43, 62, 87);
            button.FlatAppearance.MouseDownBackColor =
                primary ? Color.FromArgb(19, 112, 175) : Color.FromArgb(36, 53, 77);
            button.AccessibleName = title;
            _catalogActionTooltip.SetToolTip(button, description);
            toolbar.Controls.Add(button, col, row);
        }
    }

    private Control BuildCatalogSearchRow()
    {
        var row = new TableLayoutPanel {
            Name = "CatalogSearchRow",
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
            Margin = Padding.Empty, Padding = Padding.Empty,
            BackColor = Surface
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        _catalogQuery.AccessibleName = "Pesquisa do catálogo";
        var search = CreateSearchControl(_catalogQuery);
        search.Name = "CatalogSearchField";
        search.MaximumSize = new Size(760, 60);
        search.Margin = new Padding(4, 4, 5, 4);
        row.Controls.Add(search, 0, 0);
        var context = new Label {
            Text = "CATÁLOGO LOCAL  /  MERCADO LIVRE BRASIL",
            Dock = DockStyle.Fill, ForeColor = Muted,
            Font = new Font("Segoe UI Semibold", 8.3f),
            TextAlign = ContentAlignment.MiddleRight,
            Margin = new Padding(6, 0, 6, 0),
            AutoEllipsis = true
        };
        row.Controls.Add(context, 1, 0);
        return row;
    }

    private readonly ToolTip _catalogActionTooltip = new() {
        InitialDelay = 380, AutoPopDelay = 9000, ReshowDelay = 150
    };
}
