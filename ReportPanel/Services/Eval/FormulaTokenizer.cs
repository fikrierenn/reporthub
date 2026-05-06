using System;
using System.Collections.Generic;
using System.Globalization;

namespace ReportPanel.Services.Eval
{
    // Plan 05 — Char-by-char state machine. Whitelist dışı sembol → FormulaParseException.
    // Türkçe kolon adları: identifier Unicode letter category (Şube, Çıkış).
    // String literal SQL stili: '...' içinde, '' = literal apostrophe.
    public static class FormulaTokenizer
    {
        private const int MaxLength = 4096;       // DoS mitigation
        private const int MaxTokenCount = 1000;

        private static readonly Dictionary<string, TokenType> Keywords =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "AND", TokenType.And },
                { "OR", TokenType.Or },
                { "NOT", TokenType.Not },
                { "IF", TokenType.If },
                { "IIF", TokenType.Iif },
                { "CASE", TokenType.Case },
                { "WHEN", TokenType.When },
                { "THEN", TokenType.Then },
                { "ELSE", TokenType.Else },
                { "END", TokenType.End },
                { "TRUE", TokenType.Bool },
                { "FALSE", TokenType.Bool },
                { "NULL", TokenType.Null }
            };

        public static IReadOnlyList<FormulaToken> Tokenize(string source)
        {
            if (source == null) throw new FormulaParseException("Boş formül.", 0);
            if (source.Length > MaxLength)
                throw new FormulaParseException($"Formül çok uzun ({source.Length} > {MaxLength}).", 0);

            var tokens = new List<FormulaToken>();
            int i = 0;

            while (i < source.Length)
            {
                if (tokens.Count > MaxTokenCount)
                    throw new FormulaParseException("Formül çok karmaşık (token sayısı limit aşıldı).", i + 1);

                char c = source[i];

                if (char.IsWhiteSpace(c)) { i++; continue; }

                if (char.IsDigit(c))
                {
                    int start = i;
                    while (i < source.Length && char.IsDigit(source[i])) i++;
                    if (i < source.Length && source[i] == '.')
                    {
                        i++;
                        while (i < source.Length && char.IsDigit(source[i])) i++;
                    }
                    string numLex = source.Substring(start, i - start);
                    if (!decimal.TryParse(numLex, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
                        throw new FormulaParseException($"Geçersiz sayı: {numLex}", start + 1);
                    tokens.Add(new FormulaToken(TokenType.Number, numLex, start + 1, num));
                    continue;
                }

                if (c == '\'')
                {
                    int start = i;
                    i++; // skip opening quote
                    var sb = new System.Text.StringBuilder();
                    bool closed = false;
                    while (i < source.Length)
                    {
                        if (source[i] == '\'')
                        {
                            if (i + 1 < source.Length && source[i + 1] == '\'')
                            {
                                sb.Append('\'');
                                i += 2;
                                continue;
                            }
                            i++;
                            closed = true;
                            break;
                        }
                        sb.Append(source[i]);
                        i++;
                    }
                    if (!closed)
                        throw new FormulaParseException("String literal kapatılmadı (eksik tek tırnak).", start + 1);
                    tokens.Add(new FormulaToken(TokenType.String, sb.ToString(), start + 1, sb.ToString()));
                    continue;
                }

                if (c == '[')
                {
                    int start = i;
                    i++;
                    var sb = new System.Text.StringBuilder();
                    bool closed = false;
                    while (i < source.Length)
                    {
                        if (source[i] == ']') { closed = true; i++; break; }
                        sb.Append(source[i]);
                        i++;
                    }
                    if (!closed)
                        throw new FormulaParseException("Köşeli parantez kapatılmadı: [", start + 1);
                    string name = sb.ToString().Trim();
                    if (name.Length == 0)
                        throw new FormulaParseException("Boş kolon referansı: []", start + 1);
                    tokens.Add(new FormulaToken(TokenType.Ident, name, start + 1, name));
                    continue;
                }

                if (IsIdentStart(c))
                {
                    int start = i;
                    while (i < source.Length && IsIdentPart(source[i])) i++;
                    string lex = source.Substring(start, i - start);
                    if (Keywords.TryGetValue(lex, out var kw))
                    {
                        object? val = kw switch
                        {
                            TokenType.Bool => string.Equals(lex, "TRUE", StringComparison.OrdinalIgnoreCase),
                            TokenType.Null => null,
                            _ => null
                        };
                        tokens.Add(new FormulaToken(kw, lex, start + 1, val));
                    }
                    else
                    {
                        tokens.Add(new FormulaToken(TokenType.Ident, lex, start + 1, lex));
                    }
                    continue;
                }

                // Tek karakter operatörler / iki karakter operatörler
                switch (c)
                {
                    case '(': tokens.Add(new FormulaToken(TokenType.LParen, "(", i + 1)); i++; continue;
                    case ')': tokens.Add(new FormulaToken(TokenType.RParen, ")", i + 1)); i++; continue;
                    case ',': tokens.Add(new FormulaToken(TokenType.Comma, ",", i + 1)); i++; continue;
                    case '+': tokens.Add(new FormulaToken(TokenType.Plus, "+", i + 1)); i++; continue;
                    case '-': tokens.Add(new FormulaToken(TokenType.Minus, "-", i + 1)); i++; continue;
                    case '*': tokens.Add(new FormulaToken(TokenType.Star, "*", i + 1)); i++; continue;
                    case '/': tokens.Add(new FormulaToken(TokenType.Slash, "/", i + 1)); i++; continue;
                    case '=': tokens.Add(new FormulaToken(TokenType.Eq, "=", i + 1)); i++; continue;
                    case '!':
                        if (i + 1 < source.Length && source[i + 1] == '=')
                        {
                            tokens.Add(new FormulaToken(TokenType.Neq, "!=", i + 1));
                            i += 2; continue;
                        }
                        throw new FormulaParseException("Bilinmeyen sembol: ! (! tek başına geçersiz, != bekleniyor)", i + 1);
                    case '<':
                        if (i + 1 < source.Length && source[i + 1] == '=')
                        {
                            tokens.Add(new FormulaToken(TokenType.Lte, "<=", i + 1));
                            i += 2; continue;
                        }
                        if (i + 1 < source.Length && source[i + 1] == '>')
                        {
                            tokens.Add(new FormulaToken(TokenType.Neq, "<>", i + 1));
                            i += 2; continue;
                        }
                        tokens.Add(new FormulaToken(TokenType.Lt, "<", i + 1)); i++; continue;
                    case '>':
                        if (i + 1 < source.Length && source[i + 1] == '=')
                        {
                            tokens.Add(new FormulaToken(TokenType.Gte, ">=", i + 1));
                            i += 2; continue;
                        }
                        tokens.Add(new FormulaToken(TokenType.Gt, ">", i + 1)); i++; continue;
                    default:
                        throw new FormulaParseException($"Bilinmeyen sembol: {c}", i + 1);
                }
            }

            tokens.Add(new FormulaToken(TokenType.EOF, "", source.Length + 1));
            return tokens;
        }

        private static bool IsIdentStart(char c)
        {
            // Unicode harf veya alt çizgi (Türkçe Ş, Ç, Ğ, Ü, Ö, İ, ı dahil).
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            return cat == UnicodeCategory.UppercaseLetter
                || cat == UnicodeCategory.LowercaseLetter
                || cat == UnicodeCategory.TitlecaseLetter
                || cat == UnicodeCategory.ModifierLetter
                || cat == UnicodeCategory.OtherLetter
                || c == '_';
        }

        private static bool IsIdentPart(char c)
        {
            return IsIdentStart(c) || char.IsDigit(c);
        }
    }
}
