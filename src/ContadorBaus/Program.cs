namespace ContadorBaus;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Vigia do "Iniciar com o Fortnite": sem janela, só abre a app quando o jogo arranca.
        if (args.Contains(FortniteLauncher.WatchArg))
        {
            FortniteLauncher.RunWatcher();
            return;
        }

        var fromFortnite = args.Contains(FortniteLauncher.FromFortniteArg);
        ApplyLanguage();

        // Uma só instância: se já estiver aberta, não abre outra.
        using var mutex = new Mutex(true, FortniteLauncher.AppMutex, out var first);
        if (!first)
        {
            if (!fromFortnite)
                MessageBox.Show(Lang.F("app.alreadyOpen", Lang.AppName), Lang.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        ApplicationConfiguration.Initialize();
        try { FortniteLauncher.EnsureWatcher(); } catch { }

        // Abertura: sempre, menos quando é o Fortnite a abrir a app (não tira o foco ao jogo) ou em diagnóstico.
        var capture = args.Contains("--captura");
        var splashCapture = Array.IndexOf(args, "--captura-abertura");
        if (!fromFortnite && !capture)
        {
            var firstRun = SavedLanguage() == null || args.Contains("--escolher-idioma") || splashCapture >= 0;
            using var splash = new SplashForm(askLanguage: firstRun);
            if (splashCapture >= 0 && splashCapture + 1 < args.Length) splash.CaptureAt(args[splashCapture + 1], 300, 900, 1300, 2000, 2700);
            Application.Run(splash);
            if (splash.Chosen is { } chosen)
            {
                try { new HistoryStore().UpdateSettings(s => s.Language = Lang.Code(chosen)); } catch { }
            }
            if (splashCapture >= 0) return;
            ApplyLanguage();
        }

        // Aberta pelo Fortnite: começa na bandeja para não tirar o foco ao jogo.
        var form = new MainForm(startInTray: fromFortnite);
        // Diagnóstico: "--captura pasta" guarda uma imagem de cada página quando a análise acaba e fecha.
        var i = Array.IndexOf(args, "--captura");
        if (i >= 0 && i + 1 < args.Length) form.CaptureWhenIdle(args[i + 1]);
        Application.Run(form);

        // Mudança de idioma: a janela fecha e abre-se outra na nova língua, na mesma página.
        while (form.RestartRequested)
        {
            var page = form.CurrentPage;
            form.Dispose();
            ApplyLanguage();
            form = new MainForm(startPage: page);
            Application.Run(form);
        }
    }

    /// <summary>Língua guardada nas definições ou, se não houver, a do Windows.</summary>
    private static string? SavedLanguage()
    {
        try { return new HistoryStore().Settings.Language; } catch { return null; }
    }

    private static void ApplyLanguage()
    {
        Lang.Current = Lang.FromCode(SavedLanguage()) ?? Lang.FromSystem();
        var culture = Lang.Culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }
}
