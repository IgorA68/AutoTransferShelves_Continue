@echo off   
setlocal   
:: Назначаем имя моду   
set "MOD_NAME=AutoTransferShelves"   
set "TARGET=G:\RimWorld\Mods\AutoTransferShelves"   
:: Удаляем целевую папку   
rmdir /S /Q "%TARGET%"   
:: Копируем папку About (всегда)   
xcopy /E /I /Y "About" "%TARGET%\About"   
:: Копируем остальные папки, если они существуют   
if exist "Assemblies" (   
    xcopy /E /I /Y "Assemblies" "%TARGET%\Assemblies"   
)   
if exist "Defs" (   
    xcopy /E /I /Y "Defs" "%TARGET%\Defs"   
)   
if exist "Patches" (   
    xcopy /E /I /Y "Patches" "%TARGET%\Patches"   
)   
if exist "Textures" (   
    xcopy /E /I /Y "Textures" "%TARGET%\Textures"   
)   
if exist "Languages" (   
    xcopy /E /I /Y "Languages" "%TARGET%\Languages"   
)   
:: Дополнительные папки, если используются   
if exist "Sounds" (   
    xcopy /E /I /Y "Sounds" "%TARGET%\Sounds"   
)   
if exist "Backstories" (   
    xcopy /E /I /Y "Backstories" "%TARGET%\Backstories"   
)   
echo.   
echo All files have been copied.   
:: открыть целевую папку   
:: start "" "%TARGET%"   
pause   
