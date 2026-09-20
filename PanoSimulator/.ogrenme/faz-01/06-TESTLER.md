# 06 — Testler: xUnit ile Otomatik Doğrulama

---

## Neden Test Yazıyoruz?

Simülasyon motoru yanlış çalışırsa → kullanıcı yanlış elektrik öğreniyor.
Bu çok kritik. "Gözle kontrol ederim" yetmez çünkü:

1. Her değişiklikten sonra elle kontrol etmek zaman alır
2. İnsan gözden kaçırır (özellikle edge case'lerde)
3. Faz 2'de "start-stop mühürleme çalışıyor mu?" → 50 satır kodu elle test edemezsin

Test yazınca → `dotnet test` çalıştır, 2 saniyede hepsini kontrol et.

---

## xUnit Sözdizimi (Syntax)

```csharp
[Fact(DisplayName = "Açıklayıcı test adı")]
public void TestMetodAdi()
{
    // 3 bölüm: Arrange → Act → Assert

    // 1. ARRANGE — Test için gerekli nesneleri hazırla
    var state = new PartState();

    // 2. ACT — Test edilecek işlemi yap (bu fazda çok basit)
    state.CoilEnergized = true;

    // 3. ASSERT — Beklentini kontrol et
    Assert.True(state.CoilEnergized);    // true olmalı → geçer
    Assert.False(state.ThermalTripped);  // false olmalı → geçer
}
```

`Assert` metodları:
- `Assert.True(x)` → x true olmalı
- `Assert.False(x)` → x false olmalı
- `Assert.Equal(beklenen, gercek)` → iki değer eşit olmalı
- `Assert.Null(x)` → x null olmalı
- `Assert.NotNull(x)` → x null olmamalı
- `Assert.Contains(item, collection)` → koleksiyon item'ı içermeli

---

## Her Testin Açıklaması

### Test 1: `Terminal kaydı doğru çalışıyor`

```csharp
[Fact(DisplayName = "Terminal kaydı doğru çalışıyor")]
public void Terminal_RecordEquality_Works()
{
    var t1 = new Terminal("A1", 0, 0);
    var t2 = new Terminal("A1", 0, 0);
    Assert.Equal(t1, t2);
}
```

**Ne test ediyor?** `record` tipinin değer eşitliğini.
Normal `class`'ta `t1 == t2` → referans karşılaştırır (farklı nesneler = farklı).
`record`'da `t1 == t2` → değer karşılaştırır (aynı değerler = eşit).

**Neden önemli?** Solver'da "bu terminal zaten işlendi mi?" kontrolü için
eşitlik karşılaştırması kullanacağız. Yanlış çalışsaydı terminaller çift işlenirdi.

---

### Test 2: `Wire rengi IEC enum değerlerine sahip`

```csharp
[Fact(DisplayName = "Wire rengi IEC enum değerlerine sahip")]
public void WireColor_HasAllIecColors()
{
    var colors = Enum.GetValues<WireColor>();
    Assert.Equal(6, colors.Length);         // Tam 6 renk olmalı
    Assert.Contains(WireColor.Black, colors);
    Assert.Contains(WireColor.YellowGreen, colors);  // PE rengi eksiksiz mi?
}
```

**Ne test ediyor?** IEC 60204-1'deki 6 kablo renginin tamamının eklendiğini.
Birini unutsaydık veya yanlış isimlendirseydik test kırılırdı.

---

### Test 3: `PanelState başlangıçta güvenli durumda`

```csharp
[Fact(DisplayName = "PanelState başlangıçta güvenli durumda")]
public void PanelState_InitialState_IsSafe()
{
    var state = new PanelState();
    Assert.False(state.PowerOn);           // Güç kapalı
    Assert.False(state.StartPressed);      // Buton basılmamış
    Assert.False(state.MainBreakerTripped);// Otomat açmamış
    Assert.Equal(0, state.MotorPhaseCount);// Motor durmuş
}
```

**Ne test ediyor?** Uygulama açıldığında pano güvenli başlangıç durumunda mı?
Gerçek panoda da ilk açılışta her şey kapalı olmalı — bu elektriksel güvenlik kuralı.

---

### Test 4: `PartState kontaktör başlangıçta düşük`

```csharp
public void PartState_Contactor_StartsDeenergized()
{
    var state = new PartState();
    Assert.False(state.CoilEnergized);    // Bobin çekmemiş
    Assert.False(state.ThermalTripped);   // Termik atmamış
    Assert.True(state.BreakerClosed);     // Otomat kapalı (geçiriyor)
}
```

**Dikkat:** `BreakerClosed` başlangıçta `true`! Yani otomat geçiriyor.
Kısa devre olunca solver bunu `false` yapar. Başlangıçta `false` olsaydı
enerji bile verilmeden devre kesilmiş olurdu — yanlış davranış.

---

### Test 5: `NetResult kısa devre tespiti — iki faz aynı nette`

```csharp
public void NetResult_TwoPhasesInSameNet_IsShortCircuit()
{
    var net = new NetResult
    {
        Potentials = [Potential.L1, Potential.L2]  // İki faz aynı ağda!
    };
    Assert.True(net.HasShortCircuit);
}
```

**Elektrik ne diyor?** L1 ve L2 arasında doğrudan bağlantı = kısa devre.
Akım çok büyür, sigorta/otomat açar.

**`HasShortCircuit` nasıl hesaplanıyor?**
```csharp
// PanelState.cs'deki NetResult sınıfı içinde:
public bool HasShortCircuit =>
    Potentials.Count(p => p is Potential.L1 or Potential.L2 or Potential.L3) > 1
    || (faz + nötr arada yük yok);
```
`Count(p => p is L1 or L2 or L3) > 1` → birden fazla faz var mı?
Bu "computed property" — her çağrıda hesaplanıyor, saklanmıyor.

---

### Test 6: `NetResult PE kaçağı tespiti`

```csharp
public void NetResult_PhaseAndPeInSameNet_IsGroundFault()
{
    var net = new NetResult { Potentials = [Potential.L1, Potential.PE] };
    Assert.True(net.HasGroundFault);
}
```

**Elektrik ne diyor?** Faz hattı (L1) ile PE (toprak/koruma iletkeni) aynı nette
= elektrik kaçağı / gövde teması. İnsana çarpma riski!

---

### Test 7: `NetResult normal net — kısa devre yok`

```csharp
public void NetResult_SinglePhase_NoFault()
{
    var net = new NetResult { Potentials = [Potential.L1] };
    Assert.False(net.HasShortCircuit);
    Assert.False(net.HasGroundFault);
}
```

Sadece L1 var → normal durum, arıza yok.
Bir önceki testlerin tam tersini test ediyor (negatif test).

---

### Test 8: `ComponentCatalog Faz 1'de boş ama erişilebilir`

```csharp
public void ComponentCatalog_ReturnsEmpty_InPhase1()
{
    var all = ComponentCatalog.GetAll();
    Assert.NotNull(all);  // null değil — listeye erişebiliyoruz
    // Faz 2'de: Assert.NotEmpty(all);  ← malzemeler eklenince bu aktif olacak
}
```

Şimdilik sadece "crash etmiyor mu?" diye bakıyor.
Faz 2'de `Assert.NotEmpty(all)` satırını aktif edeceğiz.

---

### Test 9: `CircuitSolver.Solve no-op çalışıyor`

```csharp
public void CircuitSolver_Solve_DoesNotThrow()
{
    var solver = new CircuitSolver();
    var state = new PanelState { PowerOn = true };

    var ex = Record.Exception(() => solver.Solve(state));  // Exception yakala
    Assert.Null(ex);   // Exception çıkmamış olmalı
}
```

Solver şimdilik no-op (hiçbir şey yapmıyor).
Bu test "en azından crash etmiyor" güvencesi veriyor.
Faz 2'de bu test çok daha anlamlı olacak.

---

### Test 10: `EndPoint ve Wire kaydı oluşturuluyor`

```csharp
public void Wire_CanBeCreated()
{
    var wire = new Wire(
        A: new EndPoint("k1", "13"),      // K1 kontaktörü, 13. terminal
        B: new EndPoint("bt_start", "3"), // START butonu, 3. terminal
        Color: WireColor.Red              // AC kumanda = kırmızı
    );
    Assert.Equal("k1", wire.A.PlacementId);
    Assert.Equal("13", wire.A.TerminalId);
    Assert.Equal(WireColor.Red, wire.Color);
}
```

**Elektrik notu:** Bu kablo K1 mühürleme kontağından START butonuna gidiyor.
Kırmızı renk doğru — kumanda devresi (IEC 60204-1).

---

## `dotnet test` Çıktısını Okuma

```
Başarılı Terminal kaydı doğru çalışıyor [4 ms]       ← [4ms] = ne kadar sürdü
Başarılı PanelState başlangıçta güvenli durumda [1 ms]
...

Test Çalıştırması Başarılı.
Toplam test sayısı: 10
     Geçti: 10
 Toplam süre: 0,9566 Saniye
```

Bir test kırılınca:
```
BAŞARISIZ NetResult kısa devre tespiti [5 ms]
  Hata mesajı: Assert.True() Failure
  Beklenen: True
  Gerçek:   False
  Konum: Phase1SmokeTests.cs satır 67
```

---

## Faz 2'de Testler Ne Olacak?

```csharp
// Faz 2'nin asıl testi — start-stop mühürleme:
[Fact(DisplayName = "START basılınca K1 çekiyor")]
public void StartButton_Pressed_ContactorEnergizes() { ... }

[Fact(DisplayName = "START bırakılınca mühürleme çalışıyor")]
public void StartReleased_SealingHolds_ContactorStaysOn() { ... }

[Fact(DisplayName = "STOP basılınca K1 düşüyor")]
public void StopButton_Pressed_ContactorDeenergizes() { ... }

[Fact(DisplayName = "Termik atınca motor duruyor")]
public void ThermalTrip_ContactorDrops() { ... }
```

Bu testler tamamen çalışınca "simülasyon motorunu doğruladık" diyebileceğiz.
