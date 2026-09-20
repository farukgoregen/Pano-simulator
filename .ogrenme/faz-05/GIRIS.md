# 01 - Kablolama (Faz 5)

Bu fazda, simülatördeki donanımları birbirine bağlamayı sağlayan kablo mekanizmasını inşa ettik.

---

## 🏗️ Neler Yapıldı?

1. **Güç Dağıtım Bloğu (Supply Block):**
   - Panonun sol kısmına L1, L2, L3, N, ve PE hatlarından oluşan ana besleme noktalarını ekledik. Böylece enerjiyi bu noktalardan alıp devremize dağıtabileceğiz.
2. **Renk Seçimi:**
   - IEC standartlarına göre kablo renkleri seçilebilir hale getirildi (Siyah, Kırmızı, Mavi vb.). 
   - Hangi renge tıklarsan `currentWireColor` isimli değişkende o tutuluyor.
3. **Kablo Çekme Mekanizması:**
   - SVG içerisindeki her bir terminale "Tıklama" (`pointerdown`) özelliği eklendi.
   - Sürükle-Bırak mantığı web üzerinde sıkıntılı olabileceği için **"İki Kere Tıklama" (Point-to-Point)** mantığını kurduk:
     - 1. Vida: Başlangıç noktası seçilir. Vida mavi bir parlamayla işaretlenir.
     - 2. Vida: Hedef noktası seçilir. İki vida arasında seçilen renkte dümdüz bir `<line>` çizilir.
4. **Silme Modu:**
   - Sağ üst köşeye yakın "Sil" (Çöp Kutusu mantığı) butonuna basıldığında silme modu aktif olur. Farenin imleci değişir ve artık çizilmiş kablolara tıklayarak onları silebiliriz.
5. **Simülasyon Bağlantısı (Backend API):**
   - C# tarafında `SimulatorController.cs` içine bir `/Simulator/Solve` API ucu açtık.
   - Ön yüzdeki "Kontrol Et" butonuna basınca, o an panoda ne kadar malzeme ve kablo varsa JSON paketine dönüştürülüp bu API'ye gönderiliyor.
   - C# arka planda `CircuitSolver`'ı çalıştırıp matematiksel sonuçları (Otomat attı mı? Motor dönüyor mu?) hesaplıyor ve ön yüze gönderiyor.
   - Sonuçlar sağ alttaki "Pano Durumu" konsoluna düşüyor.

---

## 🧠 Nasıl Çalışıyor? (`app.js` & `SimulatorController.cs`)

Kablolar JavaScript'te şu veri yapısıyla tutuluyor:
```javascript
{
    id: "w1",
    fromPart: "__supply", // Örneğin Güç kaynağı
    fromTerm: "L1",       // L1 fazı
    toPart: "p1_breaker", // Otomat sigorta
    toTerm: "1",          // 1 numaralı girişi
    color: "black"        // Güç kablosu
}
```

"Kontrol Et" dendiğinde bu veri C# API'sine gider:
```csharp
[HttpPost]
public IActionResult Solve([FromBody] SolveRequest request)
```
C# tarafındaki motorumuz bunu bizim eski `Wire` ve `Placement` nesnelerine dönüştürür ve `solver.Solve(state)` diyerek çözer.

## ⚠️ Elektrik Mühendisliği Notu
Bu aşamada kablolar görsel olarak kuş uçuşu (dümdüz A'dan B'ye) gidiyor. Gerçek hayatta pano kabloları kanal içinden geçer ve köşeli döner. Bu köşeli çizim (Manhattan Routing) çok ileri düzey matematik gerektirir. Biz işlevselliğe odaklandığımız için önce dümdüz çizerek bağladık. Faz 8'de vaktimiz olursa güzelleştireceğiz.
