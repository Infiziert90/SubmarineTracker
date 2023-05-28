using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using SubmarineTracker.Data;

using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;
    private static ExcelSheet<Item> ItemSheet = null!;

    private static readonly ExcelSheetSelector.ExcelSheetPopupOptions<Item> ItemPopupOptions = new()
    {
        FormatRow = a => a.RowId switch { _ => $"[#{a.RowId}] {ToStr(a.Name)}" },
        FilteredSheet = Plugin.Data.GetExcelSheet<Item>()!.Skip(1).Where(i => ToStr(i.Name) != "")
    };

    public ConfigWindow(Plugin plugin) : base("Configuration")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(320, 460),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.Plugin = plugin;
        this.Configuration = plugin.Configuration;
        ItemSheet = Plugin.Data.GetExcelSheet<Item>()!;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("##ConfigTabBar"))
        {
            if (ImGui.BeginTabItem("General"))
            {
                ImGuiHelpers.ScaledDummy(5.0f);

                var changed = false;
                ImGui.TextColored(ImGuiColors.DalamudViolet, "FC Buttons:");
                ImGui.Indent(10.0f);
                changed |= ImGui.Checkbox("Show All Button", ref Configuration.ShowAll);
                ImGuiComponents.HelpMarker("Adds an All button into the FC list, which will list all known submarines and there times,\n" +
                                           "for an easy overview.\n" +
                                           "Note: This view will be messy with too many FCs.");
                changed |= ImGui.Checkbox("Use Character Name", ref Configuration.UseCharacterName);
                ImGuiComponents.HelpMarker("Use character name instead of FC tag in the overview.\n" +
                                           "If the FC tag is still shown, this means your character name has yet to be saved, this will resolve itself the next time your submarines are sent out.\n" +
                                           "Be aware this option can lead to cut-off button text.\n" +
                                           "Note: This applies to all sections were the FC-Tag would have been used.");
                changed |= ImGui.Checkbox("Let Me Resize", ref Configuration.UserResize);
                ImGuiComponents.HelpMarker("This allows you to resize the FC button size to stop clipping,\n" +
                                           "but stops them from automatically adjusting size.");
                ImGui.Unindent(10.0f);

                ImGuiHelpers.ScaledDummy(5.0f);

                ImGui.TextColored(ImGuiColors.DalamudViolet, "Overview:");
                ImGui.Indent(10.0f);
                changed |= ImGui.Checkbox("Show Repair Status", ref Configuration.ShowOnlyLowest);
                changed |= ImGui.Checkbox("Show Return Time", ref Configuration.ShowTimeInOverview);
                if (Configuration.ShowTimeInOverview)
                {
                    ImGui.Indent(10.0f);
                    changed |= ImGui.Checkbox("Show Return Date Instead", ref Configuration.UseDateTimeInstead);
                    changed |= ImGui.Checkbox("Show Both Options", ref Configuration.ShowBothOptions);
                    ImGui.Unindent(10.0f);
                }
                changed |= ImGui.Checkbox("Show Route", ref Configuration.ShowRouteInOverview);
                ImGui.Unindent(10.0f);

                ImGuiHelpers.ScaledDummy(5.0f);

                ImGui.TextColored(ImGuiColors.DalamudViolet, "Detailed View:");
                ImGui.Indent(10.0f);
                changed |= ImGui.Checkbox("Show Extended Parts List", ref Configuration.ShowExtendedPartsList);
                ImGui.Unindent(10.0f);

                if (changed)
                    Configuration.Save();

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Notify"))
            {
                ImGuiHelpers.ScaledDummy(5.0f);
                var changed = false;

                ImGui.TextColored(ImGuiColors.DalamudViolet, "Overlay:");
                ImGui.Indent(10.0f);
                changed |= ImGui.Checkbox("Show On Return", ref Configuration.NotifyOverlayAlways);
                changed |= ImGui.Checkbox("Show On Game Start", ref Configuration.NotifyOverlayOnStartup);
                ImGui.Unindent(10.0f);

                ImGui.TextColored(ImGuiColors.DalamudViolet, "Notifications:");
                ImGui.Indent(10.0f);
                changed |= ImGui.Checkbox("For Repairs", ref Configuration.NotifyForRepairs);
                if (Configuration.NotifyForRepairs)
                {
                    ImGui.Indent(10.0f);
                    changed |= ImGui.Checkbox("Show Repair Toast", ref Configuration.ShowRepairToast);
                    ImGui.Unindent(10.0f);
                }
                changed |= ImGui.Checkbox("For All Returns", ref Configuration.NotifyForAll);
                ImGui.Unindent(10.0f);

                if (!Configuration.NotifyForAll)
                {
                    ImGui.TextColored(ImGuiColors.DalamudViolet,"Only For Specific:");
                    ImGuiHelpers.ScaledDummy(5.0f);

                    if (ImGui.BeginChild("NotifyTable"))
                    {
                        ImGui.Indent(10.0f);
                        foreach (var (id, fc) in Submarines.KnownSubmarines)
                        {
                            foreach (var sub in fc.Submarines)
                            {
                                var key = $"{sub.Name}{id}";
                                Configuration.NotifySpecific.TryAdd($"{sub.Name}{id}", false);
                                var notify = Configuration.NotifySpecific[key];

                                var text = $"{sub.Name}@{fc.World}";
                                if (Configuration.UseCharacterName && fc.CharacterName != "")
                                    text = $"{sub.Name}@{fc.CharacterName}";

                                if (ImGui.Checkbox($"{text}##{id}{sub.Register}", ref notify))
                                {
                                    Configuration.NotifySpecific[key] = notify;
                                    Configuration.Save();
                                }
                            }

                            ImGuiHelpers.ScaledDummy(5.0f);
                        }

                        ImGui.Unindent(10.0f);
                    }
                    ImGui.EndChild();
                }

                if (changed)
                    Configuration.Save();

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("FCs"))
            {
                if (ImGui.BeginChild("FCContent", new Vector2(0, 0)))
                {
                    ImGuiHelpers.ScaledDummy(5.0f);
                    if (ImGui.BeginTable("##DeleteSavesTable", 4))
                    {
                        ImGui.TableSetupColumn("Saved FCs");
                        ImGui.TableSetupColumn("##OrderUp", 0, 0.1f);
                        ImGui.TableSetupColumn("##OrderDown", 0, 0.1f);
                        ImGui.TableSetupColumn("##Del", 0, 0.1f);

                        Plugin.EnsureFCOrderSafety();
                        ulong deletion = 0;
                        (int orgIdx, int newIdx) changedOrder = (0, 0);
                        foreach (var (id, idx) in Configuration.FCOrder.Select((val, i) => (val, i)))
                        {
                            var fc = Submarines.KnownSubmarines[id];
                            ImGui.TableNextColumn();

                            var text = $"{fc.Tag}@{fc.World}";
                            if (Configuration.UseCharacterName && fc.CharacterName != "")
                                text = $"{fc.CharacterName}@{fc.World}";

                            ImGui.TextUnformatted(text);

                            ImGui.TableNextColumn();
                            var first = Configuration.FCOrder.First() == id;
                            if (first) ImGui.BeginDisabled();
                            if (ImGuiComponents.IconButton($"##{id}Up", FontAwesomeIcon.ArrowUp))
                                changedOrder = (idx, idx - 1);
                            if (first) ImGui.EndDisabled();

                            ImGui.TableNextColumn();
                            var last = Configuration.FCOrder.Last() == id;
                            if (last) ImGui.BeginDisabled();
                            if (ImGuiComponents.IconButton($"##{id}Down", FontAwesomeIcon.ArrowDown))
                                changedOrder = (idx, idx + 1);
                            if (last) ImGui.EndDisabled();

                            ImGui.TableNextColumn();
                            if (ImGuiComponents.IconButton($"##{id}Del", FontAwesomeIcon.Trash) && ImGui.GetIO().KeyCtrl)
                                deletion = id;

                            if (ImGui.IsItemHovered())
                                ImGui.SetTooltip("Deleting an FC entry will additionally remove it's loot history.\nHold Control to delete");

                            ImGui.TableNextRow();
                        }

                        if (changedOrder.orgIdx != 0)
                        {
                            Configuration.FCOrder.Swap(changedOrder.orgIdx, changedOrder.newIdx);
                            Configuration.Save();
                        }

                        if (deletion != 0)
                        {
                            Configuration.FCOrder.Remove(deletion);
                            Configuration.Save();

                            Submarines.DeleteCharacter(deletion);
                        }

                        ImGui.EndTable();
                    }
                }
                ImGui.EndChild();


                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Builds"))
            {
                ImGuiHelpers.ScaledDummy(5.0f);

                if (ImGui.BeginTable("##DeleteBuildsTable", 2))
                {
                    ImGui.TableSetupColumn("Saved Builds");
                    ImGui.TableSetupColumn("Del", 0, 0.1f);

                    ImGui.TableHeadersRow();

                    var deletion = string.Empty;
                    foreach (var (key, build) in Configuration.SavedBuilds)
                    {
                        ImGui.TableNextColumn();
                        var text = FormattedRouteBuild(key, build).Split("\n");
                        ImGuiHelpers.SafeTextWrapped(text.First());
                        ImGui.TextColored(ImGuiColors.ParsedOrange, text.Last());

                        ImGui.TableNextColumn();
                        if (ImGuiComponents.IconButton(key, FontAwesomeIcon.Trash))
                            deletion = key;

                        ImGui.TableNextRow();
                    }
                    ImGui.EndTable();

                    if (deletion != string.Empty)
                    {
                        Configuration.SavedBuilds.Remove(deletion);
                        Configuration.Save();
                    }
                }

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Loot"))
            {
                ImGuiHelpers.ScaledDummy(5.0f);

                var buttonWidth = ImGui.GetContentRegionAvail().X / 2;
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), new Vector2(buttonWidth, 0));
                ImGui.PopFont();

                if (ExcelSheetSelector.ExcelSheetPopup("ItemAddPopup", out var row, ItemPopupOptions))
                {
                    var item = ItemSheet.GetRow(row)!;
                    var value = (int) (item.PriceLow > 1000 ? item.PriceLow : 0);

                    if (Configuration.CustomLootWithValue.TryAdd(row, value))
                        Configuration.Save();
                }

                ImGuiHelpers.ScaledDummy(5);
                ImGui.Separator();
                ImGuiHelpers.ScaledDummy(5);

                if (ImGui.BeginTable("##DeleteLootTable", 3))
                {
                    ImGui.TableSetupColumn("Item");
                    ImGui.TableSetupColumn("Value", 0, 0.4f);
                    ImGui.TableSetupColumn("Del", 0, 0.15f);

                    ImGui.TableHeadersRow();

                    uint deletionKey = 0;
                    foreach (var ((item, value), idx) in Configuration.CustomLootWithValue.Select((val, i) => (val, i)))
                    {
                        var resolvedItem = ItemSheet.GetRow(item)!;
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{ToStr(resolvedItem.Name)}");

                        ImGui.TableNextColumn();
                        var val = value;
                        ImGui.SetNextItemWidth(-1);
                        if (ImGui.InputInt($"##inputValue{item}", ref val, 0))
                        {
                            val = Math.Clamp(val, 0, int.MaxValue);
                            Configuration.CustomLootWithValue[item] = val;
                            Configuration.Save();
                        }

                        ImGui.TableNextColumn();
                        if (ImGuiComponents.IconButton(idx, FontAwesomeIcon.Trash))
                            deletionKey = item;

                        ImGui.TableNextRow();
                    }

                    if (deletionKey != 0)
                    {
                        Configuration.CustomLootWithValue.Remove(deletionKey);
                        Configuration.Save();
                    }

                    ImGui.EndTable();
                }

                ImGuiHelpers.ScaledDummy(5);
                ImGui.Separator();
                ImGuiHelpers.ScaledDummy(5);

                ImGui.TextColored(ImGuiColors.DalamudViolet, "Time Frame:");
                if(ImGui.BeginCombo($"##lootOptionCombo", DateUtil.GetDateLimitName(Configuration.DateLimit)))
                {
                    foreach(var dateLimit in (DateLimit[]) Enum.GetValues(typeof(DateLimit)))
                    {
                        if(ImGui.Selectable(DateUtil.GetDateLimitName(dateLimit)))
                        {
                            Configuration.DateLimit = dateLimit;
                            Configuration.Save();
                        }
                    }

                    ImGui.EndCombo();
                }

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("About"))
            {
                if (ImGui.BeginChild("AboutContent", new Vector2(0, -50)))
                {
                    ImGuiHelpers.ScaledDummy(5.0f);

                    ImGui.TextUnformatted("Author:");
                    ImGui.SameLine();
                    ImGui.TextColored(ImGuiColors.ParsedGold, Plugin.Authors);

                    ImGui.TextUnformatted("Discord:");
                    ImGui.SameLine();
                    ImGui.TextColored(ImGuiColors.ParsedGold, "Infi#6958");

                    ImGui.TextUnformatted("Version:");
                    ImGui.SameLine();
                    ImGui.TextColored(ImGuiColors.ParsedOrange, Plugin.Version);
                }
                ImGui.EndChild();

                ImGuiHelpers.ScaledDummy(5);
                ImGui.Separator();
                ImGuiHelpers.ScaledDummy(5);

                if (ImGui.BeginChild("AboutBottomBar", new Vector2(0, 0), false, 0))
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedBlue);
                    if (ImGui.Button("Discord Thread"))
                        Dalamud.Utility.Util.OpenLink("https://canary.discord.com/channels/581875019861328007/1094255662860599428");
                    ImGui.PopStyleColor();

                    ImGui.SameLine();

                    ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.DPSRed);
                    if (ImGui.Button("Issues"))
                        Dalamud.Utility.Util.OpenLink("https://github.com/Infiziert90/SubmarineTracker/issues");
                    ImGui.PopStyleColor();
                }
                ImGui.EndChild();

                ImGui.EndTabItem();
            }
        }
        ImGui.EndTabBar();
    }
}
