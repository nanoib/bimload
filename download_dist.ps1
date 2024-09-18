# Скачивание последней версии на FTP
$credentialsPath = "credentials"
$credentials = Get-Content $credentialsPath | ConvertFrom-StringData
$ftpUrl = $credentials.ftpUrl
$username = $credentials.username
$password = $credentials.password
$localPath = $credentials.localPath

# Создайте объект FtpWebRequest
$ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUrl)
$ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
$ftpRequest.Credentials = New-Object System.Net.NetworkCredential($username, $password)

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

    Write-Output "Список файлов: $filesList"

    # Сортируем файлы по алфавиту и выбираем последний
    $latestFile = $filesList | Sort-Object | Select-Object -Last 1

    # Удаляем лишние пробелы и символы новой строки из имени файла
    $latestFile = $latestFile.Trim()

    # Полный путь к локальному файлу
    $localFilePath = Join-Path -Path $localPath -ChildPath $latestFile

    # Проверка наличия файла в локальной папке
    if (Test-Path -Path $localFilePath) {
        Write-Output "На ftp нет новых файлов!"
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
    }
} catch {
    Write-Error "Ошибка: $_"
}