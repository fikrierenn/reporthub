using System;
using System.Collections.Generic;

namespace ReportPanel.Services.Eval
{
    // Plan 05 — AST tree walker. SQL benzeri semantik:
    //   * null arithmetic/comparison/logic 3VL (1 + NULL = NULL, NULL = 1 = NULL).
    //   * Aritmetik: decimal precision (SQL money/decimal koruma).
    //   * Sıfıra bölme: null (cell "—" render — production'da çökmez).
    //   * Type mismatch (string + number gibi): FormulaEvaluationException.
    //   * Tanımsız kolon: FormulaEvaluationException.
    //   * Kolon lookup: caller sağladığı IDictionary<string, object?> üzerinden
    //     (case-insensitive isteniyorsa caller OrdinalIgnoreCase comparer dict gönderir).
    public sealed class FormulaEvaluator
    {
        private readonly IFormulaNode _root;

        public FormulaEvaluator(IFormulaNode root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
        }

        public static FormulaEvaluator FromSource(string source)
            => new FormulaEvaluator(FormulaParser.Parse(source));

        public object? Evaluate(IDictionary<string, object?> row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            return Evaluate(name => row.TryGetValue(name, out var v) ? v : throw new FormulaEvaluationException($"Tanımsız kolon: {name}"));
        }

        // Esnek overload: caller kolon lookup'ı sağlar (renderer Dictionary<string,object> için kullanır).
        // getColumn kontratı: kolon yoksa FormulaEvaluationException at; varsa value (DBNull dahil) dön.
        public object? Evaluate(Func<string, object?> getColumn)
        {
            if (getColumn == null) throw new ArgumentNullException(nameof(getColumn));
            return Eval(_root, getColumn);
        }

        private static object? Eval(IFormulaNode node, Func<string, object?> getColumn)
        {
            switch (node)
            {
                case LiteralNode lit:
                    return lit.Value;

                case ColumnNode col:
                    var val = getColumn(col.Name);
                    return val is DBNull ? null : val;

                case UnaryOpNode un:
                    return EvalUnary(un, getColumn);

                case BinaryOpNode bin:
                    return EvalBinary(bin, getColumn);

                case IfNode ifn:
                    {
                        var cond = ToBool(Eval(ifn.Condition, getColumn));
                        if (cond == true) return Eval(ifn.Then, getColumn);
                        if (cond == false) return Eval(ifn.Else, getColumn);
                        return null; // null condition → null
                    }

                case CaseNode cn:
                    foreach (var br in cn.Branches)
                    {
                        var c = ToBool(Eval(br.Condition, getColumn));
                        if (c == true) return Eval(br.Value, getColumn);
                    }
                    return cn.Else != null ? Eval(cn.Else, getColumn) : null;

                default:
                    throw new FormulaEvaluationException($"Bilinmeyen node tipi: {node.GetType().Name}");
            }
        }

        private static object? EvalUnary(UnaryOpNode un, Func<string, object?> getColumn)
        {
            var v = Eval(un.Operand, getColumn);
            if (v == null) return null;
            return un.Op switch
            {
                "-" => -ToDecimal(v),
                "NOT" => !(ToBool(v) ?? throw new FormulaEvaluationException("NOT için bool değer bekleniyor.")),
                _ => throw new FormulaEvaluationException($"Bilinmeyen tek operatör: {un.Op}")
            };
        }

        private static object? EvalBinary(BinaryOpNode bin, Func<string, object?> getColumn)
        {
            // Short-circuit logic operatörleri
            if (bin.Op == "AND")
            {
                var l = ToBool(Eval(bin.Left, getColumn));
                if (l == false) return false;
                var r = ToBool(Eval(bin.Right, getColumn));
                if (r == false) return false;
                if (l == null || r == null) return null;
                return true;
            }
            if (bin.Op == "OR")
            {
                var l = ToBool(Eval(bin.Left, getColumn));
                if (l == true) return true;
                var r = ToBool(Eval(bin.Right, getColumn));
                if (r == true) return true;
                if (l == null || r == null) return null;
                return false;
            }

            var lv = Eval(bin.Left, getColumn);
            var rv = Eval(bin.Right, getColumn);

            // 3VL — herhangi biri null → null (eşitlik dahil)
            if (lv == null || rv == null) return null;

            switch (bin.Op)
            {
                case "+": return ToDecimal(lv) + ToDecimal(rv);
                case "-": return ToDecimal(lv) - ToDecimal(rv);
                case "*": return ToDecimal(lv) * ToDecimal(rv);
                case "/":
                    {
                        var d = ToDecimal(rv);
                        if (d == 0m) return null; // sıfıra bölme — null (render skip)
                        return ToDecimal(lv) / d;
                    }
                case "=": return CompareEq(lv, rv);
                case "!=": return !CompareEq(lv, rv);
                case "<": return CompareOrd(lv, rv) < 0;
                case ">": return CompareOrd(lv, rv) > 0;
                case "<=": return CompareOrd(lv, rv) <= 0;
                case ">=": return CompareOrd(lv, rv) >= 0;
                default:
                    throw new FormulaEvaluationException($"Bilinmeyen operatör: {bin.Op}");
            }
        }

        private static decimal ToDecimal(object v)
        {
            try
            {
                return v switch
                {
                    decimal d => d,
                    double db => (decimal)db,
                    float f => (decimal)f,
                    long l => l,
                    int i => i,
                    short s => s,
                    byte b => b,
                    sbyte sb => sb,
                    uint ui => ui,
                    ulong ul => ul,
                    ushort us => us,
                    bool _ => throw new FormulaEvaluationException("Bool aritmetiğe katılamaz."),
                    string _ => throw new FormulaEvaluationException("String sayısal işleme katılamaz."),
                    DateTime _ => throw new FormulaEvaluationException("Tarih aritmetiğe katılamaz (v1)."),
                    _ => Convert.ToDecimal(v, System.Globalization.CultureInfo.InvariantCulture)
                };
            }
            catch (FormatException)
            {
                throw new FormulaEvaluationException($"Sayıya çevrilemedi: {v}");
            }
            catch (OverflowException)
            {
                throw new FormulaEvaluationException($"Sayı taştı: {v}");
            }
        }

        private static bool? ToBool(object? v)
        {
            if (v == null) return null;
            if (v is bool b) return b;
            throw new FormulaEvaluationException($"Bool bekleniyordu: {v}");
        }

        private static bool CompareEq(object lv, object rv)
        {
            // Aynı tip eşitliği — type promote: numeric ailesi decimal'a normalize.
            if (IsNumeric(lv) && IsNumeric(rv))
                return ToDecimal(lv) == ToDecimal(rv);

            if (lv is string ls && rv is string rs)
                return string.Equals(ls, rs, StringComparison.Ordinal);

            if (lv is bool lb && rv is bool rb)
                return lb == rb;

            // Cross-type: false (Java/SQL semantik karışık ama "1 = '1'" reddedilsin)
            return false;
        }

        private static int CompareOrd(object lv, object rv)
        {
            if (IsNumeric(lv) && IsNumeric(rv))
                return ToDecimal(lv).CompareTo(ToDecimal(rv));

            if (lv is string ls && rv is string rs)
                return string.Compare(ls, rs, StringComparison.Ordinal);

            throw new FormulaEvaluationException(
                $"Karşılaştırma için aynı tip bekleniyor: {lv?.GetType().Name} vs {rv?.GetType().Name}");
        }

        private static bool IsNumeric(object v)
            => v is decimal or double or float or long or int or short or byte or sbyte or uint or ulong or ushort;
    }
}
