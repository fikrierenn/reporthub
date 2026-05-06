namespace ReportPanel.Services.Eval
{
    // AST node'ları. Her parser çıktısı bir IFormulaNode tree'sidir.
    // Evaluator tree'yi walker'la dolaşır, IDictionary<string, object?> row scope üstünde değer döner.
    public interface IFormulaNode { }

    // Sabit değer (sayı, string, bool, null)
    public sealed record LiteralNode(object? Value) : IFormulaNode;

    // Kolon referansı: row[Name] (case-insensitive lookup evaluator'da)
    public sealed record ColumnNode(string Name) : IFormulaNode;

    // İkili operatör: + - * / = != < > <= >= AND OR
    public sealed record BinaryOpNode(string Op, IFormulaNode Left, IFormulaNode Right) : IFormulaNode;

    // Tekli operatör: -x veya NOT x
    public sealed record UnaryOpNode(string Op, IFormulaNode Operand) : IFormulaNode;

    // IF(cond, thenVal, elseVal) veya IIF(...)
    public sealed record IfNode(IFormulaNode Condition, IFormulaNode Then, IFormulaNode Else) : IFormulaNode;

    // CASE WHEN c1 THEN v1 ... [ELSE eVal] END
    public sealed record CaseBranch(IFormulaNode Condition, IFormulaNode Value);
    public sealed record CaseNode(System.Collections.Generic.IReadOnlyList<CaseBranch> Branches, IFormulaNode? Else) : IFormulaNode;
}
