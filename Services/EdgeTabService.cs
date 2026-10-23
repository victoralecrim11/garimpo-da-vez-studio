using System.Text.Json;

namespace GarimpoDaVezStudio.Services;

public static class EdgeTabService
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(4) };

    // Ativa uma aba EXISTENTE; não abre navegador, guia ou faz login.
    // Requer Edge iniciado com --remote-debugging-port=9333.
    public static async Task<bool> ActivateExistingChatGptTabAsync()
    {
        using var response = await Client.GetAsync("http://127.0.0.1:9333/json/list");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        foreach (var tab in document.RootElement.EnumerateArray())
        {
            if (!tab.TryGetProperty("type", out var kind) || kind.GetString() != "page" ||
                !tab.TryGetProperty("url", out var urlField) ||
                !Uri.TryCreate(urlField.GetString(), UriKind.Absolute, out var url) ||
                !(url.Host.Equals("chatgpt.com", StringComparison.OrdinalIgnoreCase) ||
                  url.Host.Equals("chat.openai.com", StringComparison.OrdinalIgnoreCase)) ||
                !tab.TryGetProperty("id", out var target)) continue;

            string id = Uri.EscapeDataString(target.GetString() ?? "");
            using var activated = await Client.GetAsync(
                "http://127.0.0.1:9333/json/activate/" + id);
            return activated.IsSuccessStatusCode;
        }
        return false;
    }
}
