# 01 - Animasyonlar ve Elektrik Planı (Faz 6)

Bu fazda, pano dizimi esnasında çok ihtiyaç duyulan elektrik planını (şemasını) görebileceğimiz yapıyı ve devrenin canlandığını gösteren animasyonları kodladık.

---

## 🏗️ Neler Yapıldı?

### 1. Şema Gösterimi (Plan Modal)
- Pano dizen kişi, panoyu kafasına göre dizmez; her zaman elinde bir otomasyon şeması (elektrik projesi) vardır.
- Sağ üstteki "Seviye Planı" kutusuna bir "Büyüt" (Zoom) butonu ekledik.
- Kullanıcı buna tıkladığında ekranın ortasına dev bir pencere (Modal) açılır. Gelecekte gerçek resim dosyalarını bu alana entegre edeceğiz (EPLAN veya Autocad Electrical ile çizilmiş kumanda devresi resimleri).

### 2. Hareketli Kablolar (Enerji Akışı)
- Elektrik devresi başarılı bir şekilde kapandığında (Motor döndüğünde, kısadevre olmadığında), statik duran SVG çizgilerini (kabloları) canlandırdık.
- `components.css` içine `@keyframes flowAnts` isimli bir animasyon yazıldı.
- `stroke-dasharray: 8 4` kullanılarak kablolar tireli (kesik) hale getirildi ve `stroke-dashoffset` değeri kaydırılarak içinde elektron akıyormuş gibi (kayan ışık) bir efekt verildi.
- `app.js`'teki `runSimulation()` metodunda, C#'tan gelen sonuç başarılıysa tüm kablolara `is-energized` CSS sınıfı eklenir.

### 3. Butonlara Basılma Efekti
- Start ve Stop butonlarına `:active` CSS kuralı ekleyerek tıklandıklarında fiziksel olarak içeri doğru göçme (`transform: scale(0.95)` ve `box-shadow`) animasyonu verdik. Bu da kullanıcının sistemle kurduğu etkileşim hissini arttırır.

---

## 🎨 Öğrenme Notu (CSS vs JS Animasyon)
Kablolardaki akım hissini JavaScript ile her saniye `requestAnimationFrame` kullanarak da yapabilirdik. Ancak bu, tarayıcının işlemcisini yorardı. Biz bunun yerine doğrudan ekran kartı (GPU) üzerinden donanımsal hızlandırma ile çalışan `CSS Animations`'ı kullandık. Tarayıcı CSS'teki bu hareketleri JavaScript'ten çok daha az maliyetli şekilde oynatır.
