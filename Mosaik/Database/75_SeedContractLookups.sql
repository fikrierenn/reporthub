-- Migration 75: Plan 21 — Contract/Obligation/Calendar enum → DictionaryType lookup seed
-- Tarih: 2026-06-29
-- Plan: plans/21-lookup-table-enum-to-db.md (spec güncellendi: ayrı Lookups tablosu YOK, DictionaryType/Value kullanılır)
-- 5 type + 29 değer. Code = enum üye adının camelCase'i (MVC enum binding case-insensitive parse eder).
-- Enum INT kolonları DEĞİŞMEZ — lookup sadece TR etiket + sıra + aktiflik sağlar.
-- İdempotent (38_SeedTamimLookups pattern).

SET NOCOUNT ON;
GO

-- Yardımcı: type yoksa ekle
DECLARE @types TABLE (Code NVARCHAR(50), Name NVARCHAR(100), Descr NVARCHAR(500));
INSERT INTO @types VALUES
    (N'contractCategory',  N'Sözleşme Kategorisi',   N'Contracts: ContractCategory enum etiketleri'),
    (N'obligationCategory',N'Yükümlülük Kategorisi',  N'Obligations: ObligationCategory enum etiketleri'),
    (N'obligationType',    N'Yükümlülük Türü',        N'Obligations: ObligationType enum etiketleri'),
    (N'recurrenceType',    N'Tekrar Türü',            N'Compliance: RecurrenceType enum etiketleri'),
    (N'eventType',         N'Takvim Etkinlik Türü',   N'Calendar: EventType enum etiketleri');

INSERT INTO dbo.DictionaryTypes (Code, Name, Description, IsActive)
SELECT t.Code, t.Name, t.Descr, 1
FROM @types t
WHERE NOT EXISTS (SELECT 1 FROM dbo.DictionaryTypes dt WHERE dt.Code = t.Code);
GO

-- Değerler: (typeCode, valueCode, label, order)
DECLARE @vals TABLE (TypeCode NVARCHAR(50), Code NVARCHAR(50), Label NVARCHAR(200), Ord INT);
INSERT INTO @vals VALUES
    -- ContractCategory (enum ordinal: Lease=0..Other=6)
    (N'contractCategory', N'lease',      N'Kira',      0),
    (N'contractCategory', N'service',    N'Hizmet',    10),
    (N'contractCategory', N'supply',     N'Tedarik',   20),
    (N'contractCategory', N'employment', N'İş',        30),
    (N'contractCategory', N'license',    N'Lisans',    40),
    (N'contractCategory', N'insurance',  N'Sigorta',   50),
    (N'contractCategory', N'other',      N'Diğer',     60),
    -- ObligationCategory
    (N'obligationCategory', N'finance',   N'Finans',    0),
    (N'obligationCategory', N'tax',       N'Vergi',     10),
    (N'obligationCategory', N'hr',        N'İK',        20),
    (N'obligationCategory', N'operation', N'Operasyon', 30),
    (N'obligationCategory', N'it',        N'BT',        40),
    (N'obligationCategory', N'legal',     N'Hukuk',     50),
    -- ObligationType
    (N'obligationType', N'payment',    N'Ödeme',      0),
    (N'obligationType', N'tax',        N'Vergi',      10),
    (N'obligationType', N'compliance', N'Uyumluluk',  20),
    (N'obligationType', N'renewal',    N'Yenileme',   30),
    (N'obligationType', N'deadline',   N'Son Tarih',  40),
    (N'obligationType', N'audit',      N'Denetim',    50),
    -- RecurrenceType
    (N'recurrenceType', N'monthly',   N'Aylık',      0),
    (N'recurrenceType', N'quarterly', N'Çeyreklik',  10),
    (N'recurrenceType', N'yearly',    N'Yıllık',     20),
    (N'recurrenceType', N'custom',    N'Özel',       30),
    -- EventType
    (N'eventType', N'payment',    N'Ödeme',      0),
    (N'eventType', N'deadline',   N'Son Tarih',  10),
    (N'eventType', N'renewal',    N'Yenileme',   20),
    (N'eventType', N'tax',        N'Vergi',      30),
    (N'eventType', N'compliance', N'Uyumluluk',  40),
    (N'eventType', N'operation',  N'Operasyon',  50);

INSERT INTO dbo.DictionaryValues (TypeId, Code, Label, DisplayOrder, IsActive)
SELECT dt.Id, v.Code, v.Label, v.Ord, 1
FROM @vals v
JOIN dbo.DictionaryTypes dt ON dt.Code = v.TypeCode
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.DictionaryValues dv
    WHERE dv.TypeId = dt.Id AND dv.Code = v.Code
);
GO

PRINT 'Migration 75: contract/obligation/recurrence/event lookups seeded (idempotent).';
GO
