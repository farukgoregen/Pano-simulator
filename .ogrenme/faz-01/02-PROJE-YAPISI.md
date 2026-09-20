# 02 — Proje Yapısı: Klasörler ve Dosyalar

---

## Neden 3 Ayrı Proje?

```
PanoSimulator/
├── PanoSimulator.Engine/        ← MOTOR
├── PanoSimulator.Engine.Tests/  ← TESTLER
└── PanoSimulator.Web/           ← WEB
```

Tek bir proje de yapabilirdik ama **3 ayrı proje** yaptık çünkü:

| Sebep | Açıklama |
|-------|----------|
| **Sorumluluk ayrımı** | Motor, tarayıcıyı bilmiyor. Web, elektrik hesabını bilmiyor. Her biri kendi işini yapıyor. |
| **Test edilebilirlik** | Motor'u web olmadan test edebiliyoruz. Faz 2'de devre testlerini sunucu başlatmadan çalıştıracağız. |
| **İleride değiştirilebilirlik** | İstersen web tarafını React ile değiştirebilirsin — motor aynı kalır. |
| **Okul projesi gerçekçiliği** | Gerçek yazılım şirketlerinde proje böyle ayrılır. |

---

## Projelerin Birbirine Bağlanması (Referans)

```
PanoSimulator.Engine.Tests → PanoSimulator.Engine
PanoSimulator.Web          → PanoSimulator.Engine
```

Yani:
- Web projesi, Engine'deki tipleri (`PanelState`, `CircuitSolver` vb.) kullanabilir
- Test projesi, Engine'deki her şeyi test edebilir
- Engine, hiç kimseye bağımlı değil (bağımsız)

Bu bağlantıları `.csproj` dosyasında `<ProjectReference>` ile tanımladık:
```xml
<!-- PanoSimulator.Web.csproj içinde: -->
<ProjectReference Include="..\PanoSimulator.Engine\PanoSimulator.Engine.csproj" />
```

---

## `PanoSimulator.sln` — Ne İşe Yarıyor?

```
PanoSimulator.sln
```

Solution dosyası. İçinde kod yok, sadece "bu üç proje bir arada" diyor.
`dotnet build PanoSimulator.sln` deyince üç projeyi de birden derler.

**Bu dosyayı elle düzenleme** — `dotnet sln add` komutu otomatik güncelliyor.

---

## `PanoSimulator.Engine/` — Motor Klasörü (Detaylı)

```
PanoSimulator.Engine/
│
├── PanoSimulator.Engine.csproj     ← Proje ayarları
│
├── Types/                          ← Veri tipleri (ne var elimizde?)
│   ├── Terminal.cs
│   ├── Wire.cs
│   ├── PartDefinition.cs
│   ├── Placement.cs
│   ├── PartState.cs
│   └── PanelState.cs
│
├── Components/                     ← Malzeme kataloğu
│   └── ComponentCatalog.cs
│
└── Solver/                         ← Devre çözücü
    └── CircuitSolver.cs
```

### `PanoSimulator.Engine.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>      ← null güvenliği açık
    <ImplicitUsings>enable</ImplicitUsings>  ← ortak using'ler otomatik eklenir
  </PropertyGroup>
</Project>
```
Bu dosya "bu bir .NET 8 class library projesi" diyor. Başka bir şey yok.

### `Types/` Klasörü
Her dosya bir kavramı temsil ediyor. Detayı 03-ENGINE-TIPLERI.md'de.

### `Components/ComponentCatalog.cs`
Şimdilik boş. Faz 2'de dolacak:
```csharp
// "Kontaktör" malzemesini kataloğa kaydeder
// Terminal listesi: 1,2,3,4,5,6 (güç) + 13,14 (yardımcı) + A1,A2 (bobin)
// InternalLinks: bobin çekince hangi kontaklar kapanır?
```

### `Solver/CircuitSolver.cs`
Şimdilik no-op (hiçbir şey yapmıyor). Faz 2'de Union-Find algoritmasıyla dolacak.

---

## `PanoSimulator.Engine.Tests/` — Test Klasörü

```
PanoSimulator.Engine.Tests/
│
├── PanoSimulator.Engine.Tests.csproj   ← xUnit bağımlılığı burada
└── Phase1SmokeTests.cs                 ← 10 test metodu
```

### `.csproj` farkı (web projesinden farklı ne var?)
```xml
<PackageReference Include="xunit" Version="2.x" />
<PackageReference Include="Microsoft.NET.Test.Sdk" ... />
```
xUnit paketi eklenmiş — test çerçevesi.

---

## `PanoSimulator.Web/` — Web Projesi (Detaylı)

```
PanoSimulator.Web/
│
├── PanoSimulator.Web.csproj       ← Proje ayarları (ASP.NET MVC)
├── Program.cs                     ← Başlangıç noktası
│
├── Controllers/                   ← URL koordinatörleri
│   ├── HomeController.cs
│   └── SimulatorController.cs
│
├── Views/                         ← HTML şablonları
│   ├── _ViewImports.cshtml        ← Tüm View'lara ortak using/TagHelper
│   ├── _ViewStart.cshtml          ← Varsayılan layout belirler
│   ├── Shared/
│   │   └── _Layout.cshtml         ← Ana HTML iskeleti (tüm sayfalar bunu kullanır)
│   ├── Home/
│   │   └── Index.cshtml           ← Seviye seçim sayfası
│   └── Simulator/
│       └── Play.cshtml            ← Oyun ekranı
│
├── wwwroot/                       ← Statik dosyalar (sunucu işlemiyor, direkt gönderiyor)
│   ├── css/
│   │   ├── variables.css          ← Renkler, ölçüler, kablo renkleri
│   │   ├── layout.css             ← Sayfa yerleşimi (grid)
│   │   └── components.css         ← Buton, kart, log satırı stilleri
│   └── js/
│       └── app.js                 ← Tarayıcı etkileşimleri
│
└── Data/
    └── Levels/
        └── level-01.json          ← Seviye 1 verisi
```

### `Program.cs` — Neden Önemli?

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();  // MVC'yi aç
var app = builder.Build();

// URL yönlendirme kuralları:
app.MapControllerRoute(
    name: "simulator",
    pattern: "simulator/{id:int}",           // /simulator/4
    defaults: new { controller = "Simulator", action = "Play" }
);
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"  // /
);

app.Run();  // Sunucuyu başlat
```

`/simulator/4` gelince → `SimulatorController.Play(4)` çağrılır.
`/` gelince → `HomeController.Index()` çağrılır.

### `_ViewImports.cshtml` ve `_ViewStart.cshtml`

```cshtml
{{!-- _ViewImports.cshtml: Her View otomatik bunları "import" eder --}}
@using PanoSimulator.Web
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

```cshtml
{{!-- _ViewStart.cshtml: Her View varsayılan olarak _Layout'u kullanır --}}
@{
    Layout = "_Layout";
}
```

Bu iki dosya olmasa her `.cshtml` dosyasına ayrı ayrı `@using` yazman gerekirdi.

### `wwwroot/` — Neden Özel?

Bu klasördeki dosyalar **sunucu kodu çalıştırılmadan** direkt tarayıcıya gönderilir.
`variables.css` → 
` adresinden erişilebilir.
