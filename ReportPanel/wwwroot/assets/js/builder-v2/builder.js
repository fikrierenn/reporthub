// builder-v2/builder.js — main entry. Alpine.js x-data factory + Gridstack lifecycle.
// Render/SP/Drawer logic ayrı dosyalarda mixin pattern (Object.assign).
//
// Yükleme sırası (view'da): builder-render.js → builder-sp.js → builder-drawer.js → builder.js
//
// Plan 04 (Alpine adoption) ile tutarlı: tüm state Alpine reactive store'da, DOM template binding view'da.
// Gridstack drag-resize için imperative API kullanılır (Alpine reactive ile sync edilir).

(function () {
    "use strict";

    function buildAlpineComponent() {
        return function builderV2() {
            var base = {
                // ---- Reactive state ----
                config: { schemaVersion: 2, tabs: [{ title: 'Genel', components: [] }] },
                activeTab: 0,
                selectedId: null,
                drawerTab: 'setup', // 'setup' | 'style' (Plan 04 redesign — no-selection iken tab gizli, rapor ayarları gösterilir)
                spPreview: null,
                grid: null,
                mode: 'edit', // 'edit' | 'preview' — topbar mode-seg ile $store.builderV2Mode senkron
                showAdvanced: false, // İleri ayar collapsible (Aggregation override)
                colSearchTerm: '', // Drawer Veri tab kolon arama
                iconSearchTerm: '', // Drawer Görünüm tab icon arama
                iconCustom: '', // Manuel girilen ikon (fa-XYZ doğrudan)
                paramOverrides: {}, // Param-bar kullanıcının değiştirdiği değerler ({ Tarih: "2026-04-28" })
                paramEditing: null, // Hangi chip inline-edit modunda (field.name)
                paramDirty: false, // Kullanıcı param değiştirdi ama Çalıştır'a basmadı
                dataModal: { open: false, rsIdx: null, comp: null }, // Çift-tıkla veri modal'ı
                colDragSrc: null, // Plan 05.A: tablo Setup tab kolon drag-drop kaynağı (kolon adı)

                // ---- Init ----
                init() {
                    var self = this;
                    var configEl = document.getElementById('DashboardConfigJson');
                    if (configEl) {
                        try {
                            var raw = configEl.value.trim();
                            if (raw) {
                                var parsed = JSON.parse(raw);
                                if (parsed && parsed.tabs) self.config = parsed;
                            }
                        } catch (e) { /* invalid */ }
                    }
                    self.config.tabs[self.activeTab].components.forEach(function (c) {
                        if (!c.id) c.id = self.genId(c.type || 'x');
                    });

                    // Settings (DataSource/ProcName/ParamSchema) reactive state init
                    if (self.initSettings) self.initSettings();

                    // Gridstack init — DOM hazır olduktan sonra
                    self.$nextTick(function () {
                        self.initGridstack();
                        self.mountExistingWidgets();
                        self.fetchSpPreview();

                        // Mode toggle — topbar mode-seg → $store.builderV2Mode → bu component
                        if (window.Alpine && Alpine.store && Alpine.store('builderV2Mode')) {
                            self.mode = Alpine.store('builderV2Mode').value;
                            self.applyModeChange();
                            self.$watch(function () { return Alpine.store('builderV2Mode').value; }, function (v) {
                                self.mode = v;
                                self.applyModeChange();
                            });
                        }
                    });

                    // SP preview event listener (sp-helper.js'ten gelirse)
                    document.addEventListener('spPreviewReady', function (ev) {
                        self.spPreview = ev.detail;
                        if (self.mode === 'preview') self.refreshAllWidgets();
                    });
                },

                applyModeChange() {
                    var rootEl = this.$el;
                    if (rootEl) rootEl.classList.toggle('preview-mode', this.mode === 'preview');
                    // Drag-resize preview'da kapalı, edit'te açık
                    if (this.grid) {
                        this.grid.enableMove(this.mode === 'edit');
                        this.grid.enableResize(this.mode === 'edit');
                    }
                    this.refreshAllWidgets();
                },

                // ---- Helpers ----
                genId(type) { return 'w_' + type + '_' + Math.random().toString(16).slice(2, 10); },
                esc(s) {
                    if (s == null) return '';
                    return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
                },

                // ---- Reactive getters ----
                get components() { return this.config.tabs[this.activeTab].components; },
                get selected() {
                    var id = this.selectedId;
                    return id ? this.components.find(function (c) { return c.id === id; }) : null;
                },
                get reportMeta() { return window.__reportMeta || {}; },

                // ---- Sync: config → hidden textarea (form POST için) ----
                syncConfig() {
                    var el = document.getElementById('DashboardConfigJson');
                    if (el) el.value = JSON.stringify(this.config);
                    if (window.Alpine && Alpine.store && Alpine.store('builder')) {
                        Alpine.store('builder').dirty = true;
                    }
                },

                // ---- Gridstack ----
                initGridstack() {
                    if (typeof GridStack === 'undefined') {
                        console.warn('[builder-v2] Gridstack not loaded');
                        return;
                    }
                    var el = this.$el.querySelector('.grid-stack');
                    if (!el) return;
                    var self = this;
                    this.grid = GridStack.init({
                        column: 12,
                        cellHeight: 80,
                        margin: 4,
                        float: false,
                        animate: true,
                        handle: '.w-head',
                        resizable: { handles: 'e,se,s,sw,w' }
                    }, el);
                    this.grid.on('change', function () { self.saveLayoutFromGrid(); });
                },

                mountExistingWidgets() {
                    if (!this.grid) return;
                    var self = this;
                    this.components.forEach(function (c) {
                        var w = c.w || (c.type === 'kpi' ? 3 : c.type === 'chart' ? 8 : 4);
                        var h = c.h || (c.type === 'kpi' ? 2 : 4);
                        var hasPos = typeof c.x === 'number' && typeof c.y === 'number';
                        var opts = hasPos
                            ? { w: w, h: h, x: c.x, y: c.y, content: '' }
                            : { w: w, h: h, autoPosition: true, content: '' };
                        var el = self.grid.addWidget(opts);
                        el.setAttribute('data-widget-id', c.id);
                        el.classList.add('widget');
                        var cc = el.querySelector('.grid-stack-item-content') || el;
                        cc.innerHTML = self.widgetInnerHtml(c);
                        self.attachWidgetEvents(el);
                    });
                    this.saveLayoutFromGrid();
                },

                saveLayoutFromGrid() {
                    if (!this.grid) return;
                    var nodes = this.grid.engine.nodes;
                    var self = this;
                    nodes.forEach(function (n) {
                        var id = n.el && n.el.getAttribute('data-widget-id');
                        if (!id) return;
                        var comp = self.components.find(function (c) { return c.id === id; });
                        if (comp) { comp.x = n.x; comp.y = n.y; comp.w = n.w; comp.h = n.h; }
                    });
                    this.syncConfig();
                },

                // ---- Widget add/remove/duplicate ----
                addWidget(type) {
                    if (!this.grid) return;
                    var w = type === 'kpi' ? 3 : type === 'chart' ? 8 : 4;
                    var h = type === 'kpi' ? 2 : 4;
                    var comp = {
                        id: this.genId(type),
                        type: type,
                        title: 'Yeni ' + this.typeLabel(type),
                        span: type === 'kpi' ? 1 : type === 'chart' ? 3 : 2
                    };
                    this.components.push(comp);
                    var el = this.grid.addWidget({ w: w, h: h, autoPosition: true, content: '' });
                    el.setAttribute('data-widget-id', comp.id);
                    el.classList.add('widget');
                    var cc = el.querySelector('.grid-stack-item-content') || el;
                    cc.innerHTML = this.widgetInnerHtml(comp);
                    this.attachWidgetEvents(el);
                    this.selectedId = comp.id;
                    this.refreshSelection();
                    this.syncConfig();
                },

                removeWidget(id, el) {
                    if (this.grid && el) this.grid.removeWidget(el);
                    var idx = this.components.findIndex(function (c) { return c.id === id; });
                    if (idx >= 0) this.components.splice(idx, 1);
                    if (this.selectedId === id) this.selectedId = null;
                    this.syncConfig();
                },

                duplicateWidget(id) {
                    var c = this.components.find(function (x) { return x.id === id; });
                    if (!c) return;
                    var copy = JSON.parse(JSON.stringify(c));
                    copy.id = this.genId(copy.type);
                    copy.title = c.title + ' (kopya)';
                    delete copy.x; delete copy.y;
                    this.components.push(copy);
                    if (this.grid) {
                        var el = this.grid.addWidget({ w: c.w || 3, h: c.h || 2, autoPosition: true, content: '' });
                        el.setAttribute('data-widget-id', copy.id);
                        el.classList.add('widget');
                        var cc = el.querySelector('.grid-stack-item-content') || el;
                        cc.innerHTML = this.widgetInnerHtml(copy);
                        this.attachWidgetEvents(el);
                    }
                    this.syncConfig();
                }
            };

            // Mixin compose — render + sp + drawer method'larını base'e ekle.
            // Object.assign sağdan sola override eder, ama mixin'lerde ad çakışması yok
            // (her dosya kendi sorumluluk alanı).
            return Object.assign(
                base,
                window.__builderV2.renderMixin(),
                window.__builderV2.spMixin(),
                window.__builderV2.drawerMixin(),
                window.__builderV2.settingsMixin(),
                window.__builderV2.tabsMixin(),
                window.__builderV2.previewMixin()
            );
        };
    }

    // Alpine register — hem Alpine yüklenmeden önce hem sonra çalışır
    if (window.Alpine) {
        window.Alpine.data('builderV2', buildAlpineComponent());
    } else {
        document.addEventListener('alpine:init', function () {
            window.Alpine.data('builderV2', buildAlpineComponent());
        });
    }
})();
