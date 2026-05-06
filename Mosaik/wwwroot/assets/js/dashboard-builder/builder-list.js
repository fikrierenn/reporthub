// dashboard-builder/builder-list.js — Tabs + Component list + Drag-drop reordering
// M-11 F-7 alt-commit 1: tab + comp list render + globals + event delegation drag-drop

(function () {
    "use strict";

    if (!window.DB) return;

    var state = DB.state;
    var esc = DB.helpers.esc;

    // ---- Tabs ----
    function renderTabs() {
        var tabsHtml = '<div class="flex gap-1 border-b border-gray-200 mb-4">';
        state.config.tabs.forEach(function (tab, i) {
            var active = i === state.activeTab ? ' text-blue-600 border-blue-600' : ' text-gray-500 border-transparent';
            tabsHtml += '<div class="flex items-center gap-1 px-3 py-2 text-sm font-semibold border-b-2 cursor-pointer' + active + '" onclick="window._dbSetTab(' + i + ')">';
            tabsHtml += '<span>' + esc(tab.title) + '</span>';
            if (state.config.tabs.length > 1) {
                tabsHtml += ' <button type="button" class="text-red-400 hover:text-red-600 text-xs ml-1" onclick="event.stopPropagation(); window._dbRemoveTab(' + i + ')" title="Sekmeyi sil">&times;</button>';
            }
            tabsHtml += '</div>';
        });
        tabsHtml += '<div class="px-3 py-2 text-sm text-gray-400 cursor-pointer hover:text-blue-600" onclick="window._dbAddTab()">+ Sekme</div>';
        tabsHtml += '</div>';
        return tabsHtml;
    }

    // ---- Component list ----
    function renderComponentList() {
        var comps = state.config.tabs[state.activeTab].components;
        if (comps.length === 0) {
            return '<div class="text-sm text-gray-400 py-6 text-center border-2 border-dashed border-gray-200 rounded-lg">Henuz bilesen eklenmedi. Asagidaki formu kullanarak bilesen ekleyin.</div>';
        }
        var html = '<div id="componentListArea" class="space-y-2">';
        comps.forEach(function (comp, i) {
            var typeLabel = comp.type === 'kpi' ? 'KPI' : comp.type === 'chart' ? 'Grafik' : 'Tablo';
            var typeBg = comp.type === 'kpi' ? 'bg-blue-100 text-blue-700' : comp.type === 'chart' ? 'bg-purple-100 text-purple-700' : 'bg-emerald-100 text-emerald-700';
            var isEditing = state.editIndex === i;

            html += '<div class="comp-row flex items-center justify-between px-4 py-3 rounded-lg border ' + (isEditing ? 'border-blue-400 bg-blue-50 shadow-sm' : 'border-gray-200 bg-white hover:border-gray-300 hover:shadow-sm') + ' transition-all" draggable="true" data-idx="' + i + '">';
            html += '<div class="flex items-center gap-3 flex-1 min-w-0">';
            html += '<span class="drag-handle text-gray-300 hover:text-gray-500 cursor-move" title="Suruklemek icin tut"><i class="fas fa-grip-vertical"></i></span>';
            html += '<span class="px-2 py-0.5 rounded text-xs font-bold ' + typeBg + ' whitespace-nowrap">' + typeLabel + '</span>';
            html += '<span class="text-sm font-medium text-gray-800 truncate">' + esc(comp.title) + '</span>';
            html += '<span class="text-xs text-gray-400 whitespace-nowrap">RS:' + comp.resultSet + ' &middot; ' + comp.span + '/4</span>';
            html += '</div>';
            html += '<div class="flex items-center gap-1 ml-3">';
            html += '<button type="button" class="text-blue-600 hover:bg-blue-100 px-3 py-1.5 rounded-md text-sm font-semibold transition-colors" onclick="window._dbEditComp(' + i + ')" title="Düzenle"><i class="fas fa-pen mr-1"></i>Düzenle</button>';
            html += '<button type="button" class="text-red-500 hover:bg-red-50 hover:text-red-700 px-2 py-1.5 rounded-md text-sm transition-colors" onclick="window._dbDeleteComp(' + i + ')" title="Sil"><i class="fas fa-trash"></i></button>';
            html += '</div>';
            html += '</div>';
        });
        html += '</div>';
        return html;
    }

    // ---- Drag-drop (event delegation, F-03 fix) ----
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

        var builderEl = DB.builderEl;

        builderEl.addEventListener('dragstart', function (e) {
            var row = e.target.closest && e.target.closest('.comp-row');
            if (!row) return;
            draggedIdx = parseInt(row.dataset.idx, 10);
            row.classList.add('opacity-40');
            if (e.dataTransfer) {
                e.dataTransfer.effectAllowed = 'move';
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

            var comps = state.config.tabs[state.activeTab].components;
            var moved = comps.splice(draggedIdx, 1)[0];
            var insertAt = draggedIdx < targetIdx ? targetIdx - 1 : targetIdx;
            comps.splice(insertAt, 0, moved);

            if (state.editIndex === draggedIdx) state.editIndex = insertAt;
            else if (draggedIdx < state.editIndex && state.editIndex <= insertAt) state.editIndex--;
            else if (insertAt <= state.editIndex && state.editIndex < draggedIdx) state.editIndex++;

            draggedIdx = null;
            DB.requestRender();
        });
    }

    DB.list = {
        renderTabs: renderTabs,
        renderComponentList: renderComponentList,
        attachDragDrop: attachDragDrop
    };

    // ---- Globals: tabs ----
    window._dbSetTab = function (i) { state.activeTab = i; state.editIndex = -1; DB.requestRender(); };

    window._dbAddTab = function () {
        var name = prompt("Sekme adi:", "Sekme " + (state.config.tabs.length + 1));
        if (name) {
            state.config.tabs.push({ title: name, components: [] });
            state.activeTab = state.config.tabs.length - 1;
            state.editIndex = -1;
            DB.requestRender();
        }
    };

    window._dbRemoveTab = function (i) {
        if (state.config.tabs.length <= 1) return;
        if (!confirm("'" + state.config.tabs[i].title + "' sekmesini silmek istediginize emin misiniz?")) return;
        state.config.tabs.splice(i, 1);
        if (state.activeTab >= state.config.tabs.length) state.activeTab = state.config.tabs.length - 1;
        state.editIndex = -1;
        DB.requestRender();
    };

    // ---- Globals: comp list ----
    window._dbEditComp = function (i) {
        state.editIndex = i;
        DB.requestRender();
        var formTitleEl = document.getElementById('editingBanner');
        if (formTitleEl) {
            formTitleEl.scrollIntoView({ behavior: 'smooth', block: 'start' });
            formTitleEl.classList.add('ring-2', 'ring-blue-400');
            setTimeout(function () { formTitleEl.classList.remove('ring-2', 'ring-blue-400'); }, 1500);
        }
    };

    window._dbDeleteComp = function (i) {
        state.config.tabs[state.activeTab].components.splice(i, 1);
        if (state.editIndex === i) state.editIndex = -1;
        else if (state.editIndex > i) state.editIndex--;
        DB.requestRender();
    };

    window._dbMoveComp = function (i, dir) {
        var comps = state.config.tabs[state.activeTab].components;
        var j = i + dir;
        if (j < 0 || j >= comps.length) return;
        var tmp = comps[i]; comps[i] = comps[j]; comps[j] = tmp;
        if (state.editIndex === i) state.editIndex = j;
        else if (state.editIndex === j) state.editIndex = i;
        DB.requestRender();
    };

    window._dbCancelEdit = function () { state.editIndex = -1; DB.requestRender(); };
})();
