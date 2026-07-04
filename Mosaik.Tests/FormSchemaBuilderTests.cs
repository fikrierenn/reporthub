using System.Text.Json.Nodes;
using Mosaik.Modules.Forms.Entities;
using Mosaik.Modules.Forms.Services;
using Xunit;

namespace Mosaik.Tests
{
    // Plan 41 Faz 1 — FormField[] → survey-core JSON şema dönüşümü.
    public class FormSchemaBuilderTests
    {
        private static FormField Field(byte type, string key = "f1", bool required = false,
            string? options = null, string? validationRules = null, string? conditionalLogic = null) => new()
        {
            FieldKey = key, Label = "Alan " + key, FieldType = type, IsRequired = required,
            Options = options, ValidationRules = validationRules, ConditionalLogic = conditionalLogic
        };

        [Fact]
        public void BuildSchemaJson_ConditionalLogic_EmitsVisibleIf()
        {
            var json = FormSchemaBuilder.BuildSchemaJson(
                [Field(FormFieldType.Text, conditionalLogic: "{\"field\":\"anonim\",\"op\":\"eq\",\"value\":\"Hayır\"}")]);
            var el = JsonNode.Parse(json)!["elements"]![0]!.AsObject();

            Assert.Equal("{anonim} = \"Hayır\"", el["visibleIf"]!.GetValue<string>());
        }

        [Fact]
        public void BuildSchemaJson_NoConditionalLogic_NoVisibleIf()
        {
            var json = FormSchemaBuilder.BuildSchemaJson([Field(FormFieldType.Text)]);
            var el = JsonNode.Parse(json)!["elements"]![0]!.AsObject();

            Assert.False(el.ContainsKey("visibleIf"));
        }

        [Fact]
        public void BuildSchemaJson_TextField_MapsToTextType()
        {
            var json = FormSchemaBuilder.BuildSchemaJson([Field(FormFieldType.Text, required: true)]);
            var root = JsonNode.Parse(json)!.AsObject();
            var el = root["elements"]![0]!.AsObject();

            Assert.Equal("text", el["type"]!.GetValue<string>());
            Assert.True(el["isRequired"]!.GetValue<bool>());
        }

        [Fact]
        public void BuildSchemaJson_SignatureField_MapsToSignaturePad()
        {
            var el = JsonNode.Parse(FormSchemaBuilder.BuildSchemaJson([Field(FormFieldType.Signature)]))!["elements"]![0]!.AsObject();
            Assert.Equal("signaturepad", el["type"]!.GetValue<string>());
        }

        [Fact]
        public void BuildSchemaJson_FileField_SetsStoreDataAsTextAndMaxSize()
        {
            var el = JsonNode.Parse(FormSchemaBuilder.BuildSchemaJson([Field(FormFieldType.File)]))!["elements"]![0]!.AsObject();
            Assert.Equal("file", el["type"]!.GetValue<string>());
            Assert.True(el["storeDataAsText"]!.GetValue<bool>());
            Assert.True(el["maxSize"]!.GetValue<int>() > 0);
        }

        [Fact]
        public void BuildSchemaJson_SelectField_BuildsChoicesFromStringArray()
        {
            var json = FormSchemaBuilder.BuildSchemaJson([Field(FormFieldType.Select, options: "[\"Evet\",\"Hayır\"]")]);
            var el = JsonNode.Parse(json)!["elements"]![0]!.AsObject();

            Assert.Equal("dropdown", el["type"]!.GetValue<string>());
            var choices = el["choices"]!.AsArray();
            Assert.Equal(2, choices.Count);
            Assert.Equal("Evet", choices[0]!["value"]!.GetValue<string>());
        }

        [Fact]
        public void BuildSchemaJson_RadioField_BuildsChoicesFromCodeLabelArray()
        {
            var json = FormSchemaBuilder.BuildSchemaJson(
                [Field(FormFieldType.Radio, options: "[{\"code\":\"a\",\"label\":\"A Seçeneği\"}]")]);
            var el = JsonNode.Parse(json)!["elements"]![0]!.AsObject();

            Assert.Equal("radiogroup", el["type"]!.GetValue<string>());
            var choices = el["choices"]!.AsArray();
            Assert.Equal("a", choices[0]!["value"]!.GetValue<string>());
            Assert.Equal("A Seçeneği", choices[0]!["text"]!.GetValue<string>());
        }

        [Fact]
        public void BuildSchemaJson_SectionType_RendersAsHtmlHeader_NoRequired()
        {
            var json = FormSchemaBuilder.BuildSchemaJson([Field(FormFieldType.Section, required: true)]);
            var el = JsonNode.Parse(json)!["elements"]![0]!.AsObject();

            Assert.Equal("html", el["type"]!.GetValue<string>());
            Assert.False(el.ContainsKey("isRequired"));
        }

        [Fact]
        public void BuildSchemaJson_HiddenField_VisibleFalse()
        {
            var json = FormSchemaBuilder.BuildSchemaJson([Field(FormFieldType.Hidden)]);
            var el = JsonNode.Parse(json)!["elements"]![0]!.AsObject();

            Assert.False(el["visible"]!.GetValue<bool>());
        }

        [Fact]
        public void BuildSchemaJson_ValidationRules_AppliesMinMaxLengthAndRegexValidator()
        {
            var json = FormSchemaBuilder.BuildSchemaJson(
                [Field(FormFieldType.Text, validationRules: "{\"minLength\":2,\"maxLength\":10,\"regex\":\"^[0-9]+$\"}")]);
            var el = JsonNode.Parse(json)!["elements"]![0]!.AsObject();

            Assert.Equal(2, el["minLength"]!.GetValue<int>());
            Assert.Equal(10, el["maxLength"]!.GetValue<int>());
            Assert.Equal("regex", el["validators"]![0]!["type"]!.GetValue<string>());
        }

        [Fact]
        public void BuildSchemaJson_MalformedOptionsJson_ReturnsEmptyChoices_DoesNotThrow()
        {
            var json = FormSchemaBuilder.BuildSchemaJson([Field(FormFieldType.Select, options: "{not valid json")]);
            var el = JsonNode.Parse(json)!["elements"]![0]!.AsObject();

            Assert.Empty(el["choices"]!.AsArray());
        }

        [Fact]
        public void BuildSchemaJson_OrdersFieldsByOrder()
        {
            var f1 = Field(FormFieldType.Text, "second"); f1.Order = 2;
            var f2 = Field(FormFieldType.Text, "first"); f2.Order = 1;
            var json = FormSchemaBuilder.BuildSchemaJson([f1, f2]);
            var elements = JsonNode.Parse(json)!["elements"]!.AsArray();

            Assert.Equal("first", elements[0]!["name"]!.GetValue<string>());
            Assert.Equal("second", elements[1]!["name"]!.GetValue<string>());
        }
    }
}
