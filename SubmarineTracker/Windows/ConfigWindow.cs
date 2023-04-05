using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace SubmarineTracker.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    public ConfigWindow(Plugin plugin) : base("Configuration")
    {
        this.Size = new Vector2(232, 75);
        this.SizeCondition = ImGuiCond.Always;

        this.Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var changed = false;
        changed |= ImGui.Checkbox("Show Extended Parts List", ref Configuration.ShowExtendedPartsList);

        if (changed)
            Configuration.Save();
    }
}
