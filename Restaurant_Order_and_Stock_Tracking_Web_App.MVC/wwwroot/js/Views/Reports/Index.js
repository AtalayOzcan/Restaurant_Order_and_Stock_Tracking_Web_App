document.addEventListener("DOMContentLoaded", () => {

    // ── 1. C# Verilerini HTML'den (JSON Adacığından) Oku ──
    const dataEl = document.getElementById('hourlySalesData');
    let serverHourly = [];

    if (dataEl) {
        serverHourly = JSON.parse(dataEl.textContent);
    }

    // ── 2. Grafik Etiketlerini ve Verilerini Hazırla ──
    const labels = Array.from({ length: 24 }, (_, i) => `${String(i).padStart(2, '0')}:00`);

    const data = labels.map((_, h) => {
        // ÇÖZÜM BURADA: Hem büyük harfli (C# PascalCase) hem küçük harfli (JSON camelCase) uyumu eklendi
        const found = serverHourly.find(x => x.Hour === h || x.hour === h);
        return found ? (found.Amount || found.amount || 0) : 0;
    });

    // ── 3. Tema ve Renk Ayarları ──
    const isDark = document.documentElement.dataset.theme !== 'light';
    const gridColor = isDark ? 'rgba(255,255,255,.06)' : 'rgba(0,0,0,.06)';
    const textColor = isDark ? '#687080' : '#8A95A3';

    // ── 4. Chart.js ile Grafiği Çiz ──
    const canvas = document.getElementById('hourlyChart');
    if (canvas) {
        const ctx = canvas.getContext('2d');

        new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Ciro (₺)',
                    data: data,
                    backgroundColor: 'rgba(249,115,22,.75)',
                    borderColor: '#F97316',
                    borderWidth: 2,
                    borderRadius: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    x: {
                        grid: { color: gridColor },
                        ticks: { color: textColor, maxRotation: 0 }
                    },
                    y: {
                        grid: { color: gridColor },
                        ticks: { color: textColor, callback: v => `₺${v}` }
                    }
                }
            }
        });
    }

    // ── 5. Yenile Butonu İçin Olay Dinleyicisi ──
    const refreshBtn = document.querySelector('.rpt-refresh-btn');
    if (refreshBtn) {
        refreshBtn.addEventListener('click', () => {
            location.reload();
        });
    }

});