namespace Mosaik.Core.DataScope
{
    // Plan 14 Faz C + Plan 16.5 Faz B birleşim — vNext modüller (tamimRead,
    // documentAccess vs.) bu interface'i implement edip merkezi enforcement
    // alır. UserDataFilterInjector + ReportsController bu registry'yi kullanır.
    public interface IUserDataScope
    {
        // "spInjection", "reportAccess", "tamimRead", "documentAccess" gibi
        string Scope { get; }

        // True ise: kullanıcının en az 1 UserDataFilter kaydı zorunlu (deny-by-default).
        // False ise: kayıt yoksa "tümü görür" davranışı.
        bool RequiresExplicitGrant { get; }

        // Belirli bir veri kaynağı için kullanıcı verilen değere erişebiliyor mu?
        // BKM şube heterojenliği nedeniyle dataSourceKey zorunlu (PDKS sube ID
        // vs IK sube ID birbirine uymaz).
        Task<bool> HasAccessAsync(int userId, string? dataSourceKey, string value);

        // Kullanıcının bu scope için erişebildiği tüm değerler (UI dropdown vs).
        // "*" yıldızı tümü anlamına gelir, expand etmez.
        Task<List<string>> ListAccessibleValuesAsync(int userId, string? dataSourceKey);
    }
}
