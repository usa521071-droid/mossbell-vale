using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace MossbellVale.Editor
{
    internal enum MapEventType : byte { Warp = 1, Sign = 2, Npc = 3, Creature = 4 }

    internal sealed class MapEvent
    {
        public readonly MapEventType Type;
        public readonly int X;
        public readonly int Y;
        public readonly string Text;
        public readonly int TargetMap;
        public readonly int TargetX;
        public readonly int TargetY;

        public MapEvent(MapEventType type, int x, int y, string text, int targetMap = 0, int targetX = 0, int targetY = 0)
        {
            Type = type;
            X = x;
            Y = y;
            Text = text;
            TargetMap = targetMap;
            TargetX = targetX;
            TargetY = targetY;
        }

        public bool Matches(MapEvent other)
        {
            return Type == other.Type && X == other.X && Y == other.Y && Text == other.Text &&
                TargetMap == other.TargetMap && TargetX == other.TargetX && TargetY == other.TargetY;
        }
    }

    internal sealed class ProjectMap
    {
        public readonly int Id;
        public string Name;
        public string Group;
        public WorldData World;

        public ProjectMap(int id, string name, string group, WorldData world)
        {
            Id = id;
            Name = name;
            Group = group;
            World = world;
        }

        public override string ToString() { return Name + "  ·  " + Group; }
    }

    internal sealed class ProjectData
    {
        public const int MaximumMaps = 16;
        public const int MaximumEvents = 64;
        public readonly List<ProjectMap> Maps = new List<ProjectMap>();
        public int StartMap;
        private const uint Signature = 0x50475052;
        private const ushort Version = 2;
        private const int MaximumWorldBytes = 32786;

        public ProjectData(string name, string group, WorldData world)
        {
            StartMap = 1;
            Maps.Add(new ProjectMap(StartMap, name, group, world));
        }

        private ProjectData() { }

        public static ProjectData ReadDefault()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MossbellVale.project.rpgp");
            return stream != null ? Read(stream) : new ProjectData("Mossbell Town", "Vale", GameArt.ReadPreset("world"));
        }

        public ProjectMap Find(int id)
        {
            return Maps.Find(map => map.Id == id);
        }

        public ProjectMap Add(string name, string group, WorldData world)
        {
            if (Maps.Count >= MaximumMaps)
                throw new InvalidDataException("The project can contain at most 16 maps.");
            var id = 1;
            while (Find(id) != null)
                id++;
            var map = new ProjectMap(id, name, group, world);
            Maps.Add(map);
            return map;
        }

        public ProjectData Copy()
        {
            var other = new ProjectData { StartMap = StartMap };
            foreach (var map in Maps)
                other.Maps.Add(new ProjectMap(map.Id, map.Name, map.Group, map.World.Copy()));
            return other;
        }

        public bool Matches(ProjectData other)
        {
            if (other == null || StartMap != other.StartMap || Maps.Count != other.Maps.Count)
                return false;
            for (var i = 0; i < Maps.Count; i++)
            {
                var a = Maps[i];
                var b = other.Maps[i];
                if (a.Id != b.Id || a.Name != b.Name || a.Group != b.Group || !a.World.Matches(b.World))
                    return false;
            }
            return true;
        }

        public static ProjectData Read(Stream stream)
        {
            using (var reader = new BinaryReader(stream, Encoding.ASCII))
            {
                var format = reader.ReadUInt32();
                var version = reader.ReadUInt16();
                if (format != Signature || (version != 1 && version != Version))
                    throw new InvalidDataException("The selected file is not a supported Mossbell Vale project.");
                int count = reader.ReadUInt16();
                var project = new ProjectData { StartMap = reader.ReadUInt16() };
                if (reader.ReadUInt16() != 0 || count < 1 || count > MaximumMaps)
                    throw new InvalidDataException("The project header is invalid.");
                for (var i = 0; i < count; i++)
                {
                    int id = reader.ReadUInt16();
                    int worldLength = reader.ReadUInt16();
                    int eventCount = reader.ReadUInt16();
                    int nameLength = reader.ReadByte();
                    int groupLength = reader.ReadByte();
                    if (worldLength < 18 || worldLength > MaximumWorldBytes || eventCount > MaximumEvents)
                        throw new InvalidDataException("A map record has an invalid size.");
                    string name = ReadText(reader, nameLength, 1, 31, "Map name");
                    string group = ReadText(reader, groupLength, 1, 23, "Map group");
                    var world = WorldData.Read(new MemoryStream(ReadBytes(reader, worldLength)), true);
                    ReadBytes(reader, world.Movement.Length).CopyTo(world.Movement, 0);
                    for (var j = 0; j < eventCount; j++)
                    {
                        var type = (MapEventType)reader.ReadByte();
                        int x = reader.ReadByte();
                        int y = reader.ReadByte();
                        int textLength = reader.ReadByte();
                        int destination = reader.ReadUInt16();
                        int targetX = reader.ReadByte();
                        int targetY = reader.ReadByte();
                        string text = ReadText(reader, textLength, 0, 90, "Event text");
                        world.Events.Add(new MapEvent(type, x, y, text, destination, targetX, targetY));
                    }
                    project.Maps.Add(new ProjectMap(id, name, group, world));
                }
                if (stream.ReadByte() != -1)
                    throw new InvalidDataException("Unexpected bytes follow the project.");
                project.Validate();
                return project;
            }
        }

        public byte[] WriteBytes()
        {
            Validate();
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.ASCII))
            {
                writer.Write(Signature);
                writer.Write(Version);
                writer.Write((ushort)Maps.Count);
                writer.Write((ushort)StartMap);
                writer.Write((ushort)0);
                foreach (var map in Maps)
                {
                    var world = map.World;
                    var worldBytes = world.WriteBytes(true);
                    writer.Write((ushort)map.Id);
                    writer.Write((ushort)worldBytes.Length);
                    writer.Write((ushort)world.Events.Count);
                    writer.Write((byte)map.Name.Length);
                    writer.Write((byte)map.Group.Length);
                    writer.Write(Encoding.ASCII.GetBytes(map.Name));
                    writer.Write(Encoding.ASCII.GetBytes(map.Group));
                    writer.Write(worldBytes);
                    writer.Write(world.Movement);
                    foreach (var item in world.Events)
                    {
                        writer.Write((byte)item.Type);
                        writer.Write((byte)item.X);
                        writer.Write((byte)item.Y);
                        writer.Write((byte)item.Text.Length);
                        writer.Write((ushort)item.TargetMap);
                        writer.Write((byte)item.TargetX);
                        writer.Write((byte)item.TargetY);
                        writer.Write(Encoding.ASCII.GetBytes(item.Text));
                    }
                }
                writer.Flush();
                return stream.ToArray();
            }
        }

        public static void ValidateText(string value, int minimum, int maximum, string field)
        {
            if (value == null || value.Length < minimum || value.Length > maximum)
                throw new InvalidDataException(field + " needs " + minimum + "–" + maximum + " characters.");
            foreach (var character in value)
                if (character < ' ' || character > '~')
                    throw new InvalidDataException(field + " accepts printable ASCII characters.");
        }

        public void Validate()
        {
            if (Maps.Count == 0 || Maps.Count > MaximumMaps || Find(StartMap) == null)
                throw new InvalidDataException("Choose a start map from this project.");

            var ids = new HashSet<int>();
            foreach (var map in Maps)
            {
                if (map.Id < 1 || map.Id > UInt16.MaxValue || !ids.Add(map.Id))
                    throw new InvalidDataException("The project contains an invalid or duplicate map ID.");
                ValidateText(map.Name, 1, 31, "Map name");
                ValidateText(map.Group, 1, 23, "Map group");
                var world = map.World;
                foreach (var movement in world.Movement)
                    if (movement > 2)
                        throw new InvalidDataException(map.Name + " contains an unknown movement tile.");
                if (world.Events.Count > MaximumEvents)
                    throw new InvalidDataException(map.Name + " has more than 64 events.");
                var positions = new HashSet<int>();
                foreach (var item in world.Events)
                {
                    if (!Contains(world, item.X, item.Y) || !positions.Add(item.Y * world.Width + item.X))
                        throw new InvalidDataException(map.Name + " contains two events on the same tile or an event outside the map.");
                    ValidateEvent(map, item);
                }
                if (!world.CanWalk(world.SpawnX, world.SpawnY) || FindEvent(world, world.SpawnX, world.SpawnY) != null)
                    throw new InvalidDataException(map.Name + " needs a walkable player start without an event on it.");
            }
        }

        private void ValidateEvent(ProjectMap map, MapEvent item)
        {
            string label = map.Name + " (" + item.X + ", " + item.Y + ")";
            if (item.Type == MapEventType.Warp)
            {
                ValidateText(item.Text, 0, 90, "Warp text");
                var destination = Find(item.TargetMap);
                if (!map.World.CanWalk(item.X, item.Y))
                    throw new InvalidDataException("Warp at " + label + " must be on a walkable tile.");
                if (destination == null || !destination.World.CanWalk(item.TargetX, item.TargetY) ||
                    FindEvent(destination.World, item.TargetX, item.TargetY) != null)
                    throw new InvalidDataException("Warp at " + label + " needs a walkable destination without another event.");
                return;
            }

            if (item.Type != MapEventType.Sign && item.Type != MapEventType.Npc && item.Type != MapEventType.Creature)
                throw new InvalidDataException(label + " has an unknown event type.");
            if (item.TargetMap != 0 || item.TargetX != 0 || item.TargetY != 0)
                throw new InvalidDataException(label + " has a warp destination for a sign or NPC.");
            if (item.Type == MapEventType.Creature)
            {
                ValidateText(item.Text, 0, 90, "Creature message");
                if (!map.World.CanWalk(item.X, item.Y))
                    throw new InvalidDataException("Creature at " + label + " needs a walkable home tile.");
            }
            else ValidateText(item.Text, 1, 90, "Sign or NPC message");
        }

        public static MapEvent FindEvent(WorldData world, int x, int y)
        {
            return world.Events.Find(item => item.X == x && item.Y == y);
        }

        private static bool Contains(WorldData world, int x, int y)
        {
            return x >= 0 && y >= 0 && x < world.Width && y < world.Height;
        }

        private static string ReadText(BinaryReader reader, int count, int minimum, int maximum, string field)
        {
            var bytes = ReadBytes(reader, count);
            foreach (byte character in bytes)
                if (character < 32 || character > 126)
                    throw new InvalidDataException(field + " contains a character the game cannot display.");
            var value = Encoding.ASCII.GetString(bytes);
            ValidateText(value, minimum, maximum, field);
            return value;
        }

        private static byte[] ReadBytes(BinaryReader reader, int count)
        {
            var bytes = reader.ReadBytes(count);
            if (bytes.Length != count)
                throw new EndOfStreamException("The project ended in the middle of a map record.");
            return bytes;
        }
    }
}
