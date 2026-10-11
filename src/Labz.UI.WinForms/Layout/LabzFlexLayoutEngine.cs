using System.Drawing;
using System.Windows.Forms;

namespace Labz.UI.WinForms.Layout;

public enum LabzDirection { Horizontal, Vertical }
public enum LabzJustifyContent { Start, Center, End, SpaceBetween, SpaceAround, SpaceEvenly }
public enum LabzAlignItems { Start, Center, End, Stretch }

/// <summary>Shared deterministic flex-like layout calculations for Labz containers.</summary>
public static class LabzFlexLayoutEngine
{

    /// <summary>Flexible wrapped layout with per-item bounds and weighted line sizing.</summary>
    public static IReadOnlyList<Rectangle> ArrangeConstrained(
        IReadOnlyList<Size> sizes, Rectangle bounds, LabzDirection direction,
        int gap, bool wrap, LabzJustifyContent justify, LabzAlignItems align,
        IReadOnlyList<double> grow, IReadOnlyList<double> shrink,
        IReadOnlyList<Size> minimum, IReadOnlyList<Size> maximum)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(gap);
        var count = sizes.Count;
        if (grow.Count != count || shrink.Count != count ||
            minimum.Count != count || maximum.Count != count)
            throw new ArgumentException("All item collections must have matching counts.");
        if (grow.Any(x => !double.IsFinite(x) || x < 0) ||
            shrink.Any(x => !double.IsFinite(x) || x < 0))
            throw new ArgumentOutOfRangeException(nameof(grow), "Weights must be finite and non-negative.");
        if (count == 0) return [];
        var horizontal = direction == LabzDirection.Horizontal;
        int Main(Size s) => horizontal ? s.Width : s.Height;
        int Cross(Size s) => horizontal ? s.Height : s.Width;
        var limit = Math.Max(0, horizontal ? bounds.Width : bounds.Height);
        var crossLimit = Math.Max(0, horizontal ? bounds.Height : bounds.Width);
        var basis = new double[count];
        var min = new double[count];
        var max = new double[count];
        for (var i = 0; i < count; i++)
        {
            min[i] = Math.Max(0, Main(minimum[i]));
            // Zero maximum means unconstrained, matching WinForms MaximumSize.
            max[i] = Main(maximum[i]) <= 0 ? double.PositiveInfinity : Math.Max(min[i], Main(maximum[i]));
            basis[i] = Math.Clamp(Math.Max(0, Main(sizes[i])), min[i], max[i]);
        }
        var lines = new List<List<int>>();
        var current = new List<int>();
        var used = 0d;
        for (var i = 0; i < count; i++)
        {
            if (wrap && current.Count > 0 && used + gap + basis[i] > limit)
            {
                lines.Add(current);
                current = [];
                used = 0;
            }
            used += (current.Count == 0 ? 0 : gap) + basis[i];
            current.Add(i);
        }
        if (current.Count > 0) lines.Add(current);
        var result = new Rectangle[count];
        var crossOffset = 0;
        foreach (var line in lines)
        {
            var allocated = line.Select(i => basis[i]).ToArray();
            var free = limit - gap * (line.Count - 1) - allocated.Sum();
            // Redistribute after each item reaches its min/max constraint.
            for (var iteration = 0; iteration < line.Count && Math.Abs(free) > 0.001; iteration++)
            {
                var expanding = free > 0;
                var weights = line.Select((i, j) => expanding
                    ? (allocated[j] < max[i] - 0.001 ? grow[i] : 0)
                    : (allocated[j] > min[i] + 0.001 ? shrink[i] * basis[i] : 0)).ToArray();
                var total = weights.Sum();
                if (total <= 0) break;
                var distributed = 0d;
                for (var j = 0; j < line.Count; j++)
                {
                    var i = line[j];
                    var delta = free * weights[j] / total;
                    var next = Math.Clamp(allocated[j] + delta, min[i], max[i]);
                    distributed += next - allocated[j];
                    allocated[j] = next;
                }
                free -= distributed;
                if (Math.Abs(distributed) < 0.001) break;
            }
            var leftover = Math.Max(0, limit - gap * (line.Count - 1) - allocated.Sum());
            var start = justify switch
            {
                LabzJustifyContent.Center => leftover / 2,
                LabzJustifyContent.End => leftover,
                LabzJustifyContent.SpaceAround => leftover / (2 * line.Count),
                LabzJustifyContent.SpaceEvenly => leftover / (line.Count + 1),
                _ => 0d
            };
            var spacing = justify switch
            {
                LabzJustifyContent.SpaceBetween when line.Count > 1 => gap + leftover / (line.Count - 1),
                LabzJustifyContent.SpaceAround => gap + leftover / line.Count,
                LabzJustifyContent.SpaceEvenly => gap + leftover / (line.Count + 1),
                _ => (double)gap
            };
            var lineCross = wrap ? line.Max(i => Math.Max(0, Cross(sizes[i]))) : crossLimit;
            for (var j = 0; j < line.Count; j++)
            {
                var i = line[j];
                var cross = Math.Max(0, Cross(sizes[i]));
                if (align == LabzAlignItems.Stretch)
                {
                    var maxCross = Cross(maximum[i]);
                    cross = Math.Clamp(lineCross, Math.Max(0, Cross(minimum[i])),
                        maxCross <= 0 ? int.MaxValue : Math.Max(Cross(minimum[i]), maxCross));
                }
                var crossPosition = align switch
                {
                    LabzAlignItems.Center => (lineCross - cross) / 2,
                    LabzAlignItems.End => lineCross - cross,
                    _ => 0
                };
                var p0 = (int)Math.Round(start);
                var p1 = (int)Math.Round(start + allocated[j]);
                result[i] = horizontal
                    ? new Rectangle(bounds.X + p0, bounds.Y + crossOffset + crossPosition, Math.Max(0, p1 - p0), cross)
                    : new Rectangle(bounds.X + crossOffset + crossPosition, bounds.Y + p0, cross, Math.Max(0, p1 - p0));
                start += allocated[j] + spacing;
            }
            crossOffset += lineCross + gap;
        }
        return result;
    }

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
