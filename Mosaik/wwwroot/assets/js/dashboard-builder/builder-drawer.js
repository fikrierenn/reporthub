// dashboard-builder/builder-drawer.js — Form panel + dataset/column editors + save + datalist
// M-11 F-7 alt-commit 1: type-specific form (kpi/chart/table) + dataset/column add/remove +
// save handler + SP preview kolon datalist autocomplete.
// alt-commit 2'de drawer Veri/Gorunum tab'ina genisleyecek (F-8 oncesi iskelet).

(function () {
    "use strict";

    if (!window.DB) return;

    var state = DB.state;
    var esc = DB.helpers.esc;
    var val = DB.helpers.val;
    var genWidgetId = DB.helpers.genWidgetId;
    var colorSelect = DB.helpers.colorSelect;
    var iconSelect = DB.helpers.iconSelect;
    var aggOptions = DB.constants.aggOptions;
    var colors = DB.constants.colors;

    function renderForm() {
        var comp = state.editIndex >= 0 ? state.config.tabs[state.activeTab].components[state.editIndex] : null;
        var type = comp ? comp.type : (document.getElementById('compType') ? document.getElementById('compType').value : 'kpi');

        var contractKeys = Object.keys(state.config.resultContract || {});
        var hasContract = contractKeys.length > 0;
        var currentResult = comp ? (comp.result || '') : '';
        var legacyRsVal = comp && comp.resultSet != null ? comp.resultSet : 0;

        var html = '<div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-5 gap-3 items-end">';

        // Type
        html += '<div><label class="block text-xs font-semibold text-gray-600 mb-1">Tip</label>';
        html += '<select id="compType" class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" onchange="window._dbTypeChange()">';
        html += '<option value="kpi"' + (type === 'kpi' ? ' selected' : '') + '>KPI Karti</option>';
        html += '<option value="chart"' + (type === 'chart' ? ' selected' : '') + '>Grafik</option>';
        html += '<option value="table"' + (type === 'table' ? ' selected' : '') + '>Tablo</option>';
        html += '</select></div>';

        // Title
        html += '<div><label class="block text-xs font-semibold text-gray-600 mb-1">Baslik</label>';
        html += '<input type="text" id="compTitle" class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" value="' + esc(comp ? comp.title : '') + '" placeholder="Ornek: Toplam Kadro"></div>';

        // Result (name)
        html += '<div><label class="block text-xs font-semibold text-gray-600 mb-1">Result <span class="text-[10px] text-blue-600 normal-case">(name)</span></label>';
        html += '<select id="compResult" class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" onchange="window._dbToggleLegacyRs()"' + (hasContract ? '' : ' disabled title="Once Result Contract tanimla"') + '>';
        html += '<option value=""' + (!currentResult ? ' selected' : '') + '>— legacy index —</option>';
        contractKeys.forEach(function (k) {
            html += '<option value="' + esc(k) + '"' + (currentResult === k ? ' selected' : '') + '>' + esc(k) + '</option>';
        });
        html += '</select></div>';

        // Legacy index
        var legacyDisabled = currentResult ? ' disabled' : '';
        html += '<div><label class="block text-xs font-semibold text-gray-500 mb-1">Result Set <span class="text-[10px] text-gray-400 normal-case">(legacy)</span></label>';
        html += '<input type="number" id="compResultSet" class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm disabled:bg-gray-100 disabled:text-gray-400" value="' + legacyRsVal + '" min="0" max="20"' + legacyDisabled + '></div>';

        // Span
        html += '<div><label class="block text-xs font-semibold text-gray-600 mb-1">Genislik (1-4)</label>';
        html += '<select id="compSpan" class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm">';
        for (var s = 1; s <= 4; s++) {
            html += '<option value="' + s + '"' + (comp && comp.span === s ? ' selected' : (!comp && s === 1 ? ' selected' : '')) + '>' + s + '/4</option>';
        }
        html += '</select></div>';
        html += '</div>';

        // Type-specific
        html += '<div class="mt-3">';

        if (type === 'kpi') {
            html += '<div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-5 gap-3 items-end">';
            html += '<div><label class="block text-xs font-semibold text-gray-600 mb-1">Aggregation</label>';
            html += '<select id="compAgg" class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm">';
            aggOptions.forEach(function (a) { html += '<option value="' + a.value + '"' + (comp && comp.agg === a.value ? ' selected' : '') + '>' + a.label + '</option>'; });
            html += '</select></div>';
            html += '<div><label class="block text-xs font-semibold text-gray-600 mb-1">Kolon (opsiyonel)</label>';
            html += '<input type="text" id="compColumn" class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" value="' + esc(comp ? (comp.column || '') : '') + '" placeholder="FiiliGiris"></div>';
            html += '<div><label class="block text-xs font-semibold text-gray-600 mb-1">Kosul</label>';
            html += '<select id="compCondition" class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm">';
            html += '<option value="">Yok</option>';
            html += '<option value="notNull"' + (comp && comp.condition === 'notNull' ? ' selected' : '') + '>Dolu (notNull)</option>';
            html += '<option value="isNull"' + (comp && comp.condition === 'isNull' ? ' selected' : '') + '>Bos (isNull)</option>';
            html += '</select></div>';
            html += '<div><label class="block text-xs font-semibold text-gray-600 mb-1">Renk</label>' + colorSelect('compColor', comp ? comp.color : 'blue') + '</div>';
            html += '<div><label class="block text-xs font-semibold text-gray-600 mb-1">Ikon</label>' + iconSelect('compIcon', comp ? comp.icon : 'fas fa-chart-bar') + '</div>';
            html += '</div>';
            html += '<div class="mt-2"><label class="block text-xs font-semibold text-gray-600 mb-1">Alt Yazi (opsiyonel)</label>';
            html += '<input type="text" id="compSubtitle" class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" value="' + esc(comp ? (comp.subtitle || '') : '') + '" placeholder="Aktif personel"></div>';

        } else if (type === 'chart') {
            html += '<div class="grid grid-cols-1 sm:grid-cols-3 gap-3 items-end">';
            html += '<div><label class="block text-xs font-semibold text-gray-600 mb-1">Grafik Tipi</label>';
            html += '<select id="compChartType" class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm">';
            ['line', 'bar', 'doughnut', 'pie'].forEach(function (ct) {
                html += '<option value="' + ct + '"' + (comp && comp.chartType === ct ? ' selected' : '') + '>' + ct.charAt(0).toUpperCase() + ct.slice(1) + '</option>';
            });
            html += '</select></div>';
            html += '<div><label class="block text-xs font-semibold text-gray-600 mb-1">Label Kolonu</label>';
            html += '<input type="text" id="compLabelCol" class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" value="' + esc(comp ? (comp.labelColumn || '') : '') + '" placeholder="Tarih"></div>';
            html += '<div></div>';
            html += '</div>';

            var datasets = comp && comp.datasets ? comp.datasets : [{ column: '', label: '', color: 'blue' }];
            html += '<div class="mt-3" id="datasetsArea">';
            html += '<label class="block text-xs font-semibold text-gray-600 mb-2">Veri Setleri</label>';
            datasets.forEach(function (ds, di) {
                html += '<div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-2 mb-2 items-end">';
                html += '<div><input type="text" class="ds-col w-full px-2 py-1.5 border border-gray-300 rounded-lg text-sm" value="' + esc(ds.column) + '" placeholder="Kolon adi"></div>';
                html += '<div><input type="text" class="ds-label w-full px-2 py-1.5 border border-gray-300 rounded-lg text-sm" value="' + esc(ds.label) + '" placeholder="Etiket"></div>';
                html += '<div>' + colorSelect('dsColor_' + di, ds.color) + '</div>';
                html += '<div><button type="button" class="text-red-500 text-sm" onclick="window._dbRemoveDs(' + di + ')">Sil</button></div>';
                html += '</div>';
            });
            html += '<button type="button" class="text-blue-600 text-sm font-semibold mt-1" onclick="window._dbAddDs()">+ Dataset Ekle</button>';
            html += '</div>';

        } else if (type === 'table') {
            html += '<div class="grid grid-cols-1 md:grid-cols-2 gap-3 items-end mb-3">';
            html += '<div class="flex items-center gap-2"><input type="checkbox" id="compClickDetail"' + (comp && comp.clickDetail ? ' checked' : '') + ' class="h-4 w-4 text-blue-600 border-gray-300 rounded">';
            html += '<label class="text-sm text-gray-700">Satir tiklaninca detay modali goster</label></div>';
            html += '</div>';

            var cols = comp && comp.columns ? comp.columns : [{ key: '', label: '', align: 'left', color: '' }];
            html += '<div id="tableColsArea">';
            html += '<label class="block text-xs font-semibold text-gray-600 mb-2">Tablo Kolonlari</label>';
            cols.forEach(function (col, ci) {
                html += '<div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-5 gap-2 mb-2 items-end">';
                html += '<div><input type="text" class="tc-key w-full px-2 py-1.5 border border-gray-300 rounded-lg text-sm" value="' + esc(col.key) + '" placeholder="Kolon adi (key)"></div>';
                html += '<div><input type="text" class="tc-label w-full px-2 py-1.5 border border-gray-300 rounded-lg text-sm" value="' + esc(col.label) + '" placeholder="Etiket"></div>';
                html += '<div><select class="tc-align w-full px-2 py-1.5 border border-gray-300 rounded-lg text-sm">';
                html += '<option value="left"' + (col.align === 'left' ? ' selected' : '') + '>Sol</option>';
                html += '<option value="right"' + (col.align === 'right' ? ' selected' : '') + '>Sag</option>';
                html += '<option value="center"' + (col.align === 'center' ? ' selected' : '') + '>Orta</option>';
                html += '</select></div>';
                html += '<div><select class="tc-color w-full px-2 py-1.5 border border-gray-300 rounded-lg text-sm">';
                html += '<option value="">Varsayilan</option>';
                colors.forEach(function (c) { html += '<option value="' + c.value + '"' + (col.color === c.value ? ' selected' : '') + '>' + c.label + '</option>'; });
                html += '</select></div>';
                html += '<div><button type="button" class="text-red-500 text-sm" onclick="window._dbRemoveCol(' + ci + ')">Sil</button></div>';
                html += '</div>';
            });
            html += '<button type="button" class="text-blue-600 text-sm font-semibold mt-1" onclick="window._dbAddCol()">+ Kolon Ekle</button>';
            html += '</div>';
        }

        html += '</div>';

        // Action button
        html += '<div class="mt-4 flex gap-3">';
        html += '<button type="button" class="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg text-sm font-semibold" onclick="window._dbSaveComp()">' + (state.editIndex >= 0 ? 'Güncelle' : 'Ekle') + '</button>';
        if (state.editIndex >= 0) {
            html += '<button type="button" class="text-gray-500 hover:text-gray-700 px-4 py-2 text-sm" onclick="window._dbCancelEdit()">İptal</button>';
        }
        html += '</div>';

        return html;
    }

    // ---- SP preview kolon datalist (autocomplete) ----
    function ensureDatalist() {
        var dl = document.getElementById('spColumnsDl');
        if (!dl) {
            dl = document.createElement('datalist');
            dl.id = 'spColumnsDl';
            document.body.appendChild(dl);
        }
        return dl;
    }

    function populateColumnDatalist(detail) {
        var dl = ensureDatalist();
        dl.innerHTML = '';
        if (!detail || !detail.resultSets) return;
        var seen = {};
        detail.resultSets.forEach(function (rs) {
            (rs.columns || []).forEach(function (name) {
                if (seen[name]) return;
                seen[name] = true;
                var opt = document.createElement('option');
                opt.value = name;
                dl.appendChild(opt);
            });
        });
        attachListAttribute();
    }

    function attachListAttribute() {
        var selectors = ['#compColumn', '#compLabelCol', '.ds-col', '.col-key'];
        selectors.forEach(function (sel) {
            document.querySelectorAll(sel).forEach(function (el) {
                if (el.tagName === 'INPUT') el.setAttribute('list', 'spColumnsDl');
            });
        });
    }

    document.addEventListener('spPreviewReady', function (ev) {
        populateColumnDatalist(ev.detail);
    });

    // F5 sonrasi __spPreview varsa hemen doldur
    if (window.__spPreview) populateColumnDatalist(window.__spPreview);

    DB.drawer = {
        renderForm: renderForm,
        attachListAttribute: attachListAttribute
    };

    // ---- Globals: form ----
    window._dbToggleLegacyRs = function () {
        var sel = document.getElementById('compResult');
        var leg = document.getElementById('compResultSet');
        if (sel && leg) leg.disabled = !!sel.value;
    };

    window._dbTypeChange = function () {
        if (state.editIndex >= 0) {
            var newType = document.getElementById('compType').value;
            var cur = state.config.tabs[state.activeTab].components[state.editIndex];
            if (cur) cur.type = newType;
        }
        DB.requestRender();
    };

    // ---- Globals: dataset add/remove ----
    window._dbAddDs = function () {
        var area = document.getElementById('datasetsArea');
        if (!area) return;
        var idx = area.querySelectorAll('.ds-col').length;
        var row = document.createElement('div');
        row.className = 'grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-2 mb-2 items-end';
        row.innerHTML = '<div><input type="text" class="ds-col w-full px-2 py-1.5 border border-gray-300 rounded-lg text-sm" placeholder="Kolon adi"></div>' +
            '<div><input type="text" class="ds-label w-full px-2 py-1.5 border border-gray-300 rounded-lg text-sm" placeholder="Etiket"></div>' +
            '<div>' + colorSelect('dsColor_' + idx, 'blue') + '</div>' +
            '<div><button type="button" class="text-red-500 text-sm" onclick="this.parentElement.parentElement.remove()">Sil</button></div>';
        area.insertBefore(row, area.lastElementChild);
        attachListAttribute();
    };

    window._dbRemoveDs = function (i) {
        var area = document.getElementById('datasetsArea');
        if (!area) return;
        var rows = area.querySelectorAll('.grid');
        if (rows[i]) rows[i].remove();
    };

    // ---- Globals: column add/remove ----
    window._dbAddCol = function () {
        var area = document.getElementById('tableColsArea');
        if (!area) return;
        var row = document.createElement('div');
        row.className = 'grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-5 gap-2 mb-2 items-end';
        row.innerHTML = '<div><input type="text" class="tc-key w-full px-2 py-1.5 border border-gray-300 rounded-lg text-sm" placeholder="Kolon adi"></div>' +
            '<div><input type="text" class="tc-label w-full px-2 py-1.5 border border-gray-300 rounded-lg text-sm" placeholder="Etiket"></div>' +
            '<div><select class="tc-align w-full px-2 py-1.5 border border-gray-300 rounded-lg text-sm"><option value="left">Sol</option><option value="right">Sag</option><option value="center">Orta</option></select></div>' +
            '<div><select class="tc-color w-full px-2 py-1.5 border border-gray-300 rounded-lg text-sm"><option value="">Varsayilan</option>' +
            colors.map(function (c) { return '<option value="' + c.value + '">' + c.label + '</option>'; }).join('') + '</select></div>' +
            '<div><button type="button" class="text-red-500 text-sm" onclick="this.parentElement.parentElement.remove()">Sil</button></div>';
        area.insertBefore(row, area.lastElementChild);
    };

    window._dbRemoveCol = function (i) {
        var area = document.getElementById('tableColsArea');
        if (!area) return;
        var rows = area.querySelectorAll('.grid');
        if (rows[i]) rows[i].remove();
    };

    // ---- Globals: save ----
    window._dbSaveComp = function () {
        var type = val('compType');
        var existingComp = state.editIndex >= 0 ? state.config.tabs[state.activeTab].components[state.editIndex] : null;
        var existingId = existingComp && existingComp.id ? existingComp.id : null;
        var resultName = val('compResult') || null;

        var comp = {
            id: existingId || genWidgetId(type),
            type: type,
            title: val('compTitle') || (type === 'kpi' ? 'KPI' : type === 'chart' ? 'Grafik' : 'Tablo'),
            span: parseInt(val('compSpan')) || 1
        };
        if (resultName) {
            comp.result = resultName;
        } else {
            comp.resultSet = parseInt(val('compResultSet')) || 0;
        }

        if (type === 'kpi') {
            comp.agg = val('compAgg') || 'count';
            comp.column = val('compColumn') || null;
            comp.condition = val('compCondition') || null;
            comp.color = val('compColor') || 'blue';
            comp.icon = val('compIcon') || 'fas fa-chart-bar';
            comp.subtitle = val('compSubtitle') || null;
        } else if (type === 'chart') {
            comp.chartType = val('compChartType') || 'line';
            comp.labelColumn = val('compLabelCol') || '';
            comp.datasets = [];
            var dsCols = document.querySelectorAll('#datasetsArea .ds-col');
            var dsLabels = document.querySelectorAll('#datasetsArea .ds-label');
            dsCols.forEach(function (el, i) {
                var colName = el.value.trim();
                if (!colName) return;
                var label = dsLabels[i] ? dsLabels[i].value.trim() : colName;
                var colorEl = document.getElementById('dsColor_' + i);
                comp.datasets.push({ column: colName, label: label, color: colorEl ? colorEl.value : 'blue' });
            });
        } else if (type === 'table') {
            comp.clickDetail = document.getElementById('compClickDetail') ? document.getElementById('compClickDetail').checked : false;
            comp.columns = [];
            var tcKeys = document.querySelectorAll('#tableColsArea .tc-key');
            var tcLabels = document.querySelectorAll('#tableColsArea .tc-label');
            var tcAligns = document.querySelectorAll('#tableColsArea .tc-align');
            var tcColors = document.querySelectorAll('#tableColsArea .tc-color');
            tcKeys.forEach(function (el, i) {
                var key = el.value.trim();
                if (!key) return;
                comp.columns.push({
                    key: key,
                    label: tcLabels[i] ? tcLabels[i].value.trim() || key : key,
                    align: tcAligns[i] ? tcAligns[i].value : 'left',
                    color: tcColors[i] ? tcColors[i].value || null : null
                });
            });
        }

        if (state.editIndex >= 0) {
            state.config.tabs[state.activeTab].components[state.editIndex] = comp;
        } else {
            state.config.tabs[state.activeTab].components.push(comp);
        }
        state.editIndex = -1;
        DB.requestRender();
    };
})();
