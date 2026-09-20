# 01 - Pano Arayüzü ve Sürükle-Bırak (Faz 4)

Bu fazda, simülatörün arka plan beynini görsel bir arayüze bağlamaya başladık. Artık malzeme çekmecemiz (Drawer) var ve parçaları tutup panoya (DIN raylarına) sürükleyebiliyoruz.

---

## 🛠 Kullanılan Teknolojiler
Öğrenme odaklı bir proje olduğumuz için bu fazda hiçbir harici kütüphane (React, Vue, jQuery UI vb.) **KULLANMADIK**. Her şey saf (Vanilla) JavaScript, HTML5 ve SVG ile inşa edildi.

*   **Pano:** Bir `<svg>` etiketi. Bu sayede panoya eklediğimiz parçalar piksel piksel değil, vektörel olarak çiziliyor (Bulanıklaşmadan yakınlaştırılabilir).
*   **Parçalar:** `<g>` (grup) etiketleri. İçerisinde gövdeyi temsil eden bir `<rect>` ve terminalleri temsil eden ufak `<circle>` etiketleri var.
*   **DIN Rayı:** Gerçek panolardaki şalt malzemelerinin (otomat, kontaktör) oturtulduğu metal kızaklar. Simülatörde parçalar rastgele yerlere bırakılamaz; sadece bu 3 yatay rayın üzerine hizalanıp (snap) yapışırlar (Motor hariç, o saha bölümüne gider).

---

## 🧠 Nasıl Çalışıyor? (`app.js`)

`PanoApp` sınıfımız bu işin kalbidir. 
İki farklı "Sürükle-Bırak" (Drag & Drop) mekanizmasını aynı anda yönetir:

### 1. HTML5 Drag & Drop API (Çekmeceden Panoya)
Çekmecedeki HTML `<div>` kartlarını tutup SVG'nin içine sürüklerken kullandığımız API'dir.
*   `dragstart`: Kart tutulduğunda `dataTransfer` içine parçanın tipini (örn: `"contactor"`) kaydederiz.
*   `drop`: SVG üzerine bırakıldığında, farenin koordinatlarını okuyup `snapToRail` fonksiyonu ile raya hizalarız. Eğer geçerli bir noktaysa, `placements` (yerleşimler) dizisine yeni bir parça ekleyip ekranı tekrar çizeriz (`render()`).

### 2. Pointer Events (Pano İçindeki Parçayı Kaydırma)
Panoya bir kez yerleşmiş olan SVG `<g>` grubunu tekrar fareyle tutup sağa sola kaydırırken kullandığımız yöntemdir. HTML5 Drag & Drop, SVG elemanları üzerinde stabil çalışmaz. Bu yüzden `pointerdown`, `pointermove`, `pointerup` olaylarını dinleriz.
*   `setPointerCapture()`: Bu metot çok önemlidir. Fare parçanın üzerine basılıyken aniden çok hızlı sağa çekilirse fare imleci parçadan çıkabilir. Capture metodu farenin parçaya "kilitlenmesini" sağlar.

---

## ⚙️ C# ile İletişim (C# -> JS Veri Aktarımı)
C#'taki parçaların (katalog) özelliklerini JavaScript'in nasıl bildiği merak edilebilir.
`Play.cshtml` sayfasını sunucu hazırlarken, `ComponentCatalog.GetAll()` metodunu çalıştırıp parçaların tüm bilgilerini (genişlikleri, terminal noktalarının X ve Y koordinatları) bir JSON formatına dönüştürüp `window.COMPONENT_CATALOG` isimli global JS değişkenine bastık.
Bu sayede JS kodu, bir kontaktörün kaç pini olduğunu ve bu pinlerin nerelere çizileceğini sunucuya hiç sormadan (sıfır gecikme) çizebiliyor.
