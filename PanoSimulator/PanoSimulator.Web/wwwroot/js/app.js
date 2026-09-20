/**
 * Faz 5: Pano Çizimi, Sürükle-Bırak ve Kablolama Motoru
 */
class PanoApp {
    constructor() {
        this.svg = document.getElementById("panel-svg");
        this.partsLayer = document.getElementById("parts-layer");
        this.wiresLayer = document.getElementById("wires-layer");
        this.drawerParts = document.querySelectorAll(".part-card");
        this.catalog = window.COMPONENT_CATALOG || [];
        
        // State
        this.placements = []; 
        this.nextId = 1;
        this.dragState = null;

        // Wiring State
        this.wires = []; // { id, fromPart, fromTerm, toPart, toTerm, color }
        this.nextWireId = 1;
        this.currentWireColor = "black";
        this.wiringStartTerminal = null; // { partId, terminalId, x, y, el }
        this.deleteMode = false;
        this.isPowerOn = false;
        
        // Faz 7: Zaman rölesi ticking interval id
        this.timerIntervalId = null;

        this.rails = [
            { id: 0, y: 100 },
            { id: 1, y: 220 },
            { id: 2, y: 340 }
        ];

        this.gridUnit = 20;
        this.init();
    }

    init() {
        if (!this.svg) return;

        // 1. Çekmece Sürükle-Bırak
        this.drawerParts.forEach(card => {
            card.addEventListener('dragstart', (e) => this.onDrawerDragStart(e, card));
        });
        this.svg.addEventListener('dragover', (e) => e.preventDefault());
        this.svg.addEventListener('drop', (e) => this.onSvgDrop(e));

        // 2. Panodaki parçaları sürüklemek için Pointer olayları
        this.partsLayer.addEventListener('pointerdown', (e) => this.onPartPointerDown(e));
        this.svg.addEventListener('pointermove', (e) => this.onPointerMove(e));
        this.svg.addEventListener('pointerup', (e) => this.onPointerUp(e));
        
        // 3. Renk seçici
        const swatches = document.querySelectorAll('.wire-swatch');
        swatches.forEach(swatch => {
            swatch.addEventListener('click', () => {
                swatches.forEach(s => s.classList.remove('is-selected'));
                swatch.classList.add('is-selected');
                this.currentWireColor = swatch.dataset.color;
            });
        });
        // Siyahı varsayılan seç
        document.querySelector('.wire-swatch[data-color="black"]')?.classList.add('is-selected');

        // 4. Sil Butonu
        const btnDelete = document.getElementById('btn-delete');
        if (btnDelete) {
            btnDelete.addEventListener('click', () => {
                this.deleteMode = !this.deleteMode;
                btnDelete.setAttribute('aria-pressed', this.deleteMode);
                btnDelete.classList.toggle('is-on', this.deleteMode);
                this.svg.style.cursor = this.deleteMode ? 'crosshair' : 'default';
                this.logMessage(`Silme modu ${this.deleteMode ? 'açıldı' : 'kapatıldı'}.`, "info");
                
                this.renderWires();
                this.runSimulation();
                
                // Enerji kesildiğinde timerları sıfırla
                if (!this.isPowerOn) {
                    this.stopTimerTick();
                    this.placements.forEach(p => {
                        if (p.type === 'timer') {
                            p.state.timerElapsedMs = 0;
                            p.state.coilEnergized = false;
                        }
                    });
                    this.renderParts();
                }
                
                // Başlanmış bir kablolama varsa iptal et
                if (this.wiringStartTerminal) {
                    this.wiringStartTerminal.el.classList.remove('is-wiring');
                    this.wiringStartTerminal = null;
                    const preview = document.getElementById('wire-preview');
                    if (preview) preview.remove();
                }
            });
        }

        // Supply (Güç Dağıtım Bloğu) terminallerine de tıklama olayı bağlayacağız (Render'da değil, sabit olduğu için şimdi bağlıyoruz)
        const supplyBlock = document.getElementById('supply-block');
        if (supplyBlock) {
            supplyBlock.querySelectorAll('.sim-terminal').forEach(tGroup => {
                tGroup.addEventListener('pointerdown', (e) => this.onTerminalClick(e, tGroup));
            });
        }
        
        // 5. Kontrol Et Butonu
        const btnCheck = document.getElementById('btn-check');
        if (btnCheck) {
            btnCheck.addEventListener('click', () => this.runSimulation());
        }

        // 6. Plan Büyütme (Modal)
        const btnZoomPlan = document.getElementById('btn-zoom-plan');
        const btnClosePlan = document.getElementById('btn-close-plan');
        const planModal = document.getElementById('plan-modal-backdrop');
        
        if (btnZoomPlan && planModal && btnClosePlan) {
            btnZoomPlan.addEventListener('click', () => {
                planModal.style.display = 'flex';
                planModal.removeAttribute('aria-hidden');
            });
            btnClosePlan.addEventListener('click', () => {
                planModal.style.display = 'none';
                planModal.setAttribute('aria-hidden', 'true');
            });
        }
        
        // ESC ile iptal
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                if (this.wiringStartTerminal) {
                    this.wiringStartTerminal.el.classList.remove('is-wiring');
                    this.wiringStartTerminal = null;
                    const preview = document.getElementById('wire-preview');
                    if (preview) preview.remove();
                    this.logMessage("Kablo çekme iptal edildi.");
                }
                if (this.deleteMode) {
                    document.getElementById('btn-delete').click(); // Moddan çık
                }
            }
        });
    }

    // ─── KABLOLAMA (WIRING) ──────────────────────────────────────────────────

    onTerminalClick(e, tGroup) {
        if (this.deleteMode) return;
        
        e.stopPropagation(); // Part pointerDown'u tetikleme
        
        // Supply'da id "__supply", part'ta en üst g'de data-id var
        const partEl = tGroup.closest('.sim-part');
        if (!partEl) return;
        
        const partId = partEl.dataset.id;
        const terminalId = tGroup.dataset.terminalId;

        // Terminalin ekrandaki SVG koordinatını (merkezini) bul
        const ctm = tGroup.getCTM();
        const svgCtm = this.svg.getCTM();
        const relCtm = svgCtm.inverse().multiply(ctm);
        // tGroup içindeki dairenin merkezini bulalım (tGroup'un 0,0'ı dairenin kendisi varsayılır veya transform)
        // Daire merkezde r=4.
        const pt = this.svg.createSVGPoint();
        pt.x = 0; pt.y = 0;
        const globalPt = pt.matrixTransform(relCtm);

        if (!this.wiringStartTerminal) {
            // İLK TIKLAMA (A Noktası)
            this.wiringStartTerminal = {
                partId: partId,
                terminalId: terminalId,
                x: globalPt.x,
                y: globalPt.y,
                el: tGroup
            };
            tGroup.classList.add('is-wiring');
            this.logMessage(`Kablo başlangıcı: ${partId}:${terminalId}. Bitiş için diğer terminale tıklayın.`);
        } else {
            // İKİNCİ TIKLAMA (B Noktası)
            const start = this.wiringStartTerminal;
            
            // Aynı terminale tıkladıysa iptal et
            if (start.partId === partId && start.terminalId === terminalId) {
                start.el.classList.remove('is-wiring');
                this.wiringStartTerminal = null;
                const preview = document.getElementById('wire-preview');
                if (preview) preview.remove();
                this.logMessage("Kablo çekme iptal edildi.");
                return;
            }

            // Kabloyu ekle
            const wire = {
                id: `w${this.nextWireId++}`,
                fromPart: start.partId,
                fromTerm: start.terminalId,
                toPart: partId,
                toTerm: terminalId,
                color: this.currentWireColor
            };

            // Daha önce bu iki terminal arasında aynı bağlantı varsa engelle
            const exists = this.wires.some(w => 
                (w.fromPart === wire.fromPart && w.fromTerm === wire.fromTerm && w.toPart === wire.toPart && w.toTerm === wire.toTerm) ||
                (w.toPart === wire.fromPart && w.toTerm === wire.fromTerm && w.fromPart === wire.toPart && w.fromTerm === wire.toTerm)
            );

            start.el.classList.remove('is-wiring');
            this.wiringStartTerminal = null;
            const preview = document.getElementById('wire-preview');
            if (preview) preview.remove();

            if (!exists) {
                this.wires.push(wire);
                this.renderWires();
                this.logMessage(`Kablo çekildi: ${wire.fromPart}:${wire.fromTerm} -> ${wire.toPart}:${wire.toTerm} (${this.getWireColorName(wire.color)})`);
            } else {
                this.logMessage("Bu terminaller zaten birbirine bağlı.", "warn");
            }
        }
    }

    onWireClick(e, wireId) {
        if (!this.deleteMode) return;
        this.wires = this.wires.filter(w => w.id !== wireId);
        this.renderWires();
        this.logMessage("Kablo silindi.", "info");
    }

    getWireColorHex(color) {
        const map = {
            'brown': 'var(--wire-brown)',
            'black': 'var(--wire-black)',
            'gray': 'var(--wire-gray)',
            'red': 'var(--wire-red)',
            'blue': 'var(--wire-blue)',
            'white': 'var(--wire-white)',
            'pink': 'var(--wire-pink)',
            'purple': 'var(--wire-purple)',
            'green': 'var(--wire-green)'
        };
        return map[color] || 'var(--wire-black)';
    }

    getWireColorName(color) {
        const map = {
            'brown': 'Kahverengi',
            'black': 'Siyah',
            'gray': 'Gri',
            'red': 'Kırmızı',
            'blue': 'Mavi',
            'white': 'Beyaz',
            'pink': 'Pembe',
            'purple': 'Mor',
            'green': 'Yeşil'
        };
        return map[color] || 'Belirsiz';
    }

    // Terminalin o anki global SVG koordinatını hesaplar
    getTerminalPos(partId, terminalId) {
        const partEl = document.querySelector(`.sim-part[data-id="${partId}"]`);
        if (!partEl) return null;
        const termEl = partEl.querySelector(`.sim-terminal[data-terminal-id="${terminalId}"]`);
        if (!termEl) return null;

        const ctm = termEl.getCTM();
        const svgCtm = this.svg.getCTM();
        const relCtm = svgCtm.inverse().multiply(ctm);
        const pt = this.svg.createSVGPoint();
        pt.x = 0; pt.y = 0;
        return pt.matrixTransform(relCtm);
    }

    renderWires() {
        this.wiresLayer.innerHTML = '';
        
        for (const w of this.wires) {
            const posA = this.getTerminalPos(w.fromPart, w.fromTerm);
            const posB = this.getTerminalPos(w.toPart, w.toTerm);
            
            if (!posA || !posB) continue;

            // Hitbox (geniş, görünmez çizgi) kabloyu silmeyi kolaylaştırmak için
            const hitLine = document.createElementNS("http://www.w3.org/2000/svg", "line");
            hitLine.setAttribute("x1", posA.x);
            hitLine.setAttribute("y1", posA.y);
            hitLine.setAttribute("x2", posB.x);
            hitLine.setAttribute("y2", posB.y);
            hitLine.setAttribute("stroke", "transparent");
            hitLine.setAttribute("stroke-width", "20");
            hitLine.setAttribute("class", "sim-wire-hitbox");
            hitLine.style.cursor = this.deleteMode ? 'pointer' : 'default';

            const line = document.createElementNS("http://www.w3.org/2000/svg", "line");
            line.setAttribute("x1", posA.x);
            line.setAttribute("y1", posA.y);
            line.setAttribute("x2", posB.x);
            line.setAttribute("y2", posB.y);
            line.setAttribute("stroke", this.getWireColorHex(w.color));
            line.setAttribute("stroke-width", w.color === "yellow-green" ? "3" : "2.5");
            line.setAttribute("class", "sim-wire");
            line.style.pointerEvents = "none"; // Hitbox events will handle interaction
            
            hitLine.addEventListener('click', (e) => this.onWireClick(e, w.id));
            hitLine.addEventListener('dblclick', (e) => {
                e.preventDefault();
                // Çift tıklama ile silme moduna girmeden doğrudan sil
                this.wires = this.wires.filter(wire => wire.id !== w.id);
                this.renderWires();
                this.logMessage("Kablo çift tıklama ile silindi.", "info");
            });

            this.wiresLayer.appendChild(hitLine);
            this.wiresLayer.appendChild(line);
        }
    }


    // ─── SÜRÜKLE BIRAK (Çekmece -> Pano) ──────────────────────────────────

    onDrawerDragStart(e, card) {
        e.dataTransfer.setData("text/plain", card.dataset.type);
        e.dataTransfer.effectAllowed = "copy";
    }

    onSvgDrop(e) {
        e.preventDefault();
        const type = e.dataTransfer.getData("text/plain");
        if (!type) return;

        const pt = this.getSvgPoint(e.clientX, e.clientY);
        const snap = this.snapToRail(pt.x, pt.y, type);
        
        if (!snap) {
            this.logMessage("Parçayı DIN rayına veya sahaya bırakmalısın.", "warn");
            return;
        }

        const def = this.catalog.find(c => c.type === type);
        const placement = {
            id: `p${this.nextId++}_${type}`,
            type: type,
            x: snap.x,
            y: snap.y,
            railIndex: snap.railIndex,
            def: def,
            state: {
                isPressed: false,
                coilEnergized: false,
                timerElapsedMs: 0,
                timerSetpointMs: 3000 // Varsayılan 3 saniye
            }
        };

        this.placements.push(placement);
        this.renderParts();
        this.logMessage(`${def.label} panoya yerleştirildi.`);

        const emptyHint = document.getElementById("empty-hint");
        if (emptyHint) emptyHint.style.display = "none";
    }

    // ─── SÜRÜKLE BIRAK (Pano İçi Taşıma) ──────────────────────────────────

    onPartPointerDown(e) {
        if (e.button !== 0 || this.deleteMode || this.wiringStartTerminal) return;

        // Terminale tıklandıysa sürüklemeye başlama, terminal click halletsin
        if (e.target.closest('.sim-terminal')) return;

        const gPart = e.target.closest('.sim-part');
        if (!gPart) return;

        // __supply sürüklenemez
        const partId = gPart.dataset.id;
        if (partId === "__supply") return;

        const placement = this.placements.find(p => p.id === partId);
        if (!placement) return;

        const pt = this.getSvgPoint(e.clientX, e.clientY);
        
        this.dragState = {
            placement: placement,
            offsetX: pt.x - placement.x,
            offsetY: pt.y - placement.y,
            element: gPart
        };

        this.svg.setPointerCapture(e.pointerId);
        this.partsLayer.appendChild(gPart);
    }

    onPointerMove(e) {
        const pt = this.getSvgPoint(e.clientX, e.clientY);

        // Parça Sürükleme Önizlemesi
        if (this.dragState) {
            const newX = pt.x - this.dragState.offsetX;
            const newY = pt.y - this.dragState.offsetY;
            this.dragState.element.setAttribute('transform', `translate(${newX}, ${newY})`);
            
            // Sürüklerken kabloları da güncelle
            // Performans için sadece bu parçaya bağlı kabloları çizsek iyi olur ama şimdilik tümünü çiz
            this.dragState.placement.x = newX;
            this.dragState.placement.y = newY;
            this.renderWires();
        }

        // Kablo Çizimi Önizlemesi
        if (this.wiringStartTerminal) {
            let preview = document.getElementById('wire-preview');
            if (!preview) {
                preview = document.createElementNS("http://www.w3.org/2000/svg", "line");
                preview.id = "wire-preview";
                preview.setAttribute("stroke", this.getWireColorHex(this.currentWireColor));
                preview.setAttribute("stroke-width", "2.5");
                preview.setAttribute("stroke-dasharray", "4");
                preview.setAttribute("pointer-events", "none");
                this.wiresLayer.appendChild(preview);
            }
            preview.setAttribute("x1", this.wiringStartTerminal.x);
            preview.setAttribute("y1", this.wiringStartTerminal.y);
            preview.setAttribute("x2", pt.x);
            preview.setAttribute("y2", pt.y);
        }
    }

    onPointerUp(e) {
        if (!this.dragState) return;

        const pt = this.getSvgPoint(e.clientX, e.clientY);
        const newX = pt.x - this.dragState.offsetX;
        const newY = pt.y - this.dragState.offsetY;

        const snap = this.snapToRail(newX, newY, this.dragState.placement.type);
        if (snap) {
            this.dragState.placement.x = snap.x;
            this.dragState.placement.y = snap.y;
            this.dragState.placement.railIndex = snap.railIndex;
        } else {
            // Boşluğa bırakıldıysa, parçayı sil
            this.placements = this.placements.filter(p => p.id !== this.dragState.placement.id);
            // Bağlı kabloları da sil
            this.wires = this.wires.filter(w => w.fromPart !== this.dragState.placement.id && w.toPart !== this.dragState.placement.id);
            this.logMessage("Parça panodan kaldırıldı.", "info");
        }

        this.dragState = null;
        this.svg.releasePointerCapture(e.pointerId);
        this.renderParts();
        this.renderWires(); // Parça yeri kesinleşti, kabloları düzelt
    }

    // ─── HİZALAMA VE YARDIMCI METOTLAR ────────────────────────────────────

    snapToRail(x, y, type) {
        const def = this.catalog.find(c => c.type === type);
        if (!def) return null;

        if (def.category === "Field") {
            let snappedX = Math.round(x / this.gridUnit) * this.gridUnit;
            snappedX = Math.max(40, Math.min(500, snappedX));
            return { x: snappedX, y: 440, railIndex: -1 };
        }

        let closestRail = null;
        let minDistance = 100;

        for (const rail of this.rails) {
            const dist = Math.abs(y - rail.y); 
            if (dist < minDistance) {
                minDistance = dist;
                closestRail = rail;
            }
        }

        if (closestRail) {
            let snappedX = Math.round(x / this.gridUnit) * this.gridUnit;
            snappedX = Math.max(80, Math.min(600 - (def.widthUnits * this.gridUnit), snappedX));
            return { x: snappedX, y: closestRail.y - 10, railIndex: closestRail.id };
        }
        return null;
    }

    getSvgPoint(clientX, clientY) {
        const pt = this.svg.createSVGPoint();
        pt.x = clientX;
        pt.y = clientY;
        return pt.matrixTransform(this.svg.getScreenCTM().inverse());
    }

    logMessage(msg, type = "info") {
        const logContainer = document.querySelector('.sim-status-console__log');
        if (!logContainer) return;

        const div = document.createElement('div');
        div.className = `log-entry log-entry--${type}`;
        
        let icon = "·";
        if (type === "warn") icon = "▲";
        if (type === "fault") icon = "■";
        if (type === "ok") icon = "●";

        div.innerHTML = `<span class="log-entry__icon">${icon}</span><span class="log-entry__msg">${msg}</span>`;
        logContainer.appendChild(div);
        logContainer.scrollTop = logContainer.scrollHeight;
    }

    // ─── SİMÜLASYON API ÇAĞRISI ───────────────────────────────────────────

    async runSimulation() {
        this.logMessage("Simülasyon çözülüyor...", "info");

        try {
            const req = {
                powerOn: this.isPowerOn,
                parts: this.placements.map(p => ({
                    id: p.id,
                    type: p.type,
                    state: p.state // State bilgisini de gönder (timer, buton basılı vb)
                })),
                wires: this.wires.map(w => ({
                    id: w.id,
                    fromPart: w.fromPart,
                    fromTerminal: w.fromTerm,
                    toPart: w.toPart,
                    toTerminal: w.toTerm,
                    color: w.color
                }))
            };

            const res = await fetch('/Simulator/Solve', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(req)
            });

            if (!res.ok) {
                throw new Error("Sunucu hatası");
            }

            const data = await res.json();
            
            data.faults?.forEach(f => this.logMessage(f, "fault"));
            data.warnings?.forEach(w => this.logMessage(w, "warn"));
            data.info?.forEach(i => this.logMessage(i, "ok"));

            if (data.faults?.length === 0 && data.warnings?.length === 0) {
                if (data.info?.length > 0) {
                    // Motor çalışıyor veya benzeri pozitif bir durum var
                    this.logMessage("Tebrikler! Devre başarıyla çalışıyor.", "ok");
                } else if (this.wires.length > 0) {
                    // Sadece rastgele bağlı ve hata yok
                    this.logMessage("Bağlantılarda kısa devre veya hata yok. (Devre pasif)", "info");
                } else {
                    this.logMessage("Pano boş veya kablo bağlantısı yok.", "warn");
                }
            }
            
            // Tüm kablolara is-energized vermek yanıltıcıydı, kaldırıldı.
            document.querySelectorAll('.sim-wire').forEach(w => w.classList.remove('is-energized'));

        } catch (e) {
            this.logMessage("Bağlantı hatası: " + e.message, "fault");
            document.querySelectorAll('.sim-wire').forEach(w => w.classList.remove('is-energized'));
        }
    }

    // ─── ZAMAN RÖLESİ (Faz 7) ─────────────────────────────────────────────

    startTimerTick() {
        if (this.timerIntervalId) return; // Zaten çalışıyor

        this.logMessage("Zaman rölesi tetiklendi, süre sayılıyor...", "info");
        this.timerIntervalId = setInterval(() => {
            let needsUpdate = false;
            
            this.placements.forEach(p => {
                if (p.type === 'timer' && p.state.coilEnergized) {
                    if (p.state.timerElapsedMs < p.state.timerSetpointMs) {
                        p.state.timerElapsedMs += 200; // 200ms tick
                        needsUpdate = true;
                    }
                }
            });

            if (needsUpdate) {
                this.renderParts();
                this.runSimulation(); // Yeni süreyi backend'e yolla
            } else {
                this.stopTimerTick(); // Sayılacak süre kalmadı
            }
        }, 200);
    }

    stopTimerTick() {
        if (this.timerIntervalId) {
            clearInterval(this.timerIntervalId);
            this.timerIntervalId = null;
        }
    }

    // ──────────────────────────────────────────────────────────────────────

    deleteComponent(partId) {
        // Parçayı sil
        this.placements = this.placements.filter(p => p.id !== partId);
        // Bu parçaya bağlı kabloları da sil
        this.wires = this.wires.filter(w => w.fromPart !== partId && w.toPart !== partId);
        
        this.renderParts();
        this.renderWires();
        
        // Pano boşaldıysa empty-hint göster
        if (this.placements.length === 0) {
            const hint = document.getElementById('empty-hint');
            if (hint) hint.style.display = 'block';
        }
    }

    // ─── SVG ÇİZİM MOTORU (Parçalar) ──────────────────────────────────────

    renderParts() {
        this.partsLayer.innerHTML = ''; 

        for (const p of this.placements) {
            const g = document.createElementNS("http://www.w3.org/2000/svg", "g");
            g.setAttribute("class", "sim-part");
            g.setAttribute("data-id", p.id);
            g.setAttribute("transform", `translate(${p.x}, ${p.y})`);

            const width = p.def.widthUnits * this.gridUnit;
            const height = 80;

            const rect = document.createElementNS("http://www.w3.org/2000/svg", "rect");
            rect.setAttribute("width", width);
            rect.setAttribute("height", height);
            rect.setAttribute("rx", 2);
            rect.setAttribute("class", "sim-part__body");
            rect.style.cursor = this.deleteMode ? 'pointer' : 'grab';
            g.appendChild(rect);

            const text = document.createElementNS("http://www.w3.org/2000/svg", "text");
            text.setAttribute("x", width / 2);
            text.setAttribute("y", height / 2);
            text.setAttribute("class", "sim-part__label");
            text.setAttribute("text-anchor", "middle");
            text.setAttribute("dominant-baseline", "middle");
            text.textContent = p.def.type.toUpperCase().substring(0, 3) + " " + p.id.split('_')[0].toUpperCase();
            g.appendChild(text);

            if (p.type === 'timer') {
                const timerText = document.createElementNS("http://www.w3.org/2000/svg", "text");
                timerText.setAttribute("x", width / 2);
                timerText.setAttribute("y", height / 2 + 15);
                timerText.setAttribute("class", "sim-part__label");
                timerText.setAttribute("text-anchor", "middle");
                timerText.setAttribute("fill", "var(--clr-status-ok)");
                timerText.setAttribute("font-size", "10");
                timerText.setAttribute("font-weight", "bold");
                
                const remaining = Math.max(0, (p.state.timerSetpointMs - p.state.timerElapsedMs) / 1000);
                timerText.textContent = remaining.toFixed(1) + "s";
                g.appendChild(timerText);
            }

            // Terminale tıklamayı bağla
            for (const t of p.def.terminals) {
                const tg = document.createElementNS("http://www.w3.org/2000/svg", "g");
                tg.setAttribute("class", "sim-terminal");
                const tx = 10 + t.x; 
                const ty = t.y === 0 ? 8 : height - 8; 

                tg.setAttribute("transform", `translate(${tx}, ${ty})`);
                tg.setAttribute("data-terminal-id", t.id);

                const circle = document.createElementNS("http://www.w3.org/2000/svg", "circle");
                circle.setAttribute("r", 4);
                circle.setAttribute("class", "sim-terminal__screw");
                tg.appendChild(circle);

                const ttext = document.createElementNS("http://www.w3.org/2000/svg", "text");
                ttext.setAttribute("y", t.y === 0 ? 12 : -6); 
                ttext.setAttribute("class", "sim-terminal__label");
                ttext.setAttribute("text-anchor", "middle");
                ttext.textContent = t.id;
                tg.appendChild(ttext);
                
                tg.addEventListener('pointerdown', (e) => this.onTerminalClick(e, tg));
                g.appendChild(tg);
            }

            // Etkileşim: Butonlara ve Switch'lere basma/bırakma
            if (!this.deleteMode && (p.type === 'button-no' || p.type === 'button-nc' || p.type === 'limitSwitch')) {
                g.addEventListener('pointerdown', (e) => {
                    if (e.target.closest('.sim-terminal')) return;
                    p.state.isPressed = true;
                    if (p.type === 'button-no' || p.type === 'button-nc') {
                        rect.setAttribute("fill", "#e2e8f0"); // Basılı görsel efekt
                    } else if (p.type === 'limitSwitch') {
                        rect.setAttribute("fill", "#d6bcfa");
                    }
                    this.runSimulation();
                    
                    // Limit Switch toggle gibi çalışsın istiyorsak, pointerup'ta eski haline döner.
                    // Veya buton gibi çalışsın.
                });

                g.addEventListener('pointerup', (e) => {
                    p.state.isPressed = false;
                    rect.setAttribute("fill", ""); 
                    this.runSimulation();
                });
                
                g.addEventListener('pointerleave', (e) => {
                    if (p.state.isPressed) {
                        p.state.isPressed = false;
                        rect.setAttribute("fill", "");
                        this.runSimulation();
                    }
                });
            }

            // Silme modu (Faz 6.5)
            g.addEventListener('click', (e) => {
                if (this.deleteMode) {
                    e.stopPropagation();
                    this.deleteComponent(p.id);
                    this.logMessage("Parça silindi.", "info");
                }
            });

            this.partsLayer.appendChild(g);
        }
    }
}

document.addEventListener("DOMContentLoaded", () => {
    window.App = new PanoApp();
});
