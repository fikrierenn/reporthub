// dashboard-builder/builder-contract.js — ResultContract editor
// M-11 F-7 alt-commit 1: contract render + globals (camelCase rename, validation, widget unbinding)

(function () {
    "use strict";

    if (!window.DB) return;

    var state = DB.state;
    var esc = DB.helpers.esc;
    var sync = DB.helpers.sync;
    var shapeSelect = DB.helpers.shapeSelect;

    // Admin'in isim -> resultSet map'ini burada kurar. Widget.result bu sozlugu referans eder.
    function renderContract() {
        var keys = Object.keys(state.config.resultContract || {});
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
                var e = state.config.resultContract[k] || {};
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

    DB.contract = { renderContract: renderContract };

    // ---- Globals ----
    window._dbAddContractEntry = function () {
        if (!state.config.resultContract) state.config.resultContract = {};
        var n = 1;
        while (state.config.resultContract['result_' + n]) n++;
        state.config.resultContract['result_' + n] = { resultSet: 0, required: false };
        DB.requestRender();
    };

    window._dbRemoveContractEntry = function (btn) {
        var k = btn && btn.dataset ? btn.dataset.rcKey : null;
        if (!k || !state.config.resultContract || !state.config.resultContract[k]) return;
        var refs = [];
        state.config.tabs.forEach(function (tab) {
            (tab.components || []).forEach(function (c) {
                if (c.result === k) refs.push(c.title || c.type);
            });
        });
        if (refs.length > 0) {
            if (!confirm("'" + k + "' icin " + refs.length + " widget baglantisi var (" + refs.slice(0, 3).join(', ') + (refs.length > 3 ? '...' : '') + "). Silince widget'lar bagsiz kalir. Devam?")) return;
            state.config.tabs.forEach(function (tab) {
                (tab.components || []).forEach(function (c) {
                    if (c.result === k) { delete c.result; c.resultSet = 0; }
                });
            });
        }
        delete state.config.resultContract[k];
        DB.requestRender();
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
        if (state.config.resultContract[newKey]) {
            alert('Bu isim zaten var: ' + newKey);
            input.value = oldKey;
            return;
        }
        state.config.resultContract[newKey] = state.config.resultContract[oldKey];
        delete state.config.resultContract[oldKey];
        state.config.tabs.forEach(function (tab) {
            (tab.components || []).forEach(function (c) {
                if (c.result === oldKey) c.result = newKey;
            });
        });
        DB.requestRender();
    };

    window._dbUpdateContractField = function (input, field) {
        var k = input.getAttribute('data-rc-key');
        if (!state.config.resultContract || !state.config.resultContract[k]) return;
        if (field === 'required') {
            state.config.resultContract[k].required = input.checked;
        } else if (field === 'resultSet') {
            state.config.resultContract[k].resultSet = parseInt(input.value) || 0;
        } else if (field === 'shape') {
            if (input.value) state.config.resultContract[k].shape = input.value;
            else delete state.config.resultContract[k].shape;
        }
        sync();
    };
})();
