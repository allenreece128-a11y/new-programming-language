using System;
using System.Collections.Generic;

namespace NovaScript.Core
{
    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _current = 0;

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
        }

        public List<Stmt> Parse()
        {
            var statements = new List<Stmt>();
            while (!IsAtEnd())
            {
                var stmt = Declaration();
                if (stmt != null) statements.Add(stmt);
            }
            return statements;
        }

        private Stmt? Declaration()
        {
            try
            {
                if (Match(TokenType.MutableVar)) return VarDeclaration(true);
                if (Match(TokenType.ImmutableVar)) return VarDeclaration(false);
                if (Match(TokenType.Function)) return FunctionDeclaration("function");
                if (Match(TokenType.Structure)) return StructureDeclaration();
                return Statement();
            }
            catch (Exception)
            {
                Synchronize();
                return null;
            }
        }

        private Stmt VarDeclaration(bool isMutable)
        {
            Token name = Consume(TokenType.Identifier, "Expect variable name.");
            Expr? initializer = null;
            if (Match(TokenType.Equal))
            {
                initializer = Expression();
            }
            Consume(TokenType.Semicolon, "Expect ';' after variable declaration.");
            return new VarStmt(name, initializer, isMutable);
        }

        private FunctionStmt FunctionDeclaration(string kind)
        {
            Token name = Consume(TokenType.Identifier, $"Expect {kind} name.");
            Consume(TokenType.LeftParen, $"Expect '(' after {kind} name.");
            var parameters = new List<Parameter>();
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    bool isMutable = true;
                    if (Match(TokenType.MutableVar)) isMutable = true;
                    else if (Match(TokenType.ImmutableVar)) isMutable = false;
                    
                    Token paramName = Consume(TokenType.Identifier, "Expect parameter name.");
                    parameters.Add(new Parameter(paramName, isMutable));
                } while (Match(TokenType.Comma));
            }
            Consume(TokenType.RightParen, "Expect ')' after parameters.");
            
            Consume(TokenType.BlockStart, $"Expect '::' before {kind} body.");
            var body = Block();
            return new FunctionStmt(name, parameters, body);
        }

        private Stmt StructureDeclaration()
        {
            Token name = Consume(TokenType.Identifier, "Expect structure name.");
            Consume(TokenType.BlockStart, "Expect '::' before structure body.");
            
            var properties = new List<VarStmt>();
            var methods = new List<FunctionStmt>();
            
            while (!Check(TokenType.BlockEnd) && !IsAtEnd())
            {
                if (Match(TokenType.MutableVar)) properties.Add((VarStmt)VarDeclaration(true));
                else if (Match(TokenType.ImmutableVar)) properties.Add((VarStmt)VarDeclaration(false));
                else if (Match(TokenType.Function)) methods.Add(FunctionDeclaration("method"));
                else throw new Exception("Only properties and methods allowed in structure.");
            }
            
            Consume(TokenType.BlockEnd, "Expect ';;' after structure body.");
            return new StructureStmt(name, properties, methods);
        }

        private Stmt Statement()
        {
            if (Match(TokenType.Import)) return ImportStatement();
            if (Match(TokenType.Output)) return OutputStatement();
            if (Match(TokenType.If)) return IfStatement();
            if (Match(TokenType.Star)) return WhileStatement();
            if (Match(TokenType.Break)) return BreakStatement();
            if (Match(TokenType.Continue)) return ContinueStatement();
            if (Match(TokenType.Return)) return ReturnStatement();
            if (Match(TokenType.Try)) return TryCatchStatement();
            if (Check(TokenType.BlockStart)) 
            {
                Advance();
                return new BlockStmt(Block());
            }
            return ExpressionStatement();
        }

        private Stmt ImportStatement()
        {
            Token keyword = Previous();
            Expr pathExpression = Expression();
            Consume(TokenType.Semicolon, "Expect ';' after import path.");
            return new ImportStmt(keyword, pathExpression);
        }

        private Stmt OutputStatement()
        {
            Expr value = Expression();
            Consume(TokenType.Semicolon, "Expect ';' after value.");
            return new OutputStmt(value);
        }

        private Stmt IfStatement()
        {
            Expr condition = Expression();
            Consume(TokenType.BlockStart, "Expect '::' after if condition.");
            Stmt thenBranch = new BlockStmt(Block());
            Stmt? elseBranch = null;
            if (Match(TokenType.Else))
            {
                Consume(TokenType.BlockStart, "Expect '::' after else.");
                elseBranch = new BlockStmt(Block());
            }
            return new IfStmt(condition, thenBranch, elseBranch);
        }

        private Stmt WhileStatement()
        {
            Expr condition = Expression();
            Consume(TokenType.BlockStart, "Expect '::' after loop condition.");
            Stmt body = new BlockStmt(Block());
            return new WhileStmt(condition, body);
        }

        private Stmt BreakStatement()
        {
            Token keyword = Previous();
            Consume(TokenType.Semicolon, "Expect ';' after break.");
            return new BreakStmt(keyword);
        }

        private Stmt ContinueStatement()
        {
            Token keyword = Previous();
            Consume(TokenType.Semicolon, "Expect ';' after continue.");
            return new ContinueStmt(keyword);
        }

        private Stmt ReturnStatement()
        {
            Token keyword = Previous();
            Expr? value = null;
            if (!Check(TokenType.Semicolon))
            {
                value = Expression();
            }
            Consume(TokenType.Semicolon, "Expect ';' after return value.");
            return new ReturnStmt(keyword, value);
        }

        private Stmt TryCatchStatement()
        {
            Consume(TokenType.BlockStart, "Expect '::' after try.");
            var tryBlock = Block();
            
            Token? catchVar = null;
            List<Stmt>? catchBlock = null;
            if (Match(TokenType.Catch))
            {
                if (Check(TokenType.Identifier))
                {
                    catchVar = Advance();
                }
                Consume(TokenType.BlockStart, "Expect '::' after catch.");
                catchBlock = Block();
            }
            
            List<Stmt>? finallyBlock = null;
            if (Match(TokenType.Finally))
            {
                Consume(TokenType.BlockStart, "Expect '::' after finally.");
                finallyBlock = Block();
            }
            
            return new TryCatchStmt(tryBlock, catchVar, catchBlock, finallyBlock);
        }

        private List<Stmt> Block()
        {
            var statements = new List<Stmt>();
            while (!Check(TokenType.BlockEnd) && !IsAtEnd())
            {
                var stmt = Declaration();
                if (stmt != null) statements.Add(stmt);
            }
            Consume(TokenType.BlockEnd, "Expect ';;' after block.");
            return statements;
        }

        private Stmt ExpressionStatement()
        {
            Expr expr = Expression();
            Consume(TokenType.Semicolon, "Expect ';' after expression.");
            return new ExpressionStmt(expr);
        }

        public Expr Expression() => Assignment();

        private Expr Assignment()
        {
            Expr expr = LogicalOr();
            if (Match(TokenType.Equal))
            {
                Token equals = Previous();
                Expr value = Assignment();
                if (expr is VariableExpr v) return new AssignExpr(v.Name, value);
                if (expr is GetExpr g) return new SetExpr(g.Object, g.Name, value);
                throw new Exception("Invalid assignment target.");
            }
            return expr;
        }

        private Expr LogicalOr()
        {
            Expr expr = LogicalAnd();
            // NovaScript doesn't have explicit OR/AND keywords in the dictionary but we can add them or use symbols.
            // The spec doesn't mention them but they are usually needed. 
            // I'll skip them for now or use | and & if I find them.
            return expr;
        }

        private Expr LogicalAnd() => Equality();

        private Expr Equality()
        {
            Expr expr = Comparison();
            while (Match(TokenType.EqualEqual, TokenType.BangEqual))
            {
                Token op = Previous();
                Expr right = Comparison();
                expr = new BinaryExpr(expr, op, right);
            }
            return expr;
        }

        private Expr Comparison()
        {
            Expr expr = Term();
            while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
            {
                Token op = Previous();
                Expr right = Term();
                expr = new BinaryExpr(expr, op, right);
            }
            return expr;
        }

        private Expr Term()
        {
            Expr expr = Factor();
            while (Match(TokenType.Plus, TokenType.Minus))
            {
                Token op = Previous();
                Expr right = Factor();
                expr = new BinaryExpr(expr, op, right);
            }
            return expr;
        }

        private Expr Factor()
        {
            Expr expr = Unary();
            while (Match(TokenType.Star, TokenType.Slash))
            {
                Token op = Previous();
                Expr right = Unary();
                expr = new BinaryExpr(expr, op, right);
            }
            return expr;
        }

        private Expr Unary()
        {
            if (Match(TokenType.Minus, TokenType.Catch)) // Using ! as NOT if needed
            {
                Token op = Previous();
                Expr right = Unary();
                return new UnaryExpr(op, right);
            }
            return Call();
        }

        private Expr Call()
        {
            Expr expr = Primary();
            while (true)
            {
                if (Match(TokenType.LeftParen))
                {
                    expr = FinishCall(expr);
                }
                else if (Match(TokenType.Dot))
                {
                    Token name = Consume(TokenType.Identifier, "Expect property name after '.'.");
                    expr = new GetExpr(expr, name);
                }
                else break;
            }
            return expr;
        }

        private Expr FinishCall(Expr callee)
        {
            var arguments = new List<Expr>();
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    arguments.Add(Expression());
                } while (Match(TokenType.Comma));
            }
            Token paren = Consume(TokenType.RightParen, "Expect ')' after arguments.");
            return new CallExpr(callee, paren, arguments);
        }

        private Expr Primary()
        {
            if (Match(TokenType.False)) return new LiteralExpr(false);
            if (Match(TokenType.True)) return new LiteralExpr(true);
            if (Match(TokenType.Null)) return new LiteralExpr(null);
            
            if (Match(TokenType.Number)) return new LiteralExpr(double.Parse(Previous().Value));
            if (Match(TokenType.String)) return new LiteralExpr(Previous().Value);
            
            if (Match(TokenType.Identifier)) return new VariableExpr(Previous());

            if (Match(TokenType.New))
            {
                Token name = Consume(TokenType.Identifier, "Expect structure name after 'new'.");
                Consume(TokenType.LeftParen, "Expect '(' after structure name.");
                var arguments = new List<Expr>();
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        arguments.Add(Expression());
                    } while (Match(TokenType.Comma));
                }
                Consume(TokenType.RightParen, "Expect ')' after arguments.");
                return new NewExpr(name, arguments);
            }
            
            if (Match(TokenType.LeftParen))
            {
                Expr expr = Expression();
                Consume(TokenType.RightParen, "Expect ')' after expression.");
                return expr;
            }
            
            throw new Exception($"Expect expression at line {Peek().Line}. Found {Peek().Type} ({Peek().Value})");
        }

        private bool Match(params TokenType[] types)
        {
            foreach (var type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return Peek().Type == type;
        }

        private Token Advance()
        {
            if (!IsAtEnd()) _current++;
            return Previous();
        }

        private bool IsAtEnd() => Peek().Type == TokenType.EOF;

        private Token Peek() => _tokens[_current];

        private Token Previous() => _tokens[_current - 1];

        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            throw new Exception($"{message} at line {Peek().Line}");
        }

        private void Synchronize()
        {
            Advance();
            while (!IsAtEnd())
            {
                if (Previous().Type == TokenType.Semicolon) return;
                switch (Peek().Type)
                {
                    // Add more statement starters here if needed
                    case TokenType.MutableVar:
                    case TokenType.ImmutableVar:
                    case TokenType.Function:
                    case TokenType.Structure:
                    case TokenType.If:
                    case TokenType.Loop:
                    case TokenType.Output:
                    case TokenType.Import:
                    case TokenType.Break:
                    case TokenType.Continue:
                    case TokenType.Return:
                        return;
                }
                Advance();
            }
        }
    }
}
