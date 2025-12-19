using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using SubmarineTracker.Resources;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private void Manage()
    {
        using var tabItem = ImRaii.TabItem($"{Language.ConfigTabManage}##Manage");
        if (!tabItem.Success)
            return;

        using var child = ImRaii.Child("FCContent", Vector2.Zero);
        if (!child.Success)
            return;

        FCManagingTable();

        ImGuiHelpers.ScaledDummy(5.0f);

        CharacterManagingTable();
    }

    private void FCManagingTable()
    {
        using var savesTable = ImRaii.Table("##DeleteSavesTable", 5, ImGuiTableFlags.BordersH);
        if (savesTable.Success)
        {
            ImGui.TableSetupColumn(Language.TermsSavedFCs);
            ImGui.TableSetupColumn("##OrderUp", 0, 0.07f);
            ImGui.TableSetupColumn("##OrderDown", 0, 0.07f);
            ImGui.TableSetupColumn("##Hidden", 0, 0.07f);
            ImGui.TableSetupColumn("##Del", 0, 0.09f);

            ImGui.TableHeadersRow();

            Plugin.EnsureFCOrderSafety();
            if (Plugin.Configuration.ManagedFCs.Count == 0)
                return;

            (int DelIdx, ulong FCId) deletion = (-1, 0);
            (int OrgIdx, int NewIdx) changedOrder = (-1, -1);
            (int Idx, (ulong, bool) Status) changedStatus = (-1, (0, false));

            var firstFC = Plugin.Configuration.ManagedFCs.First();
            var lastFC = Plugin.Configuration.ManagedFCs.Last();
            foreach (var ((id, hidden), idx) in Plugin.Configuration.ManagedFCs.WithIndex())
            {
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Plugin.NameConverter.GetCombinedName(Plugin.DatabaseCache.GetFreeCompanies()[id]));

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
                    Helper.Tooltip(Language.ConfigTabTooltipSavedFCsDeletion);

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

                if (!Plugin.DatabaseCache.Database.DeleteFreeCompany(deletion.FCId))
                    Utils.AddNotification(Language.ErrorDeletionFailed, NotificationType.Error, false);
            }
        }
    }

    private void CharacterManagingTable()
    {
        using var charactersTable = ImRaii.Table("##IgnoredCharacters", 2, ImGuiTableFlags.BordersH);
        if (charactersTable.Success)
        {
            ImGui.TableSetupColumn(Language.TermsIgnoredCharacters);
            ImGui.TableSetupColumn("##CharacterDel", 0, 0.07f);

            ImGui.TableHeadersRow();
            foreach (var (id, name) in Plugin.Configuration.IgnoredCharacters.ToArray())
            {
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(Plugin.NameConverter.GetCharacterName(name));

                ImGui.TableNextColumn();
                if (Helper.Button($"##{id}CharacterDel", FontAwesomeIcon.Trash, !ImGui.GetIO().KeyCtrl))
                {
                    Plugin.Configuration.IgnoredCharacters.Remove(id);
                    Plugin.Configuration.Save();
                }

                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    Helper.Tooltip(Language.ConfigTabTooltipIgnoredCharacterDelete);

                ImGui.TableNextRow();
            }

            ImGui.TableNextColumn();
            if (ImGui.Button(Language.TermsAddCurrentCharacter))
            {
                var local = Plugin.ObjectTable.LocalPlayer;
                if (local != null)
                {
                    var name = local.Name.TextValue;
                    var tag = local.CompanyTag.TextValue;
                    var world = local.HomeWorld.Value.Name.ExtractText();

                    Plugin.Configuration.IgnoredCharacters.Add(Plugin.PlayerState.ContentId, $"({tag}) {name}@{world}");
                    Plugin.Configuration.Save();
                }
            }
        }
    }
}
