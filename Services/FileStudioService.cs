using GarimpoDaVezStudio.Models;

namespace GarimpoDaVezStudio.Services;

public static class FileStudioService
{
    private static readonly HashSet<string> ImageExtensions = new(
        StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp" };

    public static string CopyOriginal(Product product, string sourceFile)
    {
        CheckImage(sourceFile);
        var dir = Path.Combine(AppPaths.ProductDirectory(product.Id), "original");
        Directory.CreateDirectory(dir);
        var dest = Path.Combine(dir, "original" + Path.GetExtension(sourceFile).ToLowerInvariant());
        if (!Path.GetFullPath(sourceFile).Equals(Path.GetFullPath(dest),
                StringComparison.OrdinalIgnoreCase))
            File.Copy(sourceFile, dest, overwrite: true);
        return dest;
    }

    public static string CopyGenerated(Product product, string sourceFile, int version)
    {
        CheckImage(sourceFile);
        var dir = Path.Combine(AppPaths.ProductDirectory(product.Id), "ia");
        Directory.CreateDirectory(dir);
        var dest = Path.Combine(dir, $"v{version:000}" +
            Path.GetExtension(sourceFile).ToLowerInvariant());
        if (!Path.GetFullPath(sourceFile).Equals(Path.GetFullPath(dest),
                StringComparison.OrdinalIgnoreCase))
            File.Copy(sourceFile, dest, overwrite: false);
        return dest;
    }

    public static void CheckImage(string path)
    {
        if (!File.Exists(path) || !ImageExtensions.Contains(Path.GetExtension(path)))
            throw new InvalidOperationException("Escolha uma imagem JPG, PNG ou BMP existente.");
        using var testImage = Image.FromFile(path);
        if (testImage.Width < 200 || testImage.Height < 200)
            throw new InvalidOperationException("Imagem muito pequena: use ao menos 200 x 200 px.");
    }

    public static void ShowImage(PictureBox preview, string? path)
    {
        preview.Image?.Dispose();
        preview.Image = null;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
        try
        {
            // Copia para memória e fecha o arquivo: evita bloqueio do arquivo original.
            var bytes = File.ReadAllBytes(path);
            using var stream = new MemoryStream(bytes);
            using var decoded = Image.FromStream(stream);
            preview.Image = new Bitmap(decoded);
        }
        catch
        {
            // Pré-visualização é opcional; arquivo original permanece intacto.
        }
    }
}
