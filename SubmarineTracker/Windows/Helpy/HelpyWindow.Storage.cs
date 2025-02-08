using SubmarineTracker.Data;
using SubmarineTracker.Resources;

namespace SubmarineTracker.Windows.Helpy;

public partial class HelpyWindow
{
    private static readonly Vector2 IconSize = new(32, 32);

    private void StorageTab()
    {
        using var tabItem = ImRaii.TabItem($"{Language.HelpyTabStorage}##Storage");
        if (!tabItem.Success)
            return;

        ImGuiHelpers.ScaledDummy(5.0f);
        if (!Plugin.AllaganToolsConsumer.IsAvailable)
        {
            Helper.TextColored(ImGuiColors.ParsedOrange, Language.HelpyTabWarningAllaganTools);
            return;
        }

        // build cache if needed
        Storage.BuildStorageCache();

        foreach (var (key, fc) in Plugin.DatabaseCache.GetFreeCompanies())
        {
            Helper.TextColored(ImGuiColors.DalamudViolet, $"{Plugin.NameConverter.GetName(fc)}:");

            using var indent = ImRaii.PushIndent(10.0f);
            using var table = ImRaii.Table($"##SubmarineOverview{key}", 3);
            if (!table.Success)
                continue;

            ImGui.TableSetupColumn("##icon", 0, 0.1f);
            ImGui.TableSetupColumn("##count", 0, 0.15f);
            ImGui.TableSetupColumn("##item");

            foreach (var cached in Storage.StorageCache[key].Values)
            {
                ImGui.TableNextColumn();
                Helper.DrawScaledIcon(cached.Item.Icon, IconSize);

                var count = $"{cached.Count}x";
                ImGui.TableNextColumn();
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(count).X);
                ImGui.TextUnformatted(count);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(cached.Item.Name.ExtractText());

                ImGui.TableNextRow();
            }

            ImGuiHelpers.ScaledDummy(10.0f);
        }
    }
}
