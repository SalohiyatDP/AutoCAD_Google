# Koordinata tizimlari konteksti

Plagin foydalanuvchi uchun **ikki** koordinata tizimini qo'llab-quvvatlaydi
(ro'yxatda faqat shu ikkisi ko'rsatiladi):

1. **WGS 1984 Web Mercator (Auxiliary Sphere)** (EPSG:3857) — standart (default)
   - Google/ArcGIS "Web Mercator Auxiliary Sphere" bilan bir xil
   - Chizma birliklari = Web Mercator metrlari
   - DIQQAT: Web Mercator masshtabi 1/cos(kenglik) ga teng — masalan Toshkentda
     (~41.3°) masofalar haqiqiy yer metridan ~1.33× kattaroq. Umumiy/keng hudud uchun qulay.
   - Kod: `CoordinateSystemType.WebMercator_3857` -> `WebMercatorProjection`

2. **Pulkovo 1942 / Gauss-Kruger Zona 12N** (EPSG:28412) — aniq lokal ish uchun
   - Krassovskiy 1940 ellipsoidi, markaziy meridian 69°E, zonali false easting 12 500 000
   - Tipik koordinatalar: Easting ≈ 12 5xx xxx, Northing ≈ 4 5xx xxx
   - Zonaga yaqin (69°E ±3°) masshtab ~1.0 — haqiqiy yer metrlariga mos keladi
     (Web Mercator'dagi 1.33× o'lcham xatoligi bu yerda yo'q).
   - Kod: `CoordinateSystemType.Pulkovo1942_GK_Zone12N` -> `Pulkovo1942Projection(12)`

## Muhim eslatmalar

- RasterImage faqat affin joylashtirishni qo'llaydi. Pulkovo'da o'z zonasidan
  uzoq (butun dunyo ko'rinishi) tilelar qattiq qiyshayadi — `IsAffineFriendly`
  filtri bunday tilelarni o'tkazib yuboradi. Lokal (shahar/uchastka) masshtabda
  Pulkovo toza va aniq ishlaydi.
- Bo'sh/yangi chizma (0,0 atrofida) georeferensiyalanmagan — fon xarita ko'rinmaydi.
- WGS84 Geographic/UTM proyeksiyalari kodda bor, lekin UI'da yashirilgan;
  saqlangan sozlama shulardan biri bo'lsa, avtomatik Web Mercator'ga moslanadi.


## Hududiy cheklov

Foydalanuvchi talabiga ko'ra plagin **faqat O'zbekiston Respublikasi hududidagi**
tilelarni yuklaydi (butun dunyo xaritasi yuklanmaydi). Ko'rinish chegarasi
O'zbekiston chegara to'rtburchagi bilan kesib olinadi (PluginContext dagi
`UzWestLon/UzEastLon/UzSouthLat/UzNorthLat` konstantalari, taxminan
55.9–73.25°E, 37.1–45.65°N). Ko'rinish bu hududdan tashqarida bo'lsa, xarita
yuklanmaydi va mavjud tilelar tozalanadi.

## About oynasi

Mualliflar/tashkilot (AboutWindow.cs):
- Dasturchi: Abdujabborov Sherzod Jahongir o'g'li
- G'oya muallifi: Karimbekov Asadbek Nasibbek o'g'li
- Tashkilot: Davlat Kadastrlari Palatasi, Kosonsoy tuman filiali
