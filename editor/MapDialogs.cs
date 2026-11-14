using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MossbellVale.Editor
{
    internal sealed class NewMapDialog : Form
    {
        private readonly TextBox mapName = new TextBox { MaxLength = 31, Width = 210, Text = "New Map" };
        private readonly TextBox mapGroup = new TextBox { MaxLength = 23, Width = 210 };
        private readonly NumericUpDown width = SizeField(32);
        private readonly NumericUpDown height = SizeField(24);
        private readonly ComboBox preset = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 210 };

        public string MapName { get { return mapName.Text.Trim(); } }
        public string MapGroup { get { return mapGroup.Text.Trim(); } }
        public int MapWidth { get { return (int)width.Value; } }
        public int MapHeight { get { return (int)height.Value; } }
        public int Preset { get { return preset.SelectedIndex; } }

        public NewMapDialog(string group)
        {
            Text = "Add map";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 10f);
            ClientSize = new Size(410, 252);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            mapGroup.Text = group;
            preset.Items.AddRange(new object[] { "Blank meadow", "Town", "Dungeon", "Mountain" });
            preset.SelectedIndex = 0;
            preset.SelectedIndexChanged += delegate { width.Enabled = height.Enabled = preset.SelectedIndex == 0; };

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6, Padding = new Padding(15) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            AddLine(layout, 0, "Name", mapName);
            AddLine(layout, 1, "Group", mapGroup);
            AddLine(layout, 2, "Starting layout", preset);
            AddLine(layout, 3, "Width", width);
            AddLine(layout, 4, "Height", height);
            AddButtons(this, layout, 5, "Add map", delegate { DialogResult = DialogResult.OK; });
            Controls.Add(layout);
        }

        private static NumericUpDown SizeField(int value)
        {
            return new NumericUpDown { Minimum = WorldData.MinimumSize, Maximum = WorldData.MaximumSize, Value = value };
        }

        internal static void AddLine(TableLayoutPanel layout, int row, string caption, Control field)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 37));
            layout.Controls.Add(new Label { Text = caption, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            field.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            if (String.IsNullOrEmpty(field.AccessibleName))
                field.AccessibleName = caption;
            layout.Controls.Add(field, 1, row);
        }

        internal static void AddButtons(Form window, TableLayoutPanel layout, int row, string caption, Action saveAction)
        {
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
            var save = new Button { Text = caption, AutoSize = true };
            save.Click += delegate { saveAction(); };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(save);
            layout.Controls.Add(buttons, 0, row);
            layout.SetColumnSpan(buttons, 2);
            window.AcceptButton = save;
            window.CancelButton = cancel;
        }
    }

    internal sealed class MapEventDialog : Form
    {
        private readonly ComboBox type = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox destination = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly NumericUpDown targetX = new NumericUpDown();
        private readonly NumericUpDown targetY = new NumericUpDown();
        private readonly TextBox message = new TextBox { MaxLength = 90 };
        private readonly ProjectMap source;
        private readonly Point tile;
        private readonly GameArt art;
        private readonly Button chooseTile = new Button { Text = "Choose on map", AutoSize = true };

        public MapEvent Result { get; private set; }

        public MapEventDialog(ProjectData project, ProjectMap source, Point tile, MapEvent existing, bool connectionsOnly, GameArt art)
        {
            this.source = source;
            this.tile = tile;
            this.art = art;
            Text = (connectionsOnly ? "Connection" : "Map event") + " at " + tile.X + ", " + tile.Y;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 10f);
            ClientSize = new Size(510, 307);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            type.Items.AddRange(new object[] { "Warp (step onto tile)", "Sign (press A)", "NPC (press A)" });
            type.SelectedIndex = connectionsOnly ? 0 : existing == null ? 1 : (int)existing.Type - 1;
            type.Enabled = !connectionsOnly;
            foreach (var map in project.Maps)
                destination.Items.Add(map);
            destination.SelectedItem = existing == null ? source : project.Find(existing.TargetMap) ?? source;
            destination.SelectedIndexChanged += delegate { SetDestinationBounds(); };
            SetDestinationBounds();
            if (existing != null)
            {
                targetX.Value = Math.Min(targetX.Maximum, existing.TargetX);
                targetY.Value = Math.Min(targetY.Maximum, existing.TargetY);
                message.Text = existing.Text;
            }
            type.SelectedIndexChanged += delegate { UpdateFields(); };
            chooseTile.Click += delegate { PickTarget(); };
            UpdateFields();

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7, Padding = new Padding(15) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            NewMapDialog.AddLine(layout, 0, "Event type", type);
            NewMapDialog.AddLine(layout, 1, "Destination map", destination);
            NewMapDialog.AddLine(layout, 2, "Destination X", targetX);
            NewMapDialog.AddLine(layout, 3, "Destination Y", targetY);
            NewMapDialog.AddLine(layout, 4, "Destination tile", chooseTile);
            NewMapDialog.AddLine(layout, 5, "Message", message);
            NewMapDialog.AddButtons(this, layout, 6, "Save event", SaveEvent);
            Controls.Add(layout);
        }

        private void SetDestinationBounds()
        {
            var map = (ProjectMap)destination.SelectedItem;
            targetX.Maximum = map.World.Width - 1;
            targetY.Maximum = map.World.Height - 1;
            targetX.Value = map.World.SpawnX;
            targetY.Value = map.World.SpawnY;
        }

        private void UpdateFields()
        {
            destination.Enabled = targetX.Enabled = targetY.Enabled = chooseTile.Enabled = type.SelectedIndex == 0;
            message.Enabled = type.SelectedIndex != 0;
        }

        private void PickTarget()
        {
            var map = (ProjectMap)destination.SelectedItem;
            using (var selection = new CoordinateDialog(art, map, new Point((int)targetX.Value, (int)targetY.Value)))
                if (selection.ShowDialog(this) == DialogResult.OK)
                {
                    targetX.Value = selection.Coordinate.X;
                    targetY.Value = selection.Coordinate.Y;
                }
        }

        private void SaveEvent()
        {
            try
            {
                var kind = (MapEventType)(type.SelectedIndex + 1);
                if (kind == MapEventType.Warp)
                {
                    var target = (ProjectMap)destination.SelectedItem;
                    var x = (int)targetX.Value;
                    var y = (int)targetY.Value;
                    if (!source.World.CanWalk(tile.X, tile.Y))
                        throw new InvalidDataException("Choose a walkable source tile.");
                    if (!target.World.CanWalk(x, y) || ProjectData.FindEvent(target.World, x, y) != null ||
                        (target.Id == source.Id && x == tile.X && y == tile.Y))
                        throw new InvalidDataException("Choose a walkable destination tile without an event.");
                    Result = new MapEvent(kind, tile.X, tile.Y, "", target.Id, x, y);
                }
                else
                {
                    var text = message.Text.Trim();
                    ProjectData.ValidateText(text, 1, 90, "Message");
                    Result = new MapEvent(kind, tile.X, tile.Y, text);
                }
                DialogResult = DialogResult.OK;
            }
            catch (InvalidDataException error)
            {
                MessageBox.Show(this, error.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
    internal sealed class CoordinateDialog : Form
    {
        public Point Coordinate { get; private set; }

        public CoordinateDialog(GameArt art, ProjectMap map, Point target)
        {
            Text = "Choose a destination · " + map.Name;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(640, 493);
            MinimumSize = new Size(420, 340);
            var viewport = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(22, 44, 37) };
            var canvas = new MapCanvas(art) { Zoom = 1 };
            var instructions = new Label { Text = "Click a walkable tile without an event.", Dock = DockStyle.Bottom, Height = 31,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0) };
            canvas.Display(map.World, null);
            canvas.SetView(EditorView.Events);
            canvas.HighlightRegion(new Rectangle(target.X, target.Y, 1, 1));
            canvas.StrokeStarted += delegate(Point point, MouseButtons button)
            {
                if (button != MouseButtons.Left)
                    return;
                if (!map.World.CanWalk(point.X, point.Y) || ProjectData.FindEvent(map.World, point.X, point.Y) != null)
                {
                    instructions.Text = "This tile is blocked. Choose another destination.";
                    return;
                }
                Coordinate = point;
                DialogResult = DialogResult.OK;
            };
            canvas.StrokeContinued += delegate { };
            canvas.StrokeEnded += delegate { };
            canvas.TileHovered += delegate { };
            viewport.Controls.Add(canvas);
            Controls.Add(viewport);
            Controls.Add(instructions);
            Shown += delegate
            {
                viewport.AutoScrollPosition = new Point(Math.Max(0, target.X * GameArt.TileSize - viewport.Width / 2),
                    Math.Max(0, target.Y * GameArt.TileSize - viewport.Height / 2));
            };
        }
    }
}