using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MossbellVale.Editor
{
    internal sealed class PlaytestWindow : Form
    {
        private static readonly Color Ink = Color.FromArgb(25, 43, 40);
        private static readonly Color Cream = Color.FromArgb(248, 242, 215);
        private static readonly Color Leaf = Color.FromArgb(181, 219, 168);
        private static readonly Color PanelColor = Color.FromArgb(41, 66, 56);
        private readonly PlaytestSession session;
        private readonly PlaytestSurface screen;
        private readonly List<Keys> directions = new List<Keys>();
        private readonly Timer timer = new Timer { Interval = 17 };
        private readonly Label mapName = new Label { AutoSize = true, ForeColor = Cream, Margin = new Padding(0, 5, 12, 0) };
        private readonly Label counter = new Label { AutoSize = true, ForeColor = Leaf, Margin = new Padding(0, 8, 0, 0) };
        private bool useAction;
        private Keys actionDown;

        public PlaytestWindow(ProjectData project, int map, GameArt art)
        {
            session = new PlaytestSession(project, map);
            screen = new PlaytestSurface(session, art) { Dock = DockStyle.Fill, Margin = Padding.Empty };
            session.Message += screen.ShowMessage;
            Text = "Mossbell Vale · Playtest";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(860, 630);
            MinimumSize = new Size(530, 410);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 10f);
            BackColor = Ink;
            ForeColor = Cream;
            KeyPreview = true;

            var layout = new TableLayoutPanel { RowCount = 3, ColumnCount = 1, Dock = DockStyle.Fill, Padding = new Padding(10, 6, 10, 3) };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

            var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            mapName.Font = new Font(Font.FontFamily, 14f, FontStyle.Bold);
            header.Controls.Add(mapName, 0, 0);
            header.Controls.Add(counter, 1, 0);

            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var controls = new Label { Text = "Arrows / WASD   Move     Shift   Run     Z / Enter   Interact",
                AutoEllipsis = true, AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Cream, Margin = Padding.Empty };
            footer.Controls.Add(controls, 0, 0);
            var buttons = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
            var restart = CreateButton("Restart", delegate { Restart(); });
            var close = CreateButton("Exit", Close);
            buttons.Controls.Add(restart);
            buttons.Controls.Add(close);
            footer.Controls.Add(buttons, 1, 0);
            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(screen, 0, 1);
            layout.Controls.Add(footer, 0, 2);
            Controls.Add(layout);

            UpdateHeading();
            Shown += delegate { screen.Focus(); timer.Start(); };
            Deactivate += delegate { directions.Clear(); useAction = false; actionDown = Keys.None; };
            timer.Tick += delegate { UpdateSession(); };
        }

        private static Button CreateButton(string caption, Action action)
        {
            var button = new Button { Text = caption, BackColor = PanelColor, ForeColor = Cream,
                FlatStyle = FlatStyle.Flat, AutoSize = true, Height = 26, Margin = new Padding(4, 3, 0, 0) };
            button.FlatAppearance.BorderColor = Color.FromArgb(102, 140, 110);
            button.Click += delegate { action(); };
            return button;
        }

        private void Restart()
        {
            directions.Clear();
            useAction = false;
            actionDown = Keys.None;
            session.Restart();
            screen.RefreshOutfit();
            UpdateHeading();
            screen.Invalidate();
            screen.Focus();
        }

        private void UpdateSession()
        {
            var direction = directions.Count == 0 ? -1 : DirectionFor(directions[directions.Count - 1]);
            screen.AdvanceDialogue();
            session.Tick(direction, (ModifierKeys & Keys.Shift) != 0, useAction);
            useAction = false;
            UpdateHeading();
            screen.Invalidate();
        }

        private void UpdateHeading()
        {
            var heading = session.Map.Name;
            var progress = "Glowbells  " + session.Found + " / " + session.Total;
            if (session.Complete)
                progress += "  ·  Quest complete";
            if (mapName.Text != heading)
                mapName.Text = heading;
            if (counter.Text != progress)
                counter.Text = progress;
        }

        private static int DirectionFor(Keys key)
        {
            switch (key)
            {
                case Keys.Down: case Keys.S: return 0;
                case Keys.Left: case Keys.A: return 1;
                case Keys.Right: case Keys.D: return 2;
                case Keys.Up: case Keys.W: return 3;
                default: return -1;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keys)
        {
            var key = keys & Keys.KeyCode;
            if ((keys & (Keys.Alt | Keys.Control)) == 0 && DirectionFor(key) >= 0)
            {
                if (!directions.Contains(key))
                    directions.Add(key);
                return true;
            }
            switch (key)
            {
                case Keys.Escape: Close(); return true;
                case Keys.F5: Restart(); return true;
                case Keys.Z:
                case Keys.Enter:
                case Keys.Space:
                    if (actionDown != key)
                    {
                        useAction = true;
                        actionDown = key;
                    }
                    return true;
                default: return base.ProcessCmdKey(ref msg, keys);
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            directions.Remove(e.KeyCode);
            if (actionDown == e.KeyCode)
                actionDown = Keys.None;
            base.OnKeyUp(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                timer.Dispose();
            base.Dispose(disposing);
        }

        private sealed class PlaytestSurface : Control
        {
            private readonly PlaytestSession session;
            private readonly GameArt art;
            private Bitmap playerSprite;
            private string message = "";
            private int messageTime;

            public PlaytestSurface(PlaytestSession session, GameArt art)
            {
                this.session = session;
                this.art = art;
                RefreshOutfit();
                BackColor = Color.FromArgb(15, 28, 31);
                SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
                TabStop = true;
                AccessibleName = "Playable game viewport";
            }

            public void ShowMessage(string text)
            {
                message = text;
                messageTime = 150;
                AccessibleDescription = text;
                Invalidate();
            }

            public void AdvanceDialogue()
            {
                if (messageTime > 0 && --messageTime == 0)
                    AccessibleDescription = "";
            }

            public void RefreshOutfit()
            {
                if (playerSprite != null)
                    playerSprite.Dispose();
                playerSprite = art.RecolorPlayer(session.Outfit);
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                Focus();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                int scale = Math.Max(1, Math.Min(Width / 400, Height / 240));
                int width = Math.Min(Width, 400 * scale);
                int height = Math.Min(Height, 240 * scale);
                if (width < 1 || height < 1)
                    return;

                var view = new Rectangle((Width - width) / 2, (Height - height) / 2, width, height);
                var map = session.Map.World;
                int size = PlaytestSession.CellSize * scale;
                int x = Camera(session.PixelPosition.X * scale + size / 2, width, map.Width * size) - view.X;
                int y = Camera(session.PixelPosition.Y * scale + size / 2, height, map.Height * size) - view.Y;
                var camera = new Point(x, y);
                var first = new Point(Math.Max(0, (camera.X + view.Left) / size), Math.Max(0, (camera.Y + view.Top) / size));
                var last = new Point(Math.Min(map.Width - 1, (camera.X + view.Right - 1) / size),
                    Math.Min(map.Height - 1, (camera.Y + view.Bottom - 1) / size));

                e.Graphics.SetClip(view);
                GameArt.UsePixelArt(e.Graphics);
                for (int row = first.Y; row <= last.Y; row++)
                    for (int column = first.X; column <= last.X; column++)
                        art.DrawTile(e.Graphics, map.Ground[map.Width * row + column], TileBounds(column, row, size, camera));

                bool drawn = false;
                for (int row = first.Y; row <= last.Y; row++)
                {
                    if (!drawn && row * PlaytestSession.CellSize > session.PixelPosition.Y)
                    {
                        DrawPlayer(e.Graphics, camera, scale);
                        drawn = true;
                    }
                    for (int column = first.X; column <= last.X; column++)
                    {
                        int index = map.Width * row + column;
                        byte id = map.Objects[index];
                        if (id != 0 && (id != 31 || !session.IsCollected(index)))
                            art.DrawTile(e.Graphics, id, TileBounds(column, row, size, camera));
                    }

                    DrawEvents(e.Graphics, first.X, last.X, row, size, scale, camera);
                }
                if (!drawn)
                    DrawPlayer(e.Graphics, camera, scale);
                DrawDialogue(e.Graphics, view, scale);
                e.Graphics.ResetClip();
            }

            private void DrawEvents(Graphics graphics, int firstX, int lastX, int row, int size, int scale, Point camera)
            {
                var map = session.Map.World;
                foreach (var item in map.Events)
                {
                    if (item.Y != row || item.X < firstX || item.X > lastX || item.Type == MapEventType.Creature)
                        continue;
                    var bounds = TileBounds(item.X, item.Y, size, camera);
                    if (item.Type == MapEventType.Npc)
                        art.DrawNpc(graphics, bounds, (session.Map.Id + item.X + item.Y) % GameArt.NpcVariants, session.NpcFrame(item));
                    else if (item.Type == MapEventType.Sign && map.Objects[map.Width * row + item.X] == 0)
                        art.DrawTile(graphics, 30, bounds);
                }
                foreach (var creature in session.Creatures)
                {
                    var position = creature.PixelPosition;
                    if (position.Y / PlaytestSession.CellSize == row && position.X + PlaytestSession.CellSize >= firstX * PlaytestSession.CellSize &&
                        position.X < (lastX + 1) * PlaytestSession.CellSize)
                        art.DrawCreature(graphics, new Rectangle(position.X * scale - camera.X, position.Y * scale - camera.Y,
                            size, size), session.CreatureFrame);
                }
            }

            private void DrawPlayer(Graphics graphics, Point camera, int scale)
            {
                var player = session.PixelPosition;
                var bounds = new Rectangle(player.X * scale - camera.X, (player.Y + PlaytestSession.CellSize - GameArt.PlayerHeight) * scale - camera.Y,
                    GameArt.PlayerWidth * scale, GameArt.PlayerHeight * scale);
                GameArt.DrawPlayer(graphics, playerSprite, session.Direction, session.Frame, bounds);
            }

            private void DrawDialogue(Graphics graphics, Rectangle view, int scale)
            {
                if (message.Length == 0 || messageTime <= 0)
                    return;

                var box = new Rectangle(view.Left + 8 * scale, view.Bottom - 47 * scale, view.Width - 16 * scale, 39 * scale);
                using (var frame = new SolidBrush(Cream))
                using (var textColor = new SolidBrush(Color.FromArgb(250, 246, 221)))
                using (var background = new SolidBrush(Color.FromArgb(224, 27, 47, 44)))
                using (var font = new Font("Segoe UI", 10 * scale, GraphicsUnit.Pixel))
                {
                    graphics.FillRectangle(frame, box);
                    box.Inflate(-2 * scale, -2 * scale);
                    graphics.FillRectangle(background, box);
                    box.Inflate(-7 * scale, -4 * scale);
                    graphics.DrawString(message, font, textColor, box);
                }
            }

            private static Rectangle TileBounds(int x, int y, int size, Point camera)
            {
                return new Rectangle(x * size - camera.X, y * size - camera.Y, size, size);
            }

            private static int Camera(int player, int view, int map)
            {
                return map <= view ? (map - view) / 2 : Math.Max(0, Math.Min(map - view, player - view / 2));
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    playerSprite.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
