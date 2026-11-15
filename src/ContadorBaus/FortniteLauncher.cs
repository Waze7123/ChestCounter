using System.Diagnostics;
using Microsoft.Win32;

namespace ContadorBaus;

/// <summary>
/// "Iniciar com o Fortnite": um pequeno processo vigia (sem janela) arranca com o Windows e abre a app
/// quando o Fortnite começa. O vigia fica registado em HKCU\...\Run, só para este utilizador.
/// </summary>
internal static class FortniteLauncher
{
    public const string WatchArg = "--esperar-fortnite";
    public const string FromFortniteArg = "--com-fortnite";
    public const string AppMutex = "ContadorBaus.Fortnite.SingleInstance";
    private const string WatcherMutex = "ContadorBaus.Fortnite.Watcher";
    private const string StopEvent = "ContadorBaus.Fortnite.Watcher.Stop";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    // Nome próprio: "ChestCounter" já é usado por outra app com o mesmo nome (xdoofy92/ChestCounter).
    private const string RunValue = "ContadorBaus";

    /// <summary>Processos do jogo (cliente normal e anti-cheat EAC).</summary>
    private static readonly string[] GameProcesses = { "FortniteClient-Win64-Shipping", "FortniteClient-Win64-Shipping_EAC_EOS" };

    private static string ExePath => Environment.ProcessPath ?? Application.ExecutablePath;

    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(RunValue) is string;
        }
    }

    /// <summary>Liga/desliga: regista o vigia no arranque do Windows e arranca-o (ou pára-o) já.</summary>
    public static void SetEnabled(bool enabled)
    {
        using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
        {
            if (enabled) key.SetValue(RunValue, $"\"{ExePath}\" {WatchArg}");
            else key.DeleteValue(RunValue, throwOnMissingValue: false);
        }
        if (enabled) StartWatcher();
        else StopWatcher();
    }

    /// <summary>Se a opção estiver ligada mas o vigia não estiver a correr (ex.: o .exe mudou de sítio), corrige.</summary>
    public static void EnsureWatcher()
    {
        if (!IsEnabled) return;
        using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
        {
            key.SetValue(RunValue, $"\"{ExePath}\" {WatchArg}");
        }
        StartWatcher();
    }

    private static void StartWatcher()
    {
        if (Mutex.TryOpenExisting(WatcherMutex, out var existing)) { existing.Dispose(); return; }
        Process.Start(new ProcessStartInfo(ExePath, WatchArg) { UseShellExecute = false });
    }

    private static void StopWatcher()
    {
        if (EventWaitHandle.TryOpenExisting(StopEvent, out var stop))
        {
            using (stop) stop.Set();
        }
    }

    public static bool FortniteRunning() => GameProcesses.Any(n =>
    {
        var ps = Process.GetProcessesByName(n);
        foreach (var p in ps) p.Dispose();
        return ps.Length > 0;
    });

    /// <summary>Ciclo do vigia: verifica a cada 5 s; quando o Fortnite arranca e a app não está aberta, abre-a.</summary>
    public static void RunWatcher()
    {
        using var mutex = new Mutex(true, WatcherMutex, out var first);
        if (!first) return;
        using var stop = new EventWaitHandle(false, EventResetMode.ManualReset, StopEvent);
        try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal; } catch { }

        var wasRunning = false;
        while (!stop.WaitOne(TimeSpan.FromSeconds(5)))
        {
            // A opção foi desligada noutro sítio (ou a entrada apagada): o vigia termina.
            if (!IsEnabled) return;
            var running = FortniteRunning();
            if (running && !wasRunning && !AppIsOpen())
            {
                try { Process.Start(new ProcessStartInfo(ExePath, FromFortniteArg) { UseShellExecute = false }); } catch { }
            }
            wasRunning = running;
        }
    }

    private static bool AppIsOpen()
    {
        if (!Mutex.TryOpenExisting(AppMutex, out var m)) return false;
        m.Dispose();
        return true;
    }
}
