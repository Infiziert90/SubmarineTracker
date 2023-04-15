using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;

// From: https://github.com/Ottermandias/Glamourer/blob/b65658ef6337137c5d6d8aeff8620ee087f2d951/Glamourer/Gui/ComboWithFilter.cs
namespace SubmarineTracker.Windows
{
    public class ComboWithFilter<T>
    {
        private readonly string                       Label;
        private readonly string                       FilterLabel;
        private readonly string                       ListLabel;
        private          string                       CurrentFilter      = string.Empty;
        private          string                       CurrentFilterLower = string.Empty;
        private          bool                         Focus;
        private readonly float                        Size;
        private          float                        PreviewSize;
        private readonly IReadOnlyList<T>             Items;
        private readonly IReadOnlyList<(string, int)> ItemNamesLower;
        private readonly Func<T, string>              ItemToName;
        private          IReadOnlyList<(string, int)> CurrentItemNames;
        private          bool                         NeedsClear;

        public Action?        PrePreview;
        public Action?        PostPreview;
        public Func<T, bool>? CreateSelectable;
        public Action?        PreList;
        public Action?        PostList;
        public float?         HeightPerItem;

        private float _heightPerItem;

        public ImGuiComboFlags Flags       { get; set; } = ImGuiComboFlags.None;
        public int             ItemsAtOnce { get; set; } = 12;

        private void UpdateFilter(string newFilter)
        {
            if (newFilter == CurrentFilter)
                return;

            var lower = newFilter.ToLowerInvariant();
            if (CurrentFilterLower.Any() && lower.Contains(CurrentFilterLower))
                CurrentItemNames = CurrentItemNames.Where(p => p.Item1.Contains(lower)).ToArray();
            else if (lower.Any())
                CurrentItemNames = ItemNamesLower.Where(p => p.Item1.Contains(lower)).ToArray();
            else
                CurrentItemNames = ItemNamesLower;
            CurrentFilter      = newFilter;
            CurrentFilterLower = lower;
        }

        public ComboWithFilter(string label, float size, float previewSize, IReadOnlyList<T> items, Func<T, string> itemToName)
        {
            Label       = label;
            FilterLabel = $"##_{label}_filter";
            ListLabel   = $"##_{label}_list";
            ItemToName  = itemToName;
            Items       = items;
            Size        = size;
            PreviewSize = previewSize;

            ItemNamesLower   = Items.Select((i, idx) => (ItemToName(i).ToLowerInvariant(), idx)).ToArray();
            CurrentItemNames = ItemNamesLower;
        }

        private bool DrawList(string currentName, out int numItems, out int nodeIdx, ref T? value)
        {
            numItems = ItemsAtOnce;
            nodeIdx  = -1;
            if (!ImGui.BeginChild(ListLabel, new Vector2(Size, ItemsAtOnce * _heightPerItem)))
            {
                ImGui.EndChild();
                return false;
            }

            var ret = false;
            try
            {
                if (!Focus)
                {
                    ImGui.SetScrollY(0);
                    Focus = true;
                }

                var scrollY    = Math.Max((int) (ImGui.GetScrollY() / _heightPerItem) - 1, 0);
                var restHeight = scrollY * _heightPerItem;
                numItems = 0;
                nodeIdx  = 0;

                if (restHeight > 0)
                    ImGui.Dummy(Vector2.UnitY * restHeight);

                for (var i = scrollY; i < CurrentItemNames.Count; ++i)
                {
                    if (++numItems > ItemsAtOnce + 2)
                        continue;

                    nodeIdx = CurrentItemNames[i].Item2;
                    var  item = Items[nodeIdx]!;
                    bool success;
                    if (CreateSelectable != null)
                    {
                        success = CreateSelectable(item);
                    }
                    else
                    {
                        var name = ItemToName(item);
                        success = ImGui.Selectable(name, name == currentName);
                    }

                    if (success)
                    {
                        value = item;
                        ImGui.CloseCurrentPopup();
                        ret = true;
                    }
                }

                if (CurrentItemNames.Count > ItemsAtOnce + 2)
                    ImGui.Dummy(Vector2.UnitY * (CurrentItemNames.Count - ItemsAtOnce - 2 - scrollY) * _heightPerItem);
            }
            finally
            {
                ImGui.EndChild();
            }

            return ret;
        }

        public bool Draw(string currentName, out T? value, float? size = null)
        {
            if (size.HasValue)
                PreviewSize = size.Value;

            value = default;
            ImGui.SetNextItemWidth(PreviewSize);
            PrePreview?.Invoke();
            if (!ImGui.BeginCombo(Label, currentName, Flags))
            {
                if (NeedsClear)
                {
                    NeedsClear = false;
                    Focus      = false;
                    UpdateFilter(string.Empty);
                }

                PostPreview?.Invoke();
                return false;
            }

            NeedsClear = true;
            PostPreview?.Invoke();

            _heightPerItem = HeightPerItem ?? ImGui.GetTextLineHeightWithSpacing();

            bool ret;
            try
            {
                ImGui.SetNextItemWidth(-1);
                var tmp = CurrentFilter;
                if (ImGui.InputTextWithHint(FilterLabel, "Filter...", ref tmp, 255))
                    UpdateFilter(tmp);

                var isFocused = ImGui.IsItemActive();
                if (!Focus)
                    ImGui.SetKeyboardFocusHere();

                PreList?.Invoke();
                ret = DrawList(currentName, out var numItems, out var nodeIdx, ref value);
                PostList?.Invoke();

                if (!isFocused && numItems <= 1 && nodeIdx >= 0)
                {
                    value = Items[nodeIdx];
                    ret   = true;
                    ImGui.CloseCurrentPopup();
                }
            }
            finally
            {
                ImGui.EndCombo();
            }

            return ret;
        }
    }
}
