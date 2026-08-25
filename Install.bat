@echo off
:: =====================================================================================
::  GoogleSatelliteCAD — O'rnatish skripti
:: =====================================================================================
::  Bu skript plaginni AutoCAD reestrida ro'yxatdan o'tkazadi va
::  ApplicationPlugins papkasiga ham joylaydi (ikki usulda).
::  AutoCAD har safar ochilganda plagin AVTOMATIK yuklanadi!
::
::  ISHLATISH:
::    1. Loyihani Release | x64 rejimida kompilyatsiya qiling
::    2. Ushbu skriptni ishga tushiring (Administrator KERAK EMAS)
::    3. AutoCAD ni qayta ishga tushiring — plagin tayyor!
::
::  ESLATMA: AutoCAD Mechanical 2021 = R24.0, profil ACAD-3001:409
::           Agar boshqa versiya/profil bo'lsa, pastdagi qiymatlarni o'zgartiring.
:: =====================================================================================

setlocal enabledelayedexpansion

echo.
echo  ====================================================
echo   GoogleSatelliteCAD — Avtomatik yuklash o'rnatmasi
echo  ====================================================
echo.

:: ===== SOZLAMALAR (kerak bo'lsa o'zgartiring) =====
:: AutoCAD 2021 Mechanical = R24.0, profil nomi odatda ACAD-3001:409
:: Oddiy AutoCAD 2021 = ACAD-4001:409
:: Agar profilingiz boshqacha bo'lsa — pastda o'zgartiring.
set "ACAD_REG_KEY=HKEY_CURRENT_USER\Software\Autodesk\AutoCAD\R24.0\ACAD-3001:409\Applications\GoogleSatelliteCAD"

:: Kompilyatsiya qilingan DLL joylashgan papka (Release)
set "SOURCE_DIR=%~dp0GoogleSatelliteCAD\bin\x64\Release"

:: O'rnatish manzillari
set "BUNDLE_DIR=%APPDATA%\Autodesk\ApplicationPlugins\GoogleSatelliteCAD.bundle"
set "CONTENTS_DIR=%BUNDLE_DIR%\Contents"

:: ===================================================================

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

:: DLL ning to'liq yo'lini olish
set "DLL_PATH=%SOURCE_DIR%\GoogleSatelliteCAD.dll"

echo  DLL topildi: %DLL_PATH%
echo.

:: ===================== 1-USUL: REGISTRY (ENG ISHONCHLI) =====================
echo  [1/2] Windows Registry'ga yozilmoqda (demand loading)...
echo.

:: AutoCAD registry'ga plagin ma'lumotlarini yozamiz
:: LOADCTRLS = 14 (0x0E) = Startup(2) + Command(4) + Proxy(8) — barcha holatda yuklaydi
reg add "%ACAD_REG_KEY%" /v "DESCRIPTION" /t REG_SZ /d "Google Satellite fon xarita plagini" /f >nul 2>&1
reg add "%ACAD_REG_KEY%" /v "LOADCTRLS" /t REG_DWORD /d 14 /f >nul 2>&1
reg add "%ACAD_REG_KEY%" /v "LOADER" /t REG_SZ /d "%DLL_PATH%" /f >nul 2>&1
reg add "%ACAD_REG_KEY%" /v "MANAGED" /t REG_DWORD /d 1 /f >nul 2>&1

if %errorlevel%==0 (
    echo  [OK] Registry yozildi:
    echo       %ACAD_REG_KEY%
    echo       LOADER = %DLL_PATH%
    echo       LOADCTRLS = 14 (startup + command + proxy)
    echo       MANAGED = 1 (.NET assembly)
    echo.
) else (
    echo  [OGOHLANTIRISH] Registry'ga yozishda muammo. Davom etilmoqda...
    echo.
)

:: ===================== 2-USUL: BUNDLE (QOSHIMCHA) =====================
echo  [2/2] ApplicationPlugins bundle ham o'rnatilmoqda (zaxira usul)...

:: Eski o'rnatmani tozalash
if exist "%BUNDLE_DIR%" (
    rmdir /s /q "%BUNDLE_DIR%" 2>nul
)

:: Papkalarni yaratish va fayllarni ko'chirish
mkdir "%CONTENTS_DIR%" 2>nul
copy /y "%~dp0GoogleSatelliteCAD.bundle\PackageContents.xml" "%BUNDLE_DIR%\PackageContents.xml" >nul 2>&1
copy /y "%DLL_PATH%" "%CONTENTS_DIR%\" >nul
if exist "%SOURCE_DIR%\GoogleSatelliteCAD.pdb" (
    copy /y "%SOURCE_DIR%\GoogleSatelliteCAD.pdb" "%CONTENTS_DIR%\" >nul
)

:: .NET 8 build (2025-2026) DLL — agar yig'ilgan bo'lsa, uni ham bundle ichiga qo'shamiz.
:: Shunda bitta bundle 2021-2024 (.NET Framework) VA 2025-2026 (.NET 8) da ishlaydi;
:: PackageContents.xml versiyaga qarab mos DLL'ni tanlaydi.
set "SOURCE_DIR_2025=%~dp0build-2025\bin\Release"
if exist "%SOURCE_DIR_2025%\GoogleSatelliteCAD_2025.dll" (
    copy /y "%SOURCE_DIR_2025%\GoogleSatelliteCAD_2025.dll" "%CONTENTS_DIR%\" >nul
    echo  [OK] GoogleSatelliteCAD_2025.dll ^(2025-2026^) bundle ichiga qo'shildi.
) else (
    echo  [i] GoogleSatelliteCAD_2025.dll topilmadi ^(faqat 2021-2024 o'rnatiladi^).
    echo      2025-2026 uchun: dotnet build build-2025\GoogleSatelliteCAD.2025.csproj -c Release
)

echo  [OK] Bundle o'rnatildi: %BUNDLE_DIR%
echo.

:: ===================== DLL UNBLOCK =====================
echo  [*] DLL blokdan chiqarilmoqda (Unblock)...
powershell -Command "Get-ChildItem '%CONTENTS_DIR%' -Recurse | Unblock-File" >nul 2>&1
powershell -Command "Get-ChildItem '%SOURCE_DIR%' -Filter '*.dll' | Unblock-File" >nul 2>&1
echo  [OK] Unblock bajarildi.
echo.

:: ===================== NATIJA =====================
echo  ====================================================
echo   MUVAFFAQIYATLI O'RNATILDI!
echo  ====================================================
echo.
echo  Usullar:
echo    1. Registry (demand loading) — %ACAD_REG_KEY%
echo    2. Bundle — %BUNDLE_DIR%
echo.
echo  AutoCAD ni ishga tushiring — plagin avtomatik yuklanadi.
echo  Buyruqlar: GSATON, GSATOFF, GSATCLEAR
echo.
echo  ====================================================
echo   MUHIM: Agar baribir ishlamasa:
echo  ====================================================
echo.
echo  1. AutoCAD buyruq satrida yozing: SECURELOAD
echo     Qiymati 1 yoki 0 bo'lishi kerak (2 = bloklaydi!)
echo     O'zgartirish: SECURELOAD → 1 yoki 0
echo.
echo  2. Registry profil nomini tekshiring:
echo     regedit → HKCU\Software\Autodesk\AutoCAD\R24.0\
echo     ichida qanday papka bor? (ACAD-3001:409, ACAD-4001:409, ...)
echo     Agar boshqacha bo'lsa — Install.bat ichidagi
echo     ACAD_REG_KEY qatorini to'g'rilang.
echo.
echo  3. OPTIONS → Files → Trusted Locations ga DLL joylashgan
echo     papkani qo'shing.
echo.

pause
exit /b 0
