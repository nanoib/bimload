# Подключаем необходимые сборки для работы с Windows Forms
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# Функция для создания и отображения графического интерфейса

function Show-UpdateInterface {
    param(
        [Parameter(Mandatory=$true)]
        [hashtable]$SyncHash
    )
    # Создаем основное окно приложения
    $form = New-Object System.Windows.Forms.Form
    $form.Text = 'Bimload — Developer BIM Update Manager'
    $form.StartPosition = 'CenterScreen'
    $form.FormBorderStyle = 'Sizable'
    $form.MaximizeBox = $false
    $form.MinimizeBox = $true

    # Определяем ширину столбцов
    $checkBoxWidth = 30
    $fileConfigWidth = 160
    $productNameWidth = 180
    $methodWidth = 70
    $currentVersionWidth = 90
    $newVersionWidth = 90
    $statusWidth = 140

    # Вычисляем общую ширину таблицы
    $gridPadding = 30
    $gridWidth = $checkBoxWidth + $fileConfigWidth + $productNameWidth + $methodWidth + 
    $currentVersionWidth + $newVersionWidth + $statusWidth + $gridPadding

    # Добавляем небольшой отступ для скроллбара и границ
    $formPadding = 35
    $formWidth = $gridWidth + $formPadding

    # Создаем SplitContainer
    $splitContainer = New-Object System.Windows.Forms.SplitContainer
    $splitContainer.Dock = [System.Windows.Forms.DockStyle]::Fill
    $splitContainer.Orientation = [System.Windows.Forms.Orientation]::Horizontal
    $splitContainer.FixedPanel = [System.Windows.Forms.FixedPanel]::Panel2
    $form.Controls.Add($splitContainer)

    # Создаем DataGridView
    $dataGridView = New-Object System.Windows.Forms.DataGridView
    $dataGridView.Dock = [System.Windows.Forms.DockStyle]::Fill
    $dataGridView.AutoGenerateColumns = $false
    $dataGridView.AllowUserToAddRows = $false
    $dataGridView.RowHeadersVisible = $false
    $dataGridView.SelectionMode = 'FullRowSelect'
    $dataGridView.MultiSelect = $false
    $dataGridView.BackgroundColor = [System.Drawing.Color]::White
    $dataGridView.AllowUserToResizeRows = $false
    
    $splitContainer.Panel1.Controls.Add($dataGridView)
    # Устанавливаем отступ слева на 10 пикселей
    $dataGridView.Location = New-Object System.Drawing.Point(10, 0)
    $dataGridView.Anchor = [System.Windows.Forms.AnchorStyles]::Top -bor [System.Windows.Forms.AnchorStyles]::Left -bor [System.Windows.Forms.AnchorStyles]::Right -bor [System.Windows.Forms.AnchorStyles]::Bottom

    # Обновляем размер DataGridView
    $dataGridView.Width = $splitContainer.Panel1.Width - 20
    $dataGridView.Height = $splitContainer.Panel1.Height - 10

    $splitter = New-Object System.Windows.Forms.Splitter
    $splitter.Dock = [System.Windows.Forms.DockStyle]::Top
    $splitter.Height = 5
    $splitContainer.Panel2.Controls.Add($splitter)

    # Добавляем столбцы
    $checkBoxColumn = New-Object System.Windows.Forms.DataGridViewCheckBoxColumn
    $checkBoxColumn.HeaderText = ""

    $dataGridView.Columns.Add($checkBoxColumn)

    $dataGridView.Columns.Add("FileConfig", "Файл конфигурации")
    $dataGridView.Columns.Add("ProductName", "Название продукта")

    $methodColumn = New-Object System.Windows.Forms.DataGridViewComboBoxColumn
    $methodColumn.HeaderText = "Способ"
    $methodColumn.Items.AddRange(@("FTP", "HTTP"))
    $dataGridView.Columns.Add($methodColumn)

    $dataGridView.Columns.Add("CurrentVersion", "Текущая версия")
    $dataGridView.Columns.Add("NewVersion", "Новая версия")
    $dataGridView.Columns.Add("Status", "Статус")

    # Настраиваем ширину столбцов
    $checkBoxColumn.Width = $checkBoxWidth
    $dataGridView.Columns["FileConfig"].Width = $fileConfigWidth
    $dataGridView.Columns["ProductName"].Width = $productNameWidth
    $methodColumn.Width = $methodWidth
    $dataGridView.Columns["CurrentVersion"].Width = $currentVersionWidth
    $dataGridView.Columns["NewVersion"].Width = $newVersionWidth
    $dataGridView.Columns["Status"].Width = $statusWidth

    # Создаем Panel для нижней части окна
    $bottomPanel = New-Object System.Windows.Forms.Panel
    $bottomPanel.Dock = [System.Windows.Forms.DockStyle]::Fill
    $splitContainer.Panel2.Controls.Add($bottomPanel)

    # Отключаем редактирование для всех столбцов, кроме чекбоксов и метода
    foreach ($column in $dataGridView.Columns) {
        if ($column -isnot [System.Windows.Forms.DataGridViewCheckBoxColumn] -and $column -ne $methodColumn) {
            $column.ReadOnly = $true
        }
    }
    
    # Добавляем обработчик события двойного клика
    $dataGridView.Add_CellMouseDoubleClick({
        param($sender, $e)
        
        # Проверяем, что клик был по ячейке (а не по заголовку)
        if ($e.RowIndex -ge 0) {
            # Получаем текущую строку
            $currentRow = $dataGridView.Rows[$e.RowIndex]
            
            # Проверяем, что клик не был по столбцу метода
            if ($e.ColumnIndex -ne $methodColumn.Index) {
                # Инвертируем значение чекбокса
                $currentRow.Cells[0].Value = !$currentRow.Cells[0].Value
                
                # Обновляем отображение
                $dataGridView.RefreshEdit()
                $dataGridView.NotifyCurrentCellDirty($true)
            }
        }
    })

    # Создаем кнопку для выбора/отмены всех элементов списка
    $buttonsOffset = 10
    $buttonRightPadding = 30
    $toggleAllButton = New-Object System.Windows.Forms.Button
    $toggleAllButton.Location = New-Object System.Drawing.Point(
        10,
        ($bottomPanel.Top+$buttonsOffset)
        )
    $toggleAllButton.Text = 'Выбрать все'
    $toggleAllButton.AutoSize = $true
    $toggleAllButton.Padding = New-Object System.Windows.Forms.Padding(5, 2, 5, 2)
    $bottomPanel.Controls.Add($toggleAllButton)

    # Создаем кнопку для запуска обновления выбранных программ
    $updateButton = New-Object System.Windows.Forms.Button
    $updateButton.Location = New-Object System.Drawing.Point(
        ($toggleAllButton.Right + $buttonRightPadding),
        $toggleAllButton.Top
    )
    $updateButton.Text = 'Обновить'
    $updateButton.AutoSize = $true
    $updateButton.Padding = New-Object System.Windows.Forms.Padding(5, 2, 5, 2)
    $bottomPanel.Controls.Add($updateButton)


    # Создаем RichTextBox для логов
    $logTextBoxOffset = 10
    $logTextBoxWidth = $bottomPanel.Width - 20
    $logTextBox = New-Object System.Windows.Forms.RichTextBox
    $logTextBox.Location = New-Object System.Drawing.Point(10, ($toggleAllButton.Bottom+$logTextBoxOffset))
    $logTextBox.Width = $logTextBoxWidth
    $logTextBox.Anchor = [System.Windows.Forms.AnchorStyles]::Top -bor [System.Windows.Forms.AnchorStyles]::Left -bor [System.Windows.Forms.AnchorStyles]::Right -bor [System.Windows.Forms.AnchorStyles]::Bottom
    $logTextBox.ReadOnly = $true
    $logTextBox.HideSelection = $true
    $logTextBox.TabStop = $false
    $logTextBox.Cursor = [System.Windows.Forms.Cursors]::Arrow
    $bottomPanel.Controls.Add($logTextBox)

    # Присваиваем RichTextBox синхронизированному хэшу
    $SyncHash.LogTextBox = $logTextBox

    # Создаем метку для отображения статуса операции
    $statusLabelOffset1 = 0
    $statusLabelOffset2 = 10
    $statusLabel = New-Object System.Windows.Forms.Label
    $statusLabel.AutoSize = $false
    $statusLabel.Width = $bottomPanel.Width - 20
    $statusLabel.Anchor = [System.Windows.Forms.AnchorStyles]::Left -bor [System.Windows.Forms.AnchorStyles]::Right -bor [System.Windows.Forms.AnchorStyles]::Bottom
    $statusLabel.Text = 'Готов к обновлению'
    $bottomPanel.Controls.Add($statusLabel)

    # Функция для обновления положения и размера элементов
    $updateControlsPosition = {
        $statusLabel.Location = New-Object System.Drawing.Point(10, ($bottomPanel.Height - $statusLabel.Height - $statusLabelOffset1))
        $logTextBox.Height = $bottomPanel.Height - $logTextBox.Top - $statusLabel.Height - $statusLabelOffset2
    }

    # Добавляем обработчик события изменения размера для bottomPanel
    $bottomPanel.Add_Resize($updateControlsPosition)

    # Добавляем обработчик события изменения положения сплиттера
    $splitContainer.Add_SplitterMoved({
        & $updateControlsPosition
    })

    & $updateControlsPosition
    # Обновляем размер формы
    $formHeigth = 500
    # Устанавливаем начальное положение сплиттера
    $splitContainer.SplitterDistance = 0



    $form.Size = New-Object System.Drawing.Size($formWidth, $formHeigth)
    $form.MinimumSize = New-Object System.Drawing.Size($formWidth, $formHeigth)
    $form.MaximumSize = New-Object System.Drawing.Size($formWidth, 1500)    
    $form.ClientSize = New-Object System.Drawing.Size($formWidth, $formHeigth)

    # Обновляем обработчик события изменения размера формы
    $form.Add_SizeChanged({
        # Обновляем положение и размеры элементов
        & $updateControlsPosition
    })

    # Заполняем DataGridView данными
    $fileFolder = Join-Path -Path $PSScriptRoot -ChildPath "..\creds"
    $credFiles = Get-ChildItem -Path $fileFolder -Filter "*.ini"

    # Путь к файлу update_info.ps1
    $infoFilePath = Join-Path -Path $PSScriptRoot -ChildPath "..\update_info.ps1"

    # Загружаем сохраненную информацию, если файл существует
    $savedInfo = @{}
    if (Test-Path $infoFilePath) {
        $savedData = Invoke-Expression (Get-Content $infoFilePath -Raw)
        foreach ($item in $savedData) {
            $savedInfo[$item.CredFileName] = $item
        }
    }

    foreach ($file in $credFiles) {
        $credentials = Get-Credentials -fileFolder $fileFolder -fileName $file.Name
        $row = $dataGridView.Rows.Add()
        $newRow = $dataGridView.Rows[$row]

        # Проверяем, есть ли сохраненная информация для этого файла
        if ($savedInfo.ContainsKey($file.Name)) {
            $savedItem = $savedInfo[$file.Name]
            $newRow.Cells[0].Value = $savedItem.IsSelected  # Checkbox
            $method = $savedItem.Method
        } else {
            $newRow.Cells[0].Value = $true  # Checkbox по умолчанию
            $method = "FTP"  # Метод по умолчанию
        }

        $columnsToUpdate = @{
            "FileConfig" = $file.Name
            "ProductName" = $credentials.productName
            "CurrentVersion" = ""
            "NewVersion" = ""
            "Status" = "Ожидание"
        }

        foreach ($column in $columnsToUpdate.Keys) {
            if ($dataGridView.Columns.Contains($column)) {
                $newRow.Cells[$column].Value = $columnsToUpdate[$column]
            }
        }

        # Устанавливаем значение для столбца метода
        $methodColumnIndex = $dataGridView.Columns.IndexOf($methodColumn)
        if ($methodColumnIndex -ge 0) {
            $newRow.Cells[$methodColumnIndex].Value = $method
        }
    }

    $updateButton.Add_Click({
        $statusLabel.Text = "Выполняется обновление..."
        $form.Refresh()
    
        # Создаем путь к файлу для сохранения информации
        $infoFilePath = Join-Path -Path $PSScriptRoot -ChildPath "..\update_info.ps1"
    
        # Создаем массив для хранения информации
        $updateInfo = @()
    
        foreach ($row in $dataGridView.Rows) {
            $credFileName = $row.Cells["FileConfig"].Value
            $isChecked = $row.Cells[0].Value
            $method = $row.Cells[$methodColumn.Index].Value
    
            # Добавляем информацию в массив в формате PowerShell hashtable
            $updateInfo += "@{
                CredFileName = '$credFileName'
                IsSelected = `$$isChecked
                Method = '$method'
            }"
        }
    
        # Сохраняем информацию в файл
        $fileContent = "@(`n" + ($updateInfo -join ",`n") + "`n)"
        $fileContent | Out-File -FilePath $infoFilePath -Force
    
        foreach ($row in $dataGridView.Rows) {
            if ($row.Cells[0].Value -eq $true) {  # Если строка выбрана
                $credFileName = $row.Cells["FileConfig"].Value
                $method = $row.Cells[$methodColumn.Index].Value
                $row.Cells["Status"].Value = "Обновляется..."
                $dataGridView.Refresh()
    
                $result = Update-Bim -fileFolder $fileFolder -credFileName $credFileName -method $method.ToLower()
    
                $row.Cells["CurrentVersion"].Value = $result.OldVersion
                $row.Cells["NewVersion"].Value = $result.NewVersion
                $row.Cells["Status"].Value = $result.Status
    
                $dataGridView.Refresh()
            }
        }
    
        $statusLabel.Text = "Обновление завершено. Информация сохранена в $infoFilePath"
    })

    # Обновляем обработчик кнопки выбора всех элементов
    $toggleAllButton.Add_Click({
        $allChecked = $dataGridView.Rows | Where-Object { $_.Cells[0].Value -eq $true } | Measure-Object | Select-Object -ExpandProperty Count
        $allChecked = $allChecked -eq $dataGridView.Rows.Count

        foreach ($row in $dataGridView.Rows) {
            $row.Cells[0].Value = !$allChecked
        }
    })

    # Отображаем форму
    $form.ShowDialog()
}