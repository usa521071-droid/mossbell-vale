using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace MossbellVale.Editor
{
    internal sealed class WorldData
    {
        private const ushort LegacyFormatVersion = 1;
        private const ushort FormatVersion = 2;
        private const uint Signature = 0x57475052;
        private const uint ReplaceExisting = 0x1;
        private const uint WriteThrough = 0x8;
        public const int MinimumSize = 8;
        public const int MaximumSize = 128;
        public const int PaletteSize = 6;

        public readonly int Width;
        public readonly int Height;
        public readonly byte[] Ground;
        public readonly byte[] Objects;
        public readonly byte[] Movement;
        public readonly List<MapEvent> Events = new List<MapEvent>();
        public readonly byte[] Outfit = new byte[4];
        public int SpawnX;
        public int SpawnY;
        public bool WasMigrated { get; private set; }

        public WorldData(int width, int height)
        {
            if (width < MinimumSize || width > MaximumSize || height < MinimumSize || height > MaximumSize)
                throw new InvalidDataException("Map dimensions must be between 8 and 128 tiles.");

            Width = width;
            Height = height;
            Ground = new byte[width * height];
            Objects = new byte[width * height];
            Movement = new byte[width * height];
            SpawnX = width / 2;
            SpawnY = height / 2;
        }

        public static WorldData Read(Stream stream, bool project = false)
        {
            using (var reader = new BinaryReader(stream))
            {
                var signature = reader.ReadUInt32();
                var version = reader.ReadUInt16();
                if (signature != Signature || (version != LegacyFormatVersion && version != FormatVersion))
                    throw new InvalidDataException("This is not a Mossbell Vale world file supported by this editor.");

                var world = new WorldData(reader.ReadUInt16(), reader.ReadUInt16());
                world.SpawnX = reader.ReadUInt16();
                world.SpawnY = reader.ReadUInt16();
                ReadBytes(reader, world.Outfit);
                ReadBytes(reader, world.Ground);
                ReadBytes(reader, world.Objects);
                if (stream.ReadByte() != -1)
                    throw new InvalidDataException("The world file contains unexpected data.");

                if (version == LegacyFormatVersion)
                {
                    world.MigrateMountains();
                    world.WasMigrated = true;
                }
                world.Validate(!project);
                return world;
            }
        }

        public byte[] WriteBytes(bool project = false)
        {
            Validate(!project);
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Signature);
                writer.Write(FormatVersion);
                writer.Write((ushort)Width);
                writer.Write((ushort)Height);
                writer.Write((ushort)SpawnX);
                writer.Write((ushort)SpawnY);
                writer.Write(Outfit);
                writer.Write(Ground);
                writer.Write(Objects);
                writer.Flush();
                return stream.ToArray();
            }
        }

        public static void WriteAtomically(string path, byte[] bytes)
        {
            path = Path.GetFullPath(path);
            var temporary = Path.Combine(Path.GetDirectoryName(path), Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                if (!MoveFileEx(temporary, path, ReplaceExisting | WriteThrough))
                    throw new IOException("Windows could not replace the file.", new Win32Exception());
            }
            finally
            {
                File.Delete(temporary);
            }
        }

        public WorldData Copy()
        {
            var copy = new WorldData(Width, Height) { SpawnX = SpawnX, SpawnY = SpawnY };
            Ground.CopyTo(copy.Ground, 0);
            Objects.CopyTo(copy.Objects, 0);
            Movement.CopyTo(copy.Movement, 0);
            copy.Events.AddRange(Events);
            Outfit.CopyTo(copy.Outfit, 0);
            return copy;
        }

        public bool Matches(WorldData other)
        {
            if (other == null || Width != other.Width || Height != other.Height || SpawnX != other.SpawnX || SpawnY != other.SpawnY)
                return false;

            for (var i = 0; i < Outfit.Length; i++)
                if (Outfit[i] != other.Outfit[i])
                    return false;

            for (var i = 0; i < Ground.Length; i++)
                if (Ground[i] != other.Ground[i] || Objects[i] != other.Objects[i] || Movement[i] != other.Movement[i])
                    return false;

            if (Events.Count != other.Events.Count)
                return false;
            for (var i = 0; i < Events.Count; i++)
                if (!Events[i].Matches(other.Events[i]))
                    return false;
            return true;
        }

        public bool CanWalk(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                return false;
            var index = y * Width + x;
            return CanWalk(Ground[index], Objects[index], Movement[index]);
        }

        public static bool CanWalk(byte ground, byte item, byte movement)
        {
            return movement == 1 || (movement == 0 && IsWalkable(ground, item));
        }

        public static bool IsWalkable(byte ground, byte item)
        {
            var blockedGround = (ground >= 8 && ground <= 11) || ground == 61 || (ground >= 108 && ground <= 119);
            var passableObject = item == 0 || item == 22 || item == 23 || item == 31 ||
                item == 86 || item == 87 || item == 95 || (item >= 123 && item <= 125);
            return !blockedGround && passableObject;
        }

        private void MigrateMountains()
        {
            for (var i = 0; i < Ground.Length; i++)
            {
                var ground = Ground[i];
                var item = Objects[i];
                if (!(ground < 16 || (ground >= 32 && ground < 64)) ||
                    !(item == 0 || (item >= 16 && item < 32) || (item >= 64 && item < 128)))
                    throw new InvalidDataException("The legacy world file contains an unknown tile.");

                if (item >= 108 && item <= 122)
                {
                    Ground[i] = item;
                    Objects[i] = 0;
                }
                else if (ground == 61)
                    Ground[i] = 60;
            }
        }

        private void Validate(bool checkTerrainSpawn)
        {
            foreach (var choice in Outfit)
                if (choice >= PaletteSize)
                    throw new InvalidDataException("The world file contains an unknown outfit color.");

            for (var i = 0; i < Ground.Length; i++)
                if (!TileCatalog.IsGround(Ground[i]) || (Objects[i] != 0 && !TileCatalog.IsObject(Objects[i])))
                    throw new InvalidDataException("The world file contains an unknown tile.");

            if (SpawnX < 0 || SpawnX >= Width || SpawnY < 0 || SpawnY >= Height ||
                (checkTerrainSpawn && !IsWalkable(Ground[SpawnY * Width + SpawnX], Objects[SpawnY * Width + SpawnX])))
                throw new InvalidDataException("The player's starting tile must be walkable and inside the map.");
        }

        private static void ReadBytes(BinaryReader reader, byte[] data)
        {
            var bytes = reader.ReadBytes(data.Length);
            if (bytes.Length != data.Length)
                throw new EndOfStreamException("The world file ended unexpectedly.");
            bytes.CopyTo(data, 0);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool MoveFileEx(string existingPath, string newPath, uint flags);
    }
}
