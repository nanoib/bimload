using System.Drawing;
using System.Windows.Forms;
using Bimload.Core.Logging;
using Bimload.Core.Models;
using Bimload.Core.Parsers;
using Bimload.Core.Services;
using Bimload.Gui.Logging;
using Bimload.Gui.Services;

namespace Bimload.Gui.Forms;

public partial class MainForm : Form
{
    private DataGridView _dataGridView = null!;
    private RichTextBox _logTextBox = null!;
    private Button _toggleAllButton = null!;
    private Button _updateButton = null!;
    private Label _statusLabel = null!;
    private SplitContainer _splitContainer = null!;

    private readonly ConfigurationLoader _configurationLoader;
    private readonly StateManager _stateManager;
    private readonly IUpdateService _updateService;
    private readonly ILogger _logger;

    public MainForm()
    {
        InitializeComponent();

        // Initialize services
        var parser = new CredentialsParser();
        _configurationLoader = new ConfigurationLoader(parser);

        var projectRoot = FindProjectRoot();
        var credsFolder = Path.Combine(projectRoot, "creds");
        var stateFile = Path.Combine(projectRoot, "update_info.json");
        _stateManager = new StateManager(stateFile);

        // Initialize update service with real implementations
        var wmiQueryWrapper = new WmiQueryWrapper();
        var wmiService = new WmiService(wmiQueryWrapper);
        var versionService = new VersionService();
        var httpClient = new System.Net.Http.HttpClient();
        var httpClientWrapper = new HttpClientWrapper(httpClient);
        var programInstaller = new ProgramInstaller();
        _logger = new RichTextBoxLogger(_logTextBox);
        _updateService = new UpdateService(wmiService, versionService, httpClientWrapper, programInstaller, _logger);

        LoadConfigurations();
    }

    private void InitializeComponent()
    {
        // Form settings
        Text = "Bimload — Developer BIM Update Manager";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = true;

        // Column widths
        var checkBoxWidth = 30;
        var fileConfigWidth = 160;
        var productNameWidth = 180;
        var currentVersionWidth = 90;
        var newVersionWidth = 90;
        var statusWidth = 140;

        var gridPadding = 30;
        var gridWidth = checkBoxWidth + fileConfigWidth + productNameWidth +
                       currentVersionWidth + newVersionWidth + statusWidth + gridPadding;
        var formPadding = 35;
        var formWidth = gridWidth + formPadding;
        var formHeight = 450;

        Size = new Size(formWidth, formHeight);
        MinimumSize = new Size(formWidth, formHeight);
        MaximumSize = new Size(formWidth, 1500);

        // Create SplitContainer for table and logs
        _splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            FixedPanel = FixedPanel.None,
            IsSplitterFixed = false,
            BorderStyle = BorderStyle.None,
            SplitterWidth = 8,
            SplitterDistance = 300  // Initial split position
        };
        Controls.Add(_splitContainer);

        // Create DataGridView in top panel of split container
        _dataGridView = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = SystemColors.Control,
            AllowUserToResizeRows = false,
            BorderStyle = BorderStyle.None
        };

        _splitContainer.Panel1.Padding = new Padding(10, 10, 10, 0);  // No bottom padding
        _splitContainer.Panel1.Controls.Add(_dataGridView);

        // Create buttons panel under DataGridView
        var buttonsPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 45,
            Padding = new Padding(10, 5, 10, 5)
        };
        _splitContainer.Panel1.Controls.Add(buttonsPanel);

        // Create buttons in buttons panel
        _toggleAllButton = new Button
        {
            Text = "Выбрать все",
            AutoSize = true,
            Location = new Point(10, 5),
            Padding = new Padding(5, 2, 5, 2)
        };
        _toggleAllButton.Click += ToggleAllButton_Click;
        buttonsPanel.Controls.Add(_toggleAllButton);

        _updateButton = new Button
        {
            Text = "Обновить",
            AutoSize = true,
            Location = new Point(_toggleAllButton.Right + 30, 5),
            Padding = new Padding(5, 2, 5, 2)
        };
        _updateButton.Click += UpdateButton_Click;
        buttonsPanel.Controls.Add(_updateButton);

        // Add columns
        var checkBoxColumn = new DataGridViewCheckBoxColumn { HeaderText = "" };
        checkBoxColumn.Width = checkBoxWidth;
        _dataGridView.Columns.Add(checkBoxColumn);
        
        var fileConfigColumn = new DataGridViewTextBoxColumn { Name = "FileConfig", HeaderText = "Файл конфигурации" };
        fileConfigColumn.Width = fileConfigWidth;
        fileConfigColumn.ReadOnly = true;
        _dataGridView.Columns.Add(fileConfigColumn);
        
        var productNameColumn = new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "Название продукта" };
        productNameColumn.Width = productNameWidth;
        productNameColumn.ReadOnly = true;
        _dataGridView.Columns.Add(productNameColumn);
        
        var currentVersionColumn = new DataGridViewTextBoxColumn { Name = "CurrentVersion", HeaderText = "Текущая версия" };
        currentVersionColumn.Width = currentVersionWidth;
        currentVersionColumn.ReadOnly = true;
        _dataGridView.Columns.Add(currentVersionColumn);
        
        var newVersionColumn = new DataGridViewTextBoxColumn { Name = "NewVersion", HeaderText = "Новая версия" };
        newVersionColumn.Width = newVersionWidth;
        newVersionColumn.ReadOnly = true;
        _dataGridView.Columns.Add(newVersionColumn);
        
        var statusColumn = new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Статус" };
        statusColumn.Width = statusWidth;
        statusColumn.ReadOnly = true;
        _dataGridView.Columns.Add(statusColumn);

        // Create bottom panel for logs and status
        var bottomPanel = new Panel 
        { 
            Dock = DockStyle.Fill,
            MinimumSize = new Size(0, 150)  // Ensure minimum height
        };
        _splitContainer.Panel2.Padding = new Padding(0);  // No padding in Panel2
        _splitContainer.Panel2.Controls.Add(bottomPanel);

        // Create status label at the bottom - MUST be added FIRST
        _statusLabel = new Label
        {
            Text = "Готов к обновлению",
            Dock = DockStyle.Bottom,
            Height = 28,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = SystemColors.Control,
            Padding = new Padding(5, 5, 5, 5),
            TextAlign = ContentAlignment.MiddleLeft
        };
        bottomPanel.Controls.Add(_statusLabel);

        // Create RichTextBox for logs - MUST be added AFTER status label
        // Use Anchor instead of Dock to prevent overlapping with status label
        _logTextBox = new RichTextBox
        {
            Location = new Point(0, 0),
            Size = new Size(bottomPanel.Width, bottomPanel.Height - _statusLabel.Height),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            HideSelection = true,
            TabStop = false,
            Cursor = Cursors.Arrow,
            Font = new Font("Consolas", 9),
            BackColor = Color.White,
            Multiline = true,
            WordWrap = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            AcceptsTab = false
        };
        bottomPanel.Controls.Add(_logTextBox);
        
        // Update RichTextBox size when panel resizes
        bottomPanel.Resize += (s, e) =>
        {
            _logTextBox.Width = bottomPanel.Width;
            _logTextBox.Height = bottomPanel.Height - _statusLabel.Height;
        };

        Controls.Add(_splitContainer);
    }

    private void LoadConfigurations()
    {
        var projectRoot = FindProjectRoot();
        var credsFolder = Path.Combine(projectRoot, "creds");
        var configurations = _configurationLoader.LoadConfigurations(credsFolder);
        var savedStates = _stateManager.LoadState().ToDictionary(s => s.FileName, s => s);

        _dataGridView.Rows.Clear();

        foreach (var config in configurations)
        {
            var row = _dataGridView.Rows.Add();
            var newRow = _dataGridView.Rows[row];

            var isSelected = savedStates.ContainsKey(config.FileName) 
                ? savedStates[config.FileName].IsSelected 
                : true;

            newRow.Cells[0].Value = isSelected;
            newRow.Cells["FileConfig"].Value = config.FileName;
            newRow.Cells["ProductName"].Value = config.Credentials.ProductName ?? "";
            newRow.Cells["CurrentVersion"].Value = "";
            newRow.Cells["NewVersion"].Value = "";
            newRow.Cells["Status"].Value = "Ожидание";
        }
    }

    private void ToggleAllButton_Click(object? sender, EventArgs e)
    {
        var allChecked = _dataGridView.Rows.Cast<DataGridViewRow>()
            .All(row => row.Cells[0].Value as bool? == true);

        foreach (DataGridViewRow row in _dataGridView.Rows)
        {
            row.Cells[0].Value = !allChecked;
        }
    }

    private async void UpdateButton_Click(object? sender, EventArgs e)
    {
        _statusLabel.Text = "Выполняется обновление...";
        _updateButton.Enabled = false;
        Refresh();

        // Save state
        var states = new List<ProgramState>();
        foreach (DataGridViewRow row in _dataGridView.Rows)
        {
            states.Add(new ProgramState
            {
                FileName = row.Cells["FileConfig"].Value?.ToString() ?? "",
                IsSelected = row.Cells[0].Value as bool? == true
            });
        }
        _stateManager.SaveState(states);

        // Process updates
        foreach (DataGridViewRow row in _dataGridView.Rows)
        {
            if (row.Cells[0].Value as bool? == true)
            {
                var fileName = row.Cells["FileConfig"].Value?.ToString() ?? "";
                row.Cells["Status"].Value = "Обновляется...";
                Refresh();

                try
                {
                    var projectRoot = FindProjectRoot();
                    var credsFolder = Path.Combine(projectRoot, "creds");
                    var iniFile = Path.Combine(credsFolder, fileName);
                    var content = File.ReadAllText(iniFile);
                    var credentials = new CredentialsParser().Parse(content);

                    var result = await _updateService.UpdateAsync(credentials);

                    row.Cells["CurrentVersion"].Value = result.OldVersion ?? "";
                    row.Cells["NewVersion"].Value = result.NewVersion ?? "";
                    row.Cells["Status"].Value = result.Status;
                }
                catch (Exception ex)
                {
                    row.Cells["Status"].Value = $"Ошибка: {ex.Message}";
                    _logger.Log($"Ошибка при обновлении {fileName}: {ex.Message}", LogLevel.Error);
                }
            }
        }

        _statusLabel.Text = "Обновление завершено";
        _statusLabel.Refresh();
        _updateButton.Enabled = true;
        _toggleAllButton.Enabled = true;
    }

    private static string FindProjectRoot()
    {
        // Strategy: Find the directory that contains the "creds" folder
        // Start from current working directory (when running via dotnet run)
        // or from application startup path (when running exe directly)
        
        var startPath = Directory.GetCurrentDirectory();
        
        // If current directory is in bin/obj, use Application.StartupPath instead
        if (startPath.Contains(@"\bin\") || startPath.Contains(@"\obj\"))
        {
            startPath = Application.StartupPath;
        }

        var currentDir = new DirectoryInfo(startPath);
        
        // Walk up the directory tree to find the project root
        // Project root is identified by presence of "creds" folder
        while (currentDir != null)
        {
            var credsPath = Path.Combine(currentDir.FullName, "creds");
            if (Directory.Exists(credsPath))
            {
                return currentDir.FullName;
            }

            // If we're in "sharp" folder, go up one level to find "creds"
            if (currentDir.Name.Equals("sharp", StringComparison.OrdinalIgnoreCase))
            {
                var parentDir = currentDir.Parent;
                if (parentDir != null)
                {
                    var parentCredsPath = Path.Combine(parentDir.FullName, "creds");
                    if (Directory.Exists(parentCredsPath))
                    {
                        return parentDir.FullName;
                    }
                }
            }

            currentDir = currentDir.Parent;
        }

        // Fallback: try going up from Application.StartupPath
        // This handles the case when running from bin/Debug/net8.0-windows
        var startupDir = new DirectoryInfo(Application.StartupPath);
        for (int i = 0; i < 6 && startupDir != null; i++)
        {
            var credsPath = Path.Combine(startupDir.FullName, "creds");
            if (Directory.Exists(credsPath))
            {
                return startupDir.FullName;
            }
            startupDir = startupDir.Parent;
        }

        // Last resort: return a path that might work
        return Path.GetFullPath(Path.Combine(Application.StartupPath, "..", "..", "..", "..", ".."));
    }
}

