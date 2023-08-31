using Dalamud.Interface.Components;
using Lumina.Excel.GeneratedSheets;
using SubmarineTracker.Data;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow
{
    public static List<Build.SubmarineBuild> AllBuilds = new();
    public int SelectedRank;
    private SubmarineRank Rank = null!;
    private const int PartsCount = 10;
    private TargetValues Target;
    private TargetValues LockedTarget;

    private bool IgnoreBreakpoints = false;

    public void InitializeShip()
    {
        AllBuilds.Clear();

        Rank = RankSheet.Last();
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
            distance = (uint) CurrentBuild.OptimizedDistance;

        var builds = AllBuilds.Where(b => SelectedRank >= b.HighestRankPart() && b.Range >= distance && b.BuildCost <= Rank.Capacity).Where(hasRoute && !IgnoreBreakpoints ? Target.GetSectorFilter(CurrentBuild.Sectors) : Target.GetFilter()).Select(t => new Tuple<Build.SubmarineBuild, TimeSpan>(t, new TimeSpan(12, 0, 0)));
        if (hasRoute)
        {
            builds = builds.Select(tuple =>
            {
                var (build, _) = tuple;
                var route = CurrentBuild.OptimizedRoute.ToArray();
                var start = ExplorationSheet.First(t => t.Map.Row == route.First().Map.Row);

                return new Tuple<Build.SubmarineBuild, TimeSpan>(build, TimeSpan.FromSeconds(Voyage.CalculateDuration(route.Prepend(start), build)));
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
        var open = ImGui.BeginTabItem("Ship");
        if (open)
        {
            if (ImGui.SliderInt("##shipSliderRank", ref SelectedRank, 1, (int)RankSheet.Last().RowId, "Rank %d"))
            {
                Rank = RankSheet.ElementAt(SelectedRank - 1);
                RefreshList();
            }

            ImGui.SameLine();
            ImGui.Checkbox("Ignore Breakpoints", ref IgnoreBreakpoints);

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Selected Route:");
            SelectedRoute();

            ImGuiHelpers.ScaledDummy(5.0f);

            var hasRoute = CurrentBuild.Sectors.Count > 0;
            if (!hasRoute || IgnoreBreakpoints)
            {
                if (ImGui.CollapsingHeader("Stats"))
                {
                    var textWidth = ImGui.CalcTextSize("Surveillance:").X + (15.0f * ImGuiHelpers.GlobalScale);
                    var sliderWidth = ImGui.GetWindowWidth() / 3;

                    ImGui.Text("Surveillance:");
                    ImGui.SameLine(textWidth);
                    ImGui.PushItemWidth(sliderWidth);
                    if (ImGui.SliderInt("##shipSliderMinSurveillance", ref Target.MinSurveillance, LockedTarget.MinSurveillance, LockedTarget.MaxSurveillance, "Min %d"))
                    {
                        Target.MaxSurveillance = Math.Max(Target.MinSurveillance, Target.MaxSurveillance);
                    }

                    ImGui.SameLine();
                    if (ImGui.SliderInt("##shipSliderMaxSurveillance", ref Target.MaxSurveillance, LockedTarget.MinSurveillance, LockedTarget.MaxSurveillance, "Max %d"))
                    {
                        Target.MinSurveillance = Math.Min(Target.MinSurveillance, Target.MaxSurveillance);
                    }
                    ImGui.PopItemWidth();

                    ImGui.Text("Retrieval:");
                    ImGui.SameLine(textWidth);
                    ImGui.PushItemWidth(sliderWidth);
                    if (ImGui.SliderInt("##shipSliderMinRetrieval", ref Target.MinRetrieval, LockedTarget.MinRetrieval, LockedTarget.MaxRetrieval, "Min %d"))
                    {
                        Target.MaxRetrieval = Math.Max(Target.MinRetrieval, Target.MaxRetrieval);
                    }

                    ImGui.SameLine();
                    if (ImGui.SliderInt("##shipSliderMaxRetrieval", ref Target.MaxRetrieval, LockedTarget.MinRetrieval, LockedTarget.MaxRetrieval, "Max %d"))
                    {
                        Target.MinRetrieval = Math.Min(Target.MinRetrieval, Target.MaxRetrieval);
                    }
                    ImGui.PopItemWidth();

                    ImGui.Text("Favor:");
                    ImGui.SameLine(textWidth);
                    ImGui.PushItemWidth(sliderWidth);
                    if (ImGui.SliderInt("##shipSliderMinFavor", ref Target.MinFavor, LockedTarget.MinFavor, LockedTarget.MaxFavor, "Min %d"))
                    {
                        Target.MaxFavor = Math.Max(Target.MinFavor, Target.MaxFavor);
                    }

                    ImGui.SameLine();
                    if (ImGui.SliderInt("##shipSliderMaxFavor", ref Target.MaxFavor, LockedTarget.MinFavor, LockedTarget.MaxFavor, "Max %d"))
                    {
                        Target.MinFavor = Math.Min(Target.MinFavor, Target.MaxFavor);
                    }
                    ImGui.PopItemWidth();

                    ImGui.Text("Speed:");
                    ImGui.SameLine(textWidth);
                    ImGui.PushItemWidth(sliderWidth);
                    if (ImGui.SliderInt("##shipSliderMinSpeed", ref Target.MinSpeed, LockedTarget.MinSpeed, LockedTarget.MaxSpeed, "Min %d"))
                    {
                        Target.MaxSpeed = Math.Max(Target.MinSpeed, Target.MaxSpeed);
                    }

                    ImGui.SameLine();
                    if (ImGui.SliderInt("##shipSliderMaxSpeed", ref Target.MaxSpeed, LockedTarget.MinSpeed, LockedTarget.MaxSpeed, "Max %d"))
                    {
                        Target.MinSpeed = Math.Min(Target.MinSpeed, Target.MaxSpeed);
                    }
                    ImGui.PopItemWidth();
                }

                ImGuiHelpers.ScaledDummy(10.0f);
            }
            else
            {
                var secondRow = ImGui.GetWindowWidth() / 5.1f;

                var breakpoints = Sectors.CalculateBreakpoint(CurrentBuild.Sectors);

                ImGui.TextColored(ImGuiColors.DalamudViolet, "Breakpoints:");
                ImGui.TextColored(ImGuiColors.HealerGreen, $"Surveillance");
                ImGui.SameLine(secondRow);
                ImGui.TextUnformatted($"T2: {breakpoints.T2} | T3: {breakpoints.T3}");

                ImGui.TextColored(ImGuiColors.HealerGreen, $"Retrieval");
                ImGui.SameLine(secondRow);
                ImGui.TextUnformatted($"Normal: {breakpoints.Normal} | Opti: {breakpoints.Optimal}");

                ImGui.TextColored(ImGuiColors.HealerGreen, $"Favor");
                ImGui.SameLine(secondRow);
                ImGui.TextUnformatted($"Favor: {breakpoints.Favor}");

                ImGui.TextColored(ImGuiColors.DalamudViolet, "Options:");

                ImGui.Checkbox("Use T2", ref Target.UseT2);
                ImGui.SameLine();
                ImGui.Checkbox("Use Normal", ref Target.UseNormal);
                ImGui.SameLine();
                ImGui.Checkbox("Ignore Favor", ref Target.IgnoreFavor);
                ImGui.SameLine();
                ImGui.Checkbox("No Modded Parts", ref Target.NoModded);

                ImGuiHelpers.ScaledDummy(10.0f);
            }

            if (!FilterBuilds().Any())
            {
                ImGuiHelpers.ScaledDummy(20.0f);

                var width = ImGui.GetWindowSize().X;
                var textWidth   = ImGui.CalcTextSize("No build found.").X;

                ImGui.SetCursorPosX((width - textWidth) * 0.5f);
                ImGui.TextColored(ImGuiColors.DalamudOrange, "No build found.");
                ImGui.EndTabItem();
                return true;
            }

            if (ImGui.BeginTable("##shipTable", hasRoute ? 13 : 12, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable))
            {
                ImGui.TableSetupColumn("Cost");
                ImGui.TableSetupColumn("Repair");
                ImGui.TableSetupColumn("Hull", ImGuiTableColumnFlags.NoSort);
                ImGui.TableSetupColumn("Stern", ImGuiTableColumnFlags.NoSort);
                ImGui.TableSetupColumn("Bow", ImGuiTableColumnFlags.NoSort);
                ImGui.TableSetupColumn("Bridge", ImGuiTableColumnFlags.NoSort);
                ImGui.TableSetupColumn("Surveillance", ImGuiTableColumnFlags.PreferSortDescending);
                ImGui.TableSetupColumn("Retrieval", ImGuiTableColumnFlags.PreferSortDescending);
                ImGui.TableSetupColumn("Favor", ImGuiTableColumnFlags.PreferSortDescending);
                ImGui.TableSetupColumn("Speed", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.PreferSortDescending);
                ImGui.TableSetupColumn("Range", ImGuiTableColumnFlags.PreferSortDescending);
                if (hasRoute)
                    ImGui.TableSetupColumn("Duration", ImGuiTableColumnFlags.NoSort);
                ImGui.TableSetupColumn("##Import", ImGuiTableColumnFlags.NoSort);
                ImGui.TableHeadersRow();

                var tableContent = SortBuilds(ImGui.TableGetSortSpecs().Specs).ToArray();
                using (var clipper = new ListClipper(tableContent.Length, itemHeight: ImGui.CalcTextSize("W").Y * 1.1f))
                {
                    foreach (var i in clipper.Rows)
                    {
                        var (build, time) = tableContent[i];
                        ImGui.TableNextColumn();
                        ImGui.Text($"{build.BuildCost}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{build.RepairCosts}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{build.HullIdentifier}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{build.SternIdentifier}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{build.BowIdentifier}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{build.BridgeIdentifier}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{build.Surveillance}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{build.Retrieval}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{build.Favor}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{build.Speed}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{build.Range}");
                        if (hasRoute)
                        {
                            ImGui.TableNextColumn();
                            ImGui.Text($"{ToTime(time)}");
                        }

                        ImGui.TableNextColumn();
                        if (ImGuiComponents.IconButton(i, FontAwesomeIcon.ArrowRightFromBracket))
                        {
                            CurrentBuild.UpdateBuild(build, SelectedRank);
                            CurrentBuild.OriginalSub = 0;
                        }

                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip("Import this build");

                        ImGui.TableNextRow();
                    }
                }

                ImGui.EndTable();
            }

            ImGui.EndTabItem();
        }

        return open;
    }

    struct TargetValues
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
