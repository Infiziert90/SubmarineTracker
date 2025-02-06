using System.Text.RegularExpressions;
using Dalamud.Interface;
using Dalamud.Interface.Components;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private bool ValidRegex;

    private void InitializeNotify()
    {
        ValidRegex = WebhookRegex().IsMatch(Plugin.Configuration.WebhookUrl);
    }

    private void Notify()
    {
        using var tabItem = ImRaii.TabItem($"{Loc.Localize("Config Tab - Notify", "Notify")}##Notify");
        if (!tabItem.Success)
            return;

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

        changed |= ImGui.Checkbox("Use Offline Mode", ref Plugin.Configuration.WebhookOfflineMode);
        using (ImRaii.PushColor(ImGuiCol.Header, ImGuiColors.ParsedPurple))
        {
            if (ImGui.CollapsingHeader("Offline Mode Explanation"))
            {
                using var indent = ImRaii.PushIndent(5.0f);
                Helper.TextWrapped("Offline mode allows you to receive return notifications while the game client is closed. For this feature to work, some data has to be uploaded to a server for processing.");
                ImGuiHelpers.ScaledDummy(5.0f);
                Helper.TextColored(ImGuiColors.DalamudViolet, "What Data?");
                Helper.BulletText("Discord Webhook Url");
                Helper.BulletText("Submarine + FC Name [Naming Convention]");
                Helper.BulletText("Return Time");
                Helper.BulletText("Discord UserId [If set]");
                ImGuiHelpers.ScaledDummy(5.0f);
                Helper.TextColored(ImGuiColors.DalamudViolet, "Data Lifetime");
                Helper.TextWrapped("All uploaded data is removed from the database 10 minutes before the return time is reached. At this stage, all remaining memory data will automatically wiped after the return notification has been sent.");
                ImGuiHelpers.ScaledDummy(5.0f);
                Helper.TextColored(ImGuiColors.DalamudViolet, "Important Note");
                Helper.TextWrapped("For this feature to work, general upload permissions must have been granted.");
            }
        }

        using (ImRaii.PushColor(ImGuiCol.Header, ImGuiColors.ParsedPurple))
        {
            if (ImGui.CollapsingHeader("Help"))
            {
                using var indent = ImRaii.PushIndent(5.0f);
                Helper.TextWrapped("Need help creating the webhook url?");
                ImGui.AlignTextToFramePadding();
                Helper.TextWrapped("Click the button:");
                ImGui.SameLine();
                Helper.UrlButton("##webhookGuide", FontAwesomeIcon.QuestionCircle,
                                 "https://support.discord.com/hc/en-us/articles/228383668-Intro-to-Webhooks",
                                 Loc.Localize("Config Tab Tooltip - Webhook",
                                              "Click to open discord webhook guide in your browser."));

                Helper.TextWrapped("Need help finding your discord user id?");
                ImGui.AlignTextToFramePadding();
                Helper.TextWrapped("Click the button:");
                ImGui.SameLine();
                Helper.UrlButton("##userIdGuide", FontAwesomeIcon.QuestionCircle,
                                 "https://support.discord.com/hc/en-us/articles/206346498-Where-can-I-find-my-User-Server-Message-ID#h_01HRSTXPS5H5D7JBY2QKKPVKNA",
                                 "Click to open discord user id guide in your browser.");
            }
        }

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Discord Webhook Url:");
        changed |= ImGui.InputText("##Url", ref Plugin.Configuration.WebhookUrl, 255);

        if (!ValidRegex)
            ImGui.TextColored(ImGuiColors.DPSRed, "Url is not a valid discord webhook.");

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("UserId For Ping:");
        var mentionId = Plugin.Configuration.WebhookMention.ToString();
        if (ImGui.InputText("##MentionId", ref mentionId, 18, ImGuiInputTextFlags.CharsDecimal))
        {
            if (ulong.TryParse(mentionId, out var id))
            {
                Plugin.Configuration.WebhookMention = id;
                changed = true;
            }
        }

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

            using var child = ImRaii.Child("##NotifyTable", new Vector2(0, 400 * ImGuiHelpers.GlobalScale), true);
            if (child.Success)
            {
                ImGuiHelpers.ScaledIndent(10.0f);
                foreach (var (id, fc) in Plugin.DatabaseCache.GetFreeCompanies())
                {
                    foreach (var sub in Plugin.DatabaseCache.GetSubmarines(id))
                    {
                        var key = $"{sub.Name}{id}";
                        Plugin.Configuration.NotifyFCSpecific.TryAdd(key, false);
                        var notify = Plugin.Configuration.NotifyFCSpecific[key];

                        if (ImGui.Checkbox($"{Plugin.NameConverter.GetSub(sub, fc)}##{id}{sub.Register}", ref notify))
                        {
                            Plugin.Configuration.NotifyFCSpecific[key] = notify;
                            Plugin.Configuration.Save();
                        }
                    }

                    ImGuiHelpers.ScaledDummy(5.0f);
                }
                ImGuiHelpers.ScaledIndent(-10.0f);
            }
        }

        if (changed)
        {
            ValidRegex = WebhookRegex().IsMatch(Plugin.Configuration.WebhookUrl);
            Plugin.Configuration.Save();
        }
    }

    [GeneratedRegex(@"^.*(discord|discordapp)\.com\/api\/webhooks\/([\d]+)\/([a-z0-9_-]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    public static partial Regex WebhookRegex();
}
