using Mosaik.Modules.Forms.Entities;
using Mosaik.Modules.Forms.Services;
using Xunit;

namespace Mosaik.Tests
{
    // Plan 41 Faz 2 — Taslak→Yayında geçişi. DataElement mapping OPTIONAL (§4.3 düzeltme) —
    // warning üretir, blocker DEĞİL.
    public class FormPublishValidatorTests
    {
        private static FormField Field(int id, byte type) => new() { Id = id, FieldKey = "f" + id, Label = "L" + id, FieldType = type };

        [Fact]
        public void HasSubmittableField_TextField_ReturnsTrue()
        {
            Assert.True(FormPublishValidator.HasSubmittableField([Field(1, FormFieldType.Text)]));
        }

        [Fact]
        public void HasSubmittableField_OnlySectionAndHidden_ReturnsFalse()
        {
            Assert.False(FormPublishValidator.HasSubmittableField(
                [Field(1, FormFieldType.Section), Field(2, FormFieldType.Hidden)]));
        }

        [Fact]
        public void HasSubmittableField_EmptyList_ReturnsFalse()
        {
            Assert.False(FormPublishValidator.HasSubmittableField([]));
        }

        [Fact]
        public void GetUnmappedFieldWarnings_UnmappedField_ReturnsWarning()
        {
            var warnings = FormPublishValidator.GetUnmappedFieldWarnings([Field(1, FormFieldType.Text)], new HashSet<int>());
            Assert.Single(warnings);
        }

        [Fact]
        public void GetUnmappedFieldWarnings_MappedField_ReturnsEmpty()
        {
            var warnings = FormPublishValidator.GetUnmappedFieldWarnings([Field(1, FormFieldType.Text)], new HashSet<int> { 1 });
            Assert.Empty(warnings);
        }

        [Fact]
        public void GetUnmappedFieldWarnings_SectionField_NeverWarns()
        {
            var warnings = FormPublishValidator.GetUnmappedFieldWarnings([Field(1, FormFieldType.Section)], new HashSet<int>());
            Assert.Empty(warnings);
        }

        [Fact]
        public void GetEmptyChoiceErrors_SelectWithoutOptions_ReturnsError()
        {
            var f = Field(1, FormFieldType.Select); // Options null
            var errors = FormPublishValidator.GetEmptyChoiceErrors([f]);
            Assert.Single(errors);
        }

        [Fact]
        public void GetEmptyChoiceErrors_SelectWithOptions_ReturnsEmpty()
        {
            var f = Field(1, FormFieldType.Select);
            f.Options = "[\"Evet\",\"Hayır\"]";
            var errors = FormPublishValidator.GetEmptyChoiceErrors([f]);
            Assert.Empty(errors);
        }

        [Fact]
        public void GetEmptyChoiceErrors_TextField_NeverChecked()
        {
            var errors = FormPublishValidator.GetEmptyChoiceErrors([Field(1, FormFieldType.Text)]);
            Assert.Empty(errors);
        }

        [Fact]
        public void GetEmptyChoiceErrors_RadioWithMalformedJson_ReturnsError()
        {
            var f = Field(1, FormFieldType.Radio);
            f.Options = "{bozuk json";
            var errors = FormPublishValidator.GetEmptyChoiceErrors([f]);
            Assert.Single(errors);
        }
    }
}
