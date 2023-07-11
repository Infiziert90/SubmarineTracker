using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using Newtonsoft.Json;
using SubmarineTracker.Data;

namespace SubmarineTracker;

// Based on: https://github.com/Penumbra-Sync/client/blob/main/MareSynchronos/MareConfiguration/ConfigurationServiceBase.cs
public class ConfigurationBase : IDisposable
{
    private readonly Plugin Plugin;
    private readonly CancellationTokenSource CancellationToken = new();
    private readonly Dictionary<ulong, DateTime> LastWriteTimes = new();

    public string ConfigurationDirectory { get; init; }

    public ConfigurationBase(Plugin plugin)
    {
        Plugin = plugin;
        ConfigurationDirectory = Plugin.PluginInterface.ConfigDirectory.FullName;

        Task.Run(CheckForConfigChanges, CancellationToken.Token);
    }

    public void Dispose()
    {
        CancellationToken.Cancel();
        CancellationToken.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Load()
    {
        foreach (var file in Plugin.PluginInterface.ConfigDirectory.EnumerateFiles())
            if (ulong.TryParse(Path.GetFileNameWithoutExtension(file.Name), out var id))
                Submarines.KnownSubmarines[id] = new Submarines.FcSubmarines(LoadConfig(id));
    }

    private static string LoadFile(FileSystemInfo fileInfo)
    {
        for (var i = 0; i < 5; i++)
        {
            try
            {
                using var reader = new StreamReader(fileInfo.FullName);
                return reader.ReadToEnd();
            }
            catch
            {
                // Try to read until counter runs out
                var content = $"Config file read failed {i + 1}/5";
                Plugin.PluginInterface.UiBuilder.AddNotification(content, "Failed Read", NotificationType.Warning);
                PluginLog.Warning(content);
            }
        }

        return string.Empty;
    }

    public CharacterConfiguration LoadConfig(ulong contentId)
    {
        CharacterConfiguration? config;
        try
        {
            var file = new FileInfo(Path.Combine(ConfigurationDirectory, $"{contentId}.json"));
            config = JsonConvert.DeserializeObject<CharacterConfiguration>(LoadFile(file));
        }
        catch (Exception e)
        {
            PluginLog.Warning(e, $"Exception Occured during loading Character {contentId}. Loading new default config instead.");
            config = CharacterConfiguration.CreateNew();
        }

        config ??= CharacterConfiguration.CreateNew();

        LastWriteTimes[contentId] = GetConfigLastWriteTime(contentId);
        return config;
    }

    public void SaveCharacterConfig()
    {
        // Only allow saving of current character
        var contentId = Plugin.ClientState.LocalContentId;
        if (contentId == 0)
            return;

        if (!Submarines.KnownSubmarines.TryGetValue(contentId, out var savedConfig))
            return;

        var miscFolder = Path.Combine(ConfigurationDirectory, "Misc");
        Directory.CreateDirectory(miscFolder);

        var filePath = Path.Combine(ConfigurationDirectory, $"{contentId}.json");
        var existingConfigs = Directory.EnumerateFiles(miscFolder, $"{contentId}.json.bak.*")
                                       .Select(c => new FileInfo(c)).OrderByDescending(c => c.LastWriteTime).ToList();
        if (existingConfigs.Skip(5).Any())
            foreach (var file in existingConfigs.Skip(5).ToList())
                file.Delete();

        try
        {
            File.Copy(filePath, $"{Path.Combine(miscFolder, $"{contentId}.json")}.bak.{DateTime.Now:yyyyMMddHH}", overwrite: true);
        }
        catch
        {
            // ignore if file backup couldn't be created once
        }

        Task.Run(() => SaveAndTryMoveConfig(contentId, miscFolder, filePath, new CharacterConfiguration(contentId, savedConfig)));
    }

    public async Task SaveAndTryMoveConfig(ulong contentId, string miscFolder, string filePath, CharacterConfiguration savedConfig)
    {
        try
        {
            var tmpPath = $"{Path.Combine(miscFolder, $"{contentId}.json.tmp")}";
            if (File.Exists(tmpPath))
                File.Delete(tmpPath);

            File.WriteAllText(tmpPath, JsonConvert.SerializeObject(savedConfig, Formatting.Indented));

            for (var i = 0; i < 5; i++)
            {
                try
                {
                    File.Move(tmpPath, filePath, true);
                    LastWriteTimes[contentId] = new FileInfo(filePath).LastWriteTimeUtc;
                    return;
                }
                catch
                {
                    // Just try again until counter runs out
                    var content = $"Config file couldn't be moved {i + 1}/5";
                    Plugin.PluginInterface.UiBuilder.AddNotification(content, "Failed Move", NotificationType.Warning);
                    PluginLog.Warning(content);
                    await Task.Delay(50, CancellationToken.Token);
                }
            }
        }
        catch (Exception e)
        {
            PluginLog.Error(e.Message);
            PluginLog.Error(e.StackTrace!);
        }
    }

    public void DeleteCharacter(ulong id)
    {
        if (!Submarines.KnownSubmarines.ContainsKey(id))
            return;

        try
        {
            LastWriteTimes.Remove(id);
            Submarines.KnownSubmarines.Remove(id);
            var file = new FileInfo(Path.Combine(ConfigurationDirectory, $"{id}.json"));
            if (file.Exists)
                file.Delete();
        }
        catch (Exception e)
        {
            PluginLog.Error("Error while deleting character save file.");
            PluginLog.Error(e.Message);
        }
    }

    private async Task CheckForConfigChanges()
    {
        while (!CancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.Token);

            foreach (var file in Plugin.PluginInterface.ConfigDirectory.EnumerateFiles())
            {
                if (ulong.TryParse(Path.GetFileNameWithoutExtension(file.Name), out var id))
                {
                    // No need to override current character as we already have up to date config
                    if (id == Plugin.ClientState.LocalContentId)
                        continue;

                    var lastWriteTime = GetConfigLastWriteTime(id);
                    if (lastWriteTime != LastWriteTimes.GetOrCreate(id))
                    {
                        LastWriteTimes[id] = lastWriteTime;
                        Submarines.KnownSubmarines[id] = new Submarines.FcSubmarines(LoadConfig(id));
                    }
                }
            }
        }
    }

    private DateTime GetConfigLastWriteTime(ulong contentId) => new FileInfo(Path.Combine(ConfigurationDirectory, $"{contentId}.json")).LastWriteTimeUtc;
}
