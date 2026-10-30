using GarimpoDaVezStudio.Data;
using GarimpoDaVezStudio.UI;
using GarimpoDaVezStudio.Services;

[STAThread]
static void Run()
{
    Application.EnableVisualStyles();
    var directory = Path.Combine(Path.GetTempPath(),"gdv-layout-"+Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        // Reproduz a inicialização real: runner Windows limpo não possui a pasta LocalAppData.
        AppPaths.EnsureDirectories();
        var db = new StudioRepository(Path.Combine(directory,"smoke.sqlite"));
        db.Initialize();
        using var window = new MainForm(db) { ShowInTaskbar = false, Opacity = 0 };
        window.Show();
        Application.DoEvents();
        var split = Traverse(window).OfType<SplitContainer>().Single();
        if (split.Panel1.Width < 280 || split.Panel2.Width < 650)
            throw new Exception($"Painéis fora dos limites iniciais: {split.Panel1.Width}/{split.Panel2.Width}");
        window.Width = window.MinimumSize.Width + 40;
        Application.DoEvents();
        if (split.Panel1.Width < 280 || split.Panel2.Width < 650)
            throw new Exception($"Painéis fora dos limites após redimensionamento: {split.Panel1.Width}/{split.Panel2.Width}");
        var tabs = Traverse(window).OfType<TabControl>().Single();
        if (tabs.TabCount != 7) throw new Exception("Abas do Studio alteradas.");
        var imagePicker = Traverse(window).OfType<PictureBox>()
            .SingleOrDefault(x=>x.AccessibleName=="Selecionar foto de referência");
        if (imagePicker is null || imagePicker.Cursor != Cursors.Hand)
            throw new Exception("Foto de referência não está indicada como seletor clicável.");
        var search = Traverse(window).OfType<ListBox>()
            .FirstOrDefault(x=>x.DrawMode==DrawMode.OwnerDrawFixed && x.ItemHeight>=65);
        if (search is null) throw new Exception("Resultados laterais não têm layout legível.");
        tabs.SelectedIndex = 6;
        Application.DoEvents();
        var toolbar = Traverse(window).OfType<TableLayoutPanel>()
            .Single(x=>x.Name=="CatalogActionsToolbar");
        var buttons = toolbar.Controls.OfType<Button>().ToList();
        if (toolbar.RowCount!=2 || toolbar.ColumnCount!=4 || buttons.Count!=8)
            throw new Exception("Ações do catálogo precisam ocupar duas linhas de quatro colunas.");
        foreach(var b in buttons)
        {
            var cell=toolbar.GetPositionFromControl(b);
            if (b.Width<95 || b.Height<32 || cell.Column<0 || cell.Column>3 ||
                cell.Row<0 || cell.Row>1)
                throw new Exception("Botões do catálogo estão comprimidos ou fora do grid.");
        }
        var searchField = Traverse(window).Single(x=>x.Name=="CatalogSearchField");
        if (searchField.Width<300 || searchField.Width>760 || searchField.Height<32)
            throw new Exception($"Campo de pesquisa desproporcional: {searchField.Size}.");
        var alignedDiff=Math.Abs(searchField.PointToScreen(Point.Empty).X -
            buttons.Single(x=>toolbar.GetPositionFromControl(x).Column==0 &&
                              toolbar.GetPositionFromControl(x).Row==0)
                .PointToScreen(Point.Empty).X);
        if (alignedDiff>10)
            throw new Exception($"Busca e ações desalinhadas por {alignedDiff}px.");

        window.Width=1900;
        Application.DoEvents();
        if (searchField.Width>760 || buttons.Any(x=>x.Width<95))
            throw new Exception("A busca cresceu excessivamente ou botões perderam largura.");
        Console.WriteLine("8 verificações de layout, ações, busca responsiva e abertura aprovadas.");
        window.Close();
    }
    finally {try{Directory.Delete(directory,true);}catch(IOException){}}
}
static IEnumerable<Control> Traverse(Control root)
{
    foreach(Control child in root.Controls)
    {
        yield return child;
        foreach(var descendant in Traverse(child)) yield return descendant;
    }
}
Run();
