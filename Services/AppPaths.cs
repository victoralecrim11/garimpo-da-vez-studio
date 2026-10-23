namespace GarimpoDaVezStudio.Services;

public static class AppPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GarimpoDaVezStudio");

    public static string DatabaseFile => Path.Combine(Root, "studio.sqlite");
    public static string AssetsDirectory => Path.Combine(Root, "produtos");

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(AssetsDirectory);
        var promptDir = Path.Combine(Root, "prompts");
        Directory.CreateDirectory(promptDir);
        var defaultTemplate = Path.Combine(AppContext.BaseDirectory, "prompts", "pinterest-template.txt");
        var editableTemplate = Path.Combine(promptDir, "pinterest-template.txt");
        if (!File.Exists(editableTemplate) && File.Exists(defaultTemplate))
            File.Copy(defaultTemplate, editableTemplate);
    }

    public static string ProductDirectory(string productId) =>
        Path.Combine(AssetsDirectory, productId);

    public static string UtcNow() => DateTime.UtcNow.ToString("O");
}
