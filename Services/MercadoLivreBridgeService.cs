using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GarimpoDaVezStudio.Services;

public sealed class MercadoLivreCategory
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public override string ToString() => Name;
}

public sealed class MercadoLivreListing
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    [JsonPropertyName("category_id")]
    public string CategoryId { get; set; } = "";
    public string Permalink { get; set; } = "";
    public string Thumbnail { get; set; } = "";
    public decimal? Price { get; set; }
    public string Currency { get; set; } = "";
}

public sealed class ProductNameSuggestion
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    [JsonPropertyName("category_id")]
    public string CategoryId { get; set; } = "";
    public string Permalink { get; set; } = "";
    public string Thumbnail { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Verified { get; set; }
    public string LocalProductId { get; set; } = "";

    [JsonIgnore]
    public string Display => Kind switch
    {
        "local" => "Meu cadastro · " + Name,
        "listing" => "Anúncio · " + Name,
        "catalog" => "Referência de catálogo · " + Name,
        _ => Name
    };
}

public sealed class MercadoLivreResponse
{
    public bool Ok { get; set; }
    public string Error { get; set; } = "";
    public string Origin { get; set; } = "";
    public string Warning { get; set; } = "";
    public List<ProductNameSuggestion> Suggestions { get; set; } = new();
    public bool Authenticated { get; set; }
    public bool TokenValid { get; set; }
    public List<MercadoLivreCategory> Categories { get; set; } = new();
    public List<MercadoLivreListing> Items { get; set; } = new();
}

public static class MercadoLivreBridgeService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string SuggestedRepository()
    {
        var env = Environment.GetEnvironmentVariable("GARIMPO_PYTHON_REPO");
        if (ValidRepository(env)) return Path.GetFullPath(env!);
        var baseDirs = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
        foreach (var start in baseDirs)
        {
            var directory = new DirectoryInfo(start);
            for (var level = 0; directory is not null && level < 9; level++, directory = directory.Parent)
            {
                if (ValidRepository(directory.FullName)) return directory.FullName;
                var sibling = Path.Combine(directory.FullName, "garimpo-da-vez");
                if (ValidRepository(sibling)) return sibling;
            }
        }
        return "";
    }

    public static bool ValidRepository(string? root) =>
        !string.IsNullOrWhiteSpace(root) &&
        File.Exists(Path.Combine(root, "automation", "ml_discovery.py")) &&
        File.Exists(Path.Combine(root, "automation", "garimpo.py"));

    public static string SavedRepository
    {
        get
        {
            var file = Path.Combine(AppPaths.Root, "python-repository.txt");
            var saved = File.Exists(file) ? File.ReadAllText(file).Trim() : "";
            return ValidRepository(saved) ? saved : SuggestedRepository();
        }
    }

    public static void SaveRepository(string root)
    {
        if (!ValidRepository(root))
            throw new InvalidOperationException("Selecione a pasta raiz do repositório garimpo-da-vez, que contém automation/ml_discovery.py.");
        Directory.CreateDirectory(AppPaths.Root);
        File.WriteAllText(Path.Combine(AppPaths.Root, "python-repository.txt"), Path.GetFullPath(root));
    }

    public static Task<MercadoLivreResponse> OAuthStatusAsync(string root) =>
        ExecuteAsync(root, "oauth_status");

    public static Task<MercadoLivreResponse> CategoriesAsync(string root) =>
        ExecuteAsync(root, "categories");

    public static Task<MercadoLivreResponse> ChildrenAsync(string root, string category) =>
        ExecuteAsync(root, "children", "--category", category);

    public static Task<MercadoLivreResponse> SearchAsync(string root, string query, string category) =>
        ExecuteAsync(root, "search", "--query", query, "--category", category);

    public static Task<MercadoLivreResponse> SuggestNamesAsync(string root, string query, string category) =>
        ExecuteAsync(root, "suggest", "--query", query, "--category", category);

    public static Task<MercadoLivreResponse> PreviewByUrlAsync(string root, string url) =>
        ExecuteAsync(root, "preview_url", "--url", url);

    public static Task<MercadoLivreResponse> IngestAsync(string root, MercadoLivreListing item) =>
        ExecuteAsync(root, "ingest", "--url", item.Permalink, "--item-id", item.Id);

    public static Task<MercadoLivreResponse> IngestUrlAsync(string root, string url) =>
        ExecuteAsync(root, "manual", "--url", url);

    private static async Task<MercadoLivreResponse> ExecuteAsync(string root, string action, params string[] arguments)
    {
        if (!ValidRepository(root))
            throw new InvalidOperationException("Configure o diretório do repositório Python garimpo-da-vez antes de pesquisar.");

        var executable = Path.Combine(root, ".venv", "Scripts", "python.exe");
        if (!File.Exists(executable)) executable = "python";
        var script = Path.Combine(AppContext.BaseDirectory, "integration", "ml_bridge.py");
        if (!File.Exists(script))
            throw new FileNotFoundException("Ponte Python não encontrada na pasta integration.", script);

        var start = new ProcessStartInfo(executable)
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        start.ArgumentList.Add("-X");
        start.ArgumentList.Add("utf8");
        start.ArgumentList.Add(script);
        start.ArgumentList.Add("--root");
        start.ArgumentList.Add(Path.GetFullPath(root));
        start.ArgumentList.Add("--action");
        start.ArgumentList.Add(action);
        foreach (var arg in arguments) start.ArgumentList.Add(arg);

        using var process = Process.Start(start) ??
            throw new InvalidOperationException("Não foi possível iniciar o Python.");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(65));
        try
        {
            var standardOutput = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var standardError = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            var output = await standardOutput;
            _ = await standardError;
            if (output.Length > 2_000_000)
                throw new InvalidOperationException("Resposta Python excedeu o tamanho permitido.");
            MercadoLivreResponse? response;
            try { response = JsonSerializer.Deserialize<MercadoLivreResponse>(output, JsonOptions); }
            catch (JsonException) { throw new InvalidOperationException("Ponte Python retornou JSON inválido. Verifique o repositório Python configurado."); }
            if (response is null)
                throw new InvalidOperationException("Ponte Python não retornou uma resposta.");
            if (!response.Ok || process.ExitCode != 0)
                throw new InvalidOperationException(
                    !string.IsNullOrWhiteSpace(response.Error)
                    ? response.Error
                    : "Consulta ao Mercado Livre falhou; confira a autenticação e a conectividade.");
            return response;
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            throw new TimeoutException("Consulta demorou mais que 65 segundos; tente novamente.");
        }
    }
}
