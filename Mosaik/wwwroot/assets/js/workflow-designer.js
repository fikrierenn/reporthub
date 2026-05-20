// Plan 36 W-05 — sequential-workflow-designer (MIT) wrapper.
// CDN: https://cdn.jsdelivr.net/npm/sequential-workflow-designer@0.37.3
// Step types: approval | notify (delay engine'de var ama designer toolbox'tan gizli — kafa karıştırıcı, onay step deadline+escalation yeterli)
// Editor: type'a göre conditional alanlar, user/role select dropdown'lar
// Data source: GET /Workflow/Admin/StepDataSources → { users, roles }
//
// Plan 39 Faz C-3 — partial split:
//   workflow-designer-inputs.js → Input control utility'leri + data-source select'ler.
//   workflow-designer-editor.js → Step property panel builder.
//   workflow-designer.js (bu)   → Designer entry: step templates + safeParse + init.
// Yükleme sırası: inputs → editor → designer.
(function () {
    'use strict';

    if (window.MosaikWorkflowDesigner) return;

    if (!window.MosaikWorkflowDesignerEditor || !window.MosaikWorkflowDesignerInputs) {
        console.warn('MosaikWorkflowDesigner: inputs/editor modülleri eksik. View script sırasını kontrol et.');
        return;
    }
    var Editor = window.MosaikWorkflowDesignerEditor;

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
        window.MosaikWorkflowDesignerInputs.fetchDataSources();

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
                    return Editor.buildStepEditor(step, stepContext);
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

    window.MosaikWorkflowDesigner = { init: init };
})();
