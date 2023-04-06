using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using SubmarineTracker.Data;

namespace SubmarineTracker.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    public ConfigWindow(Plugin plugin) : base("Configuration")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(250, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("##ConfigTabBar"))
        {
            if (ImGui.BeginTabItem("General"))
            {
                var changed = false;
                changed |= ImGui.Checkbox("Show Extended Parts List", ref Configuration.ShowExtendedPartsList);

                if (changed)
                    Configuration.Save();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Saves"))
            {
                if (ImGui.BeginTable("##DeleteSavesTable", 2))
                {
                    ImGui.TableSetupColumn("Saved Setup");
                    ImGui.TableSetupColumn("Del", 0, 0.2f);

                    ImGui.TableHeadersRow();

                    ulong deletion = 0;
                    foreach (var (id, fc) in Submarines.KnownSubmarines)
                    {
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{fc.Tag}@{fc.World}");

                        ImGui.TableNextColumn();
                        if (ImGuiComponents.IconButton((int)id, FontAwesomeIcon.Trash))
                            deletion = id;

                        ImGui.TableNextRow();
                    }

                    if (deletion != 0)
                        Submarines.DeleteCharacter(deletion);
                }
                ImGui.EndTable();
            }
        }
        ImGui.EndTabBar();
    }
}
