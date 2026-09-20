# 05 — CSS Sistemi: Renkler, Tema ve Kablo Renkleri

---

## CSS Neden 3 Ayrı Dosya?

```
variables.css   →  Tasarım token'ları (renkler, ölçüler — "ne renk kullanılacak?")
layout.css      →  Yerleşim (hangi bölge nereye? — "nereye konacak?")
components.css  →  Bileşenler (buton nasıl görünür? — "nasıl görünecek?")
```

Bu ayrımın faydası:
- Renk değiştirmek istersen → sadece `variables.css`
- Yerleşimi değiştirmek istersen → sadece `layout.css`
- Birini değiştirince diğeri bozulmuyor

---

## `variables.css` — Design Token Sistemi

### CSS Değişkeni Nasıl Çalışır?

```css
/* Tanımlama (variables.css'de) */
:root {
    --clr-bg: #151820;
}

/* Kullanma (layout.css veya components.css'de) */
body {
    background: var(--clr-bg);  /* #151820 değeri buraya gelir */
}
```

`:root` → Tüm sayfayı kapsayan en üst element.
Burada tanımlanan değişkenler her yerde kullanılabilir.

---

### Renk Paleti — Neden Bu Renkler?

**Tasarım hedefi:** Endüstriyel/atölye hissi. Arayüz nötr, kablolar öne çıksın.

```css
/* Koyu tema (varsayılan) */
--clr-bg:           #151820;   /* Çok koyu lacivert-gri → sayfa zemini */
--clr-panel-body:   #1e2128;   /* Biraz açık → pano metal gövdesi */
--clr-panel-inner:  #242830;   /* Biraz daha açık → pano iç yüzeyi */
--clr-surface:      #2a2f3a;   /* Kart ve panel yüzeyleri */
--clr-surface-hover:#323848;   /* Fare üzerinde yken biraz açılıyor */
--clr-rail:         #2e3240;   /* DIN ray rengi */
```

Renkler sadece `#` değil — mantıklı bir hiyerarşi var:
`bg < panel-body < panel-inner < surface < surface-hover` (giderek açılıyor)

**Durum renkleri:**
```css
--clr-status-ok:    #3ab870;   /* Yeşil → motor çalışıyor, bobin enerjili */
--clr-status-warn:  #e09830;   /* Turuncu → uyarı (toprak eksik...) */
--clr-status-fault: #e03040;   /* Kırmızı → arıza (kısa devre, kaçak) */
--clr-status-info:  #5a6278;   /* Gri → bilgi mesajı */
```

---

### Kablo Renkleri — IEC 60204-1 Standardı

Bu renkler **rastgele seçilmedi**. IEC 60204-1 "Makinalarda Elektrik Donanımı"
standardının Tablo 4'ünden geliyor. Türkiye'de de bu standart uygulanıyor.

```css
--wire-black:       #1a1a1a;  /* Siyah   → AC ve DC güç devresi */
--wire-red:         #cc2222;  /* Kırmızı → AC kumanda devresi */
--wire-dark-blue:   #1a3a8a;  /* Koyu Mavi → DC kumanda devresi */
--wire-light-blue:  #4ab0d4;  /* Açık Mavi → Nötr iletkeni */
--wire-yellow-green:#7cb82f;  /* Sarı-Yeşil → PE (koruma iletkeni) */
--wire-orange:      #e07820;  /* Turuncu → Harici kilitleme devresi */
```

**Önemli:** Sarı-yeşil (YellowGreen) yalnızca PE (toprak/koruma) için kullanılır.
Başka amaçla kullanılırsa simülatör uyarı verecek (Faz 6'da kural kontrolü).

---

### Açık/Koyu Tema — Nasıl Çalışıyor?

```css
/* Varsayılan: koyu tema */
:root {
    --clr-bg: #151820;
    --clr-text: #c8ccd8;
}

/* İşletim sistemi açık tema kullanıyorsa otomatik geçiş */
@media (prefers-color-scheme: light) {
    :root {
        --clr-bg: #dde0e8;     /* Aynı değişken, farklı değer */
        --clr-text: #2a2e3a;
    }
}
```

Hiçbir HTML veya JavaScript değişmiyor.
Sadece değişkenin değeri değişiyor → her şey otomatik uyum sağlıyor.

---

### Animasyon Erişilebilirliği

```css
/* Akan kablo animasyonu tanımı */
@keyframes wire-flow {
    to { stroke-dashoffset: -20; }  /* Kesikli çizgi kayıyor gibi görünür */
}

/* Kullanıcı "hareketi azalt" seçeneği açmışsa animasyon durur */
@media (prefers-reduced-motion: reduce) {
    * {
        animation-duration: 0.01ms !important;
        transition-duration: 0.01ms !important;
    }
}
```

Bu `prefers-reduced-motion` kontrolü epilepsi hassasiyeti olan kullanıcılar için önemli.

---

## `layout.css` — Sayfa Yerleşimi

### Oyun Ekranının Grid Yapısı

```css
.sim-layout {
    display: grid;
    grid-template-rows: var(--toolbar-h) 1fr var(--drawer-h);
    /*                  ↑ 56px sabit    ↑ kalan  ↑ 148px sabit */
    height: 100vh;    /* Tam ekran yüksekliği */
    overflow: hidden; /* Scroll yok */
}
```

Görsel:
```
┌─────────────── 56px (toolbar) ────────────────┐
│                                               │
│                                               │
│           kalan yükseklik (main)              │
│                                               │
│                                               │
├─────────────── 148px (drawer) ────────────────┤
│  Malzeme Çekmecesi                            │
└───────────────────────────────────────────────┘
```

### Bindirme (position: absolute) Tekniği

Plan paneli ve durum konsolu, main alanının üzerine "yüzer":
```css
.sim-plan-panel {
    position: absolute;  /* Normal akışın dışına çık */
    top: 0.75rem;        /* Üstten 12px boşluk */
    right: 0.75rem;      /* Sağdan 12px boşluk */
    width: 340px;
    z-index: 30;         /* SVG'nin üzerinde */
}
```

`z-index` = "katman sırası". Büyük = öne geçer.
Pano SVG'si altta (z-index yok), paneller üstte (z-index: 30), toolbar en üstte (z-index: 50).

---

## `components.css` — Bileşen Stilleri

### Butonlar — Hold Davranışı

START ve STOP butonları basılı tutulurken görsel değişiyor:

```css
.btn--start {
    background: rgba(58, 184, 112, 0.1);  /* Soluk yeşil */
    border-color: var(--clr-status-ok);
    color: var(--clr-status-ok);
}

/* JavaScript .is-held class'ını ekleyince */
.btn--start.is-held {
    background: rgba(58, 184, 112, 0.35);  /* Koyu yeşil — basılı */
}
```

JavaScript (`app.js`):
```javascript
btn.addEventListener('pointerdown', e => {
    btn.classList.add('is-held');   // CSS'i tetikle
});
btn.addEventListener('pointerup', () => {
    btn.classList.remove('is-held'); // CSS'i kaldır
});
```

### Log Satırları — Renk Kodlaması

```css
.log-entry--ok    .log-entry__icon { color: var(--clr-status-ok); }    /* ● yeşil */
.log-entry--warn  .log-entry__icon { color: var(--clr-status-warn); }  /* ▲ turuncu */
.log-entry--fault .log-entry__icon { color: var(--clr-status-fault); } /* ■ kırmızı */
.log-entry--info  .log-entry__icon { color: var(--clr-status-info); }  /* · gri */
```

HTML'de bu şöyle kullanılacak (Faz 2'de JS ile dinamik):
```html
<div class="log-entry log-entry--fault">
    <span class="log-entry__icon">■</span>
    <span class="log-entry__msg">Kısa devre: L1 ile L2 aynı nette.</span>
</div>
```
