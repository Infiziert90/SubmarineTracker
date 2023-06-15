using Dalamud.Interface.Components;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private void Loot()
    {
        if (ImGui.BeginTabItem("Loot"))
        {
            // ImGuiHelpers.ScaledDummy(5.0f);
            // var changed = false;
            //
            // ImGui.TextColored(ImGuiColors.DalamudViolet, "Options:");
            // ImGui.Indent(10.0f);
            // changed |= ImGui.Checkbox("Exclude Legacy Data", ref Configuration.ExcludeLegacy);
            // ImGui.Unindent(10.0f);

            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.TextColored(ImGuiColors.DalamudViolet, "Add Items:");

            var buttonWidth = ImGui.GetContentRegionAvail().X / 2;
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), new Vector2(buttonWidth, 0));
            ImGui.PopFont();

            if (ExcelSheetSelector.ExcelSheetPopup("ItemAddPopup", out var row, ItemPopupOptions))
            {
                var item = ItemSheet.GetRow(row)!;
                var value = (int)(item.PriceLow > 1000 ? item.PriceLow : 0);

                if (Configuration.CustomLootWithValue.TryAdd(row, value))
                    Configuration.Save();
            }

            if (ImGui.BeginTable("##DeleteLootTable", 3))
            {
                ImGui.TableSetupColumn("Item");
                ImGui.TableSetupColumn("Value", 0, 0.4f);
                ImGui.TableSetupColumn("Del", 0, 0.15f);

                ImGui.TableHeadersRow();

                uint deletionKey = 0;
                foreach (var ((item, value), idx) in Configuration.CustomLootWithValue.Select((val, i) => (val, i)))
                {
                    var resolvedItem = ItemSheet.GetRow(item)!;
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{Utils.ToStr(resolvedItem.Name)}");

                    ImGui.TableNextColumn();
                    var val = value;
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputInt($"##inputValue{item}", ref val, 0))
                    {
                        val = Math.Clamp(val, 0, int.MaxValue);
                        Configuration.CustomLootWithValue[item] = val;
                        Configuration.Save();
                    }

                    ImGui.TableNextColumn();
                    if (ImGuiComponents.IconButton(idx, FontAwesomeIcon.Trash))
                        deletionKey = item;

                    ImGui.TableNextRow();
                }

                if (deletionKey != 0)
                {
                    Configuration.CustomLootWithValue.Remove(deletionKey);
                    Configuration.Save();
                }

                ImGui.EndTable();
            }

            ImGuiHelpers.ScaledDummy(5);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5);

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Time Frame:");
            if (ImGui.BeginCombo($"##lootOptionCombo", DateUtil.GetDateLimitName(Configuration.DateLimit)))
            {
                foreach (var dateLimit in (DateLimit[])Enum.GetValues(typeof(DateLimit)))
                {
                    if (ImGui.Selectable(DateUtil.GetDateLimitName(dateLimit)))
                    {
                        Configuration.DateLimit = dateLimit;
                        Configuration.Save();
                    }
                }

                ImGui.EndCombo();
            }

            // if (changed)
            //     Configuration.Save();

            ImGui.EndTabItem();
        }
    }
}
