// ── Dil sekme geçişi ──────────────────────────────────────────
function switchLangTab(lang, btn) {
    document.querySelectorAll('.lang-tab').forEach(b => b.classList.remove('active'));
    document.querySelectorAll('.lang-panel').forEach(p => p.style.display = 'none');
    btn.classList.add('active');
    document.getElementById('panel-' + lang).style.display = '';
}

// ── Form gönder ───────────────────────────────────────────────
document.getElementById('createForm')?.addEventListener('submit', async e => {
    e.preventDefault();
    const btn = e.submitter;
    btn.disabled = true;

    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    const payload = {
        menuItemName: document.getElementById('c_name').value.trim(),
        nameEn: document.getElementById('c_nameEn').value.trim(),
        nameAr: document.getElementById('c_nameAr').value.trim(),
        nameRu: document.getElementById('c_nameRu').value.trim(),
        categoryId: parseInt(document.getElementById('c_categoryId').value) || 0,
        menuItemPriceStr: document.getElementById('c_price').value,
        description: document.getElementById('c_description').value.trim(),
        descriptionEn: document.getElementById('c_descriptionEn').value.trim(),
        descriptionAr: document.getElementById('c_descriptionAr').value.trim(),
        descriptionRu: document.getElementById('c_descriptionRu').value.trim(),
        stockQuantity: parseInt(document.getElementById('c_stock').value) || 0,
        trackStock: document.getElementById('c_trackStock').checked,
        isAvailable: document.getElementById('c_isAvailable').checked
    };

    try {
        const res = await fetch('/Menu/Create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify(payload)
        });
        const data = await res.json();
        btn.disabled = false;

        if (data.success) {
            window.location.href = '/Menu';
        } else {
            const box = document.getElementById('alertBox');
            box.textContent = data.message;
            box.style.display = 'block';
        }
    } catch {
        btn.disabled = false;
        document.getElementById('alertBox').textContent = 'Bağlantı hatası oluştu.';
        document.getElementById('alertBox').style.display = 'block';
    }
});