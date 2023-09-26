using Dalamud.Utility;
using SubmarineTracker.Data;
using static SubmarineTracker.Data.Submarines;

namespace SubmarineTracker.Windows.Builder;

public partial class BuilderWindow
{
    public bool CacheValid;
    public uint VoyageInterfaceSelection;
    public Submarine SelectedSub = new();

    private void BuildTab(ref Submarine sub)
    {
        // Always refresh submarine if we have interface selection
        RefreshCache();

        if (ImGui.BeginTabItem($"{Loc.Localize("Builder Tab - Build", "Build")}##Build"))
        {
            if (ImGui.BeginChild("SubSelector", new Vector2(0, -(170 * ImGuiHelpers.GlobalScale))))
            {
                if (!CacheValid)
                {
                    Plugin.EnsureFCOrderSafety();
                    var existingSubs = Configuration.FCOrder
                                                    .SelectMany(id => KnownSubmarines[id].Submarines.Select(s => $"{s.Name} ({s.Build.FullIdentifier()})"))
                                                    .ToArray();
                    if (Configuration.ShowOnlyCurrentFC && KnownSubmarines.TryGetValue(Plugin.ClientState.LocalContentId, out var fcSub))
                        existingSubs = fcSub.Submarines.Select(s => $"{s.Name} ({s.Build.FullIdentifier()})").ToArray();
                    existingSubs = existingSubs.Prepend(Loc.Localize("Terms - Custom", "Custom")).ToArray();

                    if (existingSubs.Length < CurrentBuild.OriginalSub)
                        CurrentBuild.OriginalSub = 0;

                    var windowWidth = ImGui.GetWindowWidth() / 2;
                    ImGui.PushItemWidth(windowWidth - (5.0f * ImGuiHelpers.GlobalScale));
                    ImGui.Combo("##existingSubs", ref CurrentBuild.OriginalSub, existingSubs, existingSubs.Length);
                    ImGui.PopItemWidth();

                    // Calculate first so rank can be changed afterwards
                    if (existingSubs[CurrentBuild.OriginalSub] != Loc.Localize("Terms - Custom", "Custom"))
                    {
                        sub = KnownSubmarines.Values.SelectMany(fc => fc.Submarines).First(sub => $"{sub.Name} ({sub.Build.FullIdentifier()})" == existingSubs[CurrentBuild.OriginalSub]);
                        CurrentBuild.UpdateBuild(sub);
                    }

                    ImGui.SameLine();
                    ImGui.PushItemWidth(windowWidth - (3.0f * ImGuiHelpers.GlobalScale));
                    ImGui.SliderInt("##SliderRank", ref CurrentBuild.Rank, 1, (int) RankSheet.Last().RowId, $"{Loc.Localize("Terms - Rank", "Rank")} %d");
                    ImGui.PopItemWidth();
                }
                else
                {
                    ImGui.TextColored(ImGuiColors.HealerGreen, Loc.Localize("Builder Build Tab - Automatic Selection", "Automatic Selection: {Name} - Rank {Rank}").Format(SelectedSub.Name, SelectedSub.Rank));

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(Loc.Localize("Builder Build Tooltip - Automatic Selection", "To disable this behaviour head into the configuration, builder tab and uncheck 'Auto Select'"));
                }

                ImGuiHelpers.ScaledDummy(5);

                if (ImGui.BeginTable("##submarinePartSelection", 5, ImGuiTableFlags.BordersInnerH))
                {
                    ImGui.TableSetupColumn("Type");
                    ImGui.TableSetupColumn("Hull");
                    ImGui.TableSetupColumn("Stern");
                    ImGui.TableSetupColumn("Bow");
                    ImGui.TableSetupColumn("Bridge");

                    ImGui.TableHeadersRow();

                    BuildTableEntries("Shark", 0);
                    BuildTableEntries("Unkiu", 4);
                    BuildTableEntries("Whale", 8);
                    BuildTableEntries("Coelac.", 12);
                    BuildTableEntries("Syldra", 16);

                    BuildTableEntries("MShark", 20);
                    BuildTableEntries("MUnkiu", 24);
                    BuildTableEntries("MWhale", 28);
                    BuildTableEntries("MCoelac.", 32);
                    BuildTableEntries("MSyldra", 36, last: true);

                    ImGui.EndTable();
                }
            }
            ImGui.EndChild();

            ImGui.EndTabItem();
        }
    }

    public void BuildTableEntries(string label, int number, bool last = false)
    {
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(label);
        ImGui.TableNextColumn();
        ImGui.RadioButton($"##{label}H", ref CurrentBuild.Hull, number + 3);
        ImGui.TableNextColumn();
        ImGui.RadioButton($"##{label}S", ref CurrentBuild.Stern, number + 4);
        ImGui.TableNextColumn();
        ImGui.RadioButton($"##{label}B", ref CurrentBuild.Bow, number + 1);
        ImGui.TableNextColumn();
        ImGui.RadioButton($"##{label}Br", ref CurrentBuild.Bridge, number + 2);

        if(!last)
            ImGui.TableNextRow();
    }

    public void RefreshCache()
    {
        // Always refresh submarine if we have interface selection
        if (VoyageInterfaceSelection != 0)
        {
            if (!KnownSubmarines.TryGetValue(Plugin.ClientState.LocalContentId, out var fcSub))
                return;

            SelectedSub = fcSub.Submarines.FirstOrDefault(sub => sub.Register == VoyageInterfaceSelection) ?? new Submarine();
            var build = new Build.RouteBuild(SelectedSub);

            if (build != CurrentBuild)
                CacheValid = false;

            if (!CacheValid)
            {
                CacheValid = true;
                CurrentBuild.UpdateBuild(SelectedSub);

                // Reset BestEXP to allow automatic calculation trigger
                LastComputedBuild = Build.RouteBuild.Empty;
                BestPath = Array.Empty<uint>();
            }
        }
        else
        {
            CacheValid = false;
        }
    }
}
