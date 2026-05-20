// Plan 36 W-05 — sequential-workflow-designer (MIT, vanilla) wrapper.
// CDN: https://cdn.jsdelivr.net/npm/sequential-workflow-designer@0.31.0
// Step properties: name, assigneeUserId, assigneeRole, deadlineDays, requireComment.
(function () {
    'use strict';

    if (window.MosaikWorkflowDesigner) return;

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

    function buildToolboxStep() {
        return {
            componentType: 'task',
            type: 'approval',
            name: 'Onay Adımı',
            properties: {
                name: 'Adım',
                assigneeUserId: null,
                assigneeRole: '',
                deadlineDays: null,
                escalateTo: '',
                requireComment: false
            }
        };
    }

    function init(opts) {
        var canvas = document.getElementById(opts.canvasId);
        var hiddenInput = document.getElementById(opts.hiddenInputId);
        if (!canvas || !hiddenInput) {
            console.warn('MosaikWorkflowDesigner: canvas veya hidden input bulunamadı.');
            return null;
        }
        if (typeof sequentialWorkflowDesigner === 'undefined') {
            canvas.innerHTML = '<div style="padding:24px;color:var(--danger,#e63946);">Designer kütüphanesi yüklenmedi. CDN ulaşımını kontrol et.</div>';
            return null;
        }

        var initial = safeParse(opts.initialJson || hiddenInput.value);

        var config = {
            theme: 'light',
            isReadonly: !!opts.readonly,
            controlBar: true,
            toolbox: {
                groups: [{
                    name: 'Adımlar',
                    steps: [buildToolboxStep()]
                }]
            },
            steps: {
                iconUrlProvider: function () { return null; }
            },
            validator: {
                step: function (step) {
                    return step.properties && typeof step.properties.name === 'string' && step.properties.name.trim().length > 0;
                },
                root: function () { return true; }
            },
            editors: {
                rootEditor: {
                    provide: function (definition) {
                        var div = document.createElement('div');
                        div.style.padding = '12px';
                        var p = document.createElement('p');
                        p.textContent = 'Adımları sürükle-bırak ile ekle. Her adımın özelliklerini sağda düzenle.';
                        p.style.color = 'var(--text-muted, #666)';
                        p.style.fontSize = '13px';
                        div.appendChild(p);
                        return div;
                    }
                },
                stepEditor: {
                    provide: function (step, editorContext) {
                        return buildStepEditor(step, editorContext);
                    }
                }
            }
        };

        var designer = sequentialWorkflowDesigner.Designer.create(canvas, initial, config);

        designer.onDefinitionChanged.subscribe(function () {
            hiddenInput.value = JSON.stringify(designer.getDefinition());
        });

        // İlk yüklemede de hidden input'u senkronize et.
        hiddenInput.value = JSON.stringify(designer.getDefinition());

        return designer;
    }

    // Sağ panel step editor — basit form HTML elementleri.
    function buildStepEditor(step, editorContext) {
        var root = document.createElement('div');
        root.style.padding = '14px';
        root.style.display = 'flex';
        root.style.flexDirection = 'column';
        root.style.gap = '12px';

        function addField(labelText, input) {
            var wrapper = document.createElement('div');
            wrapper.style.display = 'flex';
            wrapper.style.flexDirection = 'column';
            wrapper.style.gap = '4px';
            var label = document.createElement('label');
            label.textContent = labelText;
            label.style.fontSize = '12px';
            label.style.fontWeight = '600';
            label.style.color = 'var(--text-muted, #666)';
            wrapper.appendChild(label);
            wrapper.appendChild(input);
            root.appendChild(wrapper);
        }

        function textInput(value) {
            var i = document.createElement('input');
            i.type = 'text';
            i.value = value == null ? '' : String(value);
            i.className = 'inp';
            return i;
        }
        function numberInput(value) {
            var i = document.createElement('input');
            i.type = 'number';
            i.value = value == null ? '' : String(value);
            i.className = 'inp';
            return i;
        }
        function checkbox(value) {
            var i = document.createElement('input');
            i.type = 'checkbox';
            i.checked = !!value;
            return i;
        }

        function notify() {
            editorContext.notifyPropertiesChanged();
        }

        var nameInput = textInput(step.properties.name);
        nameInput.addEventListener('input', function () {
            step.properties.name = nameInput.value;
            step.name = nameInput.value || 'Adım';
            editorContext.notifyNameChanged();
            notify();
        });
        addField('Adım Adı *', nameInput);

        var assigneeUserIdInput = numberInput(step.properties.assigneeUserId);
        assigneeUserIdInput.placeholder = 'örn. 42';
        assigneeUserIdInput.addEventListener('input', function () {
            var v = assigneeUserIdInput.value.trim();
            step.properties.assigneeUserId = v ? parseInt(v, 10) : null;
            notify();
        });
        addField('Atanan Kullanıcı (UserId)', assigneeUserIdInput);

        var assigneeRoleInput = textInput(step.properties.assigneeRole);
        assigneeRoleInput.placeholder = 'örn. admin, mali, ik';
        assigneeRoleInput.addEventListener('input', function () {
            step.properties.assigneeRole = assigneeRoleInput.value;
            notify();
        });
        addField('Atanan Rol', assigneeRoleInput);

        var deadlineInput = numberInput(step.properties.deadlineDays);
        deadlineInput.placeholder = 'gün cinsinden';
        deadlineInput.min = '0';
        deadlineInput.addEventListener('input', function () {
            var v = deadlineInput.value.trim();
            step.properties.deadlineDays = v ? parseInt(v, 10) : null;
            notify();
        });
        addField('Süre (gün)', deadlineInput);

        var escalateInput = textInput(step.properties.escalateTo);
        escalateInput.placeholder = 'UserId (42) veya rol (admin)';
        escalateInput.addEventListener('input', function () {
            var v = escalateInput.value.trim();
            // Sayıysa int, değilse string olarak sakla
            step.properties.escalateTo = v === '' ? '' : (isNaN(v) ? v : parseInt(v, 10));
            notify();
        });
        addField('Eskalasyon Hedefi (süre+1 gün geçince)', escalateInput);

        var requireCommentRow = document.createElement('label');
        requireCommentRow.style.display = 'flex';
        requireCommentRow.style.alignItems = 'center';
        requireCommentRow.style.gap = '8px';
        requireCommentRow.style.fontSize = '13px';
        var requireCb = checkbox(step.properties.requireComment);
        requireCb.addEventListener('change', function () {
            step.properties.requireComment = requireCb.checked;
            notify();
        });
        requireCommentRow.appendChild(requireCb);
        var rcLabel = document.createElement('span');
        rcLabel.textContent = 'Yorum zorunlu';
        requireCommentRow.appendChild(rcLabel);
        root.appendChild(requireCommentRow);

        return root;
    }

    window.MosaikWorkflowDesigner = { init: init };
})();
