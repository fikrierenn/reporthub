using Mosaik.Modules.Forms.Entities;
using Mosaik.Modules.Forms.Services;
using Xunit;

namespace Mosaik.Tests
{
    // Plan 41 Faz 1 — server-side authoritative validation (client'a güvenilmez).
    public class FormFieldValidatorTests
    {
        private static FormField Field(byte type, bool required = false, string? validationRules = null) => new()
        {
            FieldKey = "f1", Label = "Test Alanı", FieldType = type, IsRequired = required,
            ValidationRules = validationRules
        };

        [Fact]
        public void Validate_RequiredFieldEmpty_ReturnsError()
        {
            var error = FormFieldValidator.Validate(Field(FormFieldType.Text, required: true), "");
            Assert.NotNull(error);
        }

        [Fact]
        public void Validate_OptionalFieldEmpty_ReturnsNull()
        {
            var error = FormFieldValidator.Validate(Field(FormFieldType.Text), null);
            Assert.Null(error);
        }

        [Fact]
        public void Validate_NumberField_NonNumeric_ReturnsError()
        {
            var error = FormFieldValidator.Validate(Field(FormFieldType.Number), "abc");
            Assert.NotNull(error);
        }

        [Fact]
        public void Validate_NumberField_ValidNumber_ReturnsNull()
        {
            var error = FormFieldValidator.Validate(Field(FormFieldType.Number), "42.5");
            Assert.Null(error);
        }

        [Fact]
        public void Validate_DateField_InvalidDate_ReturnsError()
        {
            var error = FormFieldValidator.Validate(Field(FormFieldType.Date), "not-a-date");
            Assert.NotNull(error);
        }

        [Fact]
        public void Validate_MinLengthRule_TooShort_ReturnsError()
        {
            var error = FormFieldValidator.Validate(
                Field(FormFieldType.Text, validationRules: "{\"minLength\":5}"), "abc");
            Assert.NotNull(error);
        }

        [Fact]
        public void Validate_RegexRule_Mismatch_ReturnsError()
        {
            var error = FormFieldValidator.Validate(
                Field(FormFieldType.Text, validationRules: "{\"regex\":\"^[0-9]+$\"}"), "abc123");
            Assert.NotNull(error);
        }

        [Fact]
        public void Validate_RegexRule_Match_ReturnsNull()
        {
            var error = FormFieldValidator.Validate(
                Field(FormFieldType.Text, validationRules: "{\"regex\":\"^[0-9]+$\"}"), "12345");
            Assert.Null(error);
        }

        [Fact]
        public void Validate_HiddenField_NeverValidated()
        {
            var error = FormFieldValidator.Validate(Field(FormFieldType.Hidden, required: true), null);
            Assert.Null(error);
        }

        [Fact]
        public void Validate_MalformedValidationRulesJson_DoesNotThrow_FailsClosed()
        {
            // silent-failure-hunter CRITICAL fix: bozuk kural artık fail-open değil — submit reddedilir.
            var error = FormFieldValidator.Validate(
                Field(FormFieldType.Text, validationRules: "{not valid"), "anything");
            Assert.NotNull(error);
        }
    }
}
