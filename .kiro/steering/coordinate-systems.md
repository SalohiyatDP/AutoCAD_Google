# Koordinata tizimlari konteksti

Foydalanuvchi asosan quyidagi ikki koordinata tizimida ishlaydi. Plagin shu
ikkisida birinchi navbatda to'g'ri va aniq ishlashi kerak:

1. **Pulkovo 1942 / Gauss-Kruger Zona 12N** (EPSG:28412)
   - Krassovskiy 1940 ellipsoidi
   - Markaziy meridian: 69°E
   - Zonali soxta sharqiy (false easting): **12 500 000** (zona prefiksi bilan)
   - Tipik koordinatalar: Easting ≈ 12 5xx xxx, Northing ≈ 4 5xx xxx
   - Plagin kodi: `CoordinateSystemType.Pulkovo1942_GK_Zone12N` -> `Pulkovo1942Projection(12)`

2. **WGS 1984 Web Mercator (Auxiliary Sphere)** (EPSG:3857)
   - Google/ArcGIS "Web Mercator Auxiliary Sphere" bilan bir xil
   - Chizma birliklari = Web Mercator metrlari
   - Plagin kodi: `CoordinateSystemType.WebMercator_3857` -> `WebMercatorProjection`

## Muhim eslatmalar

- Bo'sh/yangi chizma (koordinata boshi 0,0 atrofida) bu tizimlarda
  georeferensiyalanmagan hisoblanadi — fon xarita ko'rinmaydi. Test uchun
  real koordinatali chizmadan foydalanish kerak.
- Pulkovo -> WGS84 datum o'tkazishi 7 parametrli (Bursa-Wolf) standart
  qiymatlardan foydalanadi (`DatumShift.Pulkovo1942ToWgs84`). Mintaqaviy
  aniqlik kerak bo'lsa, shu parametrlarni moslash mumkin.
