# Koordinata tizimlari konteksti

Foydalanuvchi talabi: plagin **faqat Web Mercator (EPSG:3857)** koordinata
tizimida ishlasin. Sozlamalar oynasidagi ro'yxatda boshqa tizimlar
ko'rsatilmaydi va standart (default) tizim ham shu.

- **WGS 1984 Web Mercator (Auxiliary Sphere)** (EPSG:3857)
  - Google/ArcGIS "Web Mercator Auxiliary Sphere" bilan bir xil
  - Chizma birliklari = Web Mercator metrlari
  - Plagin kodi: `CoordinateSystemType.WebMercator_3857` -> `WebMercatorProjection`
  - Standart va yagona ko'rsatiladigan tizim.

Kodda boshqa proyeksiyalar (Pulkovo 1942 GK Z12, WGS84 Geographic/UTM) hali
ham mavjud, lekin UI'da yashirilgan va sozlama yuklanganda avtomatik
EPSG:3857 ga moslanadi.
