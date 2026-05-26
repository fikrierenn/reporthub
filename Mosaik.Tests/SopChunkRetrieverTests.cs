using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaik.Core.AI.Rag;
using Mosaik.Modules.SOP.Entities;
using Mosaik.Modules.SOP.Services;

namespace Mosaik.Tests;

// Plan 34.1 Faz 1 A-11 — cosine retrieval davranış doğrulaması.
// L2 normalize embeddingler için dot product = cosine.
public class SopChunkRetrieverTests
{
    private sealed class TestContext : DbContext
    {
        public TestContext(DbContextOptions<TestContext> options) : base(options) { }

        public DbSet<SopDocument> SopDocuments => Set<SopDocument>();
        public DbSet<SopVersion> SopVersions => Set<SopVersion>();
        public DbSet<SopChunk> SopChunks => Set<SopChunk>();
        public DbSet<SopReadReceipt> SopReadReceipts => Set<SopReadReceipt>();
        public DbSet<SopApprovalSubmission> SopApprovalSubmissions => Set<SopApprovalSubmission>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            new Mosaik.Modules.SOP.SopModule().ConfigureModelBuilder(mb);
        }
    }

    private static (TestContext db, SopChunkRetriever retriever) NewRetriever(string name)
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(name + "_" + Guid.NewGuid())
            .Options;
        var db = new TestContext(options);
        var retriever = new SopChunkRetriever(db, new DefaultRagAccessPolicy(), NullLogger<SopChunkRetriever>.Instance);
        return (db, retriever);
    }

    private static float[] Normalize(float[] v)
    {
        double norm = 0;
        for (int i = 0; i < v.Length; i++) norm += v[i] * v[i];
        norm = Math.Sqrt(norm);
        if (norm == 0) return v;
        var s = (float)(1.0 / norm);
        var r = new float[v.Length];
        for (int i = 0; i < v.Length; i++) r[i] = v[i] * s;
        return r;
    }

    private static async Task SeedAsync(TestContext db, int firmaId, params (string Title, float[] Emb, byte VersionStatus)[] chunks)
    {
        for (int i = 0; i < chunks.Length; i++)
        {
            var (title, emb, status) = chunks[i];
            var doc = new SopDocument
            {
                FirmaId = firmaId,
                Title = title,
                OwnerUserId = 10,
                IsActive = true,
                CreatedBy = 10
            };
            db.SopDocuments.Add(doc);
            await db.SaveChangesAsync();

            var version = new SopVersion
            {
                SopDocumentId = doc.Id,
                VersionNumber = 1,
                ContentJson = "<p>x</p>",
                Status = status,
                CreatedBy = 10
            };
            db.SopVersions.Add(version);
            await db.SaveChangesAsync();

            db.SopChunks.Add(new SopChunk
            {
                SopVersionId = version.Id,
                ChunkOrder = 0,
                Content = $"Content for {title}",
                EmbeddingJson = JsonSerializer.Serialize(Normalize(emb))
            });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Search_ReturnsTopKByScore_OrderDescending()
    {
        var (db, retriever) = NewRetriever(nameof(Search_ReturnsTopKByScore_OrderDescending));
        await using var _ = db;

        // 3 SOP. Query vector ile en yakın olan en yüksek skor almalı.
        await SeedAsync(db, firmaId: 1,
            ("SOP-A", new float[] { 1, 0, 0 }, 2),
            ("SOP-B", new float[] { 0.5f, 0.5f, 0 }, 2),
            ("SOP-C", new float[] { 0, 1, 0 }, 2));

        var query = Normalize(new float[] { 0.9f, 0.1f, 0 });
        var hits = await retriever.SearchAsync(query, topK: 3, minScore: 0.0);

        Assert.Equal(3, hits.Count);
        Assert.Equal("SOP-A", hits[0].SopTitle);
        Assert.True(hits[0].Score > hits[1].Score);
        Assert.True(hits[1].Score > hits[2].Score);
    }

    [Fact]
    public async Task Search_FiltersBelowMinScore()
    {
        var (db, retriever) = NewRetriever(nameof(Search_FiltersBelowMinScore));
        await using var _ = db;

        await SeedAsync(db, firmaId: 1,
            ("Yakın", new float[] { 1, 0, 0 }, 2),
            ("Uzak", new float[] { 0, 0, 1 }, 2));

        var query = Normalize(new float[] { 1, 0, 0 });
        var hits = await retriever.SearchAsync(query, topK: 5, minScore: 0.5);

        Assert.Single(hits);
        Assert.Equal("Yakın", hits[0].SopTitle);
    }

    [Fact]
    public async Task Search_OnlyApprovedVersions()
    {
        var (db, retriever) = NewRetriever(nameof(Search_OnlyApprovedVersions));
        await using var _ = db;

        await SeedAsync(db, firmaId: 1,
            ("Approved", new float[] { 1, 0, 0 }, 2),       // Status=2 Approved
            ("Draft",    new float[] { 1, 0, 0 }, 0));      // Status=0 Draft — filtre dışı

        var query = Normalize(new float[] { 1, 0, 0 });
        var hits = await retriever.SearchAsync(query, topK: 5, minScore: 0.0);

        Assert.Single(hits);
        Assert.Equal("Approved", hits[0].SopTitle);
    }

    [Fact]
    public async Task Search_FirmaScope_FiltersOtherFirmas()
    {
        var (db, retriever) = NewRetriever(nameof(Search_FirmaScope_FiltersOtherFirmas));
        await using var _ = db;

        await SeedAsync(db, firmaId: 1, ("Firma1", new float[] { 1, 0, 0 }, 2));
        await SeedAsync(db, firmaId: 2, ("Firma2", new float[] { 1, 0, 0 }, 2));

        var query = Normalize(new float[] { 1, 0, 0 });
        var hits = await retriever.SearchAsync(query, topK: 5, minScore: 0.0, firmaId: 1);

        Assert.Single(hits);
        Assert.Equal("Firma1", hits[0].SopTitle);
    }

    // Plan 44 Faz 4 — Permission-aware retrieval scenario tests.

    private static async Task SeedPermissionedChunkAsync(
        TestContext db, int firmaId, string title, float[] emb,
        byte securityLevel = SopChunk.Internal,
        string? allowedRoles = null,
        string? allowedDepts = null,
        string? allowedUsers = null)
    {
        var doc = new SopDocument
        {
            FirmaId = firmaId, Title = title, OwnerUserId = 10, IsActive = true, CreatedBy = 10,
            SecurityLevel = securityLevel
        };
        db.SopDocuments.Add(doc);
        await db.SaveChangesAsync();

        var version = new SopVersion { SopDocumentId = doc.Id, VersionNumber = 1, ContentJson = "<p>x</p>", Status = 2, CreatedBy = 10 };
        db.SopVersions.Add(version);
        await db.SaveChangesAsync();

        db.SopChunks.Add(new SopChunk
        {
            SopVersionId = version.Id, ChunkOrder = 0, Content = $"Chunk: {title}",
            EmbeddingJson = JsonSerializer.Serialize(Normalize(emb)),
            SecurityLevel = securityLevel,
            AllowedRoleIds = allowedRoles,
            AllowedDepartmentIds = allowedDepts,
            AllowedUserIds = allowedUsers
        });
        await db.SaveChangesAsync();
    }

    private static RagUserContext UserCtx(int userId, int firmaId, byte clearance, params string[] roles) =>
        new(userId, firmaId, clearance, roles, Array.Empty<int>());

    [Fact]
    public async Task Search_AdminClearance_SeesRestrictedChunk()
    {
        var (db, retriever) = NewRetriever(nameof(Search_AdminClearance_SeesRestrictedChunk));
        await using var _ = db;

        await SeedPermissionedChunkAsync(db, firmaId: 1, "Gizli SOP", new float[] { 1, 0, 0 },
            securityLevel: SopChunk.Restricted);   // level=3

        var query = Normalize(new float[] { 1, 0, 0 });
        var adminCtx = UserCtx(1, firmaId: 1, clearance: 3, "admin");

        var hits = await retriever.SearchAsync(query, adminCtx, topK: 5, minScore: 0.0);

        Assert.Single(hits);
        Assert.Equal("Gizli SOP", hits[0].SopTitle);
    }

    [Fact]
    public async Task Search_UserClearance_BlockedFromConfidentialChunk()
    {
        var (db, retriever) = NewRetriever(nameof(Search_UserClearance_BlockedFromConfidentialChunk));
        await using var _ = db;

        await SeedPermissionedChunkAsync(db, firmaId: 1, "Gizli SOP", new float[] { 1, 0, 0 },
            securityLevel: SopChunk.Confidential);  // level=2

        var query = Normalize(new float[] { 1, 0, 0 });
        var depoCtx = UserCtx(42, firmaId: 1, clearance: 1, "depo");  // clearance=1 < 2

        var hits = await retriever.SearchAsync(query, depoCtx, topK: 5, minScore: 0.0);

        Assert.Empty(hits);
    }

    [Fact]
    public async Task Search_RoleWhitelist_HrRoleAllowed_DepoRoleBlocked()
    {
        var (db, retriever) = NewRetriever(nameof(Search_RoleWhitelist_HrRoleAllowed_DepoRoleBlocked));
        await using var _ = db;

        await SeedPermissionedChunkAsync(db, firmaId: 1, "İK Prosedürü", new float[] { 1, 0, 0 },
            securityLevel: SopChunk.Internal, allowedRoles: "hr,manager");

        var query = Normalize(new float[] { 1, 0, 0 });

        var hrCtx = UserCtx(1, firmaId: 1, clearance: 2, "hr");
        var depoCtx = UserCtx(2, firmaId: 1, clearance: 1, "depo");

        var hrHits = await retriever.SearchAsync(query, hrCtx, topK: 5, minScore: 0.0);
        var depoHits = await retriever.SearchAsync(query, depoCtx, topK: 5, minScore: 0.0);

        Assert.Single(hrHits);
        Assert.Empty(depoHits);
    }

    [Fact]
    public async Task Search_NullWhitelist_EveryoneCanAccess()
    {
        var (db, retriever) = NewRetriever(nameof(Search_NullWhitelist_EveryoneCanAccess));
        await using var _ = db;

        // null whitelist = no restriction
        await SeedPermissionedChunkAsync(db, firmaId: 1, "Herkese Açık SOP", new float[] { 1, 0, 0 },
            securityLevel: SopChunk.Internal, allowedRoles: null, allowedDepts: null, allowedUsers: null);

        var query = Normalize(new float[] { 1, 0, 0 });
        var anyUser = UserCtx(99, firmaId: 1, clearance: 1, "depo");

        var hits = await retriever.SearchAsync(query, anyUser, topK: 5, minScore: 0.0);

        Assert.Single(hits);
    }

    [Fact]
    public async Task Search_UserWhitelist_ExplicitUserAllowed_OtherBlocked()
    {
        var (db, retriever) = NewRetriever(nameof(Search_UserWhitelist_ExplicitUserAllowed_OtherBlocked));
        await using var _ = db;

        await SeedPermissionedChunkAsync(db, firmaId: 1, "Kişisel SOP", new float[] { 1, 0, 0 },
            securityLevel: SopChunk.Internal, allowedUsers: "5,17");

        var query = Normalize(new float[] { 1, 0, 0 });

        var allowedCtx = UserCtx(5, firmaId: 1, clearance: 1, "user");
        var blockedCtx = UserCtx(99, firmaId: 1, clearance: 1, "user");

        var allowedHits = await retriever.SearchAsync(query, allowedCtx, topK: 5, minScore: 0.0);
        var blockedHits = await retriever.SearchAsync(query, blockedCtx, topK: 5, minScore: 0.0);

        Assert.Single(allowedHits);
        Assert.Empty(blockedHits);
    }
}
