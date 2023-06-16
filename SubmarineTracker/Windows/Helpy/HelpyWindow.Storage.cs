using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Helpy;

public partial class HelpyWindow
{
    private static readonly Vector2 IconSize = new(32, 32);

    private bool StorageTab()
    {
        var open = ImGui.BeginTabItem("Storage");
        if (open)
        {
            ImGuiHelpers.ScaledDummy(5.0f);
            if (!Plugin.AllaganToolsConsumer.IsAvailable)
            {
                ImGui.TextColored(ImGuiColors.ParsedOrange, "AllaganTools not available.");
                ImGui.EndTabItem();

                return open;
            }

            // build cache if needed
            Storage.BuildStorageCache();

            foreach (var (key, fc) in Submarines.KnownSubmarines)
            {
                var text = $"{fc.Tag}@{fc.World}";
                if (Configuration.UseCharacterName && fc.CharacterName != "")
                    text = $"{fc.CharacterName}@{fc.World}";

                ImGui.TextColored(ImGuiColors.DalamudViolet, $"{text}:");

                ImGui.Indent(10.0f);
                if (ImGui.BeginTable($"##submarineOverview##{key}", 3))
                {
                    ImGui.TableSetupColumn("##icon", 0, 0.1f);
                    ImGui.TableSetupColumn("##count", 0, 0.15f);
                    ImGui.TableSetupColumn("##item");

                    foreach (var cached in Storage.StorageCache[key].Values)
                    {
                        ImGui.TableNextColumn();
                        DrawIcon(cached.Item.Icon);
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
                }
                ImGui.EndTable();
                ImGui.Unindent(10.0f);

                ImGuiHelpers.ScaledDummy(10.0f);
            }
        }
        ImGui.EndTabItem();

        return open;
    }

    private static void DrawIcon(uint iconId)
    {
        var texture = TexturesCache.Instance!.GetTextureFromIconId(iconId);
        ImGui.Image(texture.ImGuiHandle, IconSize);
    }
}
