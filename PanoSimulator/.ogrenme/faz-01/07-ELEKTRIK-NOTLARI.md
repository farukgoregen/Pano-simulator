# 07 — Faz 1'de Öğrenilen Elektrik Kavramları

Bu dosya, Faz 1'i yazarken kullandığımız elektrik terimlerini açıklıyor.
Kaynak: Ahmet Burak Sanlav — "Elektrik Kumanda Devreleri ve Ekipmanları" (§1-2)

---

## Temel Kavramlar

### Terminal (Bağlantı Noktası)
Her elektrik cihazının "bacakları". Kablo buraya bağlanır.
Terminal numaraları IEC/DIN standardına göre belirlenmiş — Türkiye dahil her yerde aynı.

**Kontaktör terminal numaraları:**
```
  Güç kontakları:  1-2, 3-4, 5-6  (3 faz geçişi)
  Yardımcı NO:    13-14           (mühürleme için kullanılır)
  Bobin:          A1, A2          (A1'e gerilim, A2'ye nötr)
```

**Termik röle terminal numaraları:**
```
  NC kontak:  95-96  (normalde kapalı → termik atınca AÇILIR)
  NO kontak:  97-98  (normalde açık   → termik atınca KAPANIR)
```
95-96 kumanda devresine seri bağlanır → termik atınca kumanda kesilir → kontaktör düşer → motor durur.

---

### Potansiyel (Gerilim Seviyesi)
Bir noktanın elektrik "yüksekliği". Farklı potansiyeller arasında akım akar.

**Trifaze (3 fazlı) sistemde (Kitap §1.1.2):**
```
L1, L2, L3  → Üç faz (R, S, T de deniyor)
N           → Nötr (dönüş hattı)
PE          → Koruma iletkeni (toprak/gövde)

Faz-faz arası:  380V (L1-L2, L2-L3, L1-L3)
Faz-nötr arası: 220V (L1-N, L2-N, L3-N)
```

Simülatörde kumanda devresi genellikle L1-N (220V) arasından beslenir.
Bu yüzden bobin A1'e L1, A2'ye N bağlanır.

---

### Net (Ağ / Düğüm Grubu)
Birbirine bağlı terminallerin oluşturduğu grup.

```
Örnek: L1 busbar → Q1 terminali 1 → Q1 terminali 2 → K1 terminali 1
Bu üç nokta aynı "net"te → hepsi L1 potansiyelinde.
```

Solver'da Union-Find algoritması bu ağları buluyor (Faz 2).

---

### Güç Devresi vs Kumanda Devresi

```
GÜÇ DEVRESİ
├── Şebeke → Ana otomat (Q1) → Kontaktör güç kontakları (1-6) → Motor (U,V,W)
├── Büyük akım (motor çalışma akımı)
├── Kablo rengi: SİYAH (IEC 60204-1)
└── Termik röle bu devrede motor akımını ölçer

KUMANDA DEVRESİ
├── L1 → Sigorta (F1) → STOP (NC) → START (NO) → K1 bobini (A1-A2) → N
├── Küçük akım (bobin çekme akımı, genelde 10-50mA)
├── Kablo rengi: KIRMIZI (IEC 60204-1)
└── Termik 95-96 kontağı bu devreye seri bağlı (arıza koruması)
```

---

### Kontaktör — Nasıl Çalışır?

```
Bobin (A1-A2) enerjilenir
    ↓
Elektromanyetik alan oluşur
    ↓
Metal göbek çekilir (mekanik hareket)
    ↓
Güç kontakları (1-2, 3-4, 5-6) KAPANIR → Motora güç gider
Yardımcı NO kontak (13-14) KAPANIR → Mühürleme sağlanır
    ↓
Bobin enerjisi kesilir
    ↓
Yay, göbeği geri iter
    ↓
Tüm kontaklar ESKİ haline döner
```

(Kitap §2.2: Kontaktör — sayfa 6)

---

### NO / NC Kontak Farkı

**NO (Normalde Açık / Normally Open):**
- Bobin/buton yok iken: açık (akım geçmez)
- Bobin çekince / buton basılınca: kapanır (akım geçer)
- Kullanım: START butonu, kontaktörün güç kontakları, mühürleme kontağı

**NC (Normalde Kapalı / Normally Closed):**
- Bobin/buton yok iken: kapalı (akım geçer)
- Bobin çekince / buton basılınca: açılır (akım kesilir)
- Kullanım: STOP butonu, termik röle 95-96 kontağı, elektriksel kilitleme

---

### Mühürleme (Seal-in) Devresi — Faz 2'nin Kalbi

Bu kavram çok önemli. Prompt'ta da özellikle vurgulandı.

**Problem:** START butonu NO. Bırakılınca açılır → bobin enerjisi kesilir → motor durur.
**Çözüm:** Kontaktörün yardımcı NO kontağını (13-14) START butonu ile paralel bağla.

```
Kumanda devresi:
L1 → F1 → STOP(NC) → [START(NO) || K1_13-14(NO)] → K1_bobin(A1) → N

START basılınca:
└─ K1 bobini çeker → 13-14 kapanır (mühürleme)

START bırakılınca:
└─ 13-14 hâlâ kapalı → bobin enerjili kalmaya devam → motor çalışmaya devam

STOP basılınca:
└─ STOP NC kontak açılır → devre kesilir → bobin düşer → 13-14 açılır → motor durur
```

**Simülatördeki kritik not (solver'ı etkileyecek):**
Solver her tick'te sıfırdan başlatılmamalı — bobin durumu `CoilEnergized` state'te
kalıcı tutulmalı. Aksi halde START bırakıldığında mühürleme çalışmaz.

---

### Termik Röle — Neden 95-96 Kumanda Devresinde?

```
Sorun: Motor aşırı ısınırsa sargılar yanar.
Çözüm: Termik röle, motor akımını ısıyla ölçer.
       Ayar değerini aşınca bimetal eleman bükülerek kontakları değiştirir.

95-96 NC kontak → Kumanda devresinde seri
├─ Normal: kapalı (akım geçer, devre çalışır)
└─ Termik attı: AÇILIR → kumanda devresi kesilir → K1 bobini düşer → motor durur
                                                                       (zarar görmeden)

Termik atınca motor kendisi DURMAZ.
Kontaktör bobinini enerjisiz bırakarak DURDURUR.
Bu yüzden termik GÜVENLIK cihazı, motorun koruyucusu.
```

(Kitap §2.5: Termik Röle — sayfa 11)

---

### Kısa Devre vs Elektrik Kaçağı vs Faz Eksikliği

| Arıza | Ne Olur | Simülatör Ne Yapar |
|-------|---------|-------------------|
| **Kısa devre** | İki farklı faz (veya faz+nötr) doğrudan bağlanır, akım çok büyür | Besleme otomat açar, tüm devre enerjisiz |
| **Elektrik kaçağı** | Faz ile PE (gövde) aynı nette, insana çarpma riski | Uyarı: "L1 koruma iletkenine temas ediyor" |
| **Faz eksikliği** | Motora 1 veya 2 faz gidiyor | "Motor uğulduyor, sargı yanar" uyarısı |

---

### DIN Ray

Panoya monte elemanların üzerine oturduğu metal profil.
Adı DIN 46277 standardından geliyor.
Standart genişliği 35mm. Otomat, kontaktör, termik röle, zaman rölesi hepsi bu raya takılır.

Simülatörde 3 DIN ray var: üst, orta, alt.
Parça bir rayın yakınına bırakılınca otomatik snap (yapışma) yapacak (Faz 4).

---

## Faz 1'de Gördüğümüz IEC Standartları

| Standart | Konu | Projede Nerede? |
|----------|------|-----------------|
| **IEC 60204-1** | Makinalarda elektrik donanımı | Kablo renkleri (`WireColor`), PE kuralı |
| **IEC 60947-4-1** | Kontaktörler ve motor starterlar | Terminal numaraları (A1,A2,1-6,13-14) |
| **IEC 60947-5-1** | Kontrol devreleri — butonlar | NC/NO buton terminalleri |
| **DIN 46277** | DIN ray profili | Pano çiziminde ray ölçüsü |

Bu standartlara referanslar, ilgili C# dosyalarının yorum satırlarında var:
```csharp
// Wire.cs içinde:
/// Kablo renkleri: IEC 60204-1 Tablo 4

// PanelState.cs içinde:
/// Motor PE bağlı değilse uyarı — IEC 60204-1 zorunluluğu
```
