namespace ContadorBaus;

/// <summary>O que entra no total: os baús, mais os tipos extra ativados nas Definições.</summary>
public sealed record CountingOptions(IReadOnlySet<string> CountedExtras)
{
    public static readonly CountingOptions ChestsOnly = new(new HashSet<string>());

    public bool Counts(string kind) => ChestKinds.IsChest(kind) || CountedExtras.Contains(ChestKinds.Normalize(kind));
}

public sealed record ModeStats(string Mode, int Matches, int Chests, int Wins)
{
    public double Average => Matches == 0 ? 0 : (double)Chests / Matches;
}

public sealed record TeammateStats(string Key, string Name, int MatchesTogether, int Chests, int WinsTogether, DateTime LastPlayedLocal)
{
    public double Average => MatchesTogether == 0 ? 0 : (double)Chests / MatchesTogether;
}

public sealed record DayStats(DateTime Day, int Chests, int Matches);

/// <summary>Cálculos sobre o histórico. Funções puras: a UI só formata o resultado.</summary>
public static class Stats
{
    /// <summary>Total de uma partida segundo as opções (baús + extras ativados).</summary>
    public static int Count(MatchRecord m, CountingOptions o) =>
        m.ChestCount + o.CountedExtras.Sum(m.ExtraCount);

    /// <summary>Total de um colega numa partida segundo as opções.</summary>
    public static int Count(TeammateRecord t, CountingOptions o) =>
        t.Chests + o.CountedExtras.Sum(k => t.Extras.GetValueOrDefault(k));

    /// <summary>Total da equipa numa partida: tu + colegas.</summary>
    public static int TeamCount(MatchRecord m, CountingOptions o) => Count(m, o) + m.Teammates.Sum(t => Count(t, o));

    /// <summary>Partidas válidas desde a data (local, inclusiva), da mais recente para a mais antiga.</summary>
    public static List<MatchRecord> InPeriod(IEnumerable<MatchRecord> all, DateTime? startDate) => all
        .Where(m => m.IsValid && (startDate == null || m.StartLocal.Date >= startDate.Value.Date))
        .OrderByDescending(m => m.StartUtc)
        .ToList();

    public static List<ModeStats> ByMode(IEnumerable<MatchRecord> matches, CountingOptions o) => matches
        .GroupBy(m => Modes.Friendly(m.Playlist))
        .Select(g => new ModeStats(g.Key, g.Count(), g.Sum(m => Count(m, o)), g.Count(m => m.IsWin)))
        .OrderByDescending(s => s.Chests)
        .ThenBy(s => s.Mode, StringComparer.CurrentCulture)
        .ToList();

    public static List<TeammateStats> ByTeammate(IEnumerable<MatchRecord> matches, CountingOptions o)
    {
        var rows = new Dictionary<string, (string Name, int Matches, int Chests, int Wins, DateTime Last)>();
        foreach (var m in matches)
        {
            foreach (var t in m.Teammates)
            {
                var r = rows.TryGetValue(t.Key, out var found) ? found : (Name: t.Name, Matches: 0, Chests: 0, Wins: 0, Last: DateTime.MinValue);
                var last = m.StartLocal > r.Last ? m.StartLocal : r.Last;
                // O nome mais recente ganha (os jogadores mudam de nome).
                var name = m.StartLocal >= r.Last ? t.Name : r.Name;
                rows[t.Key] = (name, r.Matches + 1, r.Chests + Count(t, o), r.Wins + (m.IsWin ? 1 : 0), last);
            }
        }
        return rows
            .Select(kv => new TeammateStats(kv.Key, kv.Value.Name, kv.Value.Matches, kv.Value.Chests, kv.Value.Wins, kv.Value.Last))
            .OrderByDescending(t => t.MatchesTogether)
            .ThenByDescending(t => t.Chests)
            .ToList();
    }

    /// <summary>Uma entrada por dia entre <paramref name="from"/> e <paramref name="to"/>, com zeros nos dias sem partidas.</summary>
    public static List<DayStats> ByDay(IEnumerable<MatchRecord> matches, CountingOptions o, DateTime from, DateTime to)
    {
        var map = matches.GroupBy(m => m.StartLocal.Date).ToDictionary(g => g.Key, g => (Chests: g.Sum(m => Count(m, o)), Matches: g.Count()));
        var days = new List<DayStats>();
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
        {
            var v = map.GetValueOrDefault(d);
            days.Add(new DayStats(d, v.Chests, v.Matches));
        }
        return days;
    }

    /// <summary>Todas as aberturas que contam numa partida, por ordem de tempo.</summary>
    public static List<ChestOpen> CountedOpens(MatchRecord m, CountingOptions o) => m.Chests
        .Concat(m.Extras.Where(e => o.Counts(e.Kind)))
        .OrderBy(c => c.TimeSeconds)
        .ToList();
}
