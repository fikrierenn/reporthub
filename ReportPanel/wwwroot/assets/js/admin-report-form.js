(() => {
    const paramListEl = document.getElementById('paramList');
    const paramSchemaEl = document.getElementById('ParamSchemaJson');
    if (!paramListEl || !paramSchemaEl) {
        return;
    }

    const params = [];
    let editIndex = -1;

    const renderParams = () => {
        paramListEl.innerHTML = '';
        params.forEach((p, index) => {
            const row = document.createElement('div');
            row.className = 'flex items-center justify-between bg-gray-50 border border-gray-200 rounded-lg px-4 py-2';

            const info = document.createElement('div');
            info.className = 'text-sm text-gray-700';

            const addText = (text, className) => {
                const span = document.createElement('span');
                span.textContent = text;
                if (className) {
                    span.className = className;
                }
                info.appendChild(span);
            };

            const addSep = () => addText(' • ', 'text-gray-400 mx-1');

            addText(p.name, 'font-semibold');
            addSep();
            addText(p.label || p.name);
            addSep();
            addText(p.type);

            if (p.placeholder) {
                addSep();
                addText(`placeholder: ${p.placeholder}`);
            }

            if (p.defaultValue) {
                addSep();
                addText(`default: ${p.defaultValue}`);
            }

            if (p.required) {
                addSep();
                addText('required', 'text-red-600 font-semibold');
            }

            const actions = document.createElement('div');
            actions.className = 'flex items-center gap-3';

            const editBtn = document.createElement('button');
            editBtn.type = 'button';
            editBtn.className = 'text-blue-600 text-sm font-semibold';
            editBtn.dataset.editIndex = String(index);
            editBtn.textContent = 'Duzenle';

            const removeBtn = document.createElement('button');
            removeBtn.type = 'button';
            removeBtn.className = 'text-red-600 text-sm font-semibold';
            removeBtn.dataset.index = String(index);
            removeBtn.textContent = 'Sil';

            actions.appendChild(editBtn);
            actions.appendChild(removeBtn);
            row.appendChild(info);
            row.appendChild(actions);
            paramListEl.appendChild(row);
        });
        const payload = {
            fields: params.map((p) => ({
                name: p.name,
                label: p.label,
                type: p.type,
                required: p.required,
                placeholder: p.placeholder,
                help: p.help,
                default: p.defaultValue
            }))
        };
        paramSchemaEl.value = JSON.stringify(payload, null, 2);
    };

    const tryParseJson = (raw) => {
        if (!raw) {
            return null;
        }
        try {
            return JSON.parse(raw);
        } catch {
            // continue
        }
        const trimmed = raw.trim();
        if (trimmed.startsWith('"') && trimmed.endsWith('"')) {
            try {
                return JSON.parse(JSON.parse(trimmed));
            } catch {
                // continue
            }
        }
        const unescaped = trimmed
            .replace(/\\"/g, '"')
            .replace(/\\n/g, '\n')
            .replace(/\\r/g, '\r')
            .replace(/\\t/g, '\t');
        try {
            return JSON.parse(unescaped);
        } catch {
            return null;
        }
    };

    const parseFromJson = () => {
        const raw = paramSchemaEl.value.trim();
        if (!raw) {
            return;
        }
        const parsed = tryParseJson(raw);
        if (!parsed) {
            return;
        }
        let fields = [];
        if (Array.isArray(parsed)) {
            fields = parsed;
        } else if (parsed && Array.isArray(parsed.fields)) {
            fields = parsed.fields;
        } else if (parsed && typeof parsed === 'object') {
            fields = Object.keys(parsed).map((key) => {
                const value = parsed[key];
                return {
                    name: key,
                    label: key,
                    type: typeof value === 'string' ? value : 'text',
                    required: false,
                    placeholder: '',
                    help: '',
                    default: ''
                };
            });
        }
        params.length = 0;
        fields.forEach((f) => {
            if (f && f.name) {
                params.push({
                    name: String(f.name),
                    label: String(f.label || f.name),
                    type: String(f.type || 'text'),
                    required: Boolean(f.required),
                    placeholder: String(f.placeholder || ''),
                    help: String(f.help || ''),
                    defaultValue: String(f.defaultValue || f.default || '')
                });
            }
        });
        renderParams();
    };

    const addParamBtn = document.getElementById('addParamBtn');
    if (addParamBtn) {
        const setAddButtonText = () => {
            addParamBtn.textContent = editIndex >= 0 ? 'Duzenle' : 'Ekle';
        };

        const resetForm = () => {
            document.getElementById('paramName').value = '';
            document.getElementById('paramLabel').value = '';
            document.getElementById('paramPlaceholder').value = '';
            document.getElementById('paramDefault').value = '';
            document.getElementById('paramHelp').value = '';
            document.getElementById('paramRequired').checked = false;
            editIndex = -1;
            setAddButtonText();
        };

        const setFormValues = (param) => {
            document.getElementById('paramName').value = param.name || '';
            document.getElementById('paramLabel').value = param.label || param.name || '';
            document.getElementById('paramPlaceholder').value = param.placeholder || '';
            document.getElementById('paramType').value = param.type || 'text';
            document.getElementById('paramDefault').value = param.defaultValue || '';
            document.getElementById('paramHelp').value = param.help || '';
            document.getElementById('paramRequired').checked = Boolean(param.required);
        };

        setAddButtonText();
        addParamBtn.addEventListener('click', () => {
            const name = document.getElementById('paramName').value.trim();
            const label = document.getElementById('paramLabel').value.trim() || name;
            const placeholder = document.getElementById('paramPlaceholder').value.trim();
            const type = document.getElementById('paramType').value;
            const defaultValue = document.getElementById('paramDefault').value.trim();
            const help = document.getElementById('paramHelp').value.trim();
            const required = document.getElementById('paramRequired').checked;

            if (!name) {
                return;
            }

            if (editIndex >= 0 && params[editIndex]) {
                params[editIndex] = { name, label, type, required, placeholder, help, defaultValue };
            } else {
                params.push({ name, label, type, required, placeholder, help, defaultValue });
            }
            resetForm();
            renderParams();
        });

        paramListEl.addEventListener('click', (event) => {
            const target = event.target;
            if (!target) {
                return;
            }
            if (target.dataset && target.dataset.editIndex) {
                const index = parseInt(target.dataset.editIndex, 10);
                if (!Number.isNaN(index) && params[index]) {
                    editIndex = index;
                    setAddButtonText();
                    setFormValues(params[index]);
                }
                return;
            }
            if (target.dataset && target.dataset.index) {
                const index = parseInt(target.dataset.index, 10);
                if (!Number.isNaN(index)) {
                    params.splice(index, 1);
                    if (editIndex === index) {
                        resetForm();
                    } else if (editIndex > index) {
                        editIndex -= 1;
                        setAddButtonText();
                    }
                    renderParams();
                }
            }
        });
    }

    const loadParamsBtn = document.getElementById('loadParamsBtn');
    if (loadParamsBtn) {
        loadParamsBtn.addEventListener('click', async () => {
            const dataSourceKey = document.querySelector('select[name="DataSourceKey"]')?.value || '';
            const procName = document.querySelector('input[name="ProcName"]')?.value || '';

            if (!dataSourceKey || !procName) {
                alert('Once veri kaynagi ve prosedur adini girin.');
                return;
            }

            const url = `/Admin/ProcParams?dataSourceKey=${encodeURIComponent(dataSourceKey)}&procName=${encodeURIComponent(procName)}`;
            const response = await fetch(url);
            if (!response.ok) {
                const text = await response.text();
                alert(`Parametreler alinamadi: ${text}`);
                return;
            }

            const data = await response.json();
            const fields = Array.isArray(data.fields) ? data.fields : [];
            params.length = 0;
            fields.forEach((f) => {
                if (f && f.name) {
                    params.push({
                        name: String(f.name),
                        label: String(f.label || f.name),
                        type: String(f.type || 'text'),
                        required: Boolean(f.required),
                        placeholder: String(f.placeholder || ''),
                        help: String(f.help || ''),
                        defaultValue: String(f.default || '')
                    });
                }
            });
            renderParams();
        });
    }

    parseFromJson();
})();

// ---- Stored Procedure listesi + onizleme (SpList + SpPreview backend endpoint'leri) ----
// F-01 fix (22 Nisan 2026): outer IIFE'nin disinda, boylece paramList/ParamSchemaJson olmayan
// sayfalarda (veya erken exit durumunda) da SP Onizle butonu click handler'i baglanir.
(function initSpHelpers() {
        var procInput = document.getElementById('ProcName');
        var dsSelect = document.querySelector('select[name="DataSourceKey"]');
        var spDatalist = document.getElementById('spList');
        var previewBtn = document.getElementById('spPreviewBtn');
        var previewPanel = document.getElementById('spPreviewPanel');
        if (!procInput || !dsSelect) return;

        // SP listesini datalist'e yukle
        function loadSpList() {
            if (!spDatalist) return;
            var dsKey = dsSelect.value;
            spDatalist.innerHTML = '';
            if (!dsKey) return;
            fetch('/Admin/SpList?dataSourceKey=' + encodeURIComponent(dsKey))
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    if (!data.procedures) return;
                    data.procedures.forEach(function (p) {
                        var opt = document.createElement('option');
                        opt.value = p.name;
                        spDatalist.appendChild(opt);
                    });
                })
                .catch(function () { /* sessiz */ });
        }

        dsSelect.addEventListener('change', loadSpList);
        loadSpList();

        // ---- F-02 override paneli: admin SP parametrelerini default yerine ozel deger verebilsin ----
        var overridePanel = document.createElement('div');
        overridePanel.id = 'spPreviewParams';
        overridePanel.className = 'mt-2 p-3 bg-gray-50 rounded-lg border border-gray-200 text-xs hidden';
        if (previewBtn) previewBtn.parentNode.insertBefore(overridePanel, previewBtn);

        function loadSpParams() {
            var dsKey = dsSelect.value;
            var proc = (procInput.value || '').trim();
            if (!dsKey || !proc) { overridePanel.classList.add('hidden'); return; }
            fetch('/Admin/ProcParams?dataSourceKey=' + encodeURIComponent(dsKey) + '&procName=' + encodeURIComponent(proc))
                .then(function (r) { return r.ok ? r.json() : null; })
                .then(function (data) {
                    if (!data || !data.fields || data.fields.length === 0) { overridePanel.classList.add('hidden'); return; }
                    renderOverridePanel(data.fields);
                })
                .catch(function () { overridePanel.classList.add('hidden'); });
        }

        function renderOverridePanel(fields) {
            overridePanel.textContent = '';
            overridePanel.classList.remove('hidden');
            var header = document.createElement('div');
            header.className = 'font-semibold text-gray-600 mb-2';
            header.textContent = 'Önizleme parametreleri (opsiyonel — boş bırakılırsa tip-bazlı default kullanılır)';
            overridePanel.appendChild(header);
            var grid = document.createElement('div');
            grid.className = 'grid grid-cols-2 md:grid-cols-3 gap-2';
            overridePanel.appendChild(grid);
            fields.forEach(function (p) {
                var wrap = document.createElement('div');
                var lbl = document.createElement('label');
                lbl.className = 'block text-gray-500 text-[11px] mb-0.5';
                lbl.textContent = '@' + p.name + ' (' + p.type + ')';
                var inp = document.createElement('input');
                inp.className = 'sp-override w-full px-2 py-1 border border-gray-300 rounded text-xs';
                inp.dataset.paramName = p.name;
                inp.dataset.paramType = p.type;
                switch (p.type) {
                    case 'date': inp.type = 'date'; break;
                    case 'number': inp.type = 'number'; break;
                    case 'decimal': inp.type = 'number'; inp.step = 'any'; break;
                    case 'checkbox': inp.type = 'checkbox'; break;
                    default: inp.type = 'text';
                }
                inp.placeholder = 'default';
                wrap.appendChild(lbl);
                wrap.appendChild(inp);
                grid.appendChild(wrap);
            });
        }

        dsSelect.addEventListener('change', loadSpParams);
        procInput.addEventListener('change', loadSpParams);
        procInput.addEventListener('blur', loadSpParams);
        loadSpParams();

        function collectOverrides() {
            var overrides = {};
            overridePanel.querySelectorAll('.sp-override').forEach(function (el) {
                var name = el.dataset.paramName;
                var type = el.dataset.paramType;
                var value = type === 'checkbox' ? (el.checked ? '1' : '') : (el.value || '');
                if (value !== '') overrides[name] = value;
            });
            return overrides;
        }

        // Preview: SP'yi calistir, sonuclari panelde goster
        if (previewBtn && previewPanel) {
            previewBtn.addEventListener('click', function () {
                var dsKey = dsSelect.value;
                var proc = (procInput.value || '').trim();
                if (!dsKey) { alert('Once veri kaynagini secin.'); return; }
                if (!proc) { alert('Stored procedure adi girin.'); procInput.focus(); return; }

                previewPanel.classList.remove('hidden');
                previewPanel.innerHTML = '<div class="bg-blue-50 border border-blue-200 rounded-lg p-3 text-sm text-blue-700"><i class="fas fa-spinner fa-spin mr-2"></i>SP calistiriliyor...</div>';

                var url = '/Admin/SpPreview?dataSourceKey=' + encodeURIComponent(dsKey) +
                          '&procName=' + encodeURIComponent(proc);
                var overrides = collectOverrides();
                if (Object.keys(overrides).length > 0) {
                    url += '&paramsJson=' + encodeURIComponent(JSON.stringify(overrides));
                }
                fetch(url)
                    .then(function (r) { return r.json(); })
                    .then(function (data) { renderPreview(data); })
                    .catch(function (err) {
                        previewPanel.innerHTML = '<div class="bg-red-50 border border-red-200 rounded-lg p-3 text-sm text-red-700">Istek basarisiz: ' + escText(String(err)) + '</div>';
                    });
            });
        }

        function renderPreview(data) {
            previewPanel.textContent = '';
            if (!data.success) {
                var err = document.createElement('div');
                err.className = 'bg-red-50 border border-red-200 rounded-lg p-3 text-sm text-red-700';
                err.innerHTML = '<i class="fas fa-exclamation-triangle mr-2"></i>';
                var errText = document.createElement('span');
                errText.textContent = data.error || 'SP onizleme basarisiz';
                err.appendChild(errText);
                previewPanel.appendChild(err);
                return;
            }
            var resultSets = data.resultSets || [];
            if (resultSets.length === 0) {
                var empty = document.createElement('div');
                empty.className = 'bg-amber-50 border border-amber-200 rounded-lg p-3 text-sm text-amber-700';
                empty.textContent = 'SP calisti ama hic result set donmedi.';
                previewPanel.appendChild(empty);
                window.__spPreview = { resultSets: [] };
                return;
            }

            // Ust ozet
            var summary = document.createElement('div');
            summary.className = 'bg-green-50 border border-green-200 rounded-lg p-3 mb-3 text-sm text-green-800 flex items-center gap-2';
            summary.innerHTML = '<i class="fas fa-check-circle"></i>';
            var summaryText = document.createElement('span');
            summaryText.textContent = resultSets.length + ' result set dondu. Toplam ' +
                resultSets.reduce(function (a, rs) { return a + (rs.rowCount || 0); }, 0) + ' satir.';
            summary.appendChild(summaryText);
            previewPanel.appendChild(summary);

            // Her result set icin expandable card
            resultSets.forEach(function (rs) {
                var card = document.createElement('div');
                card.className = 'border border-gray-200 rounded-lg mb-2 overflow-hidden bg-white';

                var header = document.createElement('div');
                header.className = 'px-3 py-2 bg-gray-50 border-b border-gray-200 flex items-center justify-between';
                var headerLeft = document.createElement('div');
                headerLeft.className = 'flex items-center gap-2 text-sm';
                var badge = document.createElement('span');
                badge.className = 'px-2 py-0.5 bg-blue-100 text-blue-700 rounded text-xs font-bold';
                badge.textContent = 'RS ' + rs.index;
                var meta = document.createElement('span');
                meta.className = 'text-gray-600';
                meta.textContent = (rs.columns || []).length + ' kolon, ' + (rs.rowCount || 0) + ' satir' + (rs.truncated ? ' (ilk ' + (rs.rows || []).length + ' satir gosteriliyor)' : '');
                headerLeft.appendChild(badge);
                headerLeft.appendChild(meta);
                header.appendChild(headerLeft);
                card.appendChild(header);

                // Kolon ozeti (dashboard builder icin auto-detect kaynak)
                var colsDiv = document.createElement('div');
                colsDiv.className = 'px-3 py-2 bg-gray-50 border-b border-gray-200 text-xs text-gray-700';
                colsDiv.innerHTML = '<span class="font-semibold">Kolonlar: </span>';
                (rs.columns || []).forEach(function (c, ci) {
                    if (ci > 0) colsDiv.appendChild(document.createTextNode(', '));
                    var chip = document.createElement('code');
                    chip.className = 'bg-white border border-gray-200 px-1.5 py-0.5 rounded text-xs';
                    chip.textContent = c.name;
                    chip.title = c.type;
                    colsDiv.appendChild(chip);
                });
                card.appendChild(colsDiv);

                // Ilk N satir
                if (rs.rows && rs.rows.length > 0) {
                    var tblWrap = document.createElement('div');
                    tblWrap.className = 'overflow-x-auto max-h-60';
                    var tbl = document.createElement('table');
                    tbl.className = 'w-full text-xs';
                    var thead = document.createElement('thead');
                    thead.className = 'bg-gray-50 sticky top-0';
                    var thRow = document.createElement('tr');
                    (rs.columns || []).forEach(function (c) {
                        var th = document.createElement('th');
                        th.className = 'px-2 py-1 text-left font-semibold text-gray-600 border-b border-gray-200';
                        th.textContent = c.name;
                        thRow.appendChild(th);
                    });
                    thead.appendChild(thRow);
                    tbl.appendChild(thead);
                    var tbody = document.createElement('tbody');
                    rs.rows.forEach(function (row) {
                        var tr = document.createElement('tr');
                        tr.className = 'border-b border-gray-100';
                        (rs.columns || []).forEach(function (c) {
                            var td = document.createElement('td');
                            td.className = 'px-2 py-1 text-gray-700';
                            var v = row[c.name];
                            td.textContent = v == null ? '—' : String(v);
                            tr.appendChild(td);
                        });
                        tbody.appendChild(tr);
                    });
                    tbl.appendChild(tbody);
                    tblWrap.appendChild(tbl);
                    card.appendChild(tblWrap);
                }

                previewPanel.appendChild(card);
            });

            // Dashboard builder'a column bilgisini aktar (global)
            window.__spPreview = {
                resultSets: resultSets.map(function (rs) {
                    return {
                        index: rs.index,
                        columns: (rs.columns || []).map(function (c) { return c.name; })
                    };
                })
            };
            // Builder'a haber ver (render tetiklesin)
            document.dispatchEvent(new CustomEvent('spPreviewReady', { detail: window.__spPreview }));
        }

        function escText(s) {
            return String(s == null ? '' : s)
                .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
        }
})();
