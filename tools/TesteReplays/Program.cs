using System.Diagnostics;
using ContadorBaus;

if (args.Length > 0 && args[0] == "--info") { foreach (var f in args.Skip(1)) { try { DebugInfo.Show(f); } catch (Exception e) { Console.WriteLine(f + " " + e.Message); } } return; }
// Analisa os replays de uma pasta e imprime os baús de cada partida (ferramenta de verificação).
var folder = args.Length > 0 && args[0] != "-" ? args[0] : ReplayFolder.Default;
var max = args.Length > 1 ? int.Parse(args[1]) : int.MaxValue;
var total = 0; var ok = 0; var fail = 0;
foreach (var file in Directory.GetFiles(folder, "*.replay").OrderBy(f => f).Take(max))
{
    var sw = Stopwatch.StartNew();
    try
    {
        var r = new ChestAnalyzer().Analyze(file);
        ok++; total += r.ChestCount;
        Console.WriteLine($"{r.StartLocal:yyyy-MM-dd HH:mm} {Modes.Friendly(r.Playlist),-28} #{r.Placement,-3} elim={r.Eliminations,-2} baús={r.ChestCount,-3} extras={string.Join(",", r.Extras.GroupBy(e => e.Kind).Select(g => $"{g.Key}:{g.Count()}"))} equipa={string.Join(",", r.Teammates.Select(t => $"{t.Name}:{t.Chests}"))} [{r.PlayerName}] {sw.ElapsedMilliseconds}ms {r.Playlist}");
        foreach (var c in r.Chests.GroupBy(c => c.Blueprint)) Console.WriteLine($"      {c.Count()}x {c.Key} -> {c.First().Kind}");
    }
    catch (Exception e)
    {
        fail++;
        Console.WriteLine($"FALHOU {Path.GetFileName(file)}: {e}");
    }
}
Console.WriteLine($"\nOK={ok} falhas={fail} total baús={total}");
