function Save-HttpFile($httpUrl, $httpLatestFile, $localFilePath) {
    $downloadUrl = [Uri]::new([Uri]$httpUrl, $httpLatestFile).AbsoluteUri

    Write-Log -Message "7 - Начало скачивания файла '$downloadUrl'..."

    try {
        $ProgressPreference = 'SilentlyContinue'  # Отключаем индикатор прогресса
        Invoke-WebRequest -Uri $downloadUrl -OutFile $localFilePath
        Write-Log -Message " Завершено." -Mode success
        
        if (Test-Path -Path $localFilePath) {
            Write-Log -Message "Файл успешно скачан в $localFilePath"
        } else {
            Write-Log -Message "Не удалось найти скачанный файл в $localFilePath" -Mode success
        }
    }
    catch {
        Write-Log -Message "Ошибка при скачивании файла: $_" -Mode error
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
        Write-Log "4 - Будем искать файлы на https: $httpUrl"
        $webClient = New-Object System.Net.WebClient
        $htmlContent = $webClient.DownloadString($httpUrl)

        # Используем регулярные выражения для получения текста
        $regexMatches = [regex]::Matches($htmlContent, $httpPattern)

        if ($regexMatches.Count -gt 0) {
            # Извлекаем последний матч
            $lastMatch = $regexMatches[$regexMatches.Count - 1]

            # Извлекаем текст из последнего матча
            $latestFile = $lastMatch.Groups[1].Value

            Write-Log "Файл на https найден: $latestFile"
            return $latestFile
        } else {
            Write-Log "Не найдено файлов на https!" -Mode error
            return $null
        }
    }
    catch {
        Write-Log "Ошибка при загрузке или парсинге html: $_" -Mode error
        return $null
    }
    finally {
        if ($webClient) {
            $webClient.Dispose()
        }
    }
}