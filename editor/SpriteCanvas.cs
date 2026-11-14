using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MossbellVale.Editor
{
    internal enum SpriteTool
    {
        Pencil,
        Eraser,
        Fill,
        Eyedropper
    }

    internal sealed class SpriteCanvas : Control
    {
        private static readonly Color Surface = Color.FromArgb(22, 44, 37);
        private static readonly Color Outline = Color.FromArgb(193, 217, 201);
        private Bitmap pixels;
        private Point hovered = new Point(-1, -1);
        private Point previous;
        private Point keyboardCell;
        private bool showKeyboardCell;
        private SpriteTool activeTool;
        private Color activeColor;
        private bool drawing;
        private bool edited;
        private int pixelSize = 18;

        public event Action<Color> ColorPicked;
        public event Action<Point> Positioned;
        public event Action Edited;

        public SpriteTool Tool { get; set; }
        public Color BrushColor { get; set; }
        public Color EraserColor { get; set; }
        public bool ShowGrid { get; set; }

        public Bitmap Pixels
        {
            get { return pixels; }
            set
            {
                pixels = value;
                hovered = new Point(-1, -1);
                keyboardCell = Point.Empty;
                Invalidate();
            }
        }

        public int PixelSize
        {
            get { return pixelSize; }
            set
            {
                pixelSize = value;
                Invalidate();
            }
        }

        private int CellSize
        {
            get
            {
                if (pixels == null)
                    return pixelSize;

                return Math.Max(2, Math.Min(pixelSize, Math.Min(
                    (Width - 26) / pixels.Width, (Height - 26) / pixels.Height)));
            }
        }

        private Rectangle GridBounds
        {
            get
            {
                var cell = CellSize;
                var width = pixels.Width * cell;
                var height = pixels.Height * cell;
                return new Rectangle((Width - width) / 2, (Height - height) / 2, width, height);
            }
        }

        public SpriteCanvas()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            BackColor = Surface;
            Cursor = Cursors.Cross;
            AccessibleName = "Sprite pixel grid";
            AccessibleDescription = "Use arrows to position the cursor, Space to apply the tool, or Delete to erase.";
            TabStop = true;
            ShowGrid = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (pixels == null)
                return;

            var cell = CellSize;
            var bounds = GridBounds;
            using (var light = new SolidBrush(Color.FromArgb(231, 238, 231)))
            using (var dark = new SolidBrush(Color.FromArgb(172, 190, 178)))
            {
                e.Graphics.FillRectangle(dark, bounds);
                for (var y = 0; y < pixels.Height; y++)
                    for (var x = y % 2; x < pixels.Width; x += 2)
                        e.Graphics.FillRectangle(light, bounds.X + x * cell, bounds.Y + y * cell, cell, cell);
            }

            GameArt.UsePixelArt(e.Graphics);
            e.Graphics.DrawImage(pixels, bounds, 0, 0, pixels.Width, pixels.Height, GraphicsUnit.Pixel);
            if (ShowGrid && cell >= 5)
            {
                using (var grid = new Pen(Color.FromArgb(62, 16, 35, 26)))
                {
                    for (var x = bounds.Left + cell; x < bounds.Right; x += cell)
                        e.Graphics.DrawLine(grid, x, bounds.Top, x, bounds.Bottom);
                    for (var y = bounds.Top + cell; y < bounds.Bottom; y += cell)
                        e.Graphics.DrawLine(grid, bounds.Left, y, bounds.Right, y);
                }
            }
            using (var outline = new Pen(Outline))
                e.Graphics.DrawRectangle(outline, bounds.X - 1, bounds.Y - 1, bounds.Width + 1, bounds.Height + 1);

            var cursor = hovered.X >= 0 ? hovered : showKeyboardCell && Focused ? keyboardCell : new Point(-1, -1);
            if (cursor.X >= 0)
            {
                var x = bounds.X + cursor.X * cell;
                var y = bounds.Y + cursor.Y * cell;
                e.Graphics.DrawRectangle(Pens.Black, x, y, cell, cell);
                if (cell >= 5)
                    e.Graphics.DrawRectangle(Pens.White, x + 1, y + 1, cell - 2, cell - 2);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            var hit = GetPixel(e.Location);
            if (hit.X < 0 || (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right))
                return;

            Focus();
            keyboardCell = hit;
            showKeyboardCell = false;
            activeTool = e.Button == MouseButtons.Right ? SpriteTool.Eraser : Tool;
            if (activeTool == SpriteTool.Eyedropper)
            {
                if (ColorPicked != null)
                    ColorPicked(pixels.GetPixel(hit.X, hit.Y));
                return;
            }

            activeColor = activeTool == SpriteTool.Eraser ? EraserColor : BrushColor;
            if (activeTool == SpriteTool.Fill)
            {
                edited = FloodFill(hit, activeColor);
                Publish();
                return;
            }

            drawing = true;
            Capture = true;
            previous = hit;
            edited = SetPixel(hit, activeColor);
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var hit = GetPixel(e.Location);
            if (hovered != hit)
            {
                hovered = hit;
                if (Positioned != null)
                    Positioned(hit);
                Invalidate();
            }
            if (!drawing)
                return;

            if (hit.X < 0)
            {
                previous = hit;
                return;
            }
            if (previous.X < 0)
                previous = hit;
            if (PaintLine(previous, hit))
            {
                edited = true;
                Invalidate();
            }
            previous = hit;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (drawing)
                Capture = false;
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (!Capture && drawing)
            {
                drawing = false;
                Publish();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hovered = new Point(-1, -1);
            if (Positioned != null)
                Positioned(hovered);
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.Space:
                case Keys.Delete:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (pixels == null || !IsInputKey(e.KeyData) || drawing)
                return;

            switch (e.KeyData)
            {
                case Keys.Left: keyboardCell.X = Math.Max(0, keyboardCell.X - 1); break;
                case Keys.Right: keyboardCell.X = Math.Min(pixels.Width - 1, keyboardCell.X + 1); break;
                case Keys.Up: keyboardCell.Y = Math.Max(0, keyboardCell.Y - 1); break;
                case Keys.Down: keyboardCell.Y = Math.Min(pixels.Height - 1, keyboardCell.Y + 1); break;
                case Keys.Space: ApplyKeyboardTool(Tool); break;
                case Keys.Delete: ApplyKeyboardTool(SpriteTool.Eraser); break;
                default: return;
            }
            hovered = new Point(-1, -1);
            showKeyboardCell = true;
            if (Positioned != null)
                Positioned(keyboardCell);
            e.Handled = true;
            e.SuppressKeyPress = true;
            Invalidate();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            showKeyboardCell = true;
            if (Positioned != null)
                Positioned(keyboardCell);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        private void ApplyKeyboardTool(SpriteTool tool)
        {
            if (tool == SpriteTool.Eyedropper)
            {
                if (ColorPicked != null)
                    ColorPicked(pixels.GetPixel(keyboardCell.X, keyboardCell.Y));
                return;
            }

            if (tool == SpriteTool.Fill)
                edited = FloodFill(keyboardCell, BrushColor);
            else
                edited = SetPixel(keyboardCell, tool == SpriteTool.Eraser ? EraserColor : BrushColor);
            Publish();
        }

        private void Publish()
        {
            if (!edited)
                return;

            edited = false;
            if (Edited != null)
                Edited();
            Invalidate();
        }

        private Point GetPixel(Point location)
        {
            if (pixels == null || !GridBounds.Contains(location))
                return new Point(-1, -1);
            var bounds = GridBounds;
            return new Point((location.X - bounds.X) / CellSize, (location.Y - bounds.Y) / CellSize);
        }

        private bool PaintLine(Point from, Point to)
        {
            var changed = SetPixel(from, activeColor);
            var xDistance = Math.Abs(to.X - from.X);
            var yDistance = Math.Abs(to.Y - from.Y);
            var xStep = to.X > from.X ? 1 : -1;
            var yStep = to.Y > from.Y ? 1 : -1;
            var error = xDistance - yDistance;
            while (from != to)
            {
                var doubled = error * 2;
                if (doubled > -yDistance)
                {
                    error -= yDistance;
                    from.X += xStep;
                }
                if (doubled < xDistance)
                {
                    error += xDistance;
                    from.Y += yStep;
                }
                changed |= SetPixel(from, activeColor);
            }
            return changed;
        }

        private bool SetPixel(Point point, Color color)
        {
            var current = pixels.GetPixel(point.X, point.Y);
            if (current.ToArgb() == color.ToArgb())
                return false;
            pixels.SetPixel(point.X, point.Y, color);
            return true;
        }

        private bool FloodFill(Point start, Color color)
        {
            var oldColor = pixels.GetPixel(start.X, start.Y).ToArgb();
            if (oldColor == color.ToArgb())
                return false;

            var pending = new Queue<Point>();
            pending.Enqueue(start);
            pixels.SetPixel(start.X, start.Y, color);
            while (pending.Count > 0)
            {
                var point = pending.Dequeue();
                EnqueuePixel(pending, point.X - 1, point.Y, oldColor, color);
                EnqueuePixel(pending, point.X + 1, point.Y, oldColor, color);
                EnqueuePixel(pending, point.X, point.Y - 1, oldColor, color);
                EnqueuePixel(pending, point.X, point.Y + 1, oldColor, color);
            }
            return true;
        }

        private void EnqueuePixel(Queue<Point> pending, int x, int y, int oldColor, Color color)
        {
            if (x < 0 || y < 0 || x >= pixels.Width || y >= pixels.Height || pixels.GetPixel(x, y).ToArgb() != oldColor)
                return;

            pixels.SetPixel(x, y, color);
            pending.Enqueue(new Point(x, y));
        }
    }
}
