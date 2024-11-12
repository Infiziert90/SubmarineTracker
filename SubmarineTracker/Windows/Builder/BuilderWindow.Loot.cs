using Dalamud.Interface;
using Lumina.Excel.Sheets;
using SubmarineTracker.Data;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow
{
    private uint CurrentSearchSelection;
    public ExcelSheetSelector<Item>.ExcelSheetPopupOptions? SearchPopupOptions;

    private bool LootTab()
    {
        var open = ImGui.BeginTabItem($"{Loc.Localize("Loot Tab - Search", "Loot")}##Loot");
        if (open)
        {
            ImGuiHelpers.ScaledDummy(5.0f);

            var width = ImGui.GetContentRegionAvail().X / 3;
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudViolet, "Search");
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Button(FontAwesomeIcon.Search.ToIconString(), new Vector2(width, 0));
            ImGui.PopFont();

            SearchPopupOptions ??= new ExcelSheetSelector<Item>.ExcelSheetPopupOptions
            {
                CloseOnSelection = true,
                FormatRow = a => a.RowId switch { _ => $"[#{a.RowId}] {ToStr(a.Name)}" },
                FilteredSheet = Sheets.ItemSheet.Where(i => Importer.ItemDetailed.Items.ContainsKey(i.RowId)),
            };

            if (ExcelSheetSelector<Item>.ExcelSheetPopup("BuilderSearchAddPopup", out var row, SearchPopupOptions))
                CurrentSearchSelection = row;

            ImGuiHelpers.ScaledDummy(10.0f);

            if (CurrentSearchSelection == 0)
            {
                ImGui.TextUnformatted("No search target selected ...");

                ImGui.EndTabItem();
                return open;
            }

            var item = Sheets.ItemSheet.GetRow(CurrentSearchSelection)!;
            Helper.IconHeader(item.Icon, new Vector2(32, 32), ToStr(item.Name), ImGuiColors.ParsedOrange);
            if (ImGui.BeginTable("##searchColumn", 5, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Sector", 0);
                ImGui.TableSetupColumn("Tier", 0);
                ImGui.TableSetupColumn("Poor", 0);
                ImGui.TableSetupColumn("Normal", 0);
                ImGui.TableSetupColumn("Optimal", 0);

                ImGui.TableHeadersRow();

                foreach (var itemDetail in Importer.ItemDetailed.Items[item.RowId])
                {
                    var subRow = Sheets.ExplorationSheet.GetRow(itemDetail.Sector)!;

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{UpperCaseStr(subRow.Destination)} ({NumToLetter(subRow.RowId, true)} - {MapToThreeLetter(subRow.RowId, true)})");

                    ImGui.TableNextColumn();
                    Helper.CenterText($"{itemDetail.Tier}");

                    ImGui.TableNextColumn();
                    Helper.CenterText($"{itemDetail.Poor}");

                    ImGui.TableNextColumn();
                    Helper.CenterText($"{itemDetail.Normal}");

                    ImGui.TableNextColumn();
                    Helper.CenterText($"{itemDetail.Optimal}");
                }

                ImGui.EndTable();
            }

            ImGui.EndTabItem();
        }

        return open;
    }
}
