using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mosaik.Core.Domain;

namespace Mosaik.Modules.Circular.Models
{
    // Plan 17 Faz E — Bir bloğa eklenen file (PDF/DOCX/XLSX/JPG/PNG/TXT).
    // Kullanım: DailyBlock 1-N BlockFile. Circular Detay'da bağlı blokların
    // dosyaları toplu görünür.
    //
    // Güvenlik:
    //  - Dosya adı sanitize edilir (path traversal yok)
    //  - Whitelist uzantı + MIME (BlockFileService.AllowedExtensions)
    //  - Max 10 MB (BlockFileService.MaxBytes)
    //  - Disk path GUID-based: /wwwroot/uploads/blocks/{yyyy}/{MM}/{guid}.{ext}
    //  - Download endpoint yetki kontrollü (sahip veya admin veya bağlı tamim okuyucusu)
    [Table("BlockFiles")]
    public class BlockFile : BaseEntity
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        // Hangi bloğa ait
        public int BlockId { get; set; }

        // Kullanıcının yüklediği orijinal file adı (UI'da gösterim)
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        // Disk üzerindeki relative path (örn. uploads/blocks/2026/05/abc-123.pdf)
        [Required]
        [MaxLength(500)]
        [BindNever]
        public string FilePath { get; set; } = string.Empty;

        // Uzantı (pdf/docx/xlsx/jpg/png/txt) — UI ikon belirleme
        [Required]
        [MaxLength(10)]
        public string Extension { get; set; } = string.Empty;

        // MIME type (Content-Type)
        [MaxLength(100)]
        public string? MimeType { get; set; }

        // FileSize (byte)
        public long FileSize { get; set; }

        // Yükleyen kullanıcı
        [BindNever]
        public int UploadedById { get; set; }

        [BindNever]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Soft-delete
        public bool IsActive { get; set; } = true;
    }
}
