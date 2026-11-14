using System;
using System.Collections.Generic;
using System.Drawing;

namespace MossbellVale.Editor
{
    internal sealed class PlaytestSession
    {
        public const int CellSize = 16;
        private const int Glowbell = 31;
        private const int Arch = 29;
        private const int Sign = 30;
        private const int CreatureStepTicks = 8;
        private static readonly Point[] Steps = {
            new Point(0, 1), new Point(-1, 0), new Point(1, 0), new Point(0, -1)
        };

        internal sealed class Creature
        {
            public readonly MapEvent Event;
            public Point Position;
            public Point Previous;
            public int Remaining;
            public int Cooldown;

            public Point PixelPosition
            {
                get
                {
                    return new Point(Position.X * CellSize + (Previous.X - Position.X) * Remaining * 2,
                        Position.Y * CellSize + (Previous.Y - Position.Y) * Remaining * 2);
                }
            }
            public Creature(MapEvent item)
            {
                Event = item;
                Position = new Point(item.X, item.Y);
                Previous = Position;
                Cooldown = 28 + (item.X + item.Y) % 10;
            }
        }

        private readonly ProjectData project;
        private readonly ProjectMap initialMap;
        private readonly Dictionary<int, HashSet<int>> collected = new Dictionary<int, HashSet<int>>();
        private readonly Dictionary<int, List<Creature>> creatures = new Dictionary<int, List<Creature>>();
        private Point destination;
        private int distanceRemaining;
        private int ticks;

        public ProjectMap Map { get; private set; }
        public Point Position { get; private set; }
        public Point PixelPosition { get; private set; }
        public int Direction { get; private set; }
        public int Frame { get; private set; }
        public int Found { get; private set; }
        public int Total { get; private set; }
        public bool Complete { get; private set; }
        public byte[] Outfit { get; private set; }
        public List<Creature> Creatures { get { return creatures[Map.Id]; } }
        public int CreatureFrame { get { return ticks / 7 % 3; } }
        public event Action<string> Message;

        public PlaytestSession(ProjectData source, int initialMap)
        {
            project = source.Copy();
            this.initialMap = project.Find(initialMap);
            if (this.initialMap == null)
                throw new ArgumentException("Choose a map included in the project.", "initialMap");
            project.Validate();
            foreach (var map in project.Maps)
                foreach (var id in map.World.Objects)
                    if (id == Glowbell)
                        Total++;
            Restart();
        }

        public void Restart()
        {
            collected.Clear();
            creatures.Clear();
            Found = 0;
            Complete = false;
            Frame = 0;
            Direction = 0;
            ticks = 0;
            Outfit = (byte[])initialMap.World.Outfit.Clone();
            Visit(initialMap, new Point(initialMap.World.SpawnX, initialMap.World.SpawnY));
            Say("");
        }

        public bool IsCollected(int cell)
        {
            return collected[Map.Id].Contains(cell);
        }

        public void Tick(int direction, bool run, bool interact)
        {
            ticks++;
            MoveCreatures();
            if (distanceRemaining > 0)
            {
                var speed = Math.Min(distanceRemaining, run ? 8 : 4);
                PixelPosition = Offset(PixelPosition, Steps[Direction], speed);
                distanceRemaining -= speed;
                Frame = 1 + ((PixelPosition.X + PixelPosition.Y) / CellSize & 1);
                if (distanceRemaining == 0)
                    FinishStep();
                return;
            }

            Frame = 0;
            if (interact)
            {
                Interact();
                return;
            }
            if (direction < 0)
                return;

            Direction = direction;
            var next = Offset(Position, Steps[direction], 1);
            if (!CanPlayerEnter(next))
                return;

            destination = next;
            distanceRemaining = CellSize;
        }

        private bool CanPlayerEnter(Point next)
        {
            if (!Map.World.CanWalk(next.X, next.Y) || CreatureAt(next) != null)
                return false;

            var item = ProjectData.FindEvent(Map.World, next.X, next.Y);
            return item == null || item.Type == MapEventType.Warp || item.Type == MapEventType.Creature;
        }

        private void FinishStep()
        {
            Position = destination;
            var cell = Position.Y * Map.World.Width + Position.X;
            if (Map.World.Objects[cell] == Glowbell && collected[Map.Id].Add(cell))
            {
                Found++;
                Say(Found == Total ? "All glowbells found. Face the stone arch and interact." : "You found a glowbell!");
            }

            var item = ProjectData.FindEvent(Map.World, Position.X, Position.Y);
            if (item != null && item.Type == MapEventType.Warp)
            {
                Visit(project.Find(item.TargetMap), new Point(item.TargetX, item.TargetY));
                Say(String.IsNullOrEmpty(item.Text) ? Map.Name : item.Text);
            }
        }

        private void Visit(ProjectMap next, Point landing)
        {
            Map = next;
            Position = landing;
            destination = landing;
            PixelPosition = new Point(landing.X * CellSize, landing.Y * CellSize);
            distanceRemaining = 0;
            Frame = 0;
            if (!collected.ContainsKey(next.Id))
                collected.Add(next.Id, new HashSet<int>());
            if (!creatures.ContainsKey(next.Id))
            {
                var occupants = new List<Creature>();
                foreach (var item in next.World.Events)
                    if (item.Type == MapEventType.Creature)
                        occupants.Add(new Creature(item));
                creatures.Add(next.Id, occupants);
            }
        }

        private void Interact()
        {
            var next = Offset(Position, Steps[Direction], 1);
            if (next.X < 0 || next.X >= Map.World.Width || next.Y < 0 || next.Y >= Map.World.Height)
                return;

            var creature = CreatureAt(next);
            if (creature != null)
            {
                Say(String.IsNullOrEmpty(creature.Event.Text)
                    ? "The little pink blob blinks at you." : creature.Event.Text);
                return;
            }
            var item = ProjectData.FindEvent(Map.World, next.X, next.Y);
            if (item != null && (item.Type == MapEventType.Npc || item.Type == MapEventType.Sign))
            {
                Say(item.Text);
                return;
            }

            var objectId = Map.World.Objects[next.Y * Map.World.Width + next.X];
            if (objectId == Sign)
                Say("Welcome, trailkeeper! Gather the glowbells, then ring the stone arch.");
            else if (objectId == Arch && Found < Total)
                Say("The arch is quiet. Gather the remaining glowbells.");
            else if (objectId == Arch && !Complete)
            {
                Complete = true;
                Say("The arch sings. You restored the light of Mossbell Vale!");
            }
            else if (objectId == Arch)
                Say("The arch is warm. The vale remembers its trailkeeper.");
        }

        private void MoveCreatures()
        {
            foreach (var creature in Creatures)
            {
                if (creature.Remaining > 0)
                    creature.Remaining--;
                if (--creature.Cooldown > 0)
                    continue;

                var home = creature.Event;
                creature.Cooldown = 28 + (home.X + home.Y) % 10;
                var start = (creature.Position.X * 3 + creature.Position.Y + home.X + home.Y) % Steps.Length;
                for (var attempt = 0; attempt < Steps.Length; attempt++)
                {
                    var next = Offset(creature.Position, Steps[(start + attempt) % Steps.Length], 1);
                    if (Math.Abs(next.X - home.X) + Math.Abs(next.Y - home.Y) > 2 ||
                        next == Position || next == destination || !Map.World.CanWalk(next.X, next.Y) ||
                        CreatureAt(next) != null || Map.World.Objects[next.Y * Map.World.Width + next.X] == Glowbell)
                        continue;

                    var item = ProjectData.FindEvent(Map.World, next.X, next.Y);
                    if (item != null && !Object.ReferenceEquals(item, home))
                        continue;

                    creature.Previous = creature.Position;
                    creature.Position = next;
                    creature.Remaining = CreatureStepTicks;
                    break;
                }
            }
        }

        private Creature CreatureAt(Point position)
        {
            return Creatures.Find(creature => creature.Position == position ||
                (creature.Remaining > 0 && creature.Previous == position));
        }

        private static Point Offset(Point start, Point direction, int amount)
        {
            return new Point(start.X + direction.X * amount, start.Y + direction.Y * amount);
        }

        private void Say(string text)
        {
            if (Message != null)
                Message(text);
        }
    }
}
