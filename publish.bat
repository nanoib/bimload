@echo off
echo Publishing Bimload.Gui as self-contained executable...
echo.

dotnet publish Bimload.Gui\Bimload.Gui.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:DebugType=None ^
    -p:DebugSymbols=false ^
    -o publish

echo.
echo Build completed! Executable is in: publish\Bimload.Gui.exe
echo.
pause

