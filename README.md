# GoogleSatelliteCAD

AutoCAD Mechanical 2021 uchun **Google Satellite** sun'iy yo'ldosh fon xarita plagini.
ArcGIS Desktop 10.8 dagi `google.lyr` kabi ishlaydi: DWG chizmasi ortida Google
sun'iy yo'ldosh tasviri fon sifatida ko'rinadi va zoom/pan paytida avtomatik yangilanadi.

> **Platforma:** C# · .NET Framework 4.8 · AutoCAD .NET API · Visual Studio 2022 · x64

---

## Imkoniyatlar

- `GSATON` — Google Satellite fon xaritasini yoqish (ko'rinayotgan hudud tilelarini yuklaydi).
- `GSATOFF` — fon xaritasini o'chirish (kesh saqlanadi).
- `GSATCLEAR` — disk keshini tozalash.
- Real vaqt rejimida zoom/pan kuzatuvi (`SystemVariableChanged` orqali) va avtomatik yangilash.
- Asinxron + parallel tile yuklash (`SemaphoreSlim`).
- Disk keshi: `%APPDATA%\GoogleSatelliteCAD\Cache\{z}\{x}\{y}.jpg`.
- "Google Maps" Ribbon yorlig'i: ON / OFF / Clear Cache / Coordinate System / About.
- Sozlamalar oynasi (WPF): server URL, kesh hajmi, max zoom, parallel yuklash, avtomatik yangilash, koordinata tizimi.

### Qo'llab-quvvatlanadigan koordinata tizimlari

- WGS84 Geographic
- WGS84 UTM (avtomatik zona)
- **Pulkovo 1942 / Gauss-Kruger Zona 12N** (standart)
- EPSG:3857 Web Mercator

---

## Arxitektura (SOLID)

```
GoogleSatelliteCAD/
├── Core/         PluginEntry, ConfigManager, PluginContext, Logger
├── Commands/     MapOnCommand (GSATON), MapOffCommand (GSATOFF), CacheClearCommand (GSATCLEAR)
├── Projection/   Ellipsoid, Mercator, Pulkovo1942 (Gauss-Kruger), CoordinateTransform, IProjection
├── TileEngine/   TileSystem, TileCache, TileDownloader, TileManager (+ interfeyslar)
├── Drawing/      LayerManager, RasterManager, ViewportTracker
└── UI/           RibbonUI, SettingsWindow, SettingsViewModel, AboutWindow
```

Har bir qatlam interfeyslar orqali ajratilgan (DIP), proyeksiyalar `IProjection`
ni amalga oshiradi (OCP/LSP), kesh va yuklab oluvchi `ITileCache` / `ITileDownloader`
abstraksiyalari ortida turadi.

---

## Yig'ish (Build)

### Talablar
- Windows 10/11 (x64)
- AutoCAD Mechanical 2021 o'rnatilgan
- Visual Studio 2022 (.NET desktop development workload)
- .NET Framework 4.8 SDK

### Qadamlar

1. `GoogleSatelliteCAD.sln` ni Visual Studio 2022 da oching.
2. AutoCAD referens yo'lini tekshiring. Standart qiymat:
   ```
   C:\Program Files\Autodesk\AutoCAD 2021\
   ```
   Agar AutoCAD boshqa joyda bo'lsa, `GoogleSatelliteCAD.csproj` faylidagi
   `<AutoCADReferencePath>` qiymatini o'zgartiring **yoki** buyruq qatorida bering:
   ```
   msbuild GoogleSatelliteCAD.sln /p:Configuration=Release /p:Platform=x64 ^
     /p:AutoCADReferencePath="D:\Autodesk\AutoCAD 2021\"
   ```
3. `Release | x64` konfiguratsiyasida yig'ing.
4. Natija: `GoogleSatelliteCAD\bin\x64\Release\GoogleSatelliteCAD.dll`.

> Eslatma: AutoCAD assembly'lari (`acmgd`, `acdbmgd`, `accoremgd`, `AdWindows`,
> `AcWindows`) **Copy Local = False** holatda bo'lishi shart (csproj da shunday sozlangan).

---

## Foydalanish

1. AutoCAD Mechanical 2021 ni oching.
2. `NETLOAD` buyrug'ini kiriting va `GoogleSatelliteCAD.dll` ni tanlang.
3. `GSATON` buyrug'ini ishga tushiring (yoki **Google Maps** Ribbon → *Satellite ON*).
4. Zoom va Pan qilganda xarita avtomatik yangilanadi.
5. Koordinata tizimini Ribbon → *Coordinate System* orqali tanlang (standart: Pulkovo 1942 GK Zona 12N).

DWG obyektlari har doim xarita ustida (foreground) ko'rinadi — rasterlar
`GOOGLE_SATELLITE` qatlamiga joylanib, chizish tartibida eng pastga tushiriladi,
qatlam qulflanadi va `Plot = False` qilinadi.

---

## Sozlamalar va kesh joylashuvi

```
%APPDATA%\GoogleSatelliteCAD\
├── settings.xml      (foydalanuvchi sozlamalari)
├── plugin.log        (diagnostika jurnali)
└── Cache\{z}\{x}\{y}.jpg
```

---

## Muhim eslatmalar

- Tile serverlaridan foydalanishda tegishli xizmat shartlariga rioya qiling.
  Ishlab chiqarish uchun rasmiy litsenziyalangan tile xizmatidan foydalanish tavsiya etiladi;
  server URL manzilini Sozlamalar oynasidan o'zgartirish mumkin.
- Pulkovo 1942 → WGS84 datum o'tkazishi standart 7 parametrli (Bursa-Wolf)
  qiymatlardan foydalanadi; aniqlik talab qilinsa, mintaqaviy parametrlarni
  `DatumShift` da moslang.
