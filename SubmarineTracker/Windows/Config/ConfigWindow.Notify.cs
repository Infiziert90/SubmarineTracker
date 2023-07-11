using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private void Notify()
    {
        if (ImGui.BeginTabItem("Notify"))
        {
            ImGuiHelpers.ScaledDummy(5.0f);
            var changed = false;

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
                ImGui.TextColored(ImGuiColors.DalamudViolet, "Only For Specific:");
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
    }
}
