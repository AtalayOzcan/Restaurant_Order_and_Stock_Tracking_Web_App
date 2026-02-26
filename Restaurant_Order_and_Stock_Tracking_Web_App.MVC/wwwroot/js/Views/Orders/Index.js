document.addEventListener("DOMContentLoaded", () => {

    // ── 1. Tab Değiştirme (Event Delegation / Olay Delegasyonu) ──
    const tabBtns = document.querySelectorAll('.js-tab-btn');

    tabBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            // Hangi taba tıklandığını HTML'deki data-tab özelliğinden anlıyoruz
            const tabName = this.getAttribute('data-tab');

            // Tüm butonlardaki active sınıfını kaldır ve sadece tıklanana ekle
            document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
            this.classList.add('active');

            // Tüm içerik alanlarını gizle
            document.getElementById('tabActive').classList.add('d-none');
            document.getElementById('tabPast').classList.add('d-none');

            // Seçilen içeriği göster
            if (tabName === 'active') {
                document.getElementById('tabActive').classList.remove('d-none');
            } else {
                document.getElementById('tabPast').classList.remove('d-none');
            }

            // Arama veya sayfa yenilemede aktif tabın hatırlanması için gizli inputu güncelle
            document.getElementById('tabHidden').value = tabName;
        });
    });

    // ── 2. Arama İşlemleri (Input Dinleyicisi) ──
    const searchInput = document.getElementById('searchInput');
    const searchForm = document.getElementById('searchForm');
    let searchTimer;

    if (searchInput) {
        searchInput.addEventListener('input', () => {
            clearTimeout(searchTimer);
            // Kullanıcı yazmayı bıraktıktan 600ms sonra formu otomatik gönder
            searchTimer = setTimeout(() => {
                searchForm.submit();
            }, 600);
        });
    }

    // ── 3. Aramayı Temizleme Butonu ──
    const btnClearSearch = document.getElementById('btnClearSearch');
    if (btnClearSearch) {
        btnClearSearch.addEventListener('click', () => {
            searchInput.value = '';
            searchForm.submit();
        });
    }

    // ── 4. Alert / Bildirim Mesajlarını Otomatik Kapatma ──
    setTimeout(() => {
        document.querySelectorAll('.alert').forEach(alertBox => {
            alertBox.style.transition = 'opacity .5s';
            alertBox.style.opacity = '0';
            setTimeout(() => alertBox.remove(), 500);
        });
    }, 3000);

});