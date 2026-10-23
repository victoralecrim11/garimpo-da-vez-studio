using GarimpoDaVezStudio.Data;
using GarimpoDaVezStudio.Services;
using GarimpoDaVezStudio.UI;

namespace GarimpoDaVezStudio;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        try
        {
            AppPaths.EnsureDirectories();
            var repository = new StudioRepository(AppPaths.DatabaseFile);
            repository.Initialize();
            Application.Run(new MainForm(repository));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Não foi possível iniciar o aplicativo:\n\n{ex.Message}",
                "Garimpo da Vez Studio", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
