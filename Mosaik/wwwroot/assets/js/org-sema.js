/* Organizasyon Şeması — bkm index.html chart motorunun Mosaik'e taşınmış hâli (Plan 55 Faz 5).
   Salt görüntüleme: pan/zoom/arama/dal-izole + görev tanımı detay modalı (tanım + KPI).
   Offline-özel özellikler (isim düzenle, not, e-posta, drag değişiklik) TAŞINMADI — ayrı iş.
   Veri = <script id="sema-data"> (OrgPositions omurgası + GorevVersions def). XSS: kullanıcı metni textContent. */
(function () {
  "use strict";
  var dataEl = document.getElementById("sema-data");
  var vp = document.getElementById("semaViewport");
  if (!dataEl || !vp) return;
  var DATA;
  try { DATA = JSON.parse(dataEl.textContent); } catch (e) { return; }
  DATA.org = DATA.org || [];
  DATA.advisors = DATA.advisors || [];

  var ROLE_LABEL = { ust: "Üst yönetim", mudur: "Müdür", lider: "Takım lideri", uzman: "Uzman", danisman: "Danışman", diger: "Diğer" };
  var ROLE_VAR = { ust: "--r-ust", mudur: "--r-mudur", lider: "--r-lider", uzman: "--r-uzman", danisman: "--r-danisman", diger: "--r-diger" };
  var NW = 206, NH = 60, ADV_GAP = 56;
  var ORI = "h";
  function GX() { return ORI === "v" ? 16 : 46; }
  function GY() { return ORI === "v" ? 52 : 12; }

  function el(t, c, x) { var e = document.createElement(t); if (c) e.className = c; if (x != null) e.textContent = x; return e; }
  function svg(d) { return '<svg class="ico" viewBox="0 0 24 24" aria-hidden="true">' + d + "</svg>"; }

  // düz index + ebeveyn
  var NODES = [];
  function indexNode(n, parent) { n._parent = parent; NODES.push(n); (n.children || []).forEach(function (c) { indexNode(c, n); }); }
  DATA.org.forEach(function (n) { indexNode(n, null); });
  DATA.advisors.forEach(function (n) { indexNode(n, null); });
  function pathOf(n) { var a = [], p = n; while (p) { a.unshift(p.position); p = p._parent; } return a; }
  function depthOf(n) { var d = 0, p = n._parent; while (p) { d++; p = p._parent; } return d; }

  var collapsed = {};
  NODES.forEach(function (n) { if (n.children && n.children.length && depthOf(n) >= 3) collapsed[n.id] = true; });

  var VIS = [], selId = null, query = "", focusRoot = null;
  function layout() {
    VIS = []; var cursor = 0;
    function walk(n, depth) {
      VIS.push(n); n._depth = depth;
      var kids = collapsed[n.id] ? [] : (n.children || []);
      if (ORI === "v") {
        if (!kids.length) { n._x = cursor; cursor += NW + GX(); }
        else { kids.forEach(function (k) { walk(k, depth + 1); }); n._x = (kids[0]._x + kids[kids.length - 1]._x) / 2; }
        n._y = depth * (NH + GY() * 2.6);
      } else {
        if (!kids.length) { n._y = cursor; cursor += NH + GY(); }
        else { kids.forEach(function (k) { walk(k, depth + 1); }); n._y = (kids[0]._y + kids[kids.length - 1]._y) / 2; }
        n._x = depth * (NW + GX());
      }
    }
    var roots = focusRoot ? [focusRoot] : DATA.org;
    roots.forEach(function (r) { walk(r, 0); });
    if (!focusRoot && DATA.advisors.length) {
      cursor += ADV_GAP;
      DATA.advisors.forEach(function (r) {
        VIS.push(r); r._depth = 0;
        if (ORI === "v") { r._x = cursor; cursor += NW + GX(); r._y = 0; }
        else { r._y = cursor; cursor += NH + GY(); r._x = 0; }
      });
    }
  }
  function matchNode(n) { return query && (n.position + " " + (n.name || "")).toLocaleLowerCase("tr").indexOf(query) >= 0; }

  var stage = document.getElementById("semaStage");
  var links = document.getElementById("semaLinks");
  var layer = document.getElementById("semaNodes");

  function render() {
    layout();
    var maxX = 0, maxY = 0;
    VIS.forEach(function (n) { maxX = Math.max(maxX, n._x + NW); maxY = Math.max(maxY, n._y + NH); });
    stage.style.width = (maxX + 40) + "px"; stage.style.height = (maxY + 40) + "px";
    links.setAttribute("width", maxX + 40); links.setAttribute("height", maxY + 40);
    var d = "";
    VIS.forEach(function (n) {
      var kids = collapsed[n.id] ? [] : (n.children || []);
      if (!kids.length) return;
      if (ORI === "v") {
        var px = n._x + NW / 2, py = n._y + NH;
        kids.forEach(function (k) { var cx = k._x + NW / 2, cy = k._y, my = py + (cy - py) / 2; d += "M" + px + " " + py + "V" + my + "H" + cx + "V" + cy + " "; });
      } else {
        var px2 = n._x + NW, py2 = n._y + NH / 2;
        kids.forEach(function (k) { var cx = k._x, cy = k._y + NH / 2, mx = px2 + (cx - px2) / 2; d += "M" + px2 + " " + py2 + "H" + mx + "V" + cy + "H" + cx + " "; });
      }
    });
    links.innerHTML = '<path d="' + d + '" fill="none" stroke="var(--sema-line-strong)" stroke-width="1.5"/>';
    layer.innerHTML = "";
    VIS.forEach(function (n) { layer.appendChild(nodeEl(n)); });
  }

  function nodeEl(n) {
    var el2 = el("div", "node");
    el2.style.left = n._x + "px"; el2.style.top = n._y + "px"; el2.style.width = NW + "px"; el2.style.height = NH + "px";
    el2.style.borderLeftColor = "var(" + (ROLE_VAR[n.role] || "--r-diger") + ")";
    el2.title = n.position + (n.name ? "  ·  " + n.name : "");
    if (selId === n.id) el2.classList.add("sel");
    if (query) { if (matchNode(n)) el2.classList.add("match"); else el2.classList.add("dim"); }
    el2.appendChild(el("div", "npos", n.position));
    var hasName = (n.name || "").trim();
    el2.appendChild(el("div", "nname" + (hasName ? "" : " empty"), hasName || "— isim yok —"));
    if (n.directToManager) el2.appendChild(el("span", "nchip", "müdüre doğrudan"));
    var kids = n.children || [];
    if (kids.length) {
      var t = el("button", "ntog");
      t.textContent = collapsed[n.id] ? ("+" + kids.length) : "−";
      t.title = "Aç / kapat";
      if (ORI === "h") { t.style.left = "auto"; t.style.right = "-11px"; t.style.top = "50%"; t.style.bottom = "auto"; t.style.transform = "translateY(-50%)"; }
      t.addEventListener("click", function (e) { e.stopPropagation(); toggle(n); });
      el2.appendChild(t);
    }
    el2.addEventListener("click", function () { selectNode(n); });
    el2.addEventListener("contextmenu", function (e) { e.preventDefault(); showCtx(e, n); });
    n._node = el2;
    return el2;
  }
  function toggle(n) { if (collapsed[n.id]) delete collapsed[n.id]; else collapsed[n.id] = true; render(); }

  // pan / zoom
  var scale = 1, tx = 40, ty = 30, panning = false, psx, psy;
  function applyT() { stage.style.transform = "translate(" + tx + "px," + ty + "px) scale(" + scale + ")"; }
  vp.addEventListener("pointerdown", function (e) { if (e.target.closest(".node") || e.target.closest(".zoombar")) return; panning = true; psx = e.clientX - tx; psy = e.clientY - ty; vp.classList.add("grab"); vp.setPointerCapture(e.pointerId); });
  vp.addEventListener("pointermove", function (e) { if (!panning) return; tx = e.clientX - psx; ty = e.clientY - psy; applyT(); });
  vp.addEventListener("pointerup", function () { panning = false; vp.classList.remove("grab"); });
  vp.addEventListener("wheel", function (e) { e.preventDefault(); var f = e.deltaY < 0 ? 1.12 : 0.89; var r = vp.getBoundingClientRect(); var mx = e.clientX - r.left, my = e.clientY - r.top; var ns = Math.min(2, Math.max(0.28, scale * f)); tx = mx - (mx - tx) * (ns / scale); ty = my - (my - ty) * (ns / scale); scale = ns; applyT(); }, { passive: false });
  document.getElementById("semaZin").addEventListener("click", function () { scale = Math.min(2, scale * 1.15); applyT(); });
  document.getElementById("semaZout").addEventListener("click", function () { scale = Math.max(0.28, scale / 1.15); applyT(); });
  function fit() {
    var r = vp.getBoundingClientRect(); var maxX = 0, maxY = 0;
    VIS.forEach(function (n) { maxX = Math.max(maxX, n._x + NW); maxY = Math.max(maxY, n._y + NH); });
    if (!maxX || !maxY) return;
    var s = Math.min((r.width - 60) / maxX, (r.height - 60) / maxY, 1.1);
    scale = Math.max(0.28, s); tx = (r.width - maxX * scale) / 2; if (tx < 20) tx = 20; ty = 24; applyT();
  }
  document.getElementById("semaFit").addEventListener("click", fit);

  // yönelim + arama
  function setOri(o) { ORI = o; document.getElementById("semaOriH").classList.toggle("on", o === "h"); document.getElementById("semaOriV").classList.toggle("on", o === "v"); render(); fit(); }
  document.getElementById("semaOriH").addEventListener("click", function () { setOri("h"); });
  document.getElementById("semaOriV").addEventListener("click", function () { setOri("v"); });
  var searchBox = document.getElementById("semaSearch");
  searchBox.addEventListener("input", function () { query = searchBox.value.trim().toLocaleLowerCase("tr"); render(); });

  // odak
  var unfocusBtn = document.getElementById("semaUnfocus");
  function focusBranch(n) { focusRoot = n; (function od(x) { delete collapsed[x.id]; (x.children || []).forEach(od); })(n); closeDetail(); unfocusBtn.classList.remove("is-hidden"); render(); fit(); }
  function clearFocus() { focusRoot = null; unfocusBtn.classList.add("is-hidden"); render(); fit(); }
  unfocusBtn.addEventListener("click", clearFocus);

  // detay modal
  var dov = document.getElementById("semaDov");
  function closeDetail() { dov.classList.remove("open"); }
  document.getElementById("semaDClose").addEventListener("click", closeDetail);
  dov.addEventListener("click", function (e) { if (e.target === dov) closeDetail(); });
  document.addEventListener("keydown", function (e) { if (e.key === "Escape") closeDetail(); });

  function selectNode(n) {
    selId = n.id;
    var prev = layer.querySelector(".node.sel"); if (prev) prev.classList.remove("sel");
    if (n._node) n._node.classList.add("sel");
    renderDetail(n); dov.classList.add("open");
  }

  function renderDetail(n) {
    var rv = ROLE_VAR[n.role] || "--r-diger";
    var b = document.getElementById("semaDBadge");
    b.textContent = ROLE_LABEL[n.role] || "Diğer";
    b.style.color = "var(" + rv + ")"; b.style.border = "1px solid var(" + rv + ")";
    var c = document.getElementById("semaDContent"); c.innerHTML = "";
    var crumb = el("div", "crumb"); crumb.textContent = pathOf(n).join("  ›  "); c.appendChild(crumb);
    c.appendChild(el("div", "h-title", n.position));
    var hn = el("div", "h-name");
    hn.innerHTML = svg('<circle cx="12" cy="8" r="4"/><path d="M4 21c0-4 4-6 8-6s8 2 8 6"/>');
    hn.appendChild(el("span", null, (n.name || "").trim() || "— isim yok —"));
    c.appendChild(hn);

    // şema aksiyonları (salt gezinme)
    var oa = el("div", "editrow");
    if ((n.children || []).length) {
      var cb = el("button", null);
      cb.innerHTML = svg('<path d="M7 13l5-5 5 5"/>');
      cb.appendChild(el("span", null, collapsed[n.id] ? ("Altını aç (" + n.children.length + ")") : "Altını topla"));
      cb.addEventListener("click", function () { toggle(n); renderDetail(n); });
      oa.appendChild(cb);
    }
    var fb = el("button", null);
    fb.innerHTML = svg('<circle cx="12" cy="12" r="3"/><path d="M12 3v2M12 19v2M3 12h2M19 12h2"/>');
    fb.appendChild(el("span", null, "Sadece bu dal"));
    fb.addEventListener("click", function () { focusBranch(n); });
    oa.appendChild(fb);
    c.appendChild(oa);

    // tanım
    var def = n.def || {};
    var paras = def.paragraphs || [];
    if (paras.length) {
      c.appendChild(el("div", "callout", paras[0]));
      if (n.directToManager) {
        var w = el("div", "warn");
        w.innerHTML = svg('<path d="M12 9v4M12 17h.01M10.3 3.9L2.4 18a2 2 0 001.7 3h15.8a2 2 0 001.7-3L13.7 3.9a2 2 0 00-3.4 0z"/>');
        w.appendChild(el("span", null, "Bir takım liderine değil, doğrudan müdüre bağlı."));
        c.appendChild(w);
      }
      if (paras.length > 1) {
        var h = el("h2", "sec");
        h.innerHTML = svg('<path d="M9 6h11M9 12h11M9 18h11M4 6h.01M4 12h.01M4 18h.01"/>');
        h.appendChild(el("span", null, "Görev, sorumluluk ve yetki"));
        c.appendChild(h);
        paras.slice(1).forEach(function (t) { c.appendChild(el("p", null, t)); });
      }
    }
    // KPI (salt-okunur: aktif/öneri ContentJson'dan)
    var kpi = def.kpi || [];
    if (kpi.length) {
      var hk = el("h2", "sec");
      hk.innerHTML = svg('<path d="M3 3v18h18"/><path d="M7 15l3-4 3 3 4-6"/>');
      hk.appendChild(el("span", null, "Başarı göstergeleri (KPI)"));
      c.appendChild(hk);
      c.appendChild(el("div", "kpisub", "Tik = aktif (ölçülecek) · boş = öneri (yapılabilir, ölçülemeyebilir)"));
      var ul = el("ul", "kpi");
      kpi.forEach(function (k) {
        var text = typeof k === "string" ? k : (k.text || "");
        var aktif = typeof k === "object" && !!k.active;
        var li = el("li", aktif ? "aktif" : "oneri");
        li.innerHTML = svg('<path d="M20 6L9 17l-5-5"/>');
        li.appendChild(el("span", "kt", text));
        li.appendChild(el("span", "kpill", aktif ? "aktif" : "öneri"));
        ul.appendChild(li);
      });
      c.appendChild(ul);
    }
  }

  // ---- yardımcılar: admin, token, POST, toast ----
  var wrap = document.querySelector(".sema-wrap");
  var isAdmin = wrap && wrap.getAttribute("data-admin") === "1";
  function token() { var i = document.querySelector('input[name="__RequestVerificationToken"]'); return i ? i.value : ""; }
  function opInt(n) { return parseInt(String(n.id).replace(/^op/, ""), 10); }
  function toast(m) {
    var t = document.getElementById("semaToast");
    if (!t) { t = el("div", "sema-toast"); t.id = "semaToast"; document.body.appendChild(t); }
    t.textContent = m; t.classList.add("show"); clearTimeout(t._t);
    t._t = setTimeout(function () { t.classList.remove("show"); }, 2200);
  }
  function readResp(r) {
    // 302→/Login (oturum düştü) redirect:manual ile opaqueredirect olur — "ağ hatası" ile karıştırma.
    if (r.type === "opaqueredirect" || r.status === 401 || r.status === 403)
      return { ok: false, error: "Oturumunuz sonlanmış olabilir. Sayfayı yenileyip tekrar deneyin." };
    return r.json().catch(function () { return { ok: false, error: "Beklenmeyen sunucu yanıtı." }; });
  }
  function postForm(action, data) {
    var body = new URLSearchParams(data); body.append("__RequestVerificationToken", token());
    return fetch("/GorevTanimlari/Sema/" + action, { method: "POST", credentials: "include", redirect: "manual", headers: { "Content-Type": "application/x-www-form-urlencoded", "RequestVerificationToken": token() }, body: body })
      .then(readResp).catch(function () { return { ok: false, error: "Sunucuya ulaşılamadı." }; });
  }
  function postJson(action, obj) {
    return fetch("/GorevTanimlari/Sema/" + action, { method: "POST", credentials: "include", redirect: "manual", headers: { "Content-Type": "application/json", "RequestVerificationToken": token() }, body: JSON.stringify(obj) })
      .then(readResp).catch(function () { return { ok: false, error: "Sunucuya ulaşılamadı." }; });
  }
  function afterEdit(res) {
    if (res && res.ok) { toast("Kaydedildi"); setTimeout(function () { window.location.reload(); }, 400); }
    else { toast((res && res.error) || "İşlem başarısız."); }
  }

  // ---- görev tanımı indir (.md) — düğüm + alt ağaç ----
  function subtreeMd(n, depth) {
    depth = depth || 0;
    var h = new Array(Math.min(depth + 1, 6) + 1).join("#");
    var out = [h + " " + n.position + (n.name ? " — " + n.name : "")];
    var def = n.def || {};
    (def.paragraphs || []).forEach(function (p) { out.push("", p); });
    var kpi = def.kpi || [];
    if (kpi.length) { out.push("", "**KPI:**"); kpi.forEach(function (k) { out.push("- " + (typeof k === "string" ? k : k.text)); }); }
    var s = out.join("\n");
    (n.children || []).forEach(function (c) { s += "\n\n" + subtreeMd(c, depth + 1); });
    return s;
  }
  function download(name, text) {
    var b = new Blob([text], { type: "text/markdown;charset=utf-8" });
    var a = document.createElement("a"); a.href = URL.createObjectURL(b); a.download = name; a.click();
    setTimeout(function () { URL.revokeObjectURL(a.href); }, 1000);
  }
  function downloadMd(n) {
    var fn = "gorev-" + (n.position || "tanim").replace(/[^0-9A-Za-zğüşıöçİĞÜŞÖÇ]+/g, "-").slice(0, 40) + ".md";
    download(fn, subtreeMd(n, 0));
  }

  // ---- sağ-tık context menü ----
  var ctx = el("div", "sema-ctx"); document.body.appendChild(ctx);
  function ctxItem(label, ico, fn, cls) {
    var b = el("button", cls || null);
    if (ico) b.innerHTML = svg(ico);
    b.appendChild(el("span", null, label));
    b.addEventListener("click", function () { ctx.classList.remove("open"); fn(); });
    return b;
  }
  function showCtx(e, n) {
    ctx.innerHTML = "";
    ctx.appendChild(ctxItem("Detayı aç", '<path d="M9 6h11M9 12h11M9 18h11M4 6h.01M4 12h.01M4 18h.01"/>', function () { selectNode(n); }));
    ctx.appendChild(ctxItem("Sadece bu dal", '<circle cx="12" cy="12" r="3"/><path d="M12 5v2M12 17v2M5 12h2M17 12h2"/>', function () { focusBranch(n); }));
    ctx.appendChild(ctxItem("Görev tanımını indir (.md)", '<path d="M12 3v12M7 10l5 5 5-5M5 21h14"/>', function () { downloadMd(n); }));
    if (isAdmin) {
      ctx.appendChild(el("div", "sep", "Düzenle"));
      ctx.appendChild(ctxItem("İsim / ünvan düzenle", '<path d="M12 20h9M16.5 3.5a2.1 2.1 0 013 3L7 19l-4 1 1-4z"/>', function () { editRename(n); }));
      ctx.appendChild(ctxItem("Alt pozisyon ekle", '<path d="M12 5v14M5 12h14"/>', function () { editAdd(n); }));
      ctx.appendChild(ctxItem("Görev tanımı + KPI düzenle", '<path d="M4 6h16M4 12h16M4 18h10"/>', function () { editDefinition(n); }));
      ctx.appendChild(ctxItem("Taşı (üst değiştir)", '<path d="M8 6l-4 4 4 4M4 10h12a4 4 0 014 4v2"/>', function () { editMove(n); }));
      ctx.appendChild(ctxItem("Sil", '<path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6"/>', function () { editDelete(n); }, "danger"));
    }
    ctx.style.left = Math.min(e.clientX, window.innerWidth - 220) + "px";
    ctx.style.top = Math.min(e.clientY, window.innerHeight - (isAdmin ? 320 : 130)) + "px";
    ctx.classList.add("open");
  }
  document.addEventListener("click", function () { ctx.classList.remove("open"); });
  document.addEventListener("scroll", function () { ctx.classList.remove("open"); }, true);

  // ---- düzenleme overlay + form'lar (admin) ----
  var editOv = null;
  function overlay() {
    if (editOv) return editOv;
    editOv = el("div", "sema-edit-overlay"); editOv.id = "semaEdit";
    editOv.innerHTML = '<div class="sema-edit-modal"><div class="seh"><h3 id="semaEditTitle"></h3><button type="button" class="x" id="semaEditClose">Kapat</button></div><div class="seb" id="semaEditBody"></div></div>';
    document.body.appendChild(editOv);
    editOv.addEventListener("click", function (e) { if (e.target === editOv) editOv.classList.remove("open"); });
    editOv.querySelector("#semaEditClose").addEventListener("click", function () { editOv.classList.remove("open"); });
    return editOv;
  }
  function openEdit(title, build) {
    var o = overlay();
    o.querySelector("#semaEditTitle").textContent = title;
    var b = o.querySelector("#semaEditBody"); b.innerHTML = "";
    build(b, function () { o.classList.remove("open"); });
    o.classList.add("open");
  }
  function labeled(label, input) { var f = document.createDocumentFragment(); f.appendChild(el("label", "se-lab", label)); f.appendChild(input); return f; }
  function inp(val) { var i = el("input", "se-inp"); i.type = "text"; i.value = val || ""; return i; }
  function actions(saveLabel, onSave, close, extra) {
    var row = el("div", "se-actions");
    var save = el("button", "primary", saveLabel); save.type = "button"; save.addEventListener("click", onSave);
    var cancel = el("button", null, "İptal"); cancel.type = "button"; cancel.addEventListener("click", close);
    row.append(save, cancel); if (extra) row.appendChild(extra);
    return row;
  }

  function editRename(n) {
    openEdit("İsim / ünvan düzenle", function (b, close) {
      var pos = inp(n.position), nm = inp(n.name || "");
      b.appendChild(labeled("Ünvan", pos)); b.appendChild(labeled("Kişi (holder)", nm));
      b.appendChild(actions("Kaydet", function () { postForm("Rename", { id: opInt(n), title: pos.value, holderName: nm.value }).then(afterEdit); }, close));
    });
  }
  function editAdd(n) {
    openEdit("Alt pozisyon ekle", function (b, close) {
      var pos = inp(""), nm = inp("");
      b.appendChild(el("div", "se-hint", '"' + n.position + '" altına yeni pozisyon.'));
      b.appendChild(labeled("Ünvan", pos)); b.appendChild(labeled("Kişi (holder, opsiyonel)", nm));
      b.appendChild(actions("Ekle", function () { postForm("Add", { parentId: opInt(n), title: pos.value, holderName: nm.value }).then(afterEdit); }, close));
    });
  }
  function editDelete(n) {
    openEdit("Pozisyonu sil", function (b, close) {
      b.appendChild(el("div", "se-hint", '"' + n.position + '"' + (n.name ? " (" + n.name + ")" : "") + " pasifleştirilecek (soft-delete). Alt pozisyonu varsa engellenir."));
      b.appendChild(actions("Sil", function () { postForm("Delete", { id: opInt(n) }).then(afterEdit); }, close));
    });
  }
  function editMove(n) {
    openEdit("Taşı — üst pozisyon değiştir", function (b, close) {
      var sel = el("select", "se-inp");
      var opt0 = el("option", null, "— (kök / üstü yok) —"); opt0.value = ""; sel.appendChild(opt0);
      var selfId = opInt(n);
      // alt ağacı topla (kendine/altına taşınamaz)
      var banned = {}; (function collect(x) { banned[opInt(x)] = true; (x.children || []).forEach(collect); })(n);
      NODES.slice().sort(function (a, c) { return a.position.localeCompare(c.position, "tr"); }).forEach(function (m) {
        var mid = opInt(m); if (banned[mid]) return;
        var o = el("option", null, m.position + (m.name ? " — " + m.name : "")); o.value = mid; sel.appendChild(o);
      });
      b.appendChild(el("div", "se-hint", '"' + n.position + '" hangi pozisyona bağlansın?'));
      b.appendChild(labeled("Yeni üst", sel));
      b.appendChild(actions("Taşı", function () { postForm("Move", { id: selfId, newParentId: sel.value }).then(afterEdit); }, close));
    });
  }
  function editDefinition(n) {
    openEdit("Görev tanımı + KPI düzenle", function (b, close) {
      var def = n.def || {};
      var ta = el("textarea", "se-inp"); ta.value = (def.paragraphs || []).join("\n\n");
      b.appendChild(labeled("Görev, sorumluluk ve yetki (paragraflar — boş satırla ayır)", ta));
      b.appendChild(el("label", "se-lab", "KPI (başarı göstergeleri)"));
      var kwrap = el("div", null);
      function kpiRow(text, active) {
        var row = el("div", "se-krow");
        var ti = el("input"); ti.type = "text"; ti.value = text || ""; ti.placeholder = "KPI metni";
        var lab = el("label"); var cb = el("input"); cb.type = "checkbox"; cb.checked = !!active; lab.append(cb, document.createTextNode("aktif"));
        var rm = el("button", "krm", "✕"); rm.type = "button"; rm.addEventListener("click", function () { kwrap.removeChild(row); });
        row.append(ti, lab, rm); row._get = function () { return { text: ti.value, active: cb.checked }; };
        return row;
      }
      (def.kpi || []).forEach(function (k) { kwrap.appendChild(kpiRow(typeof k === "string" ? k : k.text, typeof k === "object" && k.active)); });
      b.appendChild(kwrap);
      var add = el("button", "se-addrow", "+ KPI satırı ekle"); add.type = "button";
      add.addEventListener("click", function () { kwrap.appendChild(kpiRow("", false)); });
      b.appendChild(add);
      b.appendChild(actions("Kaydet (yeni versiyon)", function () {
        var paras = ta.value.split(/\n\s*\n/).map(function (s) { return s.trim(); }).filter(Boolean);
        var kpi = [].slice.call(kwrap.querySelectorAll(".se-krow")).map(function (r) { return r._get(); }).filter(function (k) { return k.text.trim(); });
        postJson("SaveDefinition", { orgPositionId: opInt(n), paragraphs: paras, kpi: kpi }).then(afterEdit);
      }, close));
    });
  }

  render();
  fit();
})();
