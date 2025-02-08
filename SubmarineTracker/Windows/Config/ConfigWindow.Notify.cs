using System.Text.RegularExpressions;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using SubmarineTracker.Resources;

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
        using var tabItem = ImRaii.TabItem($"{Language.ConfigTabNotify}##Notify");
        if (!tabItem.Success)
            return;

        var changed = false;
        ImGuiHelpers.ScaledDummy(5.0f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabEntryNotifications);
        using (ImRaii.PushIndent(10.0f))
        {

        }
        changed |= ImGui.Checkbox(Language.ConfigTabCheckboxReturningSub, ref Plugin.Configuration.NotifyForReturns);
        changed |= ImGui.Checkbox(Language.ConfigTabCheckboxNeededRepair, ref Plugin.Configuration.NotifyForRepairs);
        if (Plugin.Configuration.NotifyForRepairs)
        {
            using var indent = ImRaii.PushIndent(10.0f);
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxRepairToast, ref Plugin.Configuration.ShowRepairToast);
        }
        changed |= ImGui.Checkbox(Language.ConfigTabCheckboxShowStorageMessage, ref Plugin.Configuration.ShowStorageMessage);
        ImGuiComponents.HelpMarker(Language.ConfigTabTooltipShowStorageMessage);
        if (Plugin.Configuration.ShowStorageMessage)
        {
            using var indent = ImRaii.PushIndent(10.0f);
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxShowStorageStartup, ref Plugin.Configuration.ShowStorageAtStartup);
        }

        ImGuiHelpers.ScaledDummy(5.0f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabEntryWebhook);
        using (ImRaii.PushIndent(10.0f))
        {
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxSendDispatch, ref Plugin.Configuration.WebhookDispatch);
            ImGuiComponents.HelpMarker(Language.ConfigTabTooltipSendDispatch);
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxSendReturn, ref Plugin.Configuration.WebhookReturn);
            ImGuiComponents.HelpMarker(Language.ConfigTabTooltipSendReturn);

            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxOfflineMode, ref Plugin.Configuration.WebhookOfflineMode);
            using (ImRaii.PushColor(ImGuiCol.Header, ImGuiColors.ParsedPurple))
            {
                if (ImGui.CollapsingHeader(Language.ConfigTabHeaderOfflineLabel))
                {
                    using var indent = ImRaii.PushIndent(5.0f);
                    Helper.TextWrapped(Language.ConfigTabOfflineModeText1);

                    ImGuiHelpers.ScaledDummy(5.0f);

                    Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabOfflineModeText2);
                    Helper.BulletText(Language.ConfigTabOfflineModeText3);
                    Helper.BulletText(Language.ConfigTabOfflineModeText4);
                    Helper.BulletText(Language.ConfigTabOfflineModeText5);
                    Helper.BulletText(Language.ConfigTabOfflineModeText6);
                    Helper.BulletText(Language.ConfigTabOfflineModeText7);

                    ImGuiHelpers.ScaledDummy(5.0f);

                    Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabOfflineModeText8);
                    Helper.TextWrapped(Language.ConfigTabOfflineModeText9);

                    ImGuiHelpers.ScaledDummy(5.0f);

                    Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabOfflineModeText10);
                    Helper.TextWrapped(Language.ConfigTabOfflineModeText11);
                }
            }

            using (ImRaii.PushColor(ImGuiCol.Header, ImGuiColors.ParsedPurple))
            {
                if (ImGui.CollapsingHeader(Language.ConfigTabHelpHeader))
                {
                    using var indent = ImRaii.PushIndent(5.0f);
                    Helper.TextWrapped(Language.ConfigTabHelpText1);
                    ImGui.AlignTextToFramePadding();
                    Helper.TextWrapped(Language.ConfigTabHelpClickButton);
                    ImGui.SameLine();
                    Helper.UrlButton("##webhookGuide", FontAwesomeIcon.QuestionCircle,
                                     "https://support.discord.com/hc/en-us/articles/228383668-Intro-to-Webhooks",
                                     Language.ConfigTabTooltipWebhook);

                    Helper.TextWrapped(Language.ConfigTabHelpText2);
                    ImGui.AlignTextToFramePadding();
                    Helper.TextWrapped(Language.ConfigTabHelpClickButton);
                    ImGui.SameLine();
                    Helper.UrlButton("##userIdGuide", FontAwesomeIcon.QuestionCircle,
                                     "https://support.discord.com/hc/en-us/articles/206346498-Where-can-I-find-my-User-Server-Message-ID#h_01HRSTXPS5H5D7JBY2QKKPVKNA",
                                     Language.ConfigTabTooltipUserId);

                    Helper.TextWrapped(Language.ConfigTabHelpText3);
                    Helper.TextWrapped(Language.ConfigTabHelpText4);
                    ImGui.AlignTextToFramePadding();
                    Helper.TextWrapped(Language.ConfigTabHelpClickButton);
                    ImGui.SameLine();
                    Helper.UrlButton("##roleIdGuide", FontAwesomeIcon.QuestionCircle,
                                     "https://discordhelp.net/role-id",
                                     Language.ConfigTabTooltipRoleId);
                }
            }

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(Language.ConfigTabHeaderWebhookUrl);
            changed |= ImGui.InputText("##Url", ref Plugin.Configuration.WebhookUrl, 255);

            if (Plugin.Configuration.WebhookOfflineMode && !ValidRegex)
                Helper.TextColored(ImGuiColors.DPSRed, Language.ConfigTabErrorInvalidWebhook);

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(Language.ConfigTabHeaderUserId);
            var mentionId = Plugin.Configuration.WebhookMention.ToString();
            if (ImGui.InputText("##MentionId", ref mentionId, 18, ImGuiInputTextFlags.CharsDecimal))
            {
                if (ulong.TryParse(mentionId, out var id))
                {
                    Plugin.Configuration.WebhookMention = id;
                    changed = true;
                }
            }

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(Language.ConfigTabHeaderRoleId);
            var roleMentionId = Plugin.Configuration.WebhookRoleMention.ToString();
            if (ImGui.InputText("##RoleMentionId", ref roleMentionId, 19, ImGuiInputTextFlags.CharsDecimal))
            {
                if (ulong.TryParse(roleMentionId, out var id))
                {
                    Plugin.Configuration.WebhookRoleMention = id;
                    changed = true;
                }
            }
        }

        ImGuiHelpers.ScaledDummy(5.0f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabEntrySubmarines);
        using (ImRaii.PushIndent(10.0f))
            changed |= ImGui.Checkbox(Language.ConfigTabCheckboxAllReturning, ref Plugin.Configuration.NotifyForAll);

        if (!Plugin.Configuration.NotifyForAll)
        {
            Helper.TextColored(ImGuiColors.DalamudViolet, Language.ConfigTabEntrySpecificSubmarines);
            ImGuiHelpers.ScaledDummy(5.0f);

            using var child = ImRaii.Child("##NotifyTable", new Vector2(0, 400 * ImGuiHelpers.GlobalScale), true);
            if (child.Success)
            {
                using var indent = ImRaii.PushIndent(10.0f);
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
