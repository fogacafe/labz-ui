using System.Drawing;
using System.Windows.Forms;

namespace Labz.UI.WinForms.Layout;

public enum LabzDirection { Horizontal, Vertical }
public enum LabzJustifyContent { Start, Center, End, SpaceBetween, SpaceAround, SpaceEvenly }
public enum LabzAlignItems { Start, Center, End, Stretch }

/// <summary>Shared deterministic flex-like layout calculations for Labz containers.</summary>
public static class LabzFlexLayoutEngine
{
    /// <summary>Arranges one flex line using weighted growth and shrinkage.</summary>
    /// <remarks>For wrapped layouts, callers should arrange each line separately.</remarks>
    public static IReadOnlyList<Rectangle> ArrangeFlexible(
        IReadOnlyList<Size> sizes, Rectangle bounds, LabzDirection direction,
        int gap, LabzJustifyContent justify, LabzAlignItems align,
        IReadOnlyList<double> grow, IReadOnlyList<double> shrink)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(gap);
        if (sizes.Count != grow.Count || sizes.Count != shrink.Count)
            throw new ArgumentException("Size and weight counts must match.");
        if (grow.Any(x => !double.IsFinite(x) || x < 0) ||
            shrink.Any(x => !double.IsFinite(x) || x < 0))
            throw new ArgumentOutOfRangeException(nameof(grow), "Flex weights must be finite and non-negative.");
        if (sizes.Count == 0) return [];
        var horizontal = direction == LabzDirection.Horizontal;
        var limit = Math.Max(0, horizontal ? bounds.Width : bounds.Height);
        var main = sizes.Select(s => (double)Math.Max(0, horizontal ? s.Width : s.Height)).ToArray();
        var available = limit - gap * (sizes.Count - 1) - main.Sum();
        if (available > 0 && grow.Sum() > 0)
        {
            var total = grow.Sum();
            for (var i = 0; i < main.Length; i++) main[i] += available * grow[i] / total;
        }
        else if (available < 0)
        {
            // CSS-style shrink factors are weighted by the item's base size.
            var factors = main.Select((size, i) => size * shrink[i]).ToArray();
            var deficit = -available;
            var active = Enumerable.Range(0, main.Length).Where(i => factors[i] > 0).ToList();
            while (deficit > 0.0001 && active.Count > 0)
            {
                var total = active.Sum(i => factors[i]);
                var consumed = 0d;
                foreach (var i in active)
                {
                    var reduction = Math.Min(main[i], deficit * factors[i] / total);
                    main[i] -= reduction;
                    consumed += reduction;
                }
                deficit -= consumed;
                active.RemoveAll(i => main[i] < 0.0001);
            }
        }
        var free = Math.Max(0, limit - gap * (sizes.Count - 1) - main.Sum());
        var offset = justify switch
        {
            LabzJustifyContent.Center => free / 2,
            LabzJustifyContent.End => free,
            LabzJustifyContent.SpaceAround => free / (2 * sizes.Count),
            LabzJustifyContent.SpaceEvenly => free / (sizes.Count + 1),
            _ => 0d
        };
        var spacing = justify switch
        {
            LabzJustifyContent.SpaceBetween when sizes.Count > 1 => gap + free / (sizes.Count - 1),
            LabzJustifyContent.SpaceAround => gap + free / sizes.Count,
            LabzJustifyContent.SpaceEvenly => gap + free / (sizes.Count + 1),
            _ => (double)gap
        };
        var result = new Rectangle[sizes.Count];
        var crossLimit = Math.Max(0, horizontal ? bounds.Height : bounds.Width);
        for (var i = 0; i < sizes.Count; i++)
        {
            var cross = Math.Max(0, horizontal ? sizes[i].Height : sizes[i].Width);
            var crossOffset = align switch
            {
                LabzAlignItems.Center => (crossLimit - cross) / 2,
                LabzAlignItems.End => crossLimit - cross,
                _ => 0
            };
            if (align == LabzAlignItems.Stretch) cross = crossLimit;
            var start = (int)Math.Round(offset);
            var end = (int)Math.Round(offset + main[i]);
            result[i] = horizontal
                ? new Rectangle(bounds.X + start, bounds.Y + crossOffset, Math.Max(0, end - start), cross)
                : new Rectangle(bounds.X + crossOffset, bounds.Y + start, cross, Math.Max(0, end - start));
            offset += main[i] + spacing;
        }
        return result;
    }

    public static IReadOnlyList<Rectangle> Arrange(
        IReadOnlyList<Size> sizes, Rectangle bounds, LabzDirection direction,
        int gap, bool wrap, LabzJustifyContent justify, LabzAlignItems align)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(gap);
        if (sizes.Count == 0) return [];
        var horizontal = direction == LabzDirection.Horizontal;
        var mainLimit = Math.Max(0, horizontal ? bounds.Width : bounds.Height);
        var crossLimit = Math.Max(0, horizontal ? bounds.Height : bounds.Width);
        var lines = new List<List<int>>();
        var line = new List<int>();
        var used = 0;
        foreach (var index in Enumerable.Range(0, sizes.Count))
        {
            var main = Math.Max(0, horizontal ? sizes[index].Width : sizes[index].Height);
            if (wrap && line.Count > 0 && used + gap + main > mainLimit)
            {
                lines.Add(line);
                line = [];
                used = 0;
            }
            used += (line.Count > 0 ? gap : 0) + main;
            line.Add(index);
        }
        if (line.Count > 0) lines.Add(line);

        var output = new Rectangle[sizes.Count];
        var crossOffset = 0;
        foreach (var group in lines)
        {
            var totalMain = group.Sum(i => Math.Max(0, horizontal ? sizes[i].Width : sizes[i].Height));
            var remaining = Math.Max(0, mainLimit - totalMain - gap * Math.Max(0, group.Count - 1));
            var lineCross = wrap
                ? group.Max(i => Math.Max(0, horizontal ? sizes[i].Height : sizes[i].Width))
                : crossLimit;
            double offset = justify switch
            {
                LabzJustifyContent.Center => remaining / 2d,
                LabzJustifyContent.End => remaining,
                LabzJustifyContent.SpaceAround => remaining / (2d * group.Count),
                LabzJustifyContent.SpaceEvenly => remaining / (double)(group.Count + 1),
                _ => 0
            };
            double spacing = justify switch
            {
                LabzJustifyContent.SpaceBetween when group.Count > 1 => gap + remaining / (double)(group.Count - 1),
                LabzJustifyContent.SpaceAround => gap + remaining / (double)group.Count,
                LabzJustifyContent.SpaceEvenly => gap + remaining / (double)(group.Count + 1),
                _ => gap
            };
            foreach (var i in group)
            {
                var main = Math.Max(0, horizontal ? sizes[i].Width : sizes[i].Height);
                var cross = Math.Max(0, horizontal ? sizes[i].Height : sizes[i].Width);
                var crossPosition = align switch
                {
                    LabzAlignItems.Center => (lineCross - cross) / 2,
                    LabzAlignItems.End => lineCross - cross,
                    _ => 0
                };
                if (align == LabzAlignItems.Stretch) cross = lineCross;
                output[i] = horizontal
                    ? new Rectangle(bounds.X + (int)Math.Round(offset), bounds.Y + crossOffset + crossPosition, main, cross)
                    : new Rectangle(bounds.X + crossOffset + crossPosition, bounds.Y + (int)Math.Round(offset), cross, main);
                offset += main + spacing;
            }
            crossOffset += lineCross + gap;
        }
        return output;
    }
}
