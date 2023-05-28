using Dalamud.Utility;
using ImGuiScene;
using Lumina.Data.Files;

namespace SubmarineTracker;

//From: https://github.com/Tischel/ActionTimeline
public class TexturesCache : IDisposable
{
    private Dictionary<uint, TextureWrap> _cache = new();

    public TextureWrap GetTextureFromIconId(uint iconId)
    {
        if (_cache.TryGetValue(iconId, out var texture))
        {
            return texture;
        }

        var iconFile = Plugin.Data.GetFile<TexFile>($"ui/icon/{iconId / 1000 * 1000:000000}/{iconId:000000}_hr1.tex")!;
        var newTexture = Plugin.PluginInterface.UiBuilder.LoadImageRaw(iconFile.GetRgbaImageData(), iconFile.Header.Width, iconFile.Header.Height, 4);
        _cache.Add(iconId, newTexture);

        return newTexture;
    }

    #region singleton
    public static void Initialize() { Instance = new TexturesCache(); }
    public static TexturesCache Instance { get; private set; } = null!;

    ~TexturesCache()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        foreach (TextureWrap tex in _cache.Keys.Select(key => _cache[key]))
        {
            tex?.Dispose();
        }

        _cache.Clear();
    }
    #endregion
}
