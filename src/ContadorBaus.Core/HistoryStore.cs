using System.Text.Json;
using System.Text.Json.Serialization;

namespace ContadorBaus;

public sealed class AppSettings
{
    /// <summary>Data (local) a partir da qual se contam os baús. Nulo = desde sempre.</summary>
    public DateTime? StartDate { get; set; }
    /// <summary>Pasta dos replays. Nulo = pasta normal do Fortnite.</summary>
    public string? ReplayFolder { get; set; }
    /// <summary>Tipos de contentor extra que somam ao total (por defeito nenhum: só baús).</summary>
    public List<string> CountedExtras { get; set; } = new();
    public OverlaySettings Overlay { get; set; } = new();
    /// <summary>Código da língua (pt, en, es, fr, de). Nulo = língua do Windows.</summary>
    public string? Language { get; set; }

    public CountingOptions Counting() => new(CountedExtras.ToHashSet());
}

public sealed class OverlaySettings
{
    public bool Visible { get; set; }
    /// <summary>Bloqueada: não se mexe e os cliques passam para o jogo.</summary>
    public bool Locked { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }
    /// <summary>Opacidade em percentagem (40 a 100).</summary>
    public int Opacity { get; set; } = 92;
}

public sealed class AppData
{
    public AppSettings Settings { get; set; } = new();
    /// <summary>Todas as partidas analisadas, incluindo as de replays que o Fortnite já apagou.</summary>
    public List<MatchRecord> Matches { get; set; } = new();
}

/// <summary>
/// Histórico guardado em %AppData%\ContadorBaus\dados.json. O Fortnite só guarda um número limitado de
/// replays; o histórico mantém as partidas mesmo depois de os ficheiros desaparecerem.
/// </summary>
public sealed class HistoryStore
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly object _lock = new();
    private readonly string _path;
    private AppData _data;

    public HistoryStore(string? folder = null)
    {
        folder ??= DefaultFolder();
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "dados.json");
        _data = Load();
    }

    public string FilePath => _path;

    /// <summary>
    /// %AppData%\ContadorBaus. Não usa "ChestCounter": essa pasta pertence a outra app com o mesmo nome
    /// (xdoofy92/ChestCounter) e as duas não podem partilhar ficheiros.
    /// </summary>
    private static string DefaultFolder() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ContadorBaus");

    public bool IsFirstRun { get; private set; }

    private AppData Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                return JsonSerializer.Deserialize<AppData>(File.ReadAllText(_path), Json) ?? new AppData();
            }
        }
        catch
        {
            // Ficheiro corrompido: guarda uma cópia e começa de novo (os replays existentes são reanalisados).
            try { File.Copy(_path, _path + $".corrompido-{DateTime.Now:yyyyMMddHHmmss}", true); } catch { }
        }
        IsFirstRun = true;
        return new AppData();
    }

    public void Save()
    {
        lock (_lock)
        {
            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(_data, Json));
            File.Move(tmp, _path, true);
        }
    }

    public AppSettings Settings { get { lock (_lock) return _data.Settings; } }

    public void UpdateSettings(Action<AppSettings> change)
    {
        lock (_lock) change(_data.Settings);
        Save();
    }

    public List<MatchRecord> Snapshot()
    {
        lock (_lock) return _data.Matches.ToList();
    }

    public MatchRecord? Find(string replayFile)
    {
        lock (_lock) return _data.Matches.FirstOrDefault(m => string.Equals(m.ReplayFile, replayFile, StringComparison.OrdinalIgnoreCase));
    }

    public void Upsert(MatchRecord record)
    {
        lock (_lock)
        {
            var i = _data.Matches.FindIndex(m => string.Equals(m.ReplayFile, record.ReplayFile, StringComparison.OrdinalIgnoreCase));
            if (i >= 0) _data.Matches[i] = record;
            else _data.Matches.Add(record);
        }
    }
}
