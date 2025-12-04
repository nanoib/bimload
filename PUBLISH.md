# Инструкция по публикации Bimload

## Быстрый старт

Для создания self-contained exe файла выполните одну из команд:

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
dotnet publish Bimload.Gui\Bimload.Gui.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o publish
```

## Результат

После публикации в папке `publish/` будет:
- **Bimload.Gui.exe** — самодостаточный исполняемый файл (~162 МБ)
- **creds/** — папка с конфигурационными файлами

## Распространение

Скопируйте `Bimload.Gui.exe` и папку `creds/` в нужное место. Приложение автоматически найдет папку `creds/` рядом с exe файлом.

## Подробная документация

См. [doc/README-04-Publishing.md](doc/README-04-Publishing.md) для подробной информации.

