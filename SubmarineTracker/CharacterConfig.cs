using System.Diagnostics.CodeAnalysis;
using System.IO;
using Dalamud.Logging;
using Newtonsoft.Json;
using SubmarineTracker.Data;
using static SubmarineTracker.Data.Submarines;

namespace SubmarineTracker;

[Serializable]
//From: https://github.com/MidoriKami/DailyDuty/
public class CharacterConfiguration
{
    // Increase with version bump
    public int Version { get; set; } = 2;

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

    public void Save(bool saveBackup = false)
    {
        if (LocalContentId != 0)
        {
            // if (saveBackup)
            // {
            //     var org = GetConfigFileInfo(LocalContentId);
            //     var backup = GetBackupConfigFileInfo(LocalContentId);
            //     if (!backup.Exists)
            //         backup.Delete();
            //
            //     org.CopyTo(backup.FullName);
            // }


            SaveConfigFile(GetConfigFileInfo(LocalContentId));
        }
    }

    public void SaveBackup() => Save(true);
    private static FileInfo GetBackupConfigFileInfo(ulong contentID) => new(Plugin.PluginInterface.ConfigDirectory.FullName + $@"\{contentID}.bak.json");

    public static CharacterConfiguration Load(ulong contentId)
    {
        try
        {
            var mainConfigFileInfo = GetConfigFileInfo(contentId);

            return TryLoadConfiguration(mainConfigFileInfo);
        }
        catch (Exception e)
        {
            PluginLog.Warning(e, $"Exception Occured during loading Character {contentId}. Loading new default config instead.");
            return CreateNewCharacterConfiguration();
        }
    }

    private static CharacterConfiguration TryLoadConfiguration(FileSystemInfo? mainConfigInfo = null)
    {
        try
        {
            if (TryLoadSpecificConfiguration(mainConfigInfo, out var mainCharacterConfiguration))
                return mainCharacterConfiguration;
        }
        catch (Exception e)
        {
            PluginLog.Warning(e, "Exception Occured loading Main Configuration");
        }

        return CreateNewCharacterConfiguration();
    }

    private static bool TryLoadSpecificConfiguration(FileSystemInfo? fileInfo, [NotNullWhen(true)] out CharacterConfiguration? info)
    {
        if (fileInfo is null || !fileInfo.Exists)
        {
            info = null;
            return false;
        }

        info = JsonConvert.DeserializeObject<CharacterConfiguration>(LoadFile(fileInfo));
        return info is not null;
    }

    private static FileInfo GetConfigFileInfo(ulong contentId) => new(Plugin.PluginInterface.ConfigDirectory.FullName + @$"\{contentId}.json");

    private static string LoadFile(FileSystemInfo fileInfo)
    {
        using var reader = new StreamReader(fileInfo.FullName);
        return reader.ReadToEnd();
    }

    private static void SaveFile(FileSystemInfo file, string fileText)
    {
        using var writer = new StreamWriter(file.FullName);
        writer.Write(fileText);
    }

    private void SaveConfigFile(FileSystemInfo file)
    {
        var text = JsonConvert.SerializeObject(this, Formatting.Indented);
        SaveFile(file, text);
    }

    private static CharacterConfiguration CreateNewCharacterConfiguration() => new()
    {
        LocalContentId = Plugin.ClientState.LocalContentId,

        CharacterName = Plugin.ClientState.LocalPlayer?.Name.ToString() ?? "",
        Tag = Plugin.ClientState.LocalPlayer?.CompanyTag.ToString() ?? "",
        World = Plugin.ClientState.LocalPlayer?.HomeWorld.GameData?.Name.ToString() ?? "Unknown",
        Submarines = new List<Submarine>(),
    };

    #region Character Handler
    public static void LoadCharacters()
    {
        foreach (var file in Plugin.PluginInterface.ConfigDirectory.EnumerateFiles())
        {
            ulong id;
            try
            {
                id = Convert.ToUInt64(Path.GetFileNameWithoutExtension(file.Name));
            }
            catch (Exception e)
            {
                PluginLog.Error($"Found file that isn't convertable. Filename: {file.Name}");
                PluginLog.Error(e.Message);
                continue;
            }

            var config = CharacterConfiguration.Load(id);

            // Migrate version 1 to version 2
            if (config.Version == 1)
            {
                foreach (var (key, subLoot) in config.Loot)
                {
                    var sub = new Build.SubmarineBuild(config.Submarines.Find(s => s.Register == key)!);
                    foreach (var (keyLoot, valueLoot) in subLoot.Loot)
                    {
                        foreach (var detailedLoot in valueLoot)
                        {
                            detailedLoot.Sector = detailedLoot.Point;

                            detailedLoot.Rank = (int) sub.Bonus.RowId;
                            detailedLoot.Surv = sub.Surveillance;
                            detailedLoot.Ret = sub.Retrieval;
                            detailedLoot.Fav = sub.Favor;
                        }
                    }
                }

                foreach (var key in new List<uint>(config.Loot.Keys))
                {
                    if (config.Loot[key].Loot.Count == 0)
                        continue;

                    var loot = config.Loot[key].Loot.Last();
                    config.Loot[key].Loot.Remove(loot.Key);
                    config.Loot[key].Loot.Add(loot.Key - 42, loot.Value);

                    var newLoot = new List<Loot.DetailedLoot>();
                    var sub = config.Submarines.Find(s => s.Register == key)!;
                    foreach (var _ in sub.Points)
                        newLoot.Add(new Loot.DetailedLoot(sub.Build));

                    config.Loot[key].Loot.Add(loot.Key, newLoot);
                }

                config.Version = 2;
                config.Save();
            }

            KnownSubmarines.TryAdd(id, FcSubmarines.Empty);
            var playerFc = KnownSubmarines[id];

            if (SubmarinesEqual(playerFc.Submarines, config.Submarines))
                continue;

            KnownSubmarines[id] = new FcSubmarines(config.CharacterName, config.Tag, config.World, config.Submarines, config.Loot, config.ExplorationPoints);
        }
    }

    public static void SaveCharacter()
    {
        var id = Plugin.ClientState.LocalContentId;
        if (!KnownSubmarines.TryGetValue(id, out var playerFc))
            return;

        var points = playerFc.UnlockedSectors.Select(t => new Tuple<uint, bool, bool>(t.Key, t.Value, playerFc.ExploredSectors[t.Key])).ToList();

        var config = new CharacterConfiguration(id, playerFc.CharacterName, playerFc.Tag, playerFc.World, playerFc.Submarines, playerFc.SubLoot, points);
        config.Save();
    }

    public static void DeleteCharacter(ulong id)
    {
        if (!KnownSubmarines.ContainsKey(id))
            return;

        KnownSubmarines.Remove(id);
        var file = Plugin.PluginInterface.ConfigDirectory.EnumerateFiles().FirstOrDefault(f => f.Name == $"{id}.json");
        if (file == null)
            return;

        try
        {
            file.Delete();
        }
        catch (Exception e)
        {
            PluginLog.Error("Error while deleting character save file.");
            PluginLog.Error(e.Message);
        }
    }
    #endregion
}
