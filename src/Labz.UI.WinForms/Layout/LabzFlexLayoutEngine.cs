using System.Drawing;
using System.Windows.Forms;

namespace Labz.UI.WinForms.Layout;

public enum LabzDirection { Horizontal, Vertical }
public enum LabzJustifyContent { Start, Center, End, SpaceBetween, SpaceAround, SpaceEvenly }
public enum LabzAlignItems { Start, Center, End, Stretch }

/// <summary>Shared deterministic flex-like layout calculations for Labz containers.</summary>
public static class LabzFlexLayoutEngine
{
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
