@echo off
echo Publishing Bimload.Gui as framework-dependent executable...
echo.

dotnet publish Bimload.Gui\Bimload.Gui.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained false ^
    -p:PublishSingleFile=true ^
    -p:DebugType=None ^
    -p:DebugSymbols=false ^
    -o publish

echo.
echo Build completed! Executable is in: publish\Bimload.exe
echo Note: .NET 8.0 Runtime must be installed on the target machine.
echo.
pause

