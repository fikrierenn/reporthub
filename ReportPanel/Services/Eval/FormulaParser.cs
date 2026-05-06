using System.Collections.Generic;

namespace ReportPanel.Services.Eval
{
    // Plan 05 — Recursive descent parser, precedence climbing.
    // Hierarchy (lowest -> highest):
    //   formula = orExpr
    //   orExpr  = andExpr (OR andExpr)*
    //   andExpr = notExpr (AND notExpr)*
    //   notExpr = (NOT)? cmpExpr
    //   cmpExpr = addExpr ((= != <> < > <= >=) addExpr)?
    //   addExpr = mulExpr ((+ -) mulExpr)*
    //   mulExpr = unaryExpr ((* /) unaryExpr)*
    //   unaryExpr = (-)? primary
    //   primary = Number | String | Bool | Null | Ident | (formula) | IF(...) | IIF(...) | CASE...END
    public sealed class FormulaParser
    {
        private const int MaxDepth = 64;

        private readonly IReadOnlyList<FormulaToken> _tokens;
        private int _pos;
        private int _depth;

        private FormulaParser(IReadOnlyList<FormulaToken> tokens)
        {
            _tokens = tokens;
            _pos = 0;
            _depth = 0;
        }

        public static IFormulaNode Parse(string source)
        {
            var tokens = FormulaTokenizer.Tokenize(source);
            var parser = new FormulaParser(tokens);
            var node = parser.ParseFormula();
            parser.Expect(TokenType.EOF);
            return node;
        }

        public static bool TryParse(string source, out IFormulaNode? node, out string? error, out int errorPos)
        {
            try
            {
                node = Parse(source);
                error = null;
                errorPos = 0;
                return true;
            }
            catch (FormulaParseException ex)
            {
                node = null;
                error = ex.Message;
                errorPos = ex.Position;
                return false;
            }
        }

        private FormulaToken Peek() => _tokens[_pos];
        private FormulaToken Advance() => _tokens[_pos++];

        private bool Match(TokenType t)
        {
            if (Peek().Type == t) { Advance(); return true; }
            return false;
        }

        private FormulaToken Expect(TokenType t)
        {
            var tok = Peek();
            if (tok.Type != t)
                throw new FormulaParseException($"Beklenen: {TokenName(t)}, görülen: {TokenName(tok.Type)} '{tok.Lexeme}'", tok.Position);
            return Advance();
        }

        private void EnterDepth()
        {
            _depth++;
            if (_depth > MaxDepth)
                throw new FormulaParseException($"Formül çok derin (iç içe limit: {MaxDepth}).", Peek().Position);
        }

        private void ExitDepth() => _depth--;

        private IFormulaNode ParseFormula() => ParseOr();

        private IFormulaNode ParseOr()
        {
            EnterDepth();
            var left = ParseAnd();
            while (Match(TokenType.Or))
            {
                var right = ParseAnd();
                left = new BinaryOpNode("OR", left, right);
            }
            ExitDepth();
            return left;
        }

        private IFormulaNode ParseAnd()
        {
            var left = ParseNot();
            while (Match(TokenType.And))
            {
                var right = ParseNot();
                left = new BinaryOpNode("AND", left, right);
            }
            return left;
        }

        private IFormulaNode ParseNot()
        {
            if (Match(TokenType.Not))
            {
                var operand = ParseCmp();
                return new UnaryOpNode("NOT", operand);
            }
            return ParseCmp();
        }

        private IFormulaNode ParseCmp()
        {
            var left = ParseAdd();
            var t = Peek().Type;
            string? op = t switch
            {
                TokenType.Eq => "=",
                TokenType.Neq => "!=",
                TokenType.Lt => "<",
                TokenType.Gt => ">",
                TokenType.Lte => "<=",
                TokenType.Gte => ">=",
                _ => null
            };
            if (op == null) return left;
            Advance();
            var right = ParseAdd();
            return new BinaryOpNode(op, left, right);
        }

        private IFormulaNode ParseAdd()
        {
            var left = ParseMul();
            while (true)
            {
                var t = Peek().Type;
                if (t == TokenType.Plus)
                {
                    Advance();
                    left = new BinaryOpNode("+", left, ParseMul());
                }
                else if (t == TokenType.Minus)
                {
                    Advance();
                    left = new BinaryOpNode("-", left, ParseMul());
                }
                else break;
            }
            return left;
        }

        private IFormulaNode ParseMul()
        {
            var left = ParseUnary();
            while (true)
            {
                var t = Peek().Type;
                if (t == TokenType.Star)
                {
                    Advance();
                    left = new BinaryOpNode("*", left, ParseUnary());
                }
                else if (t == TokenType.Slash)
                {
                    Advance();
                    left = new BinaryOpNode("/", left, ParseUnary());
                }
                else break;
            }
            return left;
        }

        private IFormulaNode ParseUnary()
        {
            if (Match(TokenType.Minus))
            {
                var operand = ParsePrimary();
                return new UnaryOpNode("-", operand);
            }
            // Plus prefix önemsiz
            if (Match(TokenType.Plus))
            {
                return ParsePrimary();
            }
            return ParsePrimary();
        }

        private IFormulaNode ParsePrimary()
        {
            var tok = Peek();
            switch (tok.Type)
            {
                case TokenType.Number:
                    Advance();
                    return new LiteralNode(tok.Value);
                case TokenType.String:
                    Advance();
                    return new LiteralNode(tok.Value);
                case TokenType.Bool:
                    Advance();
                    return new LiteralNode(tok.Value);
                case TokenType.Null:
                    Advance();
                    return new LiteralNode(null);
                case TokenType.Ident:
                    Advance();
                    return new ColumnNode((string)tok.Value!);
                case TokenType.LParen:
                    Advance();
                    var inner = ParseFormula();
                    Expect(TokenType.RParen);
                    return inner;
                case TokenType.If:
                case TokenType.Iif:
                    return ParseIfCall();
                case TokenType.Case:
                    return ParseCase();
                default:
                    throw new FormulaParseException($"Beklenmedik sembol: {TokenName(tok.Type)} '{tok.Lexeme}'", tok.Position);
            }
        }

        private IFormulaNode ParseIfCall()
        {
            Advance(); // IF veya IIF
            Expect(TokenType.LParen);
            var cond = ParseFormula();
            Expect(TokenType.Comma);
            var thn = ParseFormula();
            Expect(TokenType.Comma);
            var els = ParseFormula();
            Expect(TokenType.RParen);
            return new IfNode(cond, thn, els);
        }

        private IFormulaNode ParseCase()
        {
            Advance(); // CASE
            var branches = new List<CaseBranch>();
            if (Peek().Type != TokenType.When)
                throw new FormulaParseException("CASE sonrası WHEN bekleniyor.", Peek().Position);

            while (Match(TokenType.When))
            {
                var cond = ParseFormula();
                Expect(TokenType.Then);
                var val = ParseFormula();
                branches.Add(new CaseBranch(cond, val));
            }

            IFormulaNode? elseNode = null;
            if (Match(TokenType.Else))
                elseNode = ParseFormula();

            Expect(TokenType.End);
            return new CaseNode(branches, elseNode);
        }

        private static string TokenName(TokenType t) => t switch
        {
            TokenType.LParen => "(",
            TokenType.RParen => ")",
            TokenType.Comma => ",",
            TokenType.End => "END",
            TokenType.Then => "THEN",
            TokenType.When => "WHEN",
            TokenType.Else => "ELSE",
            TokenType.EOF => "formül sonu",
            _ => t.ToString()
        };
    }
}
