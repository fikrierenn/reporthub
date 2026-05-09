# 🚀 ReportHub Refactor Directive

## (AI Execution Document – TR & EN)

---

# 🇹🇷 1. BAĞLAM (CONTEXT)

ReportHub mevcutta:

* ASP.NET Core backend kullanıyor
* Vanilla JS dashboard-builder içeriyor
* SQL Stored Procedure ile veri alıyor
* Dashboard ve rapor üretimi yapıyor

Sistem çalışıyor ancak:

❗ UI odaklı
❗ State yönetimi zayıf
❗ Result index bağımlılığı var
❗ Ölçeklenebilir değil

---

# 🇺🇸 1. CONTEXT

ReportHub currently:

* uses ASP.NET Core backend
* has a vanilla JS dashboard-builder
* executes SQL stored procedures
* renders dashboards and reports

The system works but is:

❗ UI-driven
❗ fragile
❗ index-dependent
❗ not scalable

---

# 🇹🇷 2. AMAÇ (OBJECTIVE)

Sistemi şu yapıya dönüştür:

> **Config-Driven Analytics Engine**

---

# 🇺🇸 2. OBJECTIVE

Transform the system into:

> **Config-Driven Analytics Engine**

---

# 🔥 3. TEMEL PRENSİP (CORE PRINCIPLE)

## 🇹🇷

> Veri bir kez gelir, her şey JSON ile şekillenir.

## 🇺🇸

> Data comes once. Everything is driven by JSON configuration.

---

# 🧱 4. HEDEF MİMARİ (TARGET ARCHITECTURE)

```
Data (SP)
↓
Contract (ResultName)
↓
State (JSON)
↓
Render (UI)
```

---

# ⚠️ 5. ZORUNLU KURALLAR (HARD RULES)

## 5.1 ❌ INDEX KULLANIMI YASAK

Yanlış:

```
result[0]
```

Doğru:

```
engine.getData("summary")
```

---

## 5.2 MULTI RESULT ZORUNLU

* 1 SP → N result
* İlk result = metadata
* Her result = isimli

---

## 5.3 BACKEND GÖREVİ

✔ SP çalıştırır
✔ Result parse eder
✔ Name-based map oluşturur

```
{
  "summary": [...],
  "detail": [...],
  "chart": [...]
}
```

❌ HTML üretmez
❌ UI logic içermez

---

## 5.4 FRONTEND GÖREVİ

✔ JSON’dan render eder
✔ state-driven çalışır

❌ index kullanmaz
❌ DB bilmez

---

## 5.5 SINGLE SOURCE OF TRUTH

✔ Tüm UI → JSON config’ten gelir

---

# 🧠 6. DATA CONTRACT

## SP STANDARDI

```
-- RESULT 0
SELECT 'summary' as ResultName, 0 as ResultIndex
UNION ALL
SELECT 'detail', 1
UNION ALL
SELECT 'chart', 2
```

---

# 📦 7. JSON STATE MODEL

```
{
  "dataSource": "sp_dashboard",
  "widgets": [
    {
      "type": "chart",
      "result": "chart",
      "config": {
        "x": "date",
        "y": "amount"
      }
    }
  ]
}
```

---

# 🖥️ 8. RENDER ENGINE

* Widget loop edilir
* Data name ile alınır
* UI render edilir

---

# 🧩 9. ZORUNLU ÖZELLİKLER

## 9.1 EDIT MODE / VIEW MODE

✔ Edit → builder
✔ View → render only

---

## 9.2 PREVIEW

✔ Gerçek renderer kullanılmalı
❌ Fake preview yasak

---

## 9.3 MULTI VIEW

Aynı data:

* table
* chart
* pivot
* combined

---

## 9.4 EVENT BUS

```
Chart click → Table filter
```

---

# 🔄 10. REFACTOR GÖREVİ

## 🇹🇷

1. Kodu analiz et
2. Problemleri bul
3. Şuna dönüştür:

   * state manager
   * renderer
   * data layer

---

## 🇺🇸

You must:

1. Analyze existing code
2. Identify problems
3. Refactor into:

   * state manager
   * renderer
   * data layer

---

# 📤 11. BEKLENEN ÇIKTI

* Sistem analizi
* Problemler
* Refactor planı
* Mimari öneri
* Örnek kod

---

# 🎯 12. FINAL GOAL

## 🇹🇷

> Tek SP + Tek JSON → Tüm UI

## 🇺🇸

> One SP + One JSON → Entire UI

---

# ⚔️ 13. HATALI ÇÖZÜM KRİTERLERİ

❌ result index kullanımı
❌ UI + data karışımı
❌ widget başına SP
❌ hardcoded layout

---

# ✅ 14. BAŞARI KRİTERLERİ

✔ Tek SP → çok widget
✔ JSON-driven UI
✔ reusable renderer
✔ scalable sistem

---

# 🔥 SON TANIM

## 🇹🇷

> ReportHub = Config-driven analytics engine

## 🇺🇸

> ReportHub = Config-driven analytics execution platform
