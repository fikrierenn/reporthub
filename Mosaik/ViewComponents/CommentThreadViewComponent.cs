using Microsoft.AspNetCore.Mvc;
using Mosaik.Core.Comments;
using Mosaik.ViewModels;

namespace Mosaik.ViewComponents
{
    // Plan 54 M5 — cross-modül yorum thread'i. Modül Details view'ları
    // @await Component.InvokeAsync("CommentThread", new { entityType, entityId, firmaId })
    // ile çağırır (ADR-002: modül MosaikContext'e değil, app-wide ViewComponent'a erişir).
    public class CommentThreadViewComponent : ViewComponent
    {
        private readonly ICommentService _comments;

        public CommentThreadViewComponent(ICommentService comments) => _comments = comments;

        public async Task<IViewComponentResult> InvokeAsync(string entityType, int entityId, int firmaId)
        {
            var items = await _comments.GetForEntityAsync(entityType, entityId, firmaId);
            return View(new CommentThreadViewModel
            {
                EntityType = entityType,
                EntityId = entityId,
                FirmaId = firmaId,
                Comments = items
            });
        }
    }
}
