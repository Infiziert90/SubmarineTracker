using SubmarineTracker.Data;
using SubmarineTracker.Resources;

namespace SubmarineTracker.Windows.Main;

public partial class MainWindow
{
    private void Overview()
    {
        var selectedFc = Plugin.DatabaseCache.GetFreeCompanies()[CurrentSelection];
        using var tabBar = ImRaii.TabBar("##fcSubmarineDetail");
        if (!tabBar.Success)
            return;

        using (var mainTab = ImRaii.TabItem($"{Language.MainWindowTabOverview}##Overview"))
        {
            if (mainTab.Success)
            {
                var secondRow = ImGui.GetContentRegionMax().X / 8;
                var thirdRow = ImGui.GetContentRegionMax().X / 4.2f;
                var lastRow = ImGui.GetContentRegionMax().X / 3;

                if (Plugin.AllaganToolsConsumer.IsAvailable)
                {
                    ImGuiHelpers.ScaledDummy(5.0f);

                    // build cache if needed
                    Storage.BuildStorageCache();

                    if (Storage.StorageCache.TryGetValue(CurrentSelection, out var cachedItems))
                    {
                        uint tanks = 0, kits = 0;
                        if (cachedItems.TryGetValue((uint)Items.Tanks, out var temp))
                            tanks = temp.Count;
                        if (cachedItems.TryGetValue((uint)Items.Kits, out temp))
                            kits = temp.Count;

                        Helper.TextColored(ImGuiColors.HealerGreen, Language.MainWindowEntryResources);
                        ImGui.SameLine();
                        Helper.TextColored(ImGuiColors.TankBlue, $"{Language.TermsTanks} x{tanks} & {Language.TermsKits} x{kits}");
                    }
                }

                foreach (var sub in Plugin.DatabaseCache.GetSubmarines(selectedFc.FreeCompanyId))
                {
                    using var indent = ImRaii.PushIndent(10.0f);

                    ImGuiHelpers.ScaledDummy(10.0f);
                    Helper.TextColored(ImGuiColors.HealerGreen, Plugin.NameConverter.GetJustSub(sub));
                    Helper.TextColored(ImGuiColors.TankBlue, $"{Language.TermsRank} {sub.Rank}");
                    ImGui.SameLine(secondRow);
                    Helper.TextColored(ImGuiColors.TankBlue, $"({sub.Build.FullIdentifier()})");


                    if (Plugin.Configuration.ShowOnlyLowest)
                    {
                        ImGui.SameLine(thirdRow);
                        Helper.TextColored(ImGuiColors.ParsedOrange, $"{sub.LowestCondition():F}%%");
                    }

                    if (sub.IsOnVoyage())
                    {
                        var time = "";
                        if (Plugin.Configuration.ShowTimeInOverview)
                        {
                            time = " Done ";

                            var returnTime = sub.ReturnTime - DateTime.Now.ToUniversalTime();
                            if (returnTime.TotalSeconds > 0)
                            {
                                if (Plugin.Configuration.ShowBothOptions)
                                    time = $" {sub.ReturnTime.ToLocalTime()} ({Utils.ToTime(returnTime)}) ";
                                else if (!Plugin.Configuration.UseDateTimeInstead)
                                    time = $" {Utils.ToTime(returnTime)} ";
                                else
                                    time = $" {sub.ReturnTime.ToLocalTime()}";
                            }
                        }

                        if (Plugin.Configuration.ShowRouteInOverview)
                            time += $" {Utils.SectorsToPath(" -> ", sub.Points)} ";

                        ImGui.SameLine(Plugin.Configuration.ShowOnlyLowest ? lastRow : thirdRow);
                        Helper.TextColored(ImGuiColors.ParsedOrange, time.Length != 0 ? $"[{time}]" : "");
                    }
                    else
                    {
                        if (Plugin.Configuration.ShowTimeInOverview || Plugin.Configuration.ShowRouteInOverview)
                        {
                            ImGui.SameLine(Plugin.Configuration.ShowOnlyLowest ? lastRow : thirdRow);
                            Helper.TextColored(ImGuiColors.ParsedOrange, $"[{Language.TermsNoVoyageData}]");
                        }
                    }
                }
            }
        }

        foreach (var (sub, idx) in Plugin.DatabaseCache.GetSubmarines(selectedFc.FreeCompanyId).WithIndex())
        {
            using var subTab = ImRaii.TabItem($"{Plugin.NameConverter.GetJustSub(sub)}##{idx}");
            if (subTab.Success)
                DetailedSub(sub);
        }

        using var lootTab = ImRaii.TabItem($"{Language.MainWindowTabLoot}##Loot");
        if (!lootTab.Success)
            return;

        ImGuiHelpers.ScaledDummy(5.0f);

        using var lootChild = ImRaii.Child("##lootOverview");
        if (!lootChild.Success)
            return;

        var fcLoot = Plugin.DatabaseCache.GetFCAllLoot(selectedFc.FreeCompanyId);
        if (fcLoot.Count == 0)
        {
            Helper.NoData();
            return;
        }

        using var lootTabBar = ImRaii.TabBar("##fcLootMap");
        if (!lootTabBar.Success)
            return;

        var fullWindowWidth = ImGui.GetWindowWidth();
        var halfWindowWidth = fullWindowWidth / 2;
        foreach (var map in Sheets.MapSheet.Where(r => r.RowId != 0))
        {
            var text = Utils.MapToShort(map.RowId);
            if (text == "")
                text = map.Name.ExtractText();

            using var mapTab = ImRaii.TabItem(text);
            if (!mapTab.Success)
                continue;

            ImGuiHelpers.ScaledDummy(10.0f);
            var endCursorPositionLeft = ImGui.GetCursorPos();
            var endCursorPositionRight = ImGui.GetCursorPos();
            var cursorPosition = ImGui.GetCursorPos();

            foreach (var ((sector, loot), idx) in fcLoot.Where(pair => Voyage.SectorToSheet[pair.Key].Map.RowId == map.RowId).WithIndex())
            {
                if (idx % 2 == 0)
                {
                    if (idx != 0 && endCursorPositionLeft.Y > endCursorPositionRight.Y)
                        ImGui.SetCursorPosY(endCursorPositionLeft.Y);

                    cursorPosition = ImGui.GetCursorPos();
                }
                else
                {
                    cursorPosition.X += halfWindowWidth;
                    ImGui.SetCursorPos(cursorPosition);
                }

                ImGui.TextUnformatted(Voyage.SectorToName(sector));
                ImGuiHelpers.ScaledDummy(5.0f);

                foreach (var (item, count) in loot)
                {
                    using var innerIndent = ImRaii.PushIndent(10.0f);

                    if (idx % 2 == 1)
                        ImGui.SetCursorPosX(cursorPosition.X + 10.0f);

                    var name = item.Name.ExtractText();
                    Helper.DrawScaledIcon(item.Icon, IconSize);
                    ImGui.SameLine();
                    ImGui.TextUnformatted(name.Truncate(MaxLength));
                    if (ImGui.IsItemHovered())
                        Helper.Tooltip(name);

                    var length = ImGui.CalcTextSize($"{count}").X;
                    ImGui.SameLine(idx % 2 == 0 ? halfWindowWidth - 30.0f - length : fullWindowWidth - 30.0f - length);
                    ImGui.TextUnformatted($"{count}");
                }

                ImGuiHelpers.ScaledDummy(10.0f);

                if (idx % 2 == 0)
                    endCursorPositionLeft = ImGui.GetCursorPos();
                else
                    endCursorPositionRight = ImGui.GetCursorPos();
            }
        }
    }
}
