using Dalamud.Interface.Components;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private void Order()
    {
        if (ImGui.BeginTabItem("Order"))
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
                            ImGui.SetTooltip(
                                "Deleting an FC entry will additionally remove it's loot history.\nHold Control to delete");

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

                        Plugin.ConfigurationBase.DeleteCharacter(deletion);
                    }

                    ImGui.EndTable();
                }
            }

            ImGui.EndChild();


            ImGui.EndTabItem();
        }
    }
}
