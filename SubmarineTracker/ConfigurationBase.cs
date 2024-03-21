using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Internal.Notifications;
using Newtonsoft.Json;
using SubmarineTracker.Data;

namespace SubmarineTracker;

// Based on: https://github.com/Penumbra-Sync/client/blob/main/MareSynchronos/MareConfiguration/ConfigurationServiceBase.cs
public class ConfigurationBase : IDisposable
{
    private record SaveObject(ulong ContentId, string FilePath, CharacterConfiguration Character);

    private readonly CancellationTokenSource CancellationToken = new();
    private readonly ConcurrentDictionary<ulong, DateTime> LastWriteTimes = new();
    private readonly ConcurrentQueue<SaveObject> SaveQueue = new();


    public string ConfigurationDirectory { get; init; }
    private string MiscFolder { get; init; }

    public ConfigurationBase()
    {
        ConfigurationDirectory = Plugin.PluginInterface.ConfigDirectory.FullName;
        MiscFolder = Path.Combine(ConfigurationDirectory, "Misc");
        Directory.CreateDirectory(MiscFolder);

        Task.Run(CheckForConfigChanges, CancellationToken.Token);
        Task.Run(SaveAndTryMoveConfig, CancellationToken.Token);
    }

    public void Dispose()
    {
        CancellationToken.Cancel();
        CancellationToken.Dispose();
        GC.SuppressFinalize(this);
    }

    private string LoadFile(FileSystemInfo fileInfo)
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
                if (i == 4)
                    Plugin.Notification.AddNotification(new Notification
                    {
                        Content = Loc.Localize("Warnings - Config Fail", "Failed to read config"),
                        Type = NotificationType.Warning,
                        Minimized = false,
                    });

                Plugin.Log.Warning($"Config file read failed {i + 1}/5");
            }
        }

        return string.Empty;
    }

    public void Load()
    {
        foreach (var file in Plugin.PluginInterface.ConfigDirectory.EnumerateFiles())
            if (ulong.TryParse(Path.GetFileNameWithoutExtension(file.Name), out var id))
                Submarines.KnownSubmarines[id] = new Submarines.FcSubmarines(LoadConfig(id)) { Refresh = true };
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
            Plugin.Log.Warning(e, $"Exception Occured during loading Character {contentId}. Loading new default config instead.");
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

        if (!Submarines.KnownSubmarines.TryGetValue(contentId, out var fc))
            return;

        Save(contentId, new CharacterConfiguration(contentId, fc));
    }

    public void SaveAll()
    {
        // This saves all characters, only allow calls if only 1 process is running
        if (Process.GetProcessesByName("ffxiv_dx11").Length > 1)
            return;

        foreach (var (contentId, fc) in Submarines.KnownSubmarines)
            Save(contentId, new CharacterConfiguration(contentId, fc));
    }

    private void Save(ulong contentId, CharacterConfiguration savedConfig)
    {
        var filePath = Path.Combine(ConfigurationDirectory, $"{contentId}.json");
        try
        {
            var existingConfigs = Directory.EnumerateFiles(MiscFolder, $"{contentId}.json.bak.*")
                                           .Select(c => new FileInfo(c)).OrderByDescending(c => c.LastWriteTime);
            foreach (var file in existingConfigs.Skip(5))
                file.Delete();

            File.Copy(filePath, $"{Path.Combine(MiscFolder, $"{contentId}.json")}.bak.{DateTime.Now:yyyyMMddHH}", overwrite: true);
        }
        catch
        {
            // ignore if file backup couldn't be created once
        }

        SaveQueue.Enqueue(new SaveObject(contentId, filePath, savedConfig));
    }

    public void DeleteCharacter(ulong id)
    {
        if (!Submarines.KnownSubmarines.ContainsKey(id))
            return;

        try
        {
            LastWriteTimes.TryRemove(id, out _);
            Submarines.KnownSubmarines.Remove(id, out _);
            var file = new FileInfo(Path.Combine(ConfigurationDirectory, $"{id}.json"));
            if (file.Exists)
                file.Delete();
        }
        catch (Exception e)
        {
            Plugin.Log.Error("Error while deleting character save file.");
            Plugin.Log.Error(e.Message);
        }
    }

    private async Task SaveAndTryMoveConfig()
    {
        while (!CancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), CancellationToken.Token);

            if (!SaveQueue.TryDequeue(out var queueObject))
                continue;

            try
            {
                var tmpPath = $"{Path.Combine(MiscFolder, $"{queueObject.ContentId}.json.tmp")}";
                if (File.Exists(tmpPath))
                    File.Delete(tmpPath);

                File.WriteAllText(tmpPath, JsonConvert.SerializeObject(queueObject.Character, Formatting.Indented));
                for (var i = 0; i < 5; i++)
                {
                    try
                    {
                        File.Move(tmpPath, queueObject.FilePath, true);
                        LastWriteTimes[queueObject.ContentId] = new FileInfo(queueObject.FilePath).LastWriteTimeUtc;
                        break;
                    }
                    catch
                    {
                        // Just try again until counter runs out
                        if (i == 4)
                            Plugin.Notification.AddNotification(new Notification
                            {
                                Content = Loc.Localize("Warnings - Move Config", "Failed to move config"),
                                Type = NotificationType.Warning,
                                Minimized = false,
                            });

                        Plugin.Log.Warning($"Config file couldn't be moved {i + 1}/5");
                        await Task.Delay(30, CancellationToken.Token);
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.Error(e.Message);
                Plugin.Log.Error(e.StackTrace ?? "Null Stacktrace");
            }
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
