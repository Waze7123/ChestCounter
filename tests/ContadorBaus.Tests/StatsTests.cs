using ContadorBaus;

[Collection("Lang")]
public class StatsTests
{
    public StatsTests() => Lang.Current = Language.Pt;

    private static ChestOpen Open(string kind, float t = 10) => new() { Kind = kind, TimeSeconds = t };

    private static MatchRecord Match(DateTime localStart, string playlist = "Playlist_DefaultSquad", int chests = 0,
        int ammo = 0, int cannons = 0, int? placement = null, params TeammateRecord[] mates) => new()
    {
        ReplayFile = Guid.NewGuid() + ".replay",
        StartUtc = localStart.ToUniversalTime(),
        Playlist = playlist,
        Placement = placement,
        Chests = Enumerable.Range(0, chests).Select(i => Open(ChestKinds.Normal, i)).ToList(),
        Extras = Enumerable.Range(0, ammo).Select(_ => Open(ChestKinds.AmmoBox))
            .Concat(Enumerable.Range(0, cannons).Select(_ => Open(ChestKinds.Cannon))).ToList(),
        Teammates = mates.ToList(),
    };

    private static CountingOptions With(params string[] kinds) => new(kinds.ToHashSet());

    [Fact]
    public void ChestsOnly_IgnoresExtras()
    {
        var m = Match(DateTime.Now, chests: 5, ammo: 3, cannons: 2);
        Assert.Equal(5, Stats.Count(m, CountingOptions.ChestsOnly));
    }

    [Fact]
    public void EnabledExtras_AreAdded()
    {
        var m = Match(DateTime.Now, chests: 5, ammo: 3, cannons: 2);
        Assert.Equal(8, Stats.Count(m, With(ChestKinds.AmmoBox)));
        Assert.Equal(10, Stats.Count(m, With(ChestKinds.AmmoBox, ChestKinds.Cannon)));
    }

    [Fact]
    public void LegacyRecords_UseOtherContainersAndOldCannonName()
    {
        var m = Match(DateTime.Now, chests: 4);
        m.OtherContainers = new() { ["Canhão de loot"] = 2, [ChestKinds.AmmoBox] = 1 };
        Assert.Equal(7, Stats.Count(m, With(ChestKinds.Cannon, ChestKinds.AmmoBox)));
    }

    [Fact]
    public void TeamCount_IsYouPlusTeammates()
    {
        var mate = new TeammateRecord { Name = "Ana", Chests = 6, Extras = new() { [ChestKinds.AmmoBox] = 2 } };
        var m = Match(DateTime.Now, chests: 4, ammo: 1, mates: mate);
        Assert.Equal(10, Stats.TeamCount(m, CountingOptions.ChestsOnly));
        Assert.Equal(13, Stats.TeamCount(m, With(ChestKinds.AmmoBox)));
    }

    [Fact]
    public void InPeriod_IncludesStartDayAndSkipsInvalid()
    {
        var day = new DateTime(2026, 11, 10);
        var before = Match(day.AddHours(-1), chests: 1);
        var onStart = Match(day.AddHours(0.5), chests: 2);
        var later = Match(day.AddDays(2), chests: 3);
        var broken = Match(day.AddDays(1)); broken.Error = "x";
        var empty = Match(day.AddDays(1)); empty.Empty = true;

        Assert.Equal(new[] { later, onStart }, Stats.InPeriod(new[] { before, onStart, later, broken, empty }, day));
        Assert.Equal(3, Stats.InPeriod(new[] { before, onStart, later, broken }, null).Count);
    }

    [Fact]
    public void ByMode_GroupsAndSortsByChests()
    {
        var rows = Stats.ByMode(new[]
        {
            Match(DateTime.Now, "Playlist_DefaultSolo", chests: 3, placement: 1),
            Match(DateTime.Now, "Playlist_DefaultSquad", chests: 10),
            Match(DateTime.Now, "Playlist_DefaultSquad", chests: 4, placement: 1),
        }, CountingOptions.ChestsOnly);

        Assert.Equal("Battle Royale · Esquadrões", rows[0].Mode);
        Assert.Equal(2, rows[0].Matches);
        Assert.Equal(14, rows[0].Chests);
        Assert.Equal(7, rows[0].Average);
        Assert.Equal(1, rows[1].Wins);
    }

    [Fact]
    public void ByTeammate_MergesByEpicIdAndKeepsLatestName()
    {
        var old = Match(new DateTime(2026, 11, 1), chests: 1, placement: 1, mates: new TeammateRecord { Name = "NomeVelho", EpicId = "abc", Chests = 5 });
        var recent = Match(new DateTime(2026, 11, 5), chests: 1, mates: new TeammateRecord { Name = "NomeNovo", EpicId = "abc", Chests = 7 });

        var row = Assert.Single(Stats.ByTeammate(new[] { recent, old }, CountingOptions.ChestsOnly));
        Assert.Equal("NomeNovo", row.Name);
        Assert.Equal(2, row.MatchesTogether);
        Assert.Equal(12, row.Chests);
        Assert.Equal(1, row.WinsTogether);
    }

    [Fact]
    public void ByDay_FillsGapsWithZero()
    {
        var d = new DateTime(2026, 11, 10);
        var days = Stats.ByDay(new[] { Match(d.AddHours(12), chests: 4), Match(d.AddDays(2).AddHours(12), chests: 6) },
            CountingOptions.ChestsOnly, d, d.AddDays(2));
        Assert.Equal(new[] { 4, 0, 6 }, days.Select(x => x.Chests));
    }

    [Fact]
    public void CountedOpens_MergesEnabledExtrasInTimeOrder()
    {
        var m = Match(DateTime.Now);
        m.Chests = new() { Open(ChestKinds.Normal, 30) };
        m.Extras = new() { Open(ChestKinds.AmmoBox, 10), Open(ChestKinds.FoodBox, 20) };
        Assert.Equal(new[] { 10f, 30f }, Stats.CountedOpens(m, With(ChestKinds.AmmoBox)).Select(o => o.TimeSeconds));
    }
}

[Collection("Lang")]
public class ClassificationTests
{
    public ClassificationTests() => Lang.Current = Language.Pt;

    [Theory]
    [InlineData("Tiered_Chest_Athena_C", ChestKinds.Normal)]
    [InlineData("AlwaysSpawn_RareChest_C", ChestKinds.Rare)]
    [InlineData("B_Athena_BuriedChest_Chest_C", ChestKinds.Buried)]
    [InlineData("B_TieredChest_Relics_C", ChestKinds.Extraction)]
    [InlineData("B_SpritePosters_Booty_Chest_C", ChestKinds.PosterChest)]
    [InlineData("Tiered_Ammo_Athena_C", ChestKinds.AmmoBox)]
    [InlineData("BGA_LootCannon_C", ChestKinds.Cannon)]
    [InlineData("B_FoodBox_Produce_C", ChestKinds.FoodBox)]
    [InlineData("Cooler_Container_C", ChestKinds.OtherContainer)]
    public void Classify(string blueprint, string expected) => Assert.Equal(expected, ChestKinds.Classify(blueprint));

    [Theory]
    [InlineData("Playlist_DefaultSquad", "Battle Royale · Esquadrões")]
    [InlineData("Playlist_DefaultSolo", "Battle Royale · Solo")]
    [InlineData("playlist_trios", "Battle Royale · Trios")]
    [InlineData("Playlist_NoBuildBR_Duo", "Zero Construção · Duos")]
    [InlineData(null, "Desconhecido")]
    public void FriendlyMode(string? playlist, string expected) => Assert.Equal(expected, Modes.Friendly(playlist));
}

[Collection("Lang")]
public class LangTests
{
    [Fact]
    public void EveryKeyHasAllFiveLanguages() => Assert.Empty(Lang.MissingTranslations());

    [Theory]
    [InlineData(Language.Pt, "Contador de Baús", "Battle Royale · Esquadrões")]
    [InlineData(Language.En, "Chest Counter", "Battle Royale · Squads")]
    [InlineData(Language.Es, "Contador de Cofres", "Battle Royale · Escuadrones")]
    [InlineData(Language.Fr, "Compteur de Coffres", "Battle Royale · Sections")]
    [InlineData(Language.De, "Truhenzähler", "Battle Royale · Squads")]
    public void NameAndModesFollowLanguage(Language l, string name, string squads)
    {
        Lang.Current = l;
        Assert.Equal(name, Lang.AppName);
        Assert.Equal(squads, Modes.Friendly("Playlist_DefaultSquad"));
        Lang.Current = Language.Pt;
    }

    [Fact]
    public void KindKeysStayStableAcrossLanguages()
    {
        Lang.Current = Language.De;
        Assert.Equal(ChestKinds.AmmoBox, ChestKinds.Classify("Tiered_Ammo_Athena_C"));
        Assert.Equal("Munitionskiste", Lang.Kind(ChestKinds.AmmoBox));
        Assert.Equal("2 Truhen", Lang.Plural(2, "u.chest"));
        Lang.Current = Language.Pt;
    }
}
