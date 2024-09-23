using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Loot;

public partial class LootWindow
{
    private int FcSelection;
    private int CurrentProfileId;

    private static readonly DateTime CustomMinimalDate = new(2023, 4, 1);
    private DateTime CustomMinDate = CustomMinimalDate;
    private DateTime CustomMaxDate = DateTime.Now.AddDays(5);

    private string CustomMinString = "";
    private string CustomMaxString = "";

    private bool HeaderOpen;

    private int NumSubs;
    private int NumVoyages;
    private Dictionary<Item, int> CachedList = [];
    private long LastRefreshTime;

    private void CustomLootTab()
    {
        using var tabItem = ImRaii.TabItem($"{Loc.Localize("Loot Tab - Custom", "Custom")}##Custom");
        if (!tabItem.Success)
            return;

        var fcSelection = FcSelection;
        var currentProfile = CurrentProfileId;

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

        if (fcSelection != FcSelection || currentProfile != CurrentProfileId)
            LastRefreshTime = 0;

        // Rebuilds every 30s
        BuildCache(selected);

        var moneyMade = 0L;
        var useLimit = (Plugin.Configuration.DateLimit != DateLimit.None || (CustomMinDate != CustomMinimalDate && CustomMaxDate != DateTime.Now));

        var textHeight = ImGui.CalcTextSize("XXXX").Y * 6.0f; // giving space for 6.0 lines
        var optionHeight = (HeaderOpen ? -65 : 0) * ImGuiHelpers.GlobalScale;
        using (var lootChild = ImRaii.Child("##customLootTableChild", new Vector2(0, -textHeight + optionHeight)))
        {
            if (lootChild.Success)
            {
                if (selected.Count == 0)
                {
                    Helper.WrappedError(Loc.Localize("Loot Tab Custom - Profile Empty", "This profile has no tracked items."));
                    Helper.WrappedError(Loc.Localize("Loot Tab Custom - Profile Tip", "You can add items via the loot tab under configuration."));
                }
                else if (CachedList.Count == 0)
                {
                    Helper.WrappedError($"{Loc.Localize("Loot Tab Custom - None 1", "None of the selected items have been looted")} {(useLimit ? Loc.Localize("Loot Tab Custom - None 2 Timeframe", "in the time frame") : Loc.Localize("Loot Tab Custom - None 2 Yet", "yet"))}.");
                }
                else
                {
                    using var table = ImRaii.Table("##customLootTable", 3);
                    if (table.Success)
                    {
                        ImGui.TableSetupColumn("##icon", 0, 0.15f);
                        ImGui.TableSetupColumn("##item");
                        ImGui.TableSetupColumn("##amount", 0, 0.3f);

                        foreach (var (item, count) in CachedList.OrderBy(pair => pair.Key.RowId))
                        {
                            moneyMade += count * selected[item.RowId];

                            ImGui.TableNextColumn();
                            Helper.DrawScaledIcon(item.Icon, IconSize);

                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted(Utils.ToStr(item.Name));

                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted($"{count:N0}");

                            ImGui.TableNextRow();
                        }
                    }
                }
            }
        }

        using var textChild = ImRaii.Child("##customLootTextChild", new Vector2(0, 0), false, 0);
        if (!textChild.Success)
            return;

        var limit = useLimit ? Plugin.Configuration.DateLimit != DateLimit.None ? $"over {Plugin.Configuration.DateLimit.GetName()}" : $"from {CustomMinDate.ToLongDateWithoutWeekday()} to {CustomMaxDate.ToLongDateWithoutWeekday()}" : "";
        ImGui.TextWrapped(Loc.Localize("Loot Tab Custom - Reward Amount", "The above rewards have been obtained {0} from a total of {1} voyages ({2} submarines).").Format(limit, NumVoyages, NumSubs));
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
                        LastRefreshTime = 0;

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

                if (DateWidget.DatePickerWithInput("FromDate", 1, ref CustomMinString, ref CustomMinDate, Format))
                    LastRefreshTime = 0;

                if (DateWidget.DatePickerWithInput("ToDate", 2, ref CustomMaxString, ref CustomMaxDate, Format, true))
                    LastRefreshTime = 0;

                ImGui.SameLine(0, 3.0f * ImGuiHelpers.GlobalScale);
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Recycle))
                    CustomReset();

                if (DateWidget.Validate(CustomMinimalDate, ref CustomMinDate, ref CustomMaxDate))
                    CustomRefresh();
            }
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

        // Rebuild Cache next frame
        LastRefreshTime = 0;
    }

    public void CustomReset()
    {
        CustomMinDate = CustomMinimalDate;
        CustomMaxDate = DateTime.Now;

        CustomRefresh();
    }

    private void BuildCache(Dictionary<uint, int> selected)
    {
        if (Environment.TickCount64 < LastRefreshTime)
            return;
        LastRefreshTime = Environment.TickCount64 + 30_000; // 30s

        NumSubs = 0;
        NumVoyages = 0;
        CachedList = [];
        foreach (var id in Plugin.DatabaseCache.GetFreeCompanies().Keys)
        {
            if (FcSelection != 0)
                if (Plugin.Configuration.FCIdOrder[FcSelection - 1] != id)
                    continue;

            NumSubs += Plugin.DatabaseCache.GetSubmarines(id).Length;
            NumVoyages += Plugin.DatabaseCache.GetLoot()
                                .Where(l => l.FreeCompanyId == id && DateCompare(l.Date) && (!Plugin.Configuration.ExcludeLegacy || l.Valid))
                                .Select(l => l.Date.Ticks).ToHashSet().Count;

            foreach (var (item, count) in Plugin.DatabaseCache.GetFCTimeLoot(id).Where(pair => DateCompare(pair.Key)).SelectMany(pair => pair.Value))
            {
                if (!selected.ContainsKey(item.RowId))
                    continue;

                if (!CachedList.TryAdd(item, count))
                    CachedList[item] += count;
            }
        }
    }
}
