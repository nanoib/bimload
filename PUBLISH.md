# Инструкция по публикации Bimload

## Быстрый старт

Для создания framework-dependent exe файла выполните одну из команд:

### PowerShell
```powershell
.\publish.ps1
```

### Batch (CMD)
```cmd
publish.bat
```

### Вручную
```powershell
dotnet publish Bimload.Gui\Bimload.Gui.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o publish
```

## Результат

После публикации в папке `publish/` будет:
- **Bimload.exe** — исполняемый файл (~5-10 МБ)
- **creds/** — папка с конфигурационными файлами

## Требования

**Важно:** На целевом компьютере должен быть установлен **.NET 8.0 Desktop Runtime**. 

Если .NET Runtime отсутствует, приложение покажет сообщение об ошибке со ссылкой на скачивание.

## Распространение

Скопируйте `Bimload.exe` и папку `creds/` в нужное место. Приложение автоматически найдет папку `creds/` рядом с exe файлом.

## Подробная документация

См. [doc/README-04-Publishing.md](doc/README-04-Publishing.md) для подробной информации.

