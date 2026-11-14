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
        public const int TileCount = 144;
        public const int PlayerWidth = 16;
        public const int PlayerHeight = 24;
        private const int TilesWidth = 128;
        private const int TilesHeight = 288;
        private const int OldTileBytes = TilesWidth * 256 * 4;
        private const int PlayerSheetWidth = 48;
        private const int PlayerSheetHeight = 96;
        private const int CreatureWidth = 48;
        private const int CreatureHeight = 16;
        private const int TileBytes = TilesWidth * TilesHeight * 4;
        private const int PlayerBytes = PlayerSheetWidth * PlayerSheetHeight * 4;
        private const int ArtFileSize = 8 + TileBytes + PlayerBytes + CreatureWidth * CreatureHeight * 4;
        private const int VersionOffset = 4;
        private const byte LegacyVersion = 1;
        private static readonly byte[] Header = { (byte)'R', (byte)'P', (byte)'G', (byte)'A', 3, 0, 0, 0 };

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

        private readonly byte[] defaultBundle;
        private Bitmap tiles;
        private Bitmap player;
        private Bitmap creature;
        public int Revision { get; private set; }

        public GameArt()
        {
            tiles = ReadBitmap("tiles.png");
            player = ReadBitmap("player.png");
            creature = ReadBitmap("blob.png");
            defaultBundle = ToBundle();
        }

        public static WorldData ReadPreset(string name)
        {
            return WorldData.Read(OpenAsset(name + ".rpgw"));
        }

        public byte[] ToBundle()
        {
            var bytes = new byte[ArtFileSize];
            Header.CopyTo(bytes, 0);
            var offset = Header.Length;
            WriteBitmap(bytes, ref offset, tiles);
            WriteBitmap(bytes, ref offset, player);
            WriteBitmap(bytes, ref offset, creature);
            return bytes;
        }

        public void LoadBundle(byte[] bytes)
        {
            if (bytes == null)
                bytes = defaultBundle;
            if (bytes.Length < Header.Length)
                throw new InvalidDataException("The art bundle has an incorrect size.");
            var version = bytes[VersionOffset];
            bool legacy = version == LegacyVersion || version == 2;
            if (bytes.Length != (legacy ? Header.Length + OldTileBytes + PlayerBytes : ArtFileSize))
                throw new InvalidDataException("The art bundle has an incorrect size.");
            for (var i = 0; i < Header.Length; i++)
                if (bytes[i] != Header[i] && !(legacy && i == VersionOffset))
                    throw new InvalidDataException("The art bundle is missing a supported RPGA header.");

            if (legacy)
                bytes = MigrateArt(bytes, version);

            var offset = Header.Length;
            using (var grounds = ReadBitmap(bytes, ref offset, TilesWidth, TilesHeight))
            using (var actor = ReadBitmap(bytes, ref offset, PlayerSheetWidth, PlayerSheetHeight))
            using (var blob = ReadBitmap(bytes, ref offset, CreatureWidth, CreatureHeight))
            {
                ValidateOpaqueGround(grounds);
                tiles.Dispose();
                player.Dispose();
                creature.Dispose();
                tiles = new Bitmap(grounds);
                player = new Bitmap(actor);
                creature = new Bitmap(blob);
            }
            Revision++;
        }

        private byte[] MigrateArt(byte[] bytes, byte version)
        {
            var result = (byte[])defaultBundle.Clone();
            Buffer.BlockCopy(bytes, Header.Length, result, Header.Length, OldTileBytes);
            Buffer.BlockCopy(bytes, Header.Length + OldTileBytes, result, Header.Length + TileBytes, PlayerBytes);
            if (version != LegacyVersion) return result;
            for (var id = 0; id < 128; id++)
            {
                if (id != 20 && id != 21 && id != 60 && id != 62 && !TileCatalog.IsMountainStructure(id))
                    continue;

                var region = SpriteRegion(false, id);
                for (var y = region.Top; y < region.Bottom; y++)
                {
                    var offset = Header.Length + (y * TilesWidth + region.Left) * 4;
                    Buffer.BlockCopy(defaultBundle, offset, result, offset, region.Width * 4);
                }
            }
            return result;
        }

        public Bitmap GetSprite(bool character, int index)
        {
            var region = SpriteRegion(character, index);
            var atlas = character ? player : tiles;
            return atlas.Clone(region, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        }

        public void SetSprite(bool character, int index, Bitmap pixels)
        {
            var region = SpriteRegion(character, index);
            var atlas = character ? player : tiles;
            if (pixels.Width != region.Width || pixels.Height != region.Height)
                throw new ArgumentException("The sprite is the wrong size.", "pixels");

            for (var y = 0; y < region.Height; y++)
                for (var x = 0; x < region.Width; x++)
                {
                    var color = pixels.GetPixel(x, y);
                    if (!character && TileCatalog.IsGround(index))
                        color = Color.FromArgb(255, color);
                    atlas.SetPixel(region.X + x, region.Y + y, color);
                }
            Revision++;
        }

        public static Color GetOutfitMarker(int part, int shade)
        {
            return Rgb(OutfitColors[part, 0, shade]);
        }

        public static void UsePixelArt(Graphics graphics)
        {
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
        }

        public void DrawTile(Graphics graphics, int id, Rectangle destination)
        {
            var bounds = SpriteRegion(false, id);
            graphics.DrawImage(tiles, destination, bounds.X, bounds.Y, bounds.Width, bounds.Height, GraphicsUnit.Pixel);
        }

        public void DrawCreature(Graphics graphics, Rectangle destination, int frame)
        {
            graphics.DrawImage(creature, destination, frame * TileSize, 0, TileSize, TileSize, GraphicsUnit.Pixel);
        }

        public Bitmap GetCreatureFrame(int frame)
        {
            return creature.Clone(new Rectangle(frame * TileSize, 0, TileSize, TileSize),
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        }

        public void SetCreatureFrame(int frame, Bitmap pixels)
        {
            if (pixels.Size != new Size(TileSize, TileSize))
                throw new ArgumentException("The creature sprite must be 16 × 16 pixels.", "pixels");
            for (var y = 0; y < TileSize; y++)
                for (var x = 0; x < TileSize; x++)
                    creature.SetPixel(frame * TileSize + x, y, pixels.GetPixel(x, y));
            Revision++;
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

        private static Rectangle SpriteRegion(bool character, int index)
        {
            if (character)
                return new Rectangle(index % 3 * PlayerWidth, index / 3 * PlayerHeight, PlayerWidth, PlayerHeight);
            return new Rectangle(index % 8 * TileSize, index / 8 * TileSize, TileSize, TileSize);
        }

        private static Color Recolor(Color color, byte[] outfit)
        {
            var rgb = color.ToArgb() & 0xFFFFFF;
            for (var part = 0; part < outfit.Length; part++)
                for (var shade = 0; shade < 2; shade++)
                    if (rgb == OutfitColors[part, 0, shade])
                        return Rgb(OutfitColors[part, outfit[part], shade]);
            return color;
        }

        private static Color Rgb(int color)
        {
            return Color.FromArgb(unchecked((int)0xFF000000) | color);
        }

        private static void WriteBitmap(byte[] bytes, ref int offset, Bitmap bitmap)
        {
            for (var y = 0; y < bitmap.Height; y++)
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var color = bitmap.GetPixel(x, y);
                    bytes[offset++] = color.R;
                    bytes[offset++] = color.G;
                    bytes[offset++] = color.B;
                    bytes[offset++] = color.A;
                }
        }

        private static Bitmap ReadBitmap(byte[] bytes, ref int offset, int width, int height)
        {
            var bitmap = new Bitmap(width, height);
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    bitmap.SetPixel(x, y, Color.FromArgb(bytes[offset + 3], bytes[offset], bytes[offset + 1], bytes[offset + 2]));
                    offset += 4;
                }
            return bitmap;
        }

        private static void ValidateOpaqueGround(Bitmap sheet)
        {
            for (var id = 0; id < TileCount; id++)
            {
                if (!TileCatalog.IsGround(id))
                    continue;

                var region = SpriteRegion(false, id);
                for (var y = region.Top; y < region.Bottom; y++)
                    for (var x = region.Left; x < region.Right; x++)
                        if (sheet.GetPixel(x, y).A != 255)
                            throw new InvalidDataException("Ground tile pixels in the art bundle must be opaque.");
            }
        }

        private static Bitmap ReadBitmap(string name)
        {
            using (var stream = OpenAsset(name))
            using (var original = new Bitmap(stream))
                return original.Clone(new Rectangle(Point.Empty, original.Size), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
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
            creature.Dispose();
        }
    }
}
