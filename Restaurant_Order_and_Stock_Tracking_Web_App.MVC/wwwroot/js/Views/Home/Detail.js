
const orderTotal = @Model.OrderTotalAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
const alreadyPaid = @totalPaid.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
let currentMethod = 'cash';
let addItemUnitPrice = 0;
let addItemQty = 1;

function parseLD(str) {
    if (!str) return 0;
    const v = parseFloat(str.trim().replace(/\./g, '').replace(',', '.'));
    return isNaN(v) ? 0 : v;
}
function fmt(n) { return '₺' + n.toFixed(2).replace('.', ','); }

function openModal(id) { document.getElementById(id).classList.add('open'); }
function closeModal(id) { document.getElementById(id).classList.remove('open'); }
document.querySelectorAll('.modal-overlay').forEach(o => {
    o.addEventListener('click', e => { if (e.target === o) o.classList.remove('open'); });
});

// ── Ürün ekle modal ─────────────────────────────────────────
function openAddItemModal(id, name, price) {
    addItemUnitPrice = price;
    addItemQty = 1;
    document.getElementById('addItemId').value = id;
    document.getElementById('addItemTitle').textContent = name + ' Ekle';
    document.getElementById('addItemSub').textContent = fmt(price) + ' / adet';
    document.getElementById('addItemNote').value = '';
    refreshAddQty();
    openModal('addItemModal');
}
function changeAddQty(delta) {
    addItemQty = Math.max(1, Math.min(99, addItemQty + delta));
    refreshAddQty();
}
function refreshAddQty() {
    document.getElementById('addItemQtyDisplay').textContent = addItemQty;
    document.getElementById('addItemQtyHidden').value = addItemQty;
    document.getElementById('addItemLineTotal').textContent = fmt(addItemQty * addItemUnitPrice);
    document.getElementById('addItemSubmitBtn').textContent = `Ekle (${addItemQty} adet)`;
}

// ── Ödeme modal: kalem seçimi ────────────────────────────────
const piselState = {};
document.querySelectorAll('.pisel-row').forEach(row => {
    const id = parseInt(row.dataset.itemId);
    const max = parseInt(row.dataset.maxQty);
    const up = parseFloat(row.dataset.unitPrice);
    piselState[id] = { selected: 0, max, up };
});

function piselChange(id, delta) {
    const s = piselState[id];
    if (!s || s.max === 0) return;
    s.selected = Math.max(0, Math.min(s.max, s.selected + delta));
    const qEl = document.getElementById('pisel-qty-' + id);
    qEl.textContent = s.selected;
    qEl.classList.toggle('has-sel', s.selected > 0);
    const sub = s.selected * s.up;
    document.getElementById('pisel-sub-' + id).textContent = s.selected > 0 ? fmt(sub) : '—';
    document.getElementById('pisel-minus-' + id).style.opacity = s.selected === 0 ? '0.3' : '1';
    document.getElementById('pisel-plus-' + id).style.opacity = s.selected === s.max ? '0.3' : '1';
    updatePiselTotal();
}
function updatePiselTotal() {
    let t = 0;
    Object.values(piselState).forEach(s => { t += s.selected * s.up; });
    document.getElementById('piselTotalVal').textContent = fmt(t);
    document.getElementById('piselApplyBtn').disabled = t <= 0;
}
function applyPisel() {
    let t = 0;
    Object.values(piselState).forEach(s => { t += s.selected * s.up; });
    if (t <= 0) return;
    document.getElementById('payAmountDisplay').value = t.toFixed(2).replace('.', ',');
    updateChange();
}

// ── Ödeme formu ──────────────────────────────────────────────
function selectMethod(btn, method) {
    document.querySelectorAll('.method-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    document.getElementById('selectedMethod').value = method;
    currentMethod = method;
    document.getElementById('changeRow').style.display = method === 'cash' ? 'block' : 'none';
    updateChange();
}
function updateRemaining() {
    const disc = parseLD(document.getElementById('discountDisplay').value);
    const net = Math.max(0, orderTotal - disc - alreadyPaid);
    const netFull = Math.max(0, orderTotal - disc);
    document.getElementById('pm-remaining').textContent = fmt(net);
    document.getElementById('fillAmountLabel').textContent = fmt(net);
    const dr = document.getElementById('pm-disc-row');
    const dl = document.getElementById('disc-lbl');
    if (disc > 0) {
        dr.style.display = 'flex'; dl.style.display = 'inline';
        document.getElementById('pm-disc-val').textContent = '−' + fmt(disc);
        document.getElementById('net-amount').textContent = netFull.toFixed(2).replace('.', ',');
    } else {
        dr.style.display = 'none'; dl.style.display = 'none';
    }
    updateChange();
}
function fillRemaining() {
    const disc = parseLD(document.getElementById('discountDisplay').value);
    const rem = Math.max(0, orderTotal - disc - alreadyPaid);
    document.getElementById('payAmountDisplay').value = rem.toFixed(2).replace('.', ',');
    updateChange();
}
function updateChange() {
    if (currentMethod !== 'cash') return;
    const disc = parseLD(document.getElementById('discountDisplay').value);
    const rem = Math.max(0, orderTotal - disc - alreadyPaid);
    const entered = parseLD(document.getElementById('payAmountDisplay').value);
    document.getElementById('changeDisplay').textContent = fmt(Math.max(0, entered - rem));
}
function syncPayForm() {
    const payVal = parseLD(document.getElementById('payAmountDisplay').value);
    const discVal = parseLD(document.getElementById('discountDisplay').value);
    const err = document.getElementById('err-amount');
    if (payVal <= 0) { err.style.display = 'block'; return false; }
    err.style.display = 'none';
    document.getElementById('paymentAmountStr').value = payVal.toFixed(2);
    document.getElementById('discountAmountStr').value = discVal.toFixed(2);
    const c = document.getElementById('piselHiddenInputs');
    c.innerHTML = '';
    Object.entries(piselState).forEach(([id, s]) => {
        if (s.selected > 0) {
            c.innerHTML += `<input type="hidden" name="paidItemIds"  value="${id}">` +
                `<input type="hidden" name="paidItemQtys" value="${s.selected}">`;
        }
    });
    return true;
}

setTimeout(() => {
    document.querySelectorAll('.alert').forEach(a => {
        a.style.transition = 'opacity .5s'; a.style.opacity = '0';
        setTimeout(() => a.remove(), 500);
    });
}, 3000);
