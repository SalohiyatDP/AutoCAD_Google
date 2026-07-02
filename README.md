# GoogleSatelliteCAD

**AutoCAD Mechanical 2021** uchun **Google Satellite** sun'iy yo'ldosh fon xarita plagini.
ArcGIS Desktop'dagi `google.lyr` kabi ishlaydi: DWG chizmasi ostida Google sun'iy yo'ldosh
tasviri fon sifatida ko'rinadi va zoom/pan paytida avtomatik yangilanadi. Chizma obyektlari
har doim xarita ustida turadi.

> **Platforma:** C# · .NET Framework 4.8 · AutoCAD .NET API · Visual Studio 2022 · x64
> **Hudud:** O'zbekiston Respublikasi

---

## Asosiy imkoniyatlar

- Google sun'iy yo'ldosh tasvirini DWG fonida real vaqtda ko'rsatish.
- Zoom/Pan paytida avtomatik yangilanish (ekranda ko'rinayotgan hudud bo'yicha).
- **Faqat O'zbekiston Respublikasi hududi** uchun tile yuklash (butun dunyo yuklanmaydi).
- Ikki koordinata tizimi: **Web Mercator (EPSG:3857)** va **Pulkovo 1942 GK Zona 12N**.
- Asinxron + parallel tile yuklash, disk keshi, subdomen aylanishi va qayta urinish.
- "Google Maps" Ribbon yorlig'i va bir bosishda Kosonsoy tumaniga o'tish.
- Sozlamalar (WPF) va About oynasi.

---

## Buyruqlar

| Buyruq | Vazifasi |
|--------|----------|
| `GSATON` | Fon xaritani yoqadi. Yoqilgan bo'lsa — ko'rinishni yangilaydi (REFRESH). |
| `GSATREFRESH` | Ko'rinib turgan hududni qayta yuklaydi (yangilaydi). |
| `GSATOFF` | Fon xaritani o'chiradi (disk keshi saqlanadi). |
| `GSATHOME` | Ko'rinishni Namangan vil., Kosonsoy tumaniga olib boradi va yoqadi. |
| `GSATCLEAR` | Disk keshini (yuklangan tilelarni) tozalaydi. |

DLL **avtomatik yuklanadi** (Autoloader bundle orqali) yoki `NETLOAD` bilan qo'lda yuklanadi.


---

## Ribbon menyu — "Google Maps"

- **Satellite ON (REFRESH)** — yoqish; yoqilgan bo'lsa, ko'rinishni yangilaydi.
- **Go to Kosonsoy** — Namangan vil., Kosonsoy tumaniga o'tish (`GSATHOME`).
- **Satellite OFF** — o'chirish.
- **Clear Cache** — keshni tozalash.
- **Coordinate System** — koordinata tizimi va sozlamalar oynasi.
- **About** — dastur va mualliflar haqida.

---

## Koordinata tizimlari

| Tizim | Maqsad | Tipik koordinata |
|-------|--------|------------------|
| **Pulkovo 1942 / GK Zona 12N (EPSG:28412)** — standart | Aniq lokal ish | easting ~**12,5xx,xxx** |
| **Pulkovo 1942 / GK Zona 12N (EPSG:28462)** | Qurilma/GPS eksporti | easting ~**5xx,xxx** |
| **WGS 1984 Web Mercator (EPSG:3857)** | Umumiy/keng hudud | metr (~7,7xx,xxx) |

- **Pulkovo GK Z12 (EPSG:28412)** — Krassovskiy 1940 ellipsoidi, markaziy meridian 69°E,
  zonali false easting **12 500 000** (easting ~12,5xx,xxx, northing ~4,5xx,xxx), WGS84'ga
  7-parametrli (Bursa-Wolf) datum o'tkazish. **Odatiy (default) tizim.**
- **Pulkovo GK Z12 (EPSG:28462)** — xuddi shu, lekin prefikssiz false easting **500 000**
  (easting ~5xx,xxx). Ko'pincha GPS/geodezik qurilma eksportlarida shu variant ishlatiladi.
- **Web Mercator** — chizma birliklari = Web Mercator metrlari (Google/ArcGIS bilan bir xil).
  Masshtab 1/cos(kenglik); Toshkent kengligida ~1.33× (o'lcham biroz kattaroq ko'rinadi).

> Koordinata tizimini chizmangiz/qurilma ma'lumotiga MOS tanlang: easting ~12,5 mln bo'lsa
> EPSG:28412, ~5xx ming bo'lsa EPSG:28462. Noto'g'ri tanlansa, xarita mos kelmaydi.

> Chizma tanlangan tizimda georeferensiyalangan bo'lishi kerak. Bo'sh chizma (0,0 atrofida)
> uchun `GSATHOME` orqali Kosonsoy hududiga o'ting.

---

## Hududiy cheklov

Plagin faqat **O'zbekiston Respublikasi** chegara to'rtburchagi ichidagi tilelarni yuklaydi
(~55.9–73.25°E, 37.1–45.65°N). Ko'rinish bu hududdan tashqarida bo'lsa, xarita yuklanmaydi.


---

## Arxitektura (SOLID)

```
GoogleSatelliteCAD/
├── Core/         PluginEntry (IExtensionApplication), ConfigManager, PluginContext, Logger
├── Commands/     GSATON/GSATREFRESH/GSATHOME, GSATOFF, GSATCLEAR
├── Projection/   Ellipsoid, Mercator, Pulkovo1942 (Gauss-Kruger + datum), CoordinateTransform, IProjection
├── TileEngine/   TileSystem, TileCache, TileDownloader, TileManager (+ interfeyslar)
├── Drawing/      LayerManager, RasterManager, ViewportTracker
└── UI/           RibbonUI, SettingsWindow, SettingsViewModel, AboutWindow

GoogleSatelliteCAD.bundle/
└── PackageContents.xml   (AutoCAD Autoloader manifest — avtomatik yuklash uchun)

Install.bat               (plaginni ApplicationPlugins papkasiga o'rnatadi)
Uninstall.bat             (plaginni olib tashlaydi)
```

---

## Texnik tafsilotlar

- **Tile tizimi:** 256×256 px, zoom 0–22 (slippy-map): `tileX = floor((lon+180)/360 · 2^z)`.
- **Joylashtirish:** ko'rinish markazida hisoblangan YAGONA lokal o'xshashlik (similarity)
  transformi orqali — barcha tilelar bir xil o'lcham/yo'nalishda, uzluksiz to'r (Pulkovo'da
  ham bo'shliq/qiyshayishsiz; Web Mercator'da piksel-aniq).
- **Ko'rinish o'qish:** DCS → WCS o'tkazish (ViewDirection/Target/Twist) — UCS yoki burilgan
  ko'rinishda ham ekranda ko'rinayotgan hudud aniq yangilanadi.
- **Zoom barqarorligi:** gisterezis (flip-flopning oldini oladi) + 4-burchakli geografik chegara.
- **Oqimlar (threading):** AutoCAD/WPF'ga faqat asosiy oqimda murojaat; yuklash fon oqimida,
  natija `Application.Idle` da qo'llanadi (cross-thread xatolar yo'q).
- **Tarmoq:** Google subdomenlari (mt0–mt3) aylanadi, har tile bir marta qayta uriniladi.
- **Chizish tartibi:** rasterlar `GOOGLE_SATELLITE` qatlamida, eng pastga (fonga) tushiriladi,
  qatlam qulflanadi, Plot = False.

---

## Yig'ish (Build)

1. `GoogleSatelliteCAD.sln` ni Visual Studio 2022 da oching.
2. AutoCAD referens yo'lini tekshiring (standart: `C:\Program Files\Autodesk\AutoCAD 2021\`).
   Boshqa joyda bo'lsa, `.csproj` dagi `AutoCADReferencePath` ni o'zgartiring yoki:
   ```
   msbuild GoogleSatelliteCAD.sln /p:Configuration=Release /p:Platform=x64 ^
     /p:AutoCADReferencePath="D:\...\AutoCAD 2021\"
   ```
3. `Release | x64` da yig'ing → `GoogleSatelliteCAD\bin\x64\Release\GoogleSatelliteCAD.dll`.

> AutoCAD assembly'lari (acmgd, acdbmgd, accoremgd, AdWindows, AcWindows) **Copy Local = False**.


---

## O'rnatish — Avtomatik yuklash (NETLOAD kerak emas!)

Plaginni bir marta o'rnatib qo'ysangiz, AutoCAD har safar ochilganda **avtomatik** yuklanadi:

### Usul 1: Install.bat (tavsiya etiladi)
1. Loyihani `Release | x64` da kompilyatsiya qiling.
2. **`Install.bat`** ni ishga tushiring (Administrator shart emas).
3. AutoCAD ni qayta oching — plagin tayyor!

### Usul 2: Qo'lda o'rnatish
1. Quyidagi papkani yarating:
   ```
   %APPDATA%\Autodesk\ApplicationPlugins\GoogleSatelliteCAD.bundle\
   ```
2. `GoogleSatelliteCAD.bundle\PackageContents.xml` ni shu papkaga ko'chiring.
3. Ichida `Contents\` papka yarating va `GoogleSatelliteCAD.dll` ni joylashtiring:
   ```
   GoogleSatelliteCAD.bundle\
   ├── PackageContents.xml
   └── Contents\
       └── GoogleSatelliteCAD.dll
   ```
4. AutoCAD ni qayta oching.

### O'chirish
- **`Uninstall.bat`** ni ishga tushiring — plagin olib tashlanadi.
- Yoki `%APPDATA%\Autodesk\ApplicationPlugins\GoogleSatelliteCAD.bundle\` papkasini o'chiring.

> **Eslatma:** Eski usul (`NETLOAD`) ham ishlaydi — lekin har safar qo'lda yuklash kerak bo'ladi.

---

## Foydalanish

1. AutoCAD Mechanical 2021 ni oching (plagin avtomatik yuklanadi).
2. Ribbon **Google Maps → Coordinate System** orqali tizimni tanlang (Web Mercator yoki Pulkovo GK Z12).
3. **Satellite ON (REFRESH)** bosing yoki `GSATON` kiriting.
4. Bo'sh chizmada bo'lsangiz — **Go to Kosonsoy** (`GSATHOME`) bilan hududga o'ting.
5. Zoom/Pan qiling — xarita ko'rinayotgan hududga avtomatik moslashadi.
6. Yangilash kerak bo'lsa — **Satellite ON (REFRESH)** yoki `GSATREFRESH`.

---

## Boshqa kompyuterga o'rnatish va muammolarni bartaraf etish

Plaginni boshqa kompyuterga ko'chirib `NETLOAD` qilganda
`System.Reflection.Assembly.LoadFrom` xatosi chiqsa, quyidagilarni tekshiring:

### 1. DLL "bloklangan" (eng ko'p uchraydigan sabab)
Windows internetdan yuklangan yoki boshqa kompyuterdan ko'chirilgan `.dll` ni
xavfsizlik uchun **bloklaydi** (Mark of the Web) — natijada `LoadFrom` ishlamaydi.

- `GoogleSatelliteCAD.dll` → o'ng tugma → **Properties** → pastda **"Unblock"** belgisini
  qo'ying → **Apply / OK**. Keyin `NETLOAD` qiling.
- Yoki PowerShell orqali butun papkani blokdan chiqaring:
  ```powershell
  Get-ChildItem "C:\plugin_papka" -Recurse | Unblock-File
  ```

### 2. AutoCAD versiyasi
Plagin **AutoCAD 2021** (.NET API v24.0) uchun yig'ilgan. Boshqa kompyuterda ham
**AutoCAD 2021** bo'lsa ishlaydi. **2022+** bo'lsa, o'sha versiya assembly'lari bilan
qaytadan yig'ish kerak (`.csproj` dagi `AutoCADReferencePath` ni o'sha versiyaga yo'naltiring).

### 3. Boshqa shartlar
- **.NET Framework 4.8** o'rnatilgan bo'lsin (Windows 10/11 da odatda mavjud).
- AutoCAD **64-bitli** bo'lsin (plagin x64).
- DLL bilan birga `bin\x64\Release\` ichidagi kerakli fayllar ko'chirilsin (AutoCAD
  assembly'lari ko'chirilmaydi — ular AutoCAD'da bor).

> Avtomatik o'rnatish uchun **Install.bat** yoki qo'lda `.bundle` papkani
> `%APPDATA%\Autodesk\ApplicationPlugins\` ga ko'chiring — shunda DLL har safar
> `NETLOAD` qilinmasdan avtomatik yuklanadi (yuqoridagi "Avtomatik yuklash" bo'limiga qarang).

---

## Sozlamalar va kesh joylashuvi

```
%APPDATA%\GoogleSatelliteCAD\
├── settings.xml      (sozlamalar: server URL, kesh hajmi, max zoom, parallel yuklash, CRS)
├── plugin.log        (diagnostika jurnali)
└── Cache\{z}\{x}\{y}.jpg   (yuklangan tilelar)
```

---

## Mualliflar

- **Dasturchi:** Abdujabborov Sherzod Jahongir o'g'li
- **G'oya muallifi:** Karimbekov Asadbek Nasibbek o'g'li
- **Tashkilot:** Davlat Kadastrlari Palatasi, Kosonsoy tuman filiali

(Ushbu ma'lumotlar Ribbon → **About** oynasida ham ko'rsatiladi.)

---

## Eslatmalar

- Tile serverlaridan foydalanishda tegishli xizmat shartlariga rioya qiling; server URL'ni
  Sozlamalar oynasidan o'zgartirish mumkin.
- Pulkovo → WGS84 datum o'tkazishi standart 7-parametrli qiymatlardan foydalanadi; mintaqaviy
  aniqlik kerak bo'lsa, `DatumShift` parametrlarini moslang.
- Hududiy cheklov yoki "Home" nuqtasini o'zgartirish uchun `PluginContext` dagi
  `UzWestLon/UzEastLon/UzSouthLat/UzNorthLat` va `HomeLon/HomeLat` konstantalarini tahrirlang.
