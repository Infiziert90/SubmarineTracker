using Dalamud.Interface.Utility.Raii;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Helpy;

public partial class HelpyWindow
{
    private static readonly Vector2 IconSize = new(32, 32);

    private void StorageTab()
    {
        using var tabItem = ImRaii.TabItem($"{Loc.Localize("Helpy Tab - Storage", "Storage")}##Storage");
        if (!tabItem.Success)
            return;

        ImGuiHelpers.ScaledDummy(5.0f);
        if (!Plugin.AllaganToolsConsumer.IsAvailable)
        {
            ImGui.TextColored(ImGuiColors.ParsedOrange, Loc.Localize("Helpy Tab Warning - AllaganTools", "AllaganTools not available."));
            return;
        }

        // build cache if needed
        Storage.BuildStorageCache();

        foreach (var (key, fc) in Plugin.DatabaseCache.GetFreeCompanies())
        {
            ImGui.TextColored(ImGuiColors.DalamudViolet, $"{Plugin.NameConverter.GetName(fc)}:");

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

                ImGui.TableNextColumn();
                var count = $"{cached.Count}x";
                var width = ImGui.CalcTextSize(count).X;
                var space = ImGui.GetContentRegionAvail().X;
                ImGui.SameLine(space-width);
                ImGui.TextUnformatted($"{cached.Count}x");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Utils.ToStr(cached.Item.Name));

                ImGui.TableNextRow();
            }
            ImGuiHelpers.ScaledDummy(10.0f);
        }
    }
}
