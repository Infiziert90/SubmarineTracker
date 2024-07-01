using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.EventArgs;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace SubmarineTracker.Windows.Migration;

public class MigrationWindow : Window, IDisposable
{
    private readonly Plugin Plugin;

    public MigrationWindow(Plugin plugin) : base("Migrate Notification##SubmarineTracker")
    {
        Plugin = plugin;

        Size = new Vector2(500, 350);
        Flags = ImGuiWindowFlags.NoResize;

        RespectCloseHotkey = false;
        DisableWindowSounds = true;

        LogAndNotify();
    }

    public void Dispose() { }

    private void NotificationClicked(INotificationClickArgs args)
    {
        IsOpen = true;
        args.Notification.DismissNow();
    }

    private void LogAndNotify()
    {
        Plugin.Log.Info($"[Migration] Checked migration notification: {Plugin.FirstTimeMigration}");

        if (Plugin.FirstTimeMigration)
        {
            var notification = Plugin.Notification.AddNotification(new Notification
            {
                // The user needs to dismiss this for it to go away.
                Type = NotificationType.Info,
                InitialDuration = TimeSpan.FromHours(24),
                Title = "SubmarineTracker Migration",
                Content = "Important notification.\nClick for more information.",
                Minimized = false,
            });

            notification.Click += NotificationClicked;
        }
    }

    public override void Draw()
    {
        ImGui.PushTextWrapPos();
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudOrange))
        {
            ImGui.TextUnformatted("This is an important notice that will effect all users, please read the following information:");
        }

        ImGuiHelpers.ScaledDummy(10.0f);

        ImGui.TextUnformatted("SubmarineTracker will be undergoing a data migration to a new format.");
        ImGui.TextUnformatted("How will the migration process work?");
        ImGui.Bullet();
        ImGui.TextUnformatted("The plugin will automatically check and migrate the aforementioned data in the background for a particular character after logging into it for the first time.");
        ImGui.Bullet();
        ImGui.TextUnformatted("Please note however that this process will be canceled if a character that has yet to be updated is not on their home world upon logging in, relogging once the character is on their home world will allow the migration process to run.");
        ImGui.Bullet();
        ImGui.TextUnformatted("While the migration is taking place, information regarding your submarines will be unavailable and won't display until the process has finished.");

        ImGui.TextUnformatted("This migration of data formats will allow for a variety of future features and long requested fixes moving forwards, so thank you for your understanding.");
        ImGui.PopTextWrapPos();
        ImGuiHelpers.ScaledDummy(10.0f);

        var colorNormal = new Vector4(0.0f, 0.70f, 0.0f, 1.0f);
        var colorHovered = new Vector4(0.059f, 0.49f, 0.0f, 1.0f);
        using (ImRaii.PushColor(ImGuiCol.Button, colorNormal))
        using (ImRaii.PushColor(ImGuiCol.ButtonHovered, colorHovered))
        {
            if (ImGui.Button("Understood"))
                IsOpen = false;
        }
    }
}
