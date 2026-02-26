
    const reservations = JSON.parse(document.getElementById('reservationData').textContent);
    const allTables    = JSON.parse(document.getElementById('allTablesData').textContent);

    // ── Modal ──────────────────────────────────────────────────────
    function openModal(id)  { document.getElementById(id).classList.add('open'); }
    function closeModal(id) { document.getElementById(id).classList.remove('open'); }
    document.querySelectorAll('.modal-overlay').forEach(o =>
        o.addEventListener('click', e => { if (e.target === o) o.classList.remove('open'); })
    );

    // ── Rezerve modal ──────────────────────────────────────────────
    function openReserveModal(tableId, tableName, maxCap) {
        document.getElementById('res-tableId').value           = tableId;
        document.getElementById('res-maxCap').value            = maxCap;
        document.getElementById('res-modal-title').textContent = tableName + ' — Rezervasyon';
        document.getElementById('res-guests').max = maxCap;
        const d = new Date(); d.setHours(d.getHours() + 1, 0, 0);
        document.getElementById('res-time').value =
            String(d.getHours()).padStart(2,'0') + ':' + String(d.getMinutes()).padStart(2,'0');
        ['res-name','res-phone','res-guests','res-time'].forEach(id => {
            document.getElementById('err-' + id).style.display = 'none';
            document.getElementById(id).style.borderColor = '';
        });
        openModal('reserveModal');
    }

    // ── ✅ #4: Birleştirme modal ───────────────────────────────────
    let mergeSourceId = null;
    let mergeTargetId = null;

    function openMergeModal(sourceTableId, sourceTableName) {
        mergeSourceId = sourceTableId;
        mergeTargetId = null;
        document.getElementById('merge-sourceTableId').value = sourceTableId;
        document.getElementById('merge-targetTableId').value = '';
        document.getElementById('merge-source-name').textContent = sourceTableName;
        document.getElementById('merge-sub').textContent =
            `${sourceTableName} adisyonunu hangi masayla birleştirmek istersiniz?`;
        document.getElementById('merge-submit-btn').disabled = true;

        // Hedef listesini doldur — kaynak hariç tüm masalar
        const list = document.getElementById('mergeTargetList');
        list.innerHTML = '';

        const targets = allTables.filter(t => t.id !== sourceTableId);
        if (targets.length === 0) {
            list.innerHTML = '<div style="text-align:center;color:var(--text-muted);padding:20px">Başka masa bulunamadı.</div>';
        } else {
            targets.forEach(t => {
                const div = document.createElement('div');
                div.className = 'merge-option';
                div.dataset.targetId = t.id;
                const statusLabel = t.status === 1 ? '🔴 Dolu' : t.status === 2 ? '🔵 Rezerve' : '🟢 Boş';
                const infoText = t.status === 1
                    ? `${t.itemCount} kalem · ₺${t.total.toFixed(2).replace('.',',')} — adisyonlar birleşecek`
                    : 'Adisyon bu masaya taşınacak';
                div.innerHTML = `
                    <div style="font-size:20px">🪑</div>
                    <div style="flex:1">
                        <div class="merge-option-name">${t.name}</div>
                        <div class="merge-option-info">${statusLabel} · ${infoText}</div>
                    </div>
                    ${t.status === 1 ? `<div class="merge-option-total">₺${t.total.toFixed(2).replace('.',',')}</div>` : ''}
                `;
                div.addEventListener('click', () => selectMergeTarget(t.id, div));
                list.appendChild(div);
            });
        }

        openModal('mergeModal');
    }

    function selectMergeTarget(targetId, el) {
        document.querySelectorAll('.merge-option').forEach(o => o.classList.remove('selected'));
        el.classList.add('selected');
        mergeTargetId = targetId;
        document.getElementById('merge-targetTableId').value = targetId;
        document.getElementById('merge-submit-btn').disabled = false;
    }

    function confirmMerge() {
        if (!mergeTargetId) return false;
        const src = allTables.find(t => t.id === mergeSourceId);
        const tgt = allTables.find(t => t.id === mergeTargetId);
        const msg = tgt.status === 1
            ? `${src.name} ve ${tgt.name} adisyonları birleştirilecek. Onaylıyor musunuz?`
            : `${src.name} adisyonu ${tgt.name} masasına taşınacak. Onaylıyor musunuz?`;
        return confirm(msg);
    }

    // ── ✅ #5: Kalemleri genişlet / daralt ────────────────────────
    function toggleItems(tableId, total, limit) {
        const container = document.getElementById('items-' + tableId);
        const btn       = document.getElementById('more-' + tableId);
        const hidden    = container.querySelectorAll('.order-items-hidden');
        const expanded  = btn.dataset.expanded === 'true';

        if (expanded) {
            // Daralt
            hidden.forEach(el => el.style.display = 'none');
            btn.textContent    = `+${total - limit} kalem daha...`;
            btn.dataset.expanded = 'false';
        } else {
            // Genişlet
            container.querySelectorAll('[data-item-index]').forEach(el => {
                el.style.display = 'flex';
            });
            btn.textContent    = '▲ Daralt';
            btn.dataset.expanded = 'true';
        }
    }

    // Sayfa yüklendiğinde hidden'ları gizle
    document.querySelectorAll('.order-items-hidden').forEach(el => {
        el.style.display = 'none';
    });

    // ── Rezervasyon detay ──────────────────────────────────────────
    function showResDetail(tableId) {
        const r = reservations.find(x => x.id == tableId);
        if (!r) return;
        document.getElementById('resDetailContent').innerHTML = `
            <div class="res-detail-row"><div class="res-detail-icon">👤</div><div><div class="res-detail-label">İsim Soyisim</div><div class="res-detail-value">${r.resName}</div></div></div>
            <div class="res-detail-row"><div class="res-detail-icon">📞</div><div><div class="res-detail-label">Telefon</div><div class="res-detail-value">${r.resPhone}</div></div></div>
            <div class="res-detail-row"><div class="res-detail-icon">👥</div><div><div class="res-detail-label">Kişi Sayısı</div><div class="res-detail-value">${r.resGuests} kişi</div></div></div>
            <div class="res-detail-row"><div class="res-detail-icon">🕐</div><div><div class="res-detail-label">Rezervasyon Saati</div><div class="res-detail-value">${r.resTime}</div></div></div>
        `;
        openModal('resDetailModal');
    }

    // ── Validasyon ─────────────────────────────────────────────────
    function validateForm() {
        let ok = true;
        const name = document.getElementById('add-name');
        const cap  = document.getElementById('add-cap');
        const errN = document.getElementById('err-add-name');
        const errC = document.getElementById('err-add-cap');
        if (!name.value.trim()) { errN.style.display='block'; name.style.borderColor='#ef4444'; ok=false; }
        else { errN.style.display='none'; name.style.borderColor=''; }
        const v = parseInt(cap.value);
        if (isNaN(v)||v<1||v>20) { errC.style.display='block'; cap.style.borderColor='#ef4444'; ok=false; }
        else { errC.style.display='none'; cap.style.borderColor=''; }
        return ok;
    }

    function validateReserveForm() {
        let ok = true;
        const maxCap = parseInt(document.getElementById('res-maxCap').value);
        const fields = [
            { id:'res-name',   err:'err-res-name',   check: v => v.trim().length > 0,  msg:'Ad soyad boş olamaz.' },
            { id:'res-phone',  err:'err-res-phone',  check: v => v.trim().length >= 10, msg:'Geçerli telefon giriniz.' },
            { id:'res-guests', err:'err-res-guests', check: v => v >= 1 && v <= maxCap, msg:`1 ile ${maxCap} arasında olmalı.` },
            { id:'res-time',   err:'err-res-time',   check: v => v.length > 0,          msg:'Saat seçiniz.' },
        ];
        fields.forEach(f => {
            const el  = document.getElementById(f.id);
            const err = document.getElementById(f.err);
            const val = f.id === 'res-guests' ? parseInt(el.value) : el.value;
            if (!f.check(val)) { err.textContent=f.msg; err.style.display='block'; el.style.borderColor='#ef4444'; ok=false; }
            else { err.style.display='none'; el.style.borderColor=''; }
        });
        return ok;
    }

    // ── Filtre ─────────────────────────────────────────────────────
    document.querySelectorAll('.filter-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('.filter-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            const f = btn.dataset.filter;
            document.querySelectorAll('.table-card').forEach(c => {
                c.style.display = f === 'all' || c.dataset.status === f ? '' : 'none';
            });
        });
    });

    // ── Alert otomatik kaybol ──────────────────────────────────────
    setTimeout(() => {
        document.querySelectorAll('.alert').forEach(a => {
            a.style.transition = 'opacity .5s'; a.style.opacity = '0';
            setTimeout(() => a.remove(), 500);
        });
    }, 3000);

    // ── Toast ──────────────────────────────────────────────────────
    function showToast(id, type, icon, title, msg, autocloseMs) {
        if (document.getElementById(id)) return;
        const toast = document.createElement('div');
        toast.id = id; toast.className = `toast ${type}`;
        toast.innerHTML = `
            <div class="toast-icon">${icon}</div>
            <div class="toast-body">
                <div class="toast-title ${type}">${title}</div>
                <div class="toast-msg">${msg}</div>
            </div>
            <button class="toast-close" onclick="this.parentElement.remove()">×</button>`;
        document.getElementById('toastContainer').appendChild(toast);
        if (autocloseMs) setTimeout(() => toast.remove(), autocloseMs);
    }

    // ── Rezervasyon uyarı sistemi ──────────────────────────────────
    function checkReservationWarnings() {
        const now = new Date();
        reservations.forEach(r => {
            const resTime = new Date(r.resTimeIso);
            const diffMin = Math.floor((resTime - now) / 60000);

            if (diffMin >= 0 && diffMin <= 30) {
                document.getElementById('toast-late-' + r.id)?.remove();
                showToast('toast-' + r.id, 'warning', '⚠️',
                    `${r.name} — Rezervasyon Yaklaşıyor`,
                    `<strong>${r.resName}</strong> (${r.resGuests} kişi) saat <strong>${r.resTime}</strong>'de bekleniyor. Kalan: ~${diffMin} dakika`,
                    null);
            }
            if (diffMin < 0 && diffMin >= -30) {
                document.getElementById('toast-' + r.id)?.remove();
                showToast('toast-late-' + r.id, 'danger', '🚨',
                    `${r.name} — Misafir Gelmedi Mi?`,
                    `<strong>${r.resName}</strong>, saatinden <strong>${Math.abs(diffMin)} dk</strong> geçti. ${30 + diffMin} dk sonra oto-temizlenecek.`,
                    null);
            }
            if (diffMin < -30) {
                document.getElementById('toast-' + r.id)?.remove();
                document.getElementById('toast-late-' + r.id)?.remove();
                if (!window._reloadScheduled) {
                    window._reloadScheduled = true;
                    showToast('toast-reload', 'info', 'ℹ️', 'Masa Durumu Güncellendi',
                        'Süresi dolan rezervasyon temizlendi. Sayfa yenileniyor...', 3500);
                    setTimeout(() => location.reload(), 4000);
                }
            }
        });
    }

    checkReservationWarnings();
    setInterval(checkReservationWarnings, 60000);

