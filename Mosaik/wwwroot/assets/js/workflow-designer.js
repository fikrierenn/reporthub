// Plan 36 W-05 — sequential-workflow-designer (MIT) wrapper.
// CDN: https://cdn.jsdelivr.net/npm/sequential-workflow-designer@0.37.3
// Step types: approval | notify (delay engine'de var ama designer toolbox'tan gizli — kafa karıştırıcı, onay step deadline+escalation yeterli)
// Editor: type'a göre conditional alanlar, user/role select dropdown'lar
// Data source: GET /Workflow/Admin/StepDataSources → { users, roles }
(function () {
    'use strict';

    if (window.MosaikWorkflowDesigner) return;

    var DATA_SOURCE_URL = '/Workflow/Admin/StepDataSources';
    var dataSourceCache = null;

    function defaultDefinition() {
        return { properties: {}, sequence: [] };
    }

    function safeParse(json) {
        if (!json) return defaultDefinition();
        try {
            var parsed = JSON.parse(json);
            // Migrate legacy {steps: [...]} → {sequence: [...]}
            if (parsed && !parsed.sequence && Array.isArray(parsed.steps)) {
                parsed.sequence = parsed.steps.map(function (s) {
                    return {
                        id: s.id,
                        componentType: 'task',
                        type: s.type || 'approval',
                        name: s.name || s.id,
                        properties: s.properties || {}
                    };
                });
                delete parsed.steps;
            }
            if (!parsed.properties) parsed.properties = {};
            if (!Array.isArray(parsed.sequence)) parsed.sequence = [];
            return parsed;
        } catch (e) {
            return defaultDefinition();
        }
    }

    function buildApprovalStep() {
        return {
            componentType: 'task',
            type: 'approval',
            name: 'Onay Adımı',
            properties: {
                name: 'Onay',
                assigneeUserId: null,
                assigneeRole: '',
                deadlineDays: null,
                escalateTo: '',
                requireComment: false
            }
        };
    }

    function buildNotifyStep() {
        return {
            componentType: 'task',
            type: 'notify',
            name: 'Bilgilendirme',
            properties: {
                name: 'Bildirim',
                assigneeUserId: null,
                assigneeRole: ''
            }
        };
    }

    function buildDelayStep() {
        return {
            componentType: 'task',
            type: 'delay',
            name: 'Bekleme',
            properties: {
                name: 'Bekle',
                waitDays: 1
            }
        };
    }

    function fetchDataSources() {
        if (dataSourceCache) return Promise.resolve(dataSourceCache);
        return fetch(DATA_SOURCE_URL, { credentials: 'same-origin' })
            .then(function (r) { return r.ok ? r.json() : { users: [], roles: [] }; })
            .then(function (data) { dataSourceCache = data; return data; })
            .catch(function () { return { users: [], roles: [] }; });
    }

    function init(opts) {
        var canvas = document.getElementById(opts.canvasId);
        var hiddenInput = document.getElementById(opts.hiddenInputId);
        if (!canvas || !hiddenInput) {
            console.warn('MosaikWorkflowDesigner: canvas veya hidden input bulunamadı.');
            return null;
        }
        if (typeof sequentialWorkflowDesigner === 'undefined') {
            canvas.textContent = 'Designer kütüphanesi yüklenmedi. CDN ulaşımını kontrol et.';
            canvas.className = 'wf-canvas-load-error';
            return null;
        }

        // Veri kaynağını önceden çek — editor build sırasında hazır olsun.
        fetchDataSources();

        var initial = safeParse(opts.initialJson || hiddenInput.value);

        var config = {
            theme: 'light',
            isReadonly: !!opts.readonly,
            controlBar: true,
            contextMenu: true,
            toolbox: {
                isCollapsed: false,
                groups: [{
                    name: 'Adımlar',
                    steps: [buildApprovalStep(), buildNotifyStep(), buildDelayStep()]
                }]
            },
            steps: {
                iconUrlProvider: function (componentType, type) {
                    var color = type === 'notify' ? '2563eb'
                        : type === 'delay' ? 'd97706'
                        : '16a34a';
                    var letter = type === 'notify' ? 'B'
                        : type === 'delay' ? 'W'
                        : 'O';
                    var svg = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">'
                        + '<rect width="24" height="24" rx="4" fill="#' + color + '"/>'
                        + '<text x="12" y="17" font-family="Arial" font-size="14" font-weight="bold" fill="white" text-anchor="middle">' + letter + '</text>'
                        + '</svg>';
                    return 'data:image/svg+xml;utf8,' + encodeURIComponent(svg);
                }
            },
            validator: {
                step: function (step) {
                    if (!step.properties || typeof step.properties.name !== 'string') return false;
                    if (step.properties.name.trim().length === 0) return false;
                    if (step.type === 'delay') {
                        return typeof step.properties.waitDays === 'number' && step.properties.waitDays > 0;
                    }
                    return true;
                },
                root: function () { return true; }
            },
            editors: {
                isCollapsed: false,
                rootEditorProvider: function (definition, rootContext, isReadonly) {
                    var div = document.createElement('div');
                    div.className = 'wf-root-editor';
                    var p = document.createElement('p');
                    p.className = 'text-muted text-xs';
                    p.textContent = 'Sol panelden adım sürükle. Her adıma sağ panelde özellik ver. Türler: Onay (kullanıcı kararı + deadline + amire eskalasyon) ve Bilgilendirme (otomatik geç, bildirim atar).';
                    div.appendChild(p);
                    return div;
                },
                stepEditorProvider: function (step, stepContext, definition, isReadonly) {
                    return buildStepEditor(step, stepContext);
                }
            }
        };

        var designer = sequentialWorkflowDesigner.Designer.create(canvas, initial, config);

        designer.onDefinitionChanged.subscribe(function () {
            hiddenInput.value = JSON.stringify(designer.getDefinition());
        });
        hiddenInput.value = JSON.stringify(designer.getDefinition());

        return designer;
    }

    // ─── Step editor builder ──────────────────────────────────────────

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

    function buildStepEditor(step, stepContext) {
        var root = el('div', 'wf-step-editor');

        function notify() { stepContext.notifyPropertiesChanged(); }

        // Tüm step type'larda ortak: Adım Adı
        root.appendChild(field('Adım Adı *', textInput(step.properties.name, 'örn. Yönetici Onayı', function (v) {
            step.properties.name = v;
            step.name = v || 'Adım';
            stepContext.notifyNameChanged();
            notify();
        })));

        var typeLabel = step.type === 'notify' ? 'Bilgilendirme'
            : step.type === 'delay' ? 'Bekleme'
            : 'Onay';
        var typeBadge = el('div', 'wf-step-type-badge');
        typeBadge.textContent = 'Tür: ' + typeLabel;
        root.appendChild(typeBadge);

        if (step.type === 'approval' || step.type === 'notify') {
            // Atanan kullanıcı
            root.appendChild(field('Atanan Kullanıcı', userSelect(step.properties.assigneeUserId, function (v) {
                step.properties.assigneeUserId = v;
                notify();
            })));
            // Atanan rol
            root.appendChild(field('Atanan Rol', roleSelect(step.properties.assigneeRole, function (v) {
                step.properties.assigneeRole = v;
                notify();
            })));
        }

        if (step.type === 'approval') {
            root.appendChild(field('Süre (gün)', numberInput(step.properties.deadlineDays, 'gün cinsinden', function (v) {
                step.properties.deadlineDays = v;
                notify();
            })));
            root.appendChild(field('Eskalasyon Hedefi (süre+1 gün geçince)', escalateInput(step.properties.escalateTo, function (v) {
                step.properties.escalateTo = v;
                notify();
            })));
            root.appendChild(checkbox(step.properties.requireComment, 'Yorum zorunlu', function (v) {
                step.properties.requireComment = v;
                notify();
            }));
        }

        if (step.type === 'delay') {
            root.appendChild(field('Bekleme süresi (gün) *', numberInput(step.properties.waitDays, 'min 1', function (v) {
                step.properties.waitDays = v == null ? 1 : v;
                notify();
            })));
        }

        return root;
    }

    window.MosaikWorkflowDesigner = { init: init };
})();
