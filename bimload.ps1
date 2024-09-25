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
    $programs = @(Get-WmiObject -Class Win32_Product |
    Where-Object { $_.Name -like "*$productName*" } |
    Sort-Object { [version]$_.Version } -Descending)
    if ($programs.Count -eq 0) {
        Write-Host "В списке приложений по запросу '$productName' ничо не найдено."
        $pcLatestProgram = $false
    } else {
        # Выводим найденные версии
        Write-Host "Найденные версии приложений по запросу '$productName':"
        $programs | ForEach-Object { Write-Host "Версия: $($_.Version)" }

        # Определяем старшую версию
        $pcLatestProgram = $programs[0]

    return $pcLatestProgram
    }   
}

function Get-PcLatestVersion($pcLatestProgram, $productVersionPattern) {
    # Извлекаем версию установленной программы
    $pcLatestVersion = $pcLatestProgram.Version -replace $productVersionPattern, '$1'
    return $pcLatestVersion
}

function Get-FtpLatestFile($ftpUrl, $ftpFolder, $username, [SecureString]$password) {
    try {
        # Составляем полный путь к файлу на FTP
        $ftpPath = [Uri]::new([Uri]$ftpUrl, $ftpFolder).AbsoluteUri
        # Создайте объект FtpWebRequest
        $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpPath)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($username, $password)
        # Получите ответ от сервера
        $ftpResponse = $ftpRequest.GetResponse()
        $responseStream = $ftpResponse.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($responseStream)

        # Чтение списка файлов
        $filesList = $reader.ReadToEnd().Split("`n") | Where-Object { $_ -ne "" }

        # Закрытие потоков
        $reader.Close()
        $responseStream.Close()
        $ftpResponse.Close()

        Write-Host "Список файлов на ftp:"
        Write-Host ($filesList -join "`n")

        # Сортируем файлы по алфавиту и выбираем последний
        $ftpLatestFile = $filesList | Sort-Object | Select-Object -Last 1

        # Удаляем лишние пробелы и символы новой строки из имени файла
        $ftpLatestFile = $ftpLatestFile.Trim()

        return $ftpLatestFile
    } catch {
        Write-Error "Ошибка при получении списка файлов с FTP: $_"
        return @()
    }
}


function Save-FtpFile($ftpUrl, $ftpFolder, $ftpLatestFile, $localFilePath, $username, [SecureString]$password) {
    # Скачивание файла
    $downloadFtpPath = "$ftpFolder$ftpLatestFile"
    $downloadUrl = [Uri]::new([Uri]$ftpUrl, $downloadFtpPath).AbsoluteUri
    $webClient = New-Object System.Net.WebClient
    $webClient.Credentials = New-Object System.Net.NetworkCredential($username, $password)

    Write-Output "Начало скачивания файла '$ftpLatestFile'..."

    # Запуск загрузки файла асинхронно
    $webClient.DownloadFileAsync($downloadUrl, $localFilePath)

    # Ожидание завершения загрузки
    while ($webClient.IsBusy) {
        Start-Sleep -Seconds 1
    }

    if (Test-Path -Path $localFilePath) {
        Write-Output "Файл $ftpLatestFile успешно скачан."
    }
}


function Uninstall-Program($pcLatestProgram) {
    Write-Output "Удаление текущей версии программы: $($pcLatestProgram.Version)"
    $result = $pcLatestProgram.Uninstall()
    if ($result.ReturnValue -ne 0) {
        Write-Error "Ошибка при удалении программы. Код ошибки: $($result.ReturnValue)"
        exit 1
    }
}

function Install-Program($localFilePath) {
    Write-Output "Начало установки новой версии программы..."
    try {
        Start-Process -FilePath $localFilePath -ArgumentList "/quiet" -Wait -NoNewWindow
        Write-Output "Новая версия программы успешно установлена."
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
            Write-Output "Не определена программа для удаления."
        }

        # Установка новой версии программы
        Install-Program($localFilePath)
    } else {
        Write-Output "Файл $localFilePath не найден или путь недействителен."
    }
}

function Compare-Versions($pcLatestVersion, $ftpLatestFile, $fileVersionPattern) {
    # Извлекаем версию из имени файла
    $ftpLatestVersion = $ftpLatestFile -replace $fileVersionPattern, '$1'

    # Проверка необходимости установки новой версии
    if ([int]$pcLatestVersion -ge [int]$ftpLatestVersion) {
        Write-Host "Установленная версия ($pcLatestVersion) самая новая. Версия на FTP та же или младше ($ftpLatestVersion). Скачивание не требуется."
        return $false
    } else {
        Write-Host "Установленная версия ($pcLatestVersion) устаревшая. Доступна новая версия на FTP ($ftpLatestVersion)."
        return $true
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
        
    # Составляем путь к локальному файлу
    $localFilePath = Join-Path -Path $credentials.localPath -ChildPath $ftpLatestFile

    # Скачивание файла
    if ($updateNeeded) {
        Write-Output "Ищем файл на диске..."
        if (Test-Path $localFilePath) {
            Write-Output "Файл $ftpLatestFile уже есть на диске и будет установлен оттуда"
        } else {
            # Скачивание файла
            Write-Output "Файл $ftpLatestFile не найден на диске"
            Save-FtpFile `
            -ftpUrl $credentials.ftpUrl `
            -ftpFolder $credentials.ftpFolder `
            -ftpLatestFile $ftpLatestFile `
            -localFilePath $localFilePath `
            -username $credentials.username `
            -password $securePassword
        }
    } else {
        Write-Output "Обновление не требуется. Выход"
        exit 0
    }

    Write-Output "Значение localFilePath: $localFilePath"
    # Переустановка программы
    Update-Program -localFilePath $localFilePath -pcLatestProgram $pcLatestProgram
}

$fileFolder = Join-Path -Path $PSScriptRoot -ChildPath "creds"

Get-ChildItem -Path $fileFolder -Filter "*.credentials" | 
ForEach-Object {
    $credFileName = $_.Name
    Write-Output "Обработка файла: $credFileName"
    Update-Bim -fileFolder $fileFolder -credFileName $credFileName
}