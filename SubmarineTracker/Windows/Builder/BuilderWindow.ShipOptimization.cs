using Dalamud.Interface;
using Dalamud.Interface.Components;
using Lumina.Excel.Sheets;
using SubmarineTracker.Data;
using SubmarineTracker.Resources;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow
{
    public static List<Build.SubmarineBuild> AllBuilds = [];
    public int SelectedRank;
    private SubmarineRank Rank;
    private const int PartsCount = 10;
    private TargetValues Target;
    private TargetValues LockedTarget;

    private bool IgnoreBreakpoints;

    public void InitializeShip()
    {
        AllBuilds.Clear();

        Rank = Sheets.RankSheet.GetRow(Sheets.LastRank);
        SelectedRank = (int) Rank.RowId;

        for (var hull = 0; hull < PartsCount; hull++)
        {
            for (var stern = 0; stern < PartsCount; stern++)
            {
                for (var bow = 0; bow < PartsCount; bow++)
                {
                    for (var bridge = 0; bridge < PartsCount; bridge++)
                    {
                        var build = new Build.SubmarineBuild(SelectedRank, (hull * 4) + 3, (stern * 4) + 4, (bow * 4) + 1, (bridge * 4) + 2);
                        AllBuilds.Add(build);
                    }
                }
            }
        }

        LockedTarget = new TargetValues(AllBuilds);
        Target = new TargetValues(LockedTarget);
    }

    public void RefreshList()
    {
        var newList = new List<Build.SubmarineBuild>();
        for (var hull = 0; hull < PartsCount; hull++)
        {
            for (var stern = 0; stern < PartsCount; stern++)
            {
                for (var bow = 0; bow < PartsCount; bow++)
                {
                    for (var bridge = 0; bridge < PartsCount; bridge++)
                    {
                        var build = new Build.SubmarineBuild(SelectedRank, (hull * 4) + 3, (stern * 4) + 4, (bow * 4) + 1, (bridge * 4) + 2);
                        newList.Add(build);
                    }
                }
            }
        }

        AllBuilds = newList;
        LockedTarget = new TargetValues(AllBuilds);
    }

    public IEnumerable<Tuple<Build.SubmarineBuild, TimeSpan>> FilterBuilds()
    {
        uint distance = 0;
        var hasRoute = CurrentBuild.Sectors.Count > 0;
        if (hasRoute)
            distance = CurrentBuild.OptimizedDistance;

        var builds = AllBuilds.Where(b => SelectedRank >= b.HighestRankPart() && b.Range >= distance && b.BuildCost <= Rank.Capacity).Where(hasRoute && !IgnoreBreakpoints ? Target.GetSectorFilter(CurrentBuild.Sectors) : Target.GetFilter()).Select(t => new Tuple<Build.SubmarineBuild, TimeSpan>(t, new TimeSpan(12, 0, 0)));
        if (hasRoute)
        {
            builds = builds.Select(tuple =>
            {
                var (build, _) = tuple;
                return new Tuple<Build.SubmarineBuild, TimeSpan>(build, TimeSpan.FromSeconds(Voyage.CalculateDuration(CurrentBuild.OptimizedRoute, build.Speed)));
            });
        }

        return builds;
    }

    public IEnumerable<Tuple<Build.SubmarineBuild, TimeSpan>> SortBuilds(ImGuiTableColumnSortSpecsPtr sortSpecsPtr)
    {
        Func<Tuple<Build.SubmarineBuild, TimeSpan>, int> sortFunc = sortSpecsPtr.ColumnIndex switch
        {
            0 => x => x.Item1.BuildCost,
            1 => x => x.Item1.RepairCosts,
            2 => x => (int)x.Item1.Hull.RowId,
            3 => x => (int)x.Item1.Stern.RowId,
            4 => x => (int)x.Item1.Bow.RowId,
            5 => x => (int)x.Item1.Bridge.RowId,
            6 => x => x.Item1.Surveillance,
            7 => x => x.Item1.Retrieval,
            8 => x => x.Item1.Favor,
            9 => x => x.Item1.Speed,
            10 => x => x.Item1.Range,
            _ => _ => 0
        };

        return sortSpecsPtr.SortDirection switch
        {
            ImGuiSortDirection.Ascending => FilterBuilds().OrderBy(sortFunc),
            ImGuiSortDirection.Descending => FilterBuilds().OrderByDescending(sortFunc),
            _ => FilterBuilds()
        };
    }

    public bool ShipTab()
    {
        using var tabItem = ImRaii.TabItem($"{Language.BuilderTabShip}##Ship");
        if (!tabItem.Success)
            return false;

        if (ImGui.SliderInt("##shipSliderRank", ref SelectedRank, 1, (int) Sheets.LastRank, $"{Language.TermsRank} %d"))
        {
            Rank = Sheets.RankSheet.GetRow((uint) SelectedRank);
            RefreshList();
        }

        ImGui.SameLine();
        ImGui.Checkbox(Language.BuilderShipCheckboxIgnoreBreakpoints, ref IgnoreBreakpoints);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.BuilderShipHeaderRoute);
        SelectedRoute();

        ImGuiHelpers.ScaledDummy(5.0f);

        var hasRoute = CurrentBuild.Sectors.Count > 0;
        if (!hasRoute || IgnoreBreakpoints)
        {
            if (ImGui.CollapsingHeader(Language.TermsStats))
            {
                var textWidth = ImGui.CalcTextSize("Surveillance:").X + (15.0f * ImGuiHelpers.GlobalScale);
                var sliderWidth = ImGui.GetWindowWidth() / 3;

                ImGui.TextUnformatted($"{Language.TermsSurveillance}:");
                ImGui.SameLine(textWidth);
                using (ImRaii.ItemWidth(sliderWidth))
                {
                    if (ImGui.SliderInt("##shipSliderMinSurveillance", ref Target.MinSurveillance, LockedTarget.MinSurveillance, LockedTarget.MaxSurveillance, "Min %d"))
                        Target.MaxSurveillance = Math.Max(Target.MinSurveillance, Target.MaxSurveillance);

                    ImGui.SameLine();

                    if (ImGui.SliderInt("##shipSliderMaxSurveillance", ref Target.MaxSurveillance, LockedTarget.MinSurveillance, LockedTarget.MaxSurveillance, "Max %d"))
                        Target.MinSurveillance = Math.Min(Target.MinSurveillance, Target.MaxSurveillance);
                }

                ImGui.TextUnformatted($"{Language.TermsRetrieval}:");
                ImGui.SameLine(textWidth);
                using (ImRaii.ItemWidth(sliderWidth))
                {
                    if (ImGui.SliderInt("##shipSliderMinRetrieval", ref Target.MinRetrieval, LockedTarget.MinRetrieval, LockedTarget.MaxRetrieval, "Min %d"))
                        Target.MaxRetrieval = Math.Max(Target.MinRetrieval, Target.MaxRetrieval);

                    ImGui.SameLine();

                    if (ImGui.SliderInt("##shipSliderMaxRetrieval", ref Target.MaxRetrieval, LockedTarget.MinRetrieval, LockedTarget.MaxRetrieval, "Max %d"))
                        Target.MinRetrieval = Math.Min(Target.MinRetrieval, Target.MaxRetrieval);
                }

                ImGui.TextUnformatted($"{Language.TermsFavor}:");
                ImGui.SameLine(textWidth);
                using (ImRaii.ItemWidth(sliderWidth))
                {
                    if (ImGui.SliderInt("##shipSliderMinFavor", ref Target.MinFavor, LockedTarget.MinFavor, LockedTarget.MaxFavor, "Min %d"))
                        Target.MaxFavor = Math.Max(Target.MinFavor, Target.MaxFavor);

                    ImGui.SameLine();

                    if (ImGui.SliderInt("##shipSliderMaxFavor", ref Target.MaxFavor, LockedTarget.MinFavor, LockedTarget.MaxFavor, "Max %d"))
                        Target.MinFavor = Math.Min(Target.MinFavor, Target.MaxFavor);
                }

                ImGui.TextUnformatted($"{Language.TermsSpeed}:");
                ImGui.SameLine(textWidth);
                using (ImRaii.ItemWidth(sliderWidth))
                {
                    if (ImGui.SliderInt("##shipSliderMinSpeed", ref Target.MinSpeed, LockedTarget.MinSpeed, LockedTarget.MaxSpeed, "Min %d"))
                        Target.MaxSpeed = Math.Max(Target.MinSpeed, Target.MaxSpeed);

                    ImGui.SameLine();

                    if (ImGui.SliderInt("##shipSliderMaxSpeed", ref Target.MaxSpeed, LockedTarget.MinSpeed, LockedTarget.MaxSpeed, "Max %d"))
                        Target.MinSpeed = Math.Min(Target.MinSpeed, Target.MaxSpeed);
                }
            }

            ImGuiHelpers.ScaledDummy(10.0f);
        }
        else
        {
            var secondRow = ImGui.GetWindowWidth() / 5.1f;

            var breakpoints = Sectors.CalculateBreakpoint(CurrentBuild.Sectors);

            Helper.TextColored(ImGuiColors.DalamudViolet, $"{Language.TermsBreakpoints}:");
            Helper.TextColored(ImGuiColors.HealerGreen, Language.TermsSurveillance);
            ImGui.SameLine(secondRow);
            ImGui.TextUnformatted($"T2: {breakpoints.T2} | T3: {breakpoints.T3}");

            Helper.TextColored(ImGuiColors.HealerGreen, Language.TermsRetrieval);
            ImGui.SameLine(secondRow);
            ImGui.TextUnformatted($"{Language.TermsNormal}: {breakpoints.Normal} | {Language.TermsOptimal}: {breakpoints.Optimal}");

            Helper.TextColored(ImGuiColors.HealerGreen, Language.TermsFavor);
            ImGui.SameLine(secondRow);
            ImGui.TextUnformatted($"{Language.TermsFavor}: {breakpoints.Favor}");

            Helper.TextColored(ImGuiColors.DalamudViolet, $"{Language.TermsOptions}:");

            ImGui.Checkbox(Language.BuilderShipCheckboxT2, ref Target.UseT2);
            ImGui.SameLine();
            ImGui.Checkbox(Language.BuilderShipCheckboxNormal, ref Target.UseNormal);
            ImGui.SameLine();
            ImGui.Checkbox(Language.BuilderShipCheckboxFavor, ref Target.IgnoreFavor);
            ImGui.SameLine();
            ImGui.Checkbox(Language.BuilderShipCheckboxModded, ref Target.NoModded);

            ImGuiHelpers.ScaledDummy(10.0f);
        }

        if (!FilterBuilds().Any())
        {
            ImGuiHelpers.ScaledDummy(20.0f);

            var text = Language.BuilderShipCalculationNothingFound;

            ImGui.SetCursorPosX((ImGui.GetWindowSize().X - ImGui.CalcTextSize(text).X) * 0.5f);
            Helper.TextColored(ImGuiColors.DalamudOrange, text);
            return true;
        }

        using var table = ImRaii.Table("##shipTable", hasRoute ? 13 : 12, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable);
        if (!table.Success)
            return true;

        ImGui.TableSetupColumn(Language.TermsCost);
        ImGui.TableSetupColumn(Language.TermsRepair);
        ImGui.TableSetupColumn(Language.TermsHull, ImGuiTableColumnFlags.NoSort);
        ImGui.TableSetupColumn(Language.TermsStern, ImGuiTableColumnFlags.NoSort);
        ImGui.TableSetupColumn(Language.TermsBow, ImGuiTableColumnFlags.NoSort);
        ImGui.TableSetupColumn(Language.TermsBridge, ImGuiTableColumnFlags.NoSort);
        ImGui.TableSetupColumn(Language.TermsSurveillance, ImGuiTableColumnFlags.PreferSortDescending);
        ImGui.TableSetupColumn(Language.TermsRetrieval, ImGuiTableColumnFlags.PreferSortDescending);
        ImGui.TableSetupColumn(Language.TermsFavor, ImGuiTableColumnFlags.PreferSortDescending);
        ImGui.TableSetupColumn(Language.TermsSpeed, ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.PreferSortDescending);
        ImGui.TableSetupColumn(Language.TermsRange, ImGuiTableColumnFlags.PreferSortDescending);
        if (hasRoute)
            ImGui.TableSetupColumn(Language.TermsDuration, ImGuiTableColumnFlags.NoSort);
        ImGui.TableSetupColumn("##Import", ImGuiTableColumnFlags.NoSort);

        ImGui.TableHeadersRow();
        var tableContent = SortBuilds(ImGui.TableGetSortSpecs().Specs).ToArray();

        using var clipper = new ListClipper(tableContent.Length, itemHeight: ImGui.GetTextLineHeight() * 1.1f);
        foreach (var i in clipper.Rows)
        {
            var (build, time) = tableContent[i];
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{build.BuildCost}");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{build.RepairCosts}");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{build.HullIdentifier}");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{build.SternIdentifier}");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{build.BowIdentifier}");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{build.BridgeIdentifier}");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{build.Surveillance}");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{build.Retrieval}");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{build.Favor}");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{build.Speed}");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{build.Range}");

            if (hasRoute)
            {
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(ToTime(time));
            }

            ImGui.TableNextColumn();
            if (ImGuiComponents.IconButton(i, FontAwesomeIcon.ArrowRightFromBracket))
            {
                CurrentBuild.UpdateBuild(build, SelectedRank);
                CurrentBuild.OriginalSub = 0;
            }

            if (ImGui.IsItemHovered())
                Helper.Tooltip(Language.BuilderShipTableSelect);

            ImGui.TableNextRow();
        }

        return true;
    }

    private struct TargetValues
    {
        public int MinSurveillance;
        public int MinRetrieval;
        public int MinSpeed;
        public int MinRange;
        public int MinFavor;
        public int MaxSurveillance;
        public int MaxRetrieval;
        public int MaxSpeed;
        public int MaxRange;
        public int MaxFavor;

        public bool UseT2 = false;
        public bool UseNormal = false;
        public bool IgnoreFavor = false;
        public bool NoModded = false;

        public TargetValues(List<Build.SubmarineBuild> allBuilds) : this()
        {
            MinSurveillance = allBuilds.Min(x => x.Surveillance);
            MinRetrieval = allBuilds.Min(x => x.Retrieval);
            MinSpeed = allBuilds.Min(x => x.Speed);
            MinRange = allBuilds.Min(x => x.Range);
            MinFavor = allBuilds.Min(x => x.Favor);
            MaxSurveillance = allBuilds.Max(x => x.Surveillance);
            MaxRetrieval = allBuilds.Max(x => x.Retrieval);
            MaxSpeed = allBuilds.Max(x => x.Speed);
            MaxRange = allBuilds.Max(x => x.Range);
            MaxFavor = allBuilds.Max(x => x.Favor);
        }

        public TargetValues(TargetValues lockedTarget)
        {
            MinSurveillance = lockedTarget.MinSurveillance;
            MinRetrieval = lockedTarget.MinRetrieval;
            MinSpeed = lockedTarget.MinSpeed;
            MinRange = lockedTarget.MinRange;
            MinFavor = lockedTarget.MinFavor;
            MaxSurveillance = lockedTarget.MaxSurveillance;
            MaxRetrieval = lockedTarget.MaxRetrieval;
            MaxSpeed = lockedTarget.MaxSpeed;
            MaxRange = lockedTarget.MaxRange;
            MaxFavor = lockedTarget.MaxFavor;
        }

        public Func<Build.SubmarineBuild, bool> GetFilter()
        {
            var tmpThis = this;
            return build =>
                build.Surveillance >= tmpThis.MinSurveillance &&
                build.Retrieval >= tmpThis.MinRetrieval &&
                build.Speed >= tmpThis.MinSpeed &&
                build.Range >= tmpThis.MinRange &&
                build.Favor >= tmpThis.MinFavor &&
                build.Surveillance <= tmpThis.MaxSurveillance &&
                build.Retrieval <= tmpThis.MaxRetrieval &&
                build.Speed <= tmpThis.MaxSpeed &&
                build.Range <= tmpThis.MaxRange &&
                build.Favor <= tmpThis.MaxFavor;
        }

        public Func<Build.SubmarineBuild, bool> GetSectorFilter(List<uint> path)
        {
            var breakpoints = Sectors.CalculateBreakpoint(path);
            var useT2 = this.UseT2;
            var useN = this.UseNormal;
            var ignoreF = IgnoreFavor;
            var noModded = NoModded;

            return build =>
                build.Surveillance >= (useT2 ? breakpoints.T2 : breakpoints.T3) &&
                build.Retrieval >= (useN ? breakpoints.Normal : breakpoints.Optimal) &&
                (ignoreF || build.Favor >= breakpoints.Favor) &&
                (!noModded || build.HighestRankPart() < 50);
        }
    }
}
