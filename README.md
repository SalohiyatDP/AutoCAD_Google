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

DLL `NETLOAD` orqali yuklanadi.


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

| Tizim | Maqsad | O'lcham aniqligi |
|-------|--------|------------------|
| **WGS 1984 Web Mercator (EPSG:3857)** — standart | Umumiy/keng hudud | Mercator masshtabi 1/cos(kenglik); Toshkent kengligida ~1.33× |
| **Pulkovo 1942 / Gauss-Kruger Zona 12N** (EPSG:28412) | Aniq lokal ish | Zonaga yaqin (69°E ±3°) ~haqiqiy yer metrlari |

- Web Mercator: chizma birliklari = Web Mercator metrlari (Google/ArcGIS bilan bir xil).
- Pulkovo GK Z12: Krassovskiy 1940 ellipsoidi, markaziy meridian 69°E, zonali false easting
  12 500 000 (tipik easting ~12,5xx,xxx, northing ~4,5xx,xxx), WGS84'ga 7-parametrli
  (Bursa-Wolf) datum o'tkazish.

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

## Foydalanish

1. AutoCAD Mechanical 2021 ni oching.
2. `NETLOAD` → `GoogleSatelliteCAD.dll` ni tanlang.
3. Ribbon **Google Maps → Coordinate System** orqali tizimni tanlang (Web Mercator yoki Pulkovo GK Z12).
4. **Satellite ON (REFRESH)** bosing yoki `GSATON` kiriting.
5. Bo'sh chizmada bo'lsangiz — **Go to Kosonsoy** (`GSATHOME`) bilan hududga o'ting.
6. Zoom/Pan qiling — xarita ko'rinayotgan hududga avtomatik moslashadi.
7. Yangilash kerak bo'lsa — **Satellite ON (REFRESH)** yoki `GSATREFRESH`.

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
