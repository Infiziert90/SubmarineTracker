using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Lumina.Excel.Sheets;
using SubmarineTracker.Data;
using SubmarineTracker.Resources;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow
{
    public readonly HashSet<SubmarineExploration> MustInclude = [];

    private Voyage.BestRoute BestRoute = Voyage.BestRoute.Empty;
    private bool ComputingPath;
    private Build.RouteBuild LastComputedBuild;
    private DateTime ComputeStart = DateTime.Now;

    private bool Calculate;

    private int LastSeenMap;
    private int LastSeenRank;
    private bool OptionsChanged;

    private bool IgnoreUnlocks;
    private bool AvgBonus;

    public ExcelSheetSelector<SubmarineExploration>.ExcelSheetPopupOptions? ExplorationPopupOptions;

    private void ExpTab()
    {
        using var tabItem = ImRaii.TabItem($"{Language.BuilderTabBestExp}##BestExp");
        if (!tabItem.Success)
            return;

        using var child = ImRaii.Child("ExpSelector", new Vector2(0, -(170 * ImGuiHelpers.GlobalScale)));
        if (!child.Success)
            return;

        if (!Plugin.DatabaseCache.GetFreeCompanies().TryGetValue(Plugin.GetFCId, out var fcSub))
        {
            Helper.NoData();
            return;
        }

        using (var pathChild = ImRaii.Child("BestPath", new Vector2(0, 170 * ImGuiHelpers.GlobalScale)))
        {
            if (pathChild.Success)
            {
                var maps = Sheets.ExplorationSheet
                             .Where(r => r.StartingPoint)
                             .Select(r => Sheets.ExplorationSheet.GetRow(r.RowId + 1))
                             .Where(r => r.RankReq <= CurrentBuild.Rank)
                             .Where(r => IgnoreUnlocks || fcSub.UnlockedSectors[r.RowId])
                             .Select(r => r.Map.Value.Name.ExtractText())
                             .ToArray();

                // Always pick highest rank map if smaller than possible
                if (maps.Length <= CurrentBuild.Map)
                {
                    CurrentBuild.Map = maps.Length - 1;
                    Reset(CurrentBuild.Map);
                }

                var selectedMap = CurrentBuild.Map;
                Helper.DrawComboWithArrows("##mapSelection", ref selectedMap, ref maps);

                var mapChanged = selectedMap != CurrentBuild.Map;
                if (mapChanged)
                {
                    Reset(selectedMap);
                    CurrentBuild.Map = selectedMap;
                }

                var beginCalculation = false;
                if (Plugin.Configuration.CalculateOnInteraction)
                {
                    if (Calculate)
                        beginCalculation = true;
                }
                else if (mapChanged || !LastComputedBuild.SameBuild(CurrentBuild) || OptionsChanged)
                {
                    beginCalculation = true;
                }

                if (beginCalculation && !ComputingPath)
                {
                    // Don't set it false until we sure it got begins calculation
                    OptionsChanged = false;

                    BestRoute = Voyage.BestRoute.Empty;
                    ComputeStart = DateTime.Now;
                    ComputingPath = true;
                    Task.Run(() =>
                    {
                        LastComputedBuild = new Build.RouteBuild(CurrentBuild);
                        Calculate = false;

                        var mustInclude = MustInclude.Select(s => s.RowId).ToArray();
                        var unlocked = fcSub.UnlockedSectors.Where(pair => pair.Value).Select(pair => pair.Key).ToArray();
                        var path = Voyage.FindBestRoute(CurrentBuild, unlocked, mustInclude, [], IgnoreUnlocks, AvgBonus);
                        if (path.Path.Length == 0)
                            CurrentBuild.NotOptimized();

                        ComputingPath = false;
                        BestRoute = path;
                    });
                }

                var height = ImGui.GetTextLineHeight() * 6.5f; // 5 items max, we give padding space for 6.5
                using (var listBox = ImRaii.ListBox("##bestPoints", new Vector2(-1, height)))
                {
                    if (listBox.Success)
                    {
                        if (ComputingPath)
                            ImGui.TextUnformatted($"{Language.TermsLoading} {new string('.', (int)((DateTime.Now - ComputeStart).TotalMilliseconds / 500) % 5)}");

                        if (BestRoute.Path.Length != 0)
                        {
                            var startPoint = Voyage.FindVoyageStart(BestRoute.Path[0]);
                            foreach (var location in Voyage.ToExplorationArray(BestRoute.Path))
                                if (location.RowId > startPoint)
                                    ImGui.TextUnformatted($"{NumToLetter(location.RowId - startPoint)}. {UpperCaseStr(location.Destination)}");

                            CurrentBuild.UpdateOptimized(BestRoute);
                        }
                        else
                        {
                            ImGui.TextUnformatted(Plugin.Configuration.CalculateOnInteraction && !Calculate ? Language.BestEXPCalculationManualCalculation : Language.BestEXPCalculationNothingFound);
                        }
                    }
                }

                if (Plugin.Configuration.CalculateOnInteraction)
                {
                    if (ImGui.Button(Language.BestEXPCalculationCalculate))
                    {
                        Calculate = true;
                        BestRoute = Voyage.BestRoute.Empty;
                    }
                }
            }
        }

        using (var expChild = ImRaii.Child("ExpOptions", Vector2.Zero))
        {
            if (expChild.Success)
            {
                var width = ImGui.GetContentRegionAvail().X / 3;
                var length = ImGui.CalcTextSize($"{Language.TermsMustInclude} 5 / 5").X + 25.0f;

                Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabEntryOptions);

                using (ImRaii.PushIndent(10.0f))
                {
                    OptionsChanged |= ImGui.Checkbox(Language.BestEXPCheckboxNoAutomatic, ref Plugin.Configuration.CalculateOnInteraction);
                    OptionsChanged |= ImGui.Checkbox(Language.BestEXPCheckboxIgnoreUnlocks, ref IgnoreUnlocks);
                    OptionsChanged |= ImGui.Checkbox(Language.BestEXPCheckboxAvgBonus, ref AvgBonus);
                    ImGuiComponents.HelpMarker(Language.BestEXPTooltipAvgBonus);
                    if (Plugin.Configuration.DurationLimit != DurationLimit.None)
                        OptionsChanged |= ImGui.Checkbox(Language.BestEXPCheckboxMaximizeDuration, ref Plugin.Configuration.MaximizeDuration);
                }

                ImGui.AlignTextToFramePadding();
                Helper.TextColored(ImGuiColors.DalamudViolet, Language.BestEXPEntryDurationLimit);

                ImGui.SameLine(length);

                ImGui.SetNextItemWidth(width);
                using (var combo = ImRaii.Combo("##durationLimitCombo", Plugin.Configuration.DurationLimit.GetName()))
                {
                    if (combo.Success)
                    {
                        foreach (var durationLimit in Enum.GetValues<DurationLimit>())
                        {
                            if (ImGui.Selectable(durationLimit.GetName()))
                            {
                                Plugin.Configuration.DurationLimit = durationLimit;
                                Plugin.Configuration.Save();

                                OptionsChanged = true;
                            }
                        }
                    }
                }

                if (Plugin.Configuration.DurationLimit == DurationLimit.Custom)
                {
                    ImGui.AlignTextToFramePadding();
                    Helper.TextColored(ImGuiColors.DalamudViolet, Language.TermsCustom);
                    ImGui.SameLine(length);

                    ImGui.SetNextItemWidth(width / 5.0f);
                    if (ImGui.InputInt("##CustomHourInput", ref Plugin.Configuration.CustomHour, 0))
                    {
                        Plugin.Configuration.CustomHour = Math.Clamp(Plugin.Configuration.CustomHour, 1, 123);
                        Plugin.Configuration.Save();
                    }
                    ImGui.SameLine();
                    ImGui.TextUnformatted(":");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(width / 5.0f);
                    if (ImGui.InputInt("##CustomMinInput", ref Plugin.Configuration.CustomMinute, 0))
                    {
                        Plugin.Configuration.CustomMinute = Math.Clamp(Plugin.Configuration.CustomMinute, 0, 59);
                        Plugin.Configuration.Save();
                    }
                    ImGui.SameLine();
                    ImGui.TextUnformatted(Language.BestEXPEntryHoursandMinutes);
                }


                Helper.TextColored(ImGuiColors.DalamudViolet, $"{Language.TermsMustInclude} {MustInclude.Count} / 5");

                var listHeight = ImGui.GetTextLineHeight() * 6.5f; // 5 items max, we give padding space for 6.5
                using (ImRaii.Disabled(MustInclude.Count >= 5))
                using (ImRaii.PushFont(UiBuilder.IconFont))
                    ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), new Vector2(30.0f * ImGuiHelpers.GlobalScale, listHeight));

                // Reset to refresh the internal state
                if (LastSeenMap != CurrentBuild.Map || LastSeenRank != CurrentBuild.Rank)
                {
                    MustInclude.Clear();
                    ExplorationPopupOptions = null;
                }

                LastSeenMap = CurrentBuild.Map;
                LastSeenRank = CurrentBuild.Rank;

                var startPoint = Voyage.FindStartFromMap(CurrentBuild.MapRowId).RowId;
                var error = !fcSub.UnlockedSectors.ContainsKey(startPoint);
                if (ExplorationPopupOptions == null)
                {
                    ExcelSheetSelector<SubmarineExploration>.FilteredSearchSheet = null!;
                    ExplorationPopupOptions = new()
                    {
                        FormatRow = e => $"{NumToLetter(e.RowId, true)}. {UpperCaseStr(e.Destination)} ({Language.TermsRank} {e.RankReq})",
                        FilteredSheet = Sheets.ExplorationSheet.Where(r => !error && !r.StartingPoint && r.Map.RowId == CurrentBuild.MapRowId && (IgnoreUnlocks || fcSub.UnlockedSectors[r.RowId]) && r.RankReq <= CurrentBuild.Rank)
                    };
                }

                if (ExcelSheetSelector<SubmarineExploration>.ExcelSheetPopup("ExplorationAddPopup", out var row, ExplorationPopupOptions, error || MustInclude.Count >= 5))
                {
                    var point = Sheets.ExplorationSheet.GetRow(row);
                    if (MustInclude.Add(point))
                        OptionsChanged = true;
                }

                ImGui.SameLine();

                using (var listBox = ImRaii.ListBox("##MustIncludePoints", new Vector2(-1, listHeight)))
                {
                    if (listBox.Success)
                    {
                        foreach (var p in MustInclude.ToArray())
                        {
                            if (ImGui.Selectable($"{NumToLetter(p.RowId - startPoint)}. {UpperCaseStr(p.Destination)}"))
                            {
                                MustInclude.Remove(p);
                                OptionsChanged = true;
                            }
                        }
                    }
                }

                if (OptionsChanged)
                    Plugin.Configuration.Save();
            }
        }
    }

    private void Reset(int newMap)
    {
        ComputingPath = false;
        BestRoute = Voyage.BestRoute.Empty;
        MustInclude.Clear();
        CurrentBuild.ChangeMap(newMap);
    }
}
