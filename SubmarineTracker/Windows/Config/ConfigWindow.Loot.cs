using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiNotification;
using Lumina.Excel.Sheets;
using SubmarineTracker.Resources;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private static ExcelSheetSelector<Item>.ExcelSheetPopupOptions ItemPopupOptions = null!;

    private int CurrentCollectionId;
    private string NewProfileName = string.Empty;

    private void InitializeLoot()
    {
        ItemPopupOptions = new ExcelSheetSelector<Item>.ExcelSheetPopupOptions
        {
            FormatRow = a => a.RowId switch { _ => $"[#{a.RowId}] {a.Name.ExtractText()}" },
            FilteredSheet = Sheets.ItemSheet.Skip(1).Where(i => i.Icon > 0)
        };
    }

    private void Loot()
    {
        using var tabItem = ImRaii.TabItem($"{Language.ConfigTabLoot}##Loot");
        if (!tabItem.Success)
            return;

        var changed = false;

        ImGuiHelpers.ScaledDummy(5.0f);
        Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabEntryOptions);
        using (ImRaii.PushIndent(10.0f))
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxLegacy, ref Plugin.Configuration.ExcludeLegacy);

        ImGuiHelpers.ScaledDummy(5.0f);
        Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabEntryCollections);
        var combo = Plugin.Configuration.CustomLootProfiles.Keys.ToArray();
        Helper.DrawComboWithArrows("##CollectionSelector", ref CurrentCollectionId, ref combo);

        ImGui.SameLine();

        var forbidden = CurrentCollectionId == 0;
        using (ImRaii.Disabled(forbidden))
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
            {
                Plugin.Configuration.CustomLootProfiles.Remove(combo[CurrentCollectionId]);
                CurrentCollectionId = 0;
            }

            if (forbidden && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                Helper.Tooltip(Language.ConfigTooltipDefaultCollection);
        }

        var selected = Plugin.Configuration.CustomLootProfiles[combo[CurrentCollectionId]];

        ImGui.InputTextWithHint("##CollectionNameInput", Language.ConfigTextInputCollectionName, ref NewProfileName, 32);
        ImGui.SameLine();

        using (ImRaii.Disabled(NewProfileName.Length <= 3))
        {
            if (ImGuiComponents.IconButton(2, FontAwesomeIcon.Plus))
            {
                if (!Plugin.Configuration.CustomLootProfiles.TryAdd(NewProfileName, new Dictionary<uint, int>()))
                    AddNotification(Language.ErrorCollectionExists, NotificationType.Error, false);

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

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabEntryCollectionItems);
        Helper.Button(FontAwesomeIcon.Plus, new Vector2(ImGui.GetContentRegionAvail().X / 3, 0));

        if (ExcelSheetSelector<Item>.ExcelSheetPopup("ItemAddPopup", out var row, ItemPopupOptions))
        {
            var item = Sheets.GetItem(row);
            var value = (int)(item.PriceLow > 1000 ? item.PriceLow : 0);

            if (selected.TryAdd(row, value))
                Plugin.Configuration.Save();
        }

        using var table = ImRaii.Table("##DeleteLootTable", 3);
        if (table.Success)
        {
            ImGui.TableSetupColumn(Language.TermsItem);
            ImGui.TableSetupColumn(Language.TermsPrice, 0, 0.4f);
            ImGui.TableSetupColumn(Language.TermsDelete, 0, 0.15f);

            ImGui.TableHeadersRow();

            uint deletionKey = 0;
            foreach (var ((item, value), idx) in selected.WithIndex())
            {
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Sheets.GetItem(item).Name.ExtractText());

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
