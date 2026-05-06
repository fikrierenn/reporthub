---
type: synthesis
subtype: weekly
tags: [synthesis, weekly]
created: <YYYY-MM-DD>
updated: <YYYY-MM-DD>
week: <YYYY-Wnn>
status: active
---

# Hafta <YYYY-Wnn> Sentezi

> Pazartesi <YYYY-MM-DD> → Pazar <YYYY-MM-DD>
> Bu sayfa `wiki-keeper` skill'i tarafından her oturum sonu append edilir; haftanın sonunda Fikri toparlar.

## Günlük Notlar

<!-- Her oturumdan sonra wiki-keeper buraya 1 blok ekler -->

## Hafta Özeti (Pazar günü doldur)

### Tamamlananlar
- <Madde>

### Kararlar (kalıcı yer)
- <Karar> → [[<ADR veya concept link>]]

### Açık İşler / Yarın'a
- <Madde>

### Öğrenmeler
- <Sürpriz çıkan, dikkat gerektiren>

### Etkilenen Entity'ler (otomatik liste için Dataview)
```dataview
TABLE updated
FROM "entities"
WHERE updated >= date(<YYYY-MM-DD>) AND updated <= date(<YYYY-MM-DD>)
SORT updated DESC
```

## Update Log

- <YYYY-MM-DD>: Hafta açılışı (wiki-keeper auto-create)
- <YYYY-MM-DD>: Hafta kapanışı (Fikri özet)
