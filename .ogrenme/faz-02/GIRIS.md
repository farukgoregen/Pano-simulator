# 📚 Faz 2 Öğrenme Kılavuzu — Simülasyon Motoru Kalbi

Bu fazda projenin beynini, yani **devre çözücüsünü (Circuit Solver)** yazdık. Hiç görsel bir şey eklemedik, tamamen matematik ve algoritmalarla uğraştık.

---

## 🛠 Neler Yaptık?

| Dosya | İçerik ve Amacı |
|-------|-----------------|
| `ComponentCatalog.cs` | 7 ana malzemeyi (Otomat, Kontaktör, Termik, Start, Stop, Motor, Lamba) terminalleri ve çalışma mantıklarıyla (InternalLinks) sisteme tanıttık. |
| `UnionFind.cs` | Elektrik ağlarını (net) bulmak için kullanılan **Union-Find (Ayrık Kümeler)** algoritmasını yazdık. |
| `CircuitSolver.cs` | Motorun beyni! Pano durumunu alır, bağlantıları çözer, potansiyelleri yayar ve kontaktör/motor durumlarını günceller. |
| `Phase2SolverTests.cs` | Mühürlemeli devreyi elle kodlayıp 12 farklı testten (boşta, start, mühürleme, stop, termik atması vb.) geçirdik. |

---

## 🧠 Nasıl Çalışıyor? (Solver Algoritması)

`CircuitSolver.Solve(state)` çalıştığında arka planda şu adımlar işlenir:

1. **Bağlantı Ağlarını (Net) Bulma:**
   `UnionFind` algoritması kullanılarak kablolar ve malzemelerin kapalı olan iç kontakları taranır. Birbirine değen tüm noktalar aynı kümede toplanır (Örn: L1 fazından gelen tüm kablolar bir net oluşturur).

2. **Potansiyel Yayma:**
   Besleme kaynaklarından (L1, L2, L3, N, PE) gelen potansiyeller, bulundukları netlere yayılır. L1'e bağlı bir kablonun ucundaki terminal de artık L1 potansiyeline sahip olur.

3. **Güvenlik Kontrolü:**
   Aynı net içinde L1 ve L2 (iki farklı faz) varsa bu **Kısa Devre**'dir. Sistem anında otomatı attırır ve motoru durdurur.

4. **Durum Güncelleme (Sabit Nokta Döngüsü):**
   Kontaktörlerin A1 ve A2 bobin uçlarına bakılır. Eğer A1'de Faz (L1) ve A2'de Nötr (N) varsa, kontaktör **çeker (CoilEnergized = true)**.
   Kontaktör çektiğinde kontakları kapanır, bu da devrenin bağlantılarını değiştirir. Bu yüzden solver, sistem **sabit duruma (hiçbir şey değişmeyene kadar)** ulaşana dek başa döner ve ağları tekrar hesaplar.

5. **Mühürleme (Seal-in) Nasıl Çalışır?**
   En kritik kısım: START butonuna bastın, bobin çekti ve 13-14 (yardımcı kontak) kapandı.
   START'ı bıraktığında solver devreyi *sıfırdan* çözmez. Önceki durumdan (bobin çekili, 13-14 kapalı) başlar.
   Akım artık START butonundan değil, 13-14 kontağı üzerinden geçerek bobine ulaşır. Böylece motor dönmeye devam eder.

---

## ⚡ Elektrik Kavramları

* **Besleme Kaynakları:** Simülatörde 5 sabit besleme kaynağı vardır (L1, L2, L3, N, PE). Bunlar panoya konmaz, direkt terminal noktalarıdır.
* **Fail-Safe Tasarım:** STOP butonu neden **Normalde Kapalı (NC)** kullanılır? Kablo kopsa bile devre açılır ve sistem güvenli bir şekilde durur. `ComponentCatalog`'da NC buton basılmadığı sürece devreyi kapatacak (akım geçirecek) şekilde ayarlandı.
* **Kumanda vs Güç Devresi:** Motor L1/L2/L3 ile 380V üzerinden beslenirken, kontaktör bobinleri L1/N ile 220V üzerinden beslenir (Eğitim seti standardı).

---

Hazır olduğunda Faz 3'e geçebiliriz!
