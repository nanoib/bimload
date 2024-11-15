function Save-FtpFile {
    param (
        [string]$ftpUrl,
        [string]$ftpFolder,
        [string]$ftpLatestFile,
        [string]$localFilePath,
        [string]$username,
        [SecureString]$password,
        $syncHash
    )

    $uri = "$ftpUrl/$ftpFolder/$ftpLatestFile"
    Write-Log -Message "Начинаем загрузку файла $uri"

    $job = Start-Job -ScriptBlock {
        param([string]$uri, [string]$localFilePath, [string]$username, [SecureString]$password)
        
        try {
            $webClient = New-Object System.Net.WebClient
            $webClient.Credentials = New-Object System.Net.NetworkCredential($username, $password)
            $webClient.DownloadFile($uri, $localFilePath)
            return @{ Success = $true; Message = "Файл успешно скачан в $localFilePath" }
        }
        catch {
            return @{ Success = $false; Message = "Ошибка при загрузке файла: $_" }
        }
        finally {
            if ($webClient) {
                $webClient.Dispose()
            }
        }
    } -ArgumentList $uri, $localFilePath, $username, $password

    while ($job.State -eq 'Running') {
        Start-Sleep -Milliseconds 100
        [System.Windows.Forms.Application]::DoEvents()
    }

    $result = Receive-Job -Job $job
    Remove-Job -Job $job

    if ($result.Success) {
        Write-Log -Message $result.Message
        return $true
    } else {
        Write-Log -Message $result.Message -Mode error
        return $false
    }
}

function Get-FtpLatestFile {
    param (
        $ftpUrl,
        $ftpFolder,
        $username,
        [SecureString]$password,
        $syncHash
    )

    
    $job = Start-Job -ScriptBlock {
        param($ftpUrl, $ftpFolder, $username, [SecureString]$password)
        
        try {
            $ftpPath = [Uri]::new([Uri]$ftpUrl, $ftpFolder).AbsoluteUri
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
    
            return @{ Success = $true; LatestFile = $ftpLatestFile; FilesCount = $filesList.Count }
        }
        catch {
            return @{ Success = $false; Message = "Ошибка при получении списка файлов с FTP: $_" }
        }
    } -ArgumentList $ftpUrl, $ftpFolder, $username, $password

    while ($job.State -eq 'Running') {
        Start-Sleep -Milliseconds 100
        [System.Windows.Forms.Application]::DoEvents()
    }

    $result = Receive-Job -Job $job
    Remove-Job -Job $job

    if ($result.Success) {
        Write-Log -Message "Найдено файлов и папок на FTP: $($result.FilesCount)"
        Write-Log -Message "Файл с последней сборкой: $($result.LatestFile)"
        return $result.LatestFile
    } else {
        Write-Log -Message $result.Message -Mode error
        return $null
    }
}