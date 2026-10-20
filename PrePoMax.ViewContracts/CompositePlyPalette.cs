using System;
using System.Drawing;
using System.Globalization;

namespace vtkControl
{
    // Presentation only. Distinct adjacent hues shared by every ply preview.
    // Labels and line weight supplement colour; the finite palette repeats after eight sources.
    public static class CompositePlyPalette
    {
        private static readonly Color[] Colors = {
            Color.FromArgb(214, 39, 40), Color.FromArgb(31, 119, 180),
            Color.FromArgb(44, 160, 44), Color.FromArgb(148, 103, 189),
            Color.FromArgb(255, 127, 14), Color.FromArgb(140, 86, 75),
            Color.FromArgb(227, 119, 194), Color.FromArgb(23, 190, 207)
        };
        public static int Source(int index, int sourceCount, bool symmetric)
        { return symmetric && index >= sourceCount ? 2 * sourceCount - 1 - index : index; }
        public static Color Get(int source) { return Colors[Math.Abs(source) % Colors.Length]; }
        public const int DimmedAlpha = 89; // 89/255 = 34.9%, preserving RGB hue.
        public static Color Display(int source, bool dimmed)
        { return Color.FromArgb(dimmed ? DimmedAlpha : 255, Get(source)); }
        public static string Label(int source, double angle)
        { return "P" + (source + 1).ToString("00", CultureInfo.InvariantCulture) + " " +
                 angle.ToString("+0.####;−0.####;0", CultureInfo.InvariantCulture) + "°"; }
    }

    public sealed class CompositePlyLegendEntry
    {
        public string Label;
        public Color Color;
        public bool Selected;
        public bool Symmetric;
    }
}
