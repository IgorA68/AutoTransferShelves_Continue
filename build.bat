@echo off

:: === НАСТРОЙКИ ===
set "MOD_NAME=AutoTransferShelves"
set "RIMWORLD_PATH=G:\RimWorld"
set "CS_PROJ=Source\%MOD_NAME%.csproj"
set "BUILD_PROPS=Source\Directory.Build.props"
set "COPY_SCRIPT=copy_to_mod.bat"

:: === ГЕНЕРАЦИЯ Directory.Build.props ===
echo Generating Directory.Build.props with RimWorld path: %RIMWORLD_PATH%
(
echo ^<Project^>
echo   ^<PropertyGroup^>
echo     ^<RimWorldPath^>%RIMWORLD_PATH%^</RimWorldPath^>
echo   ^</PropertyGroup^>
echo ^</Project^>
) > "%BUILD_PROPS%"

:: === КОМПИЛЯЦИЯ ===
echo.
echo Compiling project: %CS_PROJ%
dotnet build "%CS_PROJ%" --configuration Release
if errorlevel 1 (
    echo.
    echo Build failed!
    goto :end
)

:: === ОЧИСТКА DLL ===
echo.
echo Cleaning Assemblies folder...
cd Assemblies
if exist "%MOD_NAME%.dll" (
    mkdir temp 2>nul
    move /Y "%MOD_NAME%.dll" temp\ 2>nul
)
del /Q *.dll 2>nul
if exist "temp\%MOD_NAME%.dll" (
    move /Y "temp\%MOD_NAME%.dll" . 2>nul
    rmdir /S /Q temp
)
cd ..

echo.
echo Compilation complete. Only %MOD_NAME%.dll remains in Assemblies.

:: === ОБНОВЛЕНИЕ copy_to_mod.bat через PowerShell ===
echo.
echo Updating %COPY_SCRIPT% with RimWorld path...

powershell -Command "(Get-Content '%COPY_SCRIPT%') -replace 'set \"TARGET=.*\"', 'set \"TARGET=%RIMWORLD_PATH%\Mods\%MOD_NAME%\"' | Set-Content '%COPY_SCRIPT%'"

echo.
echo %COPY_SCRIPT% updated successfully.

:end

echo.
echo Build process complete.
pause
