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
    // Fixed size constants
    private const int DataGridViewHeight = 220; // Reduced by 40% from 300px
    private const int ButtonsPanelHeight = 45;
    private const int StatusLabelHeight = 28;
    private const int SpacingBetweenElements = 25;
    
    private DataGridView _dataGridView = null!;
    private RichTextBox _logTextBox = null!;
    private Button _toggleAllButton = null!;
    private Button _updateButton = null!;
    private Label _statusLabel = null!;
    private Panel _buttonsPanel = null!;

    private readonly ConfigurationLoader _configurationLoader;
    private readonly StateManager _stateManager;
    private readonly IUpdateService _updateService;
    private readonly ILogger _logger;

    public MainForm()
    {
        try
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
            var versionService = new VersionService();
            var httpClientHandler = new System.Net.Http.HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            var httpClient = new System.Net.Http.HttpClient(httpClientHandler)
            {
                Timeout = TimeSpan.FromSeconds(30)  // 30 seconds timeout
            };
            var httpClientWrapper = new HttpClientWrapper(httpClient);
            var programInstaller = new ProgramInstaller();
            _logger = new RichTextBoxLogger(_logTextBox);
            // Pass logger to WmiService for debugging
            var wmiService = new WmiService(wmiQueryWrapper, _logger);
            _updateService = new UpdateService(wmiService, versionService, httpClientWrapper, programInstaller, _logger);

            LoadConfigurations();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при инициализации формы: {ex.Message}\n\n{ex.StackTrace}", 
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw;
        }
    }

    private void InitializeComponent()
    {
        // Form settings
        Text = "Bimload — Developer BIM Update Manager";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = true;
        WindowState = FormWindowState.Normal;
        ShowInTaskbar = true;
        Visible = true;

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

        // Calculate minimum form height:
        // DataGridView: 300px
        // Buttons panel: 45px + 5px (top padding) + 5px (bottom padding) = 55px
        // Status label: 28px
        // Form padding: ~35px
        var minFormHeight = DataGridViewHeight + ButtonsPanelHeight + 5 + 5 + StatusLabelHeight + formPadding;
        
        // Ensure initial form height is at least the minimum
        if (formHeight < minFormHeight)
        {
            formHeight = minFormHeight;
        }

        Size = new Size(formWidth, formHeight);
        MinimumSize = new Size(formWidth, formHeight); // Cannot resize below initial height
        MaximumSize = new Size(formWidth, 1500);
        Padding = new Padding(10, 10, 10, 0); // Padding for top elements

        // Create status label at the bottom - MUST be added FIRST
        _statusLabel = new Label
        {
            Text = "Готов к обновлению",
            Dock = DockStyle.Bottom,
            Height = StatusLabelHeight,
            BorderStyle = BorderStyle.None,
            BackColor = SystemColors.Control,
            Padding = new Padding(5, 5, 5, 5),
            TextAlign = ContentAlignment.MiddleLeft
        };
        Controls.Add(_statusLabel);

        // Create panel for log - MUST be added SECOND
        // Spacing from buttons is handled by buttons panel bottom padding
        // Bottom padding ensures last log line is visible above status label
        var logPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 0, 0, StatusLabelHeight + 2) // Add padding to account for status label height
        };
        Controls.Add(logPanel);

        // Create buttons panel - MUST be added THIRD (before DataGridView)
        _buttonsPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = ButtonsPanelHeight + 5 + 5, // 45px buttons + 5px top spacing + 5px bottom spacing
            Padding = new Padding(0, 5, 0, 5) // 5px spacing from DataGridView above and log panel below
        };
        Controls.Add(_buttonsPanel);

        // Create DataGridView - MUST be added LAST (after buttons)
        _dataGridView = new DataGridView
        {
            Dock = DockStyle.Top,
            Height = DataGridViewHeight,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = SystemColors.Control,
            AllowUserToResizeRows = false,
            BorderStyle = BorderStyle.None,
            ScrollBars = ScrollBars.Vertical
        };
        _dataGridView.CellValueChanged += DataGridView_CellValueChanged;
        Controls.Add(_dataGridView);

        // Create buttons in buttons panel
        _toggleAllButton = new Button
        {
            Text = "Выбрать все",
            AutoSize = true,
            Location = new Point(10, 5),
            Padding = new Padding(5, 2, 5, 2)
        };
        _toggleAllButton.Click += ToggleAllButton_Click;
        _buttonsPanel.Controls.Add(_toggleAllButton);

        _updateButton = new Button
        {
            Text = "Обновить",
            AutoSize = true,
            Location = new Point(_toggleAllButton.Right + 30, 5),
            Padding = new Padding(5, 2, 5, 2)
        };
        _updateButton.Click += UpdateButton_Click;
        _buttonsPanel.Controls.Add(_updateButton);

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

        // Create RichTextBox for logs - fills the log panel
        _logTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
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
        logPanel.Controls.Add(_logTextBox);
    }

    private void LoadConfigurations()
    {
        try
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
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке конфигураций: {ex.Message}\n\n{ex.StackTrace}", 
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        
        // Save state after toggling all
        SaveCurrentState();
    }
    
    private void DataGridView_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        // Only save when checkbox column (column 0) is changed
        if (e.ColumnIndex == 0 && e.RowIndex >= 0)
        {
            SaveCurrentState();
        }
    }
    
    private void SaveCurrentState()
    {
        try
        {
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
        }
        catch (Exception ex)
        {
            _logger.Log($"Ошибка при сохранении состояния: {ex.Message}", LogLevel.Error);
        }
    }

    private async void UpdateButton_Click(object? sender, EventArgs e)
    {
        _statusLabel.Text = "Выполняется обновление...";
        _updateButton.Enabled = false;
        Refresh();

        // Save state
        SaveCurrentState();

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
                    var content = await File.ReadAllTextAsync(iniFile);
                    var credentials = new CredentialsParser().Parse(content);

                    // Create progress callback for this update
                    Action<long, long?>? progressCallback = null;
                    if (InvokeRequired)
                    {
                        progressCallback = (downloaded, total) =>
                        {
                            Invoke(new Action(() => UpdateDownloadProgress(downloaded, total)));
                        };
                    }
                    else
                    {
                        progressCallback = UpdateDownloadProgress;
                    }

                    var result = await _updateService.UpdateAsync(credentials, downloadProgressCallback: progressCallback);

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

    private void UpdateDownloadProgress(long downloadedBytes, long? totalBytes)
    {
        if (totalBytes.HasValue && totalBytes.Value > 0)
        {
            var downloadedMb = downloadedBytes / (1024.0 * 1024.0);
            var totalMb = totalBytes.Value / (1024.0 * 1024.0);
            _statusLabel.Text = $"Скачивается файл: {downloadedMb:F1} Мб из {totalMb:F1} Мб";
        }
        else
        {
            var downloadedMb = downloadedBytes / (1024.0 * 1024.0);
            _statusLabel.Text = $"Скачивается файл: {downloadedMb:F1} Мб";
        }
        _statusLabel.Refresh();
    }

    private static string FindProjectRoot()
    {
        // Strategy: Find the directory that contains the "creds" folder
        // 1. First, check in the directory where the exe is located (for compiled exe)
        // 2. Then, walk up from current directory to find project root (for dotnet run)
        
        // Check in exe directory first (for compiled exe)
        var exeDir = new DirectoryInfo(Application.StartupPath);
        var exeCredsPath = Path.Combine(exeDir.FullName, "creds");
        if (Directory.Exists(exeCredsPath))
        {
            return exeDir.FullName;
        }

        // Walk up from exe directory to find project root
        var currentDir = exeDir;
        while (currentDir != null)
        {
            var credsPath = Path.Combine(currentDir.FullName, "creds");
            if (Directory.Exists(credsPath))
            {
                return currentDir.FullName;
            }
            currentDir = currentDir.Parent;
        }

        // Fallback: try from current working directory (for dotnet run)
        var workDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (workDir != null)
        {
            var credsPath = Path.Combine(workDir.FullName, "creds");
            if (Directory.Exists(credsPath))
            {
                return workDir.FullName;
            }
            workDir = workDir.Parent;
        }

        // Last resort: return exe directory
        return exeDir.FullName;
    }
}

