// Plan 34.1: BKM PRD kurumsal SOP iskeleti — TinyMCE'ye yükle.
// PRD-CRM-001 template baz alındı (16 numaralı bölüm + RACI / Veri İşleme / FMEA / KPI / Revizyon tabloları).
// Confirm prompt: editor doluysa kullanıcı onayı al.
window.loadPrdTemplate = function () {
    if (typeof tinymce === 'undefined') return;
    var editor = tinymce.activeEditor;
    if (!editor) return;

    var current = (editor.getContent() || '').trim();
    if (current && current !== '<p></p>') {
        if (!window.confirm('Editör dolu. PRD template ile değiştirilsin mi? (İçerik kaybolur)')) return;
    }

    var html = '' +
        '<h1>1. AMAÇ</h1>' +
        '<p>Bu prosedürün amacı, [SÜRECİ TANIMLAYIN] süreçlerinin [İLGİLİ MEVZUAT/STANDART] kapsamında uygun yürütülmesini sağlamaktır.</p>' +

        '<h1>2. KAPSAM</h1>' +
        '<p>Bu prosedür aşağıdaki konuları kapsar:</p>' +
        '<ul><li>[Konu 1]</li><li>[Konu 2]</li><li>[Konu 3]</li></ul>' +
        '<p>Bu prosedür, [TARGET ROLE/DEPARTMAN] çalışanlarını kapsar.</p>' +

        '<h1>3. TANIMLAR</h1>' +
        '<p><strong>[Terim 1]:</strong></p><p>[Tanım]</p>' +
        '<p><strong>[Terim 2]:</strong></p><p>[Tanım]</p>' +

        '<h1>4. SORUMLULUKLAR</h1>' +
        '<p>Bu prosedür kapsamındaki sorumluluklar aşağıdaki RACI matrisi ile tanımlanmıştır.</p>' +
        '<p><em>S = Sorumlu (yapan) • A = Anahtar Onaylayan (hesap veren) • D = Danışılan • B = Bilgilendirilen</em></p>' +
        '<table style="border-collapse:collapse; width:100%;" border="1">' +
        '<thead><tr>' +
        '<th>Faaliyet</th><th>Rol 1</th><th>Rol 2</th><th>Rol 3</th><th>Rol 4</th>' +
        '</tr></thead>' +
        '<tbody>' +
        '<tr><td>[Faaliyet 1]</td><td>S</td><td>A</td><td>D</td><td>B</td></tr>' +
        '<tr><td>[Faaliyet 2]</td><td>A</td><td>S</td><td>B</td><td>D</td></tr>' +
        '</tbody></table>' +

        '<h1>5. SÜREÇ AKIŞI</h1>' +
        '<h2>5.1. Genel Akış Özeti</h2>' +
        '<p>[Sürecin genel akışını özetle.]</p>' +
        '<h2>5.2. [Alt Süreç Başlığı]</h2>' +
        '<h3>Adımlar:</h3>' +
        '<ul><li>[Adım 1]</li><li>[Adım 2]</li><li>[Adım 3]</li></ul>' +

        '<h1>6. VERİ İŞLEME DETAYLARI</h1>' +
        '<p>Aşağıdaki tabloda ilgili süreçte işlenen veriler, kategorileri, hukuki sebepleri ve saklama süreleri belirtilmiştir.</p>' +
        '<table style="border-collapse:collapse; width:100%;" border="1">' +
        '<thead><tr><th>Veri Kategorisi</th><th>Veri Türü</th><th>Hukuki Sebep</th><th>Saklama Süresi</th></tr></thead>' +
        '<tbody>' +
        '<tr><td>[Kimlik]</td><td>[Ad, Soyad]</td><td>[KVKK m.5/2/c]</td><td>[10 yıl]</td></tr>' +
        '<tr><td>[İletişim]</td><td>[Telefon]</td><td>[KVKK m.5/2/c]</td><td>[10 yıl]</td></tr>' +
        '</tbody></table>' +

        '<h1>7. AYDINLATMA VE RIZA YÖNETİMİ</h1>' +
        '<h2>7.1. Aydınlatma Metni</h2>' +
        '<p>[Aydınlatma metni gösterim ilkeleri]</p>' +
        '<h2>7.2. Rıza Yönetimi</h2>' +
        '<p>[Rıza alma, geri çekme, arşivleme akışı]</p>' +

        '<h1>8. ÖZEL DURUMLAR</h1>' +
        '<h2>8.1. [Özel durum 1]</h2>' +
        '<p>[Açıklama + akış]</p>' +

        '<h1>9. İDARİ VE TEKNİK TEDBİRLER</h1>' +
        '<h2>9.1. İdari Tedbirler</h2>' +
        '<ul><li>[Tedbir 1: Eğitim, NDA, yetki matrisi]</li><li>[Tedbir 2]</li></ul>' +
        '<h2>9.2. Teknik Tedbirler</h2>' +
        '<ul><li>[Tedbir 1: RBAC, şifreleme, MFA]</li><li>[Tedbir 2]</li></ul>' +

        '<h1>10. RİSKLER VE KONTROLLER (FMEA)</h1>' +
        '<p>Olasılık × Etki = Risk Skoru. Skor ≥ 6 olanlar kritik kabul edilir.</p>' +
        '<table style="border-collapse:collapse; width:100%;" border="1">' +
        '<thead><tr><th>Risk</th><th>Olası Etki</th><th>Olasılık</th><th>Etki</th><th>Skor</th><th>Kontrol</th></tr></thead>' +
        '<tbody>' +
        '<tr><td>[Risk 1]</td><td>[Etki]</td><td>Orta</td><td>Yüksek</td><td>9</td><td>[Kontrol]</td></tr>' +
        '</tbody></table>' +

        '<h1>11. KAYIT VE ARŞİV</h1>' +
        '<ul>' +
        '<li>[Kayıt 1] - [Saklama süresi]</li>' +
        '<li>[Kayıt 2] - [Saklama süresi]</li>' +
        '</ul>' +

        '<h1>12. PERFORMANS GÖSTERGELERİ (KPI)</h1>' +
        '<table style="border-collapse:collapse; width:100%;" border="1">' +
        '<thead><tr><th>KPI</th><th>Tanım</th><th>Hedef</th><th>Ölçüm Sıklığı</th></tr></thead>' +
        '<tbody>' +
        '<tr><td>[KPI 1]</td><td>[Tanım]</td><td>[≥ %X]</td><td>Aylık</td></tr>' +
        '</tbody></table>' +

        '<h1>13. EĞİTİM</h1>' +
        '<ul>' +
        '<li>[Eğitim 1 - kapsam ve sıklık]</li>' +
        '<li>[Eğitim 2]</li>' +
        '</ul>' +

        '<h1>14. UYUM DENETİMİ</h1>' +
        '<ul>' +
        '<li>[İç denetim kapsamı ve sıklığı]</li>' +
        '<li>[Dış denetim - opsiyonel]</li>' +
        '</ul>' +

        '<h1>15. İLGİLİ DOKÜMANLAR</h1>' +
        '<ul>' +
        '<li>[PRD-XXX-001: İlgili prosedür]</li>' +
        '<li>[AYD-XXX-001: İlgili aydınlatma metni]</li>' +
        '</ul>' +

        '<h1>16. REVİZYON GEÇMİŞİ</h1>' +
        '<table style="border-collapse:collapse; width:100%;" border="1">' +
        '<thead><tr><th>Rev. No</th><th>Tarih</th><th>Değişiklik</th><th>Hazırlayan</th><th>Onaylayan</th></tr></thead>' +
        '<tbody>' +
        '<tr><td>00</td><td>[GG.AA.YYYY]</td><td>İlk yayın</td><td>[Hazırlayan]</td><td>[Onaylayan]</td></tr>' +
        '</tbody></table>' +

        '<hr>' +
        '<h2>ONAY VE İMZA</h2>' +
        '<p>İşbu prosedür yukarıda belirtilen tarih itibariyle yürürlüğe girer. Prosedüre uyum tüm ilgili çalışanlar için zorunludur.</p>' +
        '<table style="border-collapse:collapse; width:100%;" border="1">' +
        '<thead><tr><th>HAZIRLAYAN</th><th>KONTROL</th><th>ONAYLAYAN</th></tr></thead>' +
        '<tbody>' +
        '<tr>' +
        '<td>[İsim]<br>Tarih: __/__/____<br>İmza:</td>' +
        '<td>[İsim]<br>Tarih: __/__/____<br>İmza:</td>' +
        '<td>[İsim]<br>Tarih: __/__/____<br>İmza:</td>' +
        '</tr>' +
        '</tbody></table>';

    editor.setContent(html);
};
