namespace Mosaik.Core.Workflow
{
    // Plan 57 Part C — org-omurga amir çözümleme (assigneeKind:"manager").
    // Interface Core'da, implementasyon ana projede (IEntityWorkflowProvider emsali —
    // modüller ana projeye referans veremez, ADR-002). Zincir: submitter User.Personelno
    // → OrgPositions (HolderPersonelno doğrudan; yoksa GorevPersonelMap→GorevDocument→pozisyon)
    // → ParentPositionId yukarı-yürüyüş (holder dolu ilk ata, cycle+derinlik guard'lı)
    // → amir HolderPersonelno → Users.Personelno → amir UserId.
    public interface IManagerResolver
    {
        // Çözülemezse null (fail-closed değil fail-FALLBACK: caller şablon assigneeRole'a düşer
        // + loglar; onaysız geçiş caller sorumluluğunda ASLA olmaz).
        Task<int?> ResolveManagerUserIdAsync(int submitterUserId, CancellationToken ct = default);
    }
}
