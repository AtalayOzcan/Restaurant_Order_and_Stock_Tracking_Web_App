document.addEventListener("DOMContentLoaded", () => {

    // ── 1. C# Verilerini HTML'den (JSON Adacığından) Oku ──────────────────────
    const configEl = document.getElementById('wasteReportConfig');
    let state = { preset: 'today', from: '', to: '' };
    if (configEl) {
        state = JSON.parse(configEl.textContent);
    }

    // ── Ortak Yardımcılar ──────────────────────────────────────────────────────
    let charts = {};

    function destroyChart(id) {
        if (charts[id]) { charts[id].destroy(); delete charts[id]; }
    }

    function isDark() { return document.documentElement.dataset.theme !== 'light'; }
    function gridColor() { return isDark() ? 'rgba(255,255,255,.06)' : 'rgba(0,0,0,.06)'; }
    function textColor() { return isDark() ? '#687080' : '#8A95A3'; }

    // ── Para Birimi Formatlama ─────────────────────────────────────────────────
    function formatCurrency(val) {
        return '₺' + Number(val || 0).toLocaleString('tr-TR', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    // ── FIX: Widget DOM Güncellemesi ───────────────────────────────────────────
    // Eski yapı: navigatePage() → window.location → full page reload.
    //   Grafikler AJAX ile yüklenirken widget'lar Razor'ın ilk render değerinde
    //   kalıyordu; filtre değiştikçe sayfa tamamen yeniden yüklenmek zorundaydı.
    // Yeni yapı: applyFilter() → loadWasteCharts() AJAX → hem grafikler hem de
    //   3 özet kart aynı fetch cevabıyla anlık güncellenir (Sales.js ile aynı pattern).
    function updateSummaryCards(data) {
        const total = document.getElementById('wv-total');
        const order = document.getElementById('wv-order');
        const stock = document.getElementById('wv-stock');

        if (total) total.textContent = formatCurrency(data.totalWasteLoss);
        if (order) order.textContent = formatCurrency(data.orderWasteTotal);
        if (stock) stock.textContent = formatCurrency(data.stockLogWasteTotal);
    }

    // ── QueryString Oluşturucu ─────────────────────────────────────────────────
    function buildQs() {
        const q = new URLSearchParams({ preset: state.preset });
        if (state.preset === 'custom') {
            q.set('from', state.from);
            q.set('to', state.to);
        }
        return q.toString();
    }

    // ── Filtre Uygulama: AJAX (full reload yok) ────────────────────────────────
    function applyFilter() {
        history.replaceState(null, '', `/Reports/CancelAndWaste?${buildQs()}`);
        loadWasteCharts();
    }

    // ── Accordion Toggle ───────────────────────────────────────────────────────
    window.toggleAcc = function (header) {
        const body = header.nextElementSibling;
        body.classList.toggle('open');
        const count = header.querySelector('.count');
        if (count) {
            count.textContent = count.textContent.endsWith('▼')
                ? count.textContent.replace('▼', '▲')
                : count.textContent.replace('▲', '▼');
        }
    };

    // ── Olay Dinleyicileri ─────────────────────────────────────────────────────
    document.querySelectorAll('.preset-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('.preset-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            state.preset = btn.dataset.preset;

            const isCustom = state.preset === 'custom';
            const dateFromEl = document.getElementById('dateFrom');
            const dateToEl = document.getElementById('dateTo');
            if (dateFromEl) dateFromEl.style.display = isCustom ? '' : 'none';
            if (dateToEl) dateToEl.style.display = isCustom ? '' : 'none';

            if (!isCustom) applyFilter();  // eski: navigatePage() → full reload
        });
    });

    const dateFromEl = document.getElementById('dateFrom');
    if (dateFromEl) {
        dateFromEl.addEventListener('change', e => { state.from = e.target.value; applyFilter(); });
    }

    const dateToEl = document.getElementById('dateTo');
    if (dateToEl) {
        dateToEl.addEventListener('change', e => { state.to = e.target.value; applyFilter(); });
    }

    const btnCsv = document.getElementById('btnCsv');
    if (btnCsv) {
        btnCsv.addEventListener('click', () => {
            window.location = `/Reports/ExportCsv?type=waste&${buildQs()}`;
        });
    }

    // ── Grafik + Widget Yükleme (Fetch) ───────────────────────────────────────
    async function loadWasteCharts() {
        const srcLoading = document.getElementById('sourceLoading');
        const prodLoading = document.getElementById('productLoading');
        if (srcLoading) srcLoading.style.display = 'flex';
        if (prodLoading) prodLoading.style.display = 'flex';

        try {
            const res = await fetch(`/Reports/GetWasteChartData?${buildQs()}`);
            if (!res.ok) throw new Error("Veri çekilemedi.");
            const data = await res.json();

            if (srcLoading) srcLoading.style.display = 'none';
            if (prodLoading) prodLoading.style.display = 'none';

            // ── FIX: Fetch cevabıyla 3 özet kartı anında güncelle ─────────────
            updateSummaryCards(data);

            // ── Kaynak Dağılımı (Doughnut) ────────────────────────────────────
            const ctxSource = document.getElementById('wasteSourceChart');
            if (ctxSource) {
                destroyChart('wasteSource');
                charts['wasteSource'] = new Chart(ctxSource.getContext('2d'), {
                    type: 'doughnut',
                    data: {
                        labels: ['Sipariş Kaynağı', 'Stok Hareketi'],
                        datasets: [{
                            data: [data.orderWasteTotal || 0, data.stockLogWasteTotal || 0],
                            backgroundColor: ['#ef4444', '#3b82f6'],
                            borderWidth: 2,
                            borderColor: isDark() ? '#161A20' : '#ffffff'
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: { labels: { color: textColor() } },
                            tooltip: {
                                callbacks: {
                                    label: ctx => `₺${ctx.parsed.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}`
                                }
                            }
                        }
                    }
                });
            }

            // ── En Çok Fire Veren Ürünler (Bar) ──────────────────────────────
            const top = data.topProducts || [];
            const ctxProduct = document.getElementById('wasteProductChart');
            if (ctxProduct) {
                destroyChart('wasteProduct');
                charts['wasteProduct'] = new Chart(ctxProduct.getContext('2d'), {
                    type: 'bar',
                    data: {
                        labels: top.map(x => x.name),
                        datasets: [{
                            label: 'Kayıp (₺)',
                            data: top.map(x => x.loss),
                            backgroundColor: 'rgba(239,68,68,.75)',
                            borderColor: '#ef4444',
                            borderWidth: 1,
                            borderRadius: 4
                        }]
                    },
                    options: {
                        indexAxis: 'y',
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: { legend: { display: false } },
                        scales: {
                            x: {
                                grid: { color: gridColor() },
                                ticks: { color: textColor(), callback: v => `₺${v}` }
                            },
                            y: {
                                grid: { display: false },
                                ticks: { color: textColor() }
                            }
                        }
                    }
                });
            }
        } catch (error) {
            console.error("Grafik yükleme hatası:", error);
            const srcLoading2 = document.getElementById('sourceLoading');
            const prodLoading2 = document.getElementById('productLoading');
            if (srcLoading2) srcLoading2.style.display = 'none';
            if (prodLoading2) prodLoading2.style.display = 'none';
        }
    }

    // ── İlk Yükleme ───────────────────────────────────────────────────────────
    loadWasteCharts();
});