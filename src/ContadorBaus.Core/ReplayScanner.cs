namespace ContadorBaus;

/// <summary>
/// Procura replays novos ou alterados na pasta do Fortnite e analisa-os numa thread própria.
/// O leitor de replays usa estado estático, por isso a análise é sempre sequencial.
/// </summary>
public sealed class ReplayScanner : IDisposable
{
    /// <summary>Um replay alterado há menos tempo do que isto ainda está a ser gravado (partida a decorrer).</summary>
    public static readonly TimeSpan QuietTime = TimeSpan.FromSeconds(20);

    private readonly HistoryStore _store;
    private readonly AutoResetEvent _wake = new(false);
    private readonly Thread _thread;
    private FileSystemWatcher? _watcher;
    private volatile bool _stop;
    private volatile bool _forceRetry;

    /// <summary>(feitos, total) durante uma ronda de análise. Chamado na thread de análise.</summary>
    public event Action<int, int>? Progress;
    /// <summary>Uma partida acabou de ser analisada.</summary>
    public event Action<MatchRecord>? MatchAnalyzed;
    /// <summary>Fim de uma ronda (true se algo mudou).</summary>
    public event Action<bool>? ScanFinished;
    /// <summary>Há um replay a ser gravado neste momento (partida em curso).</summary>
    public event Action<bool>? LiveMatch;

    public ReplayScanner(HistoryStore store)
    {
        _store = store;
        _thread = new Thread(Loop) { IsBackground = true, Name = "Análise de replays", Priority = ThreadPriority.BelowNormal };
    }

    public string Folder => string.IsNullOrWhiteSpace(_store.Settings.ReplayFolder) ? ReplayFolder.Default : _store.Settings.ReplayFolder!;

    public void Start()
    {
        StartWatcher();
        _thread.Start();
    }

    /// <summary>Pede uma nova ronda. <paramref name="retryFailed"/> volta a tentar replays que falharam.</summary>
    public void Rescan(bool retryFailed = false)
    {
        if (retryFailed) _forceRetry = true;
        _wake.Set();
    }

    public void FolderChanged()
    {
        StartWatcher();
        Rescan();
    }

    private void StartWatcher()
    {
        _watcher?.Dispose();
        _watcher = null;
        if (!Directory.Exists(Folder)) return;
        _watcher = new FileSystemWatcher(Folder, "*.replay")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };
        _watcher.Created += (_, _) => _wake.Set();
        _watcher.Changed += (_, _) => _wake.Set();
        _watcher.Renamed += (_, _) => _wake.Set();
    }

    private void Loop()
    {
        while (!_stop)
        {
            bool changed = false;
            var waitMs = 60_000;
            try
            {
                changed = ScanOnce(out var pendingLive);
                // Uma partida a ser gravada: volta a ver pouco depois de o ficheiro parar de mudar.
                if (pendingLive) waitMs = (int)QuietTime.TotalMilliseconds + 2000;
            }
            catch
            {
                // Pasta inacessível, etc.: tenta outra vez mais tarde.
            }
            ScanFinished?.Invoke(changed);
            _wake.WaitOne(waitMs);
            // Junta várias notificações seguidas do FileSystemWatcher numa só ronda.
            if (!_stop) Thread.Sleep(500);
        }
    }

    private bool ScanOnce(out bool pendingLive)
    {
        pendingLive = false;
        if (!Directory.Exists(Folder))
        {
            LiveMatch?.Invoke(false);
            return false;
        }

        var retry = _forceRetry;
        _forceRetry = false;
        var now = DateTime.UtcNow;
        var todo = new List<FileInfo>();
        foreach (var fi in new DirectoryInfo(Folder).GetFiles("*.replay"))
        {
            if (now - fi.LastWriteTimeUtc < QuietTime)
            {
                pendingLive = true;
                continue;
            }
            var existing = _store.Find(fi.Name);
            if (existing == null
                || existing.AnalyzerVersion < ChestAnalyzer.Version
                || existing.FileSize != fi.Length
                || existing.FileWriteUtc != fi.LastWriteTimeUtc
                || (retry && !existing.IsValid))
            {
                todo.Add(fi);
            }
        }
        LiveMatch?.Invoke(pendingLive);
        if (todo.Count == 0) return false;

        todo.Sort((a, b) => a.LastWriteTimeUtc.CompareTo(b.LastWriteTimeUtc));
        Progress?.Invoke(0, todo.Count);
        for (var i = 0; i < todo.Count && !_stop; i++)
        {
            var record = AnalyzeSafe(todo[i]);
            _store.Upsert(record);
            // Guarda a cada 10 replays para não perder trabalho se a app for fechada a meio.
            if (i % 10 == 9) _store.Save();
            MatchAnalyzed?.Invoke(record);
            Progress?.Invoke(i + 1, todo.Count);
        }
        _store.Save();
        return true;
    }

    public static MatchRecord AnalyzeSafe(FileInfo fi)
    {
        try
        {
            var task = Task.Run(() => new ChestAnalyzer().Analyze(fi.FullName));
            // Proteção contra um replay que faça o leitor entrar em ciclo.
            if (!task.Wait(TimeSpan.FromMinutes(3)))
            {
                return Failed(fi, "A análise demorou demasiado.");
            }
            return task.Result;
        }
        catch (Exception e)
        {
            var inner = e is AggregateException ae ? ae.InnerException ?? e : e;
            return Failed(fi, $"{inner.GetType().Name}: {inner.Message}");
        }
    }

    private static MatchRecord Failed(FileInfo fi, string error) => new()
    {
        ReplayFile = fi.Name,
        FileSize = fi.Length,
        FileWriteUtc = fi.LastWriteTimeUtc,
        StartUtc = fi.CreationTimeUtc,
        AnalyzerVersion = ChestAnalyzer.Version,
        Error = error,
    };

    public void Dispose()
    {
        _stop = true;
        _wake.Set();
        _watcher?.Dispose();
        // Espera que a análise em curso termine: o leitor de replays tem estado estático e não pode correr duas vezes.
        if (_thread.IsAlive) _thread.Join(TimeSpan.FromSeconds(20));
    }
}
