using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MossbellVale.Editor
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            try
            {
                Application.Run(new EditorWindow(args.Length == 0 ? null : args[0]));
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "Mossbell Vale Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.ExitCode = 1;
            }
        }
    }

    internal sealed class WorldSizeDialog : Form
    {
        private readonly NumericUpDown width = CreateField(32, "Width");
        private readonly NumericUpDown height = CreateField(24, "Height");

        public int WorldWidth { get { return (int)width.Value; } }
        public int WorldHeight { get { return (int)height.Value; } }

        public WorldSizeDialog()
        {
            Text = "New world";
            Font = new Font("Segoe UI", 10f);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(290, 152);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3,
                Padding = new Padding(12) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            layout.Controls.Add(new Label { Text = "Width in tiles", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            layout.Controls.Add(width, 1, 0);
            layout.Controls.Add(new Label { Text = "Height in tiles", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            layout.Controls.Add(height, 1, 1);
            var create = new Button { Text = "Create", DialogResult = DialogResult.OK, AutoSize = true, Anchor = AnchorStyles.Right };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true, Anchor = AnchorStyles.Right };
            layout.Controls.Add(create, 0, 2);
            layout.Controls.Add(cancel, 1, 2);
            AcceptButton = create;
            CancelButton = cancel;
            Controls.Add(layout);
        }

        private static NumericUpDown CreateField(int value, string name)
        {
            return new NumericUpDown { Minimum = WorldData.MinimumSize, Maximum = WorldData.MaximumSize, Value = value,
                AccessibleName = name, Anchor = AnchorStyles.Left | AnchorStyles.Right };
        }
    }
}
