using System.Text.Json;
using System.Text.Json.Nodes;
using Mosaik.Modules.Forms.Entities;

namespace Mosaik.Modules.Forms.Services
{
    // Plan 41 Faz 1 — FormField[] → survey-core (MIT) JSON şema dönüşümü. Saf/testable —
    // DB'ye dokunmaz. Kendi renderer YAZILMAZ (rev 2 kararı); survey-core bu şemayı render eder.
    public static class FormSchemaBuilder
    {
        public static string BuildSchemaJson(IEnumerable<FormField> fields)
        {
            var elements = new JsonArray();
            foreach (var field in fields.OrderBy(f => f.Order))
                elements.Add(BuildElement(field));

            var schema = new JsonObject { ["elements"] = elements };
            return schema.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        }

        private static JsonObject BuildElement(FormField field)
        {
            var el = new JsonObject
            {
                ["name"] = field.FieldKey,
                ["title"] = field.Label,
                ["type"] = MapType(field.FieldType)
            };

            if (field.IsRequired && field.FieldType != FormFieldType.Section)
                el["isRequired"] = true;
            if (!string.IsNullOrWhiteSpace(field.HelpText))
                el["description"] = field.HelpText;
            if (!string.IsNullOrWhiteSpace(field.Placeholder))
                el["placeHolder"] = field.Placeholder;
            if (!string.IsNullOrWhiteSpace(field.DefaultValue))
                el["defaultValue"] = field.DefaultValue;

            switch (field.FieldType)
            {
                case FormFieldType.Number:
                    el["inputType"] = "number";
                    break;
                case FormFieldType.Date:
                    el["inputType"] = "date";
                    break;
                case FormFieldType.DateTime:
                    el["inputType"] = "datetime-local";
                    break;
                case FormFieldType.Select:
                case FormFieldType.MultiSelect:
                case FormFieldType.Radio:
                    el["choices"] = BuildChoices(field.Options);
                    break;
                case FormFieldType.File:
                    // survey-core file question: tek dosya, base64 dataURL olarak data'ya gömülür
                    // (storeDataAsText — ayrı upload endpoint yok, tek POST kanalı, FormSubmissionService decode eder).
                    el["storeDataAsText"] = true;
                    el["allowMultiple"] = false;
                    el["maxSize"] = FormFileStorage.PublicMaxBytes; // client-side ön kontrol; server magic-byte+boyut yeniden doğrular
                    break;
                case FormFieldType.Signature:
                    // survey-core native signaturepad (signature_pad bundled, MIT) → data:image/png;base64 dataURL.
                    el["penColor"] = "#1f2937";
                    break;
                case FormFieldType.Hidden:
                    el["visible"] = false;
                    break;
                case FormFieldType.Section:
                    el.Remove("type");
                    el["type"] = "html";
                    el["html"] = $"<h3>{System.Net.WebUtility.HtmlEncode(field.Label)}</h3>";
                    return el; // section header taşımaz — required/validator eklenmez
            }

            ApplyValidation(el, field);
            return el;
        }

        // survey-core question tipleri (MIT). signaturepad + file native — ayrı lib gerekmez.
        private static string MapType(byte fieldType) => fieldType switch
        {
            FormFieldType.Text => "text",
            FormFieldType.TextArea => "comment",
            FormFieldType.Number => "text",
            FormFieldType.Date => "text",
            FormFieldType.DateTime => "text",
            FormFieldType.Select => "dropdown",
            FormFieldType.MultiSelect => "checkbox",
            FormFieldType.Radio => "radiogroup",
            FormFieldType.Checkbox => "boolean",
            FormFieldType.File => "file",
            FormFieldType.Signature => "signaturepad", // native (signature_pad bundled)
            FormFieldType.Hidden => "text",
            _ => "text"
        };

        // Options JSON: ["Evet","Hayır"] veya [{"code":"a","label":"A"}] — ikisini de kabul eder.
        private static JsonArray BuildChoices(string? optionsJson)
        {
            var choices = new JsonArray();
            if (string.IsNullOrWhiteSpace(optionsJson))
                return choices;

            try
            {
                var node = JsonNode.Parse(optionsJson);
                if (node is not JsonArray arr)
                    return choices;

                foreach (var item in arr)
                {
                    if (item is JsonValue v && v.TryGetValue(out string? s))
                        choices.Add(new JsonObject { ["value"] = s, ["text"] = s });
                    else if (item is JsonObject o)
                        choices.Add(new JsonObject
                        {
                            ["value"] = o["code"]?.GetValue<string>() ?? o["value"]?.GetValue<string>(),
                            ["text"] = o["label"]?.GetValue<string>() ?? o["text"]?.GetValue<string>()
                        });
                }
            }
            catch (JsonException)
            {
                // Bozuk Options JSON — boş choices döner (render kırılmaz, admin UI'da düzeltilir).
            }
            return choices;
        }

        // ValidationRules JSON: { minLength, maxLength, regex } — survey-core native property + regexvalidator.
        private static void ApplyValidation(JsonObject el, FormField field)
        {
            if (string.IsNullOrWhiteSpace(field.ValidationRules))
                return;

            JsonNode? rules;
            try { rules = JsonNode.Parse(field.ValidationRules); }
            catch (JsonException) { return; }

            if (rules is not JsonObject r)
                return;

            if (r["minLength"] is JsonValue minL && minL.TryGetValue(out int minLength))
                el["minLength"] = minLength;
            if (r["maxLength"] is JsonValue maxL && maxL.TryGetValue(out int maxLength))
                el["maxLength"] = maxLength;

            if (r["regex"] is JsonValue rx && rx.TryGetValue(out string? pattern) && !string.IsNullOrWhiteSpace(pattern))
            {
                el["validators"] = new JsonArray
                {
                    new JsonObject { ["type"] = "regex", ["regex"] = pattern }
                };
            }
        }
    }
}
