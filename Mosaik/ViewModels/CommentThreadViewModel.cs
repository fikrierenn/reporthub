using Mosaik.Core.Comments;

namespace Mosaik.ViewModels
{
    // Plan 54 M5 — CommentThread ViewComponent modeli.
    public class CommentThreadViewModel
    {
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public int FirmaId { get; set; }
        public List<Comment> Comments { get; set; } = new();
    }
}
