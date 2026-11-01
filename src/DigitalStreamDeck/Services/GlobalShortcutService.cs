using System.Runtime.InteropServices;
using System.Diagnostics;

namespace DigitalStreamDeck.Services;

public sealed class GlobalShortcutService : IDisposable
{
    private const int WhMouseLl = 14;
    private const int VkMenu = 0x12;
    private readonly ShortcutGestureFilter filter = new();
    private readonly HookProcedure callback;
    private nint hook;
    private bool disposed;

    public GlobalShortcutService() => callback = HookCallback;

    public event EventHandler? ToggleRequested;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (hook != 0) return;

        hook = SetWindowsHookEx(WhMouseLl, callback, 0, 0);
        if (hook == 0)
            throw new InvalidOperationException("Klavye kısayolu başlatılamadı. Lütfen uygulamayı yeniden başlatın.");
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (hook != 0)
        {
            UnhookWindowsHookEx(hook);
            hook = 0;
        }
    }

    private nint HookCallback(int code, nint wParam, nint lParam)
    {
        try
        {
            if (code >= 0)
            {
                var mouse = Marshal.PtrToStructure<MouseHookData>(lParam);
                var decision = filter.Process(unchecked((int)wParam), mouse.MouseData,
                    (GetAsyncKeyState(VkMenu) & 0x8000) != 0, Environment.TickCount64);
                if (DispatchDecision(decision) != 0) return 1;
            }

            return CallNextHookEx(hook, code, wParam, lParam);
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
            try { return CallNextHookEx(hook, code, wParam, lParam); }
            catch (Exception) { return 0; }
        }
    }

    internal nint DispatchDecision(ShortcutDecision decision)
    {
        if (decision == ShortcutDecision.ToggleAndConsume)
        {
            try { ToggleRequested?.Invoke(this, EventArgs.Empty); }
            catch (Exception ex) { ReportFailure(ex); }
        }

        return decision == ShortcutDecision.PassThrough ? 0 : 1;
    }

    private static void ReportFailure(Exception exception)
    {
        try { Trace.TraceError("Global shortcut callback failed: {0}", exception); }
        catch (Exception) { /* Diagnostics must not escape a native callback. */ }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseHookData
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public nuint ExtraInfo;
    }

    private delegate nint HookProcedure(int code, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, HookProcedure lpfn, nint hmod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
