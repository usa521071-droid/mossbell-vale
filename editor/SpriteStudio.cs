using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Globalization;
using System.Windows.Forms;

namespace MossbellVale.Editor
{
    internal sealed class SpriteStudio : Form
    {
        private static readonly Color Forest = Color.FromArgb(22, 44, 37);
        private static readonly Color Paper = Color.FromArgb(246, 249, 243);
        private static readonly Color Ink = Color.FromArgb(27, 48, 39);
        private static readonly string[] Directions = { "Down", "Left", "Right", "Up" };
        private static readonly string[] Frames = { "Idle", "Step 1", "Step 2" };
        private static readonly string[] PartNames = { "Shirt", "Pants", "Hair", "Skin" };
        private static readonly int[] ColorSwatches = {
            0x202433, 0xF6F2D8, 0xE75977, 0x4A4252, 0x7B7384, 0xB5B5BD,
            0x294A42, 0x427352, 0x6B9C5A, 0x8CB563, 0xBDCE73, 0xDEDE8C,
            0x634239, 0x94634A, 0xB57B52, 0xCE9C63, 0xE7BD7B, 0xF7DE9C,
            0x394267, 0x526384, 0x638CB5, 0x8CB5CE, 0xADCED6, 0xDEF7DE,
            0x634252, 0x944A5A, 0xBD636B, 0xDE8C94, 0xE7ADB5, 0xFFE7DE
        };

        private sealed class SpriteSlot
        {
            public readonly string Name;
            public readonly int Index;
            public SpriteSlot(string name, int index) { Name = name; Index = index; }
            public override string ToString() { return Name; }
        }

        private sealed class SpriteEdit : IDisposable
        {
            public readonly int Category;
            public readonly int Index;
            public readonly Bitmap Pixels;
            public readonly byte[] Bundle;

            public SpriteEdit(int category, int index, Bitmap pixels)
            {
                Category = category;
                Index = index;
                Pixels = pixels;
            }

            public SpriteEdit(int category, int index, byte[] bundle)
            {
                Category = category;
                Index = index;
                Bundle = bundle;
            }

            public void Dispose()
            {
                if (Pixels != null)
                    Pixels.Dispose();
            }
        }

        private readonly GameArt art;
        private readonly SpriteCanvas canvas = new SpriteCanvas { Dock = DockStyle.Fill, Margin = Padding.Empty };
        private readonly ComboBox category = CreatePicker("Sprite category");
        private readonly ComboBox slot = CreatePicker("Sprite slot");
        private readonly ComboBox zoom = CreatePicker("Pixel zoom");
        private readonly Label status = new Label { AutoSize = true, ForeColor = Ink, Margin = new Padding(12, 6, 0, 0) };
        private readonly Panel preview = new Panel { Width = 192, Height = 110, BackColor = Color.FromArgb(226, 237, 226) };
        private readonly Button swatch = new Button { Width = 190, Height = 30, Text = "Custom color…" };
        private readonly List<SpriteEdit> undo = new List<SpriteEdit>();
        private readonly List<SpriteEdit> redo = new List<SpriteEdit>();
        private readonly Timer timer = new Timer { Interval = 155 };
        private Button undoButton;
        private Button redoButton;
        private RadioButton pencilButton;
        private readonly Action artEdited;
        private int spriteCategory;
        private bool IsGround { get { return spriteCategory == 0; } }
        private int index;
        private int frame;
        private bool adjusting;

        public SpriteStudio(GameArt art, Action artEdited, int initialTile)
        {
            this.art = art;
            this.artEdited = artEdited;
            Text = "Mossbell Vale · Sprite Studio";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(875, 675);
            Size = new Size(1160, 820);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 10f);
            BackColor = Paper;
            ForeColor = Ink;
            KeyPreview = true;
            canvas.BrushColor = Color.FromArgb(32, 36, 51);
            canvas.Edited += CommitCanvas;
            canvas.ColorPicked += delegate(Color color)
            {
                SetColor(color);
                pencilButton.Checked = true;
            };
            canvas.Positioned += delegate(Point point) { UpdateStatus(point); };

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, ColumnCount = 3, RowCount = 3 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 227));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 227));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 33));
            var header = BuildToolbar();
            layout.Controls.Add(header, 0, 0);
            layout.SetColumnSpan(header, 3);
            layout.Controls.Add(BuildSpriteOptions(), 0, 1);
            layout.Controls.Add(canvas, 1, 1);
            layout.Controls.Add(BuildPalette(), 2, 1);
            layout.Controls.Add(status, 0, 2);
            layout.SetColumnSpan(status, 3);
            Controls.Add(layout);

            preview.Paint += PaintPreview;
            timer.Tick += delegate { frame = (frame + 1) % 4; if (spriteCategory == 2 || spriteCategory == 3) preview.Invalidate(); };
            timer.Start();
            SelectSlot(TileCatalog.IsGround(initialTile) ? 0 : 1, initialTile);
            SetColor(canvas.BrushColor);
            UpdateHistory();
        }

        private Control BuildToolbar()
        {
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Forest, Margin = Padding.Empty,
                Padding = new Padding(14, 9, 0, 0), WrapContents = false };
            panel.Controls.Add(new Label { Text = "Sprite Studio", ForeColor = Color.White, AutoSize = true,
                Font = new Font(Font.FontFamily, 16f, FontStyle.Bold), Margin = new Padding(3, 2, 18, 0) });
            undoButton = AddCommand(panel, "Undo", delegate { Restore(undo, redo); });
            redoButton = AddCommand(panel, "Redo", delegate { Restore(redo, undo); });
            AddCommand(panel, "Import PNG", ImportPng);
            AddCommand(panel, "Save PNG", SavePng);
            AddCommand(panel, "Save art bundle", SaveArt);
            AddCommand(panel, "Reset art", ResetArt);
            AddCommand(panel, "Done", Close);
            Shown += delegate { canvas.Focus(); };
            return panel;
        }

        private Control BuildSpriteOptions()
        {
            var panel = Sidebar();
            panel.Controls.Add(Heading("Sprite"));
            category.Items.AddRange(new object[] { "Ground tiles", "Object tiles", "Walking character", "Pink blob", "Townsfolk NPCs" });
            category.SelectedIndexChanged += delegate { if (!adjusting) FillSlots(0); };
            slot.SelectedIndexChanged += delegate { if (!adjusting) DisplaySlot(); };
            panel.Controls.Add(category);
            panel.Controls.Add(slot);
            panel.Controls.Add(preview);
            panel.Controls.Add(Heading("Tools"));

            pencilButton = AddTool(panel, "Pencil  ·  B", SpriteTool.Pencil);
            pencilButton.Checked = true;
            AddTool(panel, "Eraser  ·  E", SpriteTool.Eraser);
            AddTool(panel, "Fill  ·  G", SpriteTool.Fill);
            AddTool(panel, "Eyedropper  ·  I", SpriteTool.Eyedropper);
            panel.Controls.Add(Heading("View"));
            zoom.Items.AddRange(new object[] { "8 px", "12 px", "16 px", "20 px", "24 px", "Fit" });
            zoom.SelectedIndex = 5;
            zoom.SelectedIndexChanged += delegate { canvas.PixelSize = zoom.SelectedIndex == 5 ? 100 : 8 + 4 * zoom.SelectedIndex; };
            canvas.PixelSize = 100;
            panel.Controls.Add(zoom);
            var grid = new CheckBox { Text = "Pixel grid", Checked = true, AutoSize = true };
            grid.CheckedChanged += delegate { canvas.ShowGrid = grid.Checked; canvas.Invalidate(); };
            panel.Controls.Add(grid);
            var blocks = new CheckBox { Text = "8 × 8 tile guides", Checked = true, AutoSize = true };
            blocks.CheckedChanged += delegate { canvas.ShowSubdivisions = blocks.Checked; canvas.Invalidate(); };
            panel.Controls.Add(blocks);
            return panel;
        }

        private Control BuildPalette()
        {
            var panel = Sidebar();
            panel.Controls.Add(Heading("Silver-style colors"));
            var colors = new FlowLayoutPanel { Width = 190, Height = 156, Margin = Padding.Empty };
            foreach (var color in ColorSwatches)
                colors.Controls.Add(ColorButton(Color.FromArgb(unchecked((int)0xFF000000) | color), "#" + color.ToString("X6")));
            panel.Controls.Add(colors);
            swatch.Click += delegate
            {
                using (var dialog = new ColorDialog { Color = canvas.BrushColor, FullOpen = true })
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                        SetColor(dialog.Color);
            };
            panel.Controls.Add(swatch);
            panel.Controls.Add(Heading("Outfit colors"));
            panel.Controls.Add(new Label { Text = "The stock hero uses the shirt accent. Custom heroes can use all four outfit groups.",
                Width = 190, Height = 72 });
            for (var part = 0; part < PartNames.Length; part++)
            {
                var row = new FlowLayoutPanel { Width = 190, Height = 30, WrapContents = false, Margin = Padding.Empty };
                row.Controls.Add(new Label { Text = PartNames[part], Width = 95, Margin = new Padding(0, 6, 0, 0) });
                row.Controls.Add(ColorButton(GameArt.GetOutfitMarker(part, 0), PartNames[part] + " light"));
                row.Controls.Add(ColorButton(GameArt.GetOutfitMarker(part, 1), PartNames[part] + " shade"));
                panel.Controls.Add(row);
            }
            return panel;
        }

        private void SelectSlot(int selectedCategory, int id)
        {
            adjusting = true;
            category.SelectedIndex = selectedCategory;
            adjusting = false;
            FillSlots(id);
        }

        private void FillSlots(int id)
        {
            adjusting = true;
            slot.Items.Clear();
            var categoryIndex = category.SelectedIndex;
            if (categoryIndex == 2)
            {
                for (var facing = 0; facing < Directions.Length; facing++)
                    for (var pose = 0; pose < Frames.Length; pose++)
                        slot.Items.Add(new SpriteSlot(Directions[facing] + " · " + Frames[pose], facing * Frames.Length + pose));
            }
            else if (categoryIndex == 3)
            {
                slot.Items.Add(new SpriteSlot("Idle", 0));
                slot.Items.Add(new SpriteSlot("Hop", 1));
                slot.Items.Add(new SpriteSlot("Land", 2));
            }
            else if (categoryIndex == 4)
            {
                for (var pose = 0; pose < GameArt.NpcFrames; pose++)
                    for (var variant = 0; variant < GameArt.NpcVariants; variant++)
                        slot.Items.Add(new SpriteSlot("NPC " + (variant + 1) + (pose == 0 ? " · Idle" : " · Alternate"),
                            pose * GameArt.NpcVariants + variant));
            }
            else
            {
                var sets = categoryIndex == 0 ? TileCatalog.GroundSets : TileCatalog.ObjectSets;
                foreach (var set in sets)
                    for (var tile = set.First; tile < set.First + set.Count; tile++)
                        slot.Items.Add(new SpriteSlot(TileCatalog.Names[tile], tile));
            }

            var selection = 0;
            for (var item = 0; item < slot.Items.Count; item++)
                if (((SpriteSlot)slot.Items[item]).Index == id)
                    selection = item;
            slot.SelectedIndex = selection;
            adjusting = false;
            DisplaySlot();
        }

        private void DisplaySlot()
        {
            var selected = (SpriteSlot)slot.SelectedItem;
            if (selected == null)
                return;

            spriteCategory = category.SelectedIndex;
            index = selected.Index;
            var old = canvas.Pixels;
            canvas.Pixels = ReadSprite(spriteCategory, index);
            if (old != null)
                old.Dispose();

            SetColor(canvas.BrushColor);
            canvas.EraserColor = IsGround
                ? canvas.Pixels.GetPixel(0, 0) : Color.FromArgb(0, 0, 0, 0);
            preview.Invalidate();
            UpdateStatus(new Point(-1, -1));
        }

        private void PaintPreview(object sender, PaintEventArgs e)
        {
            if (canvas.Pixels == null)
                return;

            var frameToDraw = frame == 3 ? 0 : frame;
            var width = canvas.Pixels.Width * 3;
            var height = canvas.Pixels.Height * 3;
            var bounds = new Rectangle((preview.Width - width) / 2, (preview.Height - height) / 2, width, height);
            GameArt.UsePixelArt(e.Graphics);
            if (spriteCategory == 2)
            {
                using (var sprite = art.GetSprite(true, index / GameArt.PlayerFrames * GameArt.PlayerFrames + frameToDraw))
                    e.Graphics.DrawImage(sprite, bounds, 0, 0, sprite.Width, sprite.Height, GraphicsUnit.Pixel);
            }
            else if (spriteCategory == 3)
            {
                art.DrawCreature(e.Graphics, bounds, frameToDraw);
            }
            else if (spriteCategory == 4)
            {
                art.DrawNpc(e.Graphics, bounds, index % GameArt.NpcVariants, index / GameArt.NpcVariants);
            }
            else
            {
                if (TileCatalog.IsObject(index))
                    art.DrawTile(e.Graphics, 0, bounds);
                art.DrawTile(e.Graphics, index, bounds);
            }
        }

        private void SetColor(Color color)
        {
            if (color.A != 255)
            {
                if (IsGround)
                    color = Color.FromArgb(255, color);
                else if (color.A == 0)
                    color = Color.FromArgb(0, 0, 0, 0);
            }
            canvas.BrushColor = color;
            swatch.UseVisualStyleBackColor = false;
            swatch.BackColor = color.A == 0 ? Color.Gainsboro : Color.FromArgb(255, color);
            swatch.ForeColor = ((0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) > 147) || color.A == 0 ? Color.Black : Color.White;
            swatch.Text = color.A == 0 ? "Transparent" : "Custom color   #" + color.ToArgb().ToString("X8").Substring(2);
        }

        private void UpdateStatus(Point position)
        {
            if (slot.SelectedItem == null)
                return;
            var size = canvas.Pixels.Width + " × " + canvas.Pixels.Height + " px";
            status.Text = slot.SelectedItem + "     ·     " + size + (position.X >= 0 ? "     ·     X " + position.X + "   Y " + position.Y : "") +
                (IsGround ? "     ·     Ground erases to the tile’s top-left color" : "     ·     Right-click or Delete to erase");
        }

        private Bitmap ReadSprite(int selectedCategory, int id)
        {
            if (selectedCategory == 4)
                return art.GetNpcFrame(id);
            return selectedCategory == 3 ? art.GetCreatureFrame(id) : art.GetSprite(selectedCategory == 2, id);
        }

        private void WriteSprite(int selectedCategory, int id, Bitmap pixels)
        {
            if (selectedCategory == 4) art.SetNpcFrame(id, pixels);
            else if (selectedCategory == 3) art.SetCreatureFrame(id, pixels);
            else art.SetSprite(selectedCategory == 2, id, pixels);
        }

        private void CommitCanvas()
        {
            Push(undo, new SpriteEdit(spriteCategory, index, ReadSprite(spriteCategory, index)));
            Clear(redo);
            WriteSprite(spriteCategory, index, canvas.Pixels);
            FinishEdit();
        }

        private void Restore(List<SpriteEdit> from, List<SpriteEdit> to)
        {
            if (from.Count == 0)
                return;

            var edit = from[from.Count - 1];
            from.RemoveAt(from.Count - 1);
            using (edit)
            {
                if (edit.Bundle != null)
                {
                    Push(to, new SpriteEdit(edit.Category, edit.Index, art.ToBundle()));
                    art.LoadBundle(edit.Bundle);
                }
                else
                {
                    Push(to, new SpriteEdit(edit.Category, edit.Index, ReadSprite(edit.Category, edit.Index)));
                    WriteSprite(edit.Category, edit.Index, edit.Pixels);
                }
                SelectSlot(edit.Category, edit.Index);
            }
            FinishEdit();
        }

        private void FinishEdit()
        {
            artEdited();
            UpdateHistory();
            preview.Invalidate();
        }

        private void UpdateHistory()
        {
            undoButton.Enabled = undo.Count != 0;
            redoButton.Enabled = redo.Count != 0;
        }

        private static void Push(List<SpriteEdit> history, SpriteEdit edit)
        {
            if (history.Count == 96)
            {
                history[0].Dispose();
                history.RemoveAt(0);
            }
            history.Add(edit);
        }

        private static void Clear(List<SpriteEdit> history)
        {
            foreach (var edit in history)
                edit.Dispose();
            history.Clear();
        }

        private void ImportPng()
        {
            using (var dialog = new OpenFileDialog { Filter = "PNG image (*.png)|*.png" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                TryImageAction(delegate
                {
                    using (var source = new Bitmap(dialog.FileName))
                    {
                        var legacyPlayer = spriteCategory == 2 &&
                            source.Size == new Size(GameArt.PlayerWidth, GameArt.LegacyPlayerHeight);
                        if (source.Size != canvas.Pixels.Size && !legacyPlayer)
                            throw new InvalidDataException("Use a PNG matching the selected sprite size.");
                        using (var pixels = legacyPlayer ? GameArt.ConvertLegacyFrame(source) :
                            source.Clone(new Rectangle(Point.Empty, source.Size), PixelFormat.Format32bppArgb))
                        {
                            if (IsGround)
                                for (var y = 0; y < pixels.Height; y++)
                                    for (var x = 0; x < pixels.Width; x++)
                                        if (pixels.GetPixel(x, y).A != 255)
                                            throw new InvalidDataException("Ground tiles need an opaque PNG image.");

                            Push(undo, new SpriteEdit(spriteCategory, index, ReadSprite(spriteCategory, index)));
                            Clear(redo);
                            WriteSprite(spriteCategory, index, pixels);
                            DisplaySlot();
                            FinishEdit();
                        }
                    }
                });
            }
        }

        private void SavePng()
        {
            using (var dialog = new SaveFileDialog { Filter = "PNG image (*.png)|*.png", DefaultExt = "png", AddExtension = true,
                FileName = SpriteFileName() })
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    TryImageAction(delegate { canvas.Pixels.Save(dialog.FileName, ImageFormat.Png); });
        }

        private string SpriteFileName()
        {
            var prefix = spriteCategory == 2 ? "player-" : spriteCategory == 3 ? "blob-" : spriteCategory == 4 ? "npc-" : "";
            return prefix + index.ToString(CultureInfo.InvariantCulture) + ".png";
        }

        private void ResetArt()
        {
            var choice = MessageBox.Show(this, "Replace the tiles and sprites with the Silver-style defaults? Maps, events, and outfit choices stay in the project.",
                "Reset artwork", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (choice == DialogResult.Yes)
                TryImageAction(UseDefaultArt);
        }

        private void UseDefaultArt()
        {
            var snapshot = art.ToBundle();
            art.LoadBundle(null);
            Push(undo, new SpriteEdit(spriteCategory, index, snapshot));
            Clear(redo);
            DisplaySlot();
            FinishEdit();
        }

        private void SaveArt()
        {
            using (var dialog = new SaveFileDialog { Filter = "Mossbell Vale art bundle (*.rpga)|*.rpga", DefaultExt = "rpga",
                AddExtension = true, FileName = "art.rpga" })
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    TryImageAction(delegate { WorldData.WriteAtomically(dialog.FileName, art.ToBundle()); });
        }

        private void TryImageAction(Action action)
        {
            try
            {
                action();
            }
            catch (Exception error)
            {
                MessageBox.Show(this, error.Message, "Sprite Studio", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static FlowLayoutPanel Sidebar()
        {
            return new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                AutoScroll = true, WrapContents = false, Padding = new Padding(14, 9, 9, 8), Margin = Padding.Empty };
        }

        private Label Heading(string name)
        {
            return new Label { Text = name, AutoSize = true, Font = new Font(Font.FontFamily, 12f, FontStyle.Bold),
                Margin = new Padding(0, 8, 0, 5) };
        }

        private static ComboBox CreatePicker(string name)
        {
            return new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 192,
                AccessibleName = name, Margin = new Padding(0, 0, 0, 6), MaxDropDownItems = 18 };
        }

        private RadioButton AddTool(FlowLayoutPanel panel, string label, SpriteTool tool)
        {
            var button = new RadioButton { Text = label, Appearance = Appearance.Button, Width = 192, Height = 31,
                TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 1, 0, 3), Tag = tool };
            button.CheckedChanged += delegate { if (button.Checked) canvas.Tool = tool; };
            panel.Controls.Add(button);
            return button;
        }

        private static Button AddCommand(FlowLayoutPanel panel, string label, Action action)
        {
            var button = new Button { Text = label, AutoSize = true, MinimumSize = new Size(62, 34),
                Margin = new Padding(0, 1, 6, 0), ForeColor = Color.White, BackColor = Color.FromArgb(43, 78, 60),
                UseVisualStyleBackColor = false, FlatStyle = FlatStyle.Flat, Padding = new Padding(4, 0, 4, 0) };
            button.FlatAppearance.BorderColor = Color.FromArgb(99, 140, 117);
            button.Click += delegate { action(); };
            panel.Controls.Add(button);
            return button;
        }

        private Button ColorButton(Color color, string name)
        {
            var button = new Button { Width = 29, Height = 29, BackColor = color, FlatStyle = FlatStyle.Flat,
                Margin = new Padding(1), AccessibleName = name, UseVisualStyleBackColor = false };
            button.FlatAppearance.BorderColor = Color.FromArgb(70, 39, 64, 53);
            button.Click += delegate { SetColor(color); };
            return button;
        }

        private void SetTool(SpriteTool value)
        {
            foreach (Control control in pencilButton.Parent.Controls)
            {
                var button = control as RadioButton;
                if (button != null && (SpriteTool)button.Tag == value)
                    button.Checked = true;
            }
        }

        protected override bool ProcessCmdKey(ref Message message, Keys keys)
        {
            switch (keys)
            {
                case Keys.Control | Keys.Z: Restore(undo, redo); return true;
                case Keys.Control | Keys.Y: Restore(redo, undo); return true;
                case Keys.B: SetTool(SpriteTool.Pencil); return true;
                case Keys.E: SetTool(SpriteTool.Eraser); return true;
                case Keys.G: SetTool(SpriteTool.Fill); return true;
                case Keys.I: SetTool(SpriteTool.Eyedropper); return true;
                default: return base.ProcessCmdKey(ref message, keys);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Dispose();
                Clear(undo);
                Clear(redo);
                if (canvas.Pixels != null)
                    canvas.Pixels.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
