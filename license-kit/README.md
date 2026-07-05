# AutoCAD plaginlari uchun litsenziya to'plami (license-kit)

Bu to'plam — barcha AutoCAD plaginlaringizga **bir xil aktivatsiya / litsenziya
tizimini** tez qo'shish uchun tayyor namunalar to'plami. RSA-2048 imzo,
mashinaga bog'langan (Machine ID), muddatli. Format: **Base32(GZip(...))** —
`AutoCAD_table` va `AutoCAD_Google` da ishlatilgan usul bilan aynan bir xil.

> Yangi plagin uchun siz faqat **bitta faylni** (`LicenseConfig.cs`) tahrirlaysiz.

---

## Tarkibi

```
license-kit/
├── src-core/                 ← HAR DOIM ko'chiriladi
│   ├── LicenseConfig.cs       ← FAQAT SHUNI TAHRIRLAYSIZ (public key, nom, buyruq nomlari)
│   ├── LicenseCodec.cs        ← Base32 + GZip (o'zgarmaydi)
│   ├── MachineIdProvider.cs   ← Machine ID = Base32(GZip(ProcessorId)) (o'zgarmaydi)
│   ├── LicenseManager.cs      ← tekshiruv + o'rnatish (o'zgarmaydi)
│   └── LicenseGate.cs         ← buyruq darvozasi (o'zgarmaydi)
├── ui-wpf/                    ← WPF plaginlar uchun (BITTASINI tanlang)
│   ├── ActivationWindow.cs
│   └── LicenseCommands.cs
├── ui-winforms/              ← WinForms plaginlar uchun (BITTASINI tanlang)
│   ├── ActivationForm.cs
│   └── LicenseCommands.cs
├── tools/
│   ├── KalitJuftiYaratish.html   ← RSA juft kalit yaratish (public + private)
│   └── LitsenziyaGenerator.html  ← litsenziya imzolash (universal, barcha plaginlar uchun)
└── README.md
```

Barcha kod `PluginLicensing` namespace'ida — plagin nomidan mustaqil, shuning
uchun namespace'ni o'zgartirish shart emas.

---

## Yangi plaginga qo'shish (qadam-baqadam)

### 1) Kalit jufti yarating (bir marta, har plaginga alohida)
`tools/KalitJuftiYaratish.html` ni brauzerda oching → **Yangi kalit jufti yaratish**.
- **Public** kalitni saqlang (keyingi qadamda kerak).
- **Private** kalitni xavfsiz joyda saqlang (litsenziya imzolashda ishlatiladi, HECH KIMGA bermang).

### 2) Fayllarni ko'chiring
Plaginingizga `Licensing/` papkasi ochib, quyidagilarni ko'chiring:
- butun `src-core/` (5 ta fayl);
- **plagin turiga qarab bittasi**: `ui-wpf/` **yoki** `ui-winforms/` (2 ta fayl).

> WPF (lenta/palitra WPF bo'lsa) → `ui-wpf`. Oddiy WinForms dialoglar bo'lsa → `ui-winforms`.

### 3) `LicenseConfig.cs` ni tahrirlang (YAGONA sozlama)
```csharp
public const string PublicKeyXml = "<RSAKeyValue><Modulus>...1-qadamdagi PUBLIC...</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
public const string ProductName  = "MeningPlaginim";
public const string CmdActivate  = "MPLGNACTIVATE";  // har plaginда NOYOB!
public const string CmdMachineId = "MPLGNID";
public const string CmdInstall   = "MPLGNLIC";
```

### 4) Referenslar (.csproj)
- Har doim: **System.Management** (Machine ID uchun).
- `ui-wpf` uchun: WPF (PresentationFramework/PresentationCore/WindowsBase/System.Xaml)
  — SDK loyihada `<UseWPF>true</UseWPF>`.
- `ui-winforms` uchun: WinForms — SDK loyihada `<UseWindowsForms>true</UseWindowsForms>`.
- **Klassik (non-SDK) csproj** bo'lsa, ko'chirilgan har bir `.cs` uchun
  `<Compile Include="Licensing\....cs" />` qatorini qo'shing. SDK-style csproj
  esa avtomatik topadi.

### 5) Buyruqlaringizni himoyalang
Himoyalanadigan har bir buyruq metodining eng boshiga qo'shing:
```csharp
[CommandMethod("MENING_BUYRUGIM")]
public void MeningBuyrugim()
{
    if (!PluginLicensing.LicenseGate.Ensure()) return;   // <-- shu qator
    // ... buyruq mantig'i ...
}
```
> Aktivatsiya buyruqlarini (CmdActivate/CmdMachineId/CmdInstall) himoyalamang —
> ular litsenziyasiz ham ishlashi kerak.

### 6) Lentaga “Aktivatsiya qilish” tugmasi
Tugma bosilganда `CmdActivate` buyrug'ini ishga tushiring.

**WPF Ribbon (Autodesk.Windows):**
```csharp
button.CommandParameter = PluginLicensing.LicenseConfig.CmdActivate;
// RibbonCommandHandler.Execute: doc.SendStringToExecute("_." + param + " ", true, false, true);
```
**Yoki oddiy makro:** `^C^C_MPLGNACTIVATE` (buyruq nomingiz).

### 7) Build qiling — tayyor.

---

## Aktivatsiya ish oqimi

**Mijoz tomoni:**
1. Plaginда lenta → **Aktivatsiya qilish** (yoki `CmdActivate` buyrug'i).
2. Oynadagi **Product key** (Machine ID) ni **Copy** qilib sizga yuboradi.

**Vendor (siz):**
3. `tools/LitsenziyaGenerator.html` → Product key + muddat + **shu plaginning
   private kaliti** (ixtiyoriy: public kalitni ham qo'ysangiz `✓ mos` ko'rsatadi)
   → **Litsenziya yaratish** → `.lic` fayl yoki matn.

**Mijoz:**
4. **Activation key** maydoniga **Paste** → **Activate**. “Faollashtirildi”.
   Litsenziya `%APPDATA%\<ProductName>\license.lic` ga saqlanadi; himoyalangan
   buyruqlar shundan keyin ishlaydi.

---

## Muhim eslatmalar
- Har plaginга **alohida kalit jufti** yarating. Bir plagin litsenziyasi
  boshqasida ishlamaydi (ochiq kalitlar har xil).
- **Buyruq nomlari** (`Cmd*`) har plaginда noyob bo'lsin — aks holda bir necha
  plagin birga yuklanganда AutoCAD “duplicate command” xatosi beradi.
- `LicenseConfig.cs` ga faqat **OCHIQ** kalit qo'yiladi. MAXFIY kalit hech qachon
  plaginga/kodga joylanmaydi.
- `MachineIdProvider` faqat CPU `ProcessorId` ga tayanadi (Topography usuli).
  Kuchliroq bog'lash kerak bo'lsa (CPU+BIOS), ayting — moslashtirib beraman.
