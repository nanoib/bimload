function Get-Credentials($fileFolder, $fileName) {

    $otherCredentialsPath = Join-Path -Path $fileFolder -ChildPath $fileName
    $otherCredentials = Get-Content $otherCredentialsPath | ConvertFrom-StringData
    
    $credentials = @{
        httpUrl = $otherCredentials.httpUrl
        localPath = $otherCredentials.localPath
        productName = $otherCredentials.productName
        fileVersionPattern = $otherCredentials.fileVersionPattern
        productVersionPattern = $otherCredentials.productVersionPattern
    }
    
    return $credentials
}

function Get-PcLatestProgram($productName) {
    # Получаем все установленные версии программы
    Write-Host "2 - Получаем список программ"
    $programs = @(Get-WmiObject -Class Win32_Product |
    Where-Object { $_.Name -like "*$productName*" } |
    Sort-Object { [version]$_.Version } -Descending)
    if ($programs.Count -eq 0) {
        Write-Host "Не найдено ничо по запросу '$productName'"
        $pcLatestProgram = $false
    } else {
        # Выводим найденные версии
        Write-Host "Найденные программы по запросу '$productName':"
        $programs | ForEach-Object { Write-Host "$($_.Name), Версия: $($_.Version)" }
        # Определяем старшую версию
        $pcLatestProgram = $programs[0]

    return $pcLatestProgram
    }   
}

function Get-PcLatestVersion($pcLatestProgram, $productVersionPattern) {
    # Извлекаем версию установленной программы
    $pcLatestVersion = $pcLatestProgram.Version -replace $productVersionPattern, '$1'
    Write-Host "3 - Последняя установленная сборка определена: $pcLatestVersion"
    return $pcLatestVersion
}

function Get-HttpLatestFile($httpUrl) {
    try {
        # Скачиваем html
        Write-Host "4 - Будем искать файлы на https: $httpUrl"
        $webClient = New-Object System.Net.WebClient
        $htmlContent = $webClient.DownloadString($httpUrl)

        # Используем регулярные выражения для получения текста всех тегов span с классом name
        $pattern = '<span class="name">([^<]+)</span>'
        $regexMatches = [regex]::Matches($htmlContent, $pattern)

        if ($regexMatches.Count -gt 0) {
            # Извлекаем последний матч
            $lastMatch = $regexMatches[$regexMatches.Count - 1]

            # Извлекаем текст из последнего матча
            $latestFile = $lastMatch.Groups[1].Value

            Write-Host "Файл на https найден: $latestFile"
            return $latestFile
        } else {
            Write-Warning "Не найдено файлов на https!"
            return $null
        }
    }
    catch {
        Write-Error "Ошибка при загрузке или парсинге html: $_"
        return $null
    }
    finally {
        if ($webClient) {
            $webClient.Dispose()
        }
    }
}


function Compare-Versions($pcLatestVersion, $httpLatestVersion) {

    # Проверка необходимости установки новой версии
    if ($null -eq $pcLatestVersion -or $pcLatestVersion -eq '') {
        Write-Host "5 - Решено - программа не установлена, будем устанавливать (версию $httpLatestVersion)." -ForegroundColor DarkGreen
        return $true
    } elseif ([int]$pcLatestVersion -ge [int]$httpLatestVersion) {
        Write-Host "5 - Решено - установленная версия ($pcLatestVersion) самая новая. Версия на https та же или младше ($httpLatestVersion)."
        return $false
    } else {
        Write-Host "5 - Решено - установленная версия ($pcLatestVersion) устаревшая. Доступна новая версия на https ($httpLatestVersion)."
        return $true
    }
}

function Save-HttpFile($httpUrl, $httpLatestFile, $localFilePath) {
    $downloadUrl = [Uri]::new([Uri]$httpUrl, $httpLatestFile).AbsoluteUri

    Write-Host "7 - Начало скачивания файла '$downloadUrl'..." -NoNewline

    try {
        $ProgressPreference = 'SilentlyContinue'  # Отключаем индикатор прогресса
        Invoke-WebRequest -Uri $downloadUrl -OutFile $localFilePath
        Write-Host " Завершено." -ForegroundColor Green
        
        if (Test-Path -Path $localFilePath) {
            Write-Host "Файл успешно скачан в $localFilePath"
        } else {
            Write-Host "Не удалось найти скачанный файл в $localFilePath" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "Ошибка при скачивании файла: $_" -ForegroundColor Red
    }
    finally {
        $ProgressPreference = 'Continue'  # Возвращаем стандартное значение
    }
}


function Uninstall-Program($pcLatestProgram) {
    Write-Host "8 - Удаляется программа $($pcLatestProgram.Name), Версия: $($pcLatestProgram.Version)"
    $result = $pcLatestProgram.Uninstall()
    if ($result.ReturnValue -ne 0) {
        Write-Error "Ошибка при удалении программы. Код ошибки: $($result.ReturnValue)"
        exit 1
    }
}

function Install-Program($localFilePath) {
    Write-Host "9 - Начало установки новой версии: $localFilePath"
    try {
        Start-Process -FilePath $localFilePath -ArgumentList "/quiet" -Wait -NoNewWindow
        Write-Output "Процесс установки завершен"
    } catch {
        Write-Error "Ошибка при установке новой версии программы: $_"
    }
}

function Update-Program {
    param (
        [string]$localFilePath,
        $pcLatestProgram
    )
    if ($null -ne $localFilePath -and (Test-Path $localFilePath)) {
        # Удаление текущей версии программы
        if ($pcLatestProgram) {
            Uninstall-Program($pcLatestProgram)
        } else {
            Write-Host "8 - Удалять нечего"
        }
        # Установка новой версии программы
        Install-Program($localFilePath)
    } else {
        Write-Host "9 - Файл $localFilePath не найден. Установка программы пропущена"
    }
}

function Test-UpdateStatus {
    param (
        $productName,
        $productVersionPattern,
        $httpLatestVersion
    )

    $pcLatestProgram = Get-PcLatestProgram($productName)
    $pcLatestVersion = if ($pcLatestProgram) { 
        Get-PcLatestVersion -pcLatestProgram $pcLatestProgram -productVersionPattern $productVersionPattern 
    } else { 
        $null 
    }

    if ($null -eq $pcLatestVersion -and $null -ne $httpLatestVersion) {
        return "Не найдено. Скорее всего, программа не установилась"
    } elseif ($pcLatestVersion -eq $httpLatestVersion) {
        return "Программа обновлена"
    } elseif ($null -ne $pcLatestVersion -and [int]$pcLatestVersion -lt [int]$httpLatestVersion) {
        return "Программа не обновилась или обновилась некорректно! Текущая версия ниже новейшей."
    }
    return "Неизвестный статус"
}


function Update-Bim {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingPlainTextForPassword", "", Justification="Credential filename is not a password")]
    param (
        $fileFolder,
        $credFileName
    )

    $credentials = Get-Credentials -fileFolder $fileFolder -fileName $credFileName
    $pcLatestProgram = Get-PcLatestProgram($credentials.productName) 
    $pcLatestVersion = Get-PcLatestVersion -pcLatestProgram $pcLatestProgram -productVersionPattern $credentials.productVersionPattern

    $httpLatestFile = Get-HttpLatestFile -httpUrl $credentials.httpUrl
    $httpLatestVersion = $httpLatestFile -replace $credentials.fileVersionPattern, '$1'
        
    $updateNeeded = Compare-Versions -pcLatestVersion $pcLatestVersion -httpLatestVersion $httpLatestVersion
        
    if ($updateNeeded) {
        $localFilePath = Join-Path -Path $credentials.localPath -ChildPath $httpLatestFile
        if (-not (Test-Path $localFilePath)) {
            Save-HttpFile -httpUrl $credentials.httpUrl -httpLatestFile $httpLatestFile -localFilePath $localFilePath
        }
        Update-Program -localFilePath $localFilePath -pcLatestProgram $pcLatestProgram
        
        $newStatus = Test-UpdateStatus -productName $credentials.productName -productVersionPattern $credentials.productVersionPattern -httpLatestVersion $httpLatestVersion
        $newVersion = Get-PcLatestVersion -pcLatestProgram (Get-PcLatestProgram($credentials.productName)) -productVersionPattern $credentials.productVersionPattern
        
        return @{
            Status = $newStatus
            OldVersion = $pcLatestVersion
            NewVersion = $newVersion
        }
    } else {
        return @{
            Status = "Обновление не требуется"
            OldVersion = $pcLatestVersion
            NewVersion = $pcLatestVersion
        }
    }
}

# Определяем путь к папке с файлами учетных данных
$fileFolder = Join-Path -Path $PSScriptRoot -ChildPath "creds"

# Подключаем необходимые сборки для работы с Windows Forms
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# Функция для создания и отображения графического интерфейса

function Show-UpdateInterface {
    # Создаем основное окно приложения
    $form = New-Object System.Windows.Forms.Form
    $form.Text = 'Bimload — Developer BIM Update Manager'
    $form.Size = New-Object System.Drawing.Size(740,430)  # Увеличиваем ширину окна
    $form.StartPosition = 'CenterScreen'
    $form.FormBorderStyle = 'FixedSingle'
    $form.MaximizeBox = $false

    # Создаем список с чекбоксами для отображения программ
    $checkboxList = New-Object System.Windows.Forms.ListView
    $checkboxList.Location = New-Object System.Drawing.Point(10,10)
    $checkboxList.Size = New-Object System.Drawing.Size(700,250)  # Увеличиваем ширину списка
    $checkboxList.View = [System.Windows.Forms.View]::Details
    $checkboxList.CheckBoxes = $true
    $checkboxList.FullRowSelect = $true
    $checkboxList.Columns.Add("Файл конфигурации", 180)
    $checkboxList.Columns.Add("Название продукта", 200)
    $checkboxList.Columns.Add("Текущая версия", 100)  # Новый столбец
    $checkboxList.Columns.Add("Новая версия", 100)  # Новый столбец
    $checkboxList.Columns.Add("Статус", 160)
    $form.Controls.Add($checkboxList)

    # Создаем список изображений для иконок
    $imageList = New-Object System.Windows.Forms.ImageList
    $imageList.ImageSize = New-Object System.Drawing.Size(16, 16)
    $iconPath = Join-Path -Path $PSScriptRoot -ChildPath "icons"
    $imageList.Images.Add([System.Drawing.Image]::FromFile("$iconPath\question.png"))
    $imageList.Images.Add([System.Drawing.Image]::FromFile("$iconPath\wait.png"))
    $imageList.Images.Add([System.Drawing.Image]::FromFile("$iconPath\ok.png"))
    $imageList.Images.Add([System.Drawing.Image]::FromFile("$iconPath\error.png"))
    $checkboxList.SmallImageList = $imageList

    # Создаем кнопку для выбора/отмены всех элементов списка
    $toggleAllButton = New-Object System.Windows.Forms.Button
    $toggleAllButton.Location = New-Object System.Drawing.Point(10,270)
    $toggleAllButton.Size = New-Object System.Drawing.Size(660,30)  # Увеличиваем ширину кнопки
    $toggleAllButton.Text = 'Выбрать/Отменить все'
    $form.Controls.Add($toggleAllButton)

    # Создаем кнопку для запуска обновления выбранных программ
    $updateButton = New-Object System.Windows.Forms.Button
    $updateButton.Location = New-Object System.Drawing.Point(10,310)
    $updateButton.Size = New-Object System.Drawing.Size(660,30)  # Увеличиваем ширину кнопки
    $updateButton.Text = 'Обновить выбранные программы'
    $form.Controls.Add($updateButton)

    # Создаем метку для отображения статуса операции
    $statusLabel = New-Object System.Windows.Forms.Label
    $statusLabel.Location = New-Object System.Drawing.Point(10,350)
    $statusLabel.Size = New-Object System.Drawing.Size(960,30)  # Увеличиваем ширину метки
    $statusLabel.Text = 'Готов к обновлению'
    $form.Controls.Add($statusLabel)

    # Получаем список файлов учетных данных
    $fileFolder = Join-Path -Path $PSScriptRoot -ChildPath "creds"
    $credFiles = Get-ChildItem -Path $fileFolder -Filter "*.credentials"

    # Заполняем список программами из файлов учетных данных
    foreach ($file in $credFiles) {
        $credentials = Get-Credentials -fileFolder $fileFolder -fileName $file.Name
        $item = New-Object System.Windows.Forms.ListViewItem($file.Name)
        $item.SubItems.Add($credentials.productName)
        $item.SubItems.Add("")  # Пустая ячейка для текущей версии
        $item.SubItems.Add("")  # Пустая ячейка для новой версии
        $item.SubItems.Add("Ожидание")
        $item.ImageIndex = 0  # Индекс иконки question.png
        $item.Checked = $true
        $checkboxList.Items.Add($item)
    }

    # Обработчик события для кнопки выбора/отмены всех элементов
    $toggleAllButton.Add_Click({
        $allChecked = ($checkboxList.CheckedItems.Count -eq $checkboxList.Items.Count)
        foreach ($item in $checkboxList.Items) {
            $item.Checked = !$allChecked
        }
    })

    # Обработчик события для кнопки обновления
    $updateButton.Add_Click({
        $statusLabel.Text = "Выполняется обновление..."
        $form.Refresh()
    
        # Перебираем все выбранные элементы и запускаем обновление
        foreach ($item in $checkboxList.Items) {
            if ($item.Checked) {
                $credFileName = $item.Text
                $item.SubItems[4].Text = "Обновляется..."
                $item.ImageIndex = 1  # Индекс иконки wait.png
                $checkboxList.Refresh()
                
                $result = Update-Bim -fileFolder $fileFolder -credFileName $credFileName
                
                # Обновляем информацию в списке
                $item.SubItems[2].Text = $result.OldVersion  # Текущая версия
                $item.SubItems[3].Text = $result.NewVersion  # Новая версия
                $item.SubItems[4].Text = $result.Status  # Статус
                
                # Устанавливаем соответствующую иконку
                switch -Regex ($result.Status) {
                    "обновлена|не требуется" { $item.ImageIndex = 2 }  # ok.png
                    default { $item.ImageIndex = 3 }  # error.png
                }
                $checkboxList.Refresh()
            }
        }
    
        $statusLabel.Text = "Обновление завершено"
    })

    # Отображаем форму
    $form.ShowDialog()
}

# Вызываем функцию для отображения интерфейса
Show-UpdateInterface