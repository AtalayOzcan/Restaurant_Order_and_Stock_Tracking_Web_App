
        // ── Helpers ─────────────────────────────────────────────────
    function openModal(id)  {document.getElementById(id).classList.add('open'); }
    function closeModal(id) {document.getElementById(id).classList.remove('open'); }

        // Close on overlay click
        document.querySelectorAll('.modal-overlay').forEach(overlay => {
        overlay.addEventListener('click', e => {
            if (e.target === overlay) overlay.classList.remove('open');
        });
        });

    function showToast(message, type = 'success') {
            const container = document.getElementById('toastContainer');
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.innerHTML = `<span>${type === 'success' ? '✅' : '❌'}</span><span>${message}</span>`;
    container.appendChild(toast);
            setTimeout(() => toast.remove(), 3500);
        }

    function getToken() {
            return document.querySelector('input[name="__RequestVerificationToken"]').value;
        }

    // ── Create ───────────────────────────────────────────────────
    function openCreateModal() {
        document.getElementById('createForm').reset();
    document.getElementById('create_isActive').checked = true;
    openModal('createModal');
        }

        document.getElementById('createForm').addEventListener('submit', async e => {
        e.preventDefault();
    const btn = e.submitter;
    btn.disabled = true;

    const body = new URLSearchParams({
        categoryName: document.getElementById('create_categoryName').value,
    categorySortOrder: document.getElementById('create_sortOrder').value,
    isActive: document.getElementById('create_isActive').checked,
    __RequestVerificationToken: getToken()
            });

    const res  = await fetch('/Category/Create', {method: 'POST', body });
    const data = await res.json();

    btn.disabled = false;
    if (data.success) {
        closeModal('createModal');
    showToast(data.message, 'success');
                setTimeout(() => location.reload(), 800);
            } else {
        showToast(data.message, 'error');
            }
        });

    // ── Edit ─────────────────────────────────────────────────────

    async function openEditModal(id) {
            const res  = await fetch(`/Category/GetById/${id}`);
    const data = await res.json();
    if (!data.success) {showToast('Veri alınamadı.', 'error'); return; }

    document.getElementById('edit_id').value           = data.categoryId;
    document.getElementById('edit_categoryName').value = data.categoryName;
    document.getElementById('edit_sortOrder').value    = data.categorySortOrder;
    document.getElementById('edit_isActive').checked   = data.isActive;
    openModal('editModal');
        }

        document.getElementById('editForm').addEventListener('submit', async e => {
        e.preventDefault();
    const btn = e.submitter;
    btn.disabled = true;

    const id   = document.getElementById('edit_id').value;
    const body = new URLSearchParams({
        id,
        categoryName: document.getElementById('edit_categoryName').value,
    categorySortOrder: document.getElementById('edit_sortOrder').value,
    isActive: document.getElementById('edit_isActive').checked,
    __RequestVerificationToken: getToken()
            });

    const res  = await fetch('/Category/Edit', {method: 'POST', body });
    const data = await res.json();

    btn.disabled = false;
    if (data.success) {
        closeModal('editModal');
    showToast(data.message, 'success');
                setTimeout(() => location.reload(), 800);
            } else {
        showToast(data.message, 'error');
            }
        });

    // ── Delete ───────────────────────────────────────────────────
    function openDeleteModal(id, name) {
        document.getElementById('delete_id').value = id;
    document.getElementById('delete_name').textContent = name;
    openModal('deleteModal');
        }

        document.getElementById('deleteForm').addEventListener('submit', async e => {
        e.preventDefault();
    const btn = e.submitter;
    btn.disabled = true;

    const id   = document.getElementById('delete_id').value;
    const body = new URLSearchParams({
        id,
        __RequestVerificationToken: getToken()
            });

    const res  = await fetch('/Category/Delete', {method: 'POST', body });
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
        });
