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
        public const int PlayerHeight = 16;
        public const int PlayerDirections = 4;
        public const int PlayerFrames = 3;
        public const int NpcVariants = 4;
        public const int NpcFrames = 2;
        public const int LegacyPlayerHeight = 24;
        private const int TilesWidth = 128;
        private const int TilesHeight = 288;
        private const int OldTileBytes = TilesWidth * 256 * 4;
        private const int PlayerSheetWidth = PlayerWidth * PlayerFrames;
        private const int PlayerSheetHeight = PlayerHeight * PlayerDirections;
        private const int CreatureWidth = 48;
        private const int CreatureHeight = 16;
        private const int NpcWidth = NpcVariants * TileSize;
        private const int NpcHeight = NpcFrames * TileSize;
        private const int TileBytes = TilesWidth * TilesHeight * 4;
        private const int PlayerRowBytes = PlayerSheetWidth * 4;
        private const int PlayerBytes = PlayerRowBytes * PlayerSheetHeight;
        private const int LegacyPlayerBytes = PlayerRowBytes * LegacyPlayerHeight * PlayerDirections;
        private const int CreatureBytes = CreatureWidth * CreatureHeight * 4;
        private const int NpcBytes = NpcWidth * NpcHeight * 4;
        private const int ArtFileSize = 8 + TileBytes + PlayerBytes + CreatureBytes + NpcBytes;
        private const int VersionOffset = 4;
        private const byte LegacyVersion = 1;
        private const byte Version = 4;
        private static readonly byte[] Header = { (byte)'R', (byte)'P', (byte)'G', (byte)'A', Version, 0, 0, 0 };

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
        private Bitmap npc;
        public int Revision { get; private set; }

        public GameArt()
        {
            tiles = ReadBitmap("tiles.png", TilesWidth, TilesHeight);
            player = ReadBitmap("player.png", PlayerSheetWidth, PlayerSheetHeight);
            creature = ReadBitmap("blob.png", CreatureWidth, CreatureHeight);
            npc = ReadBitmap("npc.png", NpcWidth, NpcHeight);
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
            WriteBitmap(bytes, ref offset, npc);
            return bytes;
        }

        public void LoadBundle(byte[] bytes)
        {
            if (bytes == null)
                bytes = defaultBundle;
            if (bytes.Length < Header.Length)
                throw new InvalidDataException("The art bundle has an incorrect size.");
            var version = bytes[VersionOffset];
            var expectedSize = ArtFileSize;
            if (version == 3)
                expectedSize = Header.Length + TileBytes + LegacyPlayerBytes + CreatureBytes;
            else if (version == 1 || version == 2)
                expectedSize = Header.Length + OldTileBytes + LegacyPlayerBytes;
            if (bytes.Length != expectedSize)
                throw new InvalidDataException("The art bundle has an incorrect size.");
            for (var i = 0; i < Header.Length; i++)
                if (bytes[i] != Header[i] && !(version >= LegacyVersion && version < Version && i == VersionOffset))
                    throw new InvalidDataException("The art bundle is missing a supported RPGA header.");

            if (version < Version)
                bytes = MigrateArt(bytes, version);

            var offset = Header.Length;
            var groundSheet = ReadBitmap(bytes, ref offset, TilesWidth, TilesHeight);
            try
            {
                ValidateOpaqueGround(groundSheet);
            }
            catch (InvalidDataException)
            {
                groundSheet.Dispose();
                throw;
            }
            var actorSheet = ReadBitmap(bytes, ref offset, PlayerSheetWidth, PlayerSheetHeight);
            var blobSheet = ReadBitmap(bytes, ref offset, CreatureWidth, CreatureHeight);
            var npcSheet = ReadBitmap(bytes, ref offset, NpcWidth, NpcHeight);
            tiles.Dispose();
            player.Dispose();
            creature.Dispose();
            npc.Dispose();
            tiles = groundSheet;
            player = actorSheet;
            creature = blobSheet;
            npc = npcSheet;
            Revision++;
        }

        private byte[] MigrateArt(byte[] bytes, byte version)
        {
            var result = (byte[])defaultBundle.Clone();
            var oldTileSize = version == 3 ? TileBytes : OldTileBytes;
            Buffer.BlockCopy(bytes, Header.Length, result, Header.Length, oldTileSize);

            var fromPlayer = Header.Length + oldTileSize;
            var toPlayer = Header.Length + TileBytes;
            MigratePlayerFrames(bytes, fromPlayer, result, toPlayer);

            if (version == 3)
                Buffer.BlockCopy(bytes, fromPlayer + LegacyPlayerBytes, result, toPlayer + PlayerBytes, CreatureBytes);
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

        private static void MigratePlayerFrames(byte[] source, int fromSheet, byte[] target, int toSheet)
        {
            for (var direction = 0; direction < PlayerDirections; direction++)
                for (var frame = 0; frame < PlayerFrames; frame++)
                {
                    var from = fromSheet + direction * LegacyPlayerHeight * PlayerRowBytes + frame * PlayerWidth * 4;
                    var to = toSheet + direction * PlayerHeight * PlayerRowBytes + frame * PlayerWidth * 4;
                    MigratePlayerFrame(source, from, PlayerRowBytes, target, to, PlayerRowBytes);
                }
        }

        public static Bitmap ConvertLegacyFrame(Bitmap frame)
        {
            if (frame.Width != PlayerWidth || frame.Height != LegacyPlayerHeight)
                throw new ArgumentException("The legacy player frame must be 16 × 24 pixels.", "frame");

            var rowBytes = PlayerWidth * 4;
            var source = new byte[rowBytes * LegacyPlayerHeight];
            var result = new byte[rowBytes * PlayerHeight];
            var offset = 0;
            WriteBitmap(source, ref offset, frame);
            MigratePlayerFrame(source, 0, rowBytes, result, 0, rowBytes);
            offset = 0;
            return ReadBitmap(result, ref offset, PlayerWidth, PlayerHeight);
        }

        private static void MigratePlayerFrame(byte[] source, int from, int fromStride, byte[] target, int to, int toStride)
        {
            var top = LegacyPlayerHeight;
            var bottom = -1;
            for (var y = 0; y < LegacyPlayerHeight; y++)
                for (var x = 0; x < PlayerWidth; x++)
                    if (source[from + y * fromStride + x * 4 + 3] != 0)
                    {
                        top = Math.Min(top, y);
                        bottom = Math.Max(bottom, y);
                    }

            var visibleRows = bottom - top + 1;
            var firstRow = Math.Max(0, Math.Min(LegacyPlayerHeight - PlayerHeight, bottom + 1 - PlayerHeight));
            for (var y = 0; y < PlayerHeight; y++)
            {
                var destination = to + y * toStride;
                if (bottom < 0)
                {
                    Array.Clear(target, destination, PlayerWidth * 4);
                    continue;
                }

                var row = visibleRows > PlayerHeight
                    ? top + (2 * y + 1) * visibleRows / (2 * PlayerHeight)
                    : firstRow + y;
                Buffer.BlockCopy(source, from + row * fromStride, target, destination, PlayerWidth * 4);
            }
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
            SetSmallFrame(creature, frame * TileSize, 0, pixels);
        }

        public void DrawNpc(Graphics graphics, Rectangle destination, int variant, int frame = 0)
        {
            graphics.DrawImage(npc, destination, variant * TileSize, frame * TileSize, TileSize, TileSize, GraphicsUnit.Pixel);
        }

        public Bitmap GetNpcFrame(int index)
        {
            var region = new Rectangle(index % NpcVariants * TileSize, index / NpcVariants * TileSize, TileSize, TileSize);
            return npc.Clone(region, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        }

        public void SetNpcFrame(int index, Bitmap pixels)
        {
            SetSmallFrame(npc, index % NpcVariants * TileSize, index / NpcVariants * TileSize, pixels);
        }

        private void SetSmallFrame(Bitmap sheet, int left, int top, Bitmap pixels)
        {
            if (pixels.Size != new Size(TileSize, TileSize))
                throw new ArgumentException("The sprite must be 16 × 16 pixels.", "pixels");
            for (var y = 0; y < TileSize; y++)
                for (var x = 0; x < TileSize; x++)
                    sheet.SetPixel(left + x, top + y, pixels.GetPixel(x, y));
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
                return new Rectangle(index % PlayerFrames * PlayerWidth, index / PlayerFrames * PlayerHeight, PlayerWidth, PlayerHeight);
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

        private static Bitmap ReadBitmap(string name, int width, int height)
        {
            using (var stream = OpenAsset(name))
            using (var original = new Bitmap(stream))
            {
                if (original.Width != width || original.Height != height)
                    throw new InvalidDataException(name + " must be " + width + " × " + height + " pixels.");
                return original.Clone(new Rectangle(Point.Empty, original.Size), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }
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
            npc.Dispose();
        }
    }
}
