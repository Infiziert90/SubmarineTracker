using Dalamud.Interface.Components;
using Lumina.Excel.GeneratedSheets;
using SubmarineTracker.Data;
using static SubmarineTracker.Data.Submarines;

namespace SubmarineTracker.Windows.Loot;

public partial class LootWindow
{
    private int FcSelection;

    private static readonly DateTime CustomMinimalDate = new(2023, 4, 1);
    private DateTime CustomMinDate = CustomMinimalDate;
    private DateTime CustomMaxDate = DateTime.Now;

    private string CustomMinString = "";
    private string CustomMaxString = "";

    private bool HeaderOpen;

    private void CustomLootTab()
    {
        if (ImGui.BeginTabItem("Custom"))
        {


            if (!Configuration.CustomLootWithValue.Any())
            {
                ImGui.TextColored(ImGuiColors.ParsedOrange, "No Custom Loot");
                ImGui.TextColored(ImGuiColors.ParsedOrange, "You can add selected items via the loot tab under settings.");

                ImGui.EndTabItem();
                return;
            }

            ImGuiHelpers.ScaledDummy(5.0f);

            Plugin.EnsureFCOrderSafety();
            var existingFCs = Configuration.FCOrder
                                            .Select(id => $"{Helper.GetFCName(KnownSubmarines[id])}##{id}")
                                            .Prepend("All")
                                            .ToArray();

            Helper.DrawComboWithArrows("##lootSubSelection", ref FcSelection, ref existingFCs);
            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5.0f);

            var numSubs = 0;
            var numVoyages = 0;
            var moneyMade = 0L;
            var bigList = new Dictionary<Item, int>();
            foreach (var (id, fc) in KnownSubmarines)
            {
                if (FcSelection != 0 && ulong.TryParse(existingFCs[FcSelection].Split("##")[1], out var selectedFcId))
                    if (selectedFcId != id)
                        continue;

                fc.RebuildStats(Configuration.ExcludeLegacy);

                numSubs += fc.Submarines.Count;
                numVoyages += fc.SubLoot.Values.SelectMany(subLoot => subLoot.Loot.Select(pair => pair.Value.First())
                                                                             .Where(loot => DateCompare(loot.Date))
                                                                             .Where(loot => !Configuration.ExcludeLegacy || loot.Valid)).Count();

                foreach (var (item, count) in fc.TimeLoot.Where(pair => DateCompare(pair.Key)).SelectMany(pair => pair.Value))
                {
                    if (!Configuration.CustomLootWithValue.ContainsKey(item.RowId))
                        continue;

                    if (!bigList.ContainsKey(item))
                    {
                        bigList.Add(item, count);
                    }
                    else
                    {
                        bigList[item] += count;
                    }
                }
            }

            var useLimit = (Configuration.DateLimit != DateLimit.None || (CustomMinDate != CustomMinimalDate && CustomMaxDate != DateTime.Now));

            var textHeight = ImGui.CalcTextSize("XXXX").Y * 6.0f; // giving space for 6.0 lines
            var optionHeight = (HeaderOpen ? -65 : 0) * ImGuiHelpers.GlobalScale;
            if (ImGui.BeginChild("##customLootTableChild", new Vector2(0, -textHeight + optionHeight)))
            {
                if (!bigList.Any())
                {
                    Helper.WrappedError($"None of the selected items have been looted {(useLimit ? "in the time frame" : "yet")}.");
                }
                else
                {
                    if (ImGui.BeginTable($"##customLootTable", 3))
                    {
                        ImGui.TableSetupColumn("##icon", 0, 0.15f);
                        ImGui.TableSetupColumn("##item");
                        ImGui.TableSetupColumn("##amount", 0, 0.3f);

                        foreach (var (item, count) in bigList.OrderBy(pair => pair.Key.RowId))
                        {
                            ImGui.TableNextColumn();
                            Helper.DrawScaledIcon(item.Icon, IconSize);
                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted(Utils.ToStr(item.Name));
                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted($"{count:N0}");
                            ImGui.TableNextRow();

                            moneyMade += count * Configuration.CustomLootWithValue[item.RowId];
                        }

                        ImGui.EndTable();
                    }
                }
            }
            ImGui.EndChild();

            if (ImGui.BeginChild("##customLootTextChild", new Vector2(0, 0), false, 0))
            {
                var limit = useLimit ? Configuration.DateLimit != DateLimit.None ? $"over {Configuration.DateLimit.GetName()}" : $"from {CustomMinDate.ToLongDateWithoutWeekday()} to {CustomMaxDate.ToLongDateWithoutWeekday()}" : "";
                ImGui.TextWrapped($"The above rewards have been obtained {limit} from a total of {numVoyages} voyages ({numSubs} submarines).");
                ImGui.TextWrapped($"This made you a total of {moneyMade:N0} gil.");

                ImGuiHelpers.ScaledDummy(3.0f);

                HeaderOpen = ImGui.CollapsingHeader("Options");
                if (HeaderOpen)
                {
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextColored(ImGuiColors.DalamudViolet, "Fixed:");
                    ImGui.SameLine();
                    if (ImGui.BeginCombo($"##lootOptionCombo", Configuration.DateLimit.GetName()))
                    {
                        foreach (var dateLimit in (DateLimit[]) Enum.GetValues(typeof(DateLimit)))
                        {
                            if (ImGui.Selectable(dateLimit.GetName()))
                            {
                                Configuration.DateLimit = dateLimit;
                                Configuration.Save();
                            }
                        }

                        ImGui.EndCombo();
                    }
                    ImGuiComponents.HelpMarker("Selecting None will allow you to pick a specific time frame.");

                    if (Configuration.DateLimit == DateLimit.None)
                    {
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextColored(ImGuiColors.DalamudViolet, "FromTo:");
                        ImGui.SameLine();

                        DateWidget.DatePickerWithInput("FromDate", 1, ref CustomMinString, ref CustomMinDate, Format);
                        DateWidget.DatePickerWithInput("ToDate", 2, ref CustomMaxString, ref CustomMaxDate, Format, true);
                        ImGui.SameLine(0, 3.0f * ImGuiHelpers.GlobalScale);
                        if (ImGuiComponents.IconButton(FontAwesomeIcon.Recycle))
                            CustomReset();

                        if (DateWidget.Validate(CustomMinimalDate, ref CustomMinDate, ref CustomMaxDate))
                            CustomRefresh();
                    }
                }
            }
            ImGui.EndChild();

            ImGui.EndTabItem();
        }
    }

    public bool DateCompare(DateTime date)
    {
        if (Configuration.DateLimit != DateLimit.None)
        {
            var dateLimit = Configuration.DateLimit.ToDate();
            return date >= dateLimit;
        }

        return date >= CustomMinDate && date <= CustomMaxDate;
    }

    private void CustomRefresh()
    {
        CustomMinString = CustomMinDate.ToString(Format);
        CustomMaxString = CustomMaxDate.ToString(Format);
    }

    public void CustomReset()
    {
        CustomMinDate = CustomMinimalDate;
        CustomMaxDate = DateTime.Now;

        CustomRefresh();
    }
}
