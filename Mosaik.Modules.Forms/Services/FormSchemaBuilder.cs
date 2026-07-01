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

        // "text"|"comment"|"dropdown"|"checkbox"|"radiogroup"|"boolean"|"file"|"comment"(signature placeholder — Faz 4 signature_pad ile değişir)
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
            FormFieldType.Signature => "comment", // Faz 4: signature_pad Canvas ile değişir
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
