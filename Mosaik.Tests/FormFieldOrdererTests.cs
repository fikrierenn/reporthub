using Mosaik.Modules.Forms.Services;
using Xunit;

namespace Mosaik.Tests
{
    // Plan 41 Faz 2 — liste-tabanlı builder sıralama (drag-drop yok, yukarı/aşağı buton).
    public class FormFieldOrdererTests
    {
        private static readonly List<(int Id, int Order)> ThreeFields =
            [(1, 1), (2, 2), (3, 3)];

        [Fact]
        public void Move_Up_SwapsWithPreviousField()
        {
            var result = FormFieldOrderer.Move(ThreeFields, targetId: 2, FieldMoveDirection.Up);
            Assert.Equal(2, result.Count);
            Assert.Contains((1, 2), result);
            Assert.Contains((2, 1), result);
        }

        [Fact]
        public void Move_Down_SwapsWithNextField()
        {
            var result = FormFieldOrderer.Move(ThreeFields, targetId: 2, FieldMoveDirection.Down);
            Assert.Equal(2, result.Count);
            Assert.Contains((2, 3), result);
            Assert.Contains((3, 2), result);
        }

        [Fact]
        public void Move_Up_AtFirstPosition_NoOp()
        {
            var result = FormFieldOrderer.Move(ThreeFields, targetId: 1, FieldMoveDirection.Up);
            Assert.Empty(result);
        }

        [Fact]
        public void Move_Down_AtLastPosition_NoOp()
        {
            var result = FormFieldOrderer.Move(ThreeFields, targetId: 3, FieldMoveDirection.Down);
            Assert.Empty(result);
        }

        [Fact]
        public void Move_UnknownId_ReturnsEmpty()
        {
            var result = FormFieldOrderer.Move(ThreeFields, targetId: 999, FieldMoveDirection.Up);
            Assert.Empty(result);
        }
    }
}
