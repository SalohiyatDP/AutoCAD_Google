@echo off
:: =====================================================================================
::  GoogleSatelliteCAD — O'rnatish skripti
:: =====================================================================================
::  Bu skript plaginni AutoCAD ApplicationPlugins papkasiga o'rnatadi.
::  O'rnatilgandan so'ng, AutoCAD har safar ishga tushganda plagin
::  AVTOMATIK yuklanadi. NETLOAD kerak emas!
::
::  ISHLATISH:
::    1. Loyihani Release rejimida kompilyatsiya qiling (Build -> Release | x64)
::    2. Ushbu skriptni Administrator sifatida ishga tushiring
::    3. AutoCAD ni qayta ishga tushiring — plagin tayyor!
:: =====================================================================================

setlocal enabledelayedexpansion

echo.
echo  ====================================================
echo   GoogleSatelliteCAD — Avtomatik yuklash o'rnatmasi
echo  ====================================================
echo.

:: O'rnatish manzili
set "BUNDLE_DIR=%APPDATA%\Autodesk\ApplicationPlugins\GoogleSatelliteCAD.bundle"
set "CONTENTS_DIR=%BUNDLE_DIR%\Contents"

:: Kompilyatsiya qilingan DLL joylashgan papka (Release)
set "SOURCE_DIR=%~dp0GoogleSatelliteCAD\bin\x64\Release"

:: DLL mavjudligini tekshirish
if not exist "%SOURCE_DIR%\GoogleSatelliteCAD.dll" (
    echo  [XATOLIK] GoogleSatelliteCAD.dll topilmadi!
    echo.
    echo  Avval loyihani Release rejimida kompilyatsiya qiling:
    echo    Build -^> Configuration: Release, Platform: x64
    echo.
    echo  Kutilgan joy: %SOURCE_DIR%\GoogleSatelliteCAD.dll
    echo.
    pause
    exit /b 1
)

:: Eski o'rnatmani tozalash
if exist "%BUNDLE_DIR%" (
    echo  [*] Eski o'rnatma topildi, yangilanmoqda...
    rmdir /s /q "%BUNDLE_DIR%" 2>nul
)

:: Papkalarni yaratish
echo  [*] Bundle papkasi yaratilmoqda...
mkdir "%CONTENTS_DIR%" 2>nul

:: PackageContents.xml ni ko'chirish
echo  [*] PackageContents.xml ko'chirilmoqda...
copy /y "%~dp0GoogleSatelliteCAD.bundle\PackageContents.xml" "%BUNDLE_DIR%\PackageContents.xml" >nul

:: DLL va PDB fayllarni ko'chirish
echo  [*] Plagin fayllari ko'chirilmoqda...
copy /y "%SOURCE_DIR%\GoogleSatelliteCAD.dll" "%CONTENTS_DIR%\" >nul
if exist "%SOURCE_DIR%\GoogleSatelliteCAD.pdb" (
    copy /y "%SOURCE_DIR%\GoogleSatelliteCAD.pdb" "%CONTENTS_DIR%\" >nul
)

:: Tekshirish
if exist "%CONTENTS_DIR%\GoogleSatelliteCAD.dll" (
    echo.
    echo  ====================================================
    echo   MUVAFFAQIYATLI O'RNATILDI!
    echo  ====================================================
    echo.
    echo  O'rnatish joyi:
    echo    %BUNDLE_DIR%
    echo.
    echo  AutoCAD ni ishga tushiring — plagin avtomatik yuklanadi.
    echo  Buyruqlar: GSATON, GSATOFF, GSATCLEAR
    echo.
) else (
    echo.
    echo  [XATOLIK] O'rnatishda muammo yuz berdi!
    echo.
)

pause
exit /b 0
