namespace ContadorBaus;

/// <summary>Uma abertura (baú ou contentor extra) feita por um jogador numa partida.</summary>
public sealed class ChestOpen
{
    /// <summary>Segundos desde o início da gravação.</summary>
    public float TimeSeconds { get; set; }
    public string Kind { get; set; } = "";
    public string Blueprint { get; set; } = "";
    /// <summary>Partido em vez de aberto (o loot saltou na mesma).</summary>
    public bool Destroyed { get; set; }
}

/// <summary>Um colega de equipa e o que foi visto a abrir.</summary>
public sealed class TeammateRecord
{
    public string Name { get; set; } = "";
    public string? EpicId { get; set; }
    public int Chests { get; set; }
    /// <summary>Contentores extra por tipo.</summary>
    public Dictionary<string, int> Extras { get; set; } = new();

    /// <summary>Chave estável do colega entre partidas (Epic ID ou nome).</summary>
    public string Key => !string.IsNullOrEmpty(EpicId) ? EpicId! : Name.ToLowerInvariant();
}

/// <summary>Resultado da análise de um replay.</summary>
public sealed class MatchRecord
{
    public string ReplayFile { get; set; } = "";
    public long FileSize { get; set; }
    public DateTime FileWriteUtc { get; set; }
    public string? SessionId { get; set; }
    public DateTime StartUtc { get; set; }
    public int DurationSeconds { get; set; }
    public string? Playlist { get; set; }
    public string? PlayerName { get; set; }
    public string? EpicId { get; set; }
    public int? Placement { get; set; }
    public int? TotalPlayers { get; set; }
    public int? Eliminations { get; set; }

    /// <summary>Baús abertos pelo jogador.</summary>
    public List<ChestOpen> Chests { get; set; } = new();
    /// <summary>Contentores extra (munição, comida, canhões…) com o momento de cada um.</summary>
    public List<ChestOpen> Extras { get; set; } = new();
    /// <summary>Formato antigo (v1/v2): extras só por tipo, sem tempo. Mantido para partidas cujo replay já desapareceu.</summary>
    public Dictionary<string, int> OtherContainers { get; set; } = new();
    public List<TeammateRecord> Teammates { get; set; } = new();

    public int AnalyzerVersion { get; set; }
    /// <summary>A análise falhou (replay danificado, de jogo que crashou...).</summary>
    public string? Error { get; set; }
    /// <summary>Replay sem partida legível: não é mostrado.</summary>
    public bool Empty { get; set; }
    /// <summary>Replay de uma ilha criativa / UEFN (não é Battle Royale).</summary>
    public bool Creative { get; set; }

    public int ChestCount => Chests.Count;
    public DateTime StartLocal => DateTime.SpecifyKind(StartUtc, DateTimeKind.Utc).ToLocalTime();
    public bool IsValid => Error == null && !Empty;
    public bool IsWin => Placement == 1;

    /// <summary>Número de contentores extra de um tipo (formato novo ou antigo).</summary>
    public int ExtraCount(string kind) =>
        Extras.Count > 0 ? Extras.Count(e => e.Kind == kind)
            : OtherContainers.Where(kv => ChestKinds.Normalize(kv.Key) == kind).Sum(kv => kv.Value);
}

public static class ChestKinds
{
    // Baús (contam sempre)
    public const string Normal = "Baú";
    public const string Rare = "Baú raro";
    public const string Buried = "Baú enterrado";
    public const string Extraction = "Baú de relíquia";
    public const string PosterChest = "Baú de cartaz";
    public const string Other = "Outro baú";

    // Contentores extra (só contam se ativados nas Definições)
    public const string Cannon = "Canhão de saque";
    public const string AmmoBox = "Caixa de munição";
    public const string FoodBox = "Caixa de comida";
    public const string Poster = "Cartaz";
    public const string OtherContainer = "Outro contentor";

    /// <summary>Tipos extra pela ordem em que aparecem nas Definições.</summary>
    public static readonly IReadOnlyList<string> ExtraKinds = new[] { AmmoBox, Cannon, FoodBox, Poster, OtherContainer };

    private static readonly HashSet<string> ChestSet = new() { Normal, Rare, Buried, Extraction, PosterChest, Other };

    public static bool IsChest(string kind) => ChestSet.Contains(kind);

    /// <summary>Nomes do formato antigo (v1/v2) para os atuais.</summary>
    public static string Normalize(string kind) => kind == "Canhão de loot" ? Cannon : kind;

    public static string Classify(string blueprint)
    {
        var b = blueprint.ToLowerInvariant();
        if (b.Contains("cannon")) return Cannon;
        if (b.Contains("ammo")) return AmmoBox;
        if (b.Contains("foodbox") || b.Contains("food_box")) return FoodBox;
        if (b.Contains("rarechest") || b.Contains("legendary") || b.Contains("rare_chest")) return Rare;
        if (b.Contains("buried")) return Buried;
        if (b.Contains("spriteposter") && !b.Contains("chest")) return Poster;
        if (b.Contains("spriteposter") || b.Contains("booty")) return PosterChest;
        if (b.Contains("relic") || b.Contains("extraction")) return Extraction;
        if (b.Contains("tiered_chest") || b.Contains("normalchest") || b.Contains("standardchest")) return Normal;
        // Geleiras, barris, etc. não são baús.
        return b.Contains("chest") ? Other : OtherContainer;
    }
}

public static class Modes
{
    /// <summary>Nome legível do modo de jogo a partir da playlist do replay, na língua atual.</summary>
    public static string Friendly(string? playlist)
    {
        if (string.IsNullOrWhiteSpace(playlist)) return Lang.T("mode.unknown");
        var p = playlist.ToLowerInvariant();
        var size = p.Contains("solo") ? Lang.T("mode.solo")
            : p.Contains("duo") ? Lang.T("mode.duos")
            : p.Contains("trio") ? Lang.T("mode.trios")
            : p.Contains("squad") || p.Contains("quad") ? Lang.T("mode.squads")
            : null;
        string kind;
        if (p.Contains("creative") || p.Contains("playground") || p.Contains("_vk_")) kind = Lang.T("mode.creative");
        else if (p.Contains("blastberry") || p.Contains("reload")) kind = "Reload";
        else if (p.Contains("habanero") || p.Contains("ranked")) kind = p.Contains("nobuild") || p.Contains("_nb") ? "Ranked " + Lang.T("mode.zerobuild") : "Ranked";
        else if (p.Contains("nobuild") || p.Contains("_nb")) kind = Lang.T("mode.zerobuild");
        else if (p.Contains("respawn") || p.Contains("teamrumble")) kind = "Team Rumble";
        else if (p.Contains("default") || p.Contains("battleroyale") || p.Contains("playlist_")) kind = "Battle Royale";
        else kind = playlist.Replace("Playlist_", "");
        return size != null ? $"{kind} · {size}" : kind;
    }
}
