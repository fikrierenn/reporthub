(function () {
    "use strict";

    var configEl = document.getElementById("DashboardConfigJson");
    var builderEl = document.getElementById("dashboardBuilder");
    if (!configEl || !builderEl) return;

    // State
    var config = { schemaVersion: 1, layout: "standard", resultContract: {}, tabs: [{ title: "Genel", components: [] }] };
    var activeTab = 0;
    var editIndex = -1;
    var jsonViewOpen = false;

    // Try parse existing config
    try {
        var raw = configEl.value.trim();
        if (raw) {
            var parsed = JSON.parse(raw);
            if (parsed && parsed.tabs && parsed.tabs.length > 0) config = parsed;
        }
    } catch (e) { }

    // Forward-compat: eski config'lerde yeni alanlar olmayabilir.
    if (!config.resultContract || typeof config.resultContract !== 'object') config.resultContract = {};
    if (!config.schemaVersion) config.schemaVersion = 1;

    // Widget id format: w_<type>_<hash8>. Stabil, auto-gen on first save, immutable.
    function genWidgetId(type) {
        var hash = Math.random().toString(16).slice(2, 10);
        if (hash.length < 8) hash = (hash + "00000000").slice(0, 8);
        return "w_" + (type || "x") + "_" + hash;
    }

    // Color options
    var colors = [
        { value: "blue", label: "Mavi", hex: "#3b82f6" },
        { value: "green", label: "Yesil", hex: "#10b981" },
        { value: "red", label: "Kirmizi", hex: "#ef4444" },
        { value: "yellow", label: "Sari", hex: "#f59e0b" },
        { value: "gray", label: "Gri", hex: "#6b7280" },
        { value: "indigo", label: "Indigo", hex: "#6366f1" },
        { value: "purple", label: "Mor", hex: "#a855f7" }
    ];

    var icons = [
        "fas fa-users", "fas fa-chart-bar", "fas fa-chart-line", "fas fa-chart-pie",
        "fas fa-check-circle", "fas fa-times-circle", "fas fa-clock", "fas fa-calendar",
        "fas fa-database", "fas fa-table", "fas fa-list", "fas fa-th",
        "fas fa-user", "fas fa-store", "fas fa-building", "fas fa-cog",
        "fas fa-exclamation-triangle", "fas fa-info-circle", "fas fa-star", "fas fa-fire",
        "fas fa-arrow-up", "fas fa-arrow-down", "fas fa-percent", "fas fa-coins"
    ];

    var aggOptions = [
        { value: "count", label: "Satir Sayisi" },
        { value: "countWhere", label: "Filtreli Sayim" },
        { value: "sum", label: "Toplam" },
        { value: "avg", label: "Ortalama" },
        { value: "min", label: "Minimum" },
        { value: "max", label: "Maksimum" },
        { value: "first", label: "Ilk Deger" }
    ];

    var shapes = ["row", "table"];

    function sync() {
        configEl.value = JSON.stringify(config, null, 2);
    }

    function colorSelect(name, selected) {
        return '<select id="' + name + '" class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm">' +
            colors.map(function (c) {
                return '<option value="' + c.value + '"' + (c.value === selected ? ' selected' : '') +
                    ' style="color:' + c.hex + '">' + c.label + '</option>';
            }).join('') + '</select>';
    }

    function iconSelect(name, selected) {
        return '<select id="' + name + '" class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm">' +
            icons.map(function (ic) {
                return '<option value="' + ic + '"' + (ic === selected ? ' selected' : '') + '>' + ic.replace('fas fa-', '') + '</option>';
            }).join('') + '</select>';
    }

    function shapeSelect(selected, dataKey) {
        var html = '<select class="w-full px-2 py-1 border border-gray-300 rounded text-sm" data-rc-key="' + dataKey + '" onchange="window._dbUpdateContractField(this, \'shape\')">';
        html += '<option value=""' + (!selected ? ' selected' : '') + '>— yok —</option>';
        shapes.forEach(function (s) {
            html += '<option value="' + s + '"' + (selected === s ? ' selected' : '') + '>' + s + '</option>';
        });
        html += '</select>';
        return html;
    }

    // Admin'in isim -> resultSet map'ini burada kurar. Widget.result bu sozlugu referans eder.
    function renderContract() {
        var keys = Object.keys(config.resultContract || {});
        var html = '<div class="mb-4 border border-gray-200 rounded-lg p-3 bg-white">';
        html += '<div class="flex items-center justify-between mb-2">';
        html += '<h4 class="text-xs font-semibold text-gray-600 uppercase tracking-wide"><i class="fas fa-link mr-1"></i> Result Contract <span class="text-[10px] text-blue-600 normal-case">(isim -> result set)</span></h4>';
        html += '<button type="button" class="text-xs text-blue-600 hover:text-blue-800 font-semibold" onclick="window._dbAddContractEntry()"><i class="fas fa-plus mr-1"></i>Ekle</button>';
        html += '</div>';

        if (keys.length === 0) {
            html += '<p class="text-xs text-gray-400 italic">Henuz isim tanimi yok. Widget\'lar legacy resultSet index\'i kullaniyor. Isim tanimlarsan widget\'larda "Result" dropdown\'u dolacak.</p>';
        } else {
            html += '<div class="grid grid-cols-12 gap-2 text-[10px] font-semibold text-gray-500 uppercase mb-1 px-1">';
            html += '<div class="col-span-4">Isim (camelCase)</div>';
            html += '<div class="col-span-2">Result Set</div>';
            html += '<div class="col-span-2 text-center">Zorunlu</div>';
            html += '<div class="col-span-3">Sekil (hint)</div>';
            html += '<div class="col-span-1"></div>';
            html += '</div>';
            keys.forEach(function (k) {
                var e = config.resultContract[k] || {};
                var kAttr = esc(k);
                html += '<div class="grid grid-cols-12 gap-2 mb-2 items-center">';
                html += '<div class="col-span-4"><input type="text" class="w-full px-2 py-1 border border-gray-300 rounded text-sm font-mono" value="' + kAttr + '" data-rc-key="' + kAttr + '" onblur="window._dbRenameContractEntry(this)"></div>';
                html += '<div class="col-span-2"><input type="number" class="w-full px-2 py-1 border border-gray-300 rounded text-sm" value="' + (e.resultSet != null ? e.resultSet : 0) + '" min="0" max="20" data-rc-key="' + kAttr + '" onchange="window._dbUpdateContractField(this, \'resultSet\')"></div>';
                html += '<div class="col-span-2 text-center"><input type="checkbox"' + (e.required ? ' checked' : '') + ' class="h-4 w-4 text-blue-600 border-gray-300 rounded" data-rc-key="' + kAttr + '" onchange="window._dbUpdateContractField(this, \'required\')"></div>';
                html += '<div class="col-span-3">' + shapeSelect(e.shape, kAttr) + '</div>';
                html += '<div class="col-span-1 text-right"><button type="button" class="text-red-500 hover:text-red-700 text-sm px-2" data-rc-key="' + kAttr + '" onclick="window._dbRemoveContractEntry(this)" title="Sil">&times;</button></div>';
                html += '</div>';
            });
        }
        html += '</div>';
        return html;
    }

    function renderTabs() {
        var tabsHtml = '<div class="flex gap-1 border-b border-gray-200 mb-4">';
        config.tabs.forEach(function (tab, i) {
            var active = i === activeTab ? ' text-blue-600 border-blue-600' : ' text-gray-500 border-transparent';
            tabsHtml += '<div class="flex items-center gap-1 px-3 py-2 text-sm font-semibold border-b-2 cursor-pointer' + active + '" onclick="window._dbSetTab(' + i + ')">';
            tabsHtml += '<span>' + esc(tab.title) + '</span>';
            if (config.tabs.length > 1) {
                tabsHtml += ' <button type="button" class="text-red-400 hover:text-red-600 text-xs ml-1" onclick="event.stopPropagation(); window._dbRemoveTab(' + i + ')" title="Sekmeyi sil">&times;</button>';
            }
            tabsHtml += '</div>';
        });
        tabsHtml += '<div class="px-3 py-2 text-sm text-gray-400 cursor-pointer hover:text-blue-600" onclick="window._dbAddTab()">+ Sekme</div>';
        tabsHtml += '</div>';
        return tabsHtml;
    }

    function renderComponentList() {
        var comps = config.tabs[activeTab].components;
        if (comps.length === 0) {
            return '<div class="text-sm text-gray-400 py-6 text-center border-2 border-dashed border-gray-200 rounded-lg">Henuz bilesen eklenmedi. Asagidaki formu kullanarak bilesen ekleyin.</div>';
        }
        var html = '<div id="componentListArea" class="space-y-2">';
        comps.forEach(function (comp, i) {
            var typeLabel = comp.type === 'kpi' ? 'KPI' : comp.type === 'chart' ? 'Grafik' : 'Tablo';
            var typeBg = comp.type === 'kpi' ? 'bg-blue-100 text-blue-700' : comp.type === 'chart' ? 'bg-purple-100 text-purple-700' : 'bg-emerald-100 text-emerald-700';
            var isEditing = editIndex === i;

            html += '<div class="comp-row flex items-center justify-between px-4 py-3 rounded-lg border ' + (isEditing ? 'border-blue-400 bg-blue-50 shadow-sm' : 'border-gray-200 bg-white hover:border-gray-300 hover:shadow-sm') + ' transition-all" draggable="true" data-idx="' + i + '">';
            html += '<div class="flex items-center gap-3 flex-1 min-w-0">';
            html += '<span class="drag-handle text-gray-300 hover:text-gray-500 cursor-move" title="Suruklemek icin tut"><i class="fas fa-grip-vertical"></i></span>';
            html += '<span class="px-2 py-0.5 rounded text-xs font-bold ' + typeBg + ' whitespace-nowrap">' + typeLabel + '</span>';
            html += '<span class="text-sm font-medium text-gray-800 truncate">' + esc(comp.title) + '</span>';
            html += '<span class="text-xs text-gray-400 whitespace-nowrap">RS:' + comp.resultSet + ' &middot; ' + comp.span + '/4</span>';
            html += '</div>';
            html += '<div class="flex items-center gap-1 ml-3">';
            html += '<button type="button" class="text-blue-600 hover:bg-blue-100 px-3 py-1.5 rounded-md text-sm font-semibold transition-colors" onclick="window._dbEditComp(' + i + ')" title="Duzenle"><i class="fas fa-pen mr-1"></i>Duzenle</button>';
            html += '<button type="button" class="text-red-500 hover:bg-red-50 hover:text-red-700 px-2 py-1.5 rounded-md text-sm transition-colors" onclick="window._dbDeleteComp(' + i + ')" title="Sil"><i class="fas fa-trash"></i></button>';
            html += '</div>';
            html += '</div>';
        });
        html += '</div>';
        return html;
    }

    // F-03: Event delegation — builderEl stable, .comp-row'lar her render'da
    // yeniden olusur. Eskiden her row'a 5 closure'li listener eklenirdi ve
    // closure'lar stale DOM node'larini tutardi (GC gecikmesi). Simdi tek set
    // listener + e.target.closest ile row bulma.
    var dragDropBound = false;
    var draggedIdx = null;

    function clearDragOverMarkers() {
        var area = document.getElementById('componentListArea');
        if (!area) return;
        var rows = area.querySelectorAll('.comp-row');
        for (var i = 0; i < rows.length; i++) {
            rows[i].classList.remove('border-t-4', 'border-t-blue-500');
        }
    }

    function attachDragDrop() {
        if (dragDropBound) return;
        dragDropBound = true;

        builderEl.addEventListener('dragstart', function (e) {
            var row = e.target.closest && e.target.closest('.comp-row');
            if (!row) return;
            draggedIdx = parseInt(row.dataset.idx, 10);
            row.classList.add('opacity-40');
            if (e.dataTransfer) {
                e.dataTransfer.effectAllowed = 'move';
                // Chrome icin gerekli
                e.dataTransfer.setData('text/plain', row.dataset.idx);
            }
        });

        builderEl.addEventListener('dragend', function (e) {
            var row = e.target.closest && e.target.closest('.comp-row');
            if (row) row.classList.remove('opacity-40');
            clearDragOverMarkers();
        });

        builderEl.addEventListener('dragover', function (e) {
            var row = e.target.closest && e.target.closest('.comp-row');
            if (!row) return;
            e.preventDefault();
            if (e.dataTransfer) e.dataTransfer.dropEffect = 'move';
            clearDragOverMarkers();
            row.classList.add('border-t-4', 'border-t-blue-500');
        });

        builderEl.addEventListener('dragleave', function (e) {
            var row = e.target.closest && e.target.closest('.comp-row');
            if (row) row.classList.remove('border-t-4', 'border-t-blue-500');
        });

        builderEl.addEventListener('drop', function (e) {
            var row = e.target.closest && e.target.closest('.comp-row');
            if (!row) return;
            e.preventDefault();
            var targetIdx = parseInt(row.dataset.idx, 10);
            if (draggedIdx === null || draggedIdx === targetIdx) return;

            var comps = config.tabs[activeTab].components;
            var moved = comps.splice(draggedIdx, 1)[0];
            // Hedef index asagidaysa, splice sonrasi duzeltme
            var insertAt = draggedIdx < targetIdx ? targetIdx - 1 : targetIdx;
            comps.splice(insertAt, 0, moved);

            // editIndex'i de takip et
            if (editIndex === draggedIdx) editIndex = insertAt;
            else if (draggedIdx < editIndex && editIndex <= insertAt) editIndex--;
            else if (insertAt <= editIndex && editIndex < draggedIdx) editIndex++;

            draggedIdx = null;
            render();
        });
    }

    function renderForm() {
        var comp = editIndex >= 0 ? config.tabs[activeTab].components[editIndex] : null;
        var type = comp ? comp.type : (document.getElementById('compType') ? document.getElementById('compType').value : 'kpi');

        var contractKeys = Object.keys(config.resultContract || {});
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

        html += '<div><label class="block text-xs font-semibold text-gray-600 mb-1">Result <span class="text-[10px] text-blue-600 normal-case">(name)</span></label>';
        html += '<select id="compResult" class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" onchange="window._dbToggleLegacyRs()"' + (hasContract ? '' : ' disabled title="Once Result Contract tanimla"') + '>';
        html += '<option value=""' + (!currentResult ? ' selected' : '') + '>— legacy index —</option>';
        contractKeys.forEach(function (k) {
            html += '<option value="' + esc(k) + '"' + (currentResult === k ? ' selected' : '') + '>' + esc(k) + '</option>';
        });
        html += '</select></div>';

        // Legacy index. Result dropdown set oldugunda disabled — result > resultSet precedence.
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

        // Type-specific fields
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

            // Datasets
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

            // Columns
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
        html += '<button type="button" class="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg text-sm font-semibold" onclick="window._dbSaveComp()">' + (editIndex >= 0 ? 'Guncelle' : 'Ekle') + '</button>';
        if (editIndex >= 0) {
            html += '<button type="button" class="text-gray-500 hover:text-gray-700 px-4 py-2 text-sm" onclick="window._dbCancelEdit()">Iptal</button>';
        }
        html += '</div>';

        return html;
    }

    function render() {
        var html = '';

        // Layout selector
        html += '<div class="flex items-center gap-4 mb-4">';
        html += '<span class="text-xs font-semibold text-gray-500 uppercase">Layout:</span>';
        ['standard', 'compact', 'wide'].forEach(function (l) {
            var labels = { standard: '4 Sutun', compact: '2 Sutun', wide: 'Tam Genislik' };
            var active = config.layout === l ? ' bg-blue-100 text-blue-700 border-blue-300' : ' bg-gray-50 text-gray-600 border-gray-200';
            html += '<button type="button" class="px-3 py-1.5 text-xs font-semibold rounded-lg border' + active + '" onclick="window._dbSetLayout(\'' + l + '\')">' + labels[l] + '</button>';
        });
        html += '</div>';

        html += renderContract();

        // JSON view toggle — admin debug + copy/paste icin.
        html += '<div class="mb-3 flex justify-end">';
        html += '<button type="button" id="dbJsonToggleBtn" class="text-xs text-gray-600 hover:text-gray-800 font-semibold" onclick="window._dbToggleJsonView()">' + (jsonViewOpen ? '<i class="fas fa-eye-slash mr-1"></i>JSON Gizle' : '<i class="fas fa-code mr-1"></i>JSON Goster') + '</button>';
        html += '</div>';
        html += '<div id="dbJsonView" class="mb-4' + (jsonViewOpen ? '' : ' hidden') + '">';
        html += '<textarea readonly class="w-full h-64 px-3 py-2 border border-gray-300 rounded-lg text-xs font-mono bg-gray-50" placeholder="Config JSON"></textarea>';
        html += '<p class="text-[10px] text-gray-500 mt-1">Salt-okunur snapshot. Degisiklikler UI\'dan yapilir.</p>';
        html += '</div>';

        // Tabs (tam genislik)
        html += renderTabs();

        // 2 sutunlu grid: sol=liste, sag=form. Kucuk ekranda stack.
        html += '<div class="grid grid-cols-1 lg:grid-cols-12 gap-4">';

        // Sol sutun: bilesen listesi + ozet
        html += '<div class="lg:col-span-5">';
        html += '<div class="mb-2 flex items-center justify-between">';
        html += '<h4 class="text-xs font-semibold text-gray-600 uppercase tracking-wide"><i class="fas fa-list mr-1"></i> Bilesenler (' + config.tabs[activeTab].components.length + ')</h4>';
        if (config.tabs[activeTab].components.length > 1) {
            html += '<span class="text-xs text-gray-400"><i class="fas fa-grip-vertical mr-1"></i>surukleyerek siralayin</span>';
        }
        html += '</div>';
        html += renderComponentList();
        html += '</div>';

        // Sag sutun: form (sticky ust)
        html += '<div class="lg:col-span-7">';
        var formBg = editIndex >= 0 ? 'bg-blue-50 border-blue-300' : 'bg-gray-50 border-gray-200';
        html += '<div id="editingBanner" class="rounded-xl border p-4 transition-all lg:sticky lg:top-4 ' + formBg + '">';
        if (editIndex >= 0) {
            var editingComp = config.tabs[activeTab].components[editIndex];
            var editingTitle = editingComp && editingComp.title ? editingComp.title : ('#' + (editIndex + 1));
            html += '<div class="flex items-center justify-between mb-3">';
            html += '<h4 class="text-sm font-bold text-blue-800"><i class="fas fa-pen mr-1"></i> Duzenleniyor: ' + esc(editingTitle) + '</h4>';
            html += '<button type="button" class="text-xs text-gray-500 hover:text-gray-700 underline" onclick="window._dbCancelEdit()">Duzenlemeyi iptal et</button>';
            html += '</div>';
        } else {
            html += '<h4 class="text-sm font-bold text-gray-700 mb-3"><i class="fas fa-plus-circle mr-1 text-blue-600"></i> Yeni Bilesen Ekle</h4>';
        }
        html += renderForm();
        html += '</div>';
        html += '</div>';

        html += '</div>'; // end grid

        builderEl.innerHTML = html;
        attachDragDrop();
        sync();

        // JSON view acik ise content'i refresh et (her render sonrasi).
        if (jsonViewOpen) {
            var ta = document.querySelector('#dbJsonView textarea');
            if (ta) ta.value = JSON.stringify(config, null, 2);
        }
    }

    // Global handlers
    window._dbSetLayout = function (l) { config.layout = l; render(); };
    window._dbSetTab = function (i) { activeTab = i; editIndex = -1; render(); };

    window._dbAddTab = function () {
        var name = prompt("Sekme adi:", "Sekme " + (config.tabs.length + 1));
        if (name) { config.tabs.push({ title: name, components: [] }); activeTab = config.tabs.length - 1; editIndex = -1; render(); }
    };
    window._dbRemoveTab = function (i) {
        if (config.tabs.length <= 1) return;
        if (!confirm("'" + config.tabs[i].title + "' sekmesini silmek istediginize emin misiniz?")) return;
        config.tabs.splice(i, 1);
        if (activeTab >= config.tabs.length) activeTab = config.tabs.length - 1;
        editIndex = -1;
        render();
    };

    window._dbEditComp = function (i) {
        editIndex = i;
        render();
        // Formu gorunur yap - smooth scroll
        var formTitleEl = document.getElementById('editingBanner');
        if (formTitleEl) {
            formTitleEl.scrollIntoView({ behavior: 'smooth', block: 'start' });
            // Kisa bir flash animasyonu
            formTitleEl.classList.add('ring-2', 'ring-blue-400');
            setTimeout(function () { formTitleEl.classList.remove('ring-2', 'ring-blue-400'); }, 1500);
        }
    };
    window._dbDeleteComp = function (i) {
        config.tabs[activeTab].components.splice(i, 1);
        if (editIndex === i) editIndex = -1;
        else if (editIndex > i) editIndex--;
        render();
    };
    window._dbMoveComp = function (i, dir) {
        var comps = config.tabs[activeTab].components;
        var j = i + dir;
        if (j < 0 || j >= comps.length) return;
        var tmp = comps[i]; comps[i] = comps[j]; comps[j] = tmp;
        if (editIndex === i) editIndex = j;
        else if (editIndex === j) editIndex = i;
        render();
    };
    window._dbCancelEdit = function () { editIndex = -1; render(); };

    window._dbToggleLegacyRs = function () {
        var sel = document.getElementById('compResult');
        var leg = document.getElementById('compResultSet');
        if (sel && leg) leg.disabled = !!sel.value;
    };

    window._dbAddContractEntry = function () {
        if (!config.resultContract) config.resultContract = {};
        var n = 1;
        while (config.resultContract['result_' + n]) n++;
        config.resultContract['result_' + n] = { resultSet: 0, required: false };
        render();
    };

    window._dbRemoveContractEntry = function (btn) {
        var k = btn && btn.dataset ? btn.dataset.rcKey : null;
        if (!k || !config.resultContract || !config.resultContract[k]) return;
        var refs = [];
        config.tabs.forEach(function (tab) {
            (tab.components || []).forEach(function (c) {
                if (c.result === k) refs.push(c.title || c.type);
            });
        });
        if (refs.length > 0) {
            if (!confirm("'" + k + "' icin " + refs.length + " widget baglantisi var (" + refs.slice(0, 3).join(', ') + (refs.length > 3 ? '...' : '') + "). Silince widget'lar bagsiz kalir. Devam?")) return;
            // Unbind widget references
            config.tabs.forEach(function (tab) {
                (tab.components || []).forEach(function (c) {
                    if (c.result === k) { delete c.result; c.resultSet = 0; }
                });
            });
        }
        delete config.resultContract[k];
        render();
    };

    window._dbRenameContractEntry = function (input) {
        var oldKey = input.getAttribute('data-rc-key');
        var newKey = (input.value || '').trim();
        if (!newKey || newKey === oldKey) { input.value = oldKey; return; }
        if (!/^[a-z][a-zA-Z0-9_]*$/.test(newKey)) {
            alert('Isim camelCase olmali ve harfle baslamali. Ornek: chartData, summary, storePerformance');
            input.value = oldKey;
            return;
        }
        if (config.resultContract[newKey]) {
            alert('Bu isim zaten var: ' + newKey);
            input.value = oldKey;
            return;
        }
        config.resultContract[newKey] = config.resultContract[oldKey];
        delete config.resultContract[oldKey];
        // Rename widget.result references.
        config.tabs.forEach(function (tab) {
            (tab.components || []).forEach(function (c) {
                if (c.result === oldKey) c.result = newKey;
            });
        });
        render();
    };

    window._dbUpdateContractField = function (input, field) {
        var k = input.getAttribute('data-rc-key');
        if (!config.resultContract || !config.resultContract[k]) return;
        if (field === 'required') {
            config.resultContract[k].required = input.checked;
        } else if (field === 'resultSet') {
            config.resultContract[k].resultSet = parseInt(input.value) || 0;
        } else if (field === 'shape') {
            if (input.value) config.resultContract[k].shape = input.value;
            else delete config.resultContract[k].shape;
        }
        sync();
    };

    // JSON view toggle — render() tail jsonViewOpen state'ini okuyup panel/textarea'yi senkronlar.
    window._dbToggleJsonView = function () {
        jsonViewOpen = !jsonViewOpen;
        render();
    };
    // Tip degisikligi formu yeniden cizer ama edit modundan cikarmaz
    window._dbTypeChange = function () {
        if (editIndex >= 0) {
            // Edit modunda: mevcut comp'un tipini guncelle, digerleri korunsun
            var newType = document.getElementById('compType').value;
            var cur = config.tabs[activeTab].components[editIndex];
            if (cur) cur.type = newType;
        }
        render();
    };

    window._dbAddDs = function () {
        // Add a dataset row by manipulating DOM
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
    };
    window._dbRemoveDs = function (i) {
        var area = document.getElementById('datasetsArea');
        if (!area) return;
        var rows = area.querySelectorAll('.grid');
        if (rows[i]) rows[i].remove();
    };

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

    window._dbSaveComp = function () {
        var type = val('compType');
        var existingComp = editIndex >= 0 ? config.tabs[activeTab].components[editIndex] : null;
        var existingId = existingComp && existingComp.id ? existingComp.id : null;
        var resultName = val('compResult') || null;

        // Widget id immutable: edit path'inde korunur, yeni widget'ta gen edilir.
        var comp = {
            id: existingId || genWidgetId(type),
            type: type,
            title: val('compTitle') || (type === 'kpi' ? 'KPI' : type === 'chart' ? 'Grafik' : 'Tablo'),
            span: parseInt(val('compSpan')) || 1
        };
        // Binding: result (name) tercih edilir; yoksa legacy resultSet.
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

        if (editIndex >= 0) {
            config.tabs[activeTab].components[editIndex] = comp;
        } else {
            config.tabs[activeTab].components.push(comp);
        }
        editIndex = -1;
        render();
    };

    function val(id) {
        var el = document.getElementById(id);
        return el ? el.value.trim() : '';
    }

    function esc(s) {
        if (!s) return '';
        return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    // ---- Kolon datalist (SP onizleme sonrasi otomatik doldurulur) ----
    // admin-report-form.js SP Onizle butonu basarili bittiginde document uzerinde
    // "spPreviewReady" CustomEvent firlatir; detail: { resultSets: [{ index, columns }] }.
    // Biz tum result set'lerden kolon adi setini (distinct) toplayip <datalist>'a yaziyoruz;
    // ardindan form input'larina (compColumn, compLabelCol, ds-col, col-key) list attribute
    // bagliyoruz ki admin kolon adini autocomplete gibi secebilsin.

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

    // Sayfa yuklendiginde __spPreview zaten varsa (F5 sonrasi tekrar SP Onizle basmadan), doldur.
    if (window.__spPreview) populateColumnDatalist(window.__spPreview);

    // Her render sonrasi yeni input'lara list attribute baglamak icin MutationObserver
    // alternatifi yerine pragmatik: render()'dan sonra attach (render cagrilan her yerde).
    var originalRender = render;
    render = function () {
        originalRender.apply(this, arguments);
        attachListAttribute();
    };

    // Initial render
    render();
})();
