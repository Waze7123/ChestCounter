using FortniteReplayReader.Models;
using FortniteReplayReader.Models.NetFieldExports;
using FortniteReplayReader.Models.NetFieldExports.Items;
using System.Text.RegularExpressions;
using Unreal.Core;
using Unreal.Core.Contracts;
using Unreal.Core.Models;
using Unreal.Core.Models.Enums;

namespace ContadorBaus;

/// <summary>
/// Lê um ficheiro .replay do Fortnite e devolve todos os baús abertos pelo jogador que gravou o replay.
/// Um analisador serve apenas para um replay (cria-se um novo por ficheiro).
/// </summary>
public sealed class ChestAnalyzer : FortniteReplayReader.ReplayReader
{
    /// <summary>Aumentar sempre que a análise mudar, para que os replays existentes sejam reanalisados.</summary>
    public const int Version = 3;

    private sealed record SearchEvent(float Time, string Label, uint? Searcher, bool Destroyed);

    private static readonly Regex UaidSuffix = new(@"_UAID_.*$", RegexOptions.Compiled);

    private readonly Dictionary<uint, string> _channelKey = new();
    private readonly Dictionary<uint, string> _channelLabel = new();
    private readonly Dictionary<uint, uint> _lastSearcher = new();
    private readonly HashSet<string> _searched = new();
    private readonly List<SearchEvent> _searches = new();
    private readonly Dictionary<uint, uint> _pawnToPlayerState = new();
    private readonly Dictionary<uint, int> _playerStateToPlayerId = new();

    static ChestAnalyzer()
    {
        // Blueprints de baús/contentores novos (cada temporada traz uns quantos) são lidos como contentor genérico.
        NetFieldParser.AliasResolver = path =>
        {
            if (path.Contains(':') || path.EndsWith("_ClassNetCache", StringComparison.Ordinal)) return null;
            var last = path[(path.LastIndexOf('/') + 1)..];
            return path.Contains("/Containers/", StringComparison.OrdinalIgnoreCase)
                   || last.Contains("Chest", StringComparison.OrdinalIgnoreCase)
                   || last.Contains("SpritePoster", StringComparison.OrdinalIgnoreCase)
                ? GenericContainer.GroupPath
                : null;
        };

        // Canhões de saque: cada blueprint expõe o RPC FireLootCannon no seu próprio ClassNetCache.
        NetFieldParser.ClassNetCacheAliasResolver = path =>
            path.Contains("LootCannon", StringComparison.OrdinalIgnoreCase) && path.EndsWith("_ClassNetCache", StringComparison.Ordinal)
                ? LootCannonCache.Path
                : null;
    }

    public ChestAnalyzer() : base(null, ParseMode.Normal) { }

    private static float Now => Unreal.Core.ReplayReader<FortniteReplay>.CurrentTime;

    private string PathOf(uint? guid) =>
        guid is null or 0 ? "" : _netGuidCache.TryGetPathName(guid.Value, out var path) ? path : "";

    private static string CleanName(string path)
    {
        var name = path[(path.LastIndexOf('/') + 1)..];
        var dot = name.LastIndexOf('.');
        if (dot >= 0) name = name[(dot + 1)..];
        return UaidSuffix.Replace(name, "");
    }

    protected override void OnChannelOpened(uint channelIndex, NetworkGUID? actor)
    {
        base.OnChannelOpened(channelIndex, actor);
        var channel = Channels[channelIndex];
        var archetype = PathOf(channel?.ArchetypeId);
        var actorPath = PathOf(channel?.ActorId);
        // Atores colocados no mapa não têm arquétipo; o caminho do ator é único por baú.
        _channelKey[channelIndex] = !string.IsNullOrEmpty(actorPath) ? actorPath : $"guid:{channel?.ActorId}";
        _channelLabel[channelIndex] = CleanName(!string.IsNullOrEmpty(archetype) ? archetype : actorPath);
        _lastSearcher.Remove(channelIndex);
    }

    protected override void OnExportRead(uint channelIndex, INetFieldExportGroup? exportGroup)
    {
        base.OnExportRead(channelIndex, exportGroup);

        switch (exportGroup)
        {
            case BaseContainer container:
            {
                if (container.SearchingPawn is uint searcher && searcher != 0)
                {
                    _lastSearcher[channelIndex] = searcher;
                }
                var key = _channelKey.GetValueOrDefault(channelIndex, $"ch:{channelIndex}");
                var label = _channelLabel.GetValueOrDefault(channelIndex, "?");
                if (container.bAlreadySearched)
                {
                    if (_searched.Add(key))
                    {
                        _searches.Add(new SearchEvent(Now, label, _lastSearcher.TryGetValue(channelIndex, out var s) ? s : null, false));
                    }
                }
                else if (container.bDestroyed && container.SearchingPawn is uint destroyer && destroyer != 0)
                {
                    // Partido em vez de aberto: o servidor põe em SearchingPawn quem o partiu. Conta uma vez.
                    if (_searched.Add(key))
                    {
                        _searches.Add(new SearchEvent(Now, label, destroyer, true));
                    }
                }
                break;
            }

            case LootCannonFired cannon when cannon.InteractingPawn is uint gunner && gunner != 0:
            {
                // Cada disparo conta uma vez (o mesmo canhão pode ser usado várias vezes).
                var cannonKey = _channelKey.GetValueOrDefault(channelIndex, $"ch:{channelIndex}") + $"#fire{Now:F0}";
                if (_searched.Add(cannonKey))
                {
                    _searches.Add(new SearchEvent(Now, _channelLabel.GetValueOrDefault(channelIndex, "LootCannon"), gunner, false));
                }
                break;
            }

            case PlayerPawn pawn when pawn.PlayerState is uint ps && ps != 0:
                if (Channels[channelIndex]?.ActorId is uint pawnId)
                {
                    _pawnToPlayerState[pawnId] = ps;
                }
                break;

            case FortPlayerState state:
                var psId = Channels[channelIndex]?.ActorId;
                var playerId = state.PlayerId is null ? state.PlayerID : (int?)state.PlayerId;
                if (psId != null && playerId != null)
                {
                    _playerStateToPlayerId[psId.Value] = playerId.Value;
                }
                break;
        }
    }

    /// <summary>Analisa o replay e devolve os baús abertos por quem o gravou.</summary>
    public MatchRecord Analyze(string file)
    {
        var fi = new FileInfo(file);
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var replay = ReadReplay(stream);

        var players = replay.PlayerData?.ToList() ?? new List<PlayerData>();
        var recorderPs = replay.GameData?.RecorderId;

        PlayerData? me = null;
        var myStates = new HashSet<uint>();
        if (recorderPs != null)
        {
            myStates.Add(recorderPs.Value);
            if (_playerStateToPlayerId.TryGetValue(recorderPs.Value, out var myId))
            {
                me = players.FirstOrDefault(p => p.Id == myId);
                foreach (var kv in _playerStateToPlayerId.Where(kv => kv.Value == myId)) myStates.Add(kv.Key);
            }
        }
        me ??= players.FirstOrDefault(p => p.IsReplayOwner);

        HashSet<uint> PawnsOf(IEnumerable<uint> states)
        {
            var set = states.ToHashSet();
            return _pawnToPlayerState.Where(kv => set.Contains(kv.Value)).Select(kv => kv.Key).ToHashSet();
        }
        List<ChestOpen> OpensBy(HashSet<uint> pawns) => _searches
            .Where(s => s.Searcher is uint p && pawns.Contains(p))
            .OrderBy(s => s.Time)
            .Select(s => new ChestOpen { TimeSeconds = MathF.Round(s.Time, 1), Kind = ChestKinds.Classify(s.Label), Blueprint = s.Label, Destroyed = s.Destroyed })
            .ToList();

        var mine = OpensBy(PawnsOf(myStates));

        // Colegas de equipa: o replay só vê o que acontece perto de quem grava; um colega longe fica sub-contado.
        var teammates = new List<TeammateRecord>();
        if (me?.TeamIndex is int team)
        {
            var mates = players
                .Where(p => p.TeamIndex == team && p.Id != me.Id && !p.IsBot && p.Id != null)
                .GroupBy(p => p.EpicId ?? p.PlayerName ?? $"pid{p.Id}")
                .Select(g => g.First());
            foreach (var mate in mates)
            {
                var states = _playerStateToPlayerId.Where(kv => kv.Value == mate.Id).Select(kv => kv.Key);
                var opens = OpensBy(PawnsOf(states));
                teammates.Add(new TeammateRecord
                {
                    Name = string.IsNullOrWhiteSpace(mate.PlayerName) ? "Colega" : mate.PlayerName.Trim(),
                    EpicId = mate.EpicId,
                    Chests = opens.Count(o => ChestKinds.IsChest(o.Kind)),
                    Extras = opens.Where(o => !ChestKinds.IsChest(o.Kind)).GroupBy(o => o.Kind).ToDictionary(g => g.Key, g => g.Count()),
                });
            }
            teammates.Sort((a, b) => b.Chests.CompareTo(a.Chests));
        }

        var info = replay.Info;
        var start = replay.GameData?.UtcTimeStartedMatch ?? info?.Timestamp ?? fi.CreationTimeUtc;
        if (start.Year < 2017) start = info?.Timestamp ?? fi.CreationTimeUtc;

        return new MatchRecord
        {
            ReplayFile = fi.Name,
            FileSize = fi.Length,
            FileWriteUtc = fi.LastWriteTimeUtc,
            SessionId = replay.GameData?.GameSessionId,
            StartUtc = DateTime.SpecifyKind(start, DateTimeKind.Utc),
            DurationSeconds = info != null ? (int)(info.LengthInMs / 1000) : 0,
            Playlist = replay.GameData?.CurrentPlaylist,
            PlayerName = me?.PlayerName?.Trim(),
            Placement = me?.Placement ?? (int?)replay.TeamStats?.Position,
            TotalPlayers = (int?)replay.TeamStats?.TotalPlayers,
            Eliminations = (int?)replay.Stats?.Eliminations ?? (int?)me?.Kills,
            EpicId = me?.EpicId,
            Chests = mine.Where(o => ChestKinds.IsChest(o.Kind)).ToList(),
            Extras = mine.Where(o => !ChestKinds.IsChest(o.Kind)).ToList(),
            Teammates = teammates,
            AnalyzerVersion = Version,
            Empty = me == null && string.IsNullOrEmpty(replay.GameData?.CurrentPlaylist),
            Creative = replay.Header?.LevelNamesAndTimes?.Any(l => l.Item1.Contains("/VKPlay/", StringComparison.OrdinalIgnoreCase)) == true,
        };
    }
}
