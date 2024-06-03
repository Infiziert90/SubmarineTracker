using Newtonsoft.Json;

namespace SubmarineTracker;

//From: https://github.com/MidoriKami/DailyDuty/
[Serializable]
public class CharacterConfiguration
{
    // TODO Remove after migration time
    // Increase with version bump
    public int Version { get; set; } = 3;

    public ulong LocalContentId;

    public string CharacterName = "";
    public string Tag = "";
    public string World = "Unknown";
    public List<Data.Submarines.Submarine> Submarines = new();
    public Dictionary<uint, Data.Loot.SubmarineLoot> Loot = new();
    public List<Tuple<uint, bool, bool>> ExplorationPoints = new();

    [JsonConstructor]
    public CharacterConfiguration() { }

    public static CharacterConfiguration CreateNew() => new()
    {
        LocalContentId = Plugin.ClientState.LocalContentId,

        CharacterName = Plugin.ClientState.LocalPlayer?.Name.ToString() ?? "",
        Tag = Plugin.ClientState.LocalPlayer?.CompanyTag.ToString() ?? "",
        World = Plugin.ClientState.LocalPlayer?.HomeWorld.GameData?.Name.ToString() ?? "Unknown"
    };
}
