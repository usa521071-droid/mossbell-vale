using System;
using System.Drawing;
using System.Windows.Forms;

namespace MossbellVale.Editor
{
    internal sealed class MapCanvas : Control
    {
        private readonly GameArt art;
        private WorldData world;
        private Bitmap player;
        private EditorView view;
        private int zoom = 2;
        private Rectangle highlighted = Rectangle.Empty;
        private Point hovered = new Point(-1, -1);

        public event Action<Point, MouseButtons> StrokeStarted;
        public event Action<Point> StrokeContinued;
        public event Action StrokeEnded;
        public event Action<Point> TileHovered;
        public bool ShowGrid { get; set; }

        public int Zoom
        {
            get { return zoom; }
            set
            {
                zoom = value;
                ResizeCanvas();
            }
        }

        private int TilePixels { get { return GameArt.TileSize * zoom; } }

        public MapCanvas(GameArt art)
        {
            this.art = art;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint | ControlStyles.Opaque | ControlStyles.ResizeRedraw, true);
            AccessibleName = "World map";
            Cursor = Cursors.Cross;
            ShowGrid = true;
        }

        public void SetView(EditorView display)
        {
            view = display;
            HighlightRegion(Rectangle.Empty);
        }

        public void Display(WorldData map, Bitmap sprite)
        {
            if (!Object.ReferenceEquals(world, map))
                highlighted = Rectangle.Empty;
            world = map;
            player = sprite;
            ResizeCanvas();
        }

        private void ResizeCanvas()
        {
            if (world == null)
                return;
            Size = new Size(world.Width * TilePixels, world.Height * TilePixels);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (world == null)
                return;

            var pixels = TilePixels;
            var firstX = Math.Max(0, e.ClipRectangle.Left / pixels);
            var firstY = Math.Max(0, e.ClipRectangle.Top / pixels);
            var lastX = Math.Min(world.Width - 1, (e.ClipRectangle.Right - 1) / pixels);
            var lastY = Math.Min(world.Height - 1, (e.ClipRectangle.Bottom - 1) / pixels);
            GameArt.UsePixelArt(e.Graphics);
            e.Graphics.FillRectangle(Brushes.Black, e.ClipRectangle);
            for (var y = firstY; y <= lastY; y++)
            {
                for (var x = firstX; x <= lastX; x++)
                {
                    var index = y * world.Width + x;
                    var bounds = new Rectangle(x * pixels, y * pixels, pixels, pixels);
                    art.DrawTile(e.Graphics, world.Ground[index], bounds);
                    if (world.Objects[index] != 0)
                        art.DrawTile(e.Graphics, world.Objects[index], bounds);
                }
            }

            foreach (var item in world.Events)
                if (item.Type == MapEventType.Creature && item.X >= firstX && item.X <= lastX &&
                    item.Y >= firstY && item.Y <= lastY)
                    art.DrawCreature(e.Graphics, new Rectangle(item.X * pixels, item.Y * pixels, pixels, pixels), 0);

            if (view == EditorView.Movement)
                DrawMovement(e.Graphics, firstX, lastX, firstY, lastY, pixels);
            else if (view == EditorView.Events || view == EditorView.Connections)
                DrawEvents(e.Graphics, pixels);

            if (ShowGrid)
            {
                using (var grid = new Pen(Color.FromArgb(45, 12, 22, 18)))
                {
                    for (var x = firstX; x <= lastX; x++)
                        e.Graphics.DrawLine(grid, x * pixels, firstY * pixels, x * pixels, (lastY + 1) * pixels);
                    for (var y = firstY; y <= lastY; y++)
                        e.Graphics.DrawLine(grid, firstX * pixels, y * pixels, (lastX + 1) * pixels, y * pixels);
                }
            }

            var spawn = new Rectangle(world.SpawnX * pixels, world.SpawnY * pixels, pixels, pixels);
            e.Graphics.FillEllipse(Brushes.MediumSpringGreen, spawn.X + pixels / 6, spawn.Y + pixels / 2,
                pixels * 2 / 3, pixels * 2 / 5);
            if (player != null)
                GameArt.DrawPlayer(e.Graphics, player, 0, 0,
                    new Rectangle(spawn.X, spawn.Bottom - GameArt.PlayerHeight * zoom, pixels, GameArt.PlayerHeight * zoom));

            if (!highlighted.IsEmpty)
            {
                var bounds = new Rectangle(highlighted.X * pixels, highlighted.Y * pixels,
                    highlighted.Width * pixels, highlighted.Height * pixels);
                using (var fill = new SolidBrush(Color.FromArgb(78, 238, 203, 115)))
                using (var edge = new Pen(Color.FromArgb(250, 233, 159), 3))
                {
                    e.Graphics.FillRectangle(fill, bounds);
                    e.Graphics.DrawRectangle(edge, bounds);
                }
            }

            if (ContainsTile(hovered))
            {
                using (var highlight = new Pen(Color.White, 2))
                    e.Graphics.DrawRectangle(highlight, hovered.X * pixels + 1, hovered.Y * pixels + 1, pixels - 2, pixels - 2);
            }
        }

        private void DrawMovement(Graphics graphics, int firstX, int lastX, int firstY, int lastY, int pixels)
        {
            using (var blocked = new SolidBrush(Color.FromArgb(149, 157, 38, 49)))
            using (var allowed = new SolidBrush(Color.FromArgb(151, 30, 121, 104)))
            using (var terrain = new SolidBrush(Color.FromArgb(91, 28, 37, 43)))
            using (var font = new Font("Segoe UI", Math.Max(7, pixels * 0.40f), GraphicsUnit.Pixel))
            using (var centered = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                for (var y = firstY; y <= lastY; y++)
                    for (var x = firstX; x <= lastX; x++)
                    {
                        var i = y * world.Width + x;
                        var value = world.Movement[i];
                        var source = ProjectData.FindEvent(world, x, y);
                        bool eventBlocks = source != null && (source.Type == MapEventType.Sign || source.Type == MapEventType.Npc);
                        var brush = eventBlocks || value == 2 ? blocked : value == 1 ? allowed :
                            (WorldData.IsWalkable(world.Ground[i], world.Objects[i]) ? null : terrain);
                        if (brush == null)
                            continue;
                        var rectangle = new Rectangle(x * pixels, y * pixels, pixels, pixels);
                        graphics.FillRectangle(brush, rectangle);
                        graphics.DrawString(eventBlocks ? "E" : value == 0 ? "×" : value.ToString(), font, Brushes.White, rectangle, centered);
                    }
            }
        }

        private void DrawEvents(Graphics graphics, int pixels)
        {
            using (var warp = new SolidBrush(Color.FromArgb(44, 98, 169)))
            using (var sign = new SolidBrush(Color.FromArgb(132, 91, 51)))
            using (var npc = new SolidBrush(Color.FromArgb(126, 67, 137)))
            using (var font = new Font("Segoe UI", Math.Max(8, pixels * 0.43f), FontStyle.Bold, GraphicsUnit.Pixel))
            using (var centered = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                foreach (var item in world.Events)
                {
                    if (view == EditorView.Connections && item.Type != MapEventType.Warp)
                        continue;
                    var bounds = new Rectangle(item.X * pixels + 2, item.Y * pixels + 2, pixels - 4, pixels - 4);
                    var brush = item.Type == MapEventType.Warp ? warp : item.Type == MapEventType.Sign ? sign : npc;
                    var label = item.Type == MapEventType.Warp ? "W" : item.Type == MapEventType.Sign ? "S" :
                        item.Type == MapEventType.Creature ? "C" : "N";
                    graphics.FillEllipse(brush, bounds);
                    graphics.DrawEllipse(Pens.White, bounds);
                    graphics.DrawString(label, font, Brushes.White, bounds, centered);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            var tile = TileAt(e.Location);
            if ((e.Button == MouseButtons.Left || e.Button == MouseButtons.Right) && ContainsTile(tile))
            {
                Capture = true;
                StrokeStarted(tile, e.Button);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var tile = TileAt(e.Location);
            var position = ContainsTile(tile) ? tile : new Point(-1, -1);
            if (position != hovered)
            {
                RedrawTile(hovered);
                hovered = position;
                RedrawTile(hovered);
                TileHovered(hovered);
            }

            if (Capture)
            {
                var x = Math.Max(0, Math.Min(world.Width - 1, tile.X));
                var y = Math.Max(0, Math.Min(world.Height - 1, tile.Y));
                StrokeContinued(new Point(x, y));
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (Capture)
                Capture = false;
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (!Capture)
                StrokeEnded();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            RedrawTile(hovered);
            hovered = new Point(-1, -1);
            TileHovered(hovered);
        }

        public void HighlightRegion(Rectangle bounds)
        {
            highlighted = bounds;
            Invalidate();
        }

        public void RedrawTile(Point tile)
        {
            if (ContainsTile(tile))
                Invalidate(new Rectangle(tile.X * TilePixels, tile.Y * TilePixels, TilePixels, TilePixels));
        }

        private Point TileAt(Point position)
        {
            return new Point((int)Math.Floor((double)position.X / TilePixels), (int)Math.Floor((double)position.Y / TilePixels));
        }

        private bool ContainsTile(Point tile)
        {
            return world != null && tile.X >= 0 && tile.Y >= 0 && tile.X < world.Width && tile.Y < world.Height;
        }
    }

    internal sealed class PlayerPreview : Control
    {
        public Bitmap Sprite { get; set; }
        public int Direction { get; set; }
        public int Frame { get; set; }

        public PlayerPreview()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            AccessibleName = "Player outfit preview";
            BackColor = Color.FromArgb(230, 238, 225);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Sprite == null)
                return;

            GameArt.UsePixelArt(e.Graphics);
            const int pixels = 3;
            var bounds = new Rectangle((Width - GameArt.PlayerWidth * pixels) / 2,
                (Height - GameArt.PlayerHeight * pixels) / 2, GameArt.PlayerWidth * pixels, GameArt.PlayerHeight * pixels);
            using (var shadow = new SolidBrush(Color.FromArgb(42, 49, 78, 59)))
                e.Graphics.FillEllipse(shadow, bounds.X - 3, bounds.Bottom - 8, bounds.Width + 6, 12);
            GameArt.DrawPlayer(e.Graphics, Sprite, Direction, Frame, bounds);
        }
    }
}
