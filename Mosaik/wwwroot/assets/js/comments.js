/* Plan 54 M5 — yorum @mention autocomplete.
   Vanilla IIFE, ES5 hedef. XSS: tüm metin textContent ile basılır, innerHTML yok.
   Textarea'da caret öncesi "@token" yakalar → /Comments/Users?q=token → dropdown. */
(function () {
    "use strict";

    var input = document.querySelector("[data-mention-input]");
    var box = document.querySelector("[data-mention-box]");
    if (!input || !box) return;

    var MENTION_RE = /@([A-Za-z0-9._-]{0,50})$/;
    var activeIndex = -1;
    var items = [];
    var debounceTimer = null;

    function hideBox() {
        box.hidden = true;
        box.textContent = "";
        items = [];
        activeIndex = -1;
    }

    function currentToken() {
        var pos = input.selectionStart;
        var upto = input.value.slice(0, pos);
        var m = MENTION_RE.exec(upto);
        return m ? m[1] : null;
    }

    function renderItems(users) {
        box.textContent = "";
        items = users;
        if (!users.length) { hideBox(); return; }
        users.forEach(function (u, i) {
            var row = document.createElement("button");
            row.type = "button";
            row.className = "mention-item";
            row.setAttribute("role", "option");
            row.dataset.username = u.username;
            var uname = document.createElement("span");
            uname.className = "mention-username";
            uname.textContent = "@" + u.username;
            var full = document.createElement("span");
            full.className = "mention-fullname";
            full.textContent = u.fullName || "";
            row.appendChild(uname);
            row.appendChild(full);
            row.addEventListener("mousedown", function (e) {
                e.preventDefault();
                pick(i);
            });
            box.appendChild(row);
        });
        activeIndex = 0;
        highlight();
        box.hidden = false;
    }

    function highlight() {
        var rows = box.querySelectorAll(".mention-item");
        for (var i = 0; i < rows.length; i++) {
            rows[i].classList.toggle("active", i === activeIndex);
        }
    }

    function pick(i) {
        var u = items[i];
        if (!u) return;
        var pos = input.selectionStart;
        var before = input.value.slice(0, pos).replace(MENTION_RE, "@" + u.username + " ");
        var after = input.value.slice(pos);
        input.value = before + after;
        var caret = before.length;
        input.focus();
        input.setSelectionRange(caret, caret);
        hideBox();
    }

    function fetchUsers(token) {
        var url = "/Comments/Users?q=" + encodeURIComponent(token);
        fetch(url, { headers: { "Accept": "application/json" }, credentials: "same-origin" })
            .then(function (r) { return r.ok ? r.json() : []; })
            .then(function (data) { renderItems(data || []); })
            .catch(function () { hideBox(); });
    }

    input.addEventListener("input", function () {
        var token = currentToken();
        if (token === null) { hideBox(); return; }
        if (debounceTimer) clearTimeout(debounceTimer);
        debounceTimer = setTimeout(function () { fetchUsers(token); }, 150);
    });

    input.addEventListener("keydown", function (e) {
        if (box.hidden) return;
        if (e.key === "ArrowDown") {
            e.preventDefault();
            activeIndex = Math.min(activeIndex + 1, items.length - 1);
            highlight();
        } else if (e.key === "ArrowUp") {
            e.preventDefault();
            activeIndex = Math.max(activeIndex - 1, 0);
            highlight();
        } else if (e.key === "Enter" && activeIndex >= 0) {
            e.preventDefault();
            pick(activeIndex);
        } else if (e.key === "Escape") {
            hideBox();
        }
    });

    input.addEventListener("blur", function () {
        setTimeout(hideBox, 120);
    });
})();
