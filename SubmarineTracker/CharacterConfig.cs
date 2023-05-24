using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Dalamud.Logging;
using Newtonsoft.Json;
using SubmarineTracker.Data;

namespace SubmarineTracker;

[Serializable]
//From: https://github.com/MidoriKami/DailyDuty/
public class CharacterConfiguration
{
    public int Version { get; set; } = 0;

    public ulong LocalContentId;

    public string CharacterName = "";
    public string Tag = "";
    public string World = "Unknown";
    public List<Submarines.Submarine> Submarines = new();
    public Dictionary<uint, Submarines.SubmarineLoot> Loot = new();
    public List<Tuple<uint, bool, bool>> ExplorationPoints = new();

    public CharacterConfiguration() { }

    public CharacterConfiguration(ulong id, string characterName, string tag, string world, List<Submarines.Submarine> subs, Dictionary<uint, Submarines.SubmarineLoot> loot, List<Tuple<uint, bool, bool>> explorationPoints)
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
        Submarines = new List<Submarines.Submarine>(),
    };
}
