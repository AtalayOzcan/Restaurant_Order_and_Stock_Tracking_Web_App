
document.getElementById('editForm').addEventListener('submit', async e => {
    e.preventDefault();
    const btn = e.submitter; btn.disabled = true;

    const body = new URLSearchParams({
        id: document.getElementById('editId').value,
        categoryName: document.getElementById('categoryName').value,
        categorySortOrder: document.getElementById('sortOrder').value,
        isActive: document.getElementById('isActive').checked,
        __RequestVerificationToken: document.querySelector('input[name="__RequestVerificationToken"]').value
    });

    const res = await fetch('/Category/Edit', { method: 'POST', body });
    const data = await res.json();
    btn.disabled = false;

    if (data.success) {
        window.location.href = '/Category';
    } else {
        const box = document.getElementById('alertBox');
        box.textContent = data.message;
        box.style.display = 'block';
    }
});
