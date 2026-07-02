@echo off
:: =====================================================================================
::  GoogleSatelliteCAD — O'chirish (olib tashlash) skripti
:: =====================================================================================
::  Bu skript plaginni registry va ApplicationPlugins dan olib tashlaydi.
::  O'chirilgandan so'ng, AutoCAD keyingi ishga tushganda plagin yuklanmaydi.
::
::  ESLATMA: Disk keshi (%APPDATA%\GoogleSatelliteCAD\) saqlanib qoladi.
:: =====================================================================================

setlocal

echo.
echo  ====================================================
echo   GoogleSatelliteCAD — Plaginni olib tashlash
echo  ====================================================
echo.

set "ACAD_REG_KEY=HKEY_CURRENT_USER\Software\Autodesk\AutoCAD\R24.0\ACAD-3001:409\Applications\GoogleSatelliteCAD"
set "BUNDLE_DIR=%APPDATA%\Autodesk\ApplicationPlugins\GoogleSatelliteCAD.bundle"

echo  O'chirishni tasdiqlaysizmi?
set /p CONFIRM="  (H/Y): "
if /i not "%CONFIRM%"=="H" (
    if /i not "%CONFIRM%"=="Y" (
        echo  Bekor qilindi.
        pause
        exit /b 0
    )
)

echo.

:: Registry ni tozalash
echo  [*] Registry tozalanmoqda...
reg delete "%ACAD_REG_KEY%" /f >nul 2>&1
if %errorlevel%==0 (
    echo  [OK] Registry kaliti o'chirildi.
) else (
    echo  [*] Registry kaliti topilmadi (allaqachon o'chirilgan).
)

:: Bundle ni tozalash
echo  [*] Bundle papkasi o'chirilmoqda...
if exist "%BUNDLE_DIR%" (
    rmdir /s /q "%BUNDLE_DIR%" 2>nul
    echo  [OK] Bundle o'chirildi.
) else (
    echo  [*] Bundle topilmadi (allaqachon o'chirilgan).
)

echo.
echo  ====================================================
echo   MUVAFFAQIYATLI O'CHIRILDI!
echo  ====================================================
echo.
echo  AutoCAD keyingi ishga tushganda plagin yuklanmaydi.
echo.
echo  Disk keshini ham o'chirish uchun:
echo    rmdir /s /q "%APPDATA%\GoogleSatelliteCAD"
echo.

pause
exit /b 0
