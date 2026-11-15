namespace ContadorBaus;

/// <summary>
/// Estado partilhado por todas as páginas: histórico, scanner de replays, período e opções de contagem.
/// Todos os eventos são entregues na thread da interface.
/// </summary>
internal sealed class AppState : IDisposable
{
    private readonly Control _ui;
    private readonly System.Windows.Forms.Timer _coalesce = new() { Interval = 700 };
    private bool _dirty;

    public HistoryStore Store { get; } = new();
    public ReplayScanner Scanner { get; }

    /// <summary>Partidas do período, da mais recente para a mais antiga.</summary>
    public List<MatchRecord> Period { get; private set; } = new();
    /// <summary>Todas as partidas válidas (para o total geral e o calendário).</summary>
    public List<MatchRecord> AllValid { get; private set; } = new();
    public int FailedCount { get; private set; }
    public int CreativeSkipped { get; private set; }
    public CountingOptions Options { get; private set; } = CountingOptions.ChestsOnly;

    public bool Live { get; private set; }
    public (int Done, int Total)? Progress { get; private set; }

    /// <summary>Os dados ou as definições mudaram: as páginas voltam a desenhar.</summary>
    public event Action? Changed;
    /// <summary>Estado do scanner (progresso, partida a decorrer) mudou.</summary>
    public event Action? StatusChanged;

    public AppState(Control ui)
    {
        _ui = ui;
        Scanner = new ReplayScanner(Store);
        _coalesce.Tick += (_, _) => { if (_dirty) { _dirty = false; Recompute(); } };
        Scanner.MatchAnalyzed += _ => _dirty = true;
        Scanner.Progress += (done, total) => Post(() =>
        {
            Progress = done < total ? (done, total) : null;
            StatusChanged?.Invoke();
        });
        Scanner.ScanFinished += changed => Post(() =>
        {
            Progress = null;
            if (changed) Recompute();
            StatusChanged?.Invoke();
        });
        Scanner.LiveMatch += live => Post(() =>
        {
            if (Live == live) return;
            Live = live;
            StatusChanged?.Invoke();
        });
    }

    public AppSettings Settings => Store.Settings;
    public DateTime? StartDate => Settings.StartDate;

    public void Start()
    {
        Recompute();
        _coalesce.Start();
        Scanner.Start();
    }

    private void Post(Action a)
    {
        if (_ui.IsDisposed || !_ui.IsHandleCreated) return;
        try { _ui.BeginInvoke(a); } catch (ObjectDisposedException) { } catch (InvalidOperationException) { }
    }

    public void Recompute()
    {
        var all = Store.Snapshot();
        AllValid = all.Where(m => m.IsValid).OrderByDescending(m => m.StartUtc).ToList();
        FailedCount = all.Count(m => m.Error != null);
        CreativeSkipped = all.Count(m => m.Empty && m.Creative);
        Options = Settings.Counting();
        Period = Stats.InPeriod(AllValid, StartDate);
        Changed?.Invoke();
    }

    public void SetStartDate(DateTime? date)
    {
        Store.UpdateSettings(s => s.StartDate = date?.Date);
        Recompute();
    }

    public void SetExtraCounted(string kind, bool counted)
    {
        Store.UpdateSettings(s =>
        {
            s.CountedExtras.Remove(kind);
            if (counted) s.CountedExtras.Add(kind);
        });
        Recompute();
    }

    public void UpdateSettings(Action<AppSettings> change, bool recompute = true)
    {
        Store.UpdateSettings(change);
        if (recompute) Recompute();
    }

    public int Count(MatchRecord m) => Stats.Count(m, Options);

    public DateTime EarliestDay => AllValid.Count > 0 ? AllValid[^1].StartLocal.Date : DateTime.Today;

    public void Dispose()
    {
        _coalesce.Dispose();
        Scanner.Dispose();
        try { Store.Save(); } catch { }
    }
}
