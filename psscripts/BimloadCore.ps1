function Write-Log {
    param (
        [string]$Message,
        [System.Drawing.Color]$Color = [System.Drawing.Color]::Black,
        [bool]$Bold = $false
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] $Message"
    
    # Отправляем сообщение в GUI
    $syncHash.LogTextBox.Invoke([Action]{
        $syncHash.LogTextBox.SelectionStart = $syncHash.LogTextBox.TextLength
        $syncHash.LogTextBox.SelectionLength = 0
        $syncHash.LogTextBox.SelectionColor = $Color
        if ($Bold) {
            $syncHash.LogTextBox.SelectionFont = New-Object System.Drawing.Font($syncHash.LogTextBox.Font, [System.Drawing.FontStyle]::Bold)
        } else {
            $syncHash.LogTextBox.SelectionFont = $syncHash.LogTextBox.Font
        }
        $syncHash.LogTextBox.AppendText($logMessage + "`n")
        $syncHash.LogTextBox.ScrollToCaret()
    })
    
    # Также выводим в консоль
    Write-Host $logMessage
}

function Get-Credentials($fileFolder, $fileName) {
    $prodCredentialsPath = Join-Path -Path $fileFolder -ChildPath $fileName
    
    # Читаем файл и фильтруем строки, начинающиеся с '#'
    $prodContent = Get-Content $prodCredentialsPath | Where-Object { $_ -notmatch '^\s*#' }
    
    # Объединяем отфильтрованные строки в одну строку
    $prodContentString = $prodContent -join "`n"
    
    # Преобразуем строку в хэш-таблицу
    $prodCredentials = $prodContentString | ConvertFrom-StringData
    
    $credentials = @{
        httpUrl = $prodCredentials.httpUrl
        localPath = $prodCredentials.localPath
        productName = $prodCredentials.productName
        httpPattern = $prodCredentials.httpPattern
        fileVersionPattern = $prodCredentials.fileVersionPattern
        productVersionPattern = $prodCredentials.productVersionPattern
        ftpUrl = $prodCredentials.ftpUrl
        ftpUsername = $prodCredentials.ftpUsername
        ftpPassword = $prodCredentials.ftpPassword
        ftpFolder=$prodCredentials.ftpFolder
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



function Compare-Versions($pcLatestVersion, $serverLatestVersion) {

    # Проверка необходимости установки новой версии
    if ($null -eq $pcLatestVersion -or $pcLatestVersion -eq '') {
        Write-Log -Message "5 - Решено - программа не установлена, будем устанавливать (версию $serverLatestVersion)."
        return $true
    } elseif ([int]$pcLatestVersion -ge [int]$serverLatestVersion) {
        Write-Log -Message "5 - Решено - установленная версия ($pcLatestVersion) самая новая. Версия на сервере та же или младше ($serverLatestVersion)."
        return $false
    } else {
        Write-Log -Message "5 - Решено - установленная версия ($pcLatestVersion) устаревшая. Доступна новая версия на сервере ($serverLatestVersion)."
        return $true
    }
}



function Uninstall-Program($pcLatestProgram) {
    Write-Log -Message "8 - Удаляется программа $($pcLatestProgram.Name), Версия: $($pcLatestProgram.Version)"
    $result = $pcLatestProgram.Uninstall()
    if ($result.ReturnValue -ne 0) {
        Write-Error "Ошибка при удалении программы. Код ошибки: $($result.ReturnValue)"
        exit 1
    }
}

function Install-Program($localFilePath) {
    Write-Log -Message "9 - Начало установки новой версии: $localFilePath"
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
            Write-Log -Message "8 - Удалять нечего"
        }
        # Установка новой версии программы
        Install-Program($localFilePath)
    } else {
        Write-Log -Message "9 - Файл $localFilePath не найден. Установка программы пропущена"
    }
}

function Test-UpdateStatus {
    param (
        $productName,
        $productVersionPattern,
        $serverLatestVersion
    )

    $pcLatestProgram = Get-PcLatestProgram($productName)
    $pcLatestVersion = if ($pcLatestProgram) { 
        Get-PcLatestVersion -pcLatestProgram $pcLatestProgram -productVersionPattern $productVersionPattern 
    } else { 
        $null 
    }

    if ($null -eq $pcLatestVersion -and $null -ne $serverLatestVersion) {
        return "Не найдено. Скорее всего, программа не установилась"
    } elseif ($pcLatestVersion -eq $serverLatestVersion) {
        return "Программа обновлена"
    } elseif ($null -ne $pcLatestVersion -and [int]$pcLatestVersion -lt [int]$serverLatestVersion) {
        return "Программа не обновилась или обновилась некорректно! Текущая версия ниже новейшей."
    }
    return "Неизвестный статус"
}


function Update-Bim {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingPlainTextForPassword", "", Justification="Credential filename is not a password")]
    param (
        $fileFolder,
        $credFileName,
        $method
    )

    $credentials = Get-Credentials -fileFolder $fileFolder -fileName $credFileName
    $pcLatestProgram = Get-PcLatestProgram($credentials.productName) 
    $pcLatestVersion = Get-PcLatestVersion -pcLatestProgram $pcLatestProgram -productVersionPattern $credentials.productVersionPattern

    if ($method -eq "http") {
        $latestFile = Get-HttpLatestFile `
            -httpUrl $credentials.httpUrl `
            -httpPattern $credentials.httpPattern
    } else {
        if ([string]::IsNullOrWhiteSpace($credentials.ftpUrl) -or [string]::IsNullOrWhiteSpace($credentials.ftpFolder)) {
            return @{
                Status = "Ошибка: FTP URL или папка не указаны"
                OldVersion = $pcLatestVersion
                NewVersion = $pcLatestVersion
            }
        }
        $latestFile = Get-FtpLatestFile `
            -ftpUrl $credentials.ftpUrl `
            -ftpFolder $credentials.ftpFolder `
            -username $credentials.username `
            -password $credentials.password
    }

    if ($null -eq $latestFile) {
        return @{
            Status = "Ошибка: Не удалось получить информацию о последней версии"
            OldVersion = $pcLatestVersion
            NewVersion = $pcLatestVersion
        }
    }

    $serverLatestVersion = $latestFile -replace $credentials.fileVersionPattern, '$1'

    $updateNeeded = Compare-Versions -pcLatestVersion $pcLatestVersion -serverLatestVersion $serverLatestVersion

    if ($updateNeeded) {
        $localFilePath = Join-Path -Path $credentials.localPath -ChildPath $latestFile
        if (-not (Test-Path $localFilePath)) {
            if ($method -eq "http") {
                Save-HttpFile `
                    -httpUrl $credentials.httpUrl `
                    -httpLatestFile $latestFile `
                    -localFilePath $localFilePath
            } else {
                Save-FtpFile `
                    -ftpUrl $credentials.ftpUrl `
                    -ftpFolder $credentials.ftpFolder `
                    -ftpLatestFile $latestFile `
                    -localFilePath $localFilePath `
                    -username $credentials.username `
                    -password $credentials.password
            }
        }
        Update-Program -localFilePath $localFilePath -pcLatestProgram $pcLatestProgram

        $newStatus = Test-UpdateStatus `
            -productName $credentials.productName `
            -productVersionPattern $credentials.productVersionPattern `
            -serverLatestVersion $serverLatestVersion
        $newVersion = Get-PcLatestVersion `
            -pcLatestProgram (Get-PcLatestProgram($credentials.productName)) `
            -productVersionPattern $credentials.productVersionPattern

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