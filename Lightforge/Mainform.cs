using System.Diagnostics;
using System.IO.Compression;

namespace Lightforge;

public partial class Mainform : Form
{
    private static readonly string ToolsDir = Path.Combine(
        AppContext.BaseDirectory, "Toolset Binaries");

    private LightforgeProject? _project;
    private TreeView _fileTree = null!;
    private Panel _toolPanel = null!;
    private RichTextBox _outputLog = null!;
    private Label _statusLabel = null!;
    private Panel _welcomePanel = null!;
    private Panel _workspacePanel = null!;
    private readonly List<string> _recentTools = [];
    private ProgressBar? _buildProgress;
    private FileSystemWatcher? _watcher;
    private DateTime _lastRefresh = DateTime.MinValue;
    private Panel _editorPanel = null!;
    private Panel _editorTabBar = null!;
    private readonly Dictionary<string, (Panel tab, RichTextBox editor)> _openEditors = new();
    private ComboBox _versionSelector = null!;
    private WowVersion _selectedVersion = WowVersion.WotLK;

    public Mainform()
    {
        InitializeComponent();
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        KeyPreview = true;
        BuildUI();
        ShowWelcome();

        KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.N) { NewProject(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.O) { OpenProject(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.S && _project != null) { SaveActive(); e.Handled = true; }
            else if (e.KeyCode == Keys.F5 && _project != null) { RefreshFileTree(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.B && _project != null) { BuildProject(); e.Handled = true; }
            else if (e.Control && e.Shift && e.KeyCode == Keys.F && _project != null) { ShowFindInFiles(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.W) { CloseActiveEditorTab(); e.Handled = true; }
        };
    }

    // ═══════════════════════════════════════════════════════
    //  UI Construction
    // ═══════════════════════════════════════════════════════

    private void BuildUI()
    {
        SuspendLayout();

        Text = "Lightforge Engine";
        BackColor = Theme.BgDeep;
        ForeColor = Theme.TextNorm;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(1024, 640);
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1280, 800);
        WindowState = FormWindowState.Maximized;
        Font = Theme.Normal;

        // ── Status bar ──────────────────────────────────────
        var statusBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 26,
            BackColor = Color.FromArgb(0, 102, 170),
            Padding = new Padding(10, 0, 10, 0)
        };
        _statusLabel = new Label
        {
            Text = "Ready",
            Font = Theme.Small,
            ForeColor = Color.White,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _buildProgress = new ProgressBar
        {
            Dock = DockStyle.Right,
            Width = 160,
            Visible = false,
            Style = ProgressBarStyle.Continuous
        };
        statusBar.Controls.Add(_statusLabel);
        statusBar.Controls.Add(_buildProgress);

        var menuBar = MakeMenuBar();
        var toolbar = MakeToolbar();
        var separator = Theme.MakeSeparator();

        _welcomePanel = MakeWelcomePanel();
        _workspacePanel = MakeWorkspace();
        _workspacePanel.Visible = false;

        Controls.Add(_welcomePanel);
        Controls.Add(_workspacePanel);
        Controls.Add(statusBar);
        Controls.Add(menuBar);
        Controls.Add(separator);
        Controls.Add(toolbar);

        ResumeLayout(true);
    }

    private Panel MakeToolbar()
    {
        var bar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = Color.FromArgb(18, 18, 18),
            Padding = new Padding(8, 0, 8, 0)
        };

        var logo = new Label
        {
            Text = "◆ LIGHTFORGE",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Theme.Gold,
            AutoSize = true,
            Location = new Point(10, 9)
        };

        int x = 180;
        var btnNew = MakeToolbarButton("New Project", x); x += 100;
        btnNew.Click += (_, _) => NewProject();

        var btnOpen = MakeToolbarButton("Open", x); x += 68;
        btnOpen.Click += (_, _) => OpenProject();

        var btnBuild = MakeToolbarButton("Build Patch", x); x += 100;
        btnBuild.Click += (_, _) => BuildProject();

        var btnSettings = MakeToolbarButton("Settings", x); x += 80;
        btnSettings.Click += (_, _) => ShowProjectSettings();

        var btnExplorer = MakeToolbarButton("Explorer", x); x += 80;
        btnExplorer.Click += (_, _) =>
        {
            if (_project != null)
                Process.Start("explorer.exe", $"\"{_project.ProjectDir}\"");
        };

        _versionSelector = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = Theme.SectionHead,
            BackColor = Theme.BgInput,
            ForeColor = Theme.Gold,
            Width = 140,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        foreach (var (_, display, _) in WowVersionInfo.Versions)
            _versionSelector.Items.Add(display);
        _versionSelector.SelectedItem = WowVersionInfo.DisplayName(_selectedVersion);
        _versionSelector.SelectedIndexChanged += (_, _) =>
        {
            var display = _versionSelector.SelectedItem?.ToString() ?? "WotLK 3.3.5a";
            _selectedVersion = WowVersionInfo.FromDisplay(display);
            if (_project != null)
            {
                _project.Expansion = display == "All Versions"
                    ? _project.Expansion
                    : display;
                _project.Save();
            }
            PopulateToolPanel();
        };

        var searchBox = new TextBox
        {
            PlaceholderText = "Filter tools...",
            Font = Theme.Normal,
            BackColor = Theme.BgInput,
            ForeColor = Theme.TextBright,
            BorderStyle = BorderStyle.FixedSingle,
            Width = 200,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        searchBox.TextChanged += (_, _) => FilterTools(searchBox.Text);

        bar.Resize += (_, _) =>
        {
            searchBox.Location = new Point(bar.Width - searchBox.Width - 16, 9);
            _versionSelector.Location = new Point(bar.Width - searchBox.Width - _versionSelector.Width - 26, 7);
        };
        searchBox.Location = new Point(bar.Width - searchBox.Width - 16, 9);
        _versionSelector.Location = new Point(bar.Width - searchBox.Width - _versionSelector.Width - 26, 7);

        bar.Controls.AddRange([logo, btnNew, btnOpen, btnBuild, btnSettings, btnExplorer, _versionSelector, searchBox]);
        return bar;
    }

    private static Label MakeToolbarButton(string text, int x)
    {
        var lbl = new Label
        {
            Text = text,
            Font = Theme.SectionHead,
            ForeColor = Theme.TextNorm,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(x, 11),
            Cursor = Cursors.Hand,
            Padding = new Padding(6, 2, 6, 2)
        };
        lbl.MouseEnter += (_, _) => lbl.ForeColor = Theme.TextBright;
        lbl.MouseLeave += (_, _) => lbl.ForeColor = Theme.TextNorm;
        return lbl;
    }

    private MenuStrip MakeMenuBar()
    {
        var menu = new MenuStrip
        {
            BackColor = Color.FromArgb(24, 24, 24),
            ForeColor = Theme.TextNorm,
            Renderer = new DarkMenuRenderer(),
            Padding = new Padding(4, 2, 0, 2)
        };

        var fileMenu = new ToolStripMenuItem("File");
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("New Project", null, (_, _) => NewProject())
            { ShortcutKeyDisplayString = "Ctrl+N" });
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Open Project...", null, (_, _) => OpenProject())
            { ShortcutKeyDisplayString = "Ctrl+O" });

        var recentMenu = new ToolStripMenuItem("Open Recent");
        var recents = RecentProjects.Load();
        if (recents.Count > 0)
        {
            foreach (var entry in recents.Take(8))
            {
                if (!File.Exists(entry.Path)) continue;
                var p = entry.Path;
                recentMenu.DropDownItems.Add(entry.Name, null, (_, _) => LoadProject(p));
            }
            recentMenu.DropDownItems.Add(new ToolStripSeparator());
            recentMenu.DropDownItems.Add("Clear Recent", null, (_, _) =>
            {
                var recentFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lightforge", "recent.json");
                if (File.Exists(recentFile)) File.Delete(recentFile);
            });
        }
        else
        {
            recentMenu.Enabled = false;
        }
        fileMenu.DropDownItems.Add(recentMenu);

        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Close Project", null, (_, _) => CloseProject()));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Project Settings", null, (_, _) => ShowProjectSettings()));
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Export Project as ZIP...", null, (_, _) => ExportProjectZip()));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("Exit", null, (_, _) => Close());

        var toolsMenu = new ToolStripMenuItem("Tools");
        var quickTools = new[] { "Noggit3", "WDBXEditor", "Spell Editor", "MPQ Editor", "WMVx" };
        foreach (var qt in quickTools)
        {
            var tool = WowToolRegistry.GetAllTools().FirstOrDefault(t => t.Name == qt);
            if (tool != null)
            {
                var path = tool.RelativePath;
                toolsMenu.DropDownItems.Add(tool.Name, null, (_, _) => LaunchTool(path));
            }
        }
        toolsMenu.DropDownItems.Add(new ToolStripSeparator());
        toolsMenu.DropDownItems.Add("Validate Project", null, (_, _) => ValidateProject());
        toolsMenu.DropDownItems.Add("Open Tools Folder", null, (_, _) =>
        {
            if (Directory.Exists(ToolsDir))
                Process.Start("explorer.exe", $"\"{ToolsDir}\"");
        });

        var viewMenu = new ToolStripMenuItem("View");
        viewMenu.DropDownItems.Add(new ToolStripMenuItem("Refresh File Tree", null, (_, _) => RefreshFileTree())
            { ShortcutKeyDisplayString = "F5" });
        viewMenu.DropDownItems.Add("Open Project Folder", null, (_, _) =>
        {
            if (_project != null)
                Process.Start("explorer.exe", $"\"{_project.ProjectDir}\"");
        });
        viewMenu.DropDownItems.Add(new ToolStripSeparator());
        viewMenu.DropDownItems.Add("Toggle Output Panel", null, (_, _) => ToggleOutputPanel());
        viewMenu.DropDownItems.Add("Project Statistics", null, (_, _) => ShowProjectStats());
        viewMenu.DropDownItems.Add(new ToolStripMenuItem("Find in Files...", null, (_, _) => ShowFindInFiles())
            { ShortcutKeyDisplayString = "Ctrl+Shift+F" });

        var helpMenu = new ToolStripMenuItem("Help");
        helpMenu.DropDownItems.Add("Tool Sources (SOURCES.md)", null, (_, _) =>
        {
            var sourcesPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "SOURCES.md");
            if (File.Exists(sourcesPath))
                Process.Start(new ProcessStartInfo(sourcesPath) { UseShellExecute = true });
            else
                Log("[Help] SOURCES.md not found");
        });
        helpMenu.DropDownItems.Add("Keyboard Shortcuts", null, (_, _) => ShowKeyboardShortcuts());
        helpMenu.DropDownItems.Add(new ToolStripSeparator());
        helpMenu.DropDownItems.Add("About Lightforge", null, (_, _) => ShowAboutDialog());

        menu.Items.AddRange([fileMenu, toolsMenu, viewMenu, helpMenu]);
        return menu;
    }

    private Panel MakeWelcomePanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.BgDeep
        };

        var center = new Panel
        {
            Size = new Size(500, 460),
            BackColor = Theme.BgDeep
        };
        panel.Resize += (_, _) =>
        {
            center.Location = new Point(
                (panel.Width - center.Width) / 2,
                (panel.Height - center.Height) / 2 - 20);
        };

        int y = 0;

        var titleLbl = new Label
        {
            Text = "◆ LIGHTFORGE ENGINE",
            Font = new Font("Segoe UI", 22f, FontStyle.Bold),
            ForeColor = Theme.Gold,
            AutoSize = true,
            Location = new Point(0, y)
        };
        y += 46;

        var subLbl = new Label
        {
            Text = "World of Warcraft Modding Workspace",
            Font = new Font("Segoe UI", 10f),
            ForeColor = Theme.TextDim,
            AutoSize = true,
            Location = new Point(4, y)
        };
        y += 22;

        var versionLbl = new Label
        {
            Text = "v2.0  |  .NET " + Environment.Version.ToString(2),
            Font = Theme.Small,
            ForeColor = Theme.TextMuted,
            AutoSize = true,
            Location = new Point(4, y)
        };
        y += 32;

        var newBtn = MakeWelcomeButton("Create New Project", "Set up a new modding workspace with organized folders", y, () => NewProject());
        y += 64;

        var openBtn = MakeWelcomeButton("Open Existing Project", "Open a .project file from disk", y, () => OpenProject());
        y += 80;

        var recentLabel = new Label
        {
            Text = "RECENT PROJECTS",
            Font = Theme.SectionHead,
            ForeColor = Theme.TextDim,
            AutoSize = true,
            Location = new Point(4, y)
        };
        y += 24;

        center.Controls.AddRange([titleLbl, subLbl, versionLbl, newBtn, openBtn, recentLabel]);

        var recents = RecentProjects.Load();
        foreach (var entry in recents.Take(5))
        {
            if (!File.Exists(entry.Path)) continue;
            var btn = new Label
            {
                Text = $"  {entry.Name}",
                Font = Theme.Normal,
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(4, y),
                Cursor = Cursors.Hand
            };
            var path = entry.Path;
            btn.Click += (_, _) => LoadProject(path);
            btn.MouseEnter += (_, _) => btn.ForeColor = Theme.AccentHi;
            btn.MouseLeave += (_, _) => btn.ForeColor = Theme.Accent;

            var pathLbl = new Label
            {
                Text = $"     {Path.GetDirectoryName(path)}",
                Font = Theme.Tiny,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(4, y + 18)
            };
            center.Controls.AddRange([btn, pathLbl]);
            y += 38;
        }

        panel.Controls.Add(center);
        return panel;
    }

    private static Panel MakeWelcomeButton(string text, string desc, int y, Action onClick)
    {
        var panel = new Panel
        {
            Location = new Point(0, y),
            Size = new Size(460, 52),
            BackColor = Theme.BgPanel,
            Cursor = Cursors.Hand
        };
        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            using var accentPen = new Pen(Theme.Accent, 3);
            e.Graphics.DrawLine(accentPen, 0, 0, 0, panel.Height);
        };

        var nameLbl = new Label
        {
            Text = text,
            Font = Theme.SectionHead,
            ForeColor = Theme.TextBright,
            Location = new Point(12, 8),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        var descLbl = new Label
        {
            Text = desc,
            Font = Theme.Small,
            ForeColor = Theme.TextDim,
            Location = new Point(12, 28),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        panel.Controls.AddRange([nameLbl, descLbl]);

        void Enter(object? s, EventArgs e) => panel.BackColor = Theme.BgHover;
        void Leave(object? s, EventArgs e) => panel.BackColor = Theme.BgPanel;
        EventHandler click = (_, _) => onClick();

        foreach (Control c in new Control[] { panel, nameLbl, descLbl })
        {
            c.MouseEnter += Enter;
            c.MouseLeave += Leave;
            c.Click += click;
        }

        return panel;
    }

    private Panel MakeWorkspace()
    {
        var workspace = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.BgDeep
        };

        // ── Left: File Browser ──────────────────────────────
        var leftPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 240,
            BackColor = Theme.BgPanel
        };

        var leftHeader = new Label
        {
            Dock = DockStyle.Top,
            Height = 26,
            BackColor = Theme.BgDark,
            Text = "  PROJECT FILES",
            Font = Theme.SectionHead,
            ForeColor = Theme.TextDim,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _fileTree = new TreeView
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.BgPanel,
            ForeColor = Theme.TextNorm,
            Font = Theme.Normal,
            BorderStyle = BorderStyle.None,
            ShowLines = false,
            ShowRootLines = false,
            ItemHeight = 22,
            Indent = 16,
            FullRowSelect = true,
            AllowDrop = true,
            ShowNodeToolTips = true
        };
        _fileTree.NodeMouseDoubleClick += FileTree_DoubleClick;
        _fileTree.DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        };
        _fileTree.DragDrop += (_, e) =>
        {
            if (_project == null) return;
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files) return;

            var targetFolder = _project.ProjectDir;
            var pt = _fileTree.PointToClient(new Point(e.X, e.Y));
            var node = _fileTree.GetNodeAt(pt);
            if (node?.Tag is string tagPath)
            {
                if (Directory.Exists(tagPath))
                    targetFolder = tagPath;
                else
                    targetFolder = Path.GetDirectoryName(tagPath) ?? targetFolder;
            }
            else if (node?.Parent?.Tag is string parentPath && Directory.Exists(parentPath))
            {
                targetFolder = parentPath;
            }

            foreach (var file in files)
            {
                if (!File.Exists(file)) continue;
                var dest = Path.Combine(targetFolder, Path.GetFileName(file));
                File.Copy(file, dest, overwrite: true);
                Log($"[Drop] {Path.GetFileName(file)} → {Path.GetFileName(targetFolder)}/");
            }
            RefreshFileTree();
        };
        _fileTree.NodeMouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                _fileTree.SelectedNode = e.Node;
                ShowFileTreeContextMenu(e.Node, e.Location);
            }
        };

        var refreshBtn = new Label
        {
            Text = "↻",
            Font = Theme.SectionHead,
            ForeColor = Theme.TextDim,
            Size = new Size(28, 30),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand,
            BackColor = Theme.BgDark
        };
        refreshBtn.Click += (_, _) => RefreshFileTree();
        refreshBtn.MouseEnter += (_, _) => refreshBtn.ForeColor = Theme.TextBright;
        refreshBtn.MouseLeave += (_, _) => refreshBtn.ForeColor = Theme.TextDim;

        var collapseBtn = new Label
        {
            Text = "−",
            Font = Theme.SectionHead,
            ForeColor = Theme.TextDim,
            Size = new Size(28, 30),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand,
            BackColor = Theme.BgDark
        };
        collapseBtn.Click += (_, _) =>
        {
            _fileTree.CollapseAll();
            if (_fileTree.Nodes.Count > 0) _fileTree.Nodes[0].Expand();
        };
        collapseBtn.MouseEnter += (_, _) => collapseBtn.ForeColor = Theme.TextBright;
        collapseBtn.MouseLeave += (_, _) => collapseBtn.ForeColor = Theme.TextDim;

        leftHeader.Controls.Add(refreshBtn);
        leftHeader.Controls.Add(collapseBtn);

        leftPanel.Controls.Add(_fileTree);
        leftPanel.Controls.Add(Theme.MakeSeparator());
        leftPanel.Controls.Add(leftHeader);
        leftPanel.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, leftPanel.Width - 1, 0,
                leftPanel.Width - 1, leftPanel.Height);
        };

        // ── Bottom: Output Log ──────────────────────────────
        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 140,
            BackColor = Theme.BgDark
        };

        var bottomHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = Color.FromArgb(24, 24, 24)
        };
        bottomHeader.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, 0, bottomHeader.Width, 0);
            TextRenderer.DrawText(e.Graphics, "OUTPUT",
                Theme.SectionHead, new Point(10, 7), Theme.TextDim);
        };
        var clearLogBtn = new Label
        {
            Text = "Clear",
            Font = Theme.Small,
            ForeColor = Theme.TextMuted,
            Dock = DockStyle.Right,
            Width = 40,
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        clearLogBtn.Click += (_, _) => _outputLog?.Clear();
        clearLogBtn.MouseEnter += (_, _) => clearLogBtn.ForeColor = Theme.TextBright;
        clearLogBtn.MouseLeave += (_, _) => clearLogBtn.ForeColor = Theme.TextMuted;
        bottomHeader.Controls.Add(clearLogBtn);

        _outputLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(20, 20, 20),
            ForeColor = Theme.TextDim,
            Font = Theme.Mono,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            WordWrap = false
        };

        bottomPanel.Controls.Add(_outputLog);
        bottomPanel.Controls.Add(bottomHeader);

        // ── Center: Tool panels ─────────────────────────────
        _toolPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.BgDeep,
            AutoScroll = true,
            Padding = new Padding(12)
        };

        // ── Tab bar for center area ────────────────────────
        var tabBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 30,
            BackColor = Color.FromArgb(24, 24, 24)
        };
        tabBar.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, 29, tabBar.Width, 29);
        };

        var workflowPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.BgDeep,
            AutoScroll = true,
            Visible = false,
            Padding = new Padding(30)
        };
        PopulateWorkflowPanel(workflowPanel);

        _editorPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.BgDeep,
            Visible = false
        };
        _editorTabBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 26,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        var noFileLabel = new Label
        {
            Text = "Double-click a text file (.sql, .lua, .txt, .xml, .cfg, .conf) to edit it here.",
            Font = Theme.Normal,
            ForeColor = Theme.TextMuted,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        _editorPanel.Controls.Add(noFileLabel);
        _editorPanel.Controls.Add(_editorTabBar);

        var tabs = new[] { ("Tools", _toolPanel), ("Workflows", workflowPanel), ("Editor", _editorPanel) };
        var tabLabels = new List<Label>();
        int tx = 8;
        foreach (var (tabName, panel) in tabs)
        {
            var tab = new Label
            {
                Text = tabName,
                Font = Theme.SectionHead,
                ForeColor = tabName == "Tools" ? Theme.TextBright : Theme.TextMuted,
                BackColor = tabName == "Tools" ? Theme.BgDeep : Color.Transparent,
                AutoSize = false,
                Size = new Size(80, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(tx, 1),
                Cursor = Cursors.Hand
            };
            tab.Click += (_, _) =>
            {
                foreach (var (_, p) in tabs) p.Visible = false;
                panel.Visible = true;
                foreach (var lbl in tabLabels)
                {
                    lbl.ForeColor = Theme.TextMuted;
                    lbl.BackColor = Color.Transparent;
                }
                tab.ForeColor = Theme.TextBright;
                tab.BackColor = Theme.BgDeep;
            };
            tab.MouseEnter += (_, _) => { if (tab.ForeColor != Theme.TextBright) tab.ForeColor = Theme.TextNorm; };
            tab.MouseLeave += (_, _) => { if (tab.BackColor == Color.Transparent) tab.ForeColor = Theme.TextMuted; };
            tabBar.Controls.Add(tab);
            tabLabels.Add(tab);
            tx += 84;
        }

        var splitter = new Splitter
        {
            Dock = DockStyle.Left,
            Width = 3,
            BackColor = Theme.Border,
            MinSize = 160,
            MinExtra = 300
        };

        var bottomSplitter = new Splitter
        {
            Dock = DockStyle.Bottom,
            Height = 3,
            BackColor = Theme.Border,
            MinSize = 60,
            MinExtra = 200
        };

        workspace.Controls.Add(_toolPanel);
        workspace.Controls.Add(workflowPanel);
        workspace.Controls.Add(_editorPanel);
        workspace.Controls.Add(bottomSplitter);
        workspace.Controls.Add(bottomPanel);
        workspace.Controls.Add(tabBar);
        workspace.Controls.Add(splitter);
        workspace.Controls.Add(leftPanel);

        return workspace;
    }

    private void PopulateToolPanel()
    {
        _toolPanel.Controls.Clear();
        _toolPanel.SuspendLayout();

        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Theme.BgDeep
        };

        var inner = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            AutoScroll = false,
            BackColor = Theme.BgDeep,
            Padding = new Padding(8, 4, 8, 4),
            Location = Point.Empty,
            MaximumSize = new Size(2000, 0)
        };

        void SyncWidths()
        {
            int w = scroll.ClientSize.Width - 4;
            inner.Width = w;
            int contentW = w - 20;
            foreach (Control c in inner.Controls)
            {
                c.Width = contentW;
                if (c is FlowLayoutPanel fp)
                {
                    int cardsPerRow = Math.Max(1, contentW / 254);
                    int rows = (int)Math.Ceiling(fp.Controls.Count / (double)cardsPerRow);
                    fp.Height = rows * 54 + 4;
                }
            }
        }
        scroll.Resize += (_, _) => SyncWidths();
        scroll.Controls.Add(inner);

        void AddSection(string title, ToolEntry[] tools)
        {
            if (tools.Length == 0) return;

            var cardFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Size = new Size(600, 60),
                Margin = new Padding(0, 0, 0, 4),
                BackColor = Theme.BgDeep
            };
            foreach (var tool in tools)
                cardFlow.Controls.Add(MakeToolCard(tool));

            var header = new Label
            {
                Text = $"▾  {title}     ({tools.Length})",
                Font = Theme.SectionHead,
                ForeColor = Theme.TextDim,
                BackColor = Color.FromArgb(30, 30, 30),
                Size = new Size(600, 28),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Margin = new Padding(0, 6, 0, 2),
                Cursor = Cursors.Hand
            };
            header.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawLine(pen, 0, 27, header.Width, 27);
            };
            header.Click += (_, _) =>
            {
                cardFlow.Visible = !cardFlow.Visible;
                header.Text = (cardFlow.Visible ? "▾" : "▸") +
                    header.Text.Substring(1);
            };
            header.MouseEnter += (_, _) => header.ForeColor = Theme.TextNorm;
            header.MouseLeave += (_, _) => header.ForeColor = Theme.TextDim;
            inner.Controls.Add(header);
            inner.Controls.Add(cardFlow);
        }

        if (_recentTools.Count > 0)
        {
            var recentLabel = new Label
            {
                Text = "RECENTLY USED",
                Font = Theme.SectionHead,
                ForeColor = Theme.Gold,
                BackColor = Color.FromArgb(30, 30, 30),
                Size = new Size(600, 28),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Margin = new Padding(0, 6, 0, 2)
            };
            recentLabel.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Gold, 1);
                e.Graphics.DrawLine(pen, 0, 27, recentLabel.Width, 27);
            };
            inner.Controls.Add(recentLabel);

            var recentFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Size = new Size(600, 60),
                Margin = new Padding(0, 0, 0, 8),
                BackColor = Theme.BgDeep
            };
            foreach (var toolPath in _recentTools)
            {
                var toolName = Path.GetFileNameWithoutExtension(toolPath);
                recentFlow.Controls.Add(MakeToolCard(toolName, "Recently used", toolPath));
            }
            inner.Controls.Add(recentFlow);
        }

        var grouped = WowToolRegistry.GetGroupedTools(_selectedVersion);
        foreach (var category in WowToolRegistry.CategoryOrder)
        {
            var match = grouped.FirstOrDefault(g => g.category == category);
            if (match.tools is { Length: > 0 })
                AddSection(category, match.tools);
        }

        var totalTools = grouped.Sum(g => g.tools.Length);
        var versionLabel = _selectedVersion == WowVersion.All
            ? "All Versions"
            : WowVersionInfo.DisplayName(_selectedVersion);
        Log($"[Tools] Loaded {totalTools} tools for {versionLabel}");

        _toolPanel.Controls.Add(scroll);
        _toolPanel.ResumeLayout();
        if (_toolPanel.IsHandleCreated)
            _toolPanel.BeginInvoke(() => _toolPanel.AutoScrollPosition = Point.Empty);
        else
            _toolPanel.AutoScrollPosition = Point.Empty;
    }

    private void FilterTools(string query)
    {
        if (_toolPanel.Controls.Count == 0) return;
        var scroll = _toolPanel.Controls[0];
        if (scroll is not Panel container) return;

        var isSearch = !string.IsNullOrWhiteSpace(query);
        var q = query.Trim().ToLowerInvariant();

        foreach (Control ctrl in container.Controls)
        {
            if (ctrl is FlowLayoutPanel cardFlow)
            {
                bool anyVisible = false;
                foreach (Control card in cardFlow.Controls)
                {
                    if (!isSearch)
                    {
                        card.Visible = true;
                        anyVisible = true;
                        continue;
                    }

                    var match = false;
                    foreach (Control c in card.Controls)
                    {
                        if (c is Label lbl && lbl.Text.Contains(q, StringComparison.OrdinalIgnoreCase))
                        {
                            match = true;
                            break;
                        }
                    }
                    card.Visible = match;
                    if (match) anyVisible = true;
                }
                cardFlow.Visible = anyVisible;
            }
            else if (ctrl is Label header && isSearch)
            {
                var nextIdx = container.Controls.IndexOf(ctrl) + 1;
                if (nextIdx < container.Controls.Count && container.Controls[nextIdx] is FlowLayoutPanel fp)
                    header.Visible = fp.Visible;
            }
            else
            {
                ctrl.Visible = true;
            }
        }
    }

    private void PopulateWorkflowPanel(Panel panel)
    {
        var workflows = new (string name, string desc, string steps)[]
        {
            ("Full Patch Build",
             "Build MPQ patch from project files, apply to client, and verify",
             "Collect files → Create MPQ → Copy to Data/ → Verify"),
            ("Map Export Pipeline",
             "Export map changes from Noggit, generate minimaps, package",
             "Save ADT/WDT → Generate minimaps → Build MPQ"),
            ("DBC Sync",
             "Sync DBC files with database, export SQL, and apply changes",
             "Export DBCs → Generate SQL → Execute on server"),
            ("Asset Import",
             "Import M2/WMO models with textures, convert BLP, add to patch",
             "Select files → Convert textures → Copy to project → Update listfile"),
            ("Server Deploy",
             "Export all SQL scripts, apply DBC changes, restart worldserver",
             "Collect SQL → Apply to DB → Sync DBCs → Restart"),
            ("Backup Project",
             "Create timestamped archive of all project files",
             "Snapshot files → Compress → Save to Exports/"),
        };

        int y = 0;
        var title = new Label
        {
            Text = "WORKFLOWS",
            Font = Theme.Header,
            ForeColor = Theme.TextDim,
            Location = new Point(0, y),
            AutoSize = true
        };
        panel.Controls.Add(title);
        y += 30;

        var subtitle = new Label
        {
            Text = "Automated multi-step build and deployment pipelines",
            Font = Theme.Small,
            ForeColor = Theme.TextMuted,
            Location = new Point(0, y),
            AutoSize = true
        };
        panel.Controls.Add(subtitle);
        y += 30;

        foreach (var (name, desc, steps) in workflows)
        {
            var card = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(500, 80),
                BackColor = Theme.BgPanel,
                Cursor = Cursors.Hand
            };
            card.Paint += (_, e) =>
            {
                using var accentBrush = new SolidBrush(Theme.Gold);
                e.Graphics.FillRectangle(accentBrush, 0, 0, 4, card.Height);
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            var nameLbl = new Label
            {
                Text = name,
                Font = Theme.SectionHead,
                ForeColor = Theme.TextBright,
                Location = new Point(14, 8),
                AutoSize = true
            };
            var descLbl = new Label
            {
                Text = desc,
                Font = Theme.Small,
                ForeColor = Theme.TextDim,
                Location = new Point(14, 28),
                AutoSize = true
            };
            var stepsLbl = new Label
            {
                Text = steps,
                Font = Theme.Tiny,
                ForeColor = Theme.TextMuted,
                Location = new Point(14, 48),
                AutoSize = true
            };
            var runBtn = new Label
            {
                Text = "Run",
                Font = Theme.SectionHead,
                ForeColor = Theme.Accent,
                Size = new Size(40, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(450, 28),
                Cursor = Cursors.Hand
            };
            var wfName = name;
            runBtn.Click += (_, _) => RunWorkflow(wfName);
            runBtn.MouseEnter += (_, _) => runBtn.ForeColor = Theme.AccentHi;
            runBtn.MouseLeave += (_, _) => runBtn.ForeColor = Theme.Accent;

            card.Controls.AddRange([nameLbl, descLbl, stepsLbl, runBtn]);
            card.MouseEnter += (_, _) => card.BackColor = Theme.BgHover;
            card.MouseLeave += (_, _) => card.BackColor = Theme.BgPanel;
            foreach (Control c in card.Controls)
            {
                c.MouseEnter += (_, _) => card.BackColor = Theme.BgHover;
                c.MouseLeave += (_, _) => card.BackColor = Theme.BgPanel;
            }

            panel.Controls.Add(card);
            y += 90;
        }

        var cards = panel.Controls.OfType<Panel>().ToList();
        panel.Resize += (_, _) =>
        {
            int w = Math.Max(400, panel.ClientSize.Width - 60);
            foreach (var c in cards)
            {
                c.Width = w;
                foreach (Control child in c.Controls)
                {
                    if (child is Label { Text: "Run" })
                        child.Location = new Point(w - 50, 28);
                }
            }
        };
    }

    private void RunWorkflow(string name)
    {
        if (_project == null)
        {
            Log("[Workflow] No project open.");
            return;
        }

        switch (name)
        {
            case "Full Patch Build":
                BuildProject();
                break;

            case "Backup Project":
                RunBackupWorkflow();
                break;

            case "Asset Import":
                var importDir = Path.Combine(_project.ProjectDir, "Models");
                if (!Directory.Exists(importDir)) Directory.CreateDirectory(importDir);
                ImportFiles(importDir);
                break;

            default:
                Log($"[Workflow] {name}: workflow stub — implementation pending.");
                _statusLabel.Text = $"Workflow '{name}' is not yet implemented";
                break;
        }
    }

    private void RunBackupWorkflow()
    {
        if (_project == null) return;

        var sw = Stopwatch.StartNew();
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
        var backupDir = Path.Combine(_project.ProjectDir, "Exports", $"backup_{timestamp}");
        Directory.CreateDirectory(backupDir);

        Log("[Backup] ═══════════════════════════════════════════");
        Log($"[Backup] Creating project backup...");

        int count = 0;
        foreach (var folder in LightforgeProject.Folders)
        {
            if (folder == "Exports") continue;
            var srcDir = Path.Combine(_project.ProjectDir, folder);
            if (!Directory.Exists(srcDir)) continue;

            foreach (var file in Directory.GetFiles(srcDir, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(_project.ProjectDir, file);
                var dest = Path.Combine(backupDir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.Copy(file, dest);
                count++;
            }
        }

        var projFile = _project.ProjectFile;
        if (File.Exists(projFile))
        {
            File.Copy(projFile, Path.Combine(backupDir, "lightforge.project"));
            count++;
        }

        sw.Stop();
        Log($"[Backup] Backed up {count} file(s) to Exports/backup_{timestamp}");
        Log($"[Backup] Completed in {sw.ElapsedMilliseconds}ms");
        Log("[Backup] ═══════════════════════════════════════════");
        _statusLabel.Text = $"Backup complete — {count} files archived  |  {_project.Name}";
        RefreshFileTree();
    }

    private void ExportProjectZip()
    {
        if (_project == null) return;

        using var dlg = new SaveFileDialog
        {
            Title = "Export Project as ZIP",
            Filter = "ZIP Archive|*.zip",
            FileName = $"{_project.Name}_{DateTime.Now:yyyyMMdd}.zip"
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        var sw = Stopwatch.StartNew();
        Log("[Export] ═══════════════════════════════════════════");
        Log($"[Export] Creating archive: {Path.GetFileName(dlg.FileName)}");

        if (File.Exists(dlg.FileName)) File.Delete(dlg.FileName);
        ZipFile.CreateFromDirectory(_project.ProjectDir, dlg.FileName);

        sw.Stop();
        var zipSize = FormatFileSize(new FileInfo(dlg.FileName).Length);
        Log($"[Export] Archive created: {zipSize}");
        Log($"[Export] Completed in {sw.ElapsedMilliseconds}ms");
        Log("[Export] ═══════════════════════════════════════════");
        _statusLabel.Text = $"Exported to {Path.GetFileName(dlg.FileName)} ({zipSize})  |  {_project.Name}";
    }

    private Panel MakeToolCard(ToolEntry tool) => MakeToolCard(tool.Name, tool.Description, tool.RelativePath, tool);
    private Panel MakeToolCard(string name, string desc, string toolPath, ToolEntry? entry = null)
    {
        var card = new Panel
        {
            Width = 248,
            Height = 48,
            Margin = new Padding(3),
            BackColor = Theme.BgSurface,
            Cursor = Cursors.Hand
        };

        var statusColor = entry?.Status switch
        {
            "Active" => Theme.Success,
            "Maintained" or "Stable" or "Complete" => Theme.Accent,
            "Stale" or "Dormant" => Theme.Warning,
            "Archived" or "Dead" => Theme.Error,
            _ => Theme.Accent
        };

        card.Paint += (_, e) =>
        {
            using var pen = new Pen(statusColor, 2);
            e.Graphics.DrawLine(pen, 0, 1, 0, card.Height - 1);
        };

        var nameLbl = new Label
        {
            Text = name,
            Font = Theme.SectionHead,
            ForeColor = Theme.TextBright,
            Location = new Point(8, 5),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        var descLbl = new Label
        {
            Text = desc,
            Font = Theme.Tiny,
            ForeColor = Theme.TextDim,
            Location = new Point(8, 26),
            Size = new Size(232, 16),
            BackColor = Color.Transparent
        };

        card.Controls.AddRange([nameLbl, descLbl]);

        void OnClick(object? s, EventArgs e) => LaunchTool(toolPath);
        void OnEnter(object? s, EventArgs e)
        {
            card.BackColor = Theme.BgHover;
            nameLbl.ForeColor = Color.White;
        }
        void OnLeave(object? s, EventArgs e)
        {
            card.BackColor = Theme.BgSurface;
            nameLbl.ForeColor = Theme.TextBright;
        }
        void OnRightClick(object? s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || entry == null) return;
            ShowToolContextMenu(entry, card.PointToScreen(e.Location));
        }

        foreach (Control c in new Control[] { card, nameLbl, descLbl })
        {
            c.Click += OnClick;
            c.MouseEnter += OnEnter;
            c.MouseLeave += OnLeave;
            c.MouseClick += OnRightClick;
            c.Cursor = Cursors.Hand;
        }

        return card;
    }

    private void ShowToolContextMenu(ToolEntry tool, Point screenPos)
    {
        var menu = new ContextMenuStrip
        {
            BackColor = Theme.BgPanel,
            ForeColor = Theme.TextNorm,
            Renderer = new DarkMenuRenderer()
        };

        menu.Items.Add($"Launch {tool.Name}", null, (_, _) => LaunchTool(tool.RelativePath));
        menu.Items.Add(new ToolStripSeparator());

        if (!string.IsNullOrEmpty(tool.Source))
        {
            var src = tool.Source;
            menu.Items.Add($"Open Source: {tool.Source}", null, (_, _) =>
                Process.Start(new ProcessStartInfo(src) { UseShellExecute = true }));
        }

        var versions = string.Join(", ", tool.Compatible.Select(v => v.ToString()));
        var infoItem = new ToolStripMenuItem($"Compatible: {versions}") { Enabled = false };
        menu.Items.Add(infoItem);

        if (!string.IsNullOrEmpty(tool.Status))
        {
            var statusItem = new ToolStripMenuItem($"Status: {tool.Status}") { Enabled = false };
            menu.Items.Add(statusItem);
        }

        menu.Items.Add(new ToolStripSeparator());
        var fullPath = Path.Combine(ToolsDir, tool.RelativePath);
        var dir = Path.GetDirectoryName(fullPath + ".exe") ?? fullPath;
        if (Directory.Exists(dir))
        {
            menu.Items.Add("Open Tool Folder", null, (_, _) =>
                Process.Start("explorer.exe", $"\"{dir}\""));
        }

        menu.Items.Add("Copy Tool Path", null, (_, _) =>
        {
            Clipboard.SetText(fullPath);
            Log($"[Tool] Path copied: {fullPath}");
        });

        menu.Show(screenPos);
    }

    // ═══════════════════════════════════════════════════════
    //  Project Operations
    // ═══════════════════════════════════════════════════════

    public void OpenFromCommandLine(string path)
    {
        Load += (_, _) => LoadProject(path);
    }

    private void BuildProject()
    {
        if (_project == null) return;

        if (string.IsNullOrEmpty(_project.ClientPath) || !Directory.Exists(_project.ClientPath))
        {
            Log("[Build] ERROR: WoW client path not set. Open Settings to configure.");
            MessageBox.Show(
                "Set your WoW client path in Settings before building.",
                "Build", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var dataDir = Path.Combine(_project.ClientPath, "Data");
        if (!Directory.Exists(dataDir))
        {
            Log($"[Build] ERROR: Data/ folder not found in client: {_project.ClientPath}");
            return;
        }

        var sw = Stopwatch.StartNew();
        Log("[Build] ═══════════════════════════════════════════");
        Log("[Build] Starting patch build...");

        var allFiles = new List<(string src, string relPath, string category)>();

        var patchDir = Path.Combine(_project.ProjectDir, "Patches");
        if (Directory.Exists(patchDir))
            foreach (var f in Directory.GetFiles(patchDir, "*", SearchOption.AllDirectories))
                allFiles.Add((f, Path.GetRelativePath(patchDir, f), "Patch"));

        var exportDir = Path.Combine(_project.ProjectDir, "Exports");
        if (Directory.Exists(exportDir))
            foreach (var f in Directory.GetFiles(exportDir, "*", SearchOption.AllDirectories))
                allFiles.Add((f, Path.GetRelativePath(exportDir, f), "Export"));

        if (allFiles.Count == 0)
        {
            Log("[Build] No files to deploy (Patches/ and Exports/ are empty).");
            Log("[Build] ═══════════════════════════════════════════");
            return;
        }

        Log($"[Build] Deploying {allFiles.Count} file(s)...");
        _statusLabel.Text = $"Building... 0/{allFiles.Count}";
        if (_buildProgress != null)
        {
            _buildProgress.Minimum = 0;
            _buildProgress.Maximum = allFiles.Count;
            _buildProgress.Value = 0;
            _buildProgress.Visible = true;
        }

        int copied = 0;
        foreach (var (src, relPath, category) in allFiles)
        {
            var dest = Path.Combine(dataDir, relPath);
            var destDir = Path.GetDirectoryName(dest)!;
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);
            File.Copy(src, dest, overwrite: true);
            Log($"[Build] [{category}] {relPath} → Data/");
            copied++;
            _statusLabel.Text = $"Building... {copied}/{allFiles.Count}";
            if (_buildProgress != null) _buildProgress.Value = copied;
        }

        var sqlDir = Path.Combine(_project.ProjectDir, "SQL");
        if (Directory.Exists(sqlDir))
        {
            var sqlFiles = Directory.GetFiles(sqlDir, "*.sql", SearchOption.AllDirectories);
            if (sqlFiles.Length > 0)
            {
                Log($"[Build] Found {sqlFiles.Length} SQL script(s):");
                foreach (var sql in sqlFiles)
                    Log($"[Build]   - {Path.GetFileName(sql)}");
                Log("[Build] Apply via HeidiSQL or mysql CLI to your world/characters database.");
            }
        }

        sw.Stop();
        Log($"[Build] Deployed {copied} file(s) in {sw.ElapsedMilliseconds}ms");
        Log("[Build] Build complete.");
        Log("[Build] ═══════════════════════════════════════════");

        if (_buildProgress != null) _buildProgress.Visible = false;
        _statusLabel.Text = $"Build complete — {copied} file(s) deployed in {sw.ElapsedMilliseconds}ms  |  {_project.Name}";
    }

    private void ShowWelcome()
    {
        _welcomePanel.Visible = true;
        _workspacePanel.Visible = false;
        Text = "Lightforge Engine";
        _statusLabel.Text = "No project open";
    }

    private void ShowWorkspace()
    {
        _welcomePanel.Visible = false;
        _workspacePanel.Visible = true;
    }

    private void NewProject()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Choose where to create the project folder",
            UseDescriptionForTitle = true
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        var name = "MyWoWProject";
        var inputDlg = new Form
        {
            Text = "New Project",
            Size = new Size(380, 160),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Theme.BgPanel,
            ForeColor = Theme.TextNorm
        };

        var lbl = new Label { Text = "Project Name:", Location = new Point(12, 18), AutoSize = true };
        var txt = new TextBox
        {
            Text = name,
            Location = new Point(12, 40),
            Width = 340,
            BackColor = Theme.BgInput,
            ForeColor = Theme.TextBright,
            BorderStyle = BorderStyle.FixedSingle
        };
        var ok = new Button
        {
            Text = "Create",
            Location = new Point(270, 78),
            Size = new Size(82, 30),
            BackColor = Theme.Accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DialogResult = DialogResult.OK
        };

        inputDlg.Controls.AddRange([lbl, txt, ok]);
        inputDlg.AcceptButton = ok;

        if (inputDlg.ShowDialog() != DialogResult.OK) return;
        name = txt.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        _project = LightforgeProject.Create(dlg.SelectedPath, name);
        RecentProjects.Add(_project.ProjectFile, _project.Name);
        OpenProjectWorkspace();
    }

    private void OpenProject()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Lightforge Project|lightforge.project|All Files|*.*",
            Title = "Open Lightforge Project"
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;
        LoadProject(dlg.FileName);
    }

    private void LoadProject(string path)
    {
        var project = LightforgeProject.Open(path);
        if (project == null)
        {
            MessageBox.Show("Could not open project file.", "Lightforge",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _project = project;
        RecentProjects.Add(path, project.Name);
        OpenProjectWorkspace();
    }

    private void OpenProjectWorkspace()
    {
        if (_project == null) return;

        Text = $"Lightforge Engine - {_project.Name}";

        _selectedVersion = WowVersionInfo.FromDisplay(_project.Expansion);
        if (_selectedVersion == WowVersion.All) _selectedVersion = WowVersion.WotLK;
        _versionSelector.SelectedItem = WowVersionInfo.DisplayName(_selectedVersion);

        RefreshFileTree();
        PopulateToolPanel();
        ShowWorkspace();

        var totalFiles = 0;
        long totalBytes = 0;
        foreach (var folder in LightforgeProject.Folders)
        {
            var p = Path.Combine(_project.ProjectDir, folder);
            if (!Directory.Exists(p)) continue;
            foreach (var f in Directory.GetFiles(p, "*", SearchOption.AllDirectories))
            {
                totalFiles++;
                totalBytes += new FileInfo(f).Length;
            }
        }
        _statusLabel.Text = $"Project: {_project.Name}  |  {totalFiles} files ({FormatFileSize(totalBytes)})  |  {_project.Expansion}  |  {_project.ProjectDir}";

        Log($"[Project] Opened: {_project.Name}");
        Log($"[Project] Directory: {_project.ProjectDir}");
        Log($"[Project] Expansion: {_project.Expansion}");

        foreach (var folder in LightforgeProject.Folders)
        {
            var dir = Path.Combine(_project.ProjectDir, folder);
            var count = Directory.Exists(dir)
                ? Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length
                : 0;
            if (count > 0) Log($"[Project] {folder}/: {count} file(s)");
        }

        StartFileWatcher();
    }

    private void StartFileWatcher()
    {
        _watcher?.Dispose();
        if (_project == null) return;

        _watcher = new FileSystemWatcher(_project.ProjectDir)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        void OnChanged(object s, FileSystemEventArgs e)
        {
            if ((DateTime.Now - _lastRefresh).TotalMilliseconds < 500) return;
            _lastRefresh = DateTime.Now;
            if (IsHandleCreated)
                BeginInvoke(RefreshFileTree);
        }

        _watcher.Created += OnChanged;
        _watcher.Deleted += OnChanged;
        _watcher.Renamed += (s, e) => OnChanged(s, e);
    }

    private void ShowProjectSettings()
    {
        if (_project == null) return;

        var dlg = new Form
        {
            Text = "Project Settings",
            Size = new Size(500, 320),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Theme.BgPanel,
            ForeColor = Theme.TextNorm
        };

        int y = 16;

        var nameLbl = new Label { Text = "Project Name:", Location = new Point(16, y), AutoSize = true };
        y += 22;
        var nameTxt = new TextBox
        {
            Text = _project.Name,
            Location = new Point(16, y),
            Width = 450,
            BackColor = Theme.BgInput,
            ForeColor = Theme.TextBright,
            BorderStyle = BorderStyle.FixedSingle
        };
        y += 36;

        var clientLbl = new Label { Text = "WoW Client Path:", Location = new Point(16, y), AutoSize = true };
        y += 22;
        var clientTxt = new TextBox
        {
            Text = _project.ClientPath,
            Location = new Point(16, y),
            Width = 400,
            BackColor = Theme.BgInput,
            ForeColor = Theme.TextBright,
            BorderStyle = BorderStyle.FixedSingle
        };
        var browseBtn = new Button
        {
            Text = "...",
            Location = new Point(420, y - 1),
            Size = new Size(46, 24),
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.BgSurface,
            ForeColor = Theme.TextNorm
        };
        browseBtn.Click += (_, _) =>
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Select WoW client folder (contains Data/)",
                UseDescriptionForTitle = true
            };
            if (fbd.ShowDialog() == DialogResult.OK)
                clientTxt.Text = fbd.SelectedPath;
        };
        y += 36;

        var expLbl = new Label { Text = "Expansion:", Location = new Point(16, y), AutoSize = true };
        y += 22;
        var expCombo = new ComboBox
        {
            Location = new Point(16, y),
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Theme.BgInput,
            ForeColor = Theme.TextBright
        };
        foreach (var (v, display, _) in WowVersionInfo.Versions)
        {
            if (v != WowVersion.All)
                expCombo.Items.Add(display);
        }
        expCombo.SelectedItem = _project.Expansion;
        y += 40;

        var saveBtn = new Button
        {
            Text = "Save",
            Location = new Point(380, y),
            Size = new Size(86, 32),
            BackColor = Theme.Accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DialogResult = DialogResult.OK
        };

        dlg.Controls.AddRange([nameLbl, nameTxt, clientLbl, clientTxt, browseBtn,
            expLbl, expCombo, saveBtn]);
        dlg.AcceptButton = saveBtn;

        if (dlg.ShowDialog() != DialogResult.OK) return;

        _project.Name = nameTxt.Text.Trim();
        _project.ClientPath = clientTxt.Text.Trim();
        _project.Expansion = expCombo.SelectedItem?.ToString() ?? "WotLK 3.3.5a";
        _project.Save();

        _selectedVersion = WowVersionInfo.FromDisplay(_project.Expansion);
        if (_selectedVersion == WowVersion.All) _selectedVersion = WowVersion.WotLK;
        _versionSelector.SelectedItem = WowVersionInfo.DisplayName(_selectedVersion);

        Text = $"Lightforge Engine - {_project.Name}";
        _statusLabel.Text = $"Project: {_project.Name}  |  {_project.ProjectDir}";
        RefreshFileTree();
        PopulateToolPanel();
        Log($"[Settings] Project settings updated");
    }

    private void RefreshFileTree()
    {
        if (_project == null) return;

        _fileTree.BeginUpdate();
        _fileTree.Nodes.Clear();
        var root = _fileTree.Nodes.Add(_project.Name);
        root.ForeColor = Theme.Gold;

        foreach (var folder in LightforgeProject.Folders)
        {
            var dirPath = Path.Combine(_project.ProjectDir, folder);
            int fileCount = Directory.Exists(dirPath)
                ? Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories).Length : 0;
            var suffix = fileCount > 0 ? $"  ({fileCount})" : "";
            var node = root.Nodes.Add(folder + suffix);
            node.ForeColor = Theme.Accent;
            node.Tag = dirPath;

            if (Directory.Exists(dirPath))
                PopulateTreeNode(node, dirPath);

            if (fileCount > 0 && fileCount <= 10)
                node.Expand();
        }

        root.Expand();
        _fileTree.EndUpdate();
        if (_fileTree.IsHandleCreated)
            _fileTree.BeginInvoke(() => { _fileTree.TopNode = root; });
        else
            _fileTree.TopNode = root;
    }

    private static void PopulateTreeNode(TreeNode parent, string dirPath)
    {
        foreach (var subDir in Directory.GetDirectories(dirPath))
        {
            var dirNode = parent.Nodes.Add(Path.GetFileName(subDir));
            dirNode.ForeColor = Theme.TextDim;
            dirNode.Tag = subDir;
            PopulateTreeNode(dirNode, subDir);
        }

        foreach (var file in Directory.GetFiles(dirPath))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            var info = new FileInfo(file);
            var sizeStr = FormatFileSize(info.Length);
            var fileNode = parent.Nodes.Add($"{Path.GetFileName(file)}  ({sizeStr})");
            fileNode.Tag = file;
            fileNode.ToolTipText = file;
            fileNode.ForeColor = ext switch
            {
                ".dbc" => Theme.Warning,
                ".mpq" => Theme.Gold,
                ".m2" or ".wmo" or ".skin" => Color.FromArgb(160, 120, 220),
                ".blp" or ".png" or ".tga" or ".bmp" => Color.FromArgb(80, 180, 180),
                ".sql" => Color.FromArgb(120, 200, 120),
                ".lua" => Color.FromArgb(100, 160, 240),
                ".adt" or ".wdt" or ".wdl" => Color.FromArgb(200, 140, 80),
                _ => Theme.TextNorm
            };
        }
    }

    private static readonly HashSet<string> EditableExtensions = [".sql", ".lua", ".txt", ".xml", ".cfg", ".conf", ".ini", ".csv", ".md", ".json", ".yaml", ".yml"];

    private void FileTree_DoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Node.Tag is not string filePath || !File.Exists(filePath)) return;

        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (EditableExtensions.Contains(ext))
        {
            OpenFileInEditor(filePath);
            Log($"[Edit] {Path.GetFileName(filePath)} (built-in editor)");
            return;
        }

        var toolPath = ext switch
        {
            ".dbc" => @"WDBXEditor\WDBXEditor",
            ".mpq" => @"MPQEditor\MPQEditor",
            ".m2" or ".wmo" => @"WMVx\WMVx",
            ".blp" => @"BLPLab\BLP Lab",
            _ => null
        };

        if (toolPath != null)
        {
            LaunchTool(toolPath);
            Log($"[Open] {Path.GetFileName(filePath)} → {Path.GetFileNameWithoutExtension(toolPath)}");
        }
        else
        {
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            Log($"[Open] {Path.GetFileName(filePath)} (system default)");
        }
    }

    private void OpenFileInEditor(string filePath)
    {
        if (_openEditors.ContainsKey(filePath))
        {
            ActivateEditorTab(filePath);
            return;
        }

        var content = File.ReadAllText(filePath);
        var editor = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Theme.TextBright,
            Font = Theme.Mono,
            BorderStyle = BorderStyle.None,
            WordWrap = false,
            AcceptsTab = true,
            Text = content,
            Visible = false
        };
        ApplySyntaxHighlighting(editor, Path.GetExtension(filePath).ToLowerInvariant());

        var tabLabel = new Panel
        {
            Height = 24,
            Width = 140,
            BackColor = Theme.BgDeep,
            Cursor = Cursors.Hand
        };
        var nameLabel = new Label
        {
            Text = Path.GetFileName(filePath),
            Font = Theme.Small,
            ForeColor = Theme.TextBright,
            Location = new Point(6, 4),
            AutoSize = true,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        var closeBtn = new Label
        {
            Text = "x",
            Font = Theme.Small,
            ForeColor = Theme.TextMuted,
            Size = new Size(18, 24),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand
        };
        closeBtn.MouseEnter += (_, _) => closeBtn.ForeColor = Theme.Error;
        closeBtn.MouseLeave += (_, _) => closeBtn.ForeColor = Theme.TextMuted;

        var path = filePath;
        closeBtn.Click += (_, _) => CloseEditorTab(path);
        nameLabel.Click += (_, _) => ActivateEditorTab(path);
        tabLabel.Click += (_, _) => ActivateEditorTab(path);

        tabLabel.Controls.Add(nameLabel);
        tabLabel.Controls.Add(closeBtn);
        tabLabel.Left = _editorTabBar.Controls.Count * 144;
        _editorTabBar.Controls.Add(tabLabel);

        _editorPanel.Controls.Add(editor);
        _openEditors[filePath] = (tabLabel, editor);

        editor.KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                File.WriteAllText(path, editor.Text);
                nameLabel.Text = Path.GetFileName(path);
                Log($"[Save] {Path.GetFileName(path)}");
                _statusLabel.Text = $"Saved: {Path.GetFileName(path)}";
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.G)
            {
                var lineStr = ShowInputDialog("Go to Line", $"Line number (1-{editor.Lines.Length}):", "1");
                if (int.TryParse(lineStr, out int lineNum) && lineNum >= 1 && lineNum <= editor.Lines.Length)
                {
                    int charIndex = editor.GetFirstCharIndexFromLine(lineNum - 1);
                    editor.SelectionStart = charIndex;
                    editor.SelectionLength = 0;
                    editor.ScrollToCaret();
                    editor.Focus();
                }
                e.Handled = true;
            }
        };
        editor.TextChanged += (_, _) =>
        {
            if (!nameLabel.Text.StartsWith("*"))
                nameLabel.Text = "*" + nameLabel.Text;
        };
        editor.SelectionChanged += (_, _) =>
        {
            int line = editor.GetLineFromCharIndex(editor.SelectionStart) + 1;
            int col = editor.SelectionStart - editor.GetFirstCharIndexOfCurrentLine() + 1;
            _statusLabel.Text = $"Ln {line}, Col {col}  |  {Path.GetFileName(path)}";
        };

        ActivateEditorTab(filePath);
        SwitchToTab(_editorPanel);
    }

    private void SwitchToTab(Panel targetPanel)
    {
        _toolPanel.Visible = false;
        _editorPanel.Visible = false;
        foreach (Control c in _toolPanel.Parent!.Controls)
        {
            if (c is Panel { Dock: DockStyle.Fill } p && p != _toolPanel && p != _editorPanel)
                p.Visible = false;
        }
        targetPanel.Visible = true;

        var tabBar = _toolPanel.Parent.Controls.OfType<Panel>()
            .FirstOrDefault(p => p.Dock == DockStyle.Top && p.Height == 30);
        if (tabBar == null) return;
        foreach (Control child in tabBar.Controls)
        {
            if (child is not Label lbl) continue;
            bool active = (lbl.Text == "Tools" && targetPanel == _toolPanel) ||
                          (lbl.Text == "Editor" && targetPanel == _editorPanel) ||
                          (lbl.Text == "Workflows" && targetPanel != _toolPanel && targetPanel != _editorPanel);
            lbl.ForeColor = active ? Theme.TextBright : Theme.TextMuted;
            lbl.BackColor = active ? Theme.BgDeep : Color.Transparent;
        }
    }

    private void ActivateEditorTab(string filePath)
    {
        foreach (var (_, (tab, ed)) in _openEditors)
        {
            ed.Visible = false;
            tab.BackColor = Color.FromArgb(30, 30, 30);
        }
        if (_openEditors.TryGetValue(filePath, out var active))
        {
            active.editor.Visible = true;
            active.editor.BringToFront();
            active.tab.BackColor = Theme.BgDeep;
        }
    }

    private void CloseEditorTab(string filePath)
    {
        if (!_openEditors.TryGetValue(filePath, out var entry)) return;

        var (tab, editor) = entry;
        var nameLabel = tab.Controls.OfType<Label>().FirstOrDefault(l => l.Dock != DockStyle.Right);
        if (nameLabel?.Text.StartsWith("*") == true)
        {
            var result = MessageBox.Show($"Save changes to {Path.GetFileName(filePath)}?",
                "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Cancel) return;
            if (result == DialogResult.Yes)
                File.WriteAllText(filePath, editor.Text);
        }

        _editorPanel.Controls.Remove(editor);
        _editorTabBar.Controls.Remove(tab);
        editor.Dispose();
        tab.Dispose();
        _openEditors.Remove(filePath);

        int x = 0;
        foreach (Control c in _editorTabBar.Controls)
        {
            c.Left = x;
            x += 144;
        }

        if (_openEditors.Count > 0)
            ActivateEditorTab(_openEditors.Keys.Last());
    }

    private void ShowFileTreeContextMenu(TreeNode node, Point location)
    {
        var menu = new ContextMenuStrip();
        menu.BackColor = Theme.BgPanel;
        menu.ForeColor = Theme.TextNorm;
        menu.Renderer = new DarkMenuRenderer();

        if (node.Tag is string filePath && File.Exists(filePath))
        {
            menu.Items.Add("Open", null, (_, _) => FileTree_DoubleClick(null,
                new TreeNodeMouseClickEventArgs(node, MouseButtons.Left, 1,
                    location.X, location.Y)));
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (EditableExtensions.Contains(ext))
            {
                menu.Items.Add("Edit in Lightforge", null, (_, _) => OpenFileInEditor(filePath));
            }
            menu.Items.Add("Open with Default App", null, (_, _) =>
            {
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                Log($"[Open] {Path.GetFileName(filePath)} (external)");
            });
            menu.Items.Add("Open in Explorer", null, (_, _) =>
            {
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            });
            menu.Items.Add("Copy Path", null, (_, _) =>
            {
                Clipboard.SetText(filePath);
                Log($"[File] Path copied: {filePath}");
            });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Rename...", null, (_, _) =>
            {
                var fileName = Path.GetFileName(filePath);
                var newName = ShowInputDialog("Rename", "Enter new name:", fileName);
                if (newName != null && newName != fileName)
                {
                    var newPath = Path.Combine(Path.GetDirectoryName(filePath)!, newName);
                    File.Move(filePath, newPath);
                    Log($"[File] Renamed: {fileName} → {newName}");
                    RefreshFileTree();
                }
            });
            menu.Items.Add("Duplicate", null, (_, _) =>
            {
                var dir = Path.GetDirectoryName(filePath)!;
                var nameOnly = Path.GetFileNameWithoutExtension(filePath);
                var ext = Path.GetExtension(filePath);
                var copyPath = Path.Combine(dir, $"{nameOnly}_copy{ext}");
                int i = 2;
                while (File.Exists(copyPath))
                    copyPath = Path.Combine(dir, $"{nameOnly}_copy{i++}{ext}");
                File.Copy(filePath, copyPath);
                Log($"[File] Duplicated: {Path.GetFileName(copyPath)}");
                RefreshFileTree();
            });
            menu.Items.Add("Delete", null, (_, _) =>
            {
                if (MessageBox.Show($"Delete {Path.GetFileName(filePath)}?",
                    "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    File.Delete(filePath);
                    Log($"[File] Deleted: {Path.GetFileName(filePath)}");
                    RefreshFileTree();
                }
            });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Properties", null, (_, _) => ShowFileProperties(filePath));
        }
        else if (node.Tag is string dirPath && Directory.Exists(dirPath))
        {
            menu.Items.Add("Open Folder", null, (_, _) =>
                Process.Start("explorer.exe", $"\"{dirPath}\""));
            menu.Items.Add("Import Files...", null, (_, _) => ImportFiles(dirPath));
            menu.Items.Add("New File...", null, (_, _) =>
            {
                var fileName = ShowInputDialog("New File", "File name (with extension):", "");
                if (!string.IsNullOrEmpty(fileName))
                {
                    var newFile = Path.Combine(dirPath, fileName);
                    File.WriteAllText(newFile, "");
                    Log($"[File] Created: {fileName}");
                    RefreshFileTree();
                }
            });
            menu.Items.Add("New Subfolder...", null, (_, _) =>
            {
                var folderName = ShowInputDialog("New Folder", "Folder name:", "");
                if (!string.IsNullOrEmpty(folderName))
                {
                    var newDir = Path.Combine(dirPath, folderName);
                    Directory.CreateDirectory(newDir);
                    Log($"[File] Created folder: {folderName}");
                    RefreshFileTree();
                }
            });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Copy Path", null, (_, _) =>
            {
                Clipboard.SetText(dirPath);
                Log($"[File] Path copied: {dirPath}");
            });
        }

        if (menu.Items.Count > 0)
            menu.Show(_fileTree, location);
    }

    private void ImportFiles(string targetFolder)
    {
        using var dlg = new OpenFileDialog
        {
            Title = $"Import to {Path.GetFileName(targetFolder)}",
            Multiselect = true
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        foreach (var file in dlg.FileNames)
        {
            var dest = Path.Combine(targetFolder, Path.GetFileName(file));
            File.Copy(file, dest, overwrite: true);
            Log($"[Import] {Path.GetFileName(file)} → {Path.GetFileName(targetFolder)}/");
        }
        RefreshFileTree();
    }

    private void ShowProjectStats()
    {
        if (_project == null) return;

        using var dlg = new Form
        {
            Text = "Project Statistics",
            ClientSize = new Size(420, 380),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Theme.BgDark,
            ForeColor = Theme.TextNorm
        };

        var titleLbl = new Label
        {
            Text = _project.Name,
            Font = Theme.Header,
            ForeColor = Theme.Gold,
            Location = new Point(16, 14),
            AutoSize = true
        };
        dlg.Controls.Add(titleLbl);

        var listView = new ListView
        {
            Location = new Point(16, 44),
            Size = new Size(388, 240),
            View = System.Windows.Forms.View.Details,
            FullRowSelect = true,
            BackColor = Theme.BgPanel,
            ForeColor = Theme.TextNorm,
            Font = Theme.Normal,
            BorderStyle = BorderStyle.FixedSingle,
            GridLines = true
        };
        listView.Columns.Add("Folder", 120);
        listView.Columns.Add("Files", 60, HorizontalAlignment.Right);
        listView.Columns.Add("Size", 80, HorizontalAlignment.Right);
        listView.Columns.Add("Types", 120);

        int grandTotal = 0;
        long grandBytes = 0;
        foreach (var folder in LightforgeProject.Folders)
        {
            var dirPath = Path.Combine(_project.ProjectDir, folder);
            if (!Directory.Exists(dirPath)) continue;

            var files = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);
            long totalSize = 0;
            var extensions = new HashSet<string>();
            foreach (var f in files)
            {
                totalSize += new FileInfo(f).Length;
                var ext = Path.GetExtension(f).ToLowerInvariant();
                if (!string.IsNullOrEmpty(ext)) extensions.Add(ext);
            }

            var item = new ListViewItem(folder);
            item.SubItems.Add(files.Length.ToString());
            item.SubItems.Add(FormatFileSize(totalSize));
            item.SubItems.Add(string.Join(", ", extensions.OrderBy(e => e)));
            listView.Items.Add(item);

            grandTotal += files.Length;
            grandBytes += totalSize;
        }

        var totalItem = new ListViewItem("TOTAL") { ForeColor = Theme.Gold, Font = Theme.SectionHead };
        totalItem.SubItems.Add(grandTotal.ToString());
        totalItem.SubItems.Add(FormatFileSize(grandBytes));
        totalItem.SubItems.Add("");
        listView.Items.Add(totalItem);

        dlg.Controls.Add(listView);

        var infoLbl = new Label
        {
            Text = $"Expansion: {_project.Expansion}\n" +
                   $"Created: {_project.Created:yyyy-MM-dd HH:mm}\n" +
                   $"Last Opened: {_project.LastOpened:yyyy-MM-dd HH:mm}",
            Font = Theme.Small,
            ForeColor = Theme.TextDim,
            Location = new Point(16, 295),
            AutoSize = true
        };
        dlg.Controls.Add(infoLbl);

        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(320, 340),
            Size = new Size(80, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Accent,
            ForeColor = Color.White
        };
        dlg.Controls.Add(ok);
        dlg.AcceptButton = ok;
        dlg.ShowDialog();
    }

    private void CloseProject()
    {
        if (_project == null) return;
        _watcher?.Dispose();
        _watcher = null;

        foreach (var path in _openEditors.Keys.ToList())
            CloseEditorTab(path);

        _project.Save();
        _project = null;
        _outputLog?.Clear();
        _recentTools.Clear();
        ShowWelcome();
    }

    private void ToggleOutputPanel()
    {
        foreach (Control c in _workspacePanel.Controls)
        {
            if (c is Panel p && p.Dock == DockStyle.Bottom && p.Height > 5)
            {
                p.Visible = !p.Visible;
                break;
            }
        }
    }

    private void Log(string message)
    {
        if (_outputLog == null) return;

        var color = Theme.TextDim;
        if (message.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
            color = Theme.Error;
        else if (message.Contains("[Build]"))
            color = Theme.Accent;
        else if (message.Contains("[Import]") || message.Contains("[Drop]"))
            color = Theme.Success;
        else if (message.Contains("[Open]"))
            color = Color.FromArgb(160, 120, 220);
        else if (message.Contains("[Settings]") || message.Contains("[Save]"))
            color = Theme.Warning;

        var timestamp = $"[{DateTime.Now:HH:mm:ss}] ";
        _outputLog.SelectionStart = _outputLog.TextLength;
        _outputLog.SelectionColor = Theme.TextMuted;
        _outputLog.AppendText(timestamp);
        _outputLog.SelectionStart = _outputLog.TextLength;
        _outputLog.SelectionColor = color;
        _outputLog.AppendText($"{message}\n");
        _outputLog.ScrollToCaret();
    }

    // ═══════════════════════════════════════════════════════
    //  Syntax Highlighting
    // ═══════════════════════════════════════════════════════

    private static readonly string[] SqlKeywords =
    [
        "SELECT", "INSERT", "UPDATE", "DELETE", "CREATE", "ALTER", "DROP", "FROM",
        "WHERE", "AND", "OR", "NOT", "IN", "INTO", "VALUES", "SET", "JOIN", "LEFT",
        "RIGHT", "INNER", "OUTER", "ON", "AS", "TABLE", "INDEX", "VIEW", "IF",
        "EXISTS", "REPLACE", "NULL", "DEFAULT", "PRIMARY", "KEY", "AUTO_INCREMENT",
        "INT", "VARCHAR", "TEXT", "FLOAT", "DOUBLE", "TINYINT", "SMALLINT", "BIGINT",
        "ENUM", "LIKE", "BETWEEN", "ORDER", "BY", "GROUP", "HAVING", "LIMIT",
        "UNION", "BEGIN", "END", "COMMIT", "ROLLBACK", "GRANT", "REVOKE",
        "DATABASE", "USE", "SHOW", "DESCRIBE", "TRUNCATE", "ENGINE", "CHARSET",
        "UNSIGNED", "NOT", "UNIQUE", "CONSTRAINT", "FOREIGN", "REFERENCES", "CASCADE"
    ];

    private static readonly string[] LuaKeywords =
    [
        "and", "break", "do", "else", "elseif", "end", "false", "for", "function",
        "if", "in", "local", "nil", "not", "or", "repeat", "return", "then",
        "true", "until", "while"
    ];

    private static void ApplySyntaxHighlighting(RichTextBox editor, string ext)
    {
        if (ext is not ".sql" and not ".lua") return;

        var keywords = ext == ".sql" ? SqlKeywords : LuaKeywords;
        var keywordColor = ext == ".sql" ? Color.FromArgb(86, 156, 214) : Color.FromArgb(197, 134, 192);
        var stringColor = Color.FromArgb(206, 145, 120);
        var commentColor = Color.FromArgb(106, 153, 85);
        var numberColor = Color.FromArgb(181, 206, 168);

        editor.SuspendLayout();
        var pos = editor.SelectionStart;
        var text = editor.Text;

        var keywordSet = new HashSet<string>(keywords, StringComparer.OrdinalIgnoreCase);
        var wordStart = -1;

        for (int i = 0; i <= text.Length; i++)
        {
            bool isWordChar = i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_');

            if (isWordChar && wordStart < 0)
            {
                wordStart = i;
            }
            else if (!isWordChar && wordStart >= 0)
            {
                var word = text.Substring(wordStart, i - wordStart);
                if (keywordSet.Contains(word))
                {
                    editor.SelectionStart = wordStart;
                    editor.SelectionLength = word.Length;
                    editor.SelectionColor = keywordColor;
                }
                else if (word.All(char.IsDigit))
                {
                    editor.SelectionStart = wordStart;
                    editor.SelectionLength = word.Length;
                    editor.SelectionColor = numberColor;
                }
                wordStart = -1;
            }

            if (i >= text.Length) break;

            if (text[i] == '-' && i + 1 < text.Length && text[i + 1] == '-')
            {
                var end = text.IndexOf('\n', i);
                if (end < 0) end = text.Length;
                editor.SelectionStart = i;
                editor.SelectionLength = end - i;
                editor.SelectionColor = commentColor;
                i = end;
                wordStart = -1;
            }
            else if (text[i] == '#' && ext == ".sql")
            {
                var end = text.IndexOf('\n', i);
                if (end < 0) end = text.Length;
                editor.SelectionStart = i;
                editor.SelectionLength = end - i;
                editor.SelectionColor = commentColor;
                i = end;
                wordStart = -1;
            }
            else if (text[i] == '\'' || text[i] == '"')
            {
                var quote = text[i];
                var end = text.IndexOf(quote, i + 1);
                if (end < 0) end = text.Length - 1;
                editor.SelectionStart = i;
                editor.SelectionLength = end - i + 1;
                editor.SelectionColor = stringColor;
                i = end;
                wordStart = -1;
            }
        }

        editor.SelectionStart = pos;
        editor.SelectionLength = 0;
        editor.SelectionColor = Theme.TextBright;
        editor.ResumeLayout();
    }

    // ═══════════════════════════════════════════════════════
    //  Tool Launching
    // ═══════════════════════════════════════════════════════

    private void LaunchTool(string relativePath)
    {
        var fullPath = Path.Combine(ToolsDir, relativePath);

        if (fullPath.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
            fullPath.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
        {
            if (!File.Exists(fullPath))
            {
                ShowMissing(fullPath);
                return;
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = fullPath,
                WorkingDirectory = Path.GetDirectoryName(fullPath)!,
                UseShellExecute = true
            });
        }
        else
        {
            var exePath = fullPath + ".exe";
            if (!File.Exists(exePath))
            {
                ShowMissing(exePath);
                return;
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath)!,
                UseShellExecute = true
            });
        }

        _recentTools.Remove(relativePath);
        _recentTools.Insert(0, relativePath);
        if (_recentTools.Count > 8) _recentTools.RemoveRange(8, _recentTools.Count - 8);
        Log($"[Launch] {Path.GetFileName(relativePath)}");
    }

    private static string? ShowInputDialog(string title, string prompt, string defaultValue)
    {
        using var dlg = new Form
        {
            Text = title,
            ClientSize = new Size(350, 110),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Theme.BgPanel,
            ForeColor = Theme.TextNorm
        };

        var lbl = new Label { Text = prompt, Location = new Point(12, 12), AutoSize = true };
        var txt = new TextBox
        {
            Text = defaultValue,
            Location = new Point(12, 35),
            Width = 322,
            BackColor = Theme.BgInput,
            ForeColor = Theme.TextBright,
            Font = Theme.Normal
        };
        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(174, 72),
            Size = new Size(75, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Accent,
            ForeColor = Color.White
        };
        var cancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(259, 72),
            Size = new Size(75, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.BgSurface,
            ForeColor = Theme.TextNorm
        };

        dlg.Controls.AddRange([lbl, txt, ok, cancel]);
        dlg.AcceptButton = ok;
        dlg.CancelButton = cancel;
        txt.SelectAll();

        return dlg.ShowDialog() == DialogResult.OK ? txt.Text.Trim() : null;
    }

    private void CloseActiveEditorTab()
    {
        var active = _openEditors.FirstOrDefault(kv => kv.Value.editor.Visible);
        if (active.Key != null)
            CloseEditorTab(active.Key);
    }

    private void SaveActive()
    {
        var activeEditor = _openEditors.FirstOrDefault(kv => kv.Value.editor.Visible);
        if (activeEditor.Key != null)
        {
            File.WriteAllText(activeEditor.Key, activeEditor.Value.editor.Text);
            var tab = activeEditor.Value.tab;
            var nameLabel = tab.Controls.OfType<Label>().FirstOrDefault(l => l.Dock != DockStyle.Right);
            if (nameLabel != null)
                nameLabel.Text = Path.GetFileName(activeEditor.Key);
            Log($"[Save] {Path.GetFileName(activeEditor.Key)}");
            _statusLabel.Text = $"Saved: {Path.GetFileName(activeEditor.Key)}";
        }
        else
        {
            _project?.Save();
            Log("[Save] Project saved");
        }
    }

    private void ShowFindInFiles()
    {
        if (_project == null) return;

        var query = ShowInputDialog("Find in Files", "Search text:", "");
        if (string.IsNullOrWhiteSpace(query)) return;

        Log($"[Search] ═══════════════════════════════════════════");
        Log($"[Search] Searching for: \"{query}\"");

        int matches = 0;
        int filesSearched = 0;
        foreach (var file in Directory.GetFiles(_project.ProjectDir, "*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (!EditableExtensions.Contains(ext)) continue;

            filesSearched++;
            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    var relPath = Path.GetRelativePath(_project.ProjectDir, file);
                    Log($"[Search] {relPath}:{i + 1}  {lines[i].Trim()}");
                    matches++;
                }
            }
        }

        Log($"[Search] Found {matches} match(es) in {filesSearched} files");
        Log($"[Search] ═══════════════════════════════════════════");
        _statusLabel.Text = $"Search: {matches} match(es) for \"{query}\"  |  {_project.Name}";
    }

    private void ValidateProject()
    {
        if (_project == null) return;

        Log("[Validate] ═══════════════════════════════════════════");
        Log("[Validate] Running project validation...");

        int warnings = 0;
        int errors = 0;

        if (string.IsNullOrEmpty(_project.ClientPath) || !Directory.Exists(_project.ClientPath))
        {
            Log("[Validate] ERROR: WoW client path not set or invalid");
            errors++;
        }
        else
        {
            var dataDir = Path.Combine(_project.ClientPath, "Data");
            if (!Directory.Exists(dataDir))
            {
                Log("[Validate] ERROR: Data/ folder not found in client directory");
                errors++;
            }
            else
            {
                Log("[Validate] OK: Client path valid");
            }
        }

        foreach (var folder in LightforgeProject.Folders)
        {
            var dir = Path.Combine(_project.ProjectDir, folder);
            if (!Directory.Exists(dir))
            {
                Log($"[Validate] WARN: Missing folder: {folder}/");
                warnings++;
            }
        }

        var sqlDir = Path.Combine(_project.ProjectDir, "SQL");
        if (Directory.Exists(sqlDir))
        {
            foreach (var sql in Directory.GetFiles(sqlDir, "*.sql", SearchOption.AllDirectories))
            {
                var content = File.ReadAllText(sql);
                if (content.Contains("DROP TABLE", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("DROP DATABASE", StringComparison.OrdinalIgnoreCase))
                {
                    Log($"[Validate] WARN: Destructive SQL in {Path.GetFileName(sql)}");
                    warnings++;
                }
            }
        }

        var patchDir = Path.Combine(_project.ProjectDir, "Patches");
        if (Directory.Exists(patchDir))
        {
            foreach (var file in Directory.GetFiles(patchDir, "*", SearchOption.AllDirectories))
            {
                var fi = new FileInfo(file);
                if (fi.Length == 0)
                {
                    Log($"[Validate] WARN: Empty file: Patches/{Path.GetRelativePath(patchDir, file)}");
                    warnings++;
                }
                if (fi.Length > 50 * 1024 * 1024)
                {
                    Log($"[Validate] WARN: Large file ({FormatFileSize(fi.Length)}): {Path.GetFileName(file)}");
                    warnings++;
                }
            }
        }

        if (errors == 0 && warnings == 0)
            Log("[Validate] All checks passed!");
        else
            Log($"[Validate] {errors} error(s), {warnings} warning(s)");

        Log("[Validate] ═══════════════════════════════════════════");
        _statusLabel.Text = errors > 0
            ? $"Validation: {errors} error(s), {warnings} warning(s)  |  {_project.Name}"
            : $"Validation passed ({warnings} warning(s))  |  {_project.Name}";
    }

    private static void ShowKeyboardShortcuts()
    {
        using var dlg = new Form
        {
            Text = "Keyboard Shortcuts",
            ClientSize = new Size(360, 370),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Theme.BgDark,
            ForeColor = Theme.TextNorm
        };

        var shortcuts = new (string key, string action)[]
        {
            ("Ctrl+N", "New Project"),
            ("Ctrl+O", "Open Project"),
            ("Ctrl+S", "Save File / Project"),
            ("Ctrl+B", "Build Patch"),
            ("F5", "Refresh File Tree"),
            ("Ctrl+Shift+F", "Find in Files"),
            ("Ctrl+G", "Go to Line (in editor)"),
            ("Ctrl+W", "Close Editor Tab"),
            ("", ""),
            ("Double-click .sql/.lua/.txt", "Open in Editor"),
            ("Double-click .dbc", "Open in WDBXEditor"),
            ("Double-click .mpq", "Open in MPQ Editor"),
            ("Double-click .m2/.wmo", "Open in WMVx"),
            ("Double-click .blp", "Open in BLP Lab"),
        };

        int y = 14;
        foreach (var (key, action) in shortcuts)
        {
            if (string.IsNullOrEmpty(key))
            {
                dlg.Controls.Add(new Panel { Location = new Point(16, y + 4), Size = new Size(328, 1), BackColor = Theme.Border });
                y += 12;
                continue;
            }
            dlg.Controls.Add(new Label
            {
                Text = key,
                Font = Theme.Mono,
                ForeColor = Theme.Accent,
                Location = new Point(16, y),
                AutoSize = true
            });
            dlg.Controls.Add(new Label
            {
                Text = action,
                Font = Theme.Normal,
                ForeColor = Theme.TextNorm,
                Location = new Point(200, y),
                AutoSize = true
            });
            y += 24;
        }

        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(260, 330),
            Size = new Size(80, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Accent,
            ForeColor = Color.White
        };
        dlg.Controls.Add(ok);
        dlg.AcceptButton = ok;
        dlg.ShowDialog();
    }

    private static void ShowAboutDialog()
    {
        using var dlg = new Form
        {
            Text = "About Lightforge",
            ClientSize = new Size(400, 280),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Theme.BgDark,
            ForeColor = Theme.TextNorm
        };

        var logo = new Label
        {
            Text = "◆ LIGHTFORGE ENGINE",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Theme.Gold,
            Location = new Point(20, 20),
            AutoSize = true
        };
        var version = new Label
        {
            Text = "Version 2.0",
            Font = Theme.Header,
            ForeColor = Theme.TextBright,
            Location = new Point(22, 52),
            AutoSize = true
        };
        var desc = new Label
        {
            Text = "A project-based workspace for World of Warcraft\n" +
                   "modding (Vanilla through BfA). Organizes tools,\n" +
                   "assets, and build pipelines into a unified engine.",
            Font = Theme.Normal,
            ForeColor = Theme.TextDim,
            Location = new Point(22, 82),
            AutoSize = true
        };

        var sep = new Panel
        {
            Location = new Point(20, 145),
            Size = new Size(360, 1),
            BackColor = Theme.Border
        };

        var info = new Label
        {
            Text = $"Runtime: .NET {Environment.Version}\n" +
                   $"OS: {Environment.OSVersion}\n" +
                   $"Platform: {(Environment.Is64BitProcess ? "x64" : "x86")}",
            Font = Theme.Small,
            ForeColor = Theme.TextMuted,
            Location = new Point(22, 156),
            AutoSize = true
        };

        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(300, 240),
            Size = new Size(80, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Accent,
            ForeColor = Color.White
        };

        dlg.Controls.AddRange([logo, version, desc, sep, info, ok]);
        dlg.AcceptButton = ok;
        dlg.ShowDialog();
    }

    private void ShowFileProperties(string filePath)
    {
        var info = new FileInfo(filePath);
        var ext = info.Extension.ToLowerInvariant();
        var fileType = ext switch
        {
            ".dbc" => "DBC Database File",
            ".mpq" => "MPQ Archive",
            ".m2" => "M2 Model",
            ".wmo" => "World Map Object",
            ".blp" => "BLP Texture",
            ".adt" => "ADT Map Tile",
            ".wdt" => "WDT Map Definition",
            ".wdl" => "WDL Low-Res Map",
            ".sql" => "SQL Script",
            ".lua" => "Lua Script",
            ".png" or ".tga" or ".bmp" => "Image File",
            ".skin" => "M2 Skin File",
            _ => $"{ext.TrimStart('.')} File"
        };

        using var dlg = new Form
        {
            Text = "File Properties",
            ClientSize = new Size(380, 220),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Theme.BgPanel,
            ForeColor = Theme.TextNorm
        };

        int y = 14;
        void AddRow(string label, string value)
        {
            dlg.Controls.Add(new Label { Text = label, Font = Theme.SectionHead, ForeColor = Theme.TextDim, Location = new Point(14, y), AutoSize = true });
            dlg.Controls.Add(new Label { Text = value, Font = Theme.Normal, ForeColor = Theme.TextBright, Location = new Point(100, y), AutoSize = true });
            y += 24;
        }

        AddRow("Name:", info.Name);
        AddRow("Type:", fileType);
        AddRow("Size:", FormatFileSize(info.Length));
        AddRow("Created:", info.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"));
        AddRow("Modified:", info.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
        if (_project != null)
            AddRow("Relative:", Path.GetRelativePath(_project.ProjectDir, filePath));

        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(280, 180),
            Size = new Size(80, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Accent,
            ForeColor = Color.White
        };
        dlg.Controls.Add(ok);
        dlg.AcceptButton = ok;
        dlg.ShowDialog();
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }

    private static void ShowMissing(string path)
    {
        MessageBox.Show(
            $"Tool not found:\n{path}\n\nCheck SOURCES.md for download links.",
            "Lightforge",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }
}

internal class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public DarkMenuRenderer() : base(new DarkColorTable()) { }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = Color.FromArgb(200, 200, 200);
        base.OnRenderItemText(e);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item.Selected)
        {
            using var brush = new SolidBrush(Color.FromArgb(55, 55, 58));
            e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
        }
        else
        {
            using var brush = new SolidBrush(Color.FromArgb(36, 36, 36));
            e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
        }
    }
}

internal class DarkColorTable : ProfessionalColorTable
{
    public override Color MenuBorder => Color.FromArgb(48, 48, 48);
    public override Color MenuItemBorder => Color.FromArgb(55, 55, 58);
    public override Color MenuItemSelected => Color.FromArgb(55, 55, 58);
    public override Color MenuStripGradientBegin => Color.FromArgb(36, 36, 36);
    public override Color MenuStripGradientEnd => Color.FromArgb(36, 36, 36);
    public override Color ToolStripDropDownBackground => Color.FromArgb(36, 36, 36);
    public override Color ImageMarginGradientBegin => Color.FromArgb(36, 36, 36);
    public override Color ImageMarginGradientMiddle => Color.FromArgb(36, 36, 36);
    public override Color ImageMarginGradientEnd => Color.FromArgb(36, 36, 36);
    public override Color SeparatorDark => Color.FromArgb(48, 48, 48);
    public override Color SeparatorLight => Color.FromArgb(48, 48, 48);
}
