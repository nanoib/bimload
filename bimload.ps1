# Скачивание последней версии на FTP
$credentialsPath = "C:\\repos\\bimload\\credentials"
$credentials = Get-Content $credentialsPath | ConvertFrom-StringData
$ftpUrl = $credentials.ftpUrl
$username = $credentials.username
$password = $credentials.password
$localPath = $credentials.localPath
$productName = $credentials.productName
$fileVersionPattern = $credentials.fileVersionPattern
$productVersionPattern = $credentials.productVersionPattern

# Создайте объект FtpWebRequest
$ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUrl)
$ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
$ftpRequest.Credentials = New-Object System.Net.NetworkCredential($username, $password)

# Получаем все установленные версии программы
$programs = @(Get-WmiObject -Class Win32_Product |
Where-Object { $_.Name -like "*$productName*" } |
Sort-Object { [version]$_.Version } -Descending)

if ($programs.Count -eq 0) {
    Write-Host "В списке приложений по запросу '$productName' ничо не найдено."
    $latestVersion = @{ Version = "0.0.0.00000" }
} else {
    # Выводим найденные версии
    Write-Host "Найденные версии приложений по запросу '$productName':"
    $programs | ForEach-Object { Write-Host "Версия: $($_.Version)" }

    # Определяем старшую версию
    $latestVersion = $programs[0]
}

try {
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

    Write-Output "Список файлов на ftp:"
        Write-Output $filesList

    # Сортируем файлы по алфавиту и выбираем последний
    $latestFile = $filesList | Sort-Object | Select-Object -Last 1

    # Удаляем лишние пробелы и символы новой строки из имени файла
    $latestFile = $latestFile.Trim()
    
    # Извлекаем версию из имени файла
    $ftpVersion = $latestFile -replace $fileVersionPattern, '$1'

    # Извлекаем версию установленной программы
    $installedVersion = $latestVersion.Version -replace $productVersionPattern, '$1'

    # Сравниваем версии
    if ([int]$installedVersion -ge [int]$ftpVersion) {
        Write-Output "Установленная версия ($installedVersion) новее или равна версии на FTP ($ftpVersion). Скачивание не требуется."
        return
    } else {
        Write-Output "Доступна новая версия ($ftpVersion). Текущая установленная версия: $installedVersion"
    }

    # Полный путь к локальному файлу
    $localFilePath = Join-Path -Path $localPath -ChildPath $latestFile

    # Проверка наличия файла в локальной папке
    if (Test-Path -Path $localFilePath) {
        Write-Output "Файл $latestFile уже есть на диске. На FTP нет новых файлов!"
    } else {
        # Скачивание файла
        $downloadUrl = "$ftpUrl$latestFile"
        $webClient = New-Object System.Net.WebClient
        $webClient.Credentials = New-Object System.Net.NetworkCredential($username, $password)

        Write-Output "Начало скачивания файла '$latestFile'..."

        # Запуск загрузки файла асинхронно
        $webClient.DownloadFileAsync($downloadUrl, $localFilePath)

        # Ожидание завершения загрузки
        while ($webClient.IsBusy) {
            Start-Sleep -Seconds 1
        }

        if (Test-Path -Path $localFilePath) {
        Write-Output "Файл $latestFile успешно скачан."
        }
    }
    # Если файл был успешно скачан
    if (Test-Path -Path $localFilePath) {
        
        # Удаление текущей версии программы
        if ($programs.Count -gt 0) {
            $latestVersion = $programs[0]
            Write-Output "Удаление текущей версии программы: $($latestVersion.Version)"
            $latestVersion.Uninstall()

        } else {
            Write-Output "Нет установленной версии для удаления."
        }

        # Установка новой версии программы
        Write-Output "Начало установки новой версии программы..."
        try {
            Start-Process -FilePath $localFilePath -ArgumentList "/quiet" -Wait -NoNewWindow
            Write-Output "Новая версия программы успешно установлена."
        } catch {
            Write-Error "Ошибка при установке новой версии программы: $_"
        }
        
        # Здесь можно добавить код для установки новой версии
    } else {
        Write-Output "Файл $latestFile не найден."
    }

} catch {
    Write-Error "Ошибка: $_"
}