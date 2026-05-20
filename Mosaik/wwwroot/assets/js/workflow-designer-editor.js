// Plan 39 Faz C-3 — workflow-designer split (2/3): step editor builder.
// Step tipine göre (approval/notify/delay) conditional property panel üretir.
// Bağımlılık: window.MosaikWorkflowDesignerInputs (önce yükle).
// Global namespace: window.MosaikWorkflowDesignerEditor.buildStepEditor.
(function () {
    'use strict';

    if (window.MosaikWorkflowDesignerEditor) return;

    var Inputs = window.MosaikWorkflowDesignerInputs;
    if (!Inputs) {
        console.warn('MosaikWorkflowDesignerEditor: Inputs modülü yüklenmedi. workflow-designer-inputs.js önce yüklenmeli.');
        return;
    }

    function buildStepEditor(step, stepContext) {
        var root = Inputs.el('div', 'wf-step-editor');

        function notify() { stepContext.notifyPropertiesChanged(); }

        // Tüm step type'larda ortak: Adım Adı
        root.appendChild(Inputs.field('Adım Adı *', Inputs.textInput(step.properties.name, 'örn. Yönetici Onayı', function (v) {
            step.properties.name = v;
            step.name = v || 'Adım';
            stepContext.notifyNameChanged();
            notify();
        })));

        var typeLabel = step.type === 'notify' ? 'Bilgilendirme'
            : step.type === 'delay' ? 'Bekleme'
            : 'Onay';
        var typeBadge = Inputs.el('div', 'wf-step-type-badge');
        typeBadge.textContent = 'Tür: ' + typeLabel;
        root.appendChild(typeBadge);

        if (step.type === 'approval' || step.type === 'notify') {
            // Atanan kullanıcı
            root.appendChild(Inputs.field('Atanan Kullanıcı', Inputs.userSelect(step.properties.assigneeUserId, function (v) {
                step.properties.assigneeUserId = v;
                notify();
            })));
            // Atanan rol
            root.appendChild(Inputs.field('Atanan Rol', Inputs.roleSelect(step.properties.assigneeRole, function (v) {
                step.properties.assigneeRole = v;
                notify();
            })));
        }

        if (step.type === 'approval') {
            root.appendChild(Inputs.field('Süre (gün)', Inputs.numberInput(step.properties.deadlineDays, 'gün cinsinden', function (v) {
                step.properties.deadlineDays = v;
                notify();
            })));
            root.appendChild(Inputs.field('Eskalasyon Hedefi (süre+1 gün geçince)', Inputs.escalateInput(step.properties.escalateTo, function (v) {
                step.properties.escalateTo = v;
                notify();
            })));
            root.appendChild(Inputs.checkbox(step.properties.requireComment, 'Yorum zorunlu', function (v) {
                step.properties.requireComment = v;
                notify();
            }));
        }

        if (step.type === 'delay') {
            root.appendChild(Inputs.field('Bekleme süresi (gün) *', Inputs.numberInput(step.properties.waitDays, 'min 1', function (v) {
                step.properties.waitDays = v == null ? 1 : v;
                notify();
            })));
        }

        return root;
    }

    window.MosaikWorkflowDesignerEditor = { buildStepEditor: buildStepEditor };
})();
