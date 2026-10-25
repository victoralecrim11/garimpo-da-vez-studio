using System.Diagnostics;

namespace GarimpoDaVezStudio.Services;

/// <summary>
/// Não implementa OAuth novo: abre o CLI Python oficial em um terminal interativo.
/// O Client Secret é solicitado por getpass() e os tokens são mantidos pelo keyring Python.
/// </summary>
public static class MercadoLivreOAuthService
{
    private static string AppIdFile => Path.Combine(AppPaths.Root, "mercadolivre-app-id.txt");

    public static string SavedAppId()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("MERCADOLIVRE_CLIENT_ID");
        if (ProductInputValidator.IsValidClientId(fromEnvironment))
            return fromEnvironment!.Trim();
        if (!File.Exists(AppIdFile)) return "";
        var stored = File.ReadAllText(AppIdFile).Trim();
        return ProductInputValidator.IsValidClientId(stored) ? stored : "";
    }

    public static void OpenOriginalLoginTerminal(string root, string clientId)
    {
        if (!MercadoLivreBridgeService.ValidRepository(root) ||
            !File.Exists(Path.Combine(root, "automation", "ml_oauth.py")))
            throw new InvalidOperationException(
                "Selecione a pasta do garimpo-da-vez com automation/ml_oauth.py.");
        if (!ProductInputValidator.IsValidClientId(clientId))
            throw new InvalidOperationException(
                "O App ID do Mercado Livre deve conter somente de 5 a 30 dígitos.");

        var python = Path.Combine(root, ".venv", "Scripts", "python.exe");
        if (!File.Exists(python)) python = "python";
        // O script é conhecido. Caminhos são escapados como literais PowerShell.
        var command = "& '" + python.Replace("'", "''") +
                      "' 'automation/ml_oauth.py' login";
        var start = new ProcessStartInfo("powershell.exe")
        {
            WorkingDirectory = root,
            UseShellExecute = false,
            CreateNoWindow = false
        };
        start.Environment["MERCADOLIVRE_CLIENT_ID"] = clientId;
        start.ArgumentList.Add("-NoExit");
        start.ArgumentList.Add("-Command");
        start.ArgumentList.Add(command);

        Directory.CreateDirectory(AppPaths.Root);
        File.WriteAllText(AppIdFile, clientId); // App ID público; nunca guardar Client Secret.
        _ = Process.Start(start) ??
            throw new InvalidOperationException("Não foi possível abrir o PowerShell.");
    }
}
