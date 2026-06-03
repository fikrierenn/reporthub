using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Mosaik.Models
{
    // Plan 27 Faz C-06 — doküman/klasör seviyesi erişim kontrolü.
    // Subject: user veya role. Scope: contractFileId veya folderId (biri dolu, biri null).
    // Level: 1=Oku, 2=Yaz, 3=Yönet. FirmaId zorunlu — multi-tenant guard.
    public class DocumentPermission
    {
        public int Id { get; set; }
        public int FirmaId { get; set; }

        [MaxLength(10)]
        public string SubjectType { get; set; } = "user"; // "user" | "role"
        public int SubjectId { get; set; }                // UserId veya RoleId

        public int? ContractFileId { get; set; }          // null = klasör seviyesi
        public int? FolderId { get; set; }                // null = dosya seviyesi

        // 1=Oku  2=Yaz  3=Yönet
        public byte Level { get; set; } = 1;

        public DateTime GrantedAt { get; set; }
        public int GrantedById { get; set; }
        public DateTime? ValidUntil { get; set; }

        [ValidateNever]
        public ContractFile? ContractFile { get; set; }
    }
}
