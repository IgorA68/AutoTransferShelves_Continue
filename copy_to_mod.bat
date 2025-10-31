@echo off
setlocal

:: Назначаем имя моду
set "MOD_NAME=AutoTransferShelves"

:: Целевой путь — будет заменён build.bat
set "TARGET=C:\Users\user\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Mods\AutoTransferShelves"

:: Удаляем целевую папку, если существует
if exist "%TARGET%" (
    echo [•] Removing existing mod folder: %TARGET%
    rmdir /S /Q "%TARGET%"
)

:: Гарантируем, что целевая папка существует
mkdir "%TARGET%" 2>nul

:: Копируем папку About (всегда)
echo [•] Copying About...
xcopy /E /I /Y "About" "%TARGET%\About"

:: Копируем остальные папки, если они существуют
for %%D in (Assemblies Defs Patches Textures Languages Sounds Backstories) do (
    if exist "%%D" (
        echo [•] Copying %%D...
        xcopy /E /I /Y "%%D" "%TARGET%\%%D"
    )
)

echo.
echo [✓] Mod successfully copied to:
echo %TARGET%
pause
