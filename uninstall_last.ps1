# Получаем все установленные версии программы
$programs = @(Get-WmiObject -Class Win32_Product |
Where-Object { $_.Name -like "*BIM Вентиляция*" } |
Sort-Object { [version]$_.Version } -Descending)

# Проверяем, найдены ли программы
if ($programs.Count -eq 0) {
    Write-Host "Программа 'BIM Вентиляция' не найдена."
    return
}

# Выводим найденные версии
Write-Host "Найдены следующие версии программы 'BIM Вентиляция':"
$programs | ForEach-Object { Write-Host "Версия: $($_.Version)" }

# Определяем старшую версию для удаления
$latestVersion = $programs[0]
Write-Host "Будет удалена версия: $($latestVersion.Version)"

# Запрашиваем подтверждение у пользователя
$confirmation = Read-Host "Вы уверены, что хотите удалить эту версию? (да/нет)"

if ($confirmation -eq "да") {
    # Удаляем программу
    $latestVersion.Uninstall()
    Write-Host "Версия $($latestVersion.Version) успешно удалена."
} else {
    Write-Host "Удаление отменено."
}