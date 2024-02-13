using static SubmarineTracker.Data.Submarines;

namespace SubmarineTracker.Data;

public enum NameOptions
{
    Default = 0,
    FullName = 1,
    OnlyTag = 2,
    OnlyName = 3,
    Initials = 4,
    OnlyInitials = 5,
    Anon = 6,
}

public class NameConverter
{
    private readonly Plugin Plugin;

    public NameConverter(Plugin plugin)
    {
        Plugin = plugin;
    }

    public string GetName(FcSubmarines fc)
    {
        return GenerateName(fc);
    }

    public string GetSub(Submarine sub, FcSubmarines fc)
    {
        var name = $"{sub.Name} ({GenerateName(fc)})";
        return Plugin.Configuration.NameOption != NameOptions.Anon ? name : $"{Utils.GenerateHashedName(name)}@{fc.World}";
    }

    public string GetJustSub(Submarine sub)
    {
        return Plugin.Configuration.NameOption != NameOptions.Anon ? sub.Name : Utils.GenerateHashedName(sub.Name);
    }

    public string GetSubIdentifier(Submarine sub, FcSubmarines fc)
    {
        var name = $"[{GenerateName(fc)}] {sub.Name} ({sub.Identifier()})";
        return Plugin.Configuration.NameOption != NameOptions.Anon ? name : $"{Utils.GenerateHashedName(name)}@{fc.World}";
    }

    public string GetCombinedName(FcSubmarines fc)
    {
        var name = $"({fc.Tag}) {fc.CharacterName}@{fc.World}";
        return Plugin.Configuration.NameOption != NameOptions.Anon ? name : $"{Utils.GenerateHashedName(name)}@{fc.World}";
    }

    private string GenerateName(FcSubmarines fc)
    {
        return Plugin.Configuration.NameOption switch
        {
            NameOptions.Default => $"{fc.Tag}@{fc.World}",
            NameOptions.FullName => $"{fc.CharacterName}@{fc.World}",
            NameOptions.OnlyTag => $"{fc.Tag}",
            NameOptions.OnlyName => $"{fc.CharacterName}",
            NameOptions.Initials => $"{fc.CharacterName.Split(" ")[0][0]}. {fc.CharacterName.Split(" ")[1][0]}.@{fc.World}",
            NameOptions.OnlyInitials => $"{fc.CharacterName.Split(" ")[0][0]}. {fc.CharacterName.Split(" ")[1][0]}.",
            NameOptions.Anon => Utils.GenerateHashedName($"{fc.CharacterName}{fc.Tag}@{fc.World}"),
            _ => "Unknown"
        };
    }
}

public static class NameUtil
{
    public static string GetName(this NameOptions n)
    {
        return n switch
        {
            NameOptions.Default => Loc.Localize("Name Option - Default", "Default"),
            NameOptions.FullName => Loc.Localize("Name Option - Full Name", "Full Name"),
            NameOptions.OnlyTag => Loc.Localize("Name Option - Tag", "Only Tag"),
            NameOptions.OnlyName => Loc.Localize("Name Option - Name", "Only Name"),
            NameOptions.Initials => Loc.Localize("Name Option - Initials", "Initials"),
            NameOptions.OnlyInitials => Loc.Localize("Name Option - Only Initials", "Only Initials"),
            NameOptions.Anon => Loc.Localize("Name Option - Anon", "Anonymized"),
            _ => "Unknown"
        };
    }

    public static string GetExample(this NameOptions n)
    {
        return n switch
        {
            NameOptions.Default => "XYZ@Balmung",
            NameOptions.FullName => "Limsa Miqo@Balmung",
            NameOptions.OnlyTag => "CAT",
            NameOptions.OnlyName => "Rain Fhuz",
            NameOptions.Initials => "E. R.@Phoenix",
            NameOptions.OnlyInitials => "R. P.",
            NameOptions.Anon => "FE44BFE5AE",
            _ => "Unknown"
        };
    }
}
