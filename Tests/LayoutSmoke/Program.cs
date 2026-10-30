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
        Console.WriteLine("5 verificações de criação, responsividade, abas, imagem e lista aprovadas.");
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
