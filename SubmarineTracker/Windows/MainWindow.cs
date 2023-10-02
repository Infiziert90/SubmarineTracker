using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using SubmarineTracker.Data;

using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    public static ExcelSheet<SubmarineMap> MapSheet = null!;
    public static ExcelSheet<SubmarineExploration> ExplorationSheet = null!;

    private ulong CurrentSelection = 1;
    private static readonly Vector2 IconSize = new(28, 28);
    private static readonly int MaxLength = "Heavens' Eye Materia III".Length;

    private static readonly Vector2 WindowMinimumSize = new(800, 550);

    public MainWindow(Plugin plugin, Configuration configuration) : base("Tracker###SubmarineTracker")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = WindowMinimumSize,
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = configuration;

        MapSheet = Plugin.Data.GetExcelSheet<SubmarineMap>()!;
        ExplorationSheet = Plugin.Data.GetExcelSheet<SubmarineExploration>()!;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGuiHelpers.ScaledDummy(5.0f);

        if (!Submarines.KnownSubmarines.Values.Any(s => s.Submarines.Any()))
        {
            Helper.NoData();
            return;
        }

        var buttonHeight = ImGui.CalcTextSize("RRRR").Y + (20.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.BeginChild("SubContent", new Vector2(0, -buttonHeight)))
        {
            var buttonWidth = ImGui.CalcTextSize("XXXXX@Halicarnassus").X + (10 * ImGuiHelpers.GlobalScale);
            if (Configuration.UseCharacterName)
                buttonWidth = ImGui.CalcTextSize("Character Name@Halicarnassus").X + (10 * ImGuiHelpers.GlobalScale);

            ImGui.Columns(2, "columns", true);
            if (!Configuration.UserResize)
                ImGui.SetColumnWidth(0, buttonWidth + (20 * ImGuiHelpers.GlobalScale));
            else
                buttonWidth = ImGui.GetContentRegionAvail().X;

            if (ImGui.BeginChild("##fcList"))
            {
                Plugin.EnsureFCOrderSafety();
                if (!(Configuration.ShowAll && CurrentSelection == 1))
                    if (!Submarines.KnownSubmarines.ContainsKey(CurrentSelection))
                        CurrentSelection = Configuration.FCOrder.First();

                var current = CurrentSelection;

                if (Configuration.ShowAll)
                {
                    if (current == 1)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedPink);
                        if (ImGui.Button(Loc.Localize("Terms - All", "All"), new Vector2(buttonWidth, 0)))
                            CurrentSelection = 1;
                        ImGui.PopStyleColor();
                    }
                    else
                    if (ImGui.Button(Loc.Localize("Terms - All", "All"), new Vector2(buttonWidth, 0)))
                        CurrentSelection = 1;
                }

                foreach (var key in Configuration.FCOrder)
                {
                    var fc = Submarines.KnownSubmarines[key];
                    if (!fc.Submarines.Any())
                        continue;

                    var text = Helper.GetFCName(fc);
                    if (current == key)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedPink);
                        if (ImGui.Button($"{text}##{key}", new Vector2(buttonWidth, 0)))
                            CurrentSelection = key;
                        ImGui.PopStyleColor();
                    }
                    else
                        if (ImGui.Button($"{text}##{key}", new Vector2(buttonWidth, 0)))
                            CurrentSelection = key;
                }
            }
            ImGui.EndChild();

            ImGui.NextColumn();

            if (CurrentSelection != 1)
            {
                OverviewTab();
            }
            else
            {
                All();
            }
        }
        ImGui.EndChild();

        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(1.0f);

        if (ImGui.BeginChild("BottomBar", new Vector2(0, 0), false, 0))
            Helper.MainMenuIcon();
        ImGui.EndChild();
    }

    private void All()
    {
        Plugin.EnsureFCOrderSafety();
        var widthCheck = Configuration.ShowRouteInAll && ImGui.GetWindowSize().X < WindowMinimumSize.X + (300.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.BeginTable("##allTable", widthCheck ? 1 : 2))
        {
            foreach (var id in Configuration.FCOrder)
            {
                ImGui.TableNextColumn();
                var fc = Submarines.KnownSubmarines[id];
                var secondRow = ImGui.GetContentRegionAvail().X / (widthCheck ? 6.0f : Configuration.ShowDateInAll ? 3.2f : 2.8f);
                var thirdRow = ImGui.GetContentRegionAvail().X / (widthCheck ? 3.7f : Configuration.ShowDateInAll ? 1.9f : 1.6f);

                ImGui.TextColored(ImGuiColors.DalamudViolet, $"{Helper.GetFCName(fc)}:");
                foreach (var (sub, idx) in fc.Submarines.Select((val, i) => (val, i)))
                {
                    ImGui.Indent(10.0f);
                    var begin = ImGui.GetCursorScreenPos();

                    ImGui.TextColored(ImGuiColors.HealerGreen, $"{idx + 1}. ");
                    ImGui.SameLine();

                    var condition = sub.PredictDurability() > 0;
                    var color = condition ? ImGuiColors.TankBlue : ImGuiColors.DalamudYellow;
                    ImGui.TextColored(color, $"{Loc.Localize("Terms - Rank", "Rank")} {sub.Rank}");
                    ImGui.SameLine(secondRow);
                    ImGui.TextColored(color, $"({sub.Build.FullIdentifier()})");

                    ImGui.SameLine(thirdRow);

                    var route = "";
                    var time = $" {Loc.Localize("Terms - No Voyage", "No Voyage")} ";
                    if (sub.IsOnVoyage())
                    {
                        var startPoint = Voyage.FindVoyageStart(sub.Points.First());
                        route = $"{string.Join(" -> ", sub.Points.Select(p => NumToLetter(p - startPoint)))}";

                        time = $" {Loc.Localize("Terms - Done", "Done")} ";
                        var returnTime = sub.ReturnTime - DateTime.Now.ToUniversalTime();
                        if (returnTime.TotalSeconds > 0)
                            time = !Configuration.ShowDateInAll ? $" {ToTime(returnTime)} " : $" {sub.ReturnTime.ToLocalTime()}";
                    }

                    var fullText = $"[ {time}{(Configuration.ShowRouteInAll ? $"   {route}" : "")} ]";
                    ImGui.TextColored(ImGuiColors.ParsedOrange, fullText);

                    var textSize = ImGui.CalcTextSize(fullText);
                    var end = new Vector2(begin.X + textSize.X + thirdRow, begin.Y + textSize.Y + 4.0f);
                    if (ImGui.IsMouseHoveringRect(begin, end))
                    {
                        var tooltip = condition ?  "" : $"{Loc.Localize("Return Overlay Tooltip - Repair Needed", "This submarine needs repair on return.")}\n";
                        tooltip += $"{Loc.Localize("Terms - Rank", "Rank")} {sub.Rank}    ({sub.Build.FullIdentifier()})\n";

                        tooltip += $"{Loc.Localize("Terms - Route", "Route")}: {route}\n";

                        var predictedExp = sub.PredictExpGrowth();
                        tooltip += $"{Loc.Localize("Terms - EXP After", "After")}: {predictedExp.Rank} ({predictedExp.Exp:##0.00}%%)\n";

                        tooltip += $"{Loc.Localize("Terms - Repair", "Repair")}: {Loc.Localize("Main Window Tooltip - Repair", "{0} kits after {1} voyages").Format(sub.Build.RepairCosts, sub.CalculateUntilRepair())}";

                        ImGui.SetTooltip(tooltip);
                    }

                    // if (ImGui.GetIO().KeyShift)
                    //     ImGui.GetForegroundDrawList(ImGuiHelpers.MainViewport).AddRect(begin, end, 0xFF00FF00);

                    ImGui.Unindent(10.0f);
                }
                ImGuiHelpers.ScaledDummy(5.0f);
            }

            ImGui.EndTable();
        }
    }

    private void OverviewTab()
    {
        var selectedFc = Submarines.KnownSubmarines[CurrentSelection];
        if (ImGui.BeginTabBar("##fcSubmarineDetail"))
        {
            if (ImGui.BeginTabItem($"{Loc.Localize("Main Window Tab - Overview", "Overview")}##Overview"))
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
                        if (cachedItems.TryGetValue((uint) Items.Tanks, out var temp))
                            tanks = temp.Count;
                        if (cachedItems.TryGetValue((uint) Items.Kits, out temp))
                            kits = temp.Count;

                        ImGui.TextColored(ImGuiColors.HealerGreen, Loc.Localize("Main Window Entry - Resources", "Resources:"));
                        ImGui.SameLine();
                        ImGui.TextColored(ImGuiColors.TankBlue, $"{Loc.Localize("Terms - Tanks", "Tanks")} x{tanks} & {Loc.Localize("Terms - Kits", "Kits")} x{kits}");
                    }
                }

                foreach (var sub in selectedFc.Submarines)
                {
                    ImGuiHelpers.ScaledDummy(10.0f);
                    ImGui.Indent(10.0f);

                    ImGui.TextColored(ImGuiColors.HealerGreen, sub.Name);

                    ImGui.TextColored(ImGuiColors.TankBlue, $"{Loc.Localize("Terms - Rank", "Rank")} {sub.Rank}");
                    ImGui.SameLine(secondRow);
                    ImGui.TextColored(ImGuiColors.TankBlue, $"({sub.Build.FullIdentifier()})");


                    if (Configuration.ShowOnlyLowest)
                    {
                        var repair = $"{sub.LowestCondition():F}%%";

                        ImGui.SameLine(thirdRow);
                        ImGui.TextColored(ImGuiColors.ParsedOrange, repair);
                    }

                    if (sub.IsOnVoyage())
                    {
                        var time = "";
                        if (Configuration.ShowTimeInOverview)
                        {
                            time = " Done ";

                            var returnTime = sub.ReturnTime - DateTime.Now.ToUniversalTime();
                            if (returnTime.TotalSeconds > 0)
                                if (Configuration.ShowBothOptions)
                                    time = $" {sub.ReturnTime.ToLocalTime()} ({ToTime(returnTime)}) ";
                                else if (!Configuration.UseDateTimeInstead)
                                    time = $" {ToTime(returnTime)} ";
                                else
                                    time = $" {sub.ReturnTime.ToLocalTime()}";
                        }

                        if (Configuration.ShowRouteInOverview)
                            time += $" {string.Join(" -> ", sub.Points.Select(p => NumToLetter(p, true)))} ";

                        ImGui.SameLine(Configuration.ShowOnlyLowest ? lastRow : thirdRow);
                        ImGui.TextColored(ImGuiColors.ParsedOrange, time.Length != 0 ? $"[{time}]" : "");
                    }
                    else
                    {

                        if (Configuration.ShowTimeInOverview || Configuration.ShowRouteInOverview)
                        {
                            ImGui.SameLine(Configuration.ShowOnlyLowest ? lastRow : thirdRow);
                            ImGui.TextColored(ImGuiColors.ParsedOrange,$"[{Loc.Localize("Terms - No Voyage Data", "No Voyage Data")}]");
                        }
                    }

                    ImGui.Unindent(10.0f);
                }

                ImGui.EndTabItem();
            }

            foreach (var (sub, idx) in selectedFc.Submarines.Select((val, i) => (val, i)))
            {
                if (ImGui.BeginTabItem($"{sub.Name}##{idx}"))
                {
                    DetailedSub(sub);
                    ImGui.EndTabItem();
                }
            }

            if (ImGui.BeginTabItem($"{Loc.Localize("Main Window Tab - Loot", "Loot")}##Loot"))
            {
                ImGuiHelpers.ScaledDummy(5.0f);

                selectedFc.RebuildStats(Configuration.ExcludeLegacy);

                if (ImGui.BeginChild("##LootOverview"))
                {
                    if (!selectedFc.AllLoot.Any())
                    {
                        Helper.WrappedError(Loc.Localize("Error - No Data", "No data found for this character's FC\nPlease visit your Company Workshop and access Submersible Management at the Voyage Control Panel."));
                    }
                    else
                    {
                        if (ImGui.BeginTabBar("##fcLootMap"))
                        {
                            var fullWindowWidth = ImGui.GetWindowWidth();
                            var halfWindowWidth = fullWindowWidth / 2;
                            foreach (var map in MapSheet.Where(r => r.RowId != 0))
                            {
                                var text = MapToShort(map.RowId);
                                if (text == "")
                                    text = ToStr(map.Name);

                                if (ImGui.BeginTabItem(text))
                                {
                                    ImGuiHelpers.ScaledDummy(10.0f);
                                    var endCursorPositionLeft = ImGui.GetCursorPos();
                                    var endCursorPositionRight = ImGui.GetCursorPos();
                                    var cursorPosition = ImGui.GetCursorPos();
                                    foreach (var ((point, loot), idx) in selectedFc.AllLoot.Where(kv => GetPoint(kv.Key).Map.Row == map.RowId).Select((val, i) => (val, i)))
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

                                        ImGui.TextUnformatted(UpperCaseStr(ExplorationSheet.GetRow(point)!.Destination));
                                        ImGuiHelpers.ScaledDummy(5.0f);
                                        foreach (var ((item, count), iIdx) in loot.Select((val, ii) => (val, ii)))
                                        {
                                            ImGui.Indent(10.0f);
                                            if (idx % 2 == 1)
                                                ImGui.SetCursorPosX(cursorPosition.X + 10.0f);
                                            Helper.DrawScaledIcon(item.Icon, IconSize);

                                            var name = ToStr(item.Name);
                                            if (MaxLength < name.Length)
                                                name = name.Truncate(MaxLength);
                                            ImGui.SameLine();
                                            ImGui.TextUnformatted(name);
                                            if (ImGui.IsItemHovered())
                                                ImGui.SetTooltip(ToStr(item.Name));

                                            var length = ImGui.CalcTextSize($"{count}").X;
                                            ImGui.SameLine(idx % 2 == 0 ? halfWindowWidth - 30.0f - length : fullWindowWidth - 30.0f - length);
                                            ImGui.TextUnformatted($"{count}");

                                            ImGui.Unindent(10.0f);
                                        }

                                        ImGuiHelpers.ScaledDummy(10.0f);

                                        if (idx % 2 == 0)
                                            endCursorPositionLeft = ImGui.GetCursorPos();
                                        else
                                            endCursorPositionRight = ImGui.GetCursorPos();
                                    }

                                    ImGui.EndTabItem();
                                }
                            }

                            ImGui.EndTabBar();
                        }
                    }
                }
                ImGui.EndChild();

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DetailedSub(Submarines.Submarine sub)
    {
        ImGuiHelpers.ScaledDummy(5.0f);
        ImGui.Indent(10.0f);

        if (ImGui.BeginTable($"##submarineOverview##{sub.Name}", 2))
        {
            ImGui.TableSetupColumn("##key", 0, 0.2f);
            ImGui.TableSetupColumn("##value");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Loc.Localize("Terms - Rank", "Rank"));
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{sub.Rank}");

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Loc.Localize("Terms - Build", "Build"));
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{sub.Build.FullIdentifier()}");

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Loc.Localize("Terms - Repair", "Repair"));
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{sub.Build.RepairCosts}");
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{sub.HullCondition:F}% | {sub.SternCondition:F}% | {sub.BowCondition:F}% | {sub.BridgeCondition:F}%");
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Loc.Localize("Main Window Overview - Breaks After", "Breaks after {0} voyages").Format(sub.CalculateUntilRepair()));
            ImGui.TextUnformatted($"");

            ImGui.TableNextRow();

            if (sub.ValidExpRange())
            {
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Loc.Localize("Terms - Exp", "Exp"));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{sub.CExp} / {sub.NExp}");
                ImGui.SameLine();
                ImGui.TextUnformatted($"{(double) sub.CExp / sub.NExp * 100.0:##0.00}%");

                var predictedExp = sub.PredictExpGrowth();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Loc.Localize("Terms - Predicted", "Predicted"));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{Loc.Localize("Terms - Rank", "Rank")} {predictedExp.Rank} ({predictedExp.Exp:##0.00}%)");
            }

            if (sub.IsOnVoyage())
            {
                AddTableSpacing();

                var time = Loc.Localize("Terms - Done", "Done");
                var returnTime = sub.ReturnTime - DateTime.Now.ToUniversalTime();
                if (returnTime.TotalSeconds > 0)
                    time = $"{(int) returnTime.TotalHours:#00}:{returnTime:mm}:{returnTime:ss} {Loc.Localize("Terms - hours", "hours")}";

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Loc.Localize("Terms - Time", "Time"));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(time);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Loc.Localize("Terms - Date", "Date"));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{sub.ReturnTime.ToLocalTime()}");

                var startPoint = Voyage.FindVoyageStart(sub.Points.First());
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Loc.Localize("Terms - Map", "Map"));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{GetMapName(startPoint)}");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Loc.Localize("Terms - Route", "Route"));
                ImGui.TableNextColumn();
                ImGui.TextWrapped($"{string.Join(" -> ", sub.Points.Select(p => NumToLetter(p - startPoint)))}");
            }

            AddTableSpacing();
        }
        ImGui.EndTable();

        if (Configuration.ShowExtendedPartsList)
        {
            ImGuiHelpers.ScaledDummy(10.0f);

            if (ImGui.BeginTable($"##submarineOverview##{sub.Name}", 2))
            {
                ImGui.TableSetupColumn("##icon", 0, 0.15f);
                ImGui.TableSetupColumn("##partName");

                ImGui.TableNextColumn();
                Helper.DrawScaledIcon(sub.HullIconId, IconSize);
                ImGui.TableNextColumn();
                ImGui.TextColored(ImGuiColors.ParsedGold, sub.HullName);
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                Helper.DrawScaledIcon(sub.SternIconId, IconSize);
                ImGui.TableNextColumn();
                ImGui.TextColored(ImGuiColors.ParsedGold, sub.SternName);
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                Helper.DrawScaledIcon(sub.BowIconId, IconSize);
                ImGui.TableNextColumn();
                ImGui.TextColored(ImGuiColors.ParsedGold, sub.BowName);
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                Helper.DrawScaledIcon(sub.BridgeIconId, IconSize);
                ImGui.TableNextColumn();
                ImGui.TextColored(ImGuiColors.ParsedGold, sub.BridgeName);
                ImGui.TableNextRow();
            }

            ImGui.EndTable();
        }

        ImGui.Unindent(10.0f);
    }

    private void AddTableSpacing()
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGuiHelpers.ScaledDummy(10.0f);
        ImGui.TableNextRow();
    }

    private static string GetMapName(uint key) => ToStr(ExplorationSheet.First(r => r.RowId == key).Map.Value!.Name);
    private static SubmarineExploration GetPoint(uint key) => ExplorationSheet.GetRow(key)!;
}
