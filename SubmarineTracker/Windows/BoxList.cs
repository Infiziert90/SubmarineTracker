namespace SubmarineTracker.Windows;

public static class BoxList
{
    private static readonly Dictionary<int, List<Vector2>> FBoxSizes = new();

    public static void RenderList<T>(IEnumerable<T> list, Box.Modifier modifier, float arrowScale, Action<T> boxRenderContent) where T : notnull
    {
        var items = list.ToArray();

        var hash = items.GetSequenceHashCode();

        var boxSizes = FBoxSizes.TryGetValue(hash, out var sizes) ? sizes : new List<Vector2>();

        var wSize = ImGui.GetWindowSize();

        // Don't show the first pass due to sizing gathering
        if (boxSizes.Count == 0)
            ImGui.SetCursorScreenPos(wSize + new Vector2(10, 10));

        var lastWrapped = -1;

        for (var i = 0; i < items.Length; i++)
        {
            var i1 = i;
            var size = Box.SimpleBox(modifier, () =>
            {
                boxRenderContent(items[i1]);
            });

            var p = ImGui.GetCursorScreenPos();


            var height = (int)(size.Y / (3 / arrowScale));
            var offset = (int)(height / 2f);
            var heightOffset = (int)((size.Y - height) / 2);

            var isLast = i == items.Length - 1;

            var renderSize = size with { X = size.X + (isLast ? 0 : height) };

            if (boxSizes.Count == i)
                boxSizes.Add(renderSize);
            else
                boxSizes[i] = renderSize;

            if (!isLast)
            {
                ImGui.SameLine();
                p = ImGui.GetCursorScreenPos();
                var drawList = ImGui.GetWindowDrawList();
                p = p with { Y = p.Y + heightOffset };
                ImGui.Dummy(new Vector2(offset, 0));

                drawList.AddTriangle(p, p with { Y = p.Y + offset, X = p.X + offset }, p with { Y = p.Y + height }, ImGui.GetColorU32(ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudGrey)), 1.0f);

                var nextDrawEnd = boxSizes.Skip(lastWrapped).Take(i + 1 - lastWrapped).Sum(x => (int)x.X + 4);

                var nextCursorPos = p with { X = p.X + nextDrawEnd } - p;

                if (wSize.X > nextCursorPos.X)
                    ImGui.SameLine();
                else
                {
                    ImGuiHelpers.ScaledDummy(0, 20);
                    lastWrapped = i;
                }
            }
        }

        if (!FBoxSizes.TryAdd(hash, boxSizes))
        {
            FBoxSizes[hash] = boxSizes;
        }
    }

    private static int GetSequenceHashCode<T>(this IEnumerable<T> sequence) where T : notnull
    {
        const int seed = 487;
        const int modifier = 31;

        unchecked
        {
            return sequence.Aggregate(seed, (current, item) =>
                                          (current * modifier) + item.GetHashCode());
        }
    }
}
