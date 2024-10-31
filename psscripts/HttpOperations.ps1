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