/* Organizasyon Şeması — DETAY: seçim + detay modalı (tanım+KPI) + sağ-tık menü + .md indirme.
   window.__orgSema (core) üstüne selectNode/showCtx/closeDetail ekler; admin item'ları
   edit parçası S.editActions ile sağlar (yoksa salt-görüntüleme menüsü). */
(function () {
  "use strict";
  var S = window.__orgSema;
  if (!S) return;
  var el = S.el, svg = S.svg;

  // detay modal
  var dov = document.getElementById("semaDov");
  function closeDetail() { dov.classList.remove("open"); }
  document.getElementById("semaDClose").addEventListener("click", closeDetail);
  dov.addEventListener("click", function (e) { if (e.target === dov) closeDetail(); });
  document.addEventListener("keydown", function (e) { if (e.key === "Escape") closeDetail(); });

  function selectNode(n) {
    S.setSelId(n.id);
    var prev = S.layer.querySelector(".node.sel"); if (prev) prev.classList.remove("sel");
    if (n._node) n._node.classList.add("sel");
    renderDetail(n); dov.classList.add("open");
  }

  function renderDetail(n) {
    var rv = S.ROLE_VAR[n.role] || "--r-diger";
    var b = document.getElementById("semaDBadge");
    b.textContent = S.ROLE_LABEL[n.role] || "Diğer";
    b.style.color = "var(" + rv + ")"; b.style.border = "1px solid var(" + rv + ")";
    var c = document.getElementById("semaDContent"); c.innerHTML = "";
    var crumb = el("div", "crumb"); crumb.textContent = S.pathOf(n).join("  ›  "); c.appendChild(crumb);
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
      cb.appendChild(el("span", null, S.collapsed[n.id] ? ("Altını aç (" + n.children.length + ")") : "Altını topla"));
      cb.addEventListener("click", function () { S.toggle(n); renderDetail(n); });
      oa.appendChild(cb);
    }
    var fb = el("button", null);
    fb.innerHTML = svg('<circle cx="12" cy="12" r="3"/><path d="M12 3v2M12 19v2M3 12h2M19 12h2"/>');
    fb.appendChild(el("span", null, "Sadece bu dal"));
    fb.addEventListener("click", function () { S.focusBranch(n); });
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
    ctx.appendChild(ctxItem("Sadece bu dal", '<circle cx="12" cy="12" r="3"/><path d="M12 5v2M12 17v2M5 12h2M17 12h2"/>', function () { S.focusBranch(n); }));
    ctx.appendChild(ctxItem("Görev tanımını indir (.md)", '<path d="M12 3v12M7 10l5 5 5-5M5 21h14"/>', function () { downloadMd(n); }));
    var ea = S.editActions; // edit parçası (admin) yüklüyse düzenleme item'ları
    if (ea) {
      ctx.appendChild(el("div", "sep", "Düzenle"));
      ctx.appendChild(ctxItem("İsim / ünvan düzenle", '<path d="M12 20h9M16.5 3.5a2.1 2.1 0 013 3L7 19l-4 1 1-4z"/>', function () { ea.editRename(n); }));
      ctx.appendChild(ctxItem("Alt pozisyon ekle", '<path d="M12 5v14M5 12h14"/>', function () { ea.editAdd(n); }));
      ctx.appendChild(ctxItem("Görev tanımı + KPI düzenle", '<path d="M4 6h16M4 12h16M4 18h10"/>', function () { ea.editDefinition(n); }));
      ctx.appendChild(ctxItem("Taşı (üst değiştir)", '<path d="M8 6l-4 4 4 4M4 10h12a4 4 0 014 4v2"/>', function () { ea.editMove(n); }));
      ctx.appendChild(ctxItem("Sil", '<path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6"/>', function () { ea.editDelete(n); }, "danger"));
    }
    ctx.style.left = Math.min(e.clientX, window.innerWidth - 220) + "px";
    ctx.style.top = Math.min(e.clientY, window.innerHeight - (ea ? 320 : 130)) + "px";
    ctx.classList.add("open");
  }
  document.addEventListener("click", function () { ctx.classList.remove("open"); });
  document.addEventListener("scroll", function () { ctx.classList.remove("open"); }, true);

  S.selectNode = selectNode;
  S.showCtx = showCtx;
  S.closeDetail = closeDetail;
  S.renderDetail = renderDetail;
})();
