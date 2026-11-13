using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MossbellVale.Editor
{
    internal sealed class EditorWindow : Form
    {
        private static readonly Color Forest = Color.FromArgb(22, 44, 37);
        private static readonly Color Green = Color.FromArgb(43, 78, 60);
        private static readonly Color Paper = Color.FromArgb(246, 249, 243);
        private static readonly Color Ink = Color.FromArgb(27, 48, 39);
        private static readonly Color Muted = Color.FromArgb(78, 102, 90);
        private const string WorldFilter = "Mossbell Vale world (*.rpgw)|*.rpgw";
        private const int HistoryLimit = 64;

        private readonly GameArt art = new GameArt();
        private readonly ToolTip tips = new ToolTip();
        private readonly Timer previewTimer = new Timer { Interval = 150 };
        private readonly List<WorldData> undo = new List<WorldData>();
        private readonly List<WorldData> redo = new List<WorldData>();
        private readonly RadioButton[] tileButtons = new RadioButton[16];
        private readonly ComboBox[] outfitChoices = new ComboBox[4];
        private readonly Panel mapViewport = new Panel { AutoScroll = true, Dock = DockStyle.Fill, BackColor = Forest };
        private readonly Label brushLabel = new Label { AutoSize = true, ForeColor = Green };
        private readonly Label positionLabel = new Label { AutoSize = true, Anchor = AnchorStyles.Right, ForeColor = Color.White };
        private readonly Label statusLabel = new Label { AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.White };
        private readonly RadioButton groundTab = CreateToggle("Ground", 98);
        private readonly RadioButton objectTab = CreateToggle("Objects", 98);
        private readonly RadioButton spawnTool = CreateToggle("Set player start", 200);
        private readonly RadioButton eraseTool = CreateToggle("Erase object", 200);
        private readonly PlayerPreview preview = new PlayerPreview();
        private readonly MapCanvas map;
        private Button undoButton;
        private Button redoButton;
        private Button saveButton;

        private WorldData world;
        private WorldData saved;
        private WorldData strokeStart;
        private string filename;
        private Bitmap recolored;
        private bool loading;
        private bool objects;
        private int groundBrush;
        private int objectBrush = 16;
        private Point previousTile;
        private MouseButtons strokeButton;
        private int previewStep;

        public EditorWindow(string initialPath)
        {
            Text = "Mossbell Vale Editor";
            MinimumSize = new Size(975, 710);
            Size = new Size(1270, 830);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 10f);
            ForeColor = Ink;
            BackColor = Paper;
            KeyPreview = true;

            map = new MapCanvas(art);
            map.StrokeStarted += StartStroke;
            map.StrokeContinued += ContinueStroke;
            map.StrokeEnded += EndStroke;
            map.TileHovered += UpdatePosition;
            mapViewport.Controls.Add(map);

            var layout = new TableLayoutPanel { ColumnCount = 3, RowCount = 3, Dock = DockStyle.Fill };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 234));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 224));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 102));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

            var header = BuildHeader();
            var footer = BuildFooter();
            layout.Controls.Add(header, 0, 0);
            layout.SetColumnSpan(header, 3);
            layout.Controls.Add(BuildTilePanel(), 0, 1);
            layout.Controls.Add(mapViewport, 1, 1);
            layout.Controls.Add(BuildCharacterPanel(), 2, 1);
            layout.Controls.Add(footer, 0, 2);
            layout.SetColumnSpan(footer, 3);
            Controls.Add(layout);

            previewTimer.Tick += delegate
            {
                previewStep = (previewStep + 1) % 4;
                preview.Frame = previewStep == 3 ? 0 : previewStep;
                preview.Invalidate();
            };
            previewTimer.Start();
            FormClosing += delegate(object sender, FormClosingEventArgs args) { args.Cancel = !ConfirmChanges(); };
            LoadWorld(GameArt.ReadDemo(), null, "Demo world opened");
            Shown += delegate { CenterOnSpawn(); };
            if (!String.IsNullOrWhiteSpace(initialPath))
                OpenWorld(initialPath);
        }

        private Control BuildHeader()
        {
            var header = new TableLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = new Padding(16, 8, 16, 4),
                BackColor = Forest, ColumnCount = 1, RowCount = 2 };
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            header.Controls.Add(new Label {
                Text = "Mossbell Vale  /  World Editor",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font(Font.FontFamily, 16f, FontStyle.Bold),
                Anchor = AnchorStyles.Left
            });

            var commands = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            AddCommand(commands, "New", "Ctrl+N", NewWorld);
            AddCommand(commands, "Open", "Ctrl+O", ChooseWorld);
            saveButton = AddCommand(commands, "Save", "Ctrl+S", delegate { SaveWorld(false); });
            AddCommand(commands, "Save As", "Ctrl+Shift+S", delegate { SaveWorld(true); });
            AddCommand(commands, "Export to 3DS SD", "Choose the root of your 3DS SD card.", ExportWorld);
            undoButton = AddCommand(commands, "Undo", "Ctrl+Z", delegate { Restore(undo, redo); });
            redoButton = AddCommand(commands, "Redo", "Ctrl+Y", delegate { Restore(redo, undo); });
            var zoom = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 75, Margin = new Padding(8, 5, 4, 0),
                AccessibleName = "Map zoom" };
            zoom.Items.AddRange(new object[] { "100%", "200%", "300%", "400%" });
            zoom.SelectedIndex = 1;
            zoom.SelectedIndexChanged += delegate { ChangeZoom(zoom.SelectedIndex + 1); };
            commands.Controls.Add(zoom);
            var grid = new CheckBox { Text = "Grid", ForeColor = Color.White, AutoSize = true, Checked = true,
                Margin = new Padding(8, 8, 0, 0) };
            grid.CheckedChanged += delegate { map.ShowGrid = grid.Checked; map.Invalidate(); };
            commands.Controls.Add(grid);
            header.Controls.Add(commands);
            return header;
        }

        private Control BuildTilePanel()
        {
            var panel = CreateSidePanel();
            panel.Controls.Add(CreateHeading("Tiles"));
            var layers = new FlowLayoutPanel { Width = 202, Height = 40, WrapContents = false, Margin = new Padding(0, 4, 0, 4) };
            layers.Controls.Add(groundTab);
            layers.Controls.Add(objectTab);
            groundTab.Checked = true;
            groundTab.CheckedChanged += delegate { if (groundTab.Checked) ChangeLayer(false); };
            objectTab.CheckedChanged += delegate { if (objectTab.Checked) ChangeLayer(true); };
            panel.Controls.Add(layers);

            var tilePalette = new FlowLayoutPanel { Width = 204, Height = 214, Margin = new Padding(0, 0, 0, 4) };
            for (var index = 0; index < tileButtons.Length; index++)
            {
                var paletteIndex = index;
                var tile = CreateToggle("", 46);
                tile.Height = 46;
                tile.Margin = new Padding(2);
                tile.Paint += delegate(object sender, PaintEventArgs args)
                {
                    GameArt.UsePixelArt(args.Graphics);
                    var bounds = new Rectangle(7, 7, 32, 32);
                    if (objects)
                        art.DrawTile(args.Graphics, 0, bounds);
                    art.DrawTile(args.Graphics, paletteIndex + (objects ? 16 : 0), bounds);
                };
                tile.Click += delegate { SelectBrush(paletteIndex); };
                tileButtons[index] = tile;
                tilePalette.Controls.Add(tile);
            }
            panel.Controls.Add(tilePalette);

            brushLabel.Margin = new Padding(3, 0, 0, 12);
            panel.Controls.Add(brushLabel);
            var tools = new FlowLayoutPanel { Width = 204, Height = 80, WrapContents = true, Margin = Padding.Empty };
            tools.Controls.Add(spawnTool);
            tools.Controls.Add(eraseTool);
            spawnTool.Click += delegate { UseTool("Player start"); };
            eraseTool.Click += delegate { UseTool("Eraser"); };
            panel.Controls.Add(tools);
            panel.Controls.Add(new Label {
                Text = "Click or drag to paint.\r\nRight-click to erase objects.",
                AutoSize = true, ForeColor = Muted, Margin = new Padding(2, 9, 0, 0)
            });
            RefreshBrushes();
            return panel;
        }

        private Control BuildCharacterPanel()
        {
            var panel = CreateSidePanel();
            panel.Controls.Add(CreateHeading("Character"));
            preview.Size = new Size(190, 110);
            preview.Margin = new Padding(0, 8, 0, 8);
            panel.Controls.Add(preview);

            var facing = new ComboBox { Width = 190, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 0, 0, 14),
                AccessibleName = "Character preview direction" };
            facing.Items.AddRange(new object[] { "Face down", "Face left", "Face right", "Face up" });
            facing.SelectedIndex = 0;
            facing.SelectedIndexChanged += delegate { preview.Direction = facing.SelectedIndex; preview.Invalidate(); };
            panel.Controls.Add(facing);

            var parts = new[] { "Shirt", "Pants", "Hair", "Skin" };
            for (var index = 0; index < parts.Length; index++)
            {
                var part = index;
                panel.Controls.Add(new Label { Text = parts[part], AutoSize = true, Margin = new Padding(0, 2, 0, 4) });
                var choice = new ComboBox { Width = 190, DropDownStyle = ComboBoxStyle.DropDownList,
                    Margin = new Padding(0, 0, 0, 12), AccessibleName = parts[part] };
                choice.Items.AddRange(GameArt.PaletteNames[part]);
                choice.SelectedIndexChanged += delegate { ChangeOutfit(part); };
                outfitChoices[index] = choice;
                panel.Controls.Add(choice);
            }
            return panel;
        }

        private Control BuildFooter()
        {
            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Forest, Margin = Padding.Empty,
                Padding = new Padding(12, 0, 12, 0), ColumnCount = 2, RowCount = 1 };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footer.Controls.Add(statusLabel, 0, 0);
            footer.Controls.Add(positionLabel, 1, 0);
            return footer;
        }

        private static FlowLayoutPanel CreateSidePanel()
        {
            return new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = true, Margin = Padding.Empty, Padding = new Padding(15, 13, 8, 8) };
        }

        private Label CreateHeading(string text)
        {
            return new Label { Text = text, Font = new Font(Font.FontFamily, 13f, FontStyle.Bold), AutoSize = true,
                Margin = Padding.Empty };
        }

        private static RadioButton CreateToggle(string text, int width)
        {
            var toggle = new RadioButton {
                Text = text, Appearance = Appearance.Button, AutoSize = false, Width = width, Height = 31,
                TextAlign = ContentAlignment.MiddleCenter, FlatStyle = FlatStyle.Flat,
                BackColor = Paper, ForeColor = Ink, Margin = new Padding(2)
            };
            toggle.FlatAppearance.BorderColor = Color.FromArgb(162, 185, 166);
            toggle.FlatAppearance.CheckedBackColor = Color.FromArgb(168, 208, 176);
            toggle.FlatAppearance.MouseOverBackColor = Color.FromArgb(221, 235, 219);
            return toggle;
        }

        private Button AddCommand(FlowLayoutPanel commands, string label, string hint, Action action)
        {
            var button = new Button {
                Text = label, AutoSize = true, MinimumSize = new Size(58, 32), Padding = new Padding(7, 0, 7, 0),
                FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Green, Margin = new Padding(0, 3, 7, 0)
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(99, 140, 117);
            button.Click += delegate { action(); };
            tips.SetToolTip(button, hint);
            commands.Controls.Add(button);
            return button;
        }

        private void ChangeLayer(bool useObjects)
        {
            objects = useObjects;
            spawnTool.Checked = false;
            eraseTool.Checked = false;
            RefreshBrushes();
        }

        private void SelectBrush(int paletteIndex)
        {
            if (objects)
                objectBrush = paletteIndex + 16;
            else
                groundBrush = paletteIndex;
            spawnTool.Checked = false;
            eraseTool.Checked = false;
            RefreshBrushes();
        }

        private void UseTool(string name)
        {
            foreach (var tile in tileButtons)
                tile.Checked = false;
            brushLabel.Text = name;
        }

        private void RefreshBrushes()
        {
            for (var index = 0; index < tileButtons.Length; index++)
            {
                var id = index + (objects ? 16 : 0);
                var tile = tileButtons[index];
                tile.AccessibleName = GameArt.TileNames[id];
                tips.SetToolTip(tile, GameArt.TileNames[id]);
                tile.Checked = id == (objects ? objectBrush : groundBrush);
                tile.Invalidate();
            }
            brushLabel.Text = GameArt.TileNames[objects ? objectBrush : groundBrush];
        }

        private void ChangeZoom(int zoom)
        {
            var offset = mapViewport.AutoScrollPosition;
            var centerX = (-offset.X + mapViewport.ClientSize.Width / 2) * zoom / map.Zoom;
            var centerY = (-offset.Y + mapViewport.ClientSize.Height / 2) * zoom / map.Zoom;
            map.Zoom = zoom;
            mapViewport.AutoScrollPosition = new Point(Math.Max(0, centerX - mapViewport.ClientSize.Width / 2),
                Math.Max(0, centerY - mapViewport.ClientSize.Height / 2));
        }

        private void StartStroke(Point tile, MouseButtons button)
        {
            EndStroke();
            strokeStart = world.Copy();
            previousTile = tile;
            strokeButton = button;
            PaintTile(tile);
        }

        private void ContinueStroke(Point tile)
        {
            if (strokeStart == null || previousTile == tile || (spawnTool.Checked && strokeButton == MouseButtons.Left))
                return;

            var x = previousTile.X;
            var y = previousTile.Y;
            var dx = Math.Abs(tile.X - x);
            var dy = Math.Abs(tile.Y - y);
            var stepX = x < tile.X ? 1 : -1;
            var stepY = y < tile.Y ? 1 : -1;
            var error = dx - dy;

            while (x != tile.X || y != tile.Y)
            {
                var doubled = error * 2;
                if (doubled > -dy)
                {
                    error -= dy;
                    x += stepX;
                }
                if (doubled < dx)
                {
                    error += dx;
                    y += stepY;
                }
                PaintTile(new Point(x, y));
            }
            previousTile = tile;
        }

        private void EndStroke()
        {
            if (strokeStart == null)
                return;
            if (!world.Matches(strokeStart))
                Remember(strokeStart);
            strokeStart = null;
        }

        private void PaintTile(Point tile)
        {
            var index = tile.Y * world.Width + tile.X;
            var ground = world.Ground[index];
            var item = world.Objects[index];
            if (strokeButton == MouseButtons.Right || eraseTool.Checked)
                item = 0;
            else if (spawnTool.Checked)
            {
                if (!WorldData.IsWalkable(ground, item))
                {
                    SetStatus("Choose a walkable tile for the player start.");
                    return;
                }
                world.SpawnX = tile.X;
                world.SpawnY = tile.Y;
                SetStatus("Player start set");
                map.Invalidate();
                return;
            }
            else if (objects)
                item = (byte)objectBrush;
            else
                ground = (byte)groundBrush;

            if (tile.X == world.SpawnX && tile.Y == world.SpawnY && !WorldData.IsWalkable(ground, item))
            {
                SetStatus("Move the player start before placing a blocked tile here.");
                return;
            }
            world.Ground[index] = ground;
            world.Objects[index] = item;
            map.RedrawTile(tile);
        }

        private void UpdatePosition(Point tile)
        {
            positionLabel.Text = tile.X < 0 ? world.Width + " × " + world.Height + " tiles" :
                "X " + tile.X + "    Y " + tile.Y + "     ·     " + world.Width + " × " + world.Height + " tiles";
        }

        private void ChangeOutfit(int part)
        {
            if (loading || outfitChoices[part].SelectedIndex < 0)
                return;

            var previous = world.Copy();
            world.Outfit[part] = (byte)outfitChoices[part].SelectedIndex;
            RefreshPlayer();
            Remember(previous);
            SetStatus("Character outfit updated");
        }

        private void RefreshPlayer()
        {
            var old = recolored;
            recolored = art.RecolorPlayer(world.Outfit);
            preview.Sprite = recolored;
            preview.Invalidate();
            map.Display(world, recolored);
            if (old != null)
                old.Dispose();
        }

        private void ApplyWorld(WorldData data)
        {
            world = data;
            loading = true;
            for (var index = 0; index < outfitChoices.Length; index++)
                outfitChoices[index].SelectedIndex = world.Outfit[index];
            loading = false;
            RefreshPlayer();
            UpdatePosition(new Point(-1, -1));
        }

        private void LoadWorld(WorldData data, string path, string message)
        {
            ApplyWorld(data);
            filename = path;
            saved = world.Copy();
            undo.Clear();
            redo.Clear();
            CenterOnSpawn();
            UpdateCommands();
            SetStatus(message);
        }

        private void CenterOnSpawn()
        {
            var pixels = map.Zoom * GameArt.TileSize;
            mapViewport.AutoScrollPosition = new Point(
                Math.Max(0, world.SpawnX * pixels + pixels / 2 - mapViewport.ClientSize.Width / 2),
                Math.Max(0, world.SpawnY * pixels + pixels / 2 - mapViewport.ClientSize.Height / 2));
        }

        private void Remember(WorldData previous)
        {
            PushHistory(undo, previous);
            redo.Clear();
            UpdateCommands();
        }

        private static void PushHistory(List<WorldData> history, WorldData entry)
        {
            if (history.Count == HistoryLimit)
                history.RemoveAt(0);
            history.Add(entry);
        }

        private void Restore(List<WorldData> source, List<WorldData> destination)
        {
            EndStroke();
            if (source.Count == 0)
                return;

            PushHistory(destination, world);
            ApplyWorld(source[source.Count - 1]);
            source.RemoveAt(source.Count - 1);
            UpdateCommands();
            SetStatus(source == undo ? "Edit undone" : "Edit restored");
        }

        private void UpdateCommands()
        {
            var dirty = !world.Matches(saved);
            var name = filename == null ? "Untitled World" : Path.GetFileNameWithoutExtension(filename);
            Text = (dirty ? "* " : "") + name + " – Mossbell Vale Editor";
            saveButton.Enabled = dirty || filename == null;
            undoButton.Enabled = undo.Count > 0;
            redoButton.Enabled = redo.Count > 0;
        }

        private void SetStatus(string message)
        {
            statusLabel.Text = message;
        }

        private void NewWorld()
        {
            using (var dialog = new WorldSizeDialog())
                if (dialog.ShowDialog(this) == DialogResult.OK && ConfirmChanges())
                    LoadWorld(new WorldData(dialog.WorldWidth, dialog.WorldHeight), null, "New world created");
        }

        private void ChooseWorld()
        {
            using (var dialog = new OpenFileDialog { Filter = WorldFilter, CheckFileExists = true })
                if (dialog.ShowDialog(this) == DialogResult.OK && ConfirmChanges())
                    OpenWorld(dialog.FileName);
        }

        private void OpenWorld(string path)
        {
            TryFileAction("Open world", delegate
            {
                var fullPath = Path.GetFullPath(path);
                var data = WorldData.Read(File.OpenRead(fullPath));
                LoadWorld(data, fullPath, "Opened " + Path.GetFileName(fullPath));
            });
        }

        private bool SaveWorld(bool saveAs)
        {
            EndStroke();
            var target = filename;
            if (saveAs || target == null)
            {
                using (var dialog = new SaveFileDialog { Filter = WorldFilter, DefaultExt = "rpgw", AddExtension = true,
                    FileName = target == null ? "world.rpgw" : Path.GetFileName(target), OverwritePrompt = true })
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                        return false;
                    target = dialog.FileName;
                }
            }

            return TryFileAction("Save world", delegate
            {
                world.Save(target);
                filename = target;
                saved = world.Copy();
                UpdateCommands();
                SetStatus("Saved " + Path.GetFileName(target));
            });
        }

        private void ExportWorld()
        {
            EndStroke();
            using (var dialog = new FolderBrowserDialog { Description = "Select the root folder of your 3DS SD card.",
                ShowNewFolderButton = false })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                TryFileAction("Export to 3DS SD", delegate
                {
                    var target = ExportWorldTo(dialog.SelectedPath, delegate
                    {
                        return MessageBox.Show(this, "Replace the edited world on this SD card?",
                            "Export to 3DS SD", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
                    });
                    if (target == null)
                        return;

                    SetStatus("Exported to " + target);
                    MessageBox.Show(this, "Exported to:\r\n" + target + "\r\n\r\nLaunch or restart Mossbell Vale to play this world.",
                        "Export to 3DS SD", MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
            }
        }

        private string ExportWorldTo(string root, Func<bool> confirmReplace)
        {
            var directory = Path.Combine(root, "3ds", "MossbellVale");
            var target = Path.Combine(directory, "world.rpgw");
            if (File.Exists(target) && !confirmReplace())
                return null;

            Directory.CreateDirectory(directory);
            world.Save(target);
            return target;
        }

        private bool ConfirmChanges()
        {
            EndStroke();
            if (world.Matches(saved))
                return true;

            var answer = MessageBox.Show(this, "Save your changes to this world?", "Mossbell Vale Editor",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            return answer == DialogResult.No || (answer == DialogResult.Yes && SaveWorld(false));
        }

        private bool TryFileAction(string caption, Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception error)
            {
                if (!(error is IOException || error is InvalidDataException || error is UnauthorizedAccessException || error is ArgumentException))
                    throw;
                MessageBox.Show(this, error.Message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        protected override bool ProcessCmdKey(ref Message message, Keys keys)
        {
            switch (keys)
            {
                case Keys.Control | Keys.N: NewWorld(); return true;
                case Keys.Control | Keys.O: ChooseWorld(); return true;
                case Keys.Control | Keys.S: SaveWorld(false); return true;
                case Keys.Control | Keys.Shift | Keys.S: SaveWorld(true); return true;
                case Keys.Control | Keys.Z: Restore(undo, redo); return true;
                case Keys.Control | Keys.Y: Restore(redo, undo); return true;
                default: return base.ProcessCmdKey(ref message, keys);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                previewTimer.Dispose();
                tips.Dispose();
                if (recolored != null)
                    recolored.Dispose();
                art.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
