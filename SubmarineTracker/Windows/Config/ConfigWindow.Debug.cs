using System.Threading.Tasks;
using static SubmarineTracker.Data.Submarines;

namespace SubmarineTracker.Windows.Config;

public partial class ConfigWindow
{
    private static readonly Submarine TestSub = new() { Name = "Apollo 11", ReturnTime = new DateTime(1969, 7, 21, 3, 15, 16) };
    private static readonly FcSubmarines TestFC = new() { CharacterName = "Buzz Aldrin", World = "Moon" };

    private void Debug()
    {
        if (ImGui.BeginTabItem("Debug"))
        {
            ImGui.TextColored(ImGuiColors.DalamudViolet, "Notifications:");
            ImGui.Indent(10.0f);
            if (ImGui.Button("Send Return"))
                Plugin.Notify.SendReturn(TestSub, TestFC);

            if (ImGui.Button("Send Repair"))
                Plugin.Notify.SendRepair(TestSub, TestFC);

            if (ImGui.Button("Dispatch Webhook"))
                Plugin.Notify.SendDispatchWebhook(TestSub, TestFC, (uint) ((DateTimeOffset) DateTime.Now.AddMinutes(10).ToUniversalTime()).ToUnixTimeSeconds());

            if (ImGui.Button("Return Webhook"))
                Task.Run(() => Plugin.Notify.SendReturnWebhook(TestSub, TestFC));
            ImGui.Unindent(10.0f);

            ImGui.EndTabItem();
        }
    }
}
