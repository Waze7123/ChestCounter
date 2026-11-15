using FortniteReplayReader;
using Unreal.Core.Models.Enums;

static class DebugInfo
{
    public static void Show(string file)
    {
        using var s = File.OpenRead(file);
        var r = new ReplayReader(null, ParseMode.Minimal).ReadReplay(s);
        Console.WriteLine($"{Path.GetFileName(file)} live={r.Info?.IsLive} encrypted={r.Info?.IsEncrypted} keyLen={r.Info?.EncryptionKey.Length} keyZero={r.Info?.EncryptionKey.ToArray().All(b => b == 0)} lenMs={r.Info?.LengthInMs} players={r.PlayerData?.Count()} playlist={r.GameData?.CurrentPlaylist} events={r.Eliminations?.Count} levels={string.Join(";", (r.Header?.LevelNamesAndTimes ?? []).Select(x => x.Item1))} game={string.Join(";", r.Header?.GameSpecificData ?? [])}");
    }
}
