using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Mosaik.Models
{
    // Plan 27 Faz C — versiyonlama. ContractFile'ın eski versiyonlarını saklar.
    // Yeni dosya yüklenince mevcut dosyanın snapshot'ı buraya alınır, Version++ yapılır.
    // Fiziksel dosya diskte kalır — path DocumentVersion.FilePath'te referanslanır.
    public class DocumentVersion
    {
        public int Id { get; set; }
        public int ContractFileId { get; set; }
        public int VersionNumber { get; set; }

        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(260)]
        public string FileName { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [MaxLength(100)]
        public string MimeType { get; set; } = string.Empty;

        public DateTime ArchivedAt { get; set; }
        public int ArchivedById { get; set; }

        [ValidateNever]
        public ContractFile ContractFile { get; set; } = null!;
    }
}
