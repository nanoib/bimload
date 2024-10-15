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


function Compare-Versions($pcLatestVersion, $httpLatestFile, $fileVersionPattern) {
    # Извлекаем версию из имени файла
    $httpLatestVersion = $httpLatestFile -replace $fileVersionPattern, '$1'

    # Проверка необходимости установки новой версии
    if ([int]$pcLatestVersion -ge [int]$httpLatestVersion) {
        Write-Host "5 - Решено - установленная версия ($pcLatestVersion) самая новая. Версия на https та же или младше ($httpLatestVersion)."
        return $false
    } else {
        Write-Host "5 - Решено - установленная версия ($pcLatestVersion) устаревшая. Доступна новая версия на https ($httpLatestVersion)."
        return $true
    }
}

function Save-HttpFile($httpUrl, $httpLatestFile, $localFilePath) {
    # Скачивание файла
    $downloadUrl = [Uri]::new([Uri]$httpUrl, $httpLatestFile).AbsoluteUri

    Write-Output "7 - Начало скачивания файла '$downloadUrl'..."

    $webClient = New-Object System.Net.WebClient

    try {
        $webClient.DownloadFileTaskAsync($downloadUrl, $localFilePath).GetAwaiter().GetResult()
        
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

    # Получение установленных программ
    $pcLatestProgram = Get-PcLatestProgram($credentials.productName) 

    # Получение последней установленной версии программы 
    $pcLatestVersion = Get-PcLatestVersion -pcLatestProgram $pcLatestProgram -productVersionPattern $credentials.productVersionPattern

    # Получение последней https версии программы
    $httpLatestFile = Get-HttpLatestFile -httpUrl $credentials.httpUrl
        
    # Сравнение версии и завершение выполнения скрипта при необходимости
    $updateNeeded = Compare-Versions `
        -pcLatestVersion $pcLatestVersion `
        -httpLatestFile $httpLatestFile `
        -fileVersionPattern $credentials.fileVersionPattern
        
    # Скачивание файла
    if ($updateNeeded) {
        # Составляем путь к локальному файлу
        $localFilePath = Join-Path -Path $credentials.localPath -ChildPath $httpLatestFile
        Write-Host "6 - Ищем файл на диске: $localFilePath"

        if (Test-Path $localFilePath) {
            Write-Host "7 - Файл уже есть на диске и будет установлен оттуда"
        } else {
            # Скачивание файла
            Write-Host "Файл не найден на диске будем скачивать"
            Save-HttpFile `
            -httpUrl $credentials.httpUrl `
            -httpLatestFile $httpLatestFile `
            -localFilePath $localFilePath `
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