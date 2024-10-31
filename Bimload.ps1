# Определяем путь к директории скрипта
$scriptPath = $PSScriptRoot

# Подключаем модули
. "$scriptPath\psscripts\BimloadCore.ps1"
. "$scriptPath\psscripts\HttpOperations.ps1"
. "$scriptPath\psscripts\FtpOperations.ps1"
. "$scriptPath\psscripts\BimloadGui.ps1"

function Start-BimloadApplication {
    Write-Host "Запуск Bimload..."
    
    # Запускаем графический интерфейс
    try {
        Show-UpdateInterface
    }
    catch {
        $errorMessage = $_.Exception.Message
        $errorLineNumber = $_.InvocationInfo.ScriptLineNumber
        $errorFileName = $_.InvocationInfo.ScriptName
        $errorOffsetInLine = $_.InvocationInfo.OffsetInLine

        Write-Error "Произошла ошибка:`n$errorMessage`nФайл: $errorFileName`nСтрока: $errorLineNumber`nПозиция: $errorOffsetInLine"
        
        # Вывод полного стека вызовов
        Write-Host "Стек вызовов:"
        $_.ScriptStackTrace
    }
}

# Запускаем приложение
Start-BimloadApplication