function Save-FtpFile {
    param (
        [string]$ftpUrl,
        [string]$ftpFolder,
        [string]$ftpLatestFile,
        [string]$localFilePath,
        [string]$username,
        [SecureString]$password
    )

    $uri = "$ftpUrl/$ftpFolder/$ftpLatestFile"
    $webClient = New-Object System.Net.WebClient
    $webClient.Credentials = New-Object System.Net.NetworkCredential($username, $password)

    Write-Log -Message "Начинаем загрузку файла $uri"

    $downloadTask = $webClient.DownloadFileTaskAsync($uri, $localFilePath)

    while (-not $downloadTask.IsCompleted) {
        Start-Sleep -Milliseconds 100
        [System.Windows.Forms.Application]::DoEvents()
    }

    if ($downloadTask.IsFaulted) {
        Write-Log -Message "Ошибка при загрузке файла: $($downloadTask.Exception.InnerException.Message)" -Color ([System.Drawing.Color]::Red) -Bold $true
        return $false
    }

    if (Test-Path -Path $localFilePath) {
        Write-Log -Message "Файл успешно скачан в $localFilePath"
    } else {
        Write-Log -Message "Не удалось найти скачанный файл в $localFilePath" -Color ([System.Drawing.Color]::Red) -Bold $true
    }
    return $true
}


function Get-FtpLatestFile($ftpUrl, $ftpFolder, $username, [SecureString]$password) {
    $maxAttempts = 1
    $attempt = 0
    
    while ($attempt -lt $maxAttempts) {
        try {
            # Составляем полный путь к файлу на FTP
            $ftpPath = [Uri]::new([Uri]$ftpUrl, $ftpFolder).AbsoluteUri
            Write-Log -Message "Подключаемся к FTP: $ftpPath"
            
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
    
            Write-Log -Message "Найдено файлов и папок на FTP: $($filesList.Count)"
            Write-Log -Message "Файл с последней сборкой: $ftpLatestFile"
            
            return $ftpLatestFile
        } 
        catch {
            $attempt++
            Write-Log -Message "Попытка $attempt из $maxAttempts не удалась. Ошибка: $_"
            if ($attempt -lt $maxAttempts) {
                Start-Sleep -Seconds 5  # Ждем 5 секунд перед следующей попыткой
            }
        }
    }
    
    Write-Log -Message "Не удалось получить список файлов с FTP после $maxAttempts попыток." -Color ([System.Drawing.Color]::Red) -Bold $true
    return $null
}