# Faz 9: Yıldız-Üçgen Yol Verme (Star-Delta Starting)

Elektrik panosu ve otomasyon eğitiminde bir efsane olan Yıldız-Üçgen yol verme devresi, büyük güçlü asenkron motorların şebekeye ilk kalkış anında verdiği zararı önlemek için kullanılan en yaygın metottur.

## 1. Neden Yıldız-Üçgen'e İhtiyaç Var? (Kitap §3.17)

Büyük güçlü (genellikle 4 kW veya 5.5 kW üzeri) üç fazlı asenkron motorlar, ilk kalkış (demeraj) anında normal çalışma akımlarının **6 ile 10 katı** arasında yüksek bir akım çekerler. 
Eğer bu motorları şebekeye doğrudan bağlarsak (Doğrudan Yol Verme):
*   Şebekede ani gerilim düşümleri yaşanır (Işıkların kırpışması).
*   Koruma sigortaları (Otomatlar) gereksiz yere atabilir.
*   Mekanik sisteme (kayış-kasnak, redüktör vb.) ani ve sert bir darbe biner, mekanik ömür kısalır.

Yıldız-Üçgen yöntemi, motor sargılarına kalkış anında uygulanan gerilimi $\sqrt{3}$ oranında düşürerek, çekilen kalkış akımını **1/3 oranına** indiren pratik ve ekonomik bir "düşük gerilimle yol verme" yöntemidir.

## 2. Motor Klemens Kutusu ve 6 Uç

Yıldız-üçgen yapabilmek için motor klemens kutusundan sargıların **her iki ucunun da** dışarı çıkartılmış olması gerekir. (Toplam 3 sargı x 2 uç = 6 uç).
IEC standardına göre bu uçlar şu şekilde isimlendirilir:
*   **Giriş Uçları:** U1, V1, W1
*   **Çıkış Uçları:** U2, V2, W2 (Eski standartta X, Y, Z)

Klemens kutusunda üst sırada U1-V1-W1, alt sırada W2-U2-V2 dizilir. Bunun sebebi, üçgen bağlantı yaparken araya atılacak düz köprülerin (U1-W2, V1-U2, W1-V2) klemenslerin fiziksel konumuna tam uymasıdır.

## 3. Bağlantı Çeşitleri

### A. Yıldız Bağlantı (Star / Y)
*   **Bağlantı Şekli:** Motorun U2, V2, W2 çıkış uçları birbirine kısa devre edilir (Yıldız noktası oluşturulur). L1, L2, L3 fazları U1, V1, W1'e verilir.
*   **Gerilim Durumu:** Fazlar arası gerilim (380V), sargılara $\sqrt{3}$'e bölünerek uygulanır (220V). 
*   **Sonuç:** Motor düşük torkla ve düşük akımla kalkış yapar.

### B. Üçgen Bağlantı (Delta / Δ)
*   **Bağlantı Şekli:** Bir sargının çıkışı, diğer sargının girişiyle birleştirilir. U1 ile W2, V1 ile U2, W1 ile V2 birleştirilerek şebeke fazları (L1, L2, L3) bu birleşim noktalarına uygulanır.
*   **Gerilim Durumu:** Her sargı tam şebeke gerilimini (380V) görür.
*   **Sonuç:** Motor tam akım çeker ve tam devrinde/torkunda (Nominal güç) çalışır.

## 4. Kumanda Senaryosu

Yıldız-Üçgen geçişini otomatik yapmak için **3 adet kontaktör** (Ana, Yıldız, Üçgen) ve **1 adet Zaman Rölesi** kullanılır.
1.  **Start:** Start butonuna basıldığında **Ana Kontaktör (K1)** ve **Yıldız Kontaktörü (K2)** çeker. Motor "Yıldız" kalkış yapar. Aynı anda Zaman Rölesi (KT) saymaya başlar.
2.  **Geçiş:** Zaman rölesi süresi dolunca (motor devrini alınca, örn: 5 saniye), KT kontağı yer değiştirir. **Yıldız Kontaktörü (K2) devreden çıkar, Üçgen Kontaktörü (K3) devreye girer.** Ana kontaktör (K1) zaten çekilidir.
3.  **Çalışma:** Motor artık Üçgen bağlantıda tam gücüyle çalışmaya devam eder.
4.  **Elektriksel Kilitleme (ÇOK ÖNEMLİ!):** Yıldız (K2) ve Üçgen (K3) kontaktörleri KESİNLİKLE aynı anda çekmemelidir. Eğer çekerlerse, 3 faz birbiriyle anında kısa devre olur ve sistem patlar. Bunu önlemek için K2'nin bobin devresi K3'ün Normalde Kapalı (NC) kontağından, K3'ün bobin devresi de K2'nin NC kontağından geçirilir.

## 5. Yazılım Mimarisine Etkisi

Simülatörümüzde 6 uçlu motoru (M1) tasarlarken:
*   **Yıldız Algılama:** `FaultDetector.cs`'de W2, U2 ve V2 pinlerinin aynı "Net" (düğüm) üzerinde olup olmadığını taradık.
*   **Üçgen Algılama:** U1 ve W2'nin, V1 ve U2'nin, W1 ve V2'nin birbirleriyle kendi aralarında eşleşip eşleşmediğini (üç farklı düğüm oluşturduğunu) kontrol eden algoritmalar yazdık.
*   Bu sistem, kullanıcının gerçek hayatta olduğu gibi yaptığı hataları (faz eksikliği, kısa devre) matematiksel bir kesinlikle algılamamızı sağladı.
