// builder-v2/builder-utils.js — Shared utility helpers for builder-v2 modules.
// Cross-file communication via window.__BuilderV2 namespace (ES5 IIFE).
// Loaded BEFORE other builder-v2 scripts in CreateReportV2/EditReportV2 views.
//
// Exposes:
//   __BuilderV2.escHtml(s)              — XSS-safe HTML escape (incl. &quot;)
//   __BuilderV2.fmtCell(rawVal, fmt)    — table cell formatter (currency/number/percent/date/text/auto)
//   __BuilderV2.fmtChart(v, numFormat)  — chart axis/tooltip formatter
//
// AntiForgery token global: window.getAntiForgeryToken() (app-shell.js)

(function () {
    "use strict";
    window.__BuilderV2 = window.__BuilderV2 || {};

    window.__BuilderV2.escHtml = function (s) {
        return String(s == null ? '' : s)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    };

    window.__BuilderV2.fmtCell = function (rawVal, fmt) {
        if (rawVal == null || rawVal === '') return '—';
        var n = parseFloat(rawVal);
        if (fmt === 'currency') return isNaN(n) ? String(rawVal) : '₺ ' + n.toLocaleString('tr-TR', { maximumFractionDigits: 2 });
        if (fmt === 'number')   return isNaN(n) ? String(rawVal) : n.toLocaleString('tr-TR', { maximumFractionDigits: 2 });
        if (fmt === 'percent')  return isNaN(n) ? String(rawVal) : n.toFixed(1).replace('.', ',') + '%';
        if (fmt === 'date') {
            try { var d = new Date(rawVal); return isNaN(d.getTime()) ? String(rawVal) : d.toLocaleDateString('tr-TR'); }
            catch (e) { return String(rawVal); }
        }
        if (fmt === 'text') return String(rawVal);
        // auto
        if (isNaN(n)) return String(rawVal);
        return Math.abs(n) >= 1000 ? n.toLocaleString('tr-TR', { maximumFractionDigits: 1 }) : String(n);
    };

    window.__BuilderV2.fmtChart = function (v, numFormat) {
        var n = typeof v === 'number' ? v : parseFloat(v);
        if (isNaN(n)) return v;
        if (numFormat === 'currency') return '₺ ' + n.toLocaleString('tr-TR', { maximumFractionDigits: 0 });
        if (numFormat === 'currency-short') {
            if (Math.abs(n) >= 1e6) return '₺ ' + (n / 1e6).toFixed(1).replace('.', ',') + 'M';
            if (Math.abs(n) >= 1e3) return '₺ ' + (n / 1e3).toFixed(1).replace('.', ',') + 'K';
            return '₺ ' + n;
        }
        if (numFormat === 'percent') return n.toFixed(1).replace('.', ',') + '%';
        if (numFormat === 'decimal2') return n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        return Math.abs(n) >= 1000 ? (n / 1000).toLocaleString('tr-TR', { maximumFractionDigits: 1 }) + 'k' : n;
    };
})();
