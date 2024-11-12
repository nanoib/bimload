function Save-HttpFile($httpUrl, $httpLatestFile, $localFilePath) {
    $downloadUrl = [Uri]::new([Uri]$httpUrl, $httpLatestFile).AbsoluteUri

    Write-Log -Message "7 - Начало скачивания файла '$downloadUrl'..."

    try {
        $ProgressPreference = 'SilentlyContinue'  # Отключаем индикатор прогресса
        Invoke-WebRequest -Uri $downloadUrl -OutFile $localFilePath
        Write-Log -Message " Завершено." -Color ([System.Drawing.Color]::Green)
        
        if (Test-Path -Path $localFilePath) {
            Write-Log -Message "Файл успешно скачан в $localFilePath"
        } else {
            Write-Log -Message "Не удалось найти скачанный файл в $localFilePath" -Color ([System.Drawing.Color]::Green)
        }
    }
    catch {
        Write-Log -Message "Ошибка при скачивании файла: $_" -Color ([System.Drawing.Color]::Red)
    }
    finally {
        $ProgressPreference = 'Continue'  # Возвращаем стандартное значение
    }
}

function Get-HttpLatestFile{
    param (
        [string]$httpUrl,
        [string]$httpPattern
    )
    $webClient = $null
    try {
        # Скачиваем html
        Write-Host "4 - Будем искать файлы на https: $httpUrl"
        $webClient = New-Object System.Net.WebClient
        $htmlContent = $webClient.DownloadString($httpUrl)

        # Используем регулярные выражения для получения текста
        $regexMatches = [regex]::Matches($htmlContent, $httpPattern)

        if ($regexMatches.Count -gt 0) {
            # Извлекаем последний матч
            $lastMatch = $regexMatches[$regexMatches.Count - 1]

            # Извлекаем текст из последнего матча
            $latestFile = $lastMatch.Groups[1].Value

            Write-Host "Файл на https найден: $latestFile"
            return $latestFile
        } else {
            Write-Host "Не найдено файлов на https!"
            return $null
        }
    }
    catch {
        Write-Host "Ошибка при загрузке или парсинге html: $_"
        return $null
    }
    finally {
        if ($webClient) {
            $webClient.Dispose()
        }
    }
}