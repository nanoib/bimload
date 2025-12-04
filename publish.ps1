Write-Host "Publishing Bimload.Gui as framework-dependent executable..." -ForegroundColor Green
Write-Host ""

dotnet publish Bimload.Gui\Bimload.Gui.csproj `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o publish

Write-Host ""
Write-Host "Build completed! Executable is in: publish\Bimload.exe" -ForegroundColor Green
Write-Host "Note: .NET 8.0 Runtime must be installed on the target machine." -ForegroundColor Yellow
Write-Host ""

