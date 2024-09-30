using Dalamud.Utility;
using SubmarineTracker.Data;

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
                    var customTerm = Loc.Localize("Terms - Custom", "Custom")!;

                    Plugin.EnsureFCOrderSafety();
                    var existingSubs = Plugin.GetFCOrderWithoutHidden().SelectMany(id =>
                    {
                        var fc = Plugin.DatabaseCache.GetFreeCompanies()[id];
                        var subs = Plugin.DatabaseCache.GetSubmarines(id);
                        return subs.Select(s => Plugin.NameConverter.GetSubIdentifier(s, fc));
                    }).ToArray();

                    var fcId = Plugin.GetFCId;
                    if (Plugin.Configuration.ShowOnlyCurrentFC && Plugin.DatabaseCache.GetFreeCompanies().TryGetValue(Plugin.ClientState.LocalContentId, out var fcSub))
                        existingSubs = Plugin.DatabaseCache.GetSubmarines(fcId).Select(s => Plugin.NameConverter.GetSubIdentifier(s, fcSub)).ToArray();
                    existingSubs = existingSubs.Prepend(customTerm).ToArray();

                    if (existingSubs.Length < CurrentBuild.OriginalSub)
                        CurrentBuild.OriginalSub = 0;

                    var windowWidth = ImGui.GetWindowWidth() / 2;
                    ImGui.PushItemWidth(windowWidth - (5.0f * ImGuiHelpers.GlobalScale));
                    ImGui.Combo("##existingSubs", ref CurrentBuild.OriginalSub, existingSubs, existingSubs.Length);
                    ImGui.PopItemWidth();

                    // Calculate first so rank can be changed afterwards
                    if (existingSubs[CurrentBuild.OriginalSub] != customTerm)
                    {
                        sub = Plugin.GetFCOrderWithoutHidden().SelectMany(id => Plugin.DatabaseCache.GetSubmarines(id)).ToArray()[CurrentBuild.OriginalSub - 1];
                        CurrentBuild.UpdateBuild(sub);
                    }

                    ImGui.SameLine();
                    ImGui.PushItemWidth(windowWidth - (3.0f * ImGuiHelpers.GlobalScale));
                    ImGui.SliderInt("##SliderRank", ref CurrentBuild.Rank, 1, (int) RankSheet.Last().RowId, $"{Loc.Localize("Terms - Rank", "Rank")} %d");
                    ImGui.PopItemWidth();
                }
                else
                {
                    ImGui.TextColored(ImGuiColors.HealerGreen, Loc.Localize("Builder Build Tab - Automatic Selection", "Automatic Selection: {0} - Rank {1}").Format(SelectedSub.Name, SelectedSub.Rank));

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

                CommonRoutes();
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
            SelectedSub = Plugin.DatabaseCache.GetSubmarines(Plugin.GetFCId).FirstOrDefault(sub => sub.Register == VoyageInterfaceSelection) ?? new Submarine();
            var build = new Build.RouteBuild(SelectedSub);

            if (build != CurrentBuild)
                CacheValid = false;

            if (!CacheValid)
            {
                CacheValid = true;
                CurrentBuild.UpdateBuild(SelectedSub);

                // Reset BestEXP to allow automatic calculation trigger
                LastComputedBuild = Build.RouteBuild.Empty;
                BestRoute = Voyage.BestRoute.Empty();
            }
        }
        else
        {
            CacheValid = false;
        }
    }
}
