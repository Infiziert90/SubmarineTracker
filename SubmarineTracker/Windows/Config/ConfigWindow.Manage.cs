using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
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
                if (ImGui.BeginTable("##DeleteSavesTable", 5, ImGuiTableFlags.BordersH))
                {
                    ImGui.TableSetupColumn(Loc.Localize("Terms - Saved FCs", "Saved FCs"));
                    ImGui.TableSetupColumn("##OrderUp", 0, 0.07f);
                    ImGui.TableSetupColumn("##OrderDown", 0, 0.07f);
                    ImGui.TableSetupColumn("##Hidden", 0, 0.07f);
                    ImGui.TableSetupColumn("##Del", 0, 0.09f);

                    ImGui.TableHeadersRow();

                    Plugin.EnsureFCOrderSafety();
                    (int DelIdx, ulong FCId) deletion = (-1, 0);
                    (int OrgIdx, int NewIdx) changedOrder = (-1, -1);
                    (int Idx, (ulong, bool) Status) changedStatus = (-1, (0, false));

                    var firstFC = Plugin.Configuration.ManagedFCs.First();
                    var lastFC = Plugin.Configuration.ManagedFCs.Last();
                    foreach (var ((id, hidden), idx) in Plugin.Configuration.ManagedFCs.Select((val, i) => (val, i)))
                    {
                        var fc = Plugin.DatabaseCache.GetFreeCompanies()[id];
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(Plugin.NameConverter.GetCombinedName(fc));

                        ImGui.TableNextColumn();
                        if (Helper.Button($"##{id}Up", FontAwesomeIcon.ArrowUp, firstFC.Id == id))
                            changedOrder = (idx, idx - 1);

                        ImGui.TableNextColumn();
                        if (Helper.Button($"##{id}Down", FontAwesomeIcon.ArrowDown, lastFC.Id == id))
                            changedOrder = (idx, idx + 1);

                        ImGui.TableNextColumn();
                        if (Helper.Button($"##{id}Hide", hidden ? FontAwesomeIcon.EyeSlash : FontAwesomeIcon.Eye))
                            changedStatus = (idx, (id, !hidden));

                        ImGui.TableNextColumn();
                        if (Helper.Button($"##{id}Del", FontAwesomeIcon.Trash, !ImGui.GetIO().KeyCtrl))
                            deletion = (idx, id);

                        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                            ImGui.SetTooltip(Loc.Localize("Config Tab Tooltip - Saved FCs Deletion", "Deleting an FC entry will additionally remove all of its loot history.\nHold Control to delete"));

                        if (lastFC.Id != id)
                            ImGui.TableNextRow();
                    }

                    if (changedOrder.OrgIdx != -1)
                    {
                        Plugin.Configuration.ManagedFCs.Swap(changedOrder.OrgIdx, changedOrder.NewIdx);
                        Plugin.Configuration.Save();
                    }

                    if (changedStatus.Idx != -1)
                    {
                        Plugin.Configuration.ManagedFCs[changedStatus.Idx] = changedStatus.Status;
                        Plugin.Configuration.Save();
                    }

                    if (deletion.DelIdx != -1)
                    {
                        Plugin.Configuration.ManagedFCs.RemoveAt(deletion.DelIdx);
                        Plugin.Configuration.Save();

                        var ok = Plugin.DatabaseCache.Database.DeleteFreeCompany(deletion.FCId);
                        if (!ok)
                        {
                            Plugin.Notification.AddNotification(new Notification
                            {
                                Content = Loc.Localize("Error - Deletion Failed", "Unable to delete this entry, report this error to the author"),
                                Type = NotificationType.Error,
                                Minimized = false,
                            });
                        }
                    }

                    ImGui.EndTable();
                }

                ImGuiHelpers.ScaledDummy(5.0f);
                if (ImGui.BeginTable("##IgnoredCharacters", 2, ImGuiTableFlags.BordersH))
                {
                    ImGui.TableSetupColumn(Loc.Localize("Terms - Ignored Characters", "Ignored Characters"));
                    ImGui.TableSetupColumn("##CharacterDel", 0, 0.07f);

                    ImGui.TableHeadersRow();

                    var ignoredCharacters = Plugin.Configuration.IgnoredCharacters.ToArray();
                    foreach (var (id, name) in ignoredCharacters)
                    {
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(Plugin.Configuration.NameOption == NameOptions.Anon ? Utils.GenerateHashedName(name) : name);

                        ImGui.TableNextColumn();
                        if (Helper.Button($"##{id}CharacterDel", FontAwesomeIcon.Trash, !ImGui.GetIO().KeyCtrl))
                            Plugin.Configuration.IgnoredCharacters.Remove(id);

                        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                            ImGui.SetTooltip(Loc.Localize("Config Tab Tooltip - Ignored Character Delete", "Hold Control to delete"));

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

                            Plugin.Configuration.IgnoredCharacters.Add(Plugin.ClientState.LocalContentId, $"({tag}) {name}@{world}");
                            Plugin.Configuration.Save();
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
