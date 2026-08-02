using System.Globalization;
using Godot;

namespace STS2RitsuLib.Ui.Overlay
{
    internal enum RitsuDebugToolsGlyph
    {
        Cards,
        PileCards,
        Relics,
        Potions,
        Powers,
        Players,
        Creatures,
        Monsters,
        Rooms,
        Encounters,
        Events,
        Puzzle,
        ChevronLeft,
        ChevronRight,
    }

    internal static class RitsuDebugToolsIcons
    {
        private static readonly Dictionary<(RitsuDebugToolsGlyph Glyph, int Size, uint Color), ImageTexture> Cache = [];

        internal static ImageTexture? Get(RitsuDebugToolsGlyph glyph, int size, Color color)
        {
            if (size is < 8 or > 256 ||
                !float.IsFinite(color.R) ||
                !float.IsFinite(color.G) ||
                !float.IsFinite(color.B) ||
                !float.IsFinite(color.A))
                return null;
            var key = (glyph, size, color.ToRgba32());
            if (Cache.TryGetValue(key, out var cached) && GodotObject.IsInstanceValid(cached))
                return cached;

            var svg = BuildSvg(glyph, color);
            var image = new Image();
            var error = image.LoadSvgFromString(svg, size / 24f);
            if (error != Error.Ok || image.GetWidth() < 1 || image.GetHeight() < 1)
                return null;
            if (image.GetWidth() != size || image.GetHeight() != size)
                image.Resize(size, size, Image.Interpolation.Cubic);

            var texture = ImageTexture.CreateFromImage(image);
            Cache[key] = texture;
            return texture;
        }

        private static string BuildSvg(RitsuDebugToolsGlyph glyph, Color color)
        {
            var rgb = $"#{ToByte(color.R):X2}{ToByte(color.G):X2}{ToByte(color.B):X2}";
            var opacity = Mathf.Clamp(color.A, 0f, 1f).ToString(CultureInfo.InvariantCulture);
            var body = glyph switch
            {
                RitsuDebugToolsGlyph.Cards =>
                    "<rect x='5' y='3.5' width='13' height='17' rx='2'/><path d='M9 7h5M9 11h5M9 15h3'/>",
                RitsuDebugToolsGlyph.PileCards =>
                    "<rect x='7' y='4' width='12' height='15' rx='1.8'/><path d='M5 6v14h11M3 8v14h11'/>",
                RitsuDebugToolsGlyph.Relics =>
                    "<path d='M12 3l7 6-7 12L5 9zM5 9h14M8.5 9L12 21 15.5 9 12 3z'/>",
                RitsuDebugToolsGlyph.Potions =>
                    "<path d='M9 3h6M10 3v5l-4 6.5A4 4 0 0 0 9.4 21h5.2a4 4 0 0 0 3.4-6.5L14 8V3M7.7 13h8.6'/>",
                RitsuDebugToolsGlyph.Powers =>
                    "<path d='M13.5 2L5 13h6l-1 9 9-12h-6z'/>",
                RitsuDebugToolsGlyph.Players =>
                    "<circle cx='12' cy='8' r='4'/><path d='M4.5 21c.7-5 3.2-7.5 7.5-7.5s6.8 2.5 7.5 7.5'/>",
                RitsuDebugToolsGlyph.Creatures =>
                    "<path d='M4 4l6.5 6.5M14 14l6 6M20 4l-6.5 6.5M10 14l-6 6M3 3l4 1-3 3zM21 3l-4 1 3 3z'/>",
                RitsuDebugToolsGlyph.Monsters =>
                    "<path d='M6 9L4 4l5 3a8 8 0 0 1 6 0l5-3-2 5v7a6 6 0 0 1-12 0z'/><circle cx='9' cy='13' r='1'/><circle cx='15' cy='13' r='1'/><path d='M9 17h6'/>",
                RitsuDebugToolsGlyph.Rooms =>
                    "<path d='M5 21V3h13v18M9 21V7h6v14M12.8 14h.2'/>",
                RitsuDebugToolsGlyph.Encounters =>
                    "<path d='M12 3l7 3v5c0 5-2.7 8.5-7 10-4.3-1.5-7-5-7-10V6zM8.5 8.5l7 7M15.5 8.5l-7 7'/>",
                RitsuDebugToolsGlyph.Events =>
                    "<path d='M12 3l2.2 5.1 5.5.5-4.2 3.7 1.3 5.4-4.8-2.8-4.8 2.8 1.3-5.4-4.2-3.7 5.5-.5z'/><path d='M12 8v3.5M12 13.8v.2'/>",
                RitsuDebugToolsGlyph.ChevronLeft => "<path d='M15 5l-7 7 7 7'/>",
                RitsuDebugToolsGlyph.ChevronRight => "<path d='M9 5l7 7-7 7'/>",
                _ =>
                    "<path d='M9 3h4v4h4v4h4v4h-4v4h-4v-4H9v4H5v-4H3v-4h2V7h4z'/>",
            };
            var fill = glyph is RitsuDebugToolsGlyph.Powers or RitsuDebugToolsGlyph.Events
                ? rgb
                : "none";
            return $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'>" +
                   $"<g fill='{fill}' stroke='{rgb}' stroke-opacity='{opacity}' fill-opacity='{opacity}' " +
                   "stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'>" + body + "</g></svg>";
        }

        private static byte ToByte(float value)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);
        }
    }
}
