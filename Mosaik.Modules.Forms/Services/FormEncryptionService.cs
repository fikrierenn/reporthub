using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Mosaik.Modules.Forms.Services
{
    // Plan 56 M-A — form alanı at-rest şifreleme (ihbar/DSAR/whistleblower).
    // ASP.NET Core DataProtection (mosaik-security danış: AES manuel key yerine — Kural 5 secret-out-of-source;
    // key-ring framework-yönetimli, appsettings'te secret yok). IDataProtectionProvider DI'da default kayıtlı.
    // Key persist: default (ContentRoot/keys, dev DPAPI). Prod çok-sunucu → shared path + cert (README notu).
    // Purpose-string versiyonlu: ileride v2 rotate edilebilir, v1 hâlâ çözülür.
    public class FormEncryptionService
    {
        private readonly IDataProtector _protector;
        private readonly IDataProtector _fileProtector;

        public FormEncryptionService(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("Forms.FieldEncryption.v1");
            // Dosya için ayrı purpose (security M-2 defense-in-depth): alan/dosya ciphertext'i
            // yanlış path'e beslenirse purpose uyuşmaz → fail-closed. Prod'da şifreli dosya
            // henüz yok — split maliyetsiz.
            _fileProtector = provider.CreateProtector("Forms.FileEncryption.v1");
        }

        public string Encrypt(string plain) => _protector.Protect(plain);

        // Çözülemezse (key yok/bozuk cipher) exception yutulmaz üst katmana sinyal — sessiz plaintext dönme yok.
        public bool TryDecrypt(string cipher, out string plain)
        {
            try { plain = _protector.Unprotect(cipher); return true; }
            catch (CryptographicException) { plain = string.Empty; return false; }
        }

        // Plan 57 A2-#1 — şifreli formda DOSYA EKLERİ de şifrelenir (ihbar kanıt dosyası App_Data'da
        // düz metin kalıyordu; alan-şifrelemenin amacını deliyordu). Aynı key-ring, ayrı purpose.
        public byte[] EncryptBytes(byte[] plain) => _fileProtector.Protect(plain);

        public bool TryDecryptBytes(byte[] cipher, out byte[] plain)
        {
            try { plain = _fileProtector.Unprotect(cipher); return true; }
            catch (CryptographicException) { plain = []; return false; }
        }
    }
}
