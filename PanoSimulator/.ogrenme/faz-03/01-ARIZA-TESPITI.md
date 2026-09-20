# 01 - Arıza Tespiti ve Güvenlik (Faz 3)

Bu fazda, simülasyon motorumuza (CircuitSolver) **Hata Yakalama (Fault Detection)** zekâsını ekledik. Yeni eklediğimiz `FaultDetector.cs` sınıfı, çözücü her hesaplama yaptığında devrede tehlikeli bir durum olup olmadığını kontrol eder.

---

## 1. Kısa Devre (Short Circuit)

**Ne zaman olur?** 
İki farklı güç kaynağının (örneğin L1 ile L2 fazları veya L1 fazı ile Nötr hattı) birbirine dirençsiz (yük olmadan) değmesi durumudur. 

**Simülatör nasıl algılar?**
Bir bağlantı ağı (net) üzerinde birden fazla güç kaynağı (`Potential.L1`, `L2` vb.) tespit edilirse kısa devre sayılır.

**Sonucu nedir?**
Gerçek dünyada kablolar yanabilir ve yangın çıkabilir. Bu yüzden pano girişindeki Ana Otomat (Q1) anında akımı keser. Simülatörde de `state.MainBreakerTripped = true` olur ve otomat kapalı (açık) duruma geçer.

---

## 2. Toprak Kaçağı (PE Kaçağı - Ground Fault)

**Ne zaman olur?**
Fazlardan birinin doğrudan veya dolaylı olarak makine gövdesine veya PE (Koruma İletkeni / Toprak) hattına değmesi durumudur.

**Simülatör nasıl algılar?**
Bir bağlantı ağı (net) üzerinde hem güç fazı (L1, L2, vs) hem de Toprak (`Potential.PE`) bulunursa toprak kaçağı sayılır.

**Sonucu nedir?**
İnsan sağlığı için ölümcüldür. Gerçek panolarda Kaçak Akım Rölesi (KAR) devreyi keser. Bizim eğitim simülatöründe şimdilik "Kaçak var!" şeklinde sadece durum logu düşeceğiz.

---

## 3. Faz Eksikliği (Phase Loss)

**Ne zaman olur?**
3 fazlı (L1, L2, L3 ile çalışan) bir asenkron motora, kopuk bir kablo veya atan bir sigorta yüzünden sadece 1 veya 2 faz ulaşması durumudur.

**Simülatör nasıl algılar?**
Motorun U, V, W klemenslerindeki potansiyeller incelenir. Eğer bu klemenslere ulaşan *farklı* faz sayısı 3 değilse faz eksikliği vardır. (Örn: L1 köprülenip hem U hem de V klemensine girerse yine 2 faz sayılır).

**Sonucu nedir?**
Motor "uğuldar" ama dönmez. Çektiği aşırı akım sebebiyle sargıları kısa sürede ısınır ve yanar. Pratik uygulamada bunu korumak için Faz Koruma Rölesi veya Termik Röle kullanılır.

---

## 4. Topraklama Kontrolü (Motor Grounding)

IEC 60204-1 standartlarına göre her metal gövdeli cihaz (motor dahil) topraklanmak zorundadır (sarı-yeşil kablo). 
Simülatör, motorun PE klemensinin gerçekten `__supply:PE` hattına bağlı olup olmadığını denetler. Eğer bağlı değilse arıza sayılır ve sistem uyarır.

---

*Testleri nerede?*
Bu durumların tümünü kanıtlamak için `Phase3FaultTests.cs` adında özel bir dosya hazırlayıp 6 farklı senaryo testi yazdık. Tüm testler başarıyla geçiyor.
