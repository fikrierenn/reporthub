// Plan 39 Faz C-3 — workflow-designer split (1/3): input control'ler.
// Form field utility'leri + data-source bağlı select'ler + bileşik escalate input.
// Global namespace: window.MosaikWorkflowDesignerInputs = { el, field, userSelect, ... }.
// Yükleme sırası: bu dosya → workflow-designer-editor.js → workflow-designer.js.
(function () {
    'use strict';

    if (window.MosaikWorkflowDesignerInputs) return;

    var DATA_SOURCE_URL = '/Workflow/Admin/StepDataSources';
    var dataSourceCache = null;

    function fetchDataSources() {
        if (dataSourceCache) return Promise.resolve(dataSourceCache);
        return fetch(DATA_SOURCE_URL, { credentials: 'same-origin' })
            .then(function (r) { return r.ok ? r.json() : { users: [], roles: [] }; })
            .then(function (data) { dataSourceCache = data; return data; })
            .catch(function () { return { users: [], roles: [] }; });
    }

    function el(tag, className) {
        var n = document.createElement(tag);
        if (className) n.className = className;
        return n;
    }

    function field(labelText, input) {
        var wrapper = el('div', 'wf-edit-field');
        var label = el('label', 'lab');
        label.textContent = labelText;
        wrapper.appendChild(label);
        wrapper.appendChild(input);
        return wrapper;
    }

    function userSelect(currentValue, onChange) {
        var sel = el('select', 'inp');
        var blank = el('option');
        blank.value = '';
        blank.textContent = '— seçilmedi —';
        sel.appendChild(blank);
        sel.disabled = true;

        fetchDataSources().then(function (ds) {
            (ds.users || []).forEach(function (u) {
                var o = el('option');
                o.value = String(u.id);
                o.textContent = u.label;
                sel.appendChild(o);
            });
            sel.value = currentValue == null ? '' : String(currentValue);
            sel.disabled = false;
        });

        sel.addEventListener('change', function () {
            var v = sel.value;
            onChange(v === '' ? null : parseInt(v, 10));
        });
        return sel;
    }

    function roleSelect(currentValue, onChange) {
        var sel = el('select', 'inp');
        var blank = el('option');
        blank.value = '';
        blank.textContent = '— seçilmedi —';
        sel.appendChild(blank);
        sel.disabled = true;

        fetchDataSources().then(function (ds) {
            (ds.roles || []).forEach(function (r) {
                var o = el('option');
                o.value = r.name;
                o.textContent = r.name;
                sel.appendChild(o);
            });
            sel.value = currentValue || '';
            sel.disabled = false;
        });

        sel.addEventListener('change', function () { onChange(sel.value); });
        return sel;
    }

    function numberInput(value, placeholder, onChange) {
        var i = el('input', 'inp');
        i.type = 'number';
        i.value = value == null ? '' : String(value);
        if (placeholder) i.placeholder = placeholder;
        i.min = '0';
        i.addEventListener('input', function () {
            var v = i.value.trim();
            onChange(v ? parseInt(v, 10) : null);
        });
        return i;
    }

    function textInput(value, placeholder, onChange) {
        var i = el('input', 'inp');
        i.type = 'text';
        i.value = value == null ? '' : String(value);
        if (placeholder) i.placeholder = placeholder;
        i.addEventListener('input', function () { onChange(i.value); });
        return i;
    }

    function checkbox(value, labelText, onChange) {
        var wrap = el('label', 'field-check');
        var c = el('input');
        c.type = 'checkbox';
        c.checked = !!value;
        c.addEventListener('change', function () { onChange(c.checked); });
        wrap.appendChild(c);
        var span = el('span');
        span.textContent = labelText;
        wrap.appendChild(span);
        return wrap;
    }

    // EscalateTo: user dropdown VEYA role dropdown — küçük segmented control.
    function escalateInput(currentValue, onChange) {
        var wrap = el('div', 'wf-escalate-wrap');
        var modeRow = el('div', 'wf-escalate-mode');

        var userBtn = el('button', 'btn ghost sm');
        userBtn.type = 'button';
        userBtn.textContent = 'Kullanıcı';

        var roleBtn = el('button', 'btn ghost sm');
        roleBtn.type = 'button';
        roleBtn.textContent = 'Rol';

        var clearBtn = el('button', 'btn ghost sm');
        clearBtn.type = 'button';
        clearBtn.textContent = 'Boş';

        modeRow.appendChild(userBtn);
        modeRow.appendChild(roleBtn);
        modeRow.appendChild(clearBtn);
        wrap.appendChild(modeRow);

        var slot = el('div', 'wf-escalate-slot');
        wrap.appendChild(slot);

        function setMode(mode) {
            slot.replaceChildren();
            if (mode === 'user') {
                var initial = (typeof currentValue === 'number') ? currentValue : null;
                slot.appendChild(userSelect(initial, function (v) {
                    currentValue = v;
                    onChange(v);
                }));
            } else if (mode === 'role') {
                var initial = (typeof currentValue === 'string') ? currentValue : '';
                slot.appendChild(roleSelect(initial, function (v) {
                    currentValue = v;
                    onChange(v);
                }));
            } else {
                currentValue = '';
                onChange('');
            }
        }

        userBtn.addEventListener('click', function () { setMode('user'); });
        roleBtn.addEventListener('click', function () { setMode('role'); });
        clearBtn.addEventListener('click', function () { setMode('clear'); });

        // İlk durum: mevcut değere göre mode
        if (typeof currentValue === 'number') setMode('user');
        else if (typeof currentValue === 'string' && currentValue) setMode('role');
        else setMode('clear');

        return wrap;
    }

    window.MosaikWorkflowDesignerInputs = {
        fetchDataSources: fetchDataSources,
        el: el,
        field: field,
        userSelect: userSelect,
        roleSelect: roleSelect,
        numberInput: numberInput,
        textInput: textInput,
        checkbox: checkbox,
        escalateInput: escalateInput
    };
})();
