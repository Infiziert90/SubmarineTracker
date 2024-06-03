using Dalamud.Interface.Utility.Raii;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Main;

public partial class MainWindow
{
    private void Overview()
    {
        var selectedFc = Plugin.DatabaseCache.GetFreeCompanies()[CurrentSelection];
        using var tabBar = ImRaii.TabBar("##fcSubmarineDetail");
        if (!tabBar.Success)
            return;

        using (var mainTab = ImRaii.TabItem($"{Loc.Localize("Main Window Tab - Overview", "Overview")}##Overview"))
        {
            if (mainTab)
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

                        ImGui.TextColored(ImGuiColors.HealerGreen, Loc.Localize("Main Window Entry - Resources", "Resources:"));
                        ImGui.SameLine();
                        ImGui.TextColored(ImGuiColors.TankBlue, $"{Loc.Localize("Terms - Tanks", "Tanks")} x{tanks} & {Loc.Localize("Terms - Kits", "Kits")} x{kits}");
                    }
                }

                foreach (var sub in Plugin.DatabaseCache.GetSubmarines(selectedFc.FreeCompanyId))
                {
                    ImGuiHelpers.ScaledDummy(10.0f);
                    using var indent = ImRaii.PushIndent(10.0f);

                    ImGui.TextColored(ImGuiColors.HealerGreen, Plugin.NameConverter.GetJustSub(sub));
                    ImGui.TextColored(ImGuiColors.TankBlue, $"{Loc.Localize("Terms - Rank", "Rank")} {sub.Rank}");
                    ImGui.SameLine(secondRow);
                    ImGui.TextColored(ImGuiColors.TankBlue, $"({sub.Build.FullIdentifier()})");


                    if (Plugin.Configuration.ShowOnlyLowest)
                    {
                        ImGui.SameLine(thirdRow);
                        ImGui.TextColored(ImGuiColors.ParsedOrange, $"{sub.LowestCondition():F}%%");
                    }

                    if (sub.IsOnVoyage())
                    {
                        var time = "";
                        if (Plugin.Configuration.ShowTimeInOverview)
                        {
                            time = " Done ";

                            var returnTime = sub.ReturnTime - DateTime.Now.ToUniversalTime();
                            if (returnTime.TotalSeconds > 0)
                                if (Plugin.Configuration.ShowBothOptions)
                                    time = $" {sub.ReturnTime.ToLocalTime()} ({Utils.ToTime(returnTime)}) ";
                                else if (!Plugin.Configuration.UseDateTimeInstead)
                                    time = $" {Utils.ToTime(returnTime)} ";
                                else
                                    time = $" {sub.ReturnTime.ToLocalTime()}";
                        }

                        if (Plugin.Configuration.ShowRouteInOverview)
                            time += $" {string.Join(" -> ", sub.Points.Select(p => Utils.NumToLetter(p, true)))} ";

                        ImGui.SameLine(Plugin.Configuration.ShowOnlyLowest ? lastRow : thirdRow);
                        ImGui.TextColored(ImGuiColors.ParsedOrange, time.Length != 0 ? $"[{time}]" : "");
                    }
                    else
                    {
                        if (Plugin.Configuration.ShowTimeInOverview || Plugin.Configuration.ShowRouteInOverview)
                        {
                            ImGui.SameLine(Plugin.Configuration.ShowOnlyLowest ? lastRow : thirdRow);
                            ImGui.TextColored(ImGuiColors.ParsedOrange, $"[{Loc.Localize("Terms - No Voyage Data", "No Voyage Data")}]");
                        }
                    }
                }
            }
        }

        foreach (var (sub, idx) in Plugin.DatabaseCache.GetSubmarines(selectedFc.FreeCompanyId).WithIndex())
        {
            using var subTab = ImRaii.TabItem($"{Plugin.NameConverter.GetJustSub(sub)}##{idx}");
            if (subTab)
                DetailedSub(sub);
        }

        using var lootTab = ImRaii.TabItem($"{Loc.Localize("Main Window Tab - Loot", "Loot")}##Loot");
        if (!lootTab.Success)
            return;

        ImGuiHelpers.ScaledDummy(5.0f);
        // TODO: FIX
        // selectedFc.RebuildStats(Plugin.Configuration.ExcludeLegacy);

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
        foreach (var map in MapSheet.Where(r => r.RowId != 0))
        {
            var text = Utils.MapToShort(map.RowId);
            if (text == "")
                text = Utils.ToStr(map.Name);

            using var mapTab = ImRaii.TabItem(text);
            if (!mapTab.Success)
                continue;

            ImGuiHelpers.ScaledDummy(10.0f);
            var endCursorPositionLeft = ImGui.GetCursorPos();
            var endCursorPositionRight = ImGui.GetCursorPos();
            var cursorPosition = ImGui.GetCursorPos();

            var mapLoot = fcLoot.Where(pair => Voyage.SectorToPretty[pair.Key].Map.Row == map.RowId).WithIndex();
            foreach (var ((sector, loot), idx) in mapLoot)
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

                ImGui.TextUnformatted(Utils.SectorToName(sector));
                ImGuiHelpers.ScaledDummy(5.0f);
                foreach (var (item, count) in loot)
                {
                    using var innerIndent = ImRaii.PushIndent(10.0f);

                    if (idx % 2 == 1)
                        ImGui.SetCursorPosX(cursorPosition.X + 10.0f);

                    var name = Utils.ToStr(item.Name);
                    if (MaxLength < name.Length)
                        name = name.Truncate(MaxLength);

                    Helper.DrawScaledIcon(item.Icon, IconSize);
                    ImGui.SameLine();
                    ImGui.TextUnformatted(name);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(Utils.ToStr(item.Name));

                    var length = ImGui.CalcTextSize($"{count}").X;
                    ImGui.SameLine(idx % 2 == 0
                                       ? halfWindowWidth - 30.0f - length
                                       : fullWindowWidth - 30.0f - length);
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
