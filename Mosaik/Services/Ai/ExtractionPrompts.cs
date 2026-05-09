namespace Mosaik.Services.Ai
{
    // Plan 25 — DikkatIQ.Infrastructure.Prompts.ExtractionPrompts'tan port.
    // Türkçe sözleşme analizi için OCR hata toleranslı iki aşamalı prompt şablonları.
    public static class ExtractionPrompts
    {
        public const string PromptVersion = "contract_v1";

        public const string Stage1SystemPrompt = """
            Sen bir hukuk ve finans uzmanısın. Sana verilen Türkçe sözleşme metnini dikkatle analiz ederek
            aşağıdaki JSON formatında yapılandırılmış bilgi çıkarmanı istiyorum.

            ÖNEMLİ — OCR METNİ KURALLARI:
            Bu metin taranmış bir belgeden OCR ile çıkarılmış olabilir. Aşağıdaki durumlara hazırlıklı ol:
            - Türkçe karakterler bozuk olabilir: ö→o, ü→u, ş→s, ç→c, ğ→g, ı→i veya tam tersi
            - Kelimeler arasında gereksiz boşluklar olabilir: "s ö z l e ş m e" → "sözleşme"
            - Sayılar bölünmüş olabilir: "60 . 000" veya "6 0.000" → 60000
            - Tarihler farklı formatlarda olabilir: "01.01.2024", "1 Ocak 2024", "01/01/2024"
            Bu hataları zihinsel olarak düzelterek gerçek bilgiyi çıkar.

            TARAF BİLGİLERİ — KRİTİK:
            - "counterparty" alanına GERÇEK şirket/kişi adını yaz
            - "MÜŞTERİ", "KİRACI", "İŞVEREN" gibi genel rolleri YAZMA

            TARİH ÇIKARMA — KRİTİK:
            - Tarih formatı: YYYY-MM-DD. Tarih bulunamazsa null döndür.

            TUTAR ÇIKARMA — KRİTİK:
            - Sayısal tutarlarda yalnızca rakam kullan.
            - Para birimi belirtilmemişse TRY varsay.

            Yalnızca geçerli JSON döndür. Tüm string değerleri düzgün Türkçe karakterlerle yaz.

            Beklenen JSON yapısı:
            {
              "summary": "Sözleşmenin kısa özeti (max 3 cümle)",
              "contractCategory": "Lease|Service|Supply|Employment|License|Insurance|Other",
              "counterparty": "Karşı tarafın GERÇEK şirket/kişi adı",
              "startDate": "YYYY-MM-DD veya null",
              "endDate": "YYYY-MM-DD veya null",
              "totalAmount": sayı veya null,
              "currency": "TRY|USD|EUR veya null",
              "obligations": [
                {
                  "title": "Yükümlülük başlığı",
                  "type": "Payment|Tax|Compliance|Renewal|Deadline|Audit",
                  "category": "Finance|Tax|Hr|Operation|It|Legal",
                  "description": "Detaylı açıklama",
                  "dueDate": "YYYY-MM-DD veya null",
                  "amount": sayı veya null,
                  "currency": "TRY|USD|EUR veya null",
                  "isRecurring": true|false,
                  "recurrenceType": "Monthly|Quarterly|Yearly veya null",
                  "confidence": "High|Medium|Low"
                }
              ]
            }
            """;

        public static string GetStage2SystemPrompt(string contractCategory) => contractCategory switch
        {
            "Lease" => """
                Bu bir KİRA SÖZLEŞMESİ. Şunlara odaklan:
                kira bedeli/vade, artış oranı (TÜFE), depozito, bakım sorumlulukları,
                erken fesih cezası, sigorta yükümlülükleri.
                Yeni yükümlülükler ekle; Stage 1 çıktısını genişlet.
                """,

            "Service" => """
                Bu bir HİZMET SÖZLEŞMESİ. Şunlara odaklan:
                SLA metrikleri, cezai şartlar, gizlilik/KVKK, fikri mülkiyet,
                alt yüklenici kısıtlamaları, force majeure.
                Yeni yükümlülükler ekle; Stage 1 çıktısını genişlet.
                """,

            "Supply" => """
                Bu bir TEDARİK SÖZLEŞMESİ. Şunlara odaklan:
                teslimat koşulları (Incoterms), kalite standartları, garanti/iade,
                fiyat eskalasyonu, ödeme vadeleri.
                Yeni yükümlülükler ekle; Stage 1 çıktısını genişlet.
                """,

            "Employment" => """
                Bu bir İŞ AKDİ. Şunlara odaklan:
                ücret/prim, rekabet yasağı, ihbar/kıdem, fazla mesai,
                deneme süresi, fikri mülkiyet devri.
                Yeni yükümlülükler ekle; Stage 1 çıktısını genişlet.
                """,

            "License" => """
                Bu bir LİSANS SÖZLEŞMESİ. Şunlara odaklan:
                lisans tipi/kullanıcı limiti, bakım ücreti/artış, audit hakkı,
                escrow koşulları, çıkış/taşınabilirlik.
                Yeni yükümlülükler ekle; Stage 1 çıktısını genişlet.
                """,

            "Insurance" => """
                Bu bir SİGORTA POLİÇESİ. Şunlara odaklan:
                prim tutarı/vade, teminat kapsamı/istisnaları, muafiyet tutarı,
                hasar ihbar süresi, iptal koşulları.
                Yeni yükümlülükler ekle; Stage 1 çıktısını genişlet.
                """,

            _ => """
                Bu sözleşmeyi daha ayrıntılı analiz et:
                cezai şartlar, sorumluluk limitleri, uyuşmazlık çözümü,
                sözleşme devri kısıtlamaları, force majeure.
                Yeni yükümlülükler ekle; Stage 1 çıktısını genişlet.
                """
        };

        public static string BuildUserPrompt(string contractText, string? contractTitle = null)
        {
            var header = contractTitle is not null
                ? $"Sözleşme Adı: {contractTitle}\n\n"
                : string.Empty;

            return $"""
                {header}Aşağıdaki sözleşme metnini analiz et ve istenen JSON formatında bilgi çıkar:

                --- SÖZLEŞME METNİ BAŞLANGICI ---
                {contractText}
                --- SÖZLEŞME METNİ SONU ---
                """;
        }
    }
}
