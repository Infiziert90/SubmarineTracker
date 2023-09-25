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
            ImGui.Indent(10.0f);
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Returning Sub", "Returning Sub"), ref Configuration.NotifyForReturns);
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Needed Repair", "Needed Repair"), ref Configuration.NotifyForRepairs);
            if (Configuration.NotifyForRepairs)
            {
                ImGui.Indent(10.0f);
                changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Repair Toast", "Show Repair Toast"), ref Configuration.ShowRepairToast);
                ImGui.Unindent(10.0f);
            }
            ImGui.Unindent(10.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Webhook", "Webhook:"));
            ImGui.Indent(10.0f);
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Send Dispatch", "Send Dispatch"), ref Configuration.WebhookDispatch);
            ImGuiComponents.HelpMarker(Loc.Localize("Config Tab Tooltip - Send Dispatch", "Sends a webhook message on dispatch, containing a timestamp when this submarine will return."));
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - Send Return", "Send Return"), ref Configuration.WebhookReturn);
            ImGuiComponents.HelpMarker(Loc.Localize("Config Tab Tooltip - Send Return", "Sends a webhook message on return."));
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(Loc.Localize("Terms - URL", "URL"));
            ImGui.SameLine();
            changed |= ImGui.InputText("##Url", ref Configuration.WebhookUrl, 255);
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
            ImGui.Unindent(10.0f);

            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Submarines", "Submarines:"));
            ImGui.Indent(10.0f);
            changed |= ImGui.Checkbox(Loc.Localize("Config Tab Checkbox - All Returning", "All Returning Subs"), ref Configuration.NotifyForAll);
            ImGui.Unindent(10.0f);

            if (!Configuration.NotifyForAll)
            {
                ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Config Tab Entry - Specific Submarines", "Specific Submarines:"));
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

                            if (ImGui.Checkbox($"{Helper.GetSubName(sub, fc)}##{id}{sub.Register}", ref notify))
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
    }
}
