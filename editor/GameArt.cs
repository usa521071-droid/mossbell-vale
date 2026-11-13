using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;

namespace MossbellVale.Editor
{
    internal sealed class GameArt : IDisposable
    {
        public const int TileSize = 16;
        public const int PlayerWidth = 16;
        public const int PlayerHeight = 24;

        public static readonly string[] TileNames = {
            "Meadow", "Meadow Tufts", "Daisies", "Clover", "Clay Path", "Pebbled Path", "Sand", "Boardwalk",
            "Shallows", "Water", "Deep Water", "Rapids", "Stone Trail", "Moss", "Heather", "Cobblestone",
            "Fir", "Oak", "Shrub", "Boulder", "Mountain", "Cliff", "Fern", "Wildflowers",
            "Hollow Log", "Fence", "Cottage Wall", "Cottage Roof", "Stump", "Stone Arch", "Trail Sign", "Glowbell"
        };

        public static readonly string[][] PaletteNames = {
            new[] { "Rose", "Pine", "Gold", "Lilac", "Sky", "Berry" },
            new[] { "Denim", "Olive", "Charcoal", "Sand", "Plum", "Navy" },
            new[] { "Chestnut", "Ink", "Honey", "Copper", "Ash", "Auburn" },
            new[] { "Peach", "Fair", "Gold", "Tan", "Brown", "Deep" }
        };

        private static readonly int[,,] OutfitColors = {
            { { 0xE75977, 0xA53558 }, { 0x58B783, 0x317754 }, { 0xE2AB52, 0x9A6233 },
              { 0xA284D1, 0x654E96 }, { 0x63B4D5, 0x39738E }, { 0xBC6188, 0x76395E } },
            { { 0x456F9A, 0x294361 }, { 0x718854, 0x465435 }, { 0x54586A, 0x333546 },
              { 0xB08F65, 0x6D543D }, { 0x805984, 0x4B3B5D }, { 0x45627A, 0x2A3D56 } },
            { { 0x99664B, 0x613F39 }, { 0x454050, 0x2A2836 }, { 0xD5A24E, 0x936331 },
              { 0xB96D4F, 0x7D433C }, { 0xA3A3AE, 0x666779 }, { 0x805053, 0x513644 } },
            { { 0xF6C291, 0xD38C69 }, { 0xF8D1AD, 0xDFAB84 }, { 0xDEAB73, 0xAC744F },
              { 0xC8875C, 0x935A43 }, { 0x9C634D, 0x6D423C }, { 0x775045, 0x523633 } }
        };

        private readonly Bitmap tiles = ReadBitmap("tiles.png");
        private readonly Bitmap player = ReadBitmap("player.png");

        public static WorldData ReadDemo()
        {
            return WorldData.Read(OpenAsset("world.rpgw"));
        }

        public static void UsePixelArt(Graphics graphics)
        {
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
        }

        public void DrawTile(Graphics graphics, int id, Rectangle destination)
        {
            graphics.DrawImage(tiles, destination, id % 8 * TileSize, id / 8 * TileSize, TileSize, TileSize, GraphicsUnit.Pixel);
        }

        public static void DrawPlayer(Graphics graphics, Bitmap sprite, int direction, int frame, Rectangle destination)
        {
            graphics.DrawImage(sprite, destination, frame * PlayerWidth, direction * PlayerHeight,
                PlayerWidth, PlayerHeight, GraphicsUnit.Pixel);
        }

        public Bitmap RecolorPlayer(byte[] outfit)
        {
            var result = new Bitmap(player.Width, player.Height);
            for (var y = 0; y < player.Height; y++)
            {
                for (var x = 0; x < player.Width; x++)
                {
                    var color = player.GetPixel(x, y);
                    if (color.A == 255)
                        color = Recolor(color, outfit);
                    result.SetPixel(x, y, color);
                }
            }
            return result;
        }

        private static Color Recolor(Color color, byte[] outfit)
        {
            var rgb = color.ToArgb() & 0xFFFFFF;
            for (var part = 0; part < outfit.Length; part++)
                for (var shade = 0; shade < 2; shade++)
                    if (rgb == OutfitColors[part, 0, shade])
                        return Color.FromArgb(unchecked((int)0xFF000000) | OutfitColors[part, outfit[part], shade]);
            return color;
        }

        private static Bitmap ReadBitmap(string name)
        {
            using (var stream = OpenAsset(name))
            using (var original = new Bitmap(stream))
                return new Bitmap(original);
        }

        private static Stream OpenAsset(string name)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MossbellVale." + name);
            if (stream == null)
                throw new InvalidDataException("The editor is missing its embedded " + name + " asset.");
            return stream;
        }

        public void Dispose()
        {
            tiles.Dispose();
            player.Dispose();
        }
    }
}
