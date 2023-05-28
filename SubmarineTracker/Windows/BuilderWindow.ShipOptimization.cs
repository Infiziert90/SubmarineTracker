using Lumina.Excel.GeneratedSheets;
using SubmarineTracker.Data;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows;

public partial class BuilderWindow
{
    public static List<Submarines.SubmarineBuild> AllBuilds = new();
    public int SelectedRank;
    private SubmarineRank Rank;
    private const int PartsCount = 10;
    private TargetValues Target;
    private TargetValues LockedTarget;

    public void Initialize()
    {
        AllBuilds.Clear();

        Rank = RankSheet.Last();
        SelectedRank = (int)Rank.RowId;

        for (var hull = 0; hull < PartsCount; hull++)
        {
            for (var stern = 0; stern < PartsCount; stern++)
            {
                for (var bow = 0; bow < PartsCount; bow++)
                {
                    for (var bridge = 0; bridge < PartsCount; bridge++)
                    {
                        var build = new Submarines.SubmarineBuild(SelectedRank, (hull * 4) + 3, (stern * 4) + 4, (bow * 4) + 1, (bridge * 4) + 2);
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
        var newList = new List<Submarines.SubmarineBuild>();
        for (var hull = 0; hull < PartsCount; hull++)
        {
            for (var stern = 0; stern < PartsCount; stern++)
            {
                for (var bow = 0; bow < PartsCount; bow++)
                {
                    for (var bridge = 0; bridge < PartsCount; bridge++)
                    {
                        var build = new Submarines.SubmarineBuild(SelectedRank, (hull * 4) + 3, (stern * 4) + 4, (bow * 4) + 1, (bridge * 4) + 2);
                        newList.Add(build);
                    }
                }
            }
        }

        AllBuilds = newList;
    }

    public IEnumerable<Tuple<Submarines.SubmarineBuild, TimeSpan>> FilterBuilds()
    {
        uint distance = 0;
        if (CurrentBuild.Sectors.Count > 0)
        {
            var route = CurrentBuild.Sectors.Select(t => ExplorationSheet.GetRow(t)!).ToArray();
            var start = ExplorationSheet.First(t => t.Map.Row == route.First().Map.Row);
            distance += start.GetDistance(route.First()) + route.First().SurveyDistance;
            for (var i = 1; i < route.Length; i++)
            {
                distance += route[i - 1].GetDistance(route[i]) + route[i - 1].SurveyDistance;
            }
        }

        var builds = AllBuilds.Where(b => b.Range >= distance && b.BuildCost <= Rank.Capacity).Where(Target.GetFilter()).Select(t => new Tuple<Submarines.SubmarineBuild, TimeSpan>(t, new TimeSpan(12, 0, 0)));
        if (CurrentBuild.Sectors.Count > 0)
        {
            builds = builds.Select(tuple =>
            {
                var (build, time) = tuple;
                var route = CurrentBuild.Sectors.Select(t => ExplorationSheet.GetRow(t)!).ToArray();
                var start = ExplorationSheet.First(t => t.Map.Row == route.First().Map.Row);
                time = time.Add(TimeSpan.FromSeconds(start.GetSurveyTime(build.Speed) + start.GetVoyageTime(route.First(), build.Speed)));
                for (var i = 1; i < route.Length; i++)
                {
                    time = time.Add(TimeSpan.FromSeconds(route[i - 1].GetSurveyTime(build.Speed) + route[i - 1].GetVoyageTime(route[i], build.Speed)));
                }
                return new Tuple<Submarines.SubmarineBuild, TimeSpan>(build, time);
            });
        }

        return builds;
    }

    public IEnumerable<Tuple<Submarines.SubmarineBuild, TimeSpan>> SortBuilds(ImGuiTableColumnSortSpecsPtr sortSpecsPtr)
    {
        Func<Tuple<Submarines.SubmarineBuild, TimeSpan>, int> sortFunc = sortSpecsPtr.ColumnIndex switch
        {
            0 => x => x.Item1.BuildCost,
            1 => x => x.Item1.RepairCosts,
            2 => x => (int)x.Item1.Hull.RowId,
            3 => x => (int)x.Item1.Stern.RowId,
            4 => x => (int)x.Item1.Bow.RowId,
            5 => x => (int)x.Item1.Bridge.RowId,
            6 => x => x.Item1.Surveillance,
            7 => x => x.Item1.Retrieval,
            8 => x => x.Item1.Speed,
            9 => x => x.Item1.Range,
            10 => x => x.Item1.Favor,
            _ => _ => 0
        };

        return sortSpecsPtr.SortDirection switch
        {
            ImGuiSortDirection.Ascending => FilterBuilds().OrderBy(sortFunc),
            ImGuiSortDirection.Descending => FilterBuilds().OrderByDescending(sortFunc),
            _ => FilterBuilds()
        };
    }

    public void ShipTab()
    {
        if (ImGui.BeginTabItem("Ship"))
        {
            if (ImGui.SliderInt("##shipSliderRank", ref SelectedRank, 1, (int)RankSheet.Last().RowId, "Rank %d"))
            {
                Rank = RankSheet.ElementAt(SelectedRank - 1);
                RefreshList();
            }

            var windowWidth = ImGui.GetWindowWidth() / 3;

            ImGui.Text("Surveillance: ");
            ImGui.SameLine();
            ImGui.PushItemWidth(windowWidth - 3.0f);
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

            ImGui.Text("Retrieval: ");
            ImGui.SameLine();
            ImGui.PushItemWidth(windowWidth - 3.0f);
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

            ImGui.Text("Speed: ");
            ImGui.SameLine();
            ImGui.PushItemWidth(windowWidth - 3.0f);
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

            ImGui.Text("Favor: ");
            ImGui.SameLine();
            ImGui.PushItemWidth(windowWidth - 3.0f);
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

            ImGui.BeginTable("##shipTable", 12, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SortMulti | ImGuiTableFlags.Sortable);
            ImGui.TableSetupColumn("Cost");
            ImGui.TableSetupColumn("Repair");
            ImGui.TableSetupColumn("Hull", ImGuiTableColumnFlags.NoSort);
            ImGui.TableSetupColumn("Stern", ImGuiTableColumnFlags.NoSort);
            ImGui.TableSetupColumn("Bow", ImGuiTableColumnFlags.NoSort);
            ImGui.TableSetupColumn("Bridge", ImGuiTableColumnFlags.NoSort);
            ImGui.TableSetupColumn("Surveillance", ImGuiTableColumnFlags.PreferSortDescending);
            ImGui.TableSetupColumn("Retrieval", ImGuiTableColumnFlags.PreferSortDescending);
            ImGui.TableSetupColumn("Speed", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.PreferSortDescending);
            ImGui.TableSetupColumn("Range", ImGuiTableColumnFlags.PreferSortDescending);
            ImGui.TableSetupColumn("Favor", ImGuiTableColumnFlags.PreferSortDescending);
            ImGui.TableSetupColumn("Duration", ImGuiTableColumnFlags.NoSort);
            ImGui.TableHeadersRow();

            foreach (var (build, time) in SortBuilds(ImGui.TableGetSortSpecs().Specs))
            {
                ImGui.TableNextColumn();
                ImGui.Text($"{build.BuildCost}");
                ImGui.TableNextColumn();
                ImGui.Text($"{build.RepairCosts}");
                ImGui.TableNextColumn();
                ImGui.Text($"{Submarines.SectionIdToChar[build.HullCharId]}");
                ImGui.TableNextColumn();
                ImGui.Text($"{Submarines.SectionIdToChar[build.SternCharId]}");
                ImGui.TableNextColumn();
                ImGui.Text($"{Submarines.SectionIdToChar[build.BowCharId]}");
                ImGui.TableNextColumn();
                ImGui.Text($"{Submarines.SectionIdToChar[build.BridgeCharId]}");
                ImGui.TableNextColumn();
                ImGui.Text($"{build.Surveillance}");
                ImGui.TableNextColumn();
                ImGui.Text($"{build.Retrieval}");
                ImGui.TableNextColumn();
                ImGui.Text($"{build.Speed}");
                ImGui.TableNextColumn();
                ImGui.Text($"{build.Range}");
                ImGui.TableNextColumn();
                ImGui.Text($"{build.Favor}");
                ImGui.TableNextColumn();
                ImGui.Text($"{ToTime(time)}");
                ImGui.TableNextRow();
            }
            ImGui.EndTable();
            ImGui.EndTabItem();
        }
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

        public TargetValues(List<Submarines.SubmarineBuild> allBuilds) : this()
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

        public Func<Submarines.SubmarineBuild, bool> GetFilter()
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
    }
}
