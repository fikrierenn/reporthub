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
