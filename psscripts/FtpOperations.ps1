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

function Get-FtpLatestFile($ftpUrl, $ftpFolder, $username, [SecureString]$password) {
    $maxAttempts = 1
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