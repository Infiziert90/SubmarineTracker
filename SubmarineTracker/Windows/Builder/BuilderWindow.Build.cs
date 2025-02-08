using Dalamud.Utility;
using SubmarineTracker.Data;
using SubmarineTracker.Resources;

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

        using var tabItem = ImRaii.TabItem($"{Language.BuilderTabBuild}##Build");
        if (!tabItem.Success)
            return;

        using var child = ImRaii.Child("SubSelector", new Vector2(0, -(170 * ImGuiHelpers.GlobalScale)));
        if (!child.Success)
            return;

        if (!CacheValid)
        {
            var customTerm = Language.TermsCustom;

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
            ImGui.SetNextItemWidth(windowWidth - (5.0f * ImGuiHelpers.GlobalScale));
            ImGui.Combo("##existingSubs", ref CurrentBuild.OriginalSub, existingSubs, existingSubs.Length);

            // Calculate first so rank can be changed afterwards
            if (existingSubs[CurrentBuild.OriginalSub] != customTerm)
            {
                sub = Plugin.GetFCOrderWithoutHidden().SelectMany(id => Plugin.DatabaseCache.GetSubmarines(id)).ToArray()[CurrentBuild.OriginalSub];
                CurrentBuild.UpdateBuild(sub);
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(windowWidth - (3.0f * ImGuiHelpers.GlobalScale));
            ImGui.SliderInt("##SliderRank", ref CurrentBuild.Rank, 1, (int) Sheets.LastRank, $"{Language.TermsRank} %d");
        }
        else
        {
            Helper.TextColored(ImGuiColors.HealerGreen, Language.BuilderBuildTabAutomaticSelection.Format(SelectedSub.Name, SelectedSub.Rank));

            if (ImGui.IsItemHovered())
                Helper.Tooltip(Language.BuilderBuildTooltipAutomaticSelection);
        }

        ImGuiHelpers.ScaledDummy(5);

        using (var table = ImRaii.Table("##submarinePartSelection", 5, ImGuiTableFlags.BordersInnerH))
        {
            if (table.Success)
            {
                ImGui.TableSetupColumn(Language.TermType);
                ImGui.TableSetupColumn(Language.SubPartHull);
                ImGui.TableSetupColumn(Language.SubPartStern);
                ImGui.TableSetupColumn(Language.SubPartBow);
                ImGui.TableSetupColumn(Language.SubPartBridge);

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
            }
        }

        CommonRoutes();
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
                BestRoute = Voyage.BestRoute.Empty;
                LastComputedBuild = Build.RouteBuild.Empty;
            }
        }
        else
        {
            CacheValid = false;
        }
    }
}
