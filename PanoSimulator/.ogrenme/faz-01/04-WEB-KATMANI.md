# 04 — Web Katmanı: ASP.NET MVC Nasıl Çalışıyor?

---

## MVC'nin Tam Akışı (Adım Adım)

```
1. Kullanıcı tarayıcıda yazar: http://localhost:5050/simulator/4
          ↓
2. Program.cs'deki kural eşleşir:
   "simulator/{id:int}" → SimulatorController, Play metodu, id=4
          ↓
3. SimulatorController.Play(4) çalışır
   ViewBag.LevelId = 4
   return View();
          ↓
4. _ViewStart.cshtml devreye girer: Layout = "_Layout"
          ↓
5. _Layout.cshtml çalışır (HTML iskeleti):
   <html><head>...</head><body>
       @RenderBody()  ← buraya Play.cshtml enjekte edilir
   </body></html>
          ↓
6. Play.cshtml çalışır:
   @{ int levelId = ViewBag.LevelId ?? 1; }
   <h1>Seviye @levelId — ...</h1>
          ↓
7. Sunucu HTML üretir, tarayıcıya gönderir
          ↓
8. Tarayıcı HTML'i gösterir, CSS yükler, app.js çalışır
```

---

## `Program.cs` — Her Satır Ne Yapıyor?

```csharp
// Uygulamayı oluştur
var builder = WebApplication.CreateBuilder(args);

// MVC'yi kaydet (Controller ve View desteği aç)
builder.Services.AddControllersWithViews();

// Uygulamayı derle
var app = builder.Build();

// Geliştirme ortamında değilsek hata sayfası göster
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();  // HTTPS zorunluluğu
}

// Sıralama önemli! Bu middleware'ler sırayla çalışır:
app.UseHttpsRedirection();  // HTTP → HTTPS yönlendir
app.UseStaticFiles();       // wwwroot/css, wwwroot/js dosyalarını sun
app.UseRouting();           // URL'yi analiz et
app.UseAuthorization();     // Yetki kontrolü (şimdilik boş)

// URL yönlendirme kuralları:
app.MapControllerRoute(
    name: "simulator",
    pattern: "simulator/{id:int}",           // /simulator/4 ← id mutlaka int olmalı
    defaults: new { controller = "Simulator", action = "Play" }
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
    // / → Home/Index
    // /Home → Home/Index
    // /Simulator/Play → Simulator/Play
);

app.Run();  // Sunucuyu başlat, HTTP isteklerini dinle
```

---

## `HomeController.cs` — Kodun Her Satırı

```csharp
using Microsoft.AspNetCore.Mvc;

namespace PanoSimulator.Web.Controllers;

public class HomeController : Controller  // Controller'dan miras alıyor
{
    public IActionResult Index()          // GET / → bu metod çalışır
    {
        return View();
        // Views/Home/Index.cshtml'i bul ve çalıştır
        // Model geçirmiyoruz çünkü şimdilik hardcoded HTML
    }

    [ResponseCache(...)]
    public IActionResult Error()          // Hata sayfası
    {
        return View();
    }
}
```

### `IActionResult` nedir?
Metodun ne döndüreceğini belirten tip. Farklı türleri var:
- `View()` → HTML sayfası döndür
- `RedirectToAction("Index", "Home")` → başka URL'ye yönlendir
- `Json(nesne)` → JSON döndür (Faz 2'de API endpoint'leri için)
- `NotFound()` → 404 hatası

---

## `SimulatorController.cs` — Kodun Her Satırı

```csharp
public class SimulatorController : Controller
{
    private readonly IWebHostEnvironment _env;  // wwwroot yolunu almak için

    // Constructor Injection — ASP.NET otomatik sağlıyor
    public SimulatorController(IWebHostEnvironment env)
    {
        _env = env;
    }

    public IActionResult Play(int id)  // GET /simulator/4 → id=4
    {
        // Geçersiz seviye kontrolü
        if (id < 1 || id > 9)
            return RedirectToAction("Index", "Home");  // Ana sayfaya gönder

        ViewBag.LevelId = id;   // View'a veri geç (basit yol)
        return View();           // Views/Simulator/Play.cshtml çalıştır
    }
}
```

### `Constructor Injection` nedir?
Program.cs'de `builder.Services.AddControllersWithViews()` dediğimizde
ASP.NET bir "servis konteyner"i oluşturuyor. Controller oluşturulurken
ihtiyaç duyduğu servisleri otomatik "enjekte ediyor" (veriyor).

Faz 2'de şöyle kullanacağız:
```csharp
// Program.cs'e ekleyeceğiz:
builder.Services.AddSingleton<CircuitSolver>();

// Controller'a gelecek:
public SimulatorController(IWebHostEnvironment env, CircuitSolver solver)
{
    _solver = solver;  // Solver hazır!
}
```

### `ViewBag` vs Model — Fark Nedir?

**ViewBag** (hızlı ve kirli — basit veriler için):
```csharp
// Controller:
ViewBag.LevelId = 4;
ViewBag.Ad = "Mühürleme";

// View:
@ViewBag.LevelId  → 4
@ViewBag.Ad       → "Mühürleme"
```

**Typed Model** (doğru yol — Faz 6'da kullanacağız):
```csharp
// Bir model sınıfı:
public class LevelViewModel
{
    public int Id { get; set; }
    public string Ad { get; set; }
    public List<string> Testler { get; set; }
}

// Controller:
var model = new LevelViewModel { Id = 4, Ad = "Mühürleme" };
return View(model);

// View:
@model LevelViewModel
<h1>Seviye @Model.Id — @Model.Ad</h1>
```
Typed model daha güvenli — derleme zamanında hata yakalar.

---

## `_Layout.cshtml` — Her Satır Ne Yapıyor?

```html
<!DOCTYPE html>
<html lang="tr">   ← Türkçe (erişilebilirlik, SEO)
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="description" content="@(ViewData["Description"] ?? "...")" />
    ← ViewData["Description"] set edilmişse kullan, yoksa varsayılan

    <title>@(ViewData["Title"] != null ? $"{ViewData["Title"]} — " : "")Pano Simülatörü</title>
    ← Sayfa başlığı: "Seviyeler — Pano Simülatörü" veya "Pano Simülatörü"

    <!-- Google Fonts yükleme (İnter + JetBrains Mono) -->
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    ...

    <!-- CSS sıra önemli! Önce token'lar, sonra layout, sonra bileşenler -->
    <link rel="stylesheet" href="~/css/variables.css" />
    <link rel="stylesheet" href="~/css/layout.css" />
    <link rel="stylesheet" href="~/css/components.css" />

    <!-- Sayfa özel CSS varsa buraya eklensin -->
    @await RenderSectionAsync("Styles", required: false)
</head>
<body>
    @RenderBody()   ← Index.cshtml veya Play.cshtml buraya eklenir

    <script src="~/js/app.js"></script>
    @await RenderSectionAsync("Scripts", required: false)
    ← Sayfa özel JS varsa buraya eklensin
</body>
</html>
```

### `~/` ne anlama geliyor?
`~/css/variables.css` → `/css/variables.css` → `wwwroot/css/variables.css`
Tilde (~) "uygulama kök dizini" anlamına geliyor. Razor otomatik çeviriyor.

---

## `Play.cshtml` — 4 Bölgeli Ekran

```
sim-layout (CSS Grid — 3 satır)
│
├── sim-toolbar (satır 1: --toolbar-h=56px sabit)
│   ├── Seviye adı + açıklama
│   └── Butonlar: Enerji Ver | START | STOP | Termik Attır | Sil | Kontrol Et
│
├── sim-main (satır 2: kalan yükseklik)
│   ├── #panel-svg (tam boyut SVG — pano)
│   │   ├── #panel-body (gri metal gövde)
│   │   ├── #rail-0 (DIN Ray 1)
│   │   ├── #rail-1 (DIN Ray 2)
│   │   ├── #rail-2 (DIN Ray 3)
│   │   ├── #field-zone (saha bölgesi — kesikli kenarlık)
│   │   ├── #wires-layer (kablolar — Faz 5'te JS doldurur)
│   │   └── #parts-layer (parçalar — Faz 4'te JS doldurur)
│   │
│   ├── #plan-panel (sağ üst — position:absolute)
│   │   ├── Başlık + "Gizle" butonu
│   │   ├── Sekmeler: Güç Devresi | Kumanda Devresi
│   │   └── Şema alanı (Faz 6'da SVG şema gelecek)
│   │
│   └── #status-console (sol alt — position:absolute)
│       ├── Başlık + "Küçült" butonu
│       └── Log alanı (● ▲ ■ · mesajları)
│
└── sim-drawer (satır 3: --drawer-h=148px sabit)
    ├── Kablo rengi seçici (6 renk dairesi)
    └── Parça kartları (8 kart: Otomat, Kontaktör, Termik, NO Buton, NC Buton, Motor, Lamba, Zaman Rölesi)
```

### Parça SVG İkonları
Her parça kartında küçük bir SVG ikon var.
Gerçek ürün fotoğrafı değil, şematik ama tanınabilir:
```html
<!-- Kontaktör ikonu (Play.cshtml'den) -->
<svg width="36" height="36" viewBox="0 0 36 36">
    <rect x="3" y="2" width="30" height="32" rx="2" .../>  ← Gövde
    <rect x="7" y="14" width="22" height="8" .../>          ← Kontaklar bloğu
    <circle cx="18" cy="30" r="3" .../>                     ← Bobin simgesi
</svg>
```
