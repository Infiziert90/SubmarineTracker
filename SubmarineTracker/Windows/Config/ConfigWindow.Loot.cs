using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiNotification;
using Lumina.Excel.GeneratedSheets;

using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private static ExcelSheetSelector.ExcelSheetPopupOptions<Item> ItemPopupOptions = null!;

    private int CurrentCollectionId;
    private string NewProfileName = string.Empty;

    private void InitializeLoot()
    {
        ItemPopupOptions = new ExcelSheetSelector.ExcelSheetPopupOptions<Item>
        {
            FormatRow = a => a.RowId switch { _ => $"[#{a.RowId}] {ToStr(a.Name)}" },
            FilteredSheet = Sheets.ItemSheet.Skip(1).Where(i => ToStr(i.Name) != "")
        };
    }

    private void Loot()
    {
        using var tabItem = ImRaii.TabItem($"{Loc.Localize("Config Tab - Loot", "Loot")}##Loot");
        if (!tabItem.Success)
            return;

        ImGuiHelpers.ScaledDummy(5.0f);
        var changed = false;

        ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Options", "Options:"));
        ImGuiHelpers.ScaledIndent(10.0f);
        changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Legacy", "Exclude Legacy Loot"), ref Plugin.Configuration.ExcludeLegacy);
        ImGuiHelpers.ScaledIndent(-10.0f);

        ImGuiHelpers.ScaledDummy(5.0f);
        ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Collections", "Collections:"));
        var combo = Plugin.Configuration.CustomLootProfiles.Keys.ToArray();
        Helper.DrawComboWithArrows("##CollectionSelector", ref CurrentCollectionId, ref combo);
        ImGui.SameLine();
        var forbidden = CurrentCollectionId == 0;
        using (var _ = ImRaii.Disabled(forbidden))
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
            {
                Plugin.Configuration.CustomLootProfiles.Remove(combo[CurrentCollectionId]);
                CurrentCollectionId = 0;
            }
            if (forbidden && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip(Loc.Localize("Config Tooltip - Default Collection", "Default collection can't be deleted"));
        }

        var selected = Plugin.Configuration.CustomLootProfiles[combo[CurrentCollectionId]];

        ImGui.InputTextWithHint("##CollectionNameInput", Loc.Localize("Config Text Input - Collection Name", "New Collection Name"), ref NewProfileName, 32);
        ImGui.SameLine();
        var notValid = NewProfileName.Length <= 3;
        using (var _ = ImRaii.Disabled(notValid))
        {
            if (ImGuiComponents.IconButton(2, FontAwesomeIcon.Plus))
            {
                if (!Plugin.Configuration.CustomLootProfiles.TryAdd(NewProfileName, new Dictionary<uint, int>()))
                    AddNotification(Loc.Localize("Error - Collection Exists", "Collection with this name already exists"), NotificationType.Error, false);

                combo = Plugin.Configuration.CustomLootProfiles.Keys.ToArray();
                CurrentCollectionId = Array.FindIndex(combo, s => s == NewProfileName);
                if (CurrentCollectionId == -1)
                    CurrentCollectionId = 0;

                NewProfileName = string.Empty;
                selected = Plugin.Configuration.CustomLootProfiles[combo[CurrentCollectionId]];
                Plugin.Configuration.Save();
            }
        }

        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Collection Items", "Collection Items:"));
        Helper.Button(FontAwesomeIcon.Plus, new Vector2(ImGui.GetContentRegionAvail().X / 3, 0));

        if (ExcelSheetSelector.ExcelSheetPopup("ItemAddPopup", out var row, ItemPopupOptions))
        {
            var item = Sheets.ItemSheet.GetRow(row)!;
            var value = (int)(item.PriceLow > 1000 ? item.PriceLow : 0);

            if (selected.TryAdd(row, value))
                Plugin.Configuration.Save();
        }

        using var table = ImRaii.Table("##DeleteLootTable", 3);
        if (table.Success)
        {
            ImGui.TableSetupColumn(Loc.Localize("Terms - Item", "Item"));
            ImGui.TableSetupColumn(Loc.Localize("Terms - Price", "Price"), 0, 0.4f);
            ImGui.TableSetupColumn(Loc.Localize("Terms - Delete", "Del"), 0, 0.15f);

            ImGui.TableHeadersRow();

            uint deletionKey = 0;
            foreach (var ((item, value), idx) in selected.Select((val, i) => (val, i)))
            {
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(ToStr(Sheets.ItemSheet.GetRow(item)!.Name));

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                var val = value;
                if (ImGui.InputInt($"##inputValue{item}", ref val, 0))
                {
                    val = Math.Clamp(val, 0, int.MaxValue);
                    selected[item] = val;
                    Plugin.Configuration.Save();
                }

                ImGui.TableNextColumn();
                if (ImGuiComponents.IconButton(idx, FontAwesomeIcon.Trash))
                    deletionKey = item;

                ImGui.TableNextRow();
            }

            if (deletionKey != 0)
            {
                selected.Remove(deletionKey);
                Plugin.Configuration.Save();
            }
        }

        if (changed)
            Plugin.Configuration.Save();
    }
}
