using System;
using System.Collections.Generic;

namespace MossbellVale.Editor
{
    internal sealed class TileSet
    {
        public readonly string Name;
        public readonly int First;
        public readonly int Count;

        public TileSet(string name, int first, int count)
        {
            Name = name;
            First = first;
            Count = count;
        }

        public override string ToString() { return Name; }
    }

    internal sealed class TileStamp
    {
        public readonly string Name;
        public readonly int First;
        public readonly int Width;
        public readonly int Height;
        public readonly bool Reversed;

        public TileStamp(string name, int first, int width, int height, bool reversed = false)
        {
            Name = name;
            First = first;
            Width = width;
            Height = height;
            Reversed = reversed;
        }

        public override string ToString() { return Name; }
    }

    internal static class TileCatalog
    {
        public static readonly TileSet[] GroundSets = {
            new TileSet("Meadow and trails", 0, 16),
            new TileSet("Path edges and corners", 32, 12),
            new TileSet("Shore edges and corners", 44, 12),
            new TileSet("Cave floors", 56, 4),
            new TileSet("Mountain floors", 60, 4),
            new TileSet("Mountain cliff tiles", 108, 12),
            new TileSet("Mountain stairs", 120, 3),
            new TileSet("Side stairs and ladders", 128, 16)
        };

        public static readonly TileSet[] ObjectSets = {
            new TileSet("Trees, rocks and landmarks", 16, 16),
            new TileSet("Roof pieces", 64, 9),
            new TileSet("Building facades", 73, 9),
            new TileSet("Fences and shadows", 82, 6),
            new TileSet("Large oak pieces", 88, 8),
            new TileSet("Cave cliff edges and corners", 96, 12),
            new TileSet("Rock and ruin details", 123, 5)
        };

        public static readonly TileStamp[] Stamps = {
            new TileStamp("Paint one tile", 0, 1, 1),
            new TileStamp("Roof · 3 × 3", 64, 3, 3),
            new TileStamp("Facade · 3 × 2", 73, 3, 2),
            new TileStamp("Oak canopy · 3 × 2", 88, 3, 2),
            new TileStamp("Mountain stairs · 1 × 3", 120, 1, 3),
            new TileStamp("East stairs · 3 × 1", 128, 3, 1),
            new TileStamp("West stairs · 3 × 1", 131, 3, 1, true),
            new TileStamp("Outdoor ladder · 1 × 3", 134, 1, 3),
            new TileStamp("Cave ladder · 1 × 3", 137, 1, 3),
            new TileStamp("Cave east stairs · 2 × 1", 140, 2, 1),
            new TileStamp("Cave west stairs · 2 × 1", 142, 2, 1, true)
        };

        public static readonly string[] Names = MakeNames();

        public static bool IsGround(int id) { return (id >= 0 && id <= 15) || (id >= 32 && id <= 63) || (id >= 108 && id <= 122) || (id >= 128 && id <= 143); }

        public static bool IsObject(int id) { return (id >= 16 && id <= 31) || (id >= 64 && id <= 107) || (id >= 123 && id <= 127); }

        public static bool IsMountainStructure(int id) { return id == 61 || (id >= 108 && id <= 122) || (id >= 128 && id <= 143); }

        private static string[] MakeNames()
        {
            var names = new List<string> {
                "Meadow", "Meadow Tufts", "Daisies", "Clover", "Clay Path", "Pebbled Path", "Sand", "Boardwalk",
                "Shallows", "Water", "Deep Water", "Rapids", "Stone Trail", "Moss", "Tall Grass", "Cobblestone",
                "Fir", "Oak", "Shrub", "Boulder", "Broad Boulder", "Slate Cluster", "Fern", "Wildflowers",
                "Hollow Log", "Fence", "Shop Awning", "Red Roof", "Stump", "Stone Arch", "Trail Sign", "Glowbell"
            };
            AddEdges(names, "Path");
            AddEdges(names, "Shore");
            names.AddRange(new[] { "Cave Floor", "Cave Gravel", "Cave Silt", "Cave Track", "Rocky Ground", "Upper Cliff Face",
                "Slope Grass", "Slate Pavers" });
            AddPieces(names, "Roof", 3);
            AddPieces(names, "Facade", 2);
            names.AddRange(new[] { "Door", "Window", "Chimney", "Fence Left", "Fence Rail", "Fence Right",
                "Fence Corner", "Shadow East", "Shadow South" });
            AddPieces(names, "Oak Canopy", 2);
            names.AddRange(new[] { "Oak Trunk", "Oak Shadow" });
            AddEdges(names, "Cave Cliff");
            AddEdges(names, "Mountain Cliff");
            names.AddRange(new[] { "Stairs Top", "Stairs Middle", "Stairs Bottom", "Grass Fringe", "Rock Chips",
                "Cave Crystal", "Tall Boulder", "Stone Pillar" });
            names.AddRange(new[] { "East Stairs Foot", "East Stairs Middle", "East Stairs Crest",
                "West Stairs Foot", "West Stairs Middle", "West Stairs Crest",
                "Ladder Top", "Ladder Middle", "Ladder Foot", "Cave Ladder Top", "Cave Ladder Middle", "Cave Ladder Foot",
                "Cave East Stairs Foot", "Cave East Stairs Crest", "Cave West Stairs Foot", "Cave West Stairs Crest" });
            return names.ToArray();
        }

        private static void AddPieces(List<string> names, string prefix, int height)
        {
            var rows = height == 3 ? new[] { "top", "middle", "bottom" } : new[] { "top", "bottom" };
            foreach (var row in rows)
                foreach (var column in new[] { "left", "center", "right" })
                    names.Add(prefix + " " + row + " " + column);
        }

        private static void AddEdges(List<string> names, string prefix)
        {
            var suffixes = new[] { "North", "East", "South", "West", "NW outside corner", "NE outside corner",
                "SE outside corner", "SW outside corner", "NW inside corner", "NE inside corner", "SE inside corner", "SW inside corner" };
            foreach (var suffix in suffixes)
                names.Add(prefix + " " + suffix);
        }
    }
}
