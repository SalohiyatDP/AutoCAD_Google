using System.Reflection;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Runtime;

// =====================================================================================
//  Assembly haqidagi umumiy ma'lumotlar.
//  Ushbu atributlar yig'ilgan (compiled) DLL metama'lumotlarini belgilaydi.
// =====================================================================================
[assembly: AssemblyTitle("GoogleSatelliteCAD")]
[assembly: AssemblyDescription("AutoCAD Mechanical 2021 uchun Google Satellite fon xarita plagini")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("SalohiyatDP")]
[assembly: AssemblyProduct("GoogleSatelliteCAD")]
[assembly: AssemblyCopyright("Copyright © SalohiyatDP 2026")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// COM komponentlariga ko'rinmasligi kerak.
[assembly: ComVisible(false)]

// Versiyalar.
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

// =====================================================================================
//  AutoCAD plagin yuklanganda avtomatik ishga tushadigan IExtensionApplication
//  klassini ko'rsatadi. NETLOAD orqali DLL yuklanganda PluginEntry.Initialize()
//  metodi chaqiriladi.
// =====================================================================================
[assembly: ExtensionApplication(typeof(GoogleSatelliteCAD.Core.PluginEntry))]

// Buyruq sinflari joylashgan namespace'lar (ixtiyoriy, ishlash tezligini oshiradi).
[assembly: CommandClass(typeof(GoogleSatelliteCAD.Commands.MapOnCommand))]
[assembly: CommandClass(typeof(GoogleSatelliteCAD.Commands.MapOffCommand))]
[assembly: CommandClass(typeof(GoogleSatelliteCAD.Commands.CacheClearCommand))]
