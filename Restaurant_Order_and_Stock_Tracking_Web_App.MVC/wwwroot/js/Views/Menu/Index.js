window.switchTab = function (clickedBtn, tablistId) {
    const tablist = document.getElementById(tablistId);
    if (!tablist) return;

    tablist.querySelectorAll('.lang-tab-btn').forEach(btn => {
        btn.classList.remove('active');
        btn.setAttribute('aria-selected', 'false');
    });

    clickedBtn.classList.add('active');
    clickedBtn.setAttribute('aria-selected', 'true');

    const modal = tablist.closest('.modal');
    if (!modal) return;

    modal.querySelectorAll('.lang-pane').forEach(pane => {
        pane.style.display = 'none';
    });

    const targetPane = modal.querySelector('#' + clickedBtn.getAttribute('data-target'));
    if (targetPane) targetPane.style.display = '';
};

function openModal(id) { document.getElementById(id).classList.add('open'); }
function closeModal(id) { document.getElementById(id).classList.remove('open'); }

document.querySelectorAll('.modal-overlay').forEach(o =>
    o.addEventListener('click', e => { if (e.target === o) o.classList.remove('open'); })
);

function showToast(msg, type = 'success') {
    const c = document.getElementById('toastContainer');
    const t = document.createElement('div');
    t.className = `toast toast-${type}`;
    t.innerHTML = `<span>${type === 'success' ? '✅' : '❌'}</span><span>${msg}</span>`;
    c.appendChild(t);
    setTimeout(() => t.remove(), 3500);
}

function getToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]').value;
}

function resetTabsToTR(modalId, tablistId, panePrefix) {
    const modal = document.getElementById(modalId);
    const tablist = document.getElementById(tablistId);
    if (!modal || !tablist) return;

    tablist.querySelectorAll('.lang-tab-btn').forEach(btn => {
        btn.classList.remove('active');
        btn.setAttribute('aria-selected', 'false');
    });

    const firstBtn = tablist.querySelector('.lang-tab-btn');
    if (firstBtn) {
        firstBtn.classList.add('active');
        firstBtn.setAttribute('aria-selected', 'true');
    }

    modal.querySelectorAll('.lang-pane').forEach(p => p.style.display = 'none');
    const trPane = modal.querySelector(`#${panePrefix}-pane-tr`);
    if (trPane) trPane.style.display = '';
}

function filterTable() {
    const search = document.getElementById('searchInput').value.toLowerCase();
    const cat = document.getElementById('catFilter').value;
    const status = document.getElementById('statusFilter').value;

    document.querySelectorAll('#menuTable tbody tr[data-name]').forEach(row => {
        const matchName = row.dataset.name.includes(search);
        const matchCat = !cat || row.dataset.cat === cat;
        const matchStatus = !status || row.dataset.status === status;
        row.style.display = (matchName && matchCat && matchStatus) ? '' : 'none';
    });
}

// ── CREATE ───────────────────────────────────────────────────────────
function openCreateModal() {
    document.getElementById('createForm').reset();
    document.getElementById('c_isAvailable').checked = true;
    resetTabsToTR('createModal', 'createMenuLangTabs', 'cm');
    openModal('createModal');
}

document.getElementById('createForm')?.addEventListener('submit', async e => {
    e.preventDefault();
    const btn = e.submitter;
    btn.disabled = true;

    const payload = {
        menuItemName: document.getElementById('c_name').value.trim(),
        nameEn: document.getElementById('c_nameEn').value.trim() || null,
        nameAr: document.getElementById('c_nameAr').value.trim() || null,
        nameRu: document.getElementById('c_nameRu').value.trim() || null,
        categoryId: parseInt(document.getElementById('c_categoryId').value) || 0,
        menuItemPriceStr: document.getElementById('c_price').value,
        description: document.getElementById('c_description').value.trim() || null,
        descriptionEn: document.getElementById('c_descriptionEn').value.trim() || null,
        descriptionAr: document.getElementById('c_descriptionAr').value.trim() || null,
        descriptionRu: document.getElementById('c_descriptionRu').value.trim() || null,
        stockQuantity: parseInt(document.getElementById('c_stock').value) || 0,
        trackStock: document.getElementById('c_trackStock').checked,
        isAvailable: document.getElementById('c_isAvailable').checked
    };

    try {
        const res = await fetch('/Menu/Create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': getToken() },
            body: JSON.stringify(payload)
        });
        const data = await res.json();
        btn.disabled = false;

        if (data.success) {
            closeModal('createModal');
            showToast(data.message, 'success');
            setTimeout(() => location.reload(), 800);
        } else {
            showToast(data.message, 'error');
        }
    } catch {
        btn.disabled = false;
        showToast('Bağlantı hatası oluştu.', 'error');
    }
});

// ── EDIT ─────────────────────────────────────────────────────────────
async function openEditModal(id) {
    try {
        const res = await fetch(`/Menu/GetById/${id}`);
        const data = await res.json();
        if (!data.success) { showToast('Veri alınamadı.', 'error'); return; }

        document.getElementById('e_id').value = data.menuItemId;
        document.getElementById('e_name').value = data.menuItemName ?? '';
        document.getElementById('e_nameEn').value = data.nameEn ?? '';
        document.getElementById('e_nameAr').value = data.nameAr ?? '';
        document.getElementById('e_nameRu').value = data.nameRu ?? '';
        document.getElementById('e_categoryId').value = data.categoryId;
        document.getElementById('e_price').value = data.menuItemPrice;
        document.getElementById('e_description').value = data.description ?? '';
        document.getElementById('e_descriptionEn').value = data.descriptionEn ?? '';
        document.getElementById('e_descriptionAr').value = data.descriptionAr ?? '';
        document.getElementById('e_descriptionRu').value = data.descriptionRu ?? '';
        document.getElementById('e_stock').value = data.stockQuantity;
        document.getElementById('e_trackStock').checked = data.trackStock;
        document.getElementById('e_isAvailable').checked = data.isAvailable;

        resetTabsToTR('editModal', 'editMenuLangTabs', 'em');
        openModal('editModal');
    } catch {
        showToast('Veri çekilirken hata oluştu.', 'error');
    }
}

document.getElementById('editForm')?.addEventListener('submit', async e => {
    e.preventDefault();
    const btn = e.submitter;
    btn.disabled = true;

    const payload = {
        id: parseInt(document.getElementById('e_id').value),
        menuItemName: document.getElementById('e_name').value.trim(),
        nameEn: document.getElementById('e_nameEn').value.trim() || null,
        nameAr: document.getElementById('e_nameAr').value.trim() || null,
        nameRu: document.getElementById('e_nameRu').value.trim() || null,
        categoryId: parseInt(document.getElementById('e_categoryId').value) || 0,
        menuItemPriceStr: document.getElementById('e_price').value,
        description: document.getElementById('e_description').value.trim() || null,
        descriptionEn: document.getElementById('e_descriptionEn').value.trim() || null,
        descriptionAr: document.getElementById('e_descriptionAr').value.trim() || null,
        descriptionRu: document.getElementById('e_descriptionRu').value.trim() || null,
        stockQuantity: parseInt(document.getElementById('e_stock').value) || 0,
        trackStock: document.getElementById('e_trackStock').checked,
        isAvailable: document.getElementById('e_isAvailable').checked
    };

    try {
        const res = await fetch('/Menu/Edit', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': getToken() },
            body: JSON.stringify(payload)
        });
        const data = await res.json();
        btn.disabled = false;

        if (data.success) {
            closeModal('editModal');
            showToast(data.message, 'success');
            setTimeout(() => location.reload(), 800);
        } else {
            showToast(data.message, 'error');
        }
    } catch {
        btn.disabled = false;
        showToast('Bağlantı hatası oluştu.', 'error');
    }
});

// ── DELETE ───────────────────────────────────────────────────────────
function openDeleteModal(id, name) {
    document.getElementById('d_id').value = id;
    document.getElementById('d_name').textContent = name;
    openModal('deleteModal');
}

document.getElementById('deleteForm').addEventListener('submit', async e => {
    e.preventDefault();
    const btn = e.submitter;
    btn.disabled = true;

    const body = new URLSearchParams({
        id: document.getElementById('d_id').value,
        __RequestVerificationToken: getToken()
    });

    try {
        const res = await fetch('/Menu/Delete', { method: 'POST', body });
        const data = await res.json();
        btn.disabled = false;

        if (data.success) {
            closeModal('deleteModal');
            showToast(data.message, 'success');
            setTimeout(() => location.reload(), 800);
        } else {
            closeModal('deleteModal');
            showToast(data.message, 'error');
        }
    } catch {
        btn.disabled = false;
        closeModal('deleteModal');
        showToast('Bağlantı hatası oluştu.', 'error');
    }
});