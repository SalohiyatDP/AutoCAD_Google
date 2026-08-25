# GoogleSatelliteCAD

**AutoCAD 2021–2026** (va uning barcha vertikal mahsulotlari) uchun **Google Satellite**
sun'iy yo'ldosh fon xarita plagini. ArcGIS Desktop'dagi `google.lyr` kabi ishlaydi: DWG
chizmasi ostida Google sun'iy yo'ldosh tasviri fon sifatida ko'rinadi va zoom/pan paytida
avtomatik yangilanadi. Chizma obyektlari har doim xarita ustida turadi.

> **Platforma:** C# · x64 · AutoCAD .NET API · Visual Studio 2022
> **Runtime:** AutoCAD 2021–2024 → .NET Framework 4.8; AutoCAD 2025–2026 → .NET 8
> **Hudud:** O'zbekiston Respublikasi

---

## Qo'llab-quvvatlanadigan Autodesk mahsulotlari (2021+)

Plagin AutoCAD .NET API'sidan foydalanadi — shuning uchun **standart AutoCAD** hamda
undan kelib chiqqan **vertikal mahsulotlar**ning barchasida ishlaydi (bir yil uchun barchasi
bir xil API'ni ulashadi):

- **AutoCAD** 2021, 2022, 2023, 2024, 2025, 2026
- **AutoCAD Mechanical** 2021–2026
- **AutoCAD Civil 3D** 2021–2026
- **AutoCAD Map 3D** 2021–2026
- **AutoCAD Architecture** 2021–2026
- **AutoCAD MEP** 2021–2026
- **AutoCAD Electrical** 2021–2026
- **AutoCAD Plant 3D** 2021–2026
- **AutoCAD Raster Design** 2021–2026

Ikki xil runtime bo'yicha ikkita DLL chiqariladi va bundle (`PackageContents.xml`)
mos DLL'ni versiya bo'yicha avtomatik yuklaydi:

| Relizlar | .NET runtime | Chiqariladigan DLL | Series (bundle) |
|----------|--------------|--------------------|-----------------|
| 2021–2024 | .NET Framework 4.8 | `GoogleSatelliteCAD.dll` | R24.0–R24.3 |
| 2025–2026 | .NET 8 | `GoogleSatelliteCAD_2025.dll` | R25.0+ |

> Plagin yuklanganda mezbon mahsulot nomi, versiyasi va runtime `plugin.log` ga yoziladi
> (`HostProduct`). Reliz 2021 dan past bo'lsa ogohlantirish beriladi. Turli mahsulotlar bir xil
> API/runtime'ga ega deb faraz qilinmaydi — yo'naltirish bundle darajasida amalga oshadi.

---

## Asosiy imkoniyatlar

- Google sun'iy yo'ldosh tasvirini DWG fonida real vaqtda ko'rsatish.
- Zoom/Pan paytida avtomatik yangilanish (ekranda ko'rinayotgan hudud bo'yicha).
- **Faqat O'zbekiston Respublikasi hududi** uchun tile yuklash (butun dunyo yuklanmaydi).
- Koordinata tizimlari: **Pulkovo 1942 Gauss-Krüger** (butun O'zbekiston uchun **10N..13N**
  zonalari, avto-zona bilan), **WGS 84 / UTM 40N..43N** (toposyomka, easting ~5xx,xxx) va
  **Web Mercator (EPSG:3857)**.
- Asinxron + parallel tile yuklash, disk keshi, subdomen aylanishi va qayta urinish.
- "Google Maps" Ribbon yorlig'i orqali boshqarish.
- Sozlamalar (WPF) va About oynasi.

---

## Buyruqlar

| Buyruq | Vazifasi |
|--------|----------|
| `GSATON` | Fon xaritani yoqadi. Yoqilgan bo'lsa — ko'rinishni yangilaydi (REFRESH). |
| `GSATREFRESH` | Ko'rinib turgan hududni qayta yuklaydi (yangilaydi). |
| `GSATOFF` | Fon xaritani o'chiradi (disk keshi saqlanadi). |
| `GSATCLEAR` | Disk keshini (yuklangan tilelarni) tozalaydi. |

DLL **avtomatik yuklanadi** (Autoloader bundle orqali) yoki `NETLOAD` bilan qo'lda yuklanadi.


---

## Ribbon menyu — "Google Maps"

- **Satellite ON (REFRESH)** — yoqish; yoqilgan bo'lsa, ko'rinishni yangilaydi.
- **Satellite OFF** — o'chirish.
- **Clear Cache** — keshni tozalash.
- **Coordinate System** — koordinata tizimi va sozlamalar oynasi.
- **About** — dastur va mualliflar haqida.

---

## Koordinata tizimlari

Butun **O'zbekiston Respublikasi** hududi Gauss-Krüger **10N, 11N, 12N va 13N** zonalari
bilan qoplanadi (har zona 6° kenglikda). Plagin bu zonalarning barchasini qo'llab-quvvatlaydi.

| Tizim | Maqsad | Tipik easting |
|-------|--------|---------------|
| **Pulkovo 1942 / GK AVTO-ZONA (10N..13N)** — **standart** | Butun O'zbekiston, zona avtomatik | prefiksli (~z,5xx,xxx) |
| **Pulkovo 1942 / GK Zona 10N/11N/12N/13N (EPSG:28410–28413)** | Aniq zona, prefiksli easting | ~**10..13,5xx,xxx** |
| **Pulkovo 1942 / GK Zona 10N/11N/12N/13N (EPSG:28460–28463)** | Qurilma/GPS eksporti, prefikssiz | ~**5xx,xxx** |
| **WGS 84 / UTM Zona 40N/41N/42N/43N (EPSG:32640–32643)** | Toposyomka, GNSS/GPS (WGS84), prefikssiz | ~**5xx,xxx** |
| **WGS 84 / UTM Zona 40N..43N — prefiksli / AVTO-ZONA** | UTM, zona easting'da kodlangan | ~**4z,5xx,xxx** |
| **WGS 1984 Web Mercator (EPSG:3857)** | Umumiy/keng hudud | metr (~7,7xx,xxx) |

**Gauss-Krüger zonalari va markaziy meridianlari (O'zbekiston):**

| Zona | Markaziy meridian | Longitude qamrovi | Prefiksli EPSG | Prefikssiz EPSG |
|------|-------------------|-------------------|----------------|-----------------|
| 10N  | 57°E              | 54°–60°E          | 28410          | 28460           |
| 11N  | 63°E              | 60°–66°E          | 28411          | 28461           |
| 12N  | 69°E              | 66°–72°E          | 28412          | 28462           |
| 13N  | 75°E              | 72°–78°E          | 28413          | 28463           |

**WGS 84 / UTM zonalari (O'zbekiston) — toposyomka uchun (easting ~5xx,xxx):**

| UTM zona | Markaziy meridian | Longitude qamrovi | EPSG  |
|----------|-------------------|-------------------|-------|
| 40N      | 57°E              | 54°–60°E          | 32640 |
| 41N      | 63°E              | 60°–66°E          | 32641 |
| 42N      | 69°E              | 66°–72°E          | 32642 |
| 43N      | 75°E              | 72°–78°E          | 32643 |

- **AVTO-ZONA (standart, tavsiya etiladi)** — zonani chizma easting'idagi **prefiks**dan
  avtomatik aniqlaydi (masalan easting ~11,5xx,xxx → zona 11, ~12,5xx,xxx → zona 12). Butun
  O'zbekiston bo'ylab bitta tanlov bilan ishlaydi. Faqat **prefiksli (zonalangan)** easting
  uchun; prefikssiz (500000) easting'da aniq zona variantini tanlang.
- **Prefiksli GK zonalari (EPSG:284xx)** — Krassovskiy 1940 ellipsoidi, zonali false easting
  = `zona·1 000 000 + 500 000` (easting ~z,5xx,xxx), WGS84'ga 7-parametrli (Bursa-Wolf) datum.
- **Prefikssiz GK zonalari (EPSG:2846x)** — xuddi shu, lekin false easting **500 000**
  (easting ~5xx,xxx). Ko'pincha GPS/geodezik qurilma eksportlarida ishlatiladi.
- **WGS 84 / UTM zonalari — prefikssiz (EPSG:326xx)** — WGS84 ellipsoidi (datum o'tkazish
  shart emas), k₀ = 0.9996, false easting **500 000** (easting ~5xx,xxx). Toposyomka va
  GNSS/GPS eksportlari uchun qulay. Markaziy meridianlari GK zonalariga mos (57/63/69/75°E),
  lekin ellipsoid/masshtab boshqacha. Prefikssiz easting zonani kodlamaydi, shu sababli
  **aniq zonani qo'lda tanlash kerak** (bu variantda avto-zona yo'q).
- **WGS 84 / UTM zonalari — prefiksli / AVTO-ZONA** — xuddi shu UTM, lekin false easting =
  `zona·1 000 000 + 500 000` (masalan zona 42 → easting ~42,5xx,xxx). Zona easting'da
  kodlangani uchun **AVTO-ZONA ishlaydi** — butun O'zbekiston bo'ylab bitta tanlov bilan
  (Pulkovo GK avto-zona kabi). Prefiksli UTM (~4z,5xx,xxx) va prefiksli GK (~1z,5xx,xxx)
  easting diapazonlari ustma-ust tushmaydi.
- **Web Mercator** — chizma birliklari = Web Mercator metrlari (Google/ArcGIS bilan bir xil).
  Masshtab 1/cos(kenglik); Toshkent kengligida ~1.33× (o'lcham biroz kattaroq ko'rinadi).

> Ko'p hollarda **AVTO-ZONA** yetarli. Agar chizmangiz prefikssiz easting'da (~5xx,xxx)
> bo'lsa, hududingizga mos aniq zonani tanlang (masalan Toshkent/Namangan uchun 12N).
> Noto'g'ri tanlansa, xarita mos kelmaydi.

> Chizma tanlangan tizimda georeferensiyalangan bo'lishi kerak. Bo'sh chizma (0,0 atrofida)
> bo'lsa xarita ko'rinmaydi — O'zbekiston hududidagi georeferensiyalangan chizmani oching.

### Xarita ko'rinmayapti? (koordinata tizimi mos kelmasligi)

Eng ko'p uchraydigan sabab — **noto'g'ri koordinata tizimi tanlangani**. Masalan, chizma
Pulkovo koordinatalarida (easting ~12,5xx,xxx), lekin **Web Mercator** tanlangan bo'lsa,
o'sha qiymat mercator metr sifatida talqin qilinib, ko'rinish O'zbekistondan tashqariga
(lon ~112°E) tushadi va xarita **umuman ko'rinmaydi**.

Bunday holatda plagin buyruq qatorida (va `plugin.log` da) **mos tizimni avtomatik taklif
qiladi** — o'sha tavsiyaga amal qilib, Ribbon → **Coordinate System** orqali to'g'ri tizimni
tanlang. Ko'p hollarda **Pulkovo GK AVTO-ZONA** (standart) barcha zonalar uchun ishlaydi.

---

## Hududiy cheklov

Plagin faqat **O'zbekiston Respublikasi** chegara to'rtburchagi ichidagi tilelarni yuklaydi
(~55.9–73.25°E, 37.1–45.65°N). Ko'rinish bu hududdan tashqarida bo'lsa, xarita yuklanmaydi.


---

## Arxitektura (SOLID)

```
GoogleSatelliteCAD/
├── Core/         PluginEntry (IExtensionApplication), ConfigManager, PluginContext, Logger
├── Commands/     GSATON/GSATREFRESH, GSATOFF, GSATCLEAR
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

Ikki xil runtime bo'lgani uchun ikkita build mavjud. Ikkalasi ham **bir xil manba
fayllar**dan kompilyatsiya bo'ladi (kodni ikki marta yozish shart emas).

### A) AutoCAD 2021–2024 (.NET Framework 4.8)

1. `GoogleSatelliteCAD/GoogleSatelliteCAD.csproj` — Visual Studio 2022 da oching.
2. AutoCAD referens papkasini bering. `AcadVersion` (2021..2024) standart yo'lni tanlaydi,
   yoki `AutoCADReferencePath` ni to'g'ridan-to'g'ri ko'rsating (vertikallar uchun papka
   boshqacha bo'lishi mumkin):
   ```
   :: Standart AutoCAD 2024
   msbuild GoogleSatelliteCAD\GoogleSatelliteCAD.csproj /p:Configuration=Release /p:Platform=x64 /p:AcadVersion=2024

   :: Aniq papka bilan (masalan Mechanical yoki Civil 3D)
   msbuild GoogleSatelliteCAD\GoogleSatelliteCAD.csproj /p:Configuration=Release /p:Platform=x64 ^
     /p:AutoCADReferencePath="C:\Program Files\Autodesk\AutoCAD 2023\C3D\"
   ```
3. Natija: `GoogleSatelliteCAD\bin\x64\Release\GoogleSatelliteCAD.dll`.

> Bitta 2021 ga qarshi yig'ilgan DLL 2022–2024 da ham ishlaydi (AutoCAD .NET API oldinga mos).

### B) AutoCAD 2025–2026 (.NET 8)

1. .NET 8 SDK o'rnatilgan bo'lsin.
   ```
   dotnet build build-2025\GoogleSatelliteCAD.2025.csproj -c Release
   ```
2. Natija: `GoogleSatelliteCAD_2025.dll` (AutoCAD.NET 25.x NuGet paketidan referens oladi).

> AutoCAD assembly'lari (acmgd, acdbmgd, accoremgd, AdWindows, AcWindows) **Copy Local = False**;
> .NET 8 build'ida `AutoCAD.NET` NuGet paketi `runtime` aktivlarini chiqishga nusxalamaydi.


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

1. AutoCAD Mechanical 2021 ni oching (plagin bundle orqali avtomatik yuklanadi; yoki
   `NETLOAD` → `GoogleSatelliteCAD.dll`).
2. Ribbon **Google Maps → Coordinate System** orqali tizimni tanlang (odatiy: Pulkovo GK
   **AVTO-ZONA** — butun O'zbekiston; yoki aniq zona 10N..13N, yoxud Web Mercator).
3. **Satellite ON (REFRESH)** bosing yoki `GSATON` kiriting.
4. Zoom/Pan qiling — xarita ko'rinayotgan hududga avtomatik moslashadi.
5. Yangilash kerak bo'lsa — **Satellite ON (REFRESH)** yoki `GSATREFRESH`.

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
