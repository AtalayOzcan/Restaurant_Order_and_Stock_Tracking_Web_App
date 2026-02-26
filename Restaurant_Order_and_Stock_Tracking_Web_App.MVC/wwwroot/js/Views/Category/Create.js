
document.getElementById('createForm').addEventListener('submit', async e => {
    e.preventDefault();
    const btn = e.submitter; btn.disabled = true;
    const form = e.target;
    const body = new URLSearchParams(new FormData(form));

    const res = await fetch('/Category/Create', { method: 'POST', body });
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
