// Plan 20 Faz B — Klasik kutucuklu organigram (drag-drop edit dahili).
// Lib: dabeng/OrgChart v5.0.0 (jQuery, NPM `orgchart`).
// Modes: "tree" (top-down, t2b) / "horizontal" (left-right, l2r)
// Drop event'inde /Admin/OrgChartReorder'a POST. Sibling order yok — sadece parent değişimi.

(function () {
    const container = document.getElementById('oc-d3');
    if (!container) return;
    if (typeof jQuery === 'undefined') {
        container.innerHTML = '<div style="padding:20px;color:#b91c1c;">jQuery yüklenemedi.</div>';
        return;
    }
    const $ = jQuery;
    if (!$.fn.orgchart) {
        container.innerHTML = '<div style="padding:20px;color:#b91c1c;">OrgChart eklentisi yüklenemedi.</div>';
        return;
    }

    const mode = container.dataset.mode || 'tree';
    const dataEl = document.getElementById('oc-tree-data');
    if (!dataEl) return;

    let raw;
    try { raw = JSON.parse(dataEl.textContent); }
    catch (e) {
        // XSS-safe: e.message JSON içeriğinden gelebilir, textContent ile yaz
        container.innerHTML = '';
        const err = document.createElement('div');
        err.className = 'render-error';
        err.textContent = 'Veri ayrıştırma hatası: ' + e.message;
        container.appendChild(err);
        return;
    }

    // Mosaik schema → OrgChart.js schema
    // { id, code, title, isActive, children } → { id, name (Title), title (Code), className, children }
    function transform(node) {
        return {
            id: String(node.id),
            name: node.title,
            title: node.code,
            className: node.isActive ? '' : 'oc-inactive',
            children: (node.children || []).map(transform)
        };
    }

    const transformed = transform(raw);
    const realRoots = transformed.children || [];
    // Tek root varsa onu kullan; çoklu root varsa sentetik kök altında
    const data = realRoots.length === 1 ? realRoots[0] : transformed;

    container.innerHTML = '';

    // Drag-drop kapalı — taşıma sağ-tık modalı ile yapılıyor (view'daki Alpine ocMoveModal).
    // Lib drag-drop UX'i sorunluydu (cross-parent, JSONDigger quirks); sağ-tık güvenilir.
    const oc = $('#oc-d3').orgchart({
        data: data,
        nodeContent: 'title',
        direction: mode === 'horizontal' ? 'l2r' : 't2b',
        pan: true,
        zoom: true,
        zoominLimit: 2,
        zoomoutLimit: 0.3,
        draggable: false,
        toggleSiblingsResp: false,
        verticalLevel: mode === 'horizontal' ? 1 : 999,
        exportButton: false
    });

    // Switcher tab'larının yanındaki "PNG indir" butonuyla bağla.
    const exportBtn = document.getElementById('oc-export-btn');
    if (exportBtn) {
        exportBtn.addEventListener('click', () => {
            exportBtn.disabled = true;
            const oldText = exportBtn.textContent;
            exportBtn.textContent = 'Hazırlanıyor…';

            function reportFailure(err) {
                console.error('OrgChart PNG export failed', err);
                alert('PNG dışa aktarma başarısız oldu. Sayfayı yenileyip tekrar deneyin.');
            }

            try {
                // dabeng/OrgChart `export()` html2canvas ile async çalışır; sync ve
                // async (Promise rejection) failure'ları ikisini de yakala.
                const result = oc.export('organizasyon-semasi', 'png');
                if (result && typeof result.then === 'function') {
                    result.catch(reportFailure);
                }
            } catch (e) {
                reportFailure(e);
            } finally {
                setTimeout(() => {
                    exportBtn.disabled = false;
                    exportBtn.textContent = oldText;
                }, 2500);
            }
        });
    }
})();
