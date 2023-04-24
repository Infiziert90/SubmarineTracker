using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Linq;

namespace SubmarineTracker.Windows;

public class NotifyOverlay : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;
    private readonly Notify Notify;

    private bool IsStartupDraw = true;

    public NotifyOverlay(Plugin plugin, Configuration configuration, Notify notify) : base("Notify")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize;

        Plugin = plugin;
        Configuration = configuration;
        Notify = notify;
    }

    public void Dispose() { }

    public override bool DrawConditions()
    {
        if (!Notify.OverlayNotifications.Any())
            return false;

        if (Configuration.NotifyOverlayAlways)
            return true;
        if (Configuration.NotifyOverlayOnStartup && IsStartupDraw)
            return true;

        return false;
    }

    public override void Draw()
    {
        ImGuiHelpers.ScaledDummy(10.0f);
        foreach (var notification in Notify.OverlayNotifications)
            ImGui.TextColored(ImGuiColors.TankBlue, notification);
        ImGuiHelpers.ScaledDummy(10.0f);
    }

    public override void OnClose()
    {
        IsStartupDraw = false;

        if (Notify.OverlayNotifications.Any())
            Notify.OverlayNotifications.Clear();
    }
}

