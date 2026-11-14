using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MossbellVale.Editor
{
    internal sealed class PublishWindow : Form
    {
        private readonly byte[] project;
        private readonly byte[] art;
        private readonly string projectKey;
        private readonly TextBox title = Field("Game title");
        private readonly TextBox version = Field("Version");
        private readonly TextBox uniqueId = Field("Title ID");
        private readonly TextBox repository = Field("GitHub repository");
        private readonly TextBox toolRoot = Field("3DS toolchain folder");
        private readonly TextBox destination = Field("Output CIA");
        private readonly TextBox log = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
            ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 9.5f), AccessibleName = "Build log",
            BackColor = Color.FromArgb(24, 44, 39), ForeColor = Color.FromArgb(231, 242, 226) };
        private readonly Label status = new Label { AutoSize = true, Anchor = AnchorStyles.Left, Text = "Ready to build" };
        private readonly Button generate = MakeButton("New ID");
        private readonly Button browseTools = MakeButton("Browse…");
        private readonly Button browseOutput = MakeButton("Browse…");
        private readonly Button build = MakeButton("Build CIA");
        private readonly Button upload = MakeButton("Upload to GitHub");
        private readonly Button close = MakeButton("Close");
        private CancellationTokenSource running;
        private bool closeWhenDone;
        private PublishOptions built;
        private string artifactHash;

        public PublishWindow(byte[] projectBytes, byte[] artBytes, string path = null)
        {
            project = projectBytes;
            art = artBytes;
            projectKey = path;
            var originalTitle = PublishTool.SafeTitle(String.IsNullOrWhiteSpace(path) ? null : Path.GetFileNameWithoutExtension(path));
            title.Text = originalTitle;
            version.Text = "1.0.0";
            uniqueId.Text = PublishTool.NewUniqueId();
            toolRoot.Text = PublishTool.DefaultToolRoot;
            destination.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Mossbell Builds", originalTitle + ".cia");
            RestoreSettings(originalTitle);

            Text = "Publish a 3DS game";
            Font = new Font("Segoe UI", 10f);
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(640, 520);
            ClientSize = new Size(745, 570);
            ShowInTaskbar = false;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(248, 249, 242);
            ForeColor = Color.FromArgb(27, 54, 44);

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 10,
                Padding = new Padding(18, 13, 18, 13) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 106));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
            for (var row = 1; row <= 6; row++)
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            var prompt = new Label { Text = "Build an installable CIA from this project’s maps and sprites. " +
                "The first build downloads the portable 3DS tools.", Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft };
            layout.Controls.Add(prompt, 0, 0);
            layout.SetColumnSpan(prompt, 3);
            AddRow(layout, 1, "Game title", title, null);
            AddRow(layout, 2, "Version", version, null);
            AddRow(layout, 3, "Title ID", uniqueId, generate);
            AddRow(layout, 4, "GitHub (optional)", repository, null);
            AddRow(layout, 5, "3DS tools folder", toolRoot, browseTools);
            AddRow(layout, 6, "Output CIA", destination, browseOutput);
            layout.Controls.Add(log, 0, 7);
            layout.SetColumnSpan(log, 3);
            layout.Controls.Add(status, 0, 8);
            layout.SetColumnSpan(status, 3);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false, Margin = Padding.Empty };
            buttons.Controls.Add(close);
            buttons.Controls.Add(upload);
            buttons.Controls.Add(build);
            layout.Controls.Add(buttons, 0, 9);
            layout.SetColumnSpan(buttons, 3);
            Controls.Add(layout);

            build.BackColor = Color.FromArgb(62, 110, 86);
            build.ForeColor = Color.White;
            build.FlatStyle = FlatStyle.Flat;
            build.FlatAppearance.BorderSize = 0;
            upload.Enabled = false;
            generate.Click += delegate { uniqueId.Text = PublishTool.NewUniqueId(); };
            browseTools.Click += delegate { BrowseTools(); };
            browseOutput.Click += delegate { BrowseOutput(); };
            build.Click += async delegate { await BuildAsync(); };
            upload.Click += async delegate { await UploadAsync(); };
            close.Click += delegate { if (running != null) CancelWork(); else Close(); };
            foreach (var field in new[] { title, version, uniqueId, repository, toolRoot, destination })
                field.TextChanged += delegate { upload.Enabled = false; };
            AcceptButton = build;
            CancelButton = close;
        }

        private static TextBox Field(string accessibleName)
        {
            return new TextBox { AccessibleName = accessibleName, Dock = DockStyle.Fill, Margin = new Padding(3, 6, 6, 3) };
        }

        private static Button MakeButton(string text)
        {
            return new Button { Text = text, AutoSize = true, MinimumSize = new Size(85, 31),
                Margin = new Padding(5, 3, 2, 3) };
        }

        private static void AddRow(TableLayoutPanel layout, int row, string name, TextBox field, Button action)
        {
            layout.Controls.Add(new Label { Text = name, Anchor = AnchorStyles.Left, AutoSize = true }, 0, row);
            layout.Controls.Add(field, 1, row);
            if (action == null) layout.SetColumnSpan(field, 2);
            else
            {
                action.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                layout.Controls.Add(action, 2, row);
            }
        }

        private void RestoreSettings(string fallbackTitle)
        {
            var config = PublishTool.SettingsFile(projectKey, fallbackTitle);
            try
            {
                if (!File.Exists(config)) return;
                var values = File.ReadAllLines(config);
                if (values.Length != 6) return;
                title.Text = PublishTool.SafeTitle(values[0]);
                uniqueId.Text = values[1];
                version.Text = values[2];
                repository.Text = values[3];
                toolRoot.Text = values[4];
                destination.Text = values[5];
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private void SaveSettings(PublishOptions options)
        {
            try
            {
                var config = PublishTool.SettingsFile(projectKey, "My Mossbell Game");
                Directory.CreateDirectory(Path.GetDirectoryName(config));
                File.WriteAllLines(config, new[] { options.Title, options.UniqueId, options.Version,
                    options.Repository, options.ToolRoot, options.OutputFile }, new UTF8Encoding(false));
            }
            catch (IOException) { AppendLog("The build settings could not be saved."); }
            catch (UnauthorizedAccessException) { AppendLog("The build settings could not be saved."); }
        }

        private void BrowseTools()
        {
            using (var dialog = new FolderBrowserDialog { Description = "Choose a folder for the portable 3DS tools",
                SelectedPath = toolRoot.Text })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK) toolRoot.Text = dialog.SelectedPath;
            }
        }

        private void BrowseOutput()
        {
            using (var dialog = new SaveFileDialog { Filter = "Nintendo 3DS CIA (*.cia)|*.cia", DefaultExt = "cia",
                FileName = Path.GetFileName(destination.Text), OverwritePrompt = false })
            {
                var folder = Path.GetDirectoryName(destination.Text);
                if (Directory.Exists(folder)) dialog.InitialDirectory = folder;
                if (dialog.ShowDialog(this) == DialogResult.OK) destination.Text = dialog.FileName;
            }
        }

        private async Task BuildAsync()
        {
            var options = new PublishOptions { Title = title.Text.Trim(), Version = version.Text.Trim(),
                UniqueId = uniqueId.Text.Trim().ToUpperInvariant(), Repository = repository.Text.Trim(),
                ToolRoot = toolRoot.Text.Trim(), OutputFile = destination.Text.Trim() };
            try
            {
                PublishTool.Validate(options);
                if (File.Exists(options.OutputFile) && MessageBox.Show(this,
                    "Replace the existing CIA after the new build succeeds?\n" + options.OutputFile,
                    "Replace CIA", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            }
            catch (Exception error) { status.Text = error.Message; return; }

            built = null;
            artifactHash = null;
            log.Clear();
            await WorkAsync("Building CIA…", "CIA built", token =>
            {
                PublishTool.Build(options, project, art, AppendLog, token);
                artifactHash = PublishTool.HashFile(options.OutputFile);
                built = options;
            });
            if (built != null && !IsDisposed) SaveSettings(options);
        }

        private async Task UploadAsync()
        {
            var options = built;
            if (options == null || options.Repository.Length == 0) return;
            if (MessageBox.Show(this, "Create release v" + options.Version + " in " + options.Repository +
                " and upload the CIA and its update catalog?", "Upload to GitHub", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes) return;
            await WorkAsync("Uploading to GitHub…", "GitHub release created",
                token => PublishTool.Upload(options, artifactHash, AppendLog, token));
        }

        private async Task WorkAsync(string pending, string complete, Action<CancellationToken> action)
        {
            running = new CancellationTokenSource();
            var token = running.Token;
            SetBusy(true);
            status.Text = pending;
            try
            {
                await Task.Run(() => action(token));
                status.Text = complete;
            }
            catch (OperationCanceledException) { status.Text = "Canceled"; AppendLog("Canceled."); }
            catch (Exception error) { status.Text = "See the build log"; AppendLog(error.Message); }
            finally
            {
                running.Dispose();
                running = null;
                SetBusy(false);
                if (closeWhenDone) Close();
            }
        }

        private void SetBusy(bool busy)
        {
            foreach (var control in new Control[] { title, version, uniqueId, repository, toolRoot, destination,
                generate, browseTools, browseOutput, build })
                control.Enabled = !busy;
            upload.Enabled = !busy && built != null && built.Repository.Length != 0;
            close.Text = busy ? "Cancel" : "Close";
            close.Enabled = true;
            UseWaitCursor = busy;
        }

        private void CancelWork()
        {
            running.Cancel();
            close.Enabled = false;
            status.Text = "Canceling…";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (running != null)
            {
                e.Cancel = true;
                closeWhenDone = true;
                CancelWork();
            }
            base.OnFormClosing(e);
        }

        private void AppendLog(string text)
        {
            if (IsDisposed || !IsHandleCreated) return;
            if (InvokeRequired)
            {
                BeginInvoke((Action<string>)AppendLog, text);
                return;
            }
            log.AppendText(text + Environment.NewLine);
        }
    }
}
