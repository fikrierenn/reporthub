(() => {
    const paramListEl = document.getElementById('paramList');
    const paramSchemaEl = document.getElementById('ParamSchemaJson');
    if (!paramListEl || !paramSchemaEl) {
        return;
    }

    const params = [];

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

            const addSep = () => addText('|', 'text-gray-400');

            addText(p.name, 'font-semibold');
            addSep();
            addText(p.label || p.name);
            addSep();
            addText(p.type);

            if (p.placeholder) {
                addSep();
                addText(p.placeholder);
            }

            if (p.defaultValue) {
                addSep();
                addText(p.defaultValue);
            }

            if (p.required) {
                addText('*', 'ml-2 text-red-600');
            }

            const removeBtn = document.createElement('button');
            removeBtn.type = 'button';
            removeBtn.className = 'text-red-600 text-sm font-semibold';
            removeBtn.dataset.index = String(index);
            removeBtn.textContent = 'Sil';

            row.appendChild(info);
            row.appendChild(removeBtn);
            paramListEl.appendChild(row);
        });
        paramSchemaEl.value = JSON.stringify({ fields: params }, null, 2);
    };

    const parseFromJson = () => {
        try {
            const raw = paramSchemaEl.value.trim();
            if (!raw) {
                return;
            }
            const parsed = JSON.parse(raw);
            const fields = Array.isArray(parsed.fields) ? parsed.fields : [];
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
        } catch {
            // ignore invalid json
        }
    };

    const addParamBtn = document.getElementById('addParamBtn');
    if (addParamBtn) {
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

            params.push({ name, label, type, required, placeholder, help, defaultValue });
            document.getElementById('paramName').value = '';
            document.getElementById('paramLabel').value = '';
            document.getElementById('paramPlaceholder').value = '';
            document.getElementById('paramDefault').value = '';
            document.getElementById('paramHelp').value = '';
            document.getElementById('paramRequired').checked = false;
            renderParams();
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

    paramListEl.addEventListener('click', (event) => {
        const target = event.target;
        if (target && target.dataset.index) {
            const index = parseInt(target.dataset.index, 10);
            if (!Number.isNaN(index)) {
                params.splice(index, 1);
                renderParams();
            }
        }
    });

    parseFromJson();
})();
