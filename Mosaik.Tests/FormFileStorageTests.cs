using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaik.Modules.Forms.Services;
using Xunit;

namespace Mosaik.Tests
{
    // Plan 41 Faz 4 — form eki/imza decode + magic-byte doğrulama (saf — disk'e dokunmaz).
    public class FormFileStorageTests
    {
        // env decode yolunda kullanılmaz (yalnız WriteToDisk). Stub yeterli.
        // Encryption: ephemeral key-ring (test-içi, disk'e key yazmaz) — A2-#1 dosya-şifreleme ctor'u.
        private static FormFileStorage NewStorage() =>
            new(new StubEnv(),
                new FormEncryptionService(new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider()),
                NullLogger<FormFileStorage>.Instance);

        private static string DataUrl(string mime, byte[] bytes) =>
            $"data:{mime};base64,{Convert.ToBase64String(bytes)}";

        private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
        private static readonly byte[] Pdf = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31];   // %PDF-1
        private static readonly byte[] Jpg = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2];
        private static readonly byte[] Bogus = [0x00, 0x01, 0x02, 0x03, 0x04];       // magic yok

        private static string FileFieldJson(string mime, byte[] bytes) =>
            $"[{{\"name\":\"x\",\"type\":\"{mime}\",\"content\":\"{DataUrl(mime, bytes)}\"}}]";

        [Fact]
        public void DecodeSignature_ValidPng_Succeeds()
        {
            var r = NewStorage().DecodeSignature(DataUrl("image/png", Png), FormFileStorage.PublicMaxBytes);
            Assert.True(r.IsSuccess);
            Assert.Equal(".png", r.Data.Ext);
        }

        [Fact]
        public void DecodeSignature_NonPngBytes_Fails()
        {
            // Client "image/png" MIME iddia etse de magic-byte JPG → imza reddedilir (PNG zorunlu).
            var r = NewStorage().DecodeSignature(DataUrl("image/png", Jpg), FormFileStorage.PublicMaxBytes);
            Assert.False(r.IsSuccess);
        }

        [Fact]
        public void DecodeFileField_Pdf_Succeeds()
        {
            var r = NewStorage().DecodeFileField(FileFieldJson("application/pdf", Pdf), FormFileStorage.PublicMaxBytes);
            Assert.True(r.IsSuccess);
            Assert.Equal(".pdf", r.Data.Ext);
        }

        [Fact]
        public void DecodeFileField_BogusMagicByte_Fails()
        {
            // Magic-byte whitelist dışı içerik — client MIME'a güvenilmez, reddedilir.
            var r = NewStorage().DecodeFileField(FileFieldJson("application/pdf", Bogus), FormFileStorage.PublicMaxBytes);
            Assert.False(r.IsSuccess);
        }

        [Fact]
        public void DecodeFileField_OverSizeLimit_Fails()
        {
            var big = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // geçerli PDF magic
            var r = NewStorage().DecodeFileField(FileFieldJson("application/pdf", big), maxBytes: 2);
            Assert.False(r.IsSuccess);
        }

        [Fact]
        public void DecodeFileField_EmptyArray_Fails()
        {
            var r = NewStorage().DecodeFileField("[]", FormFileStorage.PublicMaxBytes);
            Assert.False(r.IsSuccess);
        }

        [Fact]
        public void DecodeFileField_MalformedJson_Fails()
        {
            var r = NewStorage().DecodeFileField("{not-json", FormFileStorage.PublicMaxBytes);
            Assert.False(r.IsSuccess);
        }

        [Fact]
        public void DecodeSignature_NotDataUrl_Fails()
        {
            var r = NewStorage().DecodeSignature("bilerek-bozuk", FormFileStorage.PublicMaxBytes);
            Assert.False(r.IsSuccess);
        }

        // Plan 57 A2-#1 — dosya byte şifreleme round-trip (aynı provider çözer, ciphertext ≠ plaintext).
        [Fact]
        public void EncryptBytes_RoundTrip_DecryptsToOriginal()
        {
            var enc = new FormEncryptionService(new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider());
            var plain = Encoding.UTF8.GetBytes("ihbar kanıt dosyası içeriği");

            var cipher = enc.EncryptBytes(plain);
            Assert.NotEqual(plain, cipher);

            Assert.True(enc.TryDecryptBytes(cipher, out var roundTrip));
            Assert.Equal(plain, roundTrip);
        }

        [Fact]
        public void TryDecryptBytes_WrongKeyRing_FailsClosed()
        {
            // Farklı provider (farklı key) → çözemez, exception değil false (fail-closed sinyal).
            var enc1 = new FormEncryptionService(new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider());
            var enc2 = new FormEncryptionService(new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider());

            var cipher = enc1.EncryptBytes([1, 2, 3]);
            Assert.False(enc2.TryDecryptBytes(cipher, out _));
        }

        private sealed class StubEnv : IWebHostEnvironment
        {
            public string WebRootPath { get; set; } = "";
            public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
            public string ApplicationName { get; set; } = "Test";
            public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
            public string ContentRootPath { get; set; } = System.IO.Path.GetTempPath();
            public string EnvironmentName { get; set; } = "Test";
        }
    }
}
