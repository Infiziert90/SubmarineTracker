using Dalamud.Interface;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private void Manage()
    {
        if (ImGui.BeginTabItem($"{Loc.Localize("Config Tab - Manage", "Manage")}##Manage"))
        {
            if (ImGui.BeginChild("FCContent", new Vector2(0, 0)))
            {
                ImGuiHelpers.ScaledDummy(5.0f);
                if (ImGui.BeginTable("##DeleteSavesTable", 4, ImGuiTableFlags.BordersH))
                {
                    ImGui.TableSetupColumn(Loc.Localize("Terms - Saved FCs", "Saved FCs"));
                    ImGui.TableSetupColumn("##OrderUp", 0, 0.05f);
                    ImGui.TableSetupColumn("##OrderDown", 0, 0.05f);
                    ImGui.TableSetupColumn("##Del", 0, 0.07f);

                    ImGui.TableHeadersRow();

                    Plugin.EnsureFCOrderSafety();
                    ulong deletion = 0;
                    (int orgIdx, int newIdx) changedOrder = (-1, -1);
                    foreach (var (id, idx) in Configuration.FCOrder.Select((val, i) => (val, i)))
                    {
                        var fc = Submarines.KnownSubmarines[id];
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(Helper.GetCombinedName(fc));

                        var first = Configuration.FCOrder.First() == id;
                        var last = Configuration.FCOrder.Last() == id;

                        ImGui.TableNextColumn();
                        if (Helper.Button($"##{id}Up", FontAwesomeIcon.ArrowUp, first))
                            changedOrder = (idx, idx - 1);

                        ImGui.TableNextColumn();
                        if (Helper.Button($"##{id}Down", FontAwesomeIcon.ArrowDown, last))
                            changedOrder = (idx, idx + 1);

                        ImGui.TableNextColumn();
                        if (Helper.Button($"##{id}Del", FontAwesomeIcon.Trash, !ImGui.GetIO().KeyCtrl))
                            deletion = id;

                        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                            ImGui.SetTooltip(Loc.Localize("Config Tab Tooltip - Saved FCs Deletion", "Deleting an FC entry will additionally remove all of its loot history.\nHold Control to delete"));

                        if (!last)
                            ImGui.TableNextRow();
                    }

                    if (changedOrder.orgIdx != -1)
                    {
                        Configuration.FCOrder.Swap(changedOrder.orgIdx, changedOrder.newIdx);
                        Configuration.Save();
                    }

                    if (deletion != 0)
                    {
                        Configuration.FCOrder.Remove(deletion);
                        Configuration.Save();

                        Plugin.ConfigurationBase.DeleteCharacter(deletion);
                    }

                    ImGui.EndTable();
                }

                ImGuiHelpers.ScaledDummy(5.0f);
                if (ImGui.BeginTable("##IgnoredCharacters", 2, ImGuiTableFlags.BordersH))
                {
                    ImGui.TableSetupColumn(Loc.Localize("Terms - Ignored Characters", "Ignored Characters"));
                    ImGui.TableSetupColumn("##CharacterDel", 0, 0.07f);

                    ImGui.TableHeadersRow();

                    var ignoredCharacters = Configuration.IgnoredCharacters.ToArray();
                    foreach (var (id, name) in ignoredCharacters)
                    {
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(Configuration.AnonNames ? Utils.GenerateHashedName(name) : name);

                        ImGui.TableNextColumn();
                        if (Helper.Button($"##{id}CharacterDel", FontAwesomeIcon.Trash, !ImGui.GetIO().KeyCtrl))
                            Configuration.IgnoredCharacters.Remove(id);

                        ImGui.TableNextRow();
                    }

                    ImGui.TableNextColumn();
                    if (ImGui.Button(Loc.Localize("Terms - Add Current Character", "Add Current Character")))
                    {
                        var local = Plugin.ClientState.LocalPlayer;
                        if (local != null)
                        {
                            var name = Utils.ToStr(local.Name);
                            var tag = Utils.ToStr(local.CompanyTag);
                            var world = Utils.ToStr(local.HomeWorld.GameData!.Name);

                            Configuration.IgnoredCharacters.Add(Plugin.ClientState.LocalContentId, $"({tag}) {name}@{world}");
                            Configuration.Save();
                        }
                    }

                    ImGui.EndTable();
                }
            }
            ImGui.EndChild();

            ImGui.EndTabItem();
        }
    }
}
