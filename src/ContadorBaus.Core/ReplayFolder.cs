namespace ContadorBaus;

public static class ReplayFolder
{
    /// <summary>Pasta onde o Fortnite guarda os replays.</summary>
    public static string Default => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortniteGame", "Saved", "Demos");
}
