# Определяем путь к директории скрипта
$scriptPath = $PSScriptRoot

# Подключаем модули
. "$scriptPath\psscripts\BimloadCore.ps1"
. "$scriptPath\psscripts\HttpOperations.ps1"
. "$scriptPath\psscripts\FtpOperations.ps1"
. "$scriptPath\psscripts\BimloadGui.ps1"

function Start-BimloadApplication {
    Write-Host "Запуск Bimload..."

    # Создаем синхронизированный хэш для отображения логов
    $syncHash = [hashtable]::Synchronized(@{})
    
    # Запускаем графический интерфейс
    try {
        Show-UpdateInterface -SyncHash $syncHash
    }
    catch {
        $errorMessage = $_.Exception.Message
        $errorLineNumber = $_.InvocationInfo.ScriptLineNumber
        $errorFileName = $_.InvocationInfo.ScriptName
        $errorOffsetInLine = $_.InvocationInfo.OffsetInLine

        Write-Log -Message "Произошла ошибка:`n$errorMessage`nФайл: $errorFileName`nСтрока: $errorLineNumber`nПозиция: $errorOffsetInLine" -Color ([System.Drawing.Color]::Red) -Bold $true
        
        # Вывод полного стека вызовов
        Write-Host "Стек вызовов:"
        $_.ScriptStackTrace
    }
}

# Запускаем приложение
Start-BimloadApplication