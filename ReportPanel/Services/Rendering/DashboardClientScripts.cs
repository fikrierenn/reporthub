using System.Text;

namespace ReportPanel.Services.Rendering
{
    // M-13 R5 (28 Nisan 2026): DashboardShellRenderer.cs 620 sat → 2 partial dosya.
    // 4 Mayıs 2026 split (csharp-conventions hard-limit 500 aşımı, 551 → ~150):
    //   - DashboardClientScripts.cs (bu dosya): RenderScripts orchestrator + util JS
    //     (switchTab, modal, fmtNum, fmtKpi, aggVal).
    //   - DashboardClientScripts.Kpi.cs: KPI variant init (basic/delta/sparkline/progress).
    //   - DashboardClientScripts.Chart.cs: Chart.js init (10 variant).
    //   - DashboardClientScripts.Table.cs: Tablo init + fmtCell + conditionalFormat + CSV export.
    internal static partial class DashboardShellRenderer
    {
        public static void RenderScripts(StringBuilder sb)
        {
            sb.AppendLine("<script>");

            EmitTabAndModal(sb);
            EmitFormatHelpers(sb);
            EmitKpiInit(sb);
            EmitChartInit(sb);
            EmitTableInit(sb);

            sb.AppendLine("</script>");
        }

        private static void EmitTabAndModal(StringBuilder sb)
        {
            // Tab switching
            sb.AppendLine(@"
function switchTab(idx) {
  document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
  document.querySelectorAll('.tab-content').forEach(c => c.style.display = 'none');
  document.querySelectorAll('.tab')[idx].classList.add('active');
  document.getElementById('tab-' + idx).style.display = 'block';
}");

            // Modal (DOM API + textContent — XSS güvenli)
            sb.AppendLine(@"
function showDetail(row) {
  document.getElementById('modalTitle').textContent = 'Satır Detayı';
  var body = document.getElementById('modalBody');
  body.textContent = '';
  var table = document.createElement('table');
  table.className = 'w-full text-sm';
  for (var key in row) {
    if (!Object.prototype.hasOwnProperty.call(row, key)) continue;
    var tr = document.createElement('tr');
    tr.className = 'border-b border-gray-100';
    var tdKey = document.createElement('td');
    tdKey.className = 'py-2 pr-4 text-gray-500 font-medium';
    tdKey.textContent = key;
    var tdVal = document.createElement('td');
    tdVal.className = 'py-2 text-gray-900';
    var raw = row[key];
    tdVal.textContent = (raw == null || raw === '') ? '—' : fmtNum(raw);
    tr.appendChild(tdKey);
    tr.appendChild(tdVal);
    table.appendChild(tr);
  }
  body.appendChild(table);
  document.getElementById('detailModal').classList.add('open');
}
function closeModal() { document.getElementById('detailModal').classList.remove('open'); }
document.addEventListener('keydown', e => { if(e.key==='Escape') closeModal(); });");
        }

        private static void EmitFormatHelpers(StringBuilder sb)
        {
            // Sayı formatlama (tr-TR)
            sb.AppendLine(@"
function fmtNum(v) {
  if (v == null || v === '' || v === '—') return '—';
  var n = parseFloat(v);
  if (isNaN(n)) return v;
  if (Math.abs(n) >= 1000) return n.toLocaleString('tr-TR', {minimumFractionDigits: 0, maximumFractionDigits: 2});
  if (n % 1 !== 0) return n.toLocaleString('tr-TR', {minimumFractionDigits: 1, maximumFractionDigits: 2});
  return n.toString();
}");

            // KPI aggregation
            sb.AppendLine(@"
function aggVal(rs, agg, col, cond) {
  var data = (window.__RS && window.__RS[rs]) ? window.__RS[rs] : [];
  if (cond === 'notNull') data = data.filter(r => r[col] != null && r[col] !== '' && r[col] !== '—');
  if (cond === 'isNull') data = data.filter(r => r[col] == null || r[col] === '' || r[col] === '—');
  if (agg === 'count' || agg === 'countWhere') return data.length;
  if (!col) return data.length;
  var vals = data.map(r => parseFloat(r[col])).filter(v => !isNaN(v));
  if (agg === 'sum') return vals.reduce((a,b) => a+b, 0);
  if (agg === 'avg') return vals.length ? (vals.reduce((a,b) => a+b, 0) / vals.length).toFixed(1) : 0;
  if (agg === 'min') return vals.length ? Math.min(...vals) : 0;
  if (agg === 'max') return vals.length ? Math.max(...vals) : 0;
  if (agg === 'first') return data.length ? (data[0][col] ?? '—') : '—';
  return data.length;
}");

            // KPI ileri ayar formul compute (fA op fB; op: + - * / % yuzde=100*A/B).
            // Formul tum 3 alani dolu ise oncelikli; degilse cfg.col + cfg.agg fallback.
            sb.AppendLine(@"
function computeKpiBase(cfg) {
  if (cfg && cfg.fA && cfg.fOp && cfg.fB) {
    var aRaw = aggVal(cfg.rs, cfg.agg, cfg.fA, cfg.cond);
    var bRaw = aggVal(cfg.rs, cfg.agg, cfg.fB, cfg.cond);
    var a = parseFloat(aRaw), b = parseFloat(bRaw);
    if (isNaN(a) || isNaN(b)) return null;
    switch (cfg.fOp) {
      case '+': return a + b;
      case '-': return a - b;
      case '*': return a * b;
      case '/': return b === 0 ? null : a / b;
      case '%': return b === 0 ? null : (100 * a / b);
    }
    return null;
  }
  return aggVal(cfg.rs, cfg.agg, cfg.col, cfg.cond);
}");

            // KPI number formatter (ADR-008 F-4: numberFormat alan destegi)
            sb.AppendLine(@"
function fmtKpi(v, fmt) {
  if (v == null || v === '' || v === '—') return '—';
  var n = parseFloat(v);
  if (isNaN(n)) return v;
  if (fmt === 'currency') return '₺ ' + n.toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  if (fmt === 'currency-short') {
    if (Math.abs(n) >= 1e6) return '₺ ' + (n/1e6).toFixed(1).replace('.', ',') + 'M';
    if (Math.abs(n) >= 1e3) return '₺ ' + (n/1e3).toFixed(1).replace('.', ',') + 'K';
    return '₺ ' + n;
  }
  if (fmt === 'percent')  return n.toFixed(1).replace('.', ',') + '%';
  if (fmt === 'decimal2') return n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  return fmtNum(n);
}");
        }
    }
}
