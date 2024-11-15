# Определяем путь к директории скрипта
$scriptPath = $PSScriptRoot

# Подключаем модули
. "$scriptPath\psscripts\BimloadCore.ps1"
. "$scriptPath\psscripts\HttpOperations.ps1"
. "$scriptPath\psscripts\FtpOperations.ps1"
. "$scriptPath\psscripts\BimloadGui.ps1"

function Start-BimloadApplication {
    Write-Host "Запуск Bimload..."

    # Создаем синхронизированный хэш для отображения логов и состояния
    $syncHash = [hashtable]::Synchronized(@{})
    $syncHash.Host = $Host
    $syncHash.Runspace = [runspacefactory]::CreateRunspace()
    $syncHash.Runspace.ApartmentState = "STA"
    $syncHash.Runspace.ThreadOptions = "ReuseThread"
    $syncHash.Runspace.Open()
    $syncHash.Runspace.SessionStateProxy.SetVariable("syncHash", $syncHash)
    
    # Запускаем графический интерфейс
    try {
        Show-UpdateInterface -SyncHash $syncHash
    }
    catch {
        $errorMessage = $_.Exception.Message
        $errorLineNumber = $_.InvocationInfo.ScriptLineNumber
        $errorFileName = $_.InvocationInfo.ScriptName
        $errorOffsetInLine = $_.InvocationInfo.OffsetInLine

        Write-Log -Message "Произошла ошибка:`n$errorMessage`nФайл: $errorFileName`nСтрока: $errorLineNumber`nПозиция: $errorOffsetInLine" -Mode error
        
        # Вывод полного стека вызовов
        Write-Log "Стек вызовов:"
        $_.ScriptStackTrace
    }
}

# Запускаем приложение
Start-BimloadApplication