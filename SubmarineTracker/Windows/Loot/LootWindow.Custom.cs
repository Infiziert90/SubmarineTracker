using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Utility;
using Lumina.Excel.Sheets;
using SubmarineTracker.Data;
using SubmarineTracker.Resources;

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

    private float ContentHeight;

    private int NumSubs;
    private int NumVoyages;
    private Dictionary<Item, int> CachedList = [];
    private long LastRefreshTime;

    private void CustomLootTab()
    {
        using var tabItem = ImRaii.TabItem($"{Language.LootTabCustom}##Custom");
        if (!tabItem.Success)
            return;

        var fcSelection = FcSelection;
        var currentProfile = CurrentProfileId;

        ImGuiHelpers.ScaledDummy(5.0f);

        var longText = "Collection:";
        var length = ImGui.CalcTextSize(longText).X + (10.0f * ImGuiHelpers.GlobalScale);

        Plugin.EnsureFCOrderSafety();
        var existingFCs = Plugin.Configuration.ManagedFCs
                                        .Select(status => $"{Plugin.NameConverter.GetName(Plugin.DatabaseCache.GetFreeCompanies()[status.Id])}##{status.Id}")
                                        .Prepend(Language.TermsNotHidden)
                                        .Prepend(Language.TermsAll)
                                        .ToArray();

        ImGui.AlignTextToFramePadding();
        Helper.TextColored(ImGuiColors.ParsedOrange, Language.TermsFC);
        ImGui.SameLine(length);
        Helper.DrawComboWithArrows("##lootSubSelection", ref FcSelection, ref existingFCs, 3);

        var combo = Plugin.Configuration.CustomLootProfiles.Keys.ToArray();
        ImGui.AlignTextToFramePadding();
        Helper.TextColored(ImGuiColors.ParsedOrange, longText);
        ImGui.SameLine(length);
        Helper.DrawComboWithArrows("##ProfileSelector", ref CurrentProfileId, ref combo, 1);

        ImGuiHelpers.ScaledDummy(5.0f);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5.0f);

        if (fcSelection != FcSelection || currentProfile != CurrentProfileId)
            LastRefreshTime = 0;

        // Rebuilds every 30s
        var selected = Plugin.Configuration.CustomLootProfiles[combo[CurrentProfileId]];
        BuildCache(selected);

        var moneyMade = 0L;
        var useLimit = (Plugin.Configuration.DateLimit != DateLimit.None || (CustomMinDate != CustomMinimalDate && CustomMaxDate != DateTime.Now));

        using (var lootChild = ImRaii.Child("##customLootTableChild", new Vector2(0, -ContentHeight)))
        {
            if (lootChild.Success)
            {
                if (selected.Count == 0)
                {
                    Helper.WrappedError(Language.LootTabCustomProfileEmpty);
                    Helper.WrappedError(Language.LootTabCustomProfileTip);
                }
                else if (CachedList.Count == 0)
                {
                    Helper.WrappedError($"{Language.LootTabCustomNone1} {(useLimit ? Language.LootTabCustomNone2Timeframe : Language.LootTabCustomNone2Yet)}.");
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
                            // Long cast is required to prevent the calculation product from overflowing
                            moneyMade += (long) count * selected[item.RowId];

                            ImGui.TableNextColumn();
                            Helper.DrawScaledIcon(item.Icon, IconSize);

                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted(item.Name.ExtractText());

                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted($"{count:N0}");

                            ImGui.TableNextRow();
                        }
                    }
                }
            }
        }

        // Ensure content height is set to a minimum
        ContentHeight = ImGui.GetTextLineHeight() + (ImGui.GetStyle().ItemSpacing.Y * 2);

        using var textChild = ImRaii.Child("##customLootTextChild", Vector2.Zero);
        if (!textChild.Success)
            return;

        var pos = ImGui.GetCursorPos();
        var limit = useLimit ? Plugin.Configuration.DateLimit != DateLimit.None ? $"over {Plugin.Configuration.DateLimit.GetName()}" : $"from {CustomMinDate.ToLongDateWithoutWeekday()} to {CustomMaxDate.ToLongDateWithoutWeekday()}" : "";
        ImGui.TextWrapped(Language.LootTabCustomRewardAmount.Format(limit, NumVoyages, NumSubs));
        ImGui.TextWrapped(Language.LootTabCustomMoneyMade.Format(moneyMade));

        ImGuiHelpers.ScaledDummy(3.0f);

        if (ImGui.CollapsingHeader(Language.TermsOptions))
        {
            using var indent = ImRaii.PushIndent(10.0f);

            ImGui.AlignTextToFramePadding();
            Helper.TextColored(ImGuiColors.DalamudViolet, Language.LootTabEntryFixed);
            ImGui.SameLine();
            using (var lootCombo = ImRaii.Combo("##lootOptionCombo", Plugin.Configuration.DateLimit.GetName()))
            {
                if (lootCombo.Success)
                {
                    foreach (var dateLimit in Enum.GetValues<DateLimit>())
                    {
                        if (ImGui.Selectable(dateLimit.GetName()))
                        {
                            LastRefreshTime = 0;

                            Plugin.Configuration.DateLimit = dateLimit;
                            Plugin.Configuration.Save();
                        }
                    }
                }
            }
            ImGuiComponents.HelpMarker(Language.LootTabTooltipFixed);

            if (Plugin.Configuration.DateLimit == DateLimit.None)
            {
                ImGui.AlignTextToFramePadding();
                Helper.TextColored(ImGuiColors.DalamudViolet, Language.LootTabEntryFromToDateSelection);
                ImGui.SameLine();

                var changed = false;
                changed |= DateWidget.DatePickerWithInput("FromDate", 1, ref CustomMinString, ref CustomMinDate, Format);
                changed |= DateWidget.DatePickerWithInput("ToDate", 2, ref CustomMaxString, ref CustomMaxDate, Format, true);

                if (changed)
                    LastRefreshTime = 0;

                ImGui.SameLine(0, ImGui.GetStyle().ItemSpacing.X);
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Recycle))
                    CustomReset();

                if (DateWidget.Validate(CustomMinimalDate, ref CustomMinDate, ref CustomMaxDate))
                    CustomRefresh();
            }
        }

        ImGuiHelpers.ScaledDummy(5.0f);

        ContentHeight = ImGui.GetCursorPos().Y - pos.Y;
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
            // 0 and 1 are used separately
            if (FcSelection > 1 && Plugin.GetManagedFCOrDefault(FcSelection - 2).Id != id)
                continue;

            // Removes Hidden FCs
            if (FcSelection == 1 && Plugin.Configuration.ManagedFCs.FirstOrDefault(status => status.Id == id).Hidden)
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
