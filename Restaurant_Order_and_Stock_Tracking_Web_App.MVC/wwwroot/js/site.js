document.addEventListener("DOMContentLoaded", () => {

    // ── Sidebar Collapse (Aç/Kapa) Mantığı ──
    const sidebar = document.getElementById('sidebar');
    const mainEl = document.getElementById('main');
    const toggleBtn = document.getElementById('toggleBtn');

    if (toggleBtn && sidebar && mainEl) {
        toggleBtn.addEventListener('click', () => {
            const collapsed = sidebar.classList.toggle('collapsed');
            mainEl.classList.toggle('shifted', collapsed);
            localStorage.setItem('sidebarCollapsed', collapsed);
        });

        // Sayfa yüklendiğinde önceki durumu kontrol et
        if (localStorage.getItem('sidebarCollapsed') === 'true') {
            sidebar.classList.add('collapsed');
            mainEl.classList.add('shifted');
        }
    }

    // ── Dark / Light Tema Değiştirme Mantığı ──
    const html = document.documentElement;
    const themeToggle = document.getElementById('themeToggle');

    // Sayfa ilk yüklendiğinde varsayılan temayı uygula
    html.dataset.theme = localStorage.getItem('theme') || 'dark';

    if (themeToggle) {
        themeToggle.addEventListener('click', () => {
            const next = html.dataset.theme === 'dark' ? 'light' : 'dark';
            html.dataset.theme = next;
            localStorage.setItem('theme', next);
        });
    }

    // ── Topbar Canlı Saat ──
    const clockEl = document.getElementById('clock');
    if (clockEl) {
        function tick() {
            clockEl.textContent = new Date().toLocaleTimeString('tr-TR', {
                hour: '2-digit',
                minute: '2-digit'
            });
        }
        setInterval(tick, 1000);
        tick(); // Sayfa açılır açılmaz saati göster
    }

});