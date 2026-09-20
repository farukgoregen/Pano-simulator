# Faz 7: Zaman Röleleri (Timer) ve Zaman Kavramı ⏱️

Bu fazda, simülatöre **zaman** (tick) kavramını dahil ettik. Önceki fazlarda simülasyon (CircuitSolver) sadece bir anlık (statik) durumu çözüyordu. Bir butona basıldığında devre çözülüyor, lambalar yanıyor veya motor dönüyordu. Zaman rölelerinin eklenmesiyle birlikte sistem, "gerçek zamanlı" bir akış kazanmıştır.

## 1. Zaman Rölesi (KT - TON) Nedir?

*TON (Timer On Delay - Çekmede Gecikmeli)* zaman rölesi, sanayide belirli bir sürecin hemen değil, ayarlanmış bir süre sonra başlamasını sağlamak için kullanılır.

*   **A1-A2 (Bobin):** Gerilim uygulandığında zamanlayıcı saniye saymaya başlar. Enerji kesilirse sayaç anında sıfırlanır.
*   **15 (Ortak Uç):** Akımın girdiği kontak.
*   **16 (NC - Normalde Kapalı):** Süre sayılırken akımı geçirir. Süre dolduğunda açılır ve akımı keser.
*   **18 (NO - Normalde Açık):** Süre sayılırken akımı geçirmez. Süre dolduğunda kapanır ve motoru/sistemi çalıştırır.

## 2. Yazılım Mimarisine Etkisi

Zaman kavramını simüle etmek mimaride özel bir yaklaşım gerektirdi:

1.  **State (Durum) Takibi:** `PartState` sınıfına `TimerElapsedMs` (Geçen süre) ve `TimerSetpointMs` (Hedef süre, örn: 3000ms) alanları eklendi.
2.  **C# Solver:** `CircuitSolver` artık rölenin A1-A2 uçları arasında gerilim olup olmadığına bakıp `CoilEnergized` değerini True yapıyor. Ardından `TimerElapsedMs >= TimerSetpointMs` koşuluna bakarak 15-18 kontağını kapatıyor.
3.  **Frontend Tick (app.js):** Sistemde enerjilenmiş bir zaman rölesi varsa, Javascript `setInterval` ile her 200ms'de bir "tick" üretir. Geçen süreyi artırarak `runSimulation` üzerinden backend'e yeni durumu iletir. Bu sayede süre dolduğunda C# tarafı röleyi kapatır, JS tarafı da motorun dönme animasyonunu başlatır.
4.  **UI Geri Bildirimi:** Kullanıcının zamanın aktığını hissetmesi için `renderParts()` fonksiyonunda SVG üzerine dinamik olarak azalan saniye (Örn: 1.2s) yazdırılır.

Bu faz, otomasyon (PLC) sistemlerinin temelini oluşturan ardışık ve zamana bağlı (sequential) kontrol algoritmalarının kapısını açmıştır.
