/* Organizasyon Şeması — ÇEKİRDEK: veri parse + layout + render + pan/zoom + arama + odak.
   (Plan 55 Faz 5; 412-satır org-sema.js hard-limit 350 aşımı → 3 parça split, js-conventions.)
   Parçalar arası köprü: window.__orgSema namespace (core kurar; detail/edit üstüne ekler).
   Yükleme sırası: core → detail → edit (Sema/Index.cshtml). XSS: kullanıcı metni textContent. */
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
    // detail/edit parçaları geç-bağlanır (yükleme sırası esnesin diye çağrı anında lookup).
    el2.addEventListener("click", function () { if (S.selectNode) S.selectNode(n); });
    el2.addEventListener("contextmenu", function (e) { e.preventDefault(); if (S.showCtx) S.showCtx(e, n); });
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
  function focusBranch(n) { focusRoot = n; (function od(x) { delete collapsed[x.id]; (x.children || []).forEach(od); })(n); if (S.closeDetail) S.closeDetail(); unfocusBtn.classList.remove("is-hidden"); render(); fit(); }
  function clearFocus() { focusRoot = null; unfocusBtn.classList.add("is-hidden"); render(); fit(); }
  unfocusBtn.addEventListener("click", clearFocus);

  // parça-köprüsü: detail/edit bu namespace üstüne selectNode/showCtx/closeDetail/editActions ekler.
  var S = window.__orgSema = {
    DATA: DATA, NODES: NODES, collapsed: collapsed,
    ROLE_LABEL: ROLE_LABEL, ROLE_VAR: ROLE_VAR,
    el: el, svg: svg, pathOf: pathOf,
    render: render, fit: fit, toggle: toggle, focusBranch: focusBranch,
    layer: layer,
    getSelId: function () { return selId; },
    setSelId: function (id) { selId = id; }
  };

  render();
  fit();
})();
