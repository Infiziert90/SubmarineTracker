using SubmarineTracker.Data;
using static SubmarineTracker.Data.Submarines;

namespace SubmarineTracker;

//From: https://github.com/MidoriKami/DailyDuty/
[Serializable]
public class CharacterConfiguration
{
    // Increase with version bump
    public int Version { get; set; } = 3;

    public ulong LocalContentId;

    public string CharacterName = "";
    public string Tag = "";
    public string World = "Unknown";
    public List<Submarine> Submarines = new();
    public Dictionary<uint, Loot.SubmarineLoot> Loot = new();
    public List<Tuple<uint, bool, bool>> ExplorationPoints = new();

    public CharacterConfiguration() { }

    public CharacterConfiguration(ulong id, string characterName, string tag, string world, List<Submarine> subs, Dictionary<uint, Loot.SubmarineLoot> loot, List<Tuple<uint, bool, bool>> explorationPoints)
    {
        LocalContentId = id;
        CharacterName = characterName;
        Tag = tag;
        World = world;
        Submarines = subs;
        Loot = loot;
        ExplorationPoints = explorationPoints;
    }

    public CharacterConfiguration(ulong id, FcSubmarines playerFc)
    {
        LocalContentId = id;
        CharacterName = playerFc.CharacterName;
        Tag = playerFc.Tag;
        World = playerFc.World;
        Submarines = playerFc.Submarines;
        Loot = playerFc.SubLoot;
        ExplorationPoints = playerFc.UnlockedSectors
                                    .Select(t => new Tuple<uint, bool, bool>(t.Key, t.Value, playerFc.ExploredSectors[t.Key]))
                                    .ToList();;
    }

    public static CharacterConfiguration CreateNew() => new()
    {
        LocalContentId = Plugin.ClientState.LocalContentId,

        CharacterName = Plugin.ClientState.LocalPlayer?.Name.ToString() ?? "",
        Tag = Plugin.ClientState.LocalPlayer?.CompanyTag.ToString() ?? "",
        World = Plugin.ClientState.LocalPlayer?.HomeWorld.GameData?.Name.ToString() ?? "Unknown"
    };
}
