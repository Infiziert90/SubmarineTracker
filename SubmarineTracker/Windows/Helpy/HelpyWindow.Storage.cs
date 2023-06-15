using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Helpy;

public partial class HelpyWindow
{
    // https://github.com/Critical-Impact/CriticalCommonLib/blob/591dc2592341aa8b7c3aca1175a792568ca51e85/Enums/InventoryType.cs#L4
    private static uint[] Inventories = { 0, 1, 2, 3, 4000, 4001, 4100, 4101, 20000, 20001, 20002, 20003, 20004, 20005, 20006, 20007, 20008, 20009, 20009, 20010 };
    private static readonly Vector2 IconSize = new(32, 32);

    private void StorageTab()
    {
        if (ImGui.BeginTabItem("Storage"))
        {
            ImGuiHelpers.ScaledDummy(5.0f);
            if (!Plugin.AllaganToolsConsumer.IsAvailable)
            {
                ImGui.TextColored(ImGuiColors.ParsedOrange, "AllaganTools not available.");
                ImGui.EndTabItem();

                return;
            }

            foreach (var (key, fc) in Submarines.KnownSubmarines)
            {
                var text = $"{fc.Tag}@{fc.World}";
                if (Configuration.UseCharacterName && fc.CharacterName != "")
                    text = $"{fc.CharacterName}@{fc.World}";

                ImGui.TextColored(ImGuiColors.DalamudViolet, $"{text}:");

                if (ImGui.BeginTable($"##submarineOverview##{key}", 3))
                {
                    ImGui.TableSetupColumn("##icon", 0, 0.15f);
                    ImGui.TableSetupColumn("##item");
                    ImGui.TableSetupColumn("##count");

                    uint tanks = 0;
                    uint kits = 0;
                    foreach (var inventory in Inventories)
                    {
                        tanks += Plugin.AllaganToolsConsumer.GetCount((uint) ImportantItems.Tanks, key, inventory);
                        kits += Plugin.AllaganToolsConsumer.GetCount((uint) ImportantItems.Kits, key, inventory);
                    }

                    var iTank = ImportantItems.Tanks.GetItem();
                    ImGui.TableNextColumn();
                    DrawIcon(iTank.Icon);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(Utils.ToStr(iTank.Name));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{tanks}");
                    ImGui.TableNextRow();

                    var iKit = ImportantItems.Kits.GetItem();
                    ImGui.TableNextColumn();
                    DrawIcon(iKit.Icon);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(Utils.ToStr(iKit.Name));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{kits}");
                    ImGui.TableNextRow();

                    var possibleItems = (ImportantItems[]) Enum.GetValues(typeof(ImportantItems));
                    foreach (var item in possibleItems.Skip(2).Select(e => e.GetItem()))
                    {
                        uint count = 0;
                        foreach (var inventory in Inventories)
                            count += Plugin.AllaganToolsConsumer.GetCount(item.RowId, key, inventory);

                        if (count != 0 && count != uint.MaxValue)
                        {
                            ImGui.TableNextColumn();
                            DrawIcon(item.Icon);
                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted(Utils.ToStr(item.Name));
                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted($"{count}");
                            ImGui.TableNextRow();
                        }
                    }
                }

                ImGui.EndTable();

                ImGuiHelpers.ScaledDummy(10.0f);
            }
        }
        ImGui.EndTabItem();
    }

    private static void DrawIcon(uint iconId)
    {
        var texture = TexturesCache.Instance!.GetTextureFromIconId(iconId);
        ImGui.Image(texture.ImGuiHandle, IconSize);
    }
}
