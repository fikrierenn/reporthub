namespace ReportPanel.Services.Eval
{
    // Plan 05 — Formula DSL token tipleri (whitelist).
    // Bu enum dışındaki hiçbir sembol kabul edilmez (security).
    public enum TokenType
    {
        // Literals
        Number,      // 12, 12.5
        String,      // 'metin'
        Bool,        // TRUE, FALSE
        Null,        // NULL

        // Identifier (kolon adı veya keyword)
        Ident,       // [Şube] veya bareword

        // Brackets
        LBracket,    // [
        RBracket,    // ]
        LParen,      // (
        RParen,      // )
        Comma,       // ,

        // Arithmetic
        Plus,        // +
        Minus,       // -
        Star,        // *
        Slash,       // /

        // Comparison
        Eq,          // =
        Neq,         // != veya <>
        Lt,          // <
        Gt,          // >
        Lte,         // <=
        Gte,         // >=

        // Logic
        And,         // AND
        Or,          // OR
        Not,         // NOT

        // Conditional
        If,          // IF
        Iif,         // IIF
        Case,        // CASE
        When,        // WHEN
        Then,        // THEN
        Else,        // ELSE
        End,         // END

        // Sentinel
        EOF
    }

    // Konum bilgili token kaydı (hata mesajları için 1-tabanlı pozisyon).
    public sealed record FormulaToken(TokenType Type, string Lexeme, int Position, object? Value = null);

    public class FormulaParseException : System.Exception
    {
        public int Position { get; }
        public FormulaParseException(string message, int position) : base(message)
        {
            Position = position;
        }
    }

    public class FormulaEvaluationException : System.Exception
    {
        public FormulaEvaluationException(string message) : base(message) { }
    }
}
