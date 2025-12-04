Write-Host "Publishing Bimload.Gui as self-contained executable..." -ForegroundColor Green
Write-Host ""

dotnet publish Bimload.Gui\Bimload.Gui.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o publish

Write-Host ""
Write-Host "Build completed! Executable is in: publish\Bimload.Gui.exe" -ForegroundColor Green
Write-Host ""

