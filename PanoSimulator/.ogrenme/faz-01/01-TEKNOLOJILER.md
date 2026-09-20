# 01 — Kullandığımız Teknolojiler

---

## 1. C# (C Sharp) — Ana Programlama Dili

### Nedir?
Microsoft'un geliştirdiği, nesne yönelimli bir programlama dili.
Java'ya çok benzer ama daha modern özellikler içerir.

### Bu projede ne için kullandık?
- Simülasyon motorunu (elektrik devre analizi)
- Web sunucusunu (HTTP isteklerini karşılamak)
- Veri tiplerini (kontaktör, kablo, terminal nedir?)

### Temel C# kavramları bu projede:

**`record`** → Değişmez veri kabı. İki record aynı değerlere sahipse "eşit" sayılır.
```csharp
// Terminal.cs'de böyle kullandık:
public record Terminal(string Id, double X, double Y);

// Kullanımı:
var t1 = new Terminal("A1", 0, 0);
var t2 = new Terminal("A1", 0, 0);
bool esit = (t1 == t2);  // TRUE — record'lar değere göre karşılaştırılır
```

**`class`** → Değişebilir veri kabı. Durumu zamanla değişen şeyler için.
```csharp
// PartState.cs'de böyle kullandık:
public class PartState
{
    public bool CoilEnergized { get; set; }  // Bobin çekti mi? Değişebilir!
    public bool ThermalTripped { get; set; }  // Termik attı mı?
}
```

**`enum`** → Sabit seçenekler listesi.
```csharp
// Wire.cs'de böyle kullandık:
public enum WireColor { Black, Red, DarkBlue, LightBlue, YellowGreen, Orange }

// Kullanımı:
var kablo = new Wire(..., Color: WireColor.Red);  // Kırmızı = AC kumanda
```

**`Func<T, TResult>`** → Fonksiyon tipi. Bir fonksiyonu parametre olarak geçirebilirsin.
```csharp
// PartDefinition.cs'de böyle kullandık:
Func<PartState, IEnumerable<(string, string)>> InternalLinks

// Anlamı: "PartState al, terminal çiftleri döndür"
// Kontaktör için: state.CoilEnergized=true ise → [(1,2), (3,4), (5,6), (13,14)]
// Faz 2'de her malzeme için bu fonksiyon yazılacak.
```

---

## 2. .NET 8 — Çalışma Ortamı (Runtime)

### Nedir?
C# kodunu çalıştıran platform. "Motor" gibi düşün — araba markası C#, motor .NET.

### Bu projede ne için kullandık?
Tüm proje .NET 8 üzerinde çalışıyor. `dotnet run`, `dotnet build`, `dotnet test`
komutlarını çalıştırınca .NET devreye giriyor.

### Neden .NET 8?
- En güncel LTS (Uzun Vadeli Destek) sürümü
- Hızlı, ücretsiz, Windows/Linux/Mac üzerinde çalışır

---

## 3. ASP.NET Core MVC — Web Çerçevesi

### Nedir?
**M**odel-**V**iew-**C**ontroller. Web uygulaması yapmak için Microsoft'un çerçevesi.
Üç kavram var:

```
KULLANICI tarayıcıda URL yazar
    ↓
CONTROLLER (C) → URL'yi yakalar, ne yapılacağına karar verir
    ↓
MODEL (M) → Veriyi hazırlar (veritabanı, JSON, hesaplama)
    ↓
VIEW (V) → HTML'i oluşturur ve kullanıcıya gönderir
```

### Bu projede örnek:
```
Kullanıcı: GET /simulator/4
    ↓
SimulatorController.Play(id: 4)   ← CONTROLLER
    ↓
ViewBag.LevelId = 4               ← MODEL (basit)
    ↓
Views/Simulator/Play.cshtml       ← VIEW
    ↓
HTML sayfası tarayıcıya gider
```

### Neden MVC?
- Okul projesinde MVC istenmiş → direkt uyuyor
- Her katman ayrı → değiştirmesi kolay
- C#'çıların en çok kullandığı yapı

---

## 4. Razor / `.cshtml` — HTML + C# Karışımı Şablon

### Nedir?
Normal HTML içine `@` işaretiyle C# kodu yazabilirsin.
Sunucu bunu işleyip saf HTML'e çevirir.

### Örnek (Play.cshtml'den):
```html
@{
    int levelId = ViewBag.LevelId ?? 1;   // C# kodu
    string ad = "Mühürlemeli Devre";      // C# değişkeni
}

<h1>Seviye @levelId — @ad</h1>
<!-- Çıktı: <h1>Seviye 4 — Mühürlemeli Devre</h1> -->

@if (levelId == 4)
{
    <p>Bu MVP seviyesi!</p>    <!-- Koşullu HTML -->
}
```

### Neden Razor?
- C# bilgisiyle HTML yazabiliyorsun
- Sunucu tarafında render → SEO dostu
- MVC ile entegre geliyor

---

## 5. xUnit — Otomatik Test Çerçevesi

### Nedir?
"Bu kod doğru çalışıyor mu?" sorusunu otomatik cevaplayan araç.
`dotnet test` çalıştırınca her test ayrı ayrı koşulur.

### Temel kavramlar:
```csharp
[Fact]  // "Bu bir test metodu"
public void Kontaktor_Baslangicta_Dusuk()
{
    // HAZIRLA
    var state = new PartState();

    // KONTROL ET
    Assert.False(state.CoilEnergized);   // "CoilEnergized false olmalı"
    Assert.False(state.ThermalTripped);  // "ThermalTripped false olmalı"
}
```

`Assert` = "iddia et". Eğer iddia yanlışsa test kırılır (kırmızı), doğruysa geçer (yeşil).

### Neden test yazıyoruz?
- Simülasyon motoru kritik → yanlış elektrik mantığı = yanlış eğitim
- Kod değişince testler "bir şeyi bozdun mu?" diye uyarır
- Faz 2'de "start-stop mühürleme çalışıyor mu?" testini yazacağız → elle denemek zorunda kalmayacağız

---

## 6. SVG — Pano Çizimi

### Nedir?
**S**calable **V**ector **G**raphics. Matematiksel şekillerle çizim.
Piksel değil, koordinat tabanlı → zoom'da bozulmuyor.

### Bu projede neden SVG?
- Terminal noktalarına tıklamak kolay (her noktanın X,Y koordinatı var)
- Kablo çizmek kolay (iki nokta arası `<line>` veya `<path>`)
- CSS ile stil verilebiliyor (`fill="var(--clr-accent)"`)
- Canvas'a göre erişilebilirlik daha iyi

### Faz 1'de ne yazdık:
```html
<!-- DIN Ray çizimi (Play.cshtml'den) -->
<rect x="0" y="8" width="600" height="8"
      fill="var(--clr-rail)" rx="1"/>
```
Faz 4'te parçalar ve kablolar da SVG ile çizilecek.

---

## 7. CSS Değişkenleri (Custom Properties)

### Nedir?
CSS içinde `--` ile başlayan, her yerde kullanılabilen değerler.

### Neden kullandık?
```css
/* variables.css'de tanımladık: */
:root {
    --clr-bg: #151820;       /* Koyu tema */
    --wire-red: #cc2222;     /* IEC 60204-1 kumanda kablosu */
}

/* Başka yerde kullandık: */
.btn--stop { color: var(--clr-status-fault); }

/* Açık tema için sadece değişkeni değiştirmek yeterli: */
@media (prefers-color-scheme: light) {
    :root { --clr-bg: #dde0e8; }  /* Geri kalan her şey otomatik değişir */
}
```

### JSON — Seviye Dosyaları

Seviye bilgilerini kod içine değil, ayrı `.json` dosyasına yazdık.
Böylece **programcı olmayan biri de yeni seviye ekleyebilir.**

```json
{
  "id": 1,
  "ad": "Faz-Nötr ve Lamba",
  "kaynak": "Kitap §1.1, §2.1",
  "testler": []   ← Faz 6'da dolacak
}
```
