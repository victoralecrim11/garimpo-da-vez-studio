using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace GarimpoDaVezStudio.Services;

public static class EdgeWindowService
{
    // Fallback sem CDP: ativa uma JANELA do Edge cuja aba ATIVA contenha ChatGPT
    // no título. Não vê abas inativas e não interage com a página.
    public static bool ActivateVisibleChatGptWindow()
    {
        nint matched = 0;
        EnumWindows((window, _) =>
        {
            if (!IsWindowVisible(window)) return true;
            var length = GetWindowTextLength(window);
            if (length <= 0) return true;
            var text = new StringBuilder(length + 1);
            GetWindowText(window, text, text.Capacity);
            if (!text.ToString().Contains("ChatGPT", StringComparison.OrdinalIgnoreCase))
                return true;

            GetWindowThreadProcessId(window, out uint processId);
            try
            {
                using var process = Process.GetProcessById((int)processId);
                if (!process.ProcessName.Equals("msedge", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch { return true; }
            matched = window;
            return false;
        }, 0);
        if (matched == 0) return false;
        ShowWindow(matched, 9); // SW_RESTORE
        return SetForegroundWindow(matched);
    }

    private delegate bool EnumWindowsCallback(nint hwnd, nint param);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsCallback callback, nint param);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hwnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(nint hwnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hwnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hwnd, int command);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hwnd);
}
