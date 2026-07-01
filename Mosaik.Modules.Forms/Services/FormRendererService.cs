using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Modules.Forms.Entities;

namespace Mosaik.Modules.Forms.Services
{
    public sealed record FormRenderModel(FormDefinition Definition, int FormVersionId, string SchemaJson);

    // Plan 41 Faz 1 — form yayınlanmış mı + aktif versiyon çöz + schema üret (FormSchemaBuilder reuse).
    public class FormRendererService(DbContext db, ILogger<FormRendererService> logger)
    {
        public async Task<FormRenderModel?> GetBySlugAsync(string slug, int firmaId, CancellationToken ct = default)
        {
            var def = await db.Set<FormDefinition>().AsNoTracking()
                .Include(d => d.Fields)
                .FirstOrDefaultAsync(d => d.Slug == slug && d.FirmaId == firmaId && d.Status == 1, ct);
            if (def == null)
                return null; // form yok / yayında değil — normal 404

            var latestVersion = await db.Set<FormDefinitionVersion>().AsNoTracking()
                .Where(v => v.FormDefinitionId == def.Id)
                .OrderByDescending(v => v.Version)
                .FirstOrDefaultAsync(ct);
            if (latestVersion == null)
            {
                // silent-failure-hunter MEDIUM: "form yok" ile "veri bütünlüğü arızası" aynı null'a
                // düşüyordu — yayınlama akışı yarım kalmış demektir, sessizce 404'e karışmasın.
                logger.LogError(
                    "FormRendererService: FormDefinitionId={Id} (slug={Slug}) Yayında ama FormDefinitionVersion snapshot yok — yayınlama akışı bozuk.",
                    def.Id, slug);
                return null;
            }

            return new FormRenderModel(def, latestVersion.Id, latestVersion.SchemaJson);
        }
    }
}
