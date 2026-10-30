using System.Diagnostics;
using GarimpoDaVezStudio.Data;
using GarimpoDaVezStudio.Models;
using GarimpoDaVezStudio.Services;

namespace GarimpoDaVezStudio.UI;

public sealed partial class MainForm
{
    private readonly CatalogRepository _catalog = new(AppPaths.DatabaseFile);
    private readonly DataGridView _catalogGrid = new();
    private readonly TextBox _catalogQuery = TextInput();
    private readonly CheckBox _catalogIncludeHistorical = new();
    private readonly Label _catalogInfo = new();
    private readonly CheckBox _autoCatalogExcel = new();
    private readonly System.Windows.Forms.Timer _catalogExcelTimer = new() { Interval = 3500 };
    private readonly System.Windows.Forms.Timer _catalogSearchTimer = new() { Interval = 300 };
    private readonly SemaphoreSlim _catalogExcelLock = new(1,1);
    private bool _catalogOperationsRunning;
    private bool _catalogExcelPending;

    private TabPage BuildCatalogTab()
    {
        var page = Page("07 · Catálogo híbrido");
        var root = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
            BackColor = Surface, Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 158));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        page.Controls.Add(root);

        var introduction = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            BackColor = Surface, Margin = new Padding(4, 0, 0, 2)
        };
        introduction.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
        introduction.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        introduction.Controls.Add(new Label {
            Text = "CATÁLOGO INTELIGENTE", ForeColor = Foreground,
            Font = new Font("Segoe UI Semibold", 16f, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomLeft, Dock = DockStyle.Fill
        }, 0, 0);
        introduction.Controls.Add(new Label {
            Text = "Produtos salvos, anúncios brasileiros e importações de planilhas",
            ForeColor = Muted, Font = new Font("Segoe UI", 9.2f),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);
        root.Controls.Add(introduction, 0, 0);

        var header = new TableLayoutPanel {
            Name = "CatalogHeader", Dock = DockStyle.Fill,
            ColumnCount = 1, RowCount = 2, BackColor = Surface, Margin = Padding.Empty
        };
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _catalogQuery.PlaceholderText = "Pesquisar nome, categoria ou código MLB...";
        _catalogQuery.MaxLength = 100;
        header.Controls.Add(BuildCatalogSearchRow(), 0, 0);
        header.Controls.Add(BuildCatalogActions(), 0, 1);
        root.Controls.Add(header, 0, 1);

        _catalogGrid.Dock=DockStyle.Fill;
        _catalogGrid.BackgroundColor=Card;
        _catalogGrid.GridColor=Surface;
        _catalogGrid.DefaultCellStyle.BackColor=Card;
        _catalogGrid.DefaultCellStyle.ForeColor=Foreground;
        _catalogGrid.DefaultCellStyle.SelectionBackColor=Accent;
        _catalogGrid.DefaultCellStyle.SelectionForeColor=Color.White;
        _catalogGrid.ColumnHeadersDefaultCellStyle.BackColor=Background;
        _catalogGrid.ColumnHeadersDefaultCellStyle.ForeColor=Foreground;
        _catalogGrid.EnableHeadersVisualStyles=false;
        _catalogGrid.AllowUserToAddRows=false;
        _catalogGrid.AllowUserToDeleteRows=false;
        _catalogGrid.ReadOnly=true;
        _catalogGrid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;
        _catalogGrid.MultiSelect=false;
        _catalogGrid.AutoGenerateColumns=false;
        _catalogGrid.RowHeadersVisible=false;
        _catalogGrid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Status",DataPropertyName=nameof(CatalogEntry.Status),Width=125});
        _catalogGrid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Nome",DataPropertyName=nameof(CatalogEntry.Name),Width=270});
        _catalogGrid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Tipo",DataPropertyName=nameof(CatalogEntry.KindLabel),Width=98});
        _catalogGrid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="ID",DataPropertyName=nameof(CatalogEntry.ExternalId),Width=125});
        _catalogGrid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Categoria",DataPropertyName=nameof(CatalogEntry.Category),Width=210});
        _catalogGrid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Preço informado",DataPropertyName=nameof(CatalogEntry.Price),Width=118});
        _catalogGrid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Origem",DataPropertyName=nameof(CatalogEntry.Origin),Width=92});
        _catalogGrid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Verificado UTC",DataPropertyName=nameof(CatalogEntry.LastVerifiedUtc),Width=175});
        var catalogBody = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1
        };
        catalogBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        catalogBody.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 305));
        catalogBody.Controls.Add(_catalogGrid,0,0);
        catalogBody.Controls.Add(BuildCatalogPhotoPanel(),1,0);
        root.Controls.Add(catalogBody,0,2);
        _catalogGrid.SelectionChanged += (_, _) => OnCatalogPhotoSelectionChanged();

        var bottom = new TableLayoutPanel {
            Dock = DockStyle.Fill, BackColor = Surface, ColumnCount = 1, RowCount = 3,
            Padding = new Padding(5, 3, 3, 0), Margin = Padding.Empty
        };
        bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _catalogIncludeHistorical.Text = "Incluir referências históricas em espanhol (2019)";
        _catalogIncludeHistorical.ForeColor = Muted;
        _catalogIncludeHistorical.BackColor = Surface;
        _catalogIncludeHistorical.Dock = DockStyle.Fill;
        _catalogIncludeHistorical.Font = new Font("Segoe UI", 9f);
        _catalogIncludeHistorical.CheckedChanged += (_,_) => RefreshCatalog();
        bottom.Controls.Add(_catalogIncludeHistorical, 0, 0);
        _autoCatalogExcel.Text = "Manter catálogo Excel atualizado automaticamente";
        _autoCatalogExcel.ForeColor = Foreground;
        _autoCatalogExcel.BackColor = Surface;
        _autoCatalogExcel.Checked = true;
        _autoCatalogExcel.Dock = DockStyle.Fill;
        _autoCatalogExcel.Font = new Font("Segoe UI", 9f);
        bottom.Controls.Add(_autoCatalogExcel, 0, 1);
        _catalogInfo.ForeColor = Muted;
        _catalogInfo.AutoEllipsis = true;
        _catalogInfo.Dock = DockStyle.Fill;
        _catalogInfo.Font = new Font("Segoe UI", 8.5f);
        _catalogInfo.Text = "Referências offline não comprovam preço ou disponibilidade.";
        bottom.Controls.Add(_catalogInfo, 0, 2);
        root.Controls.Add(bottom, 0, 3);

        _catalogQuery.TextChanged+=(_,_)=>{_catalogSearchTimer.Stop();_catalogSearchTimer.Start();};
        _catalogSearchTimer.Tick+=(_,_)=>{_catalogSearchTimer.Stop();RefreshCatalog();};
        _catalogExcelTimer.Tick+=async (_,_)=> {
            _catalogExcelTimer.Stop();
            await WriteAutomaticCatalogExcelAsync();
        };
        FormClosed+=(_,_)=> {
            _catalogSearchTimer.Dispose();
            _catalogExcelTimer.Dispose();
            // Exportação em andamento ainda pode liberar o semáforo ao concluir.
            // Evita descartá-lo enquanto uma tarefa assíncrona estiver ativa.
        };
        page.Enter+=(_,_)=>RefreshCatalog();
        RefreshCatalog();
        return page;
    }

    private CatalogEntry? SelectedCatalogEntry() =>
        _catalogGrid.SelectedRows.Count == 1
            ? _catalogGrid.SelectedRows[0].DataBoundItem as CatalogEntry
            : null;

    private void RefreshCatalog()
    {
        try
        {
            var items=_catalog.Search(_catalogQuery.Text,200,
                includeHistorical:_catalogIncludeHistorical.Checked);
            var previousKey=SelectedCatalogEntry()?.Key;
            _catalogGrid.DataSource=null;
            _catalogGrid.DataSource=items;
            _catalogGrid.ClearSelection();
            if (previousKey is not null)
                foreach (DataGridViewRow row in _catalogGrid.Rows)
                    if (row.DataBoundItem is CatalogEntry x && x.Key==previousKey) {
                        row.Selected=true;_catalogGrid.CurrentCell=row.Cells[0];break;
                    }
            _catalogInfo.Text=$"{_catalog.Count():N0} registro(s) no total; {items.Count} exibido(s). " +
                (_catalogIncludeHistorical.Checked
                    ? "Inclui base histórica ES. "
                    : "Pesquisa brasileira; marque a opção para incluir ES 2019. ") +
                "Referências não garantem anúncio atual ou comissão.";
        }
        catch(Exception ex) { _catalogInfo.Text="Falha no catálogo: "+ex.Message; }
    }

    private void ScheduleCatalogExcel()
    {
        if (!_autoCatalogExcel.Checked) return;
        _catalogExcelPending=true;
        _catalogExcelTimer.Stop();
        _catalogExcelTimer.Start();
    }

    private async Task WriteAutomaticCatalogExcelAsync()
    {
        if (!_catalogExcelPending || !_autoCatalogExcel.Checked) return;
        if (!await _catalogExcelLock.WaitAsync(0)) return;
        _catalogExcelPending=false;
        try
        {
            await Task.Run(()=>CatalogFilesService.Export(_catalog,CatalogFilesService.AutomaticPath));
            if(!IsDisposed)
                _footer.Text="Catálogo sincronizado: "+CatalogFilesService.AutomaticPath;
        }
        catch(Exception ex)
        {
            if(!IsDisposed)
                _catalogInfo.Text="Catálogo salvo no SQLite, mas Excel automático pendente: "+ex.Message;
            // Pode estar aberto no Excel; usuário consegue exportar manualmente.
        }
        finally {_catalogExcelLock.Release();}
    }

    private async Task ImportCatalogFileAsync()
    {
        if (_catalogOperationsRunning) return;
        using var dialog=new OpenFileDialog {
            Filter="Catálogos CSV e Excel (*.csv;*.xlsx)|*.csv;*.xlsx",
            Title="Escolha uma planilha com coluna Nome / Produto / Title"
        };
        if (dialog.ShowDialog()!=DialogResult.OK) return;
        try
        {
            _catalogOperationsRunning=true;
            _catalogInfo.Text="Importando planilha, validando e deduplicando...";
            var count=await Task.Run(()=>CatalogFilesService.Import(_catalog,dialog.FileName));
            RefreshCatalog();
            _catalogInfo.Text=$"{count:N0} linha(s) importada(s)/atualizada(s) de {Path.GetFileName(dialog.FileName)}. Confira os dados antes de divulgar.";
            ScheduleCatalogExcel();
        }
        catch(Exception ex) {ShowError(ex);_catalogInfo.Text="Falha na importação: "+ex.Message;}
        finally {_catalogOperationsRunning=false;}
    }

    private async Task ExportCatalogAsync()
    {
        using var picker=new SaveFileDialog{
            Filter="Planilha Excel (*.xlsx)|*.xlsx",FileName="Garimpo_Catalogo_"+DateTime.Now.ToString("yyyyMMdd")+".xlsx"};
        if(picker.ShowDialog()!=DialogResult.OK)return;
        try
        {
            _catalogInfo.Text="Gerando Excel...";
            await Task.Run(()=>CatalogFilesService.Export(_catalog,picker.FileName));
            _catalogInfo.Text="Excel criado: "+picker.FileName;
        }
        catch(Exception ex){ShowError(ex);}
    }

    private void OpenAutoCatalogExcel()
    {
        if (!File.Exists(CatalogFilesService.AutomaticPath))
        {
            _catalogInfo.Text="Ainda não existe exportação automática. Importe um arquivo ou use Exportar Excel.";
            return;
        }
        Process.Start(new ProcessStartInfo(CatalogFilesService.AutomaticPath){UseShellExecute=true});
    }

    private async Task SyncStudioCatalogAsync()
    {
        try
        {
            var result=await Task.Run(()=>{
                var records=_repository.GetProducts().Select(p=>CatalogFromStudio(p)).ToList();
                return _catalog.UpsertBatch(records,"Sincronização com cadastros Studio");
            });
            RefreshCatalog();ScheduleCatalogExcel();
            _catalogInfo.Text=$"{result} cadastro(s) do Studio indexado(s); links aguardam confirmação individual.";
        }
        catch(Exception ex){ShowError(ex);}
    }

    private static CatalogEntry CatalogFromStudio(Product p)
    {
        var validUrl=ProductInputValidator.IsMercadoLivreListing(p.ListingUrl,true);
        var listing=validUrl && System.Text.RegularExpressions.Regex.IsMatch(
            p.ExternalId,@"\AMLB\d{6,18}\z",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return new CatalogEntry {
            Kind=listing?"listing":"offline",Origin="studio",ExternalId=p.ExternalId,Name=p.Name,
            Category=p.Category,CategoryId=p.CategoryId,SubcategoryId=p.SubcategoryId,
            Description=p.Description,ListingUrl=validUrl?p.ListingUrl:"",SourceFile="Studio SQLite"
        };
    }

    private void CaptureCatalogSuggestions(IEnumerable<ProductNameSuggestion> suggestions, string origin)
    {
        var rows=suggestions.Where(s=>(s.Kind is "listing" or "catalog") &&
            !string.IsNullOrWhiteSpace(s.Name)).Select(s=>new CatalogEntry{
            Kind=s.Kind,Origin=origin,ExternalId=s.Id,Name=s.Name,
            CategoryId=s.CategoryId,Description=s.Description,
            ListingUrl=ProductInputValidator.IsMercadoLivreListing(s.Permalink)
                ? s.Permalink:"",
            ImageUrl=s.Thumbnail,Price=s.Price,Currency=s.Currency,
            SourceFile="Mercado Livre: "+origin
            // Sugestão não comprova anúncio ativo ou comissão sem consulta individual.
        }).ToArray();
        if(rows.Length==0)return;
        _catalog.UpsertBatch(rows,"Descoberta "+origin);
        ScheduleCatalogExcel();
    }

    private void CaptureCatalogSuggestion(ProductNameSuggestion suggestion, string origin)
        => CaptureCatalogSuggestions(new[]{suggestion},origin);

    private void CaptureCatalogListings(IEnumerable<MercadoLivreListing> listings)
    {
        var rows=listings.Where(x=>!string.IsNullOrWhiteSpace(x.Title)).Select(x=>new CatalogEntry{
            Kind="listing",Origin="api",ExternalId=x.Id,Name=x.Title,
            CategoryId=x.CategoryId,ListingUrl=x.Permalink,ImageUrl=x.Thumbnail,
            Price=x.Price,Currency=x.Currency,SourceFile="API busca oficial"
        }).ToArray();
        if(rows.Length==0)return;
        _catalog.UpsertBatch(rows,"Consulta de anúncios oficial");
        ScheduleCatalogExcel();
        if(_tabs.TabPages.Count>6)RefreshCatalog();
    }

    private async Task CollectHighlightsAsync()
    {
        var category = (_mlChildren.SelectedItem as MercadoLivreCategory)?.Id;
        if (string.IsNullOrWhiteSpace(category))
            category = (_mlCategories.SelectedItem as MercadoLivreCategory)?.Id;
        var root=MercadoLivreBridgeService.SavedRepository;
        if(string.IsNullOrWhiteSpace(category) || !MercadoLivreBridgeService.ValidRepository(root))
        {
            _catalogInfo.Text="Na aba 02 · Garimpo, carregue e selecione uma subcategoria oficial e configure o Python/OAuth.";
            return;
        }
        if(_catalogOperationsRunning)return;
        try
        {
            _catalogOperationsRunning=true;
            _catalogInfo.Text="Consultando ranking oficial e detalhes de até 15 produtos por categoria...";
            var response=await MercadoLivreBridgeService.HighlightsAsync(root,category);
            var rows=response.Suggestions.Select(s=>new CatalogEntry{
                Kind=s.Kind,Origin="api",ExternalId=s.Id,Name=s.Name,
                CategoryId=s.CategoryId,ListingUrl=s.Permalink,ImageUrl=s.Thumbnail,
                Price=s.Price,Currency=s.Currency,LastVerifiedUtc=s.Verified?
                    DateTime.UtcNow.ToString("O"):"",
                SourceFile="API destaques da categoria "+category
            }).ToArray();
            var total=await Task.Run(()=>_catalog.UpsertBatch(rows,"Ranking oficial "+category));
            RefreshCatalog();ScheduleCatalogExcel();
            _catalogInfo.Text=$"{total} item(ns) do ranking registrados. "+
                "Ranking não garante comissão nem disponibilidade futura. "+response.Warning;
        }
        catch(Exception ex)
        {
            _catalogInfo.Text="Coleta oficial indisponível: "+ex.Message+
                " (a API pode exigir permissão ou categoria folha).";
        }
        finally{_catalogOperationsRunning=false;}
    }

    private void PromoteCatalogEntry()
    {
        var row=SelectedCatalogEntry();
        if(row is null){MessageBox.Show("Selecione uma linha do catálogo.");return;}
        LoadCatalogEntry(row);
    }

    private void LoadCatalogEntry(CatalogEntry row)
    {
        NewProduct();
        _suppressProductNameLookup=true;
        _name.Text=ProductTitleFormatter.Suggest(row.Name);
        _suppressProductNameLookup=false;
        _source.Text = SidebarSearchItem.IsHistoricalSpanish(row)
            ? "Referência histórica MeLi 2019 (ES)"
            : row.Kind == "offline" ? "Catálogo importado" : "Mercado Livre";
        _description.Text=row.Description[..Math.Min(row.Description.Length,_description.MaxLength)];
        _category.Text=row.Category;
        if (!string.IsNullOrWhiteSpace(row.ImageUrl))
            _ = ShowProductSuggestionPreviewAsync(row.ImageUrl);
        if(row.Kind=="listing")
        {
            _externalId.Text=row.ExternalId;
            _listingUrl.Text=row.ListingUrl;
            if(row.AffiliateConfirmed)_affiliateUrl.Text=row.AffiliateUrl;
        }
        _current!.CategoryId=row.CategoryId;
        _current.SubcategoryId=row.SubcategoryId;
        SelectSavedProductCategory(_current);
        _pinTitle.Text=_name.Text[..Math.Min(_name.Text.Length,100)];
        _tabs.SelectedIndex=0;
        _footer.Text = SidebarSearchItem.IsHistoricalSpanish(row)
            ? "Base histórica em espanhol (2019), sem anúncio ou imagem atual. Busque um anúncio brasileiro antes de divulgar."
            : "Referência carregada. Revise anúncio, descrição e link antes de Salvar.";
        // Apenas IDs do catálogo oficial brasileiro podem ser enriquecidos.
        if (row.Kind is "listing" or "catalog" &&
            !string.IsNullOrWhiteSpace(row.ExternalId) &&
            MercadoLivreBridgeService.ValidRepository(MercadoLivreBridgeService.SavedRepository))
            _ = EnrichSelectedProductAsync(new ProductNameSuggestion {
                Kind = row.Kind, Id = row.ExternalId, Name = row.Name,
                CategoryId = row.CategoryId, Permalink = row.ListingUrl,
                Thumbnail = row.ImageUrl
            }, _current?.Id ?? "");
    }

    private async Task VerifyCatalogEntryAsync()
    {
        var entry=SelectedCatalogEntry();
        if(entry is null){MessageBox.Show("Selecione um produto no catálogo.");return;}
        if(entry.Kind is "offline" || string.IsNullOrWhiteSpace(entry.ExternalId))
        {
            _catalogInfo.Text="Referência offline não tem ID oficial verificável. Informe um anúncio real primeiro.";
            return;
        }
        if(!MercadoLivreBridgeService.ValidRepository(MercadoLivreBridgeService.SavedRepository))
        {
            _catalogInfo.Text="Configure o repositório Python e OAuth na aba 02 antes de atualizar.";
            return;
        }
        try
        {
            _catalogInfo.Text="Conferindo informações na API oficial...";
            var result=await MercadoLivreBridgeService.EnrichSuggestionAsync(
                MercadoLivreBridgeService.SavedRepository,new ProductNameSuggestion{
                    Kind=entry.Kind,Id=entry.ExternalId,Name=entry.Name,CategoryId=entry.CategoryId});
            var details=result.Suggestions.FirstOrDefault();
            if(details is null)throw new InvalidOperationException("Sem metadados de retorno.");
            var verified=details.CategorySource is "anuncio" or "catalogo" ||
                !string.IsNullOrWhiteSpace(details.DescriptionSource);
            var updated=new CatalogEntry{
                Kind=entry.Kind,Origin="api",ExternalId=entry.ExternalId,Name=entry.Name,
                Category= result.Categories.LastOrDefault()?.Name ?? entry.Category,
                CategoryId=result.Categories.FirstOrDefault()?.Id ?? entry.CategoryId,
                SubcategoryId=result.Categories.LastOrDefault()?.Id ?? entry.SubcategoryId,
                Description=details.Description,
                ListingUrl=ProductInputValidator.IsMercadoLivreListing(details.Permalink) ? details.Permalink : entry.ListingUrl,
                ImageUrl=string.IsNullOrWhiteSpace(details.Thumbnail) ? entry.ImageUrl : details.Thumbnail,
                Price=entry.Price,Currency=entry.Currency,
                LastVerifiedUtc=verified?DateTime.UtcNow.ToString("O"):"",
                SourceFile="API detalhes oficiais"
            };
            _catalog.UpsertBatch(new[]{updated},"Verificação API");
            RefreshCatalog();ScheduleCatalogExcel();
            _catalogInfo.Text=verified
                ? "Informações consultadas. Confira preço, link e elegibilidade de afiliado."
                : "API não confirmou detalhes suficientes; status permanece não validado.";
            if(result.Warning.Length>0)_footer.Text=result.Warning;
        }
        catch(Exception ex){_catalogInfo.Text="Verificação indisponível: "+ex.Message;}
    }

    private void ConfirmCatalogAffiliate()
    {
        var selected=SelectedCatalogEntry();
        if(selected is null){MessageBox.Show("Selecione um anúncio individual no catálogo.");return;}
        if(selected.Kind!="listing")
        {
            MessageBox.Show("Somente anúncios individuais podem receber link oficial confirmado.");
            return;
        }
        // Atribuição do link por ação humana, nunca pela URL pública do anúncio.
        var current=Microsoft.VisualBasic.Interaction.InputBox(
            "Cole o link gerado pela Barra/Central de Afiliados para ESTE anúncio:",
            "Confirmar link de afiliado",selected.AffiliateUrl);
        if(string.IsNullOrWhiteSpace(current))return;
        if(MessageBox.Show(
            "Confirma que este link foi gerado para o anúncio selecionado?\n"+
            selected.Name+"\n"+selected.ExternalId,
            "Confirmação humana",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes) return;
        try
        {
            _catalog.ConfirmAffiliate(selected.Key,current);
            RefreshCatalog();ScheduleCatalogExcel();
            _catalogInfo.Text="Link registrado como confirmado pelo usuário. A comissão depende do programa do Mercado Livre.";
        }
        catch(Exception ex){ShowError(ex);}
    }
}
