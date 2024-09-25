function Get-Credentials($fileFolder, $fileName) {
    $ftpCredentialsPath = Join-Path -Path $fileFolder -ChildPath "ftp"
    $otherCredentialsPath = Join-Path -Path $fileFolder -ChildPath $fileName
    
    $ftpCredentials = Get-Content $ftpCredentialsPath | ConvertFrom-StringData
    $otherCredentials = Get-Content $otherCredentialsPath | ConvertFrom-StringData
    
    $credentials = @{
        ftpUrl = $ftpCredentials.ftpUrl
        username = $ftpCredentials.username
        password = $ftpCredentials.password
        ftpFolder=$otherCredentials.ftpFolder
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

function Get-FtpLatestFile($ftpUrl, $ftpFolder, $username, [SecureString]$password) {
    $maxAttempts = 3
    $attempt = 0
    
    while ($attempt -lt $maxAttempts) {
        try {
            # Составляем полный путь к файлу на FTP
            $ftpPath = [Uri]::new([Uri]$ftpUrl, $ftpFolder).AbsoluteUri
            Write-Host "4 - Подключаемся к FTP: $ftpPath"
            
            # Объект FtpWebRequest
            $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpPath)
            $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
            $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($username, $password)
            
            # Ответ от сервера
            $ftpResponse = $ftpRequest.GetResponse()
            $responseStream = $ftpResponse.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($responseStream)

            # Чтение списка файлов
            $filesList = $reader.ReadToEnd().Split("`n") | Where-Object { $_ -ne "" }
    
            # Закрытие потоков
            $reader.Close()
            $responseStream.Close()
            $ftpResponse.Close()
    
            # Сортируем файлы по алфавиту и выбираем последний
            $ftpLatestFile = $filesList | Sort-Object | Select-Object -Last 1
    
            # Удаляем лишние пробелы и символы новой строки из имени файла
            $ftpLatestFile = $ftpLatestFile.Trim()
    
            Write-Host "Найдено файлов и папок на FTP: $($filesList.Count)"
            Write-Host "Файл с последней сборкой: $ftpLatestFile"
            
            return $ftpLatestFile
        } 
        catch {
            $attempt++
            Write-Warning "Попытка $attempt из $maxAttempts не удалась. Ошибка: $_"
            if ($attempt -lt $maxAttempts) {
                Start-Sleep -Seconds 5  # Ждем 5 секунд перед следующей попыткой
            }
        }
    }
    
    Write-Error "Не удалось получить список файлов с FTP после $maxAttempts попыток."
    return $null
}

function Compare-Versions($pcLatestVersion, $ftpLatestFile, $fileVersionPattern) {
    # Извлекаем версию из имени файла
    $ftpLatestVersion = $ftpLatestFile -replace $fileVersionPattern, '$1'

    # Проверка необходимости установки новой версии
    if ([int]$pcLatestVersion -ge [int]$ftpLatestVersion) {
        Write-Host "5 - Решено - установленная версия ($pcLatestVersion) самая новая. Версия на FTP та же или младше ($ftpLatestVersion)."
        return $false
    } else {
        Write-Host "5 - Решено - установленная версия ($pcLatestVersion) устаревшая. Доступна новая версия на FTP ($ftpLatestVersion)."
        return $true
    }
}

function Save-FtpFile($ftpUrl, $ftpFolder, $ftpLatestFile, $localFilePath, $username, [SecureString]$password) {
    # Скачивание файла
    $downloadFtpPath = "$ftpFolder$ftpLatestFile"
    $downloadUrl = [Uri]::new([Uri]$ftpUrl, $downloadFtpPath).AbsoluteUri

    Write-Output "7 - Начало скачивания файла '$downloadFtpPath'..."

    $webClient = New-Object System.Net.WebClient
    $webClient.Credentials = New-Object System.Net.NetworkCredential($username, $password)

    try {
        $webClient.DownloadFile($downloadUrl, $localFilePath)
        
        if (Test-Path -Path $localFilePath) {
            Write-Output "Файл успешно скачан в $localFilePath"
        } else {
            Write-Error "Не удалось найти скачанный файл в $localFilePath"
        }
    }
    catch {
        Write-Error "Ошибка при скачивании файла: $_"
    }
    finally {
        $webClient.Dispose()
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



function Update-Bim {
    param (
        $fileFolder,
        $credFileName
    )


    # Чтение пользовательских данных
    $credentials = Get-Credentials -fileFolder $fileFolder -fileName $credFileName
    $securePassword = ConvertTo-SecureString $credentials.password -AsPlainText -Force

    # Получение установленных программ
    $pcLatestProgram = Get-PcLatestProgram($credentials.productName) 

    # Получение последней установленной версии программы 
    $pcLatestVersion = Get-PcLatestVersion -pcLatestProgram $pcLatestProgram -productVersionPattern $credentials.productVersionPattern

    # Получение последней FTP версии программы
    $ftpLatestFile = Get-FtpLatestFile `
        -ftpUrl $credentials.ftpUrl `
        -ftpFolder $credentials.ftpFolder `
        -username $credentials.username `
        -password $securePassword
        
    # Сравнение версии и завершение выполнения скрипта при необходимости
    $updateNeeded = Compare-Versions `
        -pcLatestVersion $pcLatestVersion `
        -ftpLatestFile $ftpLatestFile `
        -fileVersionPattern $credentials.fileVersionPattern
        
    # Скачивание файла
    if ($updateNeeded) {
        # Составляем путь к локальному файлу
        $localFilePath = Join-Path -Path $credentials.localPath -ChildPath $ftpLatestFile
        Write-Host "6 - Ищем файл на диске: $localFilePath"

        if (Test-Path $localFilePath) {
            Write-Host "7 - Файл уже есть на диске и будет установлен оттуда"
        } else {
            # Скачивание файла
            Write-Host "Файл не найден на диске будем скачивать"
            Save-FtpFile `
            -ftpUrl $credentials.ftpUrl `
            -ftpFolder $credentials.ftpFolder `
            -ftpLatestFile $ftpLatestFile `
            -localFilePath $localFilePath `
            -username $credentials.username `
            -password $securePassword
        }
        # Переустановка программы
        Update-Program -localFilePath $localFilePath -pcLatestProgram $pcLatestProgram

    } else {
        Write-Host "6 - Обновление не требуется"
    }

}

$fileFolder = Join-Path -Path $PSScriptRoot -ChildPath "creds"

Get-ChildItem -Path $fileFolder -Filter "*.credentials" | 
ForEach-Object {
    $credFileName = $_.Name
    Write-Host "1 - Обработка файла: $credFileName"
    Update-Bim -fileFolder $fileFolder -credFileName $credFileName
    Write-Host "10 - Завершена обработка файла: $credFileName"
    Write-Output ""
    Write-Output ""

}