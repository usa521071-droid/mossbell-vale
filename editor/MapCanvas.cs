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
        private int zoom = 2;
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

        public void Display(WorldData map, Bitmap sprite)
        {
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
            GameArt.DrawPlayer(e.Graphics, player, 0, 0,
                new Rectangle(spawn.X, spawn.Bottom - GameArt.PlayerHeight * zoom, pixels, GameArt.PlayerHeight * zoom));

            if (ContainsTile(hovered))
            {
                using (var highlight = new Pen(Color.White, 2))
                    e.Graphics.DrawRectangle(highlight, hovered.X * pixels + 1, hovered.Y * pixels + 1, pixels - 2, pixels - 2);
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
