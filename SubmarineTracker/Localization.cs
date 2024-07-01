using System.IO;
using System.Reflection;
using Dalamud.Game;

namespace SubmarineTracker;

public class Localization
{
    public static readonly string[] ApplicableLangCodes = { "de", "ja", "fr", "zh" };

    private const string FallbackLangCode = "en";
    private const string LocResourceDirectory = "loc";

    private readonly Assembly Assembly;

    public Localization()
    {
        Assembly = Assembly.GetCallingAssembly();
    }

    public void ExportLocalizable() => Loc.ExportLocalizableForAssembly(Assembly);
    public void SetupWithFallbacks() => Loc.SetupWithFallbacks(Assembly);

    public void SetupWithLangCode(string langCode)
    {
        if (langCode.ToLower() == FallbackLangCode || !ApplicableLangCodes.Contains(langCode.ToLower()))
        {
            SetupWithFallbacks();
            return;
        }

        try
        {
            Loc.Setup(ReadLocData(langCode), Assembly);
        }
        catch (Exception)
        {
            Plugin.Log.Warning($"Could not load loc {langCode}. Setting up fallbacks.");
            SetupWithFallbacks();
        }
    }

    private string ReadLocData(string langCode)
    {
        return File.ReadAllText(Path.Combine(Plugin.PluginInterface.AssemblyLocation.DirectoryName!, LocResourceDirectory, $"{langCode}.json"));
    }

    public static ClientLanguage LangCodeToClientLanguage(string langCode)
    {
        return langCode switch
        {
            "en" => ClientLanguage.English,
            "de" => ClientLanguage.German,
            "fr" => ClientLanguage.French,
            "ja" => ClientLanguage.Japanese,
            _ => ClientLanguage.English
        };
    }
}
