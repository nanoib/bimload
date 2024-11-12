﻿# Подключаем необходимые сборки для работы с Windows Forms
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
    $form.FormBorderStyle = 'FixedSingle'
    $form.MaximizeBox = $false

    # Определяем ширину столбцов
    $checkBoxWidth = 30
    $fileConfigWidth = 160
    $productNameWidth = 180
    $methodWidth = 70
    $currentVersionWidth = 90
    $newVersionWidth = 90
    $statusWidth = 140

    # Вычисляем общую ширину таблицы
    $gridPadding = 20
    $gridWidth = $checkBoxWidth + $fileConfigWidth + $productNameWidth + $methodWidth + 
    $currentVersionWidth + $newVersionWidth + $statusWidth + $gridPadding

    # Добавляем небольшой отступ для скроллбара и границ
    $formPadding = 35
    $formWidth = $gridWidth + $formPadding

    # Создаем DataGridView
    $dataGridView = New-Object System.Windows.Forms.DataGridView
    $dataGridView.Location = New-Object System.Drawing.Point(10,10)
    $dataGridView.AutoGenerateColumns = $false
    $dataGridView.AllowUserToAddRows = $false
    $dataGridView.RowHeadersVisible = $false
    $dataGridView.SelectionMode = 'FullRowSelect'
    $dataGridView.MultiSelect = $false
    $dataGridView.BackgroundColor = [System.Drawing.Color]::White
    $dataGridView.AllowUserToResizeRows = $false
    $form.Controls.Add($dataGridView)

    # Обновляем размер DataGridView
    $gridHeight = 250
    $dataGridView.Size = New-Object System.Drawing.Size($gridWidth, $gridHeight)

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

    # Отключаем редактирование для всех столбцов, кроме чекбоксов и метода
    foreach ($column in $dataGridView.Columns) {
        if ($column -isnot [System.Windows.Forms.DataGridViewCheckBoxColumn] -and $column -ne $methodColumn) {
            $column.ReadOnly = $true
        }
    }
    
    # Создаем кнопку для выбора/отмены всех элементов списка
    $buttonsOffset = 10
    $buttonRightPadding = 30
    $toggleAllButton = New-Object System.Windows.Forms.Button
    $toggleAllButton.Location = New-Object System.Drawing.Point(
        10,
        ($dataGridView.Bottom+$buttonsOffset)
        )
    $toggleAllButton.Text = 'Выбрать все'
    $toggleAllButton.AutoSize = $true
    $toggleAllButton.Padding = New-Object System.Windows.Forms.Padding(5, 2, 5, 2)
    $form.Controls.Add($toggleAllButton)

    # Создаем кнопку для запуска обновления выбранных программ
    $updateButton = New-Object System.Windows.Forms.Button
    $updateButton.Location = New-Object System.Drawing.Point(
        ($toggleAllButton.Right + $buttonRightPadding),
        $toggleAllButton.Top
    )
    $updateButton.Text = 'Обновить'
    $updateButton.AutoSize = $true
    $updateButton.Padding = New-Object System.Windows.Forms.Padding(5, 2, 5, 2)
    $form.Controls.Add($updateButton)


    # Создаем RichTextBox для логов
    $logTextBoxOffset = 10
    $logTextBoxHeigth = 75
    $logTextBoxWidth = $gridWidth
    $logTextBox = New-Object System.Windows.Forms.RichTextBox
    $logTextBox.Location = New-Object System.Drawing.Point(10, ($toggleAllButton.Bottom+$logTextBoxOffset))
    $logTextBox.Size = New-Object System.Drawing.Size($logTextBoxWidth, $logTextBoxHeigth)
    $logTextBox.ReadOnly = $true
    $form.Controls.Add($logTextBox)

    # Присваиваем RichTextBox синхронизированному хэшу
    $SyncHash.LogTextBox = $logTextBox

    # Создаем метку для отображения статуса операции
    $statusLabelOffset = 10
    $statusLabel = New-Object System.Windows.Forms.Label
    $statusLabel.Location = New-Object System.Drawing.Point(10,($logTextBox.Bottom+$statusLabelOffset))
    $statusLabel.Size = New-Object System.Drawing.Size($gridWidth,30)  # Увеличиваем ширину метки
    $statusLabel.Text = 'Готов к обновлению'
    $form.Controls.Add($statusLabel)

    # Обновляем размер формы
    $formNeathPadding = 30
    $formHeigth = $statusLabel.Bottom + $formNeathPadding
    $form.Size = New-Object System.Drawing.Size($formWidth, $formHeigth)


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