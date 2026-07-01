namespace Mosaik.Modules.Forms.Services
{
    public enum FieldMoveDirection { Up, Down }

    // Plan 41 Faz 2 — liste-tabanlı builder alan sıralama (drag-drop yok, yukarı/aşağı buton).
    // Saf/testable — DB'ye dokunmaz, sadece (Id,Order) çiftlerini yeniden hesaplar.
    public static class FormFieldOrderer
    {
        // targetId'yi bir konum yukarı/aşağı taşır (komşusuyla Order swap). Sınırda (ilk/son) no-op.
        // Dönüş: değişen (Id,NewOrder) çiftleri — çağıran taraf sadece bunları DB'ye yazar.
        public static List<(int Id, int NewOrder)> Move(
            IReadOnlyList<(int Id, int Order)> fields, int targetId, FieldMoveDirection direction)
        {
            var ordered = fields.OrderBy(f => f.Order).ToList();
            var index = ordered.FindIndex(f => f.Id == targetId);
            if (index < 0)
                return [];

            var swapIndex = direction == FieldMoveDirection.Up ? index - 1 : index + 1;
            if (swapIndex < 0 || swapIndex >= ordered.Count)
                return []; // sınırda — no-op

            var a = ordered[index];
            var b = ordered[swapIndex];
            return [(a.Id, b.Order), (b.Id, a.Order)];
        }
    }
}
