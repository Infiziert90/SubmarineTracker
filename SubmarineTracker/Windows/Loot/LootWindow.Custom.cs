using System.Diagnostics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Loot;

public partial class LootWindow
{
    private int CurrentProfileId;
    private int FcSelection;

    private static readonly DateTime CustomMinimalDate = new(2023, 4, 1);
    private DateTime CustomMinDate = CustomMinimalDate;
    private DateTime CustomMaxDate = DateTime.Now.AddDays(5);

    private string CustomMinString = "";
    private string CustomMaxString = "";

    private bool HeaderOpen;

    private void CustomLootTab()
    {
        if (ImGui.BeginTabItem($"{Loc.Localize("Loot Tab - Custom", "Custom")}##Custom"))
        {
            ImGuiHelpers.ScaledDummy(5.0f);
            var longText = "Profile:";
            var length = ImGui.CalcTextSize(longText).X + (10.0f * ImGuiHelpers.GlobalScale);

            Plugin.EnsureFCOrderSafety();
            var existingFCs = Plugin.Configuration.FCIdOrder
                                            .Select(id => $"{Plugin.NameConverter.GetName(Plugin.DatabaseCache.GetFreeCompanies()[id])}##{id}")
                                            .Prepend(Loc.Localize("Terms - All", "All"))
                                            .ToArray();

            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.ParsedOrange, "FC:");
            ImGui.SameLine(length);
            Helper.DrawComboWithArrows("##lootSubSelection", ref FcSelection, ref existingFCs, 3);

            var combo = Plugin.Configuration.CustomLootProfiles.Keys.ToArray();
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.ParsedOrange, longText);
            ImGui.SameLine(length);
            Helper.DrawComboWithArrows("##ProfileSelector", ref CurrentProfileId, ref combo, 1);
            var selected = Plugin.Configuration.CustomLootProfiles[combo[CurrentProfileId]];

            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5.0f);

            var numSubs = 0;
            var numVoyages = 0;
            var moneyMade = 0L;
            var bigList = new Dictionary<Item, int>();
            foreach (var id in Plugin.DatabaseCache.GetFreeCompanies().Keys)
            {
                if (FcSelection != 0 && ulong.TryParse(existingFCs[FcSelection].Split("##")[1], out var selectedFcId))
                    if (selectedFcId != id)
                        continue;

                numSubs += Plugin.DatabaseCache.GetSubmarines(id).Length;
                numVoyages += Plugin.DatabaseCache.GetLoot().Where(l => l.FreeCompanyId == id).Count(l => DateCompare(l.Date) && (!Plugin.Configuration.ExcludeLegacy || l.Valid));

                foreach (var (item, count) in Plugin.DatabaseCache.GetFCTimeLoot(id).Where(pair => DateCompare(pair.Key)).SelectMany(pair => pair.Value))
                {
                    if (!selected.ContainsKey(item.RowId))
                        continue;

                    if (!bigList.TryAdd(item, count))
                        bigList[item] += count;
                }
            }

            var useLimit = (Plugin.Configuration.DateLimit != DateLimit.None || (CustomMinDate != CustomMinimalDate && CustomMaxDate != DateTime.Now));

            var textHeight = ImGui.CalcTextSize("XXXX").Y * 6.0f; // giving space for 6.0 lines
            var optionHeight = (HeaderOpen ? -65 : 0) * ImGuiHelpers.GlobalScale;
            if (ImGui.BeginChild("##customLootTableChild", new Vector2(0, -textHeight + optionHeight)))
            {
                if (selected.Count == 0)
                {
                    Helper.WrappedError(Loc.Localize("Loot Tab Custom - Profile Empty", "This profile has no tracked items."));
                    Helper.WrappedError(Loc.Localize("Loot Tab Custom - Profile Tip", "You can add items via the loot tab under configuration."));
                }
                else if (bigList.Count == 0)
                {
                    Helper.WrappedError($"{Loc.Localize("Loot Tab Custom - None 1", "None of the selected items have been looted")} {(useLimit ? Loc.Localize("Loot Tab Custom - None 2 Timeframe", "in the time frame") : Loc.Localize("Loot Tab Custom - None 2 Yet", "yet"))}.");
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

                            moneyMade += count * selected[item.RowId];
                        }

                        ImGui.EndTable();
                    }
                }
            }
            ImGui.EndChild();

            if (ImGui.BeginChild("##customLootTextChild", new Vector2(0, 0), false, 0))
            {
                var limit = useLimit ? Plugin.Configuration.DateLimit != DateLimit.None ? $"over {Plugin.Configuration.DateLimit.GetName()}" : $"from {CustomMinDate.ToLongDateWithoutWeekday()} to {CustomMaxDate.ToLongDateWithoutWeekday()}" : "";
                ImGui.TextWrapped(Loc.Localize("Loot Tab Custom - Reward Amount", "The above rewards have been obtained {0} from a total of {1} voyages ({2} submarines).").Format(limit, numVoyages, numSubs));
                ImGui.TextWrapped(Loc.Localize("Loot Tab Custom - Money Made", "This made you a total of {0:N0} gil.").Format(moneyMade));

                ImGuiHelpers.ScaledDummy(3.0f);

                HeaderOpen = ImGui.CollapsingHeader(Loc.Localize("Terms - Options", "Options"));
                if (HeaderOpen)
                {
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Loot Tab Entry - Fixed", "Fixed:"));
                    ImGui.SameLine();
                    if (ImGui.BeginCombo($"##lootOptionCombo", Plugin.Configuration.DateLimit.GetName()))
                    {
                        foreach (var dateLimit in (DateLimit[]) Enum.GetValues(typeof(DateLimit)))
                        {
                            if (ImGui.Selectable(dateLimit.GetName()))
                            {
                                Plugin.Configuration.DateLimit = dateLimit;
                                Plugin.Configuration.Save();
                            }
                        }

                        ImGui.EndCombo();
                    }
                    ImGuiComponents.HelpMarker(Loc.Localize("Loot Tab Tooltip - Fixed", "Selecting None will allow you to pick a specific time frame."));

                    if (Plugin.Configuration.DateLimit == DateLimit.None)
                    {
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Loot Tab Entry - FromTo Date Selection", "FromTo:"));
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
        if (Plugin.Configuration.DateLimit != DateLimit.None)
        {
            var dateLimit = Plugin.Configuration.DateLimit.ToDate();
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
