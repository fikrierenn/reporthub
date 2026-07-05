/* Organizasyon Şeması — DÜZENLEME (admin): POST yardımcıları + overlay + 5 form
   (rename / alt ekle / sil / taşı / tanım+KPI). window.__orgSema.editActions'ı kurar;
   admin değilse (data-admin!=1) hiçbir şey eklemez — detail menüsü salt-görüntüleme kalır. */
(function () {
  "use strict";
  var S = window.__orgSema;
  if (!S) return;
  var wrap = document.querySelector(".sema-wrap");
  var isAdmin = wrap && wrap.getAttribute("data-admin") === "1";
  if (!isAdmin) return;
  var el = S.el;

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

  // ---- düzenleme overlay + form'lar ----
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
      S.NODES.slice().sort(function (a, c) { return a.position.localeCompare(c.position, "tr"); }).forEach(function (m) {
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

  S.editActions = {
    editRename: editRename, editAdd: editAdd, editDelete: editDelete,
    editMove: editMove, editDefinition: editDefinition
  };
})();
