
        /* ════════════════════════════════════════════════════════════
    STATE
    ════════════════════════════════════════════════════════════ */
    const basket = { }; // {[id]: {id, name, price, qty, note} }

    /* ════════════════════════════════════════════════════════════
       ÜRÜN EKLE / ÇIKAR
    ════════════════════════════════════════════════════════════ */
    function addItem(id) {
            const card = document.getElementById('icard-' + id);
    if (!card) return;

    const name  = card.dataset.name;
    const price = parseFloat(card.dataset.price);

    if (basket[id]) {
        basket[id].qty++;
            } else {
        basket[id] = { id, name, price, qty: 1, note: '' };
            }

    // Kart → yeşil + rozet
    card.classList.add('selected');
    updateBadge(id);

    // Küçük "tıkladım" geri bildirimi
    card.style.transform = 'scale(.94)';
            setTimeout(() => card.style.transform = '', 120);

    render();
        }

    function removeItem(id) {
        delete basket[id];
    const card = document.getElementById('icard-' + id);
    if (card) {
        card.classList.remove('selected');
    updateBadge(id);
            }
    render();
        }

    function changeQty(id, delta) {
            if (!basket[id]) return;
    basket[id].qty += delta;
    if (basket[id].qty < 1) {removeItem(id); return; }
    updateBadge(id);
    render();
        }

    function updateNote(id, val) {
            if (basket[id]) basket[id].note = val;
    syncHidden();
        }

    function updateBadge(id) {
            const badge = document.getElementById('badge-' + id);
    if (!badge) return;
    const item = basket[id];
    badge.textContent = item ? item.qty : '';
        }

    /* ════════════════════════════════════════════════════════════
       RENDER
    ════════════════════════════════════════════════════════════ */
    function render() {
            const items   = Object.values(basket);
    const container = document.getElementById('cartItems');
    const empty   = document.getElementById('cartEmpty');
    const count   = document.getElementById('cartCount');
    const total   = document.getElementById('cartTotal');
    const btnOpen = document.getElementById('btnOpen');

            // Mevcut citem'leri temizle
            container.querySelectorAll('.citem').forEach(el => el.remove());

            const totalQty = items.reduce((s, i) => s + i.qty, 0);
            const totalAmt = items.reduce((s, i) => s + i.price * i.qty, 0);

    // Count rozeti
    count.textContent = totalQty;
    count.classList.remove('pop');
    void count.offsetWidth;
    count.classList.add('pop');

    if (items.length === 0) {
        empty.style.display = 'block';
    btnOpen.disabled = true;
            } else {
        empty.style.display = 'none';
    btnOpen.disabled = false;

                items.forEach(item => {
                    const div = document.createElement('div');
    div.className = 'citem';
    div.innerHTML = `
    <div class="citem-row1">
        <span class="citem-name" title="${esc(item.name)}">${esc(item.name)}</span>
        <div class="citem-qty-ctrl">
            <button type="button" class="cqbtn minus" onclick="changeQty(${item.id},-1)">−</button>
            <span class="citem-qty">${item.qty}</span>
            <button type="button" class="cqbtn" onclick="changeQty(${item.id},1)">+</button>
        </div>
        <span class="citem-price">₺${(item.price * item.qty).toFixed(2).replace('.', ',')}</span>
        <button type="button" class="citem-del" onclick="removeItem(${item.id})" title="Kaldır">×</button>
    </div>
    <input class="citem-note" type="text"
        placeholder="Not: acısız, az pişmiş..."
        value="${esc(item.note)}"
        oninput="updateNote(${item.id}, this.value)"
        maxlength="200" />
    `;
    container.appendChild(div);
                });
            }

    // Toplam animasyonlu güncelle
    total.classList.remove('bump');
    void total.offsetWidth;
    total.classList.add('bump');
    total.textContent = '₺' + totalAmt.toFixed(2).replace('.', ',');

    syncHidden();
        }

    /* ════════════════════════════════════════════════════════════
       HIDDEN INPUT SYNC
    ════════════════════════════════════════════════════════════ */
    function syncHidden() {
            const cont = document.getElementById('hiddenInputs');
    cont.innerHTML = '';
            Object.values(basket).forEach(item => {
        cont.innerHTML += `
                    <input type="hidden" name="menuItemIds" value="${item.id}" />
                    <input type="hidden" name="quantities"  value="${item.qty}" />
                    <input type="hidden" name="itemNotes"   value="${esc(item.note)}" />
                `;
            });
        }

    /* ════════════════════════════════════════════════════════════
       KATEGORİ FİLTRE
    ════════════════════════════════════════════════════════════ */
    let activeCat = 'all';

    function filterCat(catKey, btn) {
        activeCat = catKey;

    // Arama temizle
    document.getElementById('searchInput').value = '';
    document.getElementById('searchLabel').classList.remove('show');

            // Tab durumu
            document.querySelectorAll('.cat-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');

            // Tüm kartları göster / gizle
            document.querySelectorAll('.item-card').forEach(c => c.style.display = '');

            // Kategori bloklarını göster / gizle
            document.querySelectorAll('.cat-block').forEach(block => {
                const show = catKey === 'all' || block.id === catKey;
    block.style.display = show ? '' : 'none';
            });

    document.getElementById('itemsEmpty').classList.remove('visible');
        }

    /* ════════════════════════════════════════════════════════════
       ARAMA
    ════════════════════════════════════════════════════════════ */
    function doSearch(q) {
        q = q.toLowerCase().trim();
    const label = document.getElementById('searchLabel');

    if (!q) {
        // Arama boş → aktif kategoriye dön
        filterCat(activeCat, document.querySelector(`.cat-btn[data-cat="${activeCat}"]`));
    return;
            }

            // Tab aktifliğini kaldır (arama modundayken)
            document.querySelectorAll('.cat-btn').forEach(b => b.classList.remove('active'));

            // Tüm blokları aç
            document.querySelectorAll('.cat-block').forEach(b => b.style.display = '');

    // Kartları filtrele
    let found = 0;
            document.querySelectorAll('.item-card').forEach(card => {
                const match = card.dataset.keywords.includes(q);
    card.style.display = match ? '' : 'none';
    if (match) found++;
            });

            // Boş kalan kategori bloklarını gizle
            document.querySelectorAll('.cat-block').forEach(block => {
                const anyVisible = [...block.querySelectorAll('.item-card')]
                    .some(c => c.style.display !== 'none');
    block.style.display = anyVisible ? '' : 'none';
            });

    label.textContent = `"${q}" — ${found} ürün bulundu`;
    label.classList.toggle('show', true);

    const emptyEl = document.getElementById('itemsEmpty');
    emptyEl.classList.toggle('visible', found === 0);
        }

        // Escape ile aramayı kapat
        document.getElementById('searchInput').addEventListener('keydown', e => {
            if (e.key === 'Escape') {
        e.target.value = '';
    doSearch('');
    e.target.blur();
            }
        });

    /* ════════════════════════════════════════════════════════════
       FORM VALİDASYON
    ════════════════════════════════════════════════════════════ */
    function clearErr(field) {
            if (field === 'garson') {
        document.getElementById('inp-garson').classList.remove('error');
    document.getElementById('err-garson').classList.remove('show');
            }
        }

    function validateOrder() {
        let ok = true;
    const garson = document.getElementById('inp-garson');
    const errG   = document.getElementById('err-garson');

    if (!garson.value.trim()) {
        garson.classList.add('error');
    errG.classList.add('show');
    garson.focus();
    ok = false;
            } else {
        garson.classList.remove('error');
    errG.classList.remove('show');
            }

    if (Object.keys(basket).length === 0) {
                // Küçük alert yerine sepet panelini hafifçe shake yap
                const cart = document.querySelector('.pos-cart');
    cart.style.animation = 'none';
    void cart.offsetWidth;
    cart.style.animation = 'shake .35s ease';
    ok = false;
            }

    return ok;
        }

    // Sepet shake animasyonu
    const shakeStyle = document.createElement('style');
    shakeStyle.textContent = `
    @keyframes shake {
        0 %, 100 % { transform: translateX(0) }
            20%{transform:translateX(-5px)}
    40%{transform:translateX(5px)}
    60%{transform:translateX(-4px)}
    80%{transform:translateX(4px)}
        }`;
    document.head.appendChild(shakeStyle);

    /* ════════════════════════════════════════════════════════════
       YARDIMCI
    ════════════════════════════════════════════════════════════ */
    function esc(str) {
            return String(str)
    .replace(/&/g,'&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
        }

// Alert otomatik kaybol
setTimeout(() => {
    const a = document.querySelector('.pos-alert');
    if (a) { a.style.transition = 'opacity .5s'; a.style.opacity = '0'; setTimeout(() => a.remove(), 500); }
}, 3000);
 