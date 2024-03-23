using Dalamud.Interface;
using Dalamud.Interface.Components;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private void Notify()
    {
        if (ImGui.BeginTabItem($"{Loc.Localize("Config Tab - Notify", "Notify")}##Notify"))
        {
            var changed = false;
            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Notifications", "Notifications:"));
            ImGuiHelpers.ScaledIndent(10.0f);
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Returning Sub", "Returning Sub"), ref Plugin.Configuration.NotifyForReturns);
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Needed Repair", "Needed Repair"), ref Plugin.Configuration.NotifyForRepairs);
            if (Plugin.Configuration.NotifyForRepairs)
            {
                ImGuiHelpers.ScaledIndent(10.0f);
                changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Repair Toast", "Show Repair Toast"), ref Plugin.Configuration.ShowRepairToast);
                ImGuiHelpers.ScaledIndent(-10.0f);
            }
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Show Storage Message", "Show Storage Message"), ref Plugin.Configuration.ShowStorageMessage);
            ImGuiComponents.HelpMarker(Loc.Localize("Config Tab Tooltip - Show Storage Message", "Show a message whenever you enter the workshop, informing you about your tank and repair kit status"));
            ImGuiHelpers.ScaledIndent(-10.0f);

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Webhook", "Webhook:"));
            ImGuiHelpers.ScaledIndent(10.0f);
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Send Dispatch", "Send Dispatch"), ref Plugin.Configuration.WebhookDispatch);
            ImGuiComponents.HelpMarker(Loc.Localize("Config Tab Tooltip - Send Dispatch", "Sends a webhook message on dispatch, containing a timestamp when this submarine will return."));
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Send Return", "Send Return"), ref Plugin.Configuration.WebhookReturn);
            ImGuiComponents.HelpMarker(Loc.Localize("Config Tab Tooltip - Send Return", "Sends a webhook message on return."));
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(Loc.Localize("Terms - URL", "URL"));
            ImGui.SameLine();
            changed |= ImGui.InputText("##Url", ref Plugin.Configuration.WebhookUrl, 255);
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.QuestionCircle))
            {
                try {
                    Dalamud.Utility.Util.OpenLink("https://support.discord.com/hc/en-us/articles/228383668-Intro-to-Webhooks");
                } catch {
                    // ignored
                }
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Loc.Localize("Config Tab Tooltip - Webhook", "Click to open discord webhook guide in your browser."));
            ImGuiHelpers.ScaledIndent(-10.0f);

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Submarines", "Submarines:"));
            ImGuiHelpers.ScaledIndent(10.0f);
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - All Returning", "All Returning Subs"), ref Plugin.Configuration.NotifyForAll);
            ImGuiHelpers.ScaledIndent(-10.0f);

            if (!Plugin.Configuration.NotifyForAll)
            {
                ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Specific Submarines", "Specific Submarines:"));
                ImGuiHelpers.ScaledDummy(5.0f);

                if (ImGui.BeginChild("NotifyTable"))
                {
                    ImGuiHelpers.ScaledIndent(10.0f);
                    foreach (var (id, fc) in Submarines.KnownSubmarines)
                    {
                        foreach (var sub in fc.Submarines)
                        {
                            var key = $"{sub.Name}{id}";
                            Plugin.Configuration.NotifySpecific.TryAdd($"{sub.Name}{id}", false);
                            var notify = Plugin.Configuration.NotifySpecific[key];

                            if (ImGui.Checkbox($"{Plugin.NameConverter.GetSub(sub, fc)}##{id}{sub.Register}", ref notify))
                            {
                                Plugin.Configuration.NotifySpecific[key] = notify;
                                Plugin.Configuration.Save();
                            }
                        }

                        ImGuiHelpers.ScaledDummy(5.0f);
                    }

                    ImGuiHelpers.ScaledIndent(-10.0f);
                }

                ImGui.EndChild();
            }

            if (changed)
                Plugin.Configuration.Save();

            ImGui.EndTabItem();
        }
    }
}
