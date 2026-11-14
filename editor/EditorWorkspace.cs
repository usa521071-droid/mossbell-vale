using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MossbellVale.Editor
{
    internal enum EditorView { Map, Movement, Events, Connections, Header }

    internal sealed partial class EditorWindow
    {
        private sealed class MapHistory
        {
            public readonly List<WorldData> Undo = new List<WorldData>();
            public readonly List<WorldData> Redo = new List<WorldData>();
        }

        private readonly Dictionary<int, MapHistory> mapHistories = new Dictionary<int, MapHistory>();
        private readonly TreeView mapTree = new TreeView { Dock = DockStyle.Fill, HideSelection = false,
            BorderStyle = BorderStyle.None, AccessibleName = "Project maps" };
        private readonly TabControl workspace = new TabControl { Dock = DockStyle.Fill, AccessibleName = "Map editing views" };
        private readonly ComboBox movementBrush = MakeSelector("Movement brush");
        private readonly Panel[] mapHosts = new Panel[(int)EditorView.Header];
        private readonly ListView eventList = new ListView { View = View.Details, FullRowSelect = true, MultiSelect = false,
            HideSelection = false, Dock = DockStyle.Bottom, Height = 126, AccessibleName = "Map events" };
        private readonly TextBox mapName = new TextBox { MaxLength = 31, Width = 210, AccessibleName = "Map name" };
        private readonly TextBox mapGroup = new TextBox { MaxLength = 23, Width = 210, AccessibleName = "Map group" };
        private readonly Label mapDetail = new Label { AutoSize = true };
        private readonly Label startDetail = new Label { AutoSize = true };
        private Button startMapButton;
        private Button removeMapButton;
        private EditorView editingView;

        private Control BuildMapTree()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Paper, ColumnCount = 1, RowCount = 3,
                Padding = new Padding(10, 10, 10, 8) };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
            panel.Controls.Add(CreateHeading("Maps"), 0, 0);
            mapTree.BackColor = Paper;
            mapTree.ForeColor = Ink;
            mapTree.ImageList = new ImageList { ColorDepth = ColorDepth.Depth32Bit };
            mapTree.ImageList.Images.Add(MakeTreeIcon(true));
            mapTree.ImageList.Images.Add(MakeTreeIcon(false));
            mapTree.AfterSelect += delegate(object sender, TreeViewEventArgs args)
            {
                var selected = args.Node.Tag as ProjectMap;
                if (selected != null)
                    SelectMap(selected);
            };
            panel.Controls.Add(mapTree, 0, 1);
            var buttons = ActionRow();
            ViewButton(buttons, "Add map", AddMap);
            ViewButton(buttons, "Header", delegate { workspace.SelectedIndex = (int)EditorView.Header; });
            panel.Controls.Add(buttons, 0, 2);
            return panel;
        }

        private static Bitmap MakeTreeIcon(bool group)
        {
            var icon = new Bitmap(16, 16);
            using (var graphics = Graphics.FromImage(icon))
            {
                if (group)
                {
                    using (var ink = new SolidBrush(Color.FromArgb(173, 130, 58)))
                    using (var light = new SolidBrush(Color.FromArgb(239, 207, 124)))
                    {
                        graphics.FillRectangle(ink, 1, 3, 8, 10);
                        graphics.FillRectangle(ink, 1, 5, 14, 9);
                        graphics.FillRectangle(light, 2, 6, 12, 7);
                    }
                }
                else
                {
                    using (var ink = new SolidBrush(Color.FromArgb(48, 104, 79)))
                    using (var light = new SolidBrush(Color.FromArgb(153, 198, 144)))
                    {
                        graphics.FillRectangle(ink, 3, 2, 10, 12);
                        graphics.FillRectangle(light, 4, 3, 8, 10);
                        graphics.FillRectangle(ink, 5, 8, 6, 1);
                        graphics.FillRectangle(ink, 7, 5, 1, 6);
                    }
                }
            }
            return icon;
        }

        private Control BuildWorkspace()
        {
            foreach (EditorView view in Enum.GetValues(typeof(EditorView)))
            {
                var tab = new TabPage(view.ToString()) { BackColor = Paper, Padding = new Padding(2) };
                workspace.TabPages.Add(tab);
            }

            movementBrush.Items.AddRange(new object[] { "0 · Tile default", "1 · Walkable", "2 · Blocked" });
            movementBrush.SelectedIndex = 0;
            movementBrush.Width = 145;
            eventList.Columns.Add("Type", 82);
            eventList.Columns.Add("Source", 76);
            eventList.Columns.Add("Destination or message", 400);
            eventList.DoubleClick += delegate { EditSelectedEvent(); };
            for (var i = 0; i < mapHosts.Length; i++)
                BuildMapView(workspace.TabPages[i], (EditorView)i);
            BuildHeaderTab(workspace.TabPages[(int)EditorView.Header]);
            workspace.SelectedIndexChanged += delegate { ShowView((EditorView)workspace.SelectedIndex); };
            ShowView(EditorView.Map);
            return workspace;
        }

        private void BuildMapView(TabPage page, EditorView view)
        {
            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, view == EditorView.Map ? 0 : 39));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute,
                view == EditorView.Events || view == EditorView.Connections ? 126 : 0));
            var host = new Panel { Dock = DockStyle.Fill, BackColor = Forest, Margin = Padding.Empty };
            mapHosts[(int)view] = host;
            body.Controls.Add(host, 0, 1);
            if (view != EditorView.Map)
            {
                var tools = ActionRow();
                if (view == EditorView.Movement)
                {
                    tools.Controls.Add(movementBrush);
                    tools.Controls.Add(InputLabel("Drag to paint · × blocked · E event · right-click clears"));
                }
                else
                {
                    ViewButton(tools, "Edit selected", EditSelectedEvent);
                    ViewButton(tools, "Delete", DeleteSelectedEvent);
                    if (view == EditorView.Connections)
                        ViewButton(tools, "Go to destination", InspectDestination);
                    tools.Controls.Add(InputLabel(view == EditorView.Connections ? "Click a tile to place a warp." : "Click a tile to place an event."));
                }
                body.Controls.Add(tools, 0, 0);
            }
            page.Controls.Add(body);
        }

        private void BuildHeaderTab(TabPage tab)
        {
            var body = CreateSidePanel();
            body.FlowDirection = FlowDirection.LeftToRight;
            body.WrapContents = true;
            var fields = new FlowLayoutPanel { Width = 277, Height = 420, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, Margin = new Padding(8) };
            fields.Controls.Add(CreateHeading("Map header"));
            fields.Controls.Add(mapDetail);
            fields.Controls.Add(InputLabel("Name"));
            fields.Controls.Add(mapName);
            fields.Controls.Add(InputLabel("Group"));
            fields.Controls.Add(mapGroup);
            ViewButton(fields, "Apply header", ApplyHeader);
            fields.Controls.Add(startDetail);
            startMapButton = ViewButton(fields, "Use as game start", SetStartMap);
            removeMapButton = ViewButton(fields, "Delete map", DeleteMap);
            body.Controls.Add(fields);
            body.Controls.Add(BuildCharacterPanel());
            tab.Controls.Add(body);
        }

        private void ShowView(EditorView view)
        {
            EndStroke();
            editingView = view;
            if (view == EditorView.Header)
                return;

            var scroll = mapViewport.AutoScrollPosition;
            var host = mapHosts[(int)view];
            host.Controls.Add(mapViewport);
            mapViewport.AutoScrollPosition = new Point(-scroll.X, -scroll.Y);
            if (view == EditorView.Events || view == EditorView.Connections)
            {
                var body = (TableLayoutPanel)host.Parent;
                eventList.Dock = DockStyle.Fill;
                body.Controls.Add(eventList, 0, 2);
            }
            map.SetView(view);
            UpdateEventView();
        }

        private static FlowLayoutPanel ActionRow()
        {
            return new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        }

        private static Label InputLabel(string text)
        {
            return new Label { Text = text, AutoSize = true, Margin = new Padding(2, 8, 4, 3) };
        }

        private static Button ViewButton(Control parent, string name, Action action)
        {
            var button = new Button { Text = name, AutoSize = true, MinimumSize = new Size(54, 29),
                Padding = new Padding(4, 0, 4, 0), Margin = new Padding(2, 3, 4, 3) };
            button.Click += delegate { action(); };
            parent.Controls.Add(button);
            return button;
        }

        private void RefreshTree(int selected)
        {
            mapTree.BeginUpdate();
            mapTree.Nodes.Clear();
            var groups = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase);
            TreeNode selectedNode = null;
            foreach (var entry in project.Maps)
            {
                TreeNode group;
                if (!groups.TryGetValue(entry.Group, out group))
                {
                    group = mapTree.Nodes.Add(entry.Group);
                    group.ImageIndex = 0;
                    group.SelectedImageIndex = 0;
                    groups.Add(entry.Group, group);
                }
                var text = entry.Name + (entry.Id == project.StartMap ? "  ★" : "");
                var node = group.Nodes.Add(text);
                node.Tag = entry;
                node.ImageIndex = 1;
                node.SelectedImageIndex = 1;
                if (entry.Id == selected)
                    selectedNode = node;
            }
            mapTree.ExpandAll();
            mapTree.SelectedNode = selectedNode;
            mapTree.EndUpdate();
            if (selectedNode != null && !Object.ReferenceEquals(activeMap, selectedNode.Tag))
                SelectMap((ProjectMap)selectedNode.Tag);
        }

        private void SelectMap(ProjectMap selected)
        {
            if (Object.ReferenceEquals(activeMap, selected))
                return;

            EndStroke();
            activeMap = selected;
            MapHistory history;
            if (!mapHistories.TryGetValue(selected.Id, out history))
            {
                history = new MapHistory();
                mapHistories.Add(selected.Id, history);
            }
            undo = history.Undo;
            redo = history.Redo;
            ApplyWorld(selected.World);
            CenterOnSpawn();
            UpdateCommands();
            SetStatus(selected.Name + " opened");
        }

        private void RefreshMapHeader()
        {
            if (activeMap == null)
                return;
            mapName.Text = activeMap.Name;
            mapGroup.Text = activeMap.Group;
            mapDetail.Text = "Map " + activeMap.Id + "  ·  " + world.Width + " × " + world.Height + " tiles";
            startDetail.Text = "Game start: " + project.Find(project.StartMap).Name;
            startMapButton.Enabled = activeMap.Id != project.StartMap;
            removeMapButton.Enabled = project.Maps.Count > 1 && activeMap.Id != project.StartMap;
        }

        private void ApplyHeader()
        {
            TryFileAction("Map header", delegate
            {
                var name = mapName.Text.Trim();
                var group = mapGroup.Text.Trim();
                ProjectData.ValidateText(name, 1, 31, "Map name");
                ProjectData.ValidateText(group, 1, 23, "Map group");
                activeMap.Name = name;
                activeMap.Group = group;
                RefreshTree(activeMap.Id);
                RefreshMapHeader();
                UpdateEventView();
                UpdateCommands();
                SetStatus("Map header updated");
            });
        }

        private void SetStartMap()
        {
            project.StartMap = activeMap.Id;
            RefreshTree(activeMap.Id);
            RefreshMapHeader();
            UpdateCommands();
            SetStatus("Game start set to " + activeMap.Name);
        }

        private void AddMap()
        {
            using (var dialog = new NewMapDialog(activeMap.Group))
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    TryFileAction("Add map", delegate
                    {
                        WorldData data;
                        if (dialog.Preset == 0)
                            data = new WorldData(dialog.MapWidth, dialog.MapHeight);
                        else
                            data = GameArt.ReadPreset(new[] { "world", "dungeon", "mountain" }[dialog.Preset - 1]);
                        ProjectData.ValidateText(dialog.MapName, 1, 31, "Map name");
                        ProjectData.ValidateText(dialog.MapGroup, 1, 23, "Map group");
                        var newMap = project.Add(dialog.MapName, dialog.MapGroup, data);
                        RefreshTree(newMap.Id);
                        workspace.SelectedIndex = (int)EditorView.Map;
                    });
        }

        private void DeleteMap()
        {
            if (project.StartMap == activeMap.Id)
                return;

            foreach (var entry in project.Maps)
                if (entry.Id != activeMap.Id && entry.World.Events.Exists(item => item.TargetMap == activeMap.Id))
                {
                    MessageBox.Show(this, entry.Name + " has a connection to this map. Remove the connection first.",
                        "Delete map", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

            if (MessageBox.Show(this, "Delete " + activeMap.Name + " from this project?", "Delete map",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            project.Maps.Remove(activeMap);
            var removedId = activeMap.Id;
            mapHistories.Remove(removedId);
            foreach (var history in mapHistories.Values)
            {
                history.Undo.RemoveAll(data => data.Events.Exists(item => item.TargetMap == removedId));
                history.Redo.RemoveAll(data => data.Events.Exists(item => item.TargetMap == removedId));
            }
            RefreshTree(project.StartMap);
            UpdateCommands();
        }

        private void PaintMovement(Point tile)
        {
            var index = tile.Y * world.Width + tile.X;
            var value = strokeButton == MouseButtons.Right ? (byte)0 : (byte)movementBrush.SelectedIndex;
            if (tile.X == world.SpawnX && tile.Y == world.SpawnY && !WorldData.CanWalk(world.Ground[index], world.Objects[index], value))
            {
                SetStatus("Move the player start before blocking this tile.");
                return;
            }
            var item = ProjectData.FindEvent(world, tile.X, tile.Y);
            if (item != null && (item.Type == MapEventType.Sign || item.Type == MapEventType.Npc) && value == 1)
            {
                SetStatus("Signs and NPCs block movement. Move or delete the event to clear the tile.");
                return;
            }
            world.Movement[index] = value;
            map.RedrawTile(tile);
        }

        private void UpdateEventView()
        {
            if (activeMap == null)
                return;

            eventList.BeginUpdate();
            eventList.Items.Clear();
            foreach (var item in world.Events)
            {
                if (editingView == EditorView.Connections && item.Type != MapEventType.Warp)
                    continue;
                var row = new ListViewItem(item.Type == MapEventType.Npc ? "NPC" : item.Type.ToString());
                row.Tag = item;
                row.SubItems.Add(item.X + ", " + item.Y);
                if (item.Type == MapEventType.Warp)
                {
                    var target = project.Find(item.TargetMap);
                    row.SubItems.Add((target == null ? "(missing map)" : target.Name) + "  ·  " + item.TargetX + ", " + item.TargetY);
                }
                else
                    row.SubItems.Add(item.Text);
                eventList.Items.Add(row);
            }
            eventList.EndUpdate();
        }

        private MapEvent SelectedEvent()
        {
            return eventList.SelectedItems.Count == 0 ? null : (MapEvent)eventList.SelectedItems[0].Tag;
        }

        private void EditSelectedEvent()
        {
            var item = SelectedEvent();
            if (item != null)
                EditMapEvent(new Point(item.X, item.Y), editingView == EditorView.Connections);
        }

        private void EditMapEvent(Point tile, bool connectionsOnly)
        {
            var existing = ProjectData.FindEvent(world, tile.X, tile.Y);
            if (connectionsOnly && existing != null && existing.Type != MapEventType.Warp)
            {
                SetStatus("This tile contains an event. Edit it from the Events tab.");
                return;
            }
            if (tile.X == world.SpawnX && tile.Y == world.SpawnY)
            {
                SetStatus("Move the player start before placing an event here.");
                return;
            }
            if (existing == null && world.Events.Count >= ProjectData.MaximumEvents)
            {
                SetStatus("This map already has 64 events.");
                return;
            }

            map.Capture = false;
            using (var dialog = new MapEventDialog(project, activeMap, tile, existing, connectionsOnly, art))
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    PutEvent(existing, dialog.Result);
        }

        private void PutEvent(MapEvent existing, MapEvent updated)
        {
            var previous = world.Copy();
            if (existing == null)
                world.Events.Add(updated);
            else
                world.Events[world.Events.IndexOf(existing)] = updated;
            Remember(previous);
            UpdateEventView();
            map.Invalidate();
            SetStatus(updated.Type == MapEventType.Warp ? "Connection updated" : "Map event updated");
        }

        private void DeleteSelectedEvent()
        {
            var item = SelectedEvent();
            if (item == null)
                return;
            var previous = world.Copy();
            world.Events.Remove(item);
            Remember(previous);
            UpdateEventView();
            map.Invalidate();
            SetStatus("Map event deleted");
        }

        private void InspectDestination()
        {
            var item = SelectedEvent();
            if (item == null || item.Type != MapEventType.Warp || project.Find(item.TargetMap) == null)
                return;
            RefreshTree(item.TargetMap);
            var pixels = map.Zoom * GameArt.TileSize;
            mapViewport.AutoScrollPosition = new Point(Math.Max(0, item.TargetX * pixels - mapViewport.Width / 2),
                Math.Max(0, item.TargetY * pixels - mapViewport.Height / 2));
            map.HighlightRegion(new Rectangle(item.TargetX, item.TargetY, 1, 1));
            SetStatus("Connection destination at " + item.TargetX + ", " + item.TargetY);
        }

        private void PublishProject()
        {
            EndStroke();
            TryFileAction("Publish project", delegate
            {
                using (var dialog = new PublishWindow(project.WriteBytes(), CurrentArt(), filename))
                    dialog.ShowDialog(this);
            });
        }
    }
}
