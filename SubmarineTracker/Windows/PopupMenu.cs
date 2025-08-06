﻿using static Dalamud.Bindings.ImGui.ImGuiHoveredFlags;

namespace SubmarineTracker.Windows;

// From: https://github.com/Critical-Impact/InventoryTools/blob/a9df0be1c6f1499198a724bcdf36422f240ad6f2/InventoryTools/Ui/Widgets/PopupMenu.cs
public class PopupMenu
{
    private readonly List<IPopupMenuItem> Items;
    private readonly PopupMenuButtons OpenButtons;
    private readonly string Id;

    public enum PopupMenuButtons
    {
        Left,
        Right,
        Middle,
        LeftRight,
        All
    }

    public interface IPopupMenuItem
    {
        public void DrawPopup();
    }

    public class PopupMenuItemSelectable : IPopupMenuItem
    {
        private readonly string Name;
        private readonly string Tooltip;
        private Action Callback { get; }

        public PopupMenuItemSelectable(string name, Action callback, string tooltip = "")
        {
            Name = name;
            Tooltip = tooltip;
            Callback = callback;
        }

        public void DrawPopup()
        {
            if (ImGui.Selectable(Name))
            {
                Callback?.Invoke();
                return;
            }

            if (Tooltip != "")
                if (ImGui.IsItemHovered())
                    Helper.Tooltip(Tooltip);
        }
    }


    public class PopupMenuItemSeparator : IPopupMenuItem
    {
        public void DrawPopup()
        {
            ImGuiHelpers.ScaledDummy(1.0f);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(1.0f);
        }
    }

    public PopupMenu(string id, PopupMenuButtons openButtons,List<IPopupMenuItem> items)
    {
        Id = id;
        OpenButtons = openButtons;
        Items = items;
    }

    public void Draw()
    {
        var isMouseReleased = ImGui.IsMouseReleased(ImGuiMouseButton.Left) && OpenButtons is PopupMenuButtons.All or PopupMenuButtons.Left or PopupMenuButtons.LeftRight ||
                              ImGui.IsMouseReleased(ImGuiMouseButton.Right) && OpenButtons is PopupMenuButtons.All or PopupMenuButtons.Right or PopupMenuButtons.LeftRight ||
                              ImGui.IsMouseReleased(ImGuiMouseButton.Middle) && OpenButtons is PopupMenuButtons.All or PopupMenuButtons.Middle;

        if (ImGui.IsItemHovered(AllowWhenDisabled & AllowWhenOverlapped & AllowWhenBlockedByPopup & AllowWhenBlockedByActiveItem & AnyWindow) && isMouseReleased)
            ImGui.OpenPopup($"RightClick{Id}");

        using var popup = ImRaii.Popup($"RightClick{Id}");
        if (popup.Success)
        {
            foreach (var item in Items)
                item.DrawPopup();
        }
    }
}
