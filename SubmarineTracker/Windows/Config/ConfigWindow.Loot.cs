using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal.Notifications;
using Lumina.Excel.GeneratedSheets;
using SubmarineTracker.Data;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private static ExcelSheetSelector.ExcelSheetPopupOptions<Item> ItemPopupOptions = null!;

    private int CurrentProfileId;
    private string NewProfileName = string.Empty;

    private void InitializeLoot()
    {
        ItemPopupOptions = new ExcelSheetSelector.ExcelSheetPopupOptions<Item>
        {
            FormatRow = a => a.RowId switch { _ => $"[#{a.RowId}] {ToStr(a.Name)}" },
            FilteredSheet = Plugin.Data.GetExcelSheet<Item>()!.Skip(1).Where(i => ToStr(i.Name) != "")
        };
    }

    private void Loot()
    {
        if (ImGui.BeginTabItem($"{Loc.Localize("Config Tab - Loot", "Loot")}##Loot"))
        {
            ImGuiHelpers.ScaledDummy(5.0f);
            var changed = false;

            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Options", "Options:"));
            ImGui.Indent(10.0f);
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Legacy", "Exclude Legacy Loot"), ref Configuration.ExcludeLegacy);
            ImGui.Unindent(10.0f);

            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.TextColored(ImGuiColors.ParsedOrange, "Select a profile:");
            var combo = Configuration.CustomLootProfiles.Keys.ToArray();
            Helper.DrawComboWithArrows("##ProfileSelector", ref CurrentProfileId, ref combo, 0);
            var selected = Configuration.CustomLootProfiles[combo[CurrentProfileId]];
            ImGui.InputTextWithHint("##ProfileNameInput", "Profile Name", ref NewProfileName, 32);
            ImGui.SameLine();
            var notValid = NewProfileName.Length <= 3;
            if (notValid) ImGui.BeginDisabled();
            if (ImGuiComponents.IconButton(2, FontAwesomeIcon.Plus))
            {
                if (!Configuration.CustomLootProfiles.TryAdd(NewProfileName, new Dictionary<uint, int>()))
                    Plugin.PluginInterface.UiBuilder.AddNotification("Profile name already exists ...", "[Submarine Tracker]", NotificationType.Error);

                combo = Configuration.CustomLootProfiles.Keys.ToArray();
                CurrentProfileId = Array.FindIndex(combo, s => s == NewProfileName);
                if (CurrentProfileId == -1)
                    CurrentProfileId = 0;

                NewProfileName = string.Empty;
                selected = Configuration.CustomLootProfiles[combo[CurrentProfileId]];
                Configuration.Save();
            }
            if (notValid) ImGui.EndDisabled();

            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Add Items", "Add Items:"));

            var buttonWidth = ImGui.GetContentRegionAvail().X / 2;
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), new Vector2(buttonWidth, 0));
            ImGui.PopFont();

            if (ExcelSheetSelector.ExcelSheetPopup("ItemAddPopup", out var row, ItemPopupOptions))
            {
                var item = ItemSheet.GetRow(row)!;
                var value = (int)(item.PriceLow > 1000 ? item.PriceLow : 0);

                if (selected.TryAdd(row, value))
                    Configuration.Save();
            }

            if (ImGui.BeginTable("##DeleteLootTable", 3))
            {
                ImGui.TableSetupColumn(Loc.Localize("Terms - Item", "Item"));
                ImGui.TableSetupColumn(Loc.Localize("Terms - Price", "Price"), 0, 0.4f);
                ImGui.TableSetupColumn(Loc.Localize("Terms - Delete", "Del"), 0, 0.15f);

                ImGui.TableHeadersRow();

                uint deletionKey = 0;
                foreach (var ((item, value), idx) in selected.Select((val, i) => (val, i)))
                {
                    var resolvedItem = ItemSheet.GetRow(item)!;
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{ToStr(resolvedItem.Name)}");

                    ImGui.TableNextColumn();
                    var val = value;
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputInt($"##inputValue{item}", ref val, 0))
                    {
                        val = Math.Clamp(val, 0, int.MaxValue);
                        selected[item] = val;
                        Configuration.Save();
                    }

                    ImGui.TableNextColumn();
                    if (ImGuiComponents.IconButton(idx, FontAwesomeIcon.Trash))
                        deletionKey = item;

                    ImGui.TableNextRow();
                }

                if (deletionKey != 0)
                {
                    selected.Remove(deletionKey);
                    Configuration.Save();
                }

                ImGui.EndTable();
            }

            if (changed)
            {
                foreach (var fc in Submarines.KnownSubmarines.Values)
                    fc.Refresh = true;
                Configuration.Save();
            }

            ImGui.EndTabItem();
        }
    }
}
