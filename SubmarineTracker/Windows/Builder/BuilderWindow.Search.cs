using Dalamud.Interface;
using Lumina.Excel.GeneratedSheets;
using SubmarineTracker.Data;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow
{
    private uint CurrentSearchSelection;
    public ExcelSheetSelector.ExcelSheetPopupOptions<Item>? SearchPopupOptions;

    private void SearchTab()
    {
        if (ImGui.BeginTabItem($"{Loc.Localize("Builder Tab - Search", "Search")}##Search"))
        {
            if (ImGui.BeginChild("search", new Vector2(0, 0)))
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.Button($"{FontAwesomeIcon.Plus.ToIconString()}##addButton");
                ImGui.PopFont();

                SearchPopupOptions ??= new ExcelSheetSelector.ExcelSheetPopupOptions<Item>
                {
                    FormatRow = a => a.RowId switch { _ => $"[#{a.RowId}] {ToStr(a.Name)}" },
                    FilteredSheet = ItemSheet.Where(i => Importer.ItemDetails.ContainsKey(i.RowId))
                };

                if (ExcelSheetSelector.ExcelSheetPopup("BuilderSearchAddPopup", out var row, SearchPopupOptions))
                {
                    CurrentSearchSelection = row;
                }

                if (CurrentSearchSelection == 0)
                {
                    ImGui.TextUnformatted("No item selected ...");

                    ImGui.EndChild();
                    ImGui.EndTabItem();
                    return;
                }

                var item = ItemSheet.GetRow(CurrentSearchSelection)!;
                ImGui.TextUnformatted($"Displaying item: {item.Name}");
                if (ImGui.BeginTable("##searchColumn", 5))
                {
                    ImGui.TableSetupColumn("Sector", 0);
                    ImGui.TableSetupColumn("Tier", 0);
                    ImGui.TableSetupColumn("Poor", 0);
                    ImGui.TableSetupColumn("Normal", 0);
                    ImGui.TableSetupColumn("Optimal", 0);

                    ImGui.TableHeadersRow();

                    foreach (var itemDetail in Importer.ItemDetails[item.RowId])
                    {
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{ExplorationSheet.GetRow(itemDetail.Sector)!.Destination}");

                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{itemDetail.Tier}");

                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{itemDetail.Poor}");

                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{itemDetail.Normal}");

                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{itemDetail.Optimal}");
                    }

                    ImGui.EndTable();
                }
            }

            ImGui.EndChild();
            ImGui.EndTabItem();
        }
    }
}
