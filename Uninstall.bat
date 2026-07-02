@echo off
:: =====================================================================================
::  GoogleSatelliteCAD — O'chirish (olib tashlash) skripti
:: =====================================================================================
::  Bu skript plaginni AutoCAD ApplicationPlugins papkasidan olib tashlaydi.
::  O'chirilgandan so'ng, AutoCAD keyingi ishga tushganda plagin yuklanmaydi.
::
::  ESLATMA: Disk keshi (%APPDATA%\GoogleSatelliteCAD\) saqlanib qoladi.
::           Keshni ham o'chirish uchun quyidagi papkani qo'lda o'chiring:
::           %APPDATA%\GoogleSatelliteCAD\
:: =====================================================================================

setlocal

echo.
echo  ====================================================
echo   GoogleSatelliteCAD — Plaginni olib tashlash
echo  ====================================================
echo.

set "BUNDLE_DIR=%APPDATA%\Autodesk\ApplicationPlugins\GoogleSatelliteCAD.bundle"

if not exist "%BUNDLE_DIR%" (
    echo  Plagin o'rnatilmagan yoki allaqachon olib tashlangan.
    echo  Joy: %BUNDLE_DIR%
    echo.
    pause
    exit /b 0
)

echo  Plagin topildi: %BUNDLE_DIR%
echo.
set /p CONFIRM="  O'chirishni tasdiqlaysizmi? (H/Y): "
if /i not "%CONFIRM%"=="H" (
    if /i not "%CONFIRM%"=="Y" (
        echo  Bekor qilindi.
        pause
        exit /b 0
    )
)

rmdir /s /q "%BUNDLE_DIR%" 2>nul

if not exist "%BUNDLE_DIR%" (
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
) else (
    echo.
    echo  [XATOLIK] O'chirishda muammo yuz berdi!
    echo  Papkani qo'lda o'chirib ko'ring: %BUNDLE_DIR%
    echo.
)

pause
exit /b 0
