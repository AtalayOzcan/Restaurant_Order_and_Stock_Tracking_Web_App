
document.getElementById('createForm').addEventListener('submit', async e => {
	e.preventDefault();
	const btn = e.submitter; btn.disabled = true;

	const body = new URLSearchParams({
		menuItemName: document.getElementById('menuItemName').value,
		categoryId: document.getElementById('categoryId').value,
		menuItemPrice: document.getElementById('menuItemPrice').value,
		description: document.getElementById('description').value,
		stockQuantity: document.getElementById('stockQuantity').value,
		trackStock: document.getElementById('trackStock').checked,
		isAvailable: document.getElementById('isAvailable').checked,
		__RequestVerificationToken: document.querySelector('input[name="__RequestVerificationToken"]').value
	});

	const res = await fetch('/Menu/Create', { method: 'POST', body });
	const data = await res.json();
	btn.disabled = false;

	if (data.success) {
		window.location.href = '/Menu';
	} else {
		const box = document.getElementById('alertBox');
		box.textContent = data.message;
		box.style.display = 'block';
	}
});
